using SecOpsLite.Worker;
using SecOpsLite.Worker.Models;
using System.Threading.Channels;
using SecOpsLite.Worker.Processing;
using SecOpsLite.Worker.Detection;
using Microsoft.EntityFrameworkCore;
using SecOpsLite.Worker.Data;
using SecOpsLite.Worker.Ai;

var builder = Host.CreateApplicationBuilder(args);
var channel = Channel.CreateUnbounded <NetworkPacket>();

builder.Services.AddSingleton(channel);
builder.Services.AddSingleton(channel.Reader);
builder.Services.AddSingleton(channel.Writer);
builder.Services.AddSingleton<IAnormalyRule , BruteForceRule>();
builder.Services.AddSingleton<IAnormalyRule , LargeTransferRule>();
builder.Services.AddSingleton<AnormalyDetector>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<PacketConsumer>();
builder.Services.AddHttpClient<IGroqSummaryService, GroqSummaryService>();
builder.Services.AddHostedService<SummaryWorker>();
builder.Services.AddDbContext<AppDbContext>(options =>
 options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
host.Run();
