using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace PitayaPartner;

public enum PacketType : byte
{
    Handshake = 0x01,
    HandshakeAck = 0x02,
    Heartbeat = 0x03,
    Data = 0x04,
    Kick = 0x05
}

public static class Constants
{
    public const int HeadLength = 4;
    public const int MaxPacketSize = 1 << 24; // 16MB
}

public class Packet:IFixedHeaderRequestInfo,IRequestInfoBuilder
{
    public Header header =new Header();
    public Message message =new Message();
    public HandshakeData handshakeRequest;
    public HandshakeDataRsp handshakeResponse;
    
    public void SetPacketType(PacketType typ)
    {
        this.header.packetType = typ;
    }
    
    public void SetHandshakeRequest(HandshakeData hd)
    {
        this.handshakeRequest = hd;
    }
    
    public void SetMessageType(MsgType typ)
    {
        this.message.Type = typ;
    }
    
    public void SetMessageId(uint id)
    {
        this.message.ID = id;
    }
    
    public void SetRoute(string route)
    {
        this.message.Route = route;
    }
    
    public void SetData(byte[] data)
    {
        this.message.Data = data;
    }
    
    int IFixedHeaderRequestInfo.BodyLength => this.header.bodyLength;
    
    bool IFixedHeaderRequestInfo.OnParsingBody(byte[] body)
    {
        if (this.header.packetType == PacketType.Kick)
        {
            Console.WriteLine("收到kick包，服务端主动将此客户端提出");
            return true;
        }

        if (this.header.packetType == PacketType.Heartbeat)
        {
            return true;
        }

        if (this.header.packetType == PacketType.Handshake)
        {
            this.handshakeResponse=SerializeConvert.JsonDeserializeFromBytes<HandshakeDataRsp>(body);
            return true;
        }

        // 此时满足 this.header.packetType == PacketType.Data
        
        if (body.Length < (int)MsgFlags.MsgHeadLength)
        {
            throw new Exception("invalid message");
        }
        
        byte flag = body[0];
        int offset = 1;
        this.message.Type = (MsgType)((flag >> 1) & (int)MsgFlags.MsgTypeMask);
        
        if (this.message.Type == MsgType.Request || this.message.Type == MsgType.Response)
        {
            uint id = 0;
            int i;
            // little end byte order
            // WARNING: must can be stored in 64 bits integer
            // variant length encode
            for (i = offset; i < body.Length; i++)
            {
                byte b = body[i];
                id += (uint)(b & 0x7F) << (7 * (i - offset));
                if (b < 128)
                {
                    offset = i + 1;
                    break;
                }
            }
            this.message.ID = id;
        }
        
        this.message.Err = (flag & (byte)MsgFlags.ErrorMask) == (byte)MsgFlags.ErrorMask;

        //response包里没有route，push包里有route
        int size = body.Length;
        if (Message.Routable(this.message.Type)) {
            byte rl = body[offset];
            offset++;
        
            this.message.Route = Encoding.UTF8.GetString(body, offset, rl);
            offset += rl;
        }
        
        if (offset > size)
        {
            return false;
        }

        this.message.Data = body.Skip(offset).ToArray();

        return true;
    }
    
    bool IFixedHeaderRequestInfo.OnParsingHeader(byte[] headerBytes)
    {
        if (headerBytes.Length != Constants.HeadLength)
        {
            throw new Exception("header length not expected");
        }

        this.header.packetType = (PacketType)headerBytes[0];
        
        if (headerBytes[0] < (int)PacketType.Handshake || headerBytes[0] > (int)PacketType.Kick)
        {
            //程序走到这里后会直接返回，控制台日志会打印出
            //2024-04-13 09:52:14 6949 | Error | unknown packet type
            throw new Exception("unknown packet type");
        }

        headerBytes[0] = 0x00; //这样实际上就是仅仅转换了header的后三个字节
        int size = TouchSocketBitConverter.BigEndian.ToInt32(headerBytes, 0);
        
        if (size > Constants.MaxPacketSize)
        {
            throw new Exception("header length not expected");
        }
        
        //解析header字节数组，并把解析结果赋给header对象。
        //以此来告诉adapter，body的长度。
        this.header.bodyLength = size;

        return true;
    }
    
    public int MaxLength => Constants.MaxPacketSize;
    
    public void Build(ByteBlock byteBlock)
    {
        byte[] encMsg =null;

        if (this.header.packetType == PacketType.Heartbeat)
        {
            encMsg = new byte[] { };
        }

        if (this.header.packetType == PacketType.Handshake)
        {
            encMsg = SerializeConvert.JsonSerializeToBytes(this.handshakeRequest);
        }

        if (this.header.packetType == PacketType.Data)
        {
            var m = new Message
            {
                Type = this.message.Type,
                ID = this.message.ID,
                Route = this.message.Route,
                Data = this.message.Data,
                Err = false
            };
            encMsg = MessagesEncoder.Encode(m);
        }

        if (this.header.packetType == PacketType.HandshakeAck)
        {
            encMsg = new byte[0];
        }
        
        byteBlock.Write((byte)this.header.packetType);
        byteBlock.Write(TouchSocketBitConverter.BigEndian.GetBytes(encMsg.Length).Skip(1).ToArray());
        byteBlock.Write(encMsg);
    }
    
    
    
    
    
    
    public static void test()
    {
        // byte[] ba=TouchSocketBitConverter.BigEndian.GetBytes(4).Skip(1).ToArray();
        // Console.WriteLine("buf.ToArray()");
        // foreach (byte b in ba.ToArray())
        // {
        //     Console.Write(b.ToString("X2") + " ");
        // }

        byte[] ba = {0x04,0x00,0x00,0x0B };
        
        //offset指定1后，会从ba的第二个字节开始往后转换，得到的只有三个字节，所以报错Specified argument was out of the range
        int size = TouchSocketBitConverter.BigEndian.ToInt32(ba, 0);
        
        Console.WriteLine("size:"+size);

        byte[] a = new byte[0];
        Console.WriteLine("a.Length:"+a.Length);
    }
}