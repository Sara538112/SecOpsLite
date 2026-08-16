using Microsoft.AspNetCore.SignalR;
namespace SecOpsLite.Web.Hubs;

//Yayin merkezi
//Worker , buraya packet gonderir
//Client burdan packeti alir
public class PacketHub:Hub{

    public async Task SendPacket(NetworkPacketDto packet){
        await Clients.All.SendAsync("ReceivePacket" , packet);
    }
    public async Task SendAnormaly( object anormaly){
        await Clients.All.SendAsync("ReceiveAnormaly" , anormaly);
    }
}

public class NetworkPacketDto{
    public string SourceIp {get; set;}=string.Empty;
    public string DestinationIp {get; set;}= string.Empty;
    public int DestinationPort {get; set;}
    public int SizeBytes {get; set;}
    public DateTime Timestamp {get; set;}
}

