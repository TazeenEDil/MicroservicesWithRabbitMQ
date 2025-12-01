using Microsoft.EntityFrameworkCore;
using OrderService.Data;
//using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("OrdersDb")));
//builder.Services.AddSingleton<RabbitMqConnection>();
//builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.EnsureCreated(); // simple for dev
}

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}
app.MapControllers();
app.Run();
