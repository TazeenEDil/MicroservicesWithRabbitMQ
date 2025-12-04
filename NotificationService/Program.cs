using NotificationService;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotificationService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/notificationservice-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting NotificationService...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices(services =>
        {
            services.AddHostedService<Worker>();
        })
        .Build();

    Log.Information("NotificationService is ready");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}