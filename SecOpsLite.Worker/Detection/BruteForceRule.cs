using SecOpsLite.Worker.Models;

namespace SecOpsLite.Worker.Detection;

public class BruteForceRule : IAnormalyRule{
    public string RuleName => "Brute Force Tespiti";
    private const int Threshold = 5;
    private static readonly int[] SensitivePorts = {22, 3389};

    public AnormalyResult Evaluate(List<NetworkPacket> recentPackets){
        var suspiciousGroups = recentPackets
        .Where(p=> SensitivePorts.Contains(p.DestinationPort))
        .GroupBy(p=>p.SourceIp)
        .Where(g=>g.Count() >= Threshold)
        .ToList();

        if(suspiciousGroups.Count ==0)
        return new AnormalyResult(false , string.Empty);

        var worstOffender = suspiciousGroups.OrderByDescending(g=>g.Count()).First();

        return new AnormalyResult(
            true,
            $"{worstOffender.Key} adresinden {worstOffender.Count()} kez hassas port denemesi (SSH/RDP)",
            worstOffender.Key);
        
    }
}