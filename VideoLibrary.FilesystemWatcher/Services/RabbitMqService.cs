using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VideoLibrary.RabbitMqBase;
using VideoLibrary.RabbitMqBase.Helpers;
using VideoLibrary.RabbitMqBase.Services;
using VideoLibrary.RabbitMqBase.Variables;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.FilesystemWatcher.Services;

public class RabbitMqService(RabbitMqCore rabbitMqCore)
{
    private IModel? _channel;
    public event EventHandler? OnManualUpdateTriggered;
    public event EventHandler<Guid>? OnFolderDeleteRequestTriggered;
    public event EventHandler? OnDeleteVideosRequestTriggered;

    public async Task StartListening()
    {
        _channel = rabbitMqCore.GetChannel();

        var queue = rabbitMqCore.FilesystemWatcherQueue;

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += MessageReceived;

        _channel.BasicConsume(queue, RabbitVariables.RABBIT_MQ_AUTO_ACK, consumer);
    }

    private void MessageReceived(object? sender, BasicDeliverEventArgs args)
    {
        switch (args.RoutingKey)
        {
            case RabbitMqRoutingKeys.MANUAL_SCAN_FOLDER:
                OnManualUpdateTriggered?.Invoke(this, null!);
                break;
            case RabbitMqRoutingKeys.DELETE_FOLDER:
                var folderId = JsonConvert.DeserializeObject<Guid>(Encoding.UTF8.GetString(args.Body.ToArray()));
                OnFolderDeleteRequestTriggered?.Invoke(this, folderId);
                break;
            case RabbitMqRoutingKeys.DELETE_VIDEOS:
                OnDeleteVideosRequestTriggered?.Invoke(this, null!);
                break;
        }

        _channel?.BasicAck(args.DeliveryTag, false);
    }

    public void SendVideoProcessRequest(VideoProcess videoProcessInfo)
    {
        rabbitMqCore.GetChannel().BasicPublish(RabbitVariables.RABBIT_MQ_EXCHANGE_NAME,
            RabbitMqRoutingKeys.PROCESS_VIDEO, body: RabbitMqHelper.GetBytesFromMessage(videoProcessInfo));
    }
}