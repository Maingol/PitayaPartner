namespace PitayaPartner;

public enum MsgType : byte
{
    Request = 0x00,
    Notify = 0x01,
    Response = 0x02,
    Push = 0x03
}

public enum MsgFlags
{
    ErrorMask = 0x20,
    GzipMask = 0x10,
    MsgRouteCompressMask = 0x01,
    MsgTypeMask = 0x07,
    MsgRouteLengthMask = 0xFF,
    MsgHeadLength = 0x02
}

public class Message
{
    public MsgType Type { get; set; }    // message type
    public uint ID { get; set; }       // unique id, zero while notify mode
    public string Route { get; set; }  // route for locating service
    public byte[] Data { get; set; }   // payload
    public bool Compressed { get; set; } // is message compressed
    public bool Err { get; set; }      // is an error message
    
    // func routable(t Type) bool {
    //     return t == Request || t == Notify || t == Push
    // }

    public static bool Routable(MsgType t)
    {
        return t == MsgType.Request || t == MsgType.Notify || t == MsgType.Push;
    }

    // public static void TestRoutable()
    // {
    //     MsgType t = MsgType.Response;
    //     Console.WriteLine("Routable(t):"+Routable(t));
    // }
}