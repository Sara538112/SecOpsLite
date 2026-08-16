namespace SecOpsLite.Worker.Models;

public class AnomalyEvent{
    public int Id {get; set;}
    public string RuleName {get; set;}= string.Empty;
    public string Description {get; set;}=string.Empty;
    public string? SourceIp {get; set;}
    public DateTime DetectedAt {get;set;} = DateTime.UtcNow;
}