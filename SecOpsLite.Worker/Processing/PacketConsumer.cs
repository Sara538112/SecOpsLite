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
    private readonly IConfiguration _configuration;


    public PacketConsumer(ILogger <PacketConsumer> logger , ChannelReader <NetworkPacket> reader ,
     AnormalyDetector anormalyDetector , IServiceProvider serviceProvider , IConfiguration configuration){
        _logger = logger;
        _reader = reader;
        _anormalyDetector = anormalyDetector;
        _serviceProvider = serviceProvider;
         _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hubUrl = _configuration["HubUrl"]
                    ?? "http://localhost:5128/packetHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        var connected = false;
        var retryCount = 0;

        while (!connected && retryCount < 10)
        {
        try
        {
            await _hubConnection.StartAsync(stoppingToken);
            connected = true;

            _logger.LogInformation(
                "SignalR Hub'na bağlandı: {HubUrl}",
                hubUrl);
        }
        catch (Exception ex)
        {
            retryCount++;

            _logger.LogWarning(
                "Hub'a bağlanılamadı ({Attempt}/10), 5 saniye sonra tekrar denenecek: {Error}",
                retryCount,
                ex.Message);

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
        }

        if (!connected)
        {
        _logger.LogError("SignalR Hub'a bağlanılamadı.");
        return;
        }



        await foreach (var packet in _reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync(
                        "SendPacket",
                        packet,
                        stoppingToken);
                }

                _recentPackets.Add(packet);

                var cutoff = DateTime.UtcNow - _analysisWindow;

                _recentPackets.RemoveAll(
                    p => p.Timestamp < cutoff);

                var anomalies =
                    _anormalyDetector.DetectAnormalies(_recentPackets);

                foreach (var (ruleName, result) in anomalies)
                {
                    try
                    {
                        var notificationKey =
                            $"{ruleName}:{result.SourceIp}";

                        if (_recentlyNotified.TryGetValue(
                            notificationKey,
                            out var lastNotified))
                        {
                            if (DateTime.UtcNow - lastNotified <
                                _notificationCooldown)
                            {
                                continue;
                            }
                        }

                        _recentlyNotified[notificationKey] =
                            DateTime.UtcNow;

                        _logger.LogWarning(
                            "ANOMALİ [{Rule}]: {Description}",
                            ruleName,
                            result.Description);

                        using var scope =
                            _serviceProvider.CreateScope();

                        var context =
                            scope.ServiceProvider
                                .GetRequiredService<AppDbContext>();

                        context.AnomalyEvents.Add(
                            new AnomalyEvent
                            {
                                RuleName = ruleName,
                                Description = result.Description,
                                SourceIp = result.SourceIp,
                                DetectedAt = DateTime.UtcNow
                            });

                        await context.SaveChangesAsync(
                            stoppingToken);

                        if (_hubConnection.State ==
                            HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync(
                                "SendAnormaly",
                                new
                                {
                                    RuleName = ruleName,
                                    Description = result.Description,
                                    Timestamp = DateTime.UtcNow
                                },
                                stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Anomali işlenirken hata oluştu. Rule: {Rule}",
                            ruleName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Packet işlenirken hata oluştu.");
            }
        }
    }
}