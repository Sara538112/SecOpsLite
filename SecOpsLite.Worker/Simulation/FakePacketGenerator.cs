using SecOpsLite.Worker.Models;

namespace SecOpsLite.Worker.Simulation;

public class FackePacketGenerator {
    private readonly Random _random = new();
    private readonly string[] _sampleIps ={
        "192.168.1.10" , "192.168.1.11" , "10.0.0.5" , "203.0.113.42" , "198.51.100.7"
    };
    private readonly int[] _commonPorts = { 80 , 443 , 22, 3389 , 8080};

    public NetworkPacket GenerateRandomPacket(){
        return new NetworkPacket{
            SourceIp= _sampleIps[_random.Next(_sampleIps.Length)],            DestinationIp= "192.168.1.1",
            DestinationPort = _commonPorts[_random.Next(_commonPorts.Length)],
            SizeBytes = _random.Next(64 , 1500),
            Timestamp = DateTime.UtcNow
        };
    }
}