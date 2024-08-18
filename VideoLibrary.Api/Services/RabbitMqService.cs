using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VideoLibrary.Data.Repository;
using VideoLibrary.RabbitMqBase;
using VideoLibrary.RabbitMqBase.Helpers;
using VideoLibrary.RabbitMqBase.Services;
using VideoLibrary.RabbitMqBase.Variables;
using VideoLibrary.Shared.Enums;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.Api.Services;

public class RabbitMqService(
    RabbitMqCore rabbitMqCore,
    VideoRepository videoRepository,
    VideoProcessRepository videoProcessRepository)
{
    private IModel? _channel;

    public async Task StartListening()
    {
        _channel = rabbitMqCore.GetChannel();

        if (_channel is null)
        {
            throw new NullReferenceException("RabbitMq channel is null");
        }

        var queue = rabbitMqCore.ApiQueue;

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += MessageReceived;

        _channel.BasicConsume(queue, RabbitVariables.RABBIT_MQ_AUTO_ACK, consumer);
    }

    public void SendUpdateLibraryRequest()
    {
        _channel?.BasicPublish(RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.MANUAL_SCAN_FOLDER);
    }

    public void SendFolderDeletionRequest(Guid folderId)
    {
        _channel?.BasicPublish(RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.DELETE_FOLDER,
            body: RabbitMqHelper.GetBytesFromMessage(folderId));
    }

    public void SendVideoDeletionRequest()
    {
        _channel?.BasicPublish(RabbitVariables.RABBIT_MQ_EXCHANGE_NAME, RabbitMqRoutingKeys.DELETE_VIDEOS);
    }

    private void MessageReceived(object? sender, BasicDeliverEventArgs args)
    {
        switch (args.RoutingKey)
        {
            case RabbitMqRoutingKeys.COMPLETE_VIDEO:
                var videoProcess =
                    JsonConvert.DeserializeObject<VideoProcess>(Encoding.UTF8.GetString(args.Body.ToArray()));

                if (videoProcess is null)
                {
                    _channel?.BasicAck(args.DeliveryTag, false);
                    throw new NullReferenceException("Message from rabbit is null");
                }

                switch (videoProcess.VideoProcessingType)
                {
                    case VideoProcessingType.NewThumbnail:
                        videoRepository.Add(videoProcess);
                        break;
                    case VideoProcessingType.MissingThumbnail:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Wrong video process status. {nameof(videoProcess.VideoProcessingType)}");
                }

                videoProcessRepository.Delete(videoProcess);
                break;
        }

        _channel?.BasicAck(args.DeliveryTag, false);
    }
}