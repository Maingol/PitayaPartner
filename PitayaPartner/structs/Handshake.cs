namespace PitayaPartner;

[Serializable]
public class HandshakeClientData
{
    public string Platform { get; set; }
    public string LibVersion { get; set; }
    public string BuildNumber { get; set; }
    public string Version { get; set; }
}

//客户端发送的握手数据
[Serializable]
public class HandshakeData
{
    public HandshakeClientData Sys { get; set; }
    public Dictionary<string, object> User { get; set; }
}

[Serializable]
public class HandshakeSys
{
    public Dictionary<string, ushort> dict { get; set; }

    public int heartbeat { get; set; }

    public string serializer { get; set; }
}

//服务断响应的握手数据
[Serializable]
public class HandshakeDataRsp
{
    public HandshakeSys sys { get; set; }
    public int code { get; set; }
}