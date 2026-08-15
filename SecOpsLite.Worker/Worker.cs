using SecOpsLite.Worker.Simulation;
using SecOpsLite.Worker.Models;
using System.Threading.Channels;

namespace SecOpsLite.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ChannelWriter <NetworkPacket> _writer;
    private readonly FackePacketGenerator _packetGenerator = new();

    public Worker(ILogger<Worker> logger , ChannelWriter <NetworkPacket> writer)
    {
        _logger = logger;
        _writer = writer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var packet = _packetGenerator.GenerateRandomPacket();
            await _writer.WriteAsync(packet , stoppingToken);
            await Task.Delay(500, stoppingToken);
        }
    }
}
