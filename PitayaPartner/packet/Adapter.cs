using TouchSocket.Core;

namespace PitayaPartner;

public class Adapter: CustomFixedHeaderDataHandlingAdapter<Packet>
{
    public override int HeaderLength => 4;
    protected override Packet GetInstance()
    {
        return new Packet();
    }
    
    //因为MyRequestPackage已经实现IRequestInfoBuilder接口，所以可以使用True。
    public override bool CanSendRequestInfo => true;
}