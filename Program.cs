using System.Diagnostics;
using System.Diagnostics.Metrics;

using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry;

public class Program
{
    public const string ServiceName = "Jerico.XYZ.OpenTelemetryTest";
    public const string ServiceVersion = "1.0.0";
    public static readonly ActivitySource ServiceActivitySource = new ActivitySource(ServiceName, ServiceVersion);
    private static readonly HttpClient httpClient = new HttpClient();
    private const string GetTrafficDataAction = "GetTrafficData";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var appResourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: ServiceName, serviceVersion: ServiceVersion);

        var meter = new Meter(ServiceName);
        var counter = meter.CreateCounter<long>("app.request-counter");

        builder.Services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                .AddConsoleExporter()
                .AddSource(ServiceActivitySource.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(builder =>
            {
                builder
                // .AddConsoleExporter()
                .AddMeter(meter.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
            })
            .StartWithHost();

        var app = builder.Build();

        app.MapGet("/", async () =>
        {
            var timeInterval = "Daily";
            var intervals = 7;
            using var serverActivity = ServiceActivitySource.StartActivity(GetTrafficDataAction, ActivityKind.Server);
            serverActivity?.SetTag("timeInterval", timeInterval);
            serverActivity?.SetTag("intervals", intervals);

            using var clientActivity = ServiceActivitySource.StartActivity(GetTrafficDataAction, ActivityKind.Client);
            var content = await httpClient.GetStringAsync("https://jerico.xyz/api/traffic?timeInterval=Daily&intervals=7");
            clientActivity?.SetTag("responseData", content);
            counter.Add(1);

            return content;
        });

        app.Run();
    }
}