using System.Threading.Channels;
using SecOpsLite.Worker.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SecOpsLite.Worker.Detection;
using SecOpsLite.Worker.Data;

namespace SecOpsLite.Worker.Processing;

public class PacketConsumer : BackgroundService{
    private readonly ILogger <PacketConsumer> _logger;
    private readonly ChannelReader <NetworkPacket> _reader;
    private readonly IServiceProvider _serviceProvider;
    private HubConnection? _hubConnection;
    private readonly AnormalyDetector _anormalyDetector;
    private readonly List<NetworkPacket> _recentPackets = new();
    private readonly TimeSpan _analysisWindow = TimeSpan.FromSeconds(10); 
    private readonly Dictionary<string , DateTime> _recentlyNotified = new();
    private readonly TimeSpan _notificationCooldown = TimeSpan.FromSeconds(30);

    public PacketConsumer(ILogger <PacketConsumer> logger , ChannelReader <NetworkPacket> reader ,
     AnormalyDetector anormalyDetector , IServiceProvider serviceProvider){
        _logger = logger;
        _reader = reader;
        _anormalyDetector = anormalyDetector;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        //SignalR baglanti nesnesi
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5128/packetHub")
            .WithAutomaticReconnect()
            .Build();
        
        await _hubConnection.StartAsync(stoppingToken);
        
        await foreach ( var packet in _reader.ReadAllAsync(stoppingToken)){
            await _hubConnection.InvokeAsync("SendPacket" , packet , stoppingToken);
            _recentPackets.Add(packet);

            var cutoff = DateTime.UtcNow - _analysisWindow;
            _recentPackets.RemoveAll(p => p.Timestamp < cutoff);

            var anormalies = _anormalyDetector.DetectAnormalies(_recentPackets);

            foreach ( var (ruleName , result) in anormalies){
                
                var notificationKey = $"{ruleName}:{result.SourceIp}";                
                if(_recentlyNotified.TryGetValue(notificationKey , out var lastNotified)){
                    if(DateTime.UtcNow - lastNotified < _notificationCooldown){
                        continue;
                    }
                }
                _recentlyNotified[notificationKey] = DateTime.UtcNow;

                _logger.LogWarning("ANOMALİ [{Rule}]: {Description}", ruleName, result.Description);
                
                //veritabanda kayet
                using (var scope =_serviceProvider.CreateScope()){
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    context.AnomalyEvents.Add(new AnomalyEvent{
                        RuleName = ruleName,
                        Description = result.Description,
                        SourceIp= result.SourceIp,
                        DetectedAt = DateTime.UtcNow
                    });

                    await context.SaveChangesAsync(stoppingToken);
                }
                
                await _hubConnection.InvokeAsync("SendAnormaly" , new
                {
                    RuleName = ruleName,
                    Description = result.Description,
                    Timestamp = DateTime.UtcNow
                } , stoppingToken);
            }
        
    

        }
    }
}