using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;
using SecOpsLite.Worker.Ai;
using SecOpsLite.Worker.Analysis;
using SecOpsLite.Worker.Data;

namespace SecOpsLite.Worker;

public class SummaryWorker : BackgroundService{
private readonly ILogger<SummaryWorker> _logger;
private readonly IServiceProvider _serviceProvider;
private readonly TimeSpan _summaryInterval = TimeSpan.FromSeconds(60);
private readonly TimeSpan _lookbackWindow = TimeSpan.FromMinutes(5);
private HubConnection? _hubConnection;
private readonly IConfiguration _configuration;


public SummaryWorker(ILogger<SummaryWorker> logger , IServiceProvider serviceProvider, IConfiguration configuration){
    _logger = logger;
    _serviceProvider = serviceProvider;
    _configuration = configuration;
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken ){
    var hubUrl = _configuration["HubUrl"] ?? "http://localhost:5128/packetHub";

    _hubConnection = new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect()
        .Build();

await _hubConnection.StartAsync(stoppingToken);

while(!stoppingToken.IsCancellationRequested){
    await Task.Delay(_summaryInterval , stoppingToken);
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var groqService = scope.ServiceProvider.GetRequiredService<IGroqSummaryService>();
    var stats = await AnomalyAnalyzer.AnalyzeRecentAsync(context , _lookbackWindow);

    if(stats is null){
        _logger.LogInformation("Son {Minutes} dakikada anomali yok, özet üretilmedi.", _lookbackWindow.TotalMinutes);
        continue;
    }

    try{
        var summary = await groqService.GenerateSummaryAsync(stats);
          _logger.LogInformation("AI ÖZET: {Summary}", summary);

                await _hubConnection.InvokeAsync("SendSummary", new
                {
                    Summary = summary,
                    Timestamp = DateTime.UtcNow
                }, stoppingToken);
    }catch(Exception ex)
    {
        _logger.LogWarning("Özet üretilirken hata oluştu: {Error}", ex.Message);
    }
}
}

}