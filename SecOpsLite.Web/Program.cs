using SecOpsLite.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Components.Server.Circuits",
    LogLevel.Debug);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Components",
    LogLevel.Debug);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}


app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<SecOpsLite.Web.Hubs.PacketHub>("/packetHub");

app.Run();