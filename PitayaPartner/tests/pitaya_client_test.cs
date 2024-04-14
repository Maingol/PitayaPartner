using System.Text;
using TouchSocket.Core;

namespace PitayaPartner;

public class pitaya_client_test
{
    public static PitayaTCPClient _ptc;
    public static void start()
    {
        HandshakeData hd = new HandshakeData
        {
            Sys = new HandshakeClientData
            {
                Platform = "mac",
                LibVersion = "0.3.5-release",
                BuildNumber = "20",
                Version = "1.0.0"
            },
            User = new Dictionary<string, object>
            {
                { "age", 30 }
            }
        };
        
        PitayaTCPClient ptc = new PitayaTCPClient(2000,hd);
        _ptc = ptc;
        ptc.ConnectTo("127.0.0.1:3250");

        ptc.WaitForConnection();//等待应用层握手完成
        
        ptc.RegPushMsgHandler("onMembers",MyPushMsgHandler);
        
        byte[] rspData = ptc.SafeSendRequest("room_srv.room.entry", new byte[]{});
        if (rspData != null)
        {
            RspForReq rsp = SerializeConvert.JsonDeserializeFromBytes<RspForReq>(rspData);
            ptc.touchClient.Logger.Debug($"收到响应 | 请求路由:room_srv.room.entry | 响应结果:{rsp.result}");
        }
        
        
        rspData = ptc.SafeSendRequest("room_srv.room.join", new byte[]{});
        if (rspData != null)
        {
            RspForReq rsp = SerializeConvert.JsonDeserializeFromBytes<RspForReq>(rspData);
            ptc.touchClient.Logger.Debug($"收到响应 | 请求路由:room_srv.room.join | 响应结果:{rsp.result}");
        }

        byte[] enc = SerializeConvert.JsonSerializeToBytes(new chat_message
        {
            name = "周杰伦",
            content = "大家好"
        });
        
        //notify类型，服务端不返回response
        ptc.SendRequestWithMsgType(MsgType.Notify, "room_srv.room.message", enc);

        Console.ReadKey();
    }
    
    public static void MyPushMsgHandler(Message msg)
    {
        _ptc.touchClient.Logger.Debug($"~~~~~~~收到服务端的push消息 route:{msg.Route} data:{Encoding.UTF8.GetString(msg.Data)}");
    }
}