namespace SecOpsLite.Worker.Analysis;

public record AnomalySummaryStats(
    int TotalAnomalies , 
    int BruteForceCount ,
    int LargeTransferCount ,
    List<string> TopOffenderIps
);