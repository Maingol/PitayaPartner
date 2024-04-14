using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using TouchSocket.Core;
using TouchSocket.Sockets;
using TimersTimer = System.Timers.Timer;

namespace PitayaPartner
{
    public class PitayaTCPClient
    {
        private uint nextId;
        private readonly object idLock = new();
        public TcpClient touchClient;
        public bool connected;//应用层握手成功后置为true
        private readonly ConcurrentDictionary<uint,  Channel<byte[]>> responses=new();
        private readonly ConcurrentDictionary<string,  Channel<byte[]>> pushes=new();
        private readonly ConcurrentDictionary<string,  PushPacketHandler> pushPacketHandlers=new();
        private int reqTimeout; //超时时间，单位：毫秒
        private ManualResetEvent _connectedResetEvent = new ManualResetEvent(false);
        public delegate void PushPacketHandler(Message msg);

        public PitayaTCPClient(int timeout,HandshakeData handshakeRequest)
        {
            reqTimeout = timeout;
            touchClient = new TcpClient();
            touchClient.Connected = (client, e) =>
            {
                touchClient.Logger.Debug("客户端发送握手请求");
                var request = new Packet();
                request.SetPacketType(PacketType.Handshake);
                request.SetHandshakeRequest(handshakeRequest);
                touchClient.Send(request);
                return Task.CompletedTask;
            };
            touchClient.Disconnected = (client, e) =>
            {
                touchClient.Logger.Warning("连接已断开");
                return Task.CompletedTask;
            };//从服务器断开连接，当连接不成功时不会触发。
            touchClient.Received = async (client, e) =>
            {
                if (e.RequestInfo is Packet requestPackage)
                {
                    if (requestPackage.header.packetType == PacketType.Heartbeat)
                    {
                        touchClient.Logger.Debug("收到服务端发来的心跳");
                    }
                    
                    if (requestPackage.header.packetType == PacketType.Data)
                    {
                        if (requestPackage.message.Type == MsgType.Response)
                        {
                            Channel <byte[]> ch = GetResponseChannelForID(requestPackage.message.ID);
                            await ch.Writer.WriteAsync(requestPackage.message.Data);
                            ch.Writer.Complete();//标记写入完成
                        }
                        
                        if (requestPackage.message.Type == MsgType.Push)
                        {
                            // push消息的处理不像response那样需要请求发送后进行等待，所以push消息的处理可以直接让上层注册回调
                            var ph=pushPacketHandlers.GetOrAdd(requestPackage.message.Route, (key) => PushMsgHandler);
                            ph(requestPackage.message);
                        }
                    }
                    
                    if (requestPackage.header.packetType == PacketType.Handshake)
                    {
                        var request = new Packet();
                        request.SetPacketType(PacketType.HandshakeAck);
                        touchClient.Send(request);
                        connected = true;
                        OnHandshakeComplete();
                        touchClient.Logger.Debug("服务端发来的心跳时间："+requestPackage.handshakeResponse.sys.heartbeat);
                        
                        // 参考网址：https://blog.csdn.net/qq_33670157/article/details/104689571
                        var timer1 = new TimersTimer();
                        timer1.AutoReset = true;
                        timer1.Interval =requestPackage.handshakeResponse.sys.heartbeat*1000;
                        timer1.Elapsed += sendHeartbeatPkg;
                        timer1.Start();
                    }

                    if (requestPackage.header.packetType == PacketType.Kick)
                    {
                        touchClient.SafeClose();
                        touchClient.Logger.Debug("服务器主动将此客户端踢出");
                    }
                }
            };
        }
        
        public void sendHeartbeatPkg(object sender, System.Timers.ElapsedEventArgs e)
        {
            var request = new Packet();
            request.SetPacketType(PacketType.Heartbeat);
            touchClient.SendAsync(request);
            touchClient.Logger.Debug("心跳发送成功");
        }

        public void ConnectTo(string serverHost)
        {
            touchClient.Setup(GetConfig(serverHost)
                .SetTcpDataHandlingAdapter(()=>new Adapter())
            );//载入配置
            touchClient.Connect();//连接
            touchClient.Logger.Info("客户端成功连接");
        }
        
        public void OnHandshakeComplete()
        {
            _connectedResetEvent.Set();
        }
        
        public void WaitForConnection()
        {
            _connectedResetEvent.WaitOne(); // 这将阻塞当前线程直到事件被设置
            touchClient.Logger.Info("握手完成，连接建立成功");
        }
        
