namespace SecOpsLite.Worker.Models;

public class NetworkPacket{
    public string SourceIp {get; set;} = string.Empty;
    public string DestinationIp {get; set;}= string.Empty;
    public int DestinationPort {get; set;}
    public int SizeBytes {get; set;}
    public DateTime Timestamp {get; set;} = DateTime.UtcNow;
}