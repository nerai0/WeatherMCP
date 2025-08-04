using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using WeatherMCP.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddHttpClient()
    .AddLogging()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>()
    .WithTools<WeatherTools>();

builder.Services.AddTransient<WeatherTools>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var tools = scope.ServiceProvider.GetRequiredService<WeatherTools>();
    var result = await tools.GetCurrentWeather("Astana", "KZ");
    Console.Error.WriteLine($"[TEST] {result}");

    var forecast = await tools.GetWeatherForecast("Astana", "KZ");
    Console.Error.WriteLine($"[TEST_FORECAST] {forecast}");

    var alerts = await tools.GetWeatherAlerts("Astana", "KZ");
    Console.Error.WriteLine($"[TEST_ALERTS] {alerts}");
}

await host.RunAsync();