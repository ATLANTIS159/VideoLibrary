using VideoLibrary.RabbitMqBase.Services;
using VideoLibrary.VideoProcessor.Services;

namespace VideoLibrary.VideoProcessor.Extensions;

public static class RabbitMqExtension
{
    public static void AddRabbitMqServices(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqCore>();
        services.AddSingleton<RabbitMqService>();
    }
}