using SecOpsLite.Worker;
using SecOpsLite.Worker.Models;
using System.Threading.Channels;
using SecOpsLite.Worker.Processing;

var builder = Host.CreateApplicationBuilder(args);
var channel = Channel.CreateUnbounded <NetworkPacket>();

builder.Services.AddSingleton(channel);
builder.Services.AddSingleton(channel.Reader);
builder.Services.AddSingleton(channel.Writer);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<PacketConsumer>();

var host = builder.Build();
host.Run();
