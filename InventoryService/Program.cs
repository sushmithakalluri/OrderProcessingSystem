using InventoryService;
using InventoryService.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<RabbitMqSettings>(
builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();