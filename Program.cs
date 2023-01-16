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

    public static void Main(string[] args)
    {

        var MyActivitySource = new ActivitySource(ServiceName);

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
                .AddSource(MyActivitySource.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation();
            })
            .WithMetrics(builder =>
            {
                builder
                .AddConsoleExporter()
                .AddMeter(meter.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
            })
            .StartWithHost();

        var app = builder.Build();

        app.MapGet("/hello", () =>
        {
            // Track work inside of the request
            using var activity = MyActivitySource.StartActivity("SayHello");
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            // Up a counter for each request
            counter.Add(1);

            return "Hello, World!";
        });

        app.Run();
    }
}