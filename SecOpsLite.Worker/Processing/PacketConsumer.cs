using System.Threading.Channels;
using SecOpsLite.Worker.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace SecOpsLite.Worker.Processing;

public class PacketConsumer : BackgroundService{
    private readonly ILogger <PacketConsumer> _logger;
    private readonly ChannelReader <NetworkPacket> _reader;
    private HubConnection? _hubConnection;

    public PacketConsumer(ILogger <PacketConsumer> logger , ChannelReader <NetworkPacket> reader){
        _logger = logger;
        _reader = reader;
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
            _logger.LogInformation(
                "İşlendi: {SourceIp} -> {DestIp}:{Port} ({Size} byte)",
                packet.SourceIp , packet.DestinationIp , packet.DestinationPort , packet.SizeBytes
            );

            await _hubConnection.InvokeAsync("SendPacket" , packet , stoppingToken);
        }
    }
}