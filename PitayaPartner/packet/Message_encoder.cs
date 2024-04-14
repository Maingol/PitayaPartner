namespace PitayaPartner;


public class MessagesEncoder
{
    // ------------------------------------------
    // |   type   |  flag  |       other        |
    // |----------|--------|--------------------|
    // | request  |----000-|<message id>|<route>|
    // | notify   |----001-|<route>             |
    // | response |----010-|<message id>        |
    // | push     |----011-|<route>             |
    // ------------------------------------------
    public static byte[] Encode(Message message)
    {
        List<byte> buf = new List<byte>();

        byte flag = (byte)message.Type;
        flag <<= 1;
        buf.Add(flag);

        if (message.Type == MsgType.Request || message.Type == MsgType.Response)
        {
            uint n = message.ID;
            while (true)
            {
                byte b = (byte)(n % 128);
                n >>= 7;
                if (n != 0)
                {
                    buf.Add((byte)(b + 128));
                }
                else
                {
                    buf.Add(b);
                    break;
                }
            }
        }
        
        buf.Add((byte)message.Route.Length);
        buf.AddRange(System.Text.Encoding.UTF8.GetBytes(message.Route));
        
        buf.AddRange(message.Data);
        
        return buf.ToArray();
    }
}