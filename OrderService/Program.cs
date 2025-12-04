using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Interfaces;
using OrderService.Repositories;
using OrderService.Services;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "OrderService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/orderservice-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Service} - {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting OrderService...");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database
    builder.Services.AddDbContext<OrdersDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb")));

    // Repository Pattern
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();

    // RabbitMQ
    builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

    // Background Service to consume payment updates
  

    // CORS (if needed)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        db.Database.Migrate();
        Log.Information("Database migrations applied");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("OrderService is running on {Urls}",
        string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}