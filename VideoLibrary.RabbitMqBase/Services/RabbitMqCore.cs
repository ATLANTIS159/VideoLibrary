using RabbitMQ.Client;
using VideoLibrary.RabbitMqBase.Variables;

namespace VideoLibrary.RabbitMqBase.Services;

public class RabbitMqCore
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = RabbitVariables.RABBIT_MQ_SERVER_HOST,
        Port = Convert.ToInt32(RabbitVariables.RABBIT_MQ_SERVER_PORT ??
                               throw new ArgumentException("Неверный параметр порта RabbitMq")),
        UserName = RabbitVariables.RABBIT_MQ_SERVER_USERNAME,
        Password = RabbitVariables.RABBIT_MQ_SERVER_PASSWORD
    };

    private IModel? _channel;

    private IConnection? _connection;
    public QueueDeclareOk? ApiQueue { get; private set; }
    public QueueDeclareOk? FilesystemWatcherQueue { get; private set; }
    public QueueDeclareOk? VideoProcessorQueue { get; private set; }

    public IModel? GetChannel()
    {
        return _channel is not null && _channel.IsOpen ? _channel : InitRabbitMqConnection();
    }

    private void InitQueues()
    {
        if (_channel is null)
        {
            throw new NullReferenceException("Error when initializing RabbitMq queues. Channel is null");
        }

        ApiQueue = DeclareQueue(_channel, RabbitVariables.RABBIT_MQ_VL_API_QUEUE);
        FilesystemWatcherQueue = DeclareQueue(_channel, RabbitVariables.RABBIT_MQ_VL_FILESYSTEM_WATCHER_QUEUE);
        VideoProcessorQueue = DeclareQueue(_channel, RabbitVariables.RABBIT_MQ_VL_VIDEO_PROCESSOR_QUEUE);

        //VideoApi
        BingQueue(_channel, RabbitVariables.RABBIT_MQ_VL_API_QUEUE,
            RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.COMPLETE_VIDEO);

        //FilesystemWatcher
        BingQueue(_channel, RabbitVariables.RABBIT_MQ_VL_FILESYSTEM_WATCHER_QUEUE,
            RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.MANUAL_SCAN_FOLDER);
        BingQueue(_channel, RabbitVariables.RABBIT_MQ_VL_FILESYSTEM_WATCHER_QUEUE,
            RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.DELETE_FOLDER);
        BingQueue(_channel, RabbitVariables.RABBIT_MQ_VL_FILESYSTEM_WATCHER_QUEUE,
            RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.DELETE_VIDEOS);

        //VideoProcessor
        BingQueue(_channel, RabbitVariables.RABBIT_MQ_VL_VIDEO_PROCESSOR_QUEUE,
            RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.PROCESS_VIDEO);
    }

    private QueueDeclareOk? DeclareQueue(IModel channel, string queue,
        bool isDurable = RabbitVariables.RABBIT_MQ_DURABLE,
        bool isExclusive = RabbitVariables.RABBIT_MQ_EXCLUSIVE,
        bool isAutoDelete = RabbitVariables.RABBIT_MQ_AUTO_DELETE)
    {
        return channel.QueueDeclare(queue, isDurable, isExclusive, isAutoDelete, RabbitVariables.RabbitMqQueueArgs);
    }

    private void BingQueue(IModel channel, string queue, string exchange, string routingKey)
    {
        channel.QueueBind(queue, exchange, routingKey, null);
    }

    private IModel? InitRabbitMqConnection()
    {
        if (_connection is not null && !_connection.IsOpen)
        {
            _connection.Close();
            _connection.Dispose();
        }

        if (_channel is not null && _channel.IsClosed)
        {
            _channel.Close();
            _channel.Dispose();
        }

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, ExchangeType.Topic,
            RabbitVariables.RABBIT_MQ_DURABLE);

        InitQueues();

        return _channel;
    }
}