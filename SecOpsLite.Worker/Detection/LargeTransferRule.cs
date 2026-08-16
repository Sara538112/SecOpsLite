using SecOpsLite.Worker.Models;
namespace SecOpsLite.Worker.Detection;

//bir ipden gonderilen Toplam veri miktari , belirli bir esigi asarsa , veri sizdirma belirtisi olabilir

public class LargeTransferRule : IAnormalyRule{
    public string RuleName => "Anormal Veri Transferi";
    private const int TotalBytesThreshold = 5000;

    public AnormalyResult Evaluate(List<NetworkPacket> recentPackets){
        var transferTotals = recentPackets
            .GroupBy(p => p.SourceIp)
            .Select(g=> new
            {
                SourceIp = g.Key,
                TotalBytes = g.Sum( p => p.SizeBytes)
            }
            )
            .Where ( x=> x.TotalBytes >= TotalBytesThreshold)
            .OrderByDescending(x=> x.TotalBytes)
            .ToList();

        if(transferTotals.Count ==0)
        return new AnormalyResult(false, string.Empty);
        var worst = transferTotals.First();

        return new AnormalyResult(
        true,
        $"{worst.SourceIp} adresinden toplam {worst.TotalBytes} byte veri transferi tespit edildi",
        worst.SourceIp);
    
    }
}