using Microsoft.EntityFrameworkCore;
using SecOpsLite.Worker.Data;

namespace SecOpsLite.Worker.Analysis;

public static class AnomalyAnalyzer{
    public static async Task<AnomalySummaryStats?> AnalyzeRecentAsync(AppDbContext context , TimeSpan window){
        var cutoff= DateTime.UtcNow - window;
        var recentEvents = await context.AnomalyEvents
            .Where(e => e.DetectedAt >= cutoff)
            .ToListAsync();

        if(recentEvents.Count ==0){
            return null;
        }
        var bruteForceCount=  recentEvents.Count(e=>e.RuleName == "Brute Force Tespiti");
        var largeTransferCount = recentEvents.Count(e => e.RuleName == "Anormal Veri Transferi");

        var topOffenderIps = recentEvents
            .Where( e => e.SourceIp is not null)
            .GroupBy( e => e.SourceIp)
            .OrderByDescending( g => g.Count())
            .Take(3)
            .Select( g=> $"{g.Key} ({g.Count()} olay)")
            .ToList();
        
        return new AnomalySummaryStats(
            TotalAnomalies : recentEvents.Count,
            BruteForceCount: bruteForceCount,
            LargeTransferCount: largeTransferCount,
            TopOffenderIps: topOffenderIps
        );
    }
}