namespace VideoLibrary.RabbitMqBase.Variables;

public static class RabbitMqRoutingKeys
{
    public const string PROCESS_VIDEO = "video-processor.process-video";
    public const string COMPLETE_VIDEO = "video-library-api.completed-video";
    public const string MANUAL_SCAN_FOLDER = "filesystem-watcher.manual-scan-folder";
    public const string DELETE_FOLDER = "filesystem-watcher.delete-folder";
    public const string DELETE_VIDEOS = "filesystem-watcher.delete-video";
}