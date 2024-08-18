using VideoLibrary.FilesystemWatcher.Helper;

namespace VideoLibrary.FilesystemWatcher.Services;

public class WatcherService(FilesystemHelper fsHelper, RabbitMqService rabbitMqService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = InitWatcher(stoppingToken);

        return Task.CompletedTask;
    }

    private async Task InitWatcher(CancellationToken stoppingToken)
    {
        rabbitMqService.OnManualUpdateTriggered += (_, _) => fsHelper.StartUpdateLibrary();
        rabbitMqService.OnFolderDeleteRequestTriggered += (_, folderId) => fsHelper.DeleteFolderWithVideos(folderId);
        rabbitMqService.OnDeleteVideosRequestTriggered += (_, _) => fsHelper.DeleteVideosMarkedAsDeleted();

        while (!stoppingToken.IsCancellationRequested)
        {
            fsHelper.StartUpdateLibrary();

#if DEBUG
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
#else
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
#endif
        }
    }
}