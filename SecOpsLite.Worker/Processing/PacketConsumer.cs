using System.Threading.Channels;
using SecOpsLite.Worker.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SecOpsLite.Worker.Detection;

namespace SecOpsLite.Worker.Processing;

public class PacketConsumer : BackgroundService{
    private readonly ILogger <PacketConsumer> _logger;
    private readonly ChannelReader <NetworkPacket> _reader;
    private HubConnection? _hubConnection;
    private readonly AnormalyDetector _anormalyDetector;
    private readonly List<NetworkPacket> _recentPackets = new();
    private readonly TimeSpan _analysisWindow = TimeSpan.FromSeconds(10); 

    public PacketConsumer(ILogger <PacketConsumer> logger , ChannelReader <NetworkPacket> reader , AnormalyDetector anormalyDetector){
        _logger = logger;
        _reader = reader;
        _anormalyDetector = anormalyDetector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        //SignalR baglanti nesnesi
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5128/packetHub")
            .WithAutomaticReconnect()
            .Build();
        
        await _hubConnection.StartAsync(stoppingToken);
        _logger.LogInformation("SignalR Hub'na baglandi .");
        
        await foreach ( var packet in _reader.ReadAllAsync(stoppingToken)){
            await _hubConnection.InvokeAsync("SendPacket" , packet , stoppingToken);
            _recentPackets.Add(packet);
            var cutoff = DateTime.UtcNow - _analysisWindow;
            _recentPackets.RemoveAll(p => p.Timestamp < cutoff);

            var anormalies = _anormalyDetector.DetectAnormalies(_recentPackets);

            foreach ( var (ruleName , result) in anormalies){
                _logger.LogWarning("ANOMALİ [{Rule}]: {Description}", ruleName, result.Description);

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