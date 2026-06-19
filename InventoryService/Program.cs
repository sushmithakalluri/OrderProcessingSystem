using InventoryService;
using InventoryService.Configuration;
using InventoryService.Data;
using Microsoft.EntityFrameworkCore;
using InventoryService.Messaging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<RabbitMqPublisher>();

var host = builder.Build();

host.Run();