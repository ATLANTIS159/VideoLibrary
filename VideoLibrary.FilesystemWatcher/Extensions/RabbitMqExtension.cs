using VideoLibrary.FilesystemWatcher.Services;
using VideoLibrary.RabbitMqBase.Services;

namespace VideoLibrary.FilesystemWatcher.Extensions;

public static class RabbitMqExtension
{
    public static void AddRabbitMqServices(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqCore>();
        services.AddSingleton<RabbitMqService>();
    }
}