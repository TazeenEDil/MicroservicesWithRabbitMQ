using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.Data;
using PaymentService.Interfaces;
using PaymentService.Repositories;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "PaymentService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/paymentservice-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting PaymentService...");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    builder.Services.AddControllers();

    // Database
    builder.Services.AddDbContext<PaymentsDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDb")));

    // Repository Pattern
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

    // Background Worker
    builder.Services.AddHostedService<Worker>();

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        db.Database.Migrate();
        Log.Information("PaymentService database migrations applied");
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "PaymentService",
        timestamp = DateTime.UtcNow
    }));

    Log.Information("PaymentService is ready");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PaymentService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}