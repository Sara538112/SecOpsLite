using SecOpsLite.Worker.Analysis;

namespace SecOpsLite.Worker.Ai;

public interface IGroqSummaryService{
    Task<string> GenerateSummaryAsync(AnomalySummaryStats stats);
}