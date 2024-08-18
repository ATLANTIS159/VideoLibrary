using VideoLibrary.Data.Context;
using VideoLibrary.Data.Extensions;
using VideoLibrary.FilesystemWatcher.Extensions;
using VideoLibrary.FilesystemWatcher.Helper;
using VideoLibrary.FilesystemWatcher.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMqServices();
builder.Services.AddRepositories();
builder.Services.AddSingleton<FilesystemHelper>();
builder.Services.AddHostedService<WatcherService>();

builder.Services.AddDbContext<VideoLibraryDbContext>();

var app = builder.Build();

var rabbitService = app.Services.GetRequiredService<RabbitMqService>();
await rabbitService.StartListening();

app.Run();