        public byte[] SendRequest(string route,byte[] data)
        {
            uint msgId;
            lock (idLock)
            {
                msgId=++nextId;
            }
            var request = new Packet();
            request.SetPacketType(PacketType.Data);
            request.SetMessageType(MsgType.Request);
            request.SetMessageId(msgId);
            request.SetRoute(route);
            request.SetData(data);
            touchClient.Send(request);
            
            // 同步方式等待并读取数据，支持超时
            Channel<byte[]> ch = GetResponseChannelForID(msgId);
            byte[] rspData = {};
            while (!ch.Reader.Completion.IsCompleted)
            {
                Task<bool> waitTask = ch.Reader.WaitToReadAsync().AsTask();

                if (waitTask.Wait(reqTimeout)) // 阻塞等待，直到数据可读或超时。channel中无数据时，主线程会阻塞于此。
                {
                    if (waitTask.Result && ch.Reader.TryRead(out byte[] item))
                    {
                        rspData = item;
                        RemoveResponseChannelForID(msgId);
                        break;
                    }
                }
                else
                {
                    throw new RequestTimeoutException(route);
                }
            }

            return rspData;
        }
        
        public byte[] SafeSendRequest(string route, byte[] data)
        {
            try
            {
                byte[] response = SendRequest(route, data);
                // touchClient.Logger.Debug("Response received successfully.");
                return response;
            }
            catch (RequestTimeoutException rte)
            {
                touchClient.Logger.Debug(rte.Message);
            }
            catch (Exception ex)
            {
                touchClient.Logger.Debug($"An error occurred: {ex.Message}");
            }
            return null; // 或者返回一个特定的错误码、空数组等，取决于你的错误处理策略
        }

        //处理push消息的默认回调
        public void PushMsgHandler(Message msg)
        {
            touchClient.Logger.Debug($"收到服务端的push消息 route:{msg.Route} data:{Encoding.UTF8.GetString(msg.Data)}");
        }

        //注册处理push消息的回调
        public void RegPushMsgHandler(string route,PushPacketHandler ph)
        {
            // pushPacketHandlers.GetOrAdd(route, (key) => PushMsgHandler);
            // 使用 AddOrUpdate 方法添加或更新一个值
            pushPacketHandlers.AddOrUpdate(
                route,                // 路由作为键
                ph,                   // 如果键不存在，使用该值创建新条目
                (key, existingVal) => ph // 如果键已存在，使用新的处理器覆盖旧的处理器
            );
        }
        
        public void SendRequestWithMsgType(MsgType msgType,string route,byte[] data)
        {
            lock (idLock)
            {
                nextId+=1;
            }
            var request = new Packet();
            request.SetPacketType(PacketType.Data);
            request.SetMessageType(msgType);
            request.SetMessageId(nextId);
            request.SetRoute(route);
            request.SetData(data);
            touchClient.Send(request);
        }

        private static TouchSocketConfig GetConfig(string host)
        {
            return new TouchSocketConfig()
                    .SetRemoteIPHost(new IPHost(host))
                    .ConfigurePlugins(a =>
                    {
                        a.UseReconnection(successCallback: (cli) => {cli.Logger.Debug("重连成功");})
                        .SetTick(TimeSpan.FromSeconds(1))
                        .UsePolling();
                    })
                    .ConfigureContainer(a =>
                    {
                        a.AddConsoleLogger();//添加一个日志注入
                    });
        }
        
        public Channel<byte[]> GetResponseChannelForID(uint id)
        {
            // 使用 GetOrAdd 方法获取或创建一个新的 Channel
            return responses.GetOrAdd(id, (key) => Channel.CreateBounded<byte[]>(new BoundedChannelOptions(10)
            {
                //缓冲区满时写等待。因为push消息的messageId都是0
                FullMode = BoundedChannelFullMode.Wait
            }));
        }
        
        public Channel<byte[]> GetPushChannelForRoute(string route)
        {
            // 使用 GetOrAdd 方法获取或创建一个新的 Channel
            return pushes.GetOrAdd(route, (key) => Channel.CreateBounded<byte[]>(new BoundedChannelOptions(10)
            {
                //缓冲区满时写等待。因为push消息的messageId都是0
                FullMode = BoundedChannelFullMode.Wait
            }));
        }

        public void RemoveResponseChannelForID(uint id)
        {
            // 尝试移除指定的键
            responses.Remove(id, out Channel<byte[]> removedChannel);
        }
    }
}