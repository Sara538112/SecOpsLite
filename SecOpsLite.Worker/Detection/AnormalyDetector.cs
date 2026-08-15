using SecOpsLite.Worker.Models;

namespace SecOpsLite.Worker.Detection;

//Tum kurallar calistirma yeri
public class AnormalyDetector{
    private readonly IEnumerable<IAnormalyRule> _rules;
    public AnormalyDetector(IEnumerable <IAnormalyRule> rules){
        _rules = rules;
    }
    public List <(string RuleName , AnormalyResult Result)> DetectAnormalies(List<NetworkPacket> recentPackets){
        var detectedAnormalies= new List<(string , AnormalyResult)>();
        foreach (var rule in _rules){
            var result = rule.Evaluate(recentPackets);
            if(result.IsAnormaly){
                detectedAnormalies.Add((rule.RuleName , result));
            }
        }
        return detectedAnormalies;
    }
}