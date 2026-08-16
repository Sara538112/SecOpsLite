using SecOpsLite.Worker;
using SecOpsLite.Worker.Models;
using System.Threading.Channels;
using SecOpsLite.Worker.Processing;
using SecOpsLite.Worker.Detection;
using Microsoft.EntityFrameworkCore;
using SecOpsLite.Worker.Data;

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
builder.Services.AddDbContext<AppDbContext>(options =>
 options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
host.Run();
