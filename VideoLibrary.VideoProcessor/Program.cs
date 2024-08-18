using VideoLibrary.VideoProcessor.Extensions;
using VideoLibrary.VideoProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMqServices();
builder.Services.AddScoped<ProcessorService>();

var app = builder.Build();

var rabbitService = app.Services.GetRequiredService<RabbitMqService>();
await rabbitService.StartListening();

app.Run();