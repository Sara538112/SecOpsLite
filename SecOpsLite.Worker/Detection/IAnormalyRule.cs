using SecOpsLite.Worker.Models;
namespace SecOpsLite.Worker.Detection;

//anormal kurallarin sozlesmesi
public interface IAnormalyRule{
    string RuleName {get;}
    AnormalyResult Evaluate(List<NetworkPacket> resentPackets);
}

public record AnormalyResult (bool IsAnormaly , string Description , string? SourceIp = null);