namespace VideoLibrary.RabbitMqBase;

public static class RabbitVariables
{
    public const string RABBIT_MQ_EXCHANGE_NAME = "video-library";
    public const bool RABBIT_MQ_DURABLE = true;
    public const bool RABBIT_MQ_AUTO_DELETE = false;
    public const bool RABBIT_MQ_AUTO_ACK = false;
    public const bool RABBIT_MQ_EXCLUSIVE = false;

    public const string RABBIT_MQ_VL_VIDEO_PROCESSOR_QUEUE = "vl-video-processor";
    public const string RABBIT_MQ_VL_API_QUEUE = "vl-api";
    public const string RABBIT_MQ_VL_FILESYSTEM_WATCHER_QUEUE = "vl-filesystem-watcher";

    // public static string? RabbitMqServerHost => Environment.GetEnvironmentVariable("RABBITMQ_SERVER_HOST");
    // public static string? RabbitMqServerPort => Environment.GetEnvironmentVariable("RABBITMQ_SERVER_PORT");
    // public static string? RabbitMqServerUsername => Environment.GetEnvironmentVariable("RABBITMQ_SERVER_USERNAME");
    // public static string? RabbitMqServerPassword => Environment.GetEnvironmentVariable("RABBITMQ_SERVER_PASSWORD");

    public const string RABBIT_MQ_SERVER_HOST = "192.168.1.149";
    public const string RABBIT_MQ_SERVER_PORT = "5673";
    public const string RABBIT_MQ_SERVER_USERNAME = "testuser";
    public const string RABBIT_MQ_SERVER_PASSWORD = "testpassword";

    public static readonly Dictionary<string, object> RabbitMqQueueArgs = new()
    {
        { "x-consumer-timeout", TimeSpan.FromDays(60).TotalMilliseconds }
    };
}