using VideoLibrary.Shared;
using VideoLibrary.Shared.Enums;
using VideoLibrary.Shared.Models;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace VideoLibrary.VideoProcessor.Services;

public class ProcessorService(ILogger<ProcessorService> logger)
{
    private const string FFMPEG_SMALL_SCALE_ARGUMENT = " -vf scale=-1:360";
    private const string FFMPEG_THUMBNAIL_ARGUMENTS_TEMPLATE = "-ss {0} -i \"{1}\"{2} -frames:v 1 \"{3}\"";
    private readonly object _lockObject = new();

    public async Task<(bool isSuccess, VideoProcess videoProcessInfo)> StartThumbnailGeneration(
        VideoProcess videoProcessInfo)
    {
        await CheckFfmpegFiles();

        switch (videoProcessInfo.VideoProcessingType)
        {
            case VideoProcessingType.NewThumbnail:
                videoProcessInfo.VideoId = Guid.NewGuid();
                break;
            case VideoProcessingType.MissingThumbnail:
                break;
            default:
                throw new Exception(
                    $"Wrong video processing type {nameof(VideoProcess.VideoProcessingType)}. Selected type: {videoProcessInfo.VideoProcessingType}");
        }

        var isSuccess = await GenerateThumbnail(videoProcessInfo);

        return (isSuccess, videoProcessInfo);
    }

    private async Task<bool> GenerateThumbnail(VideoProcess videoProcess)
    {
        if (!Directory.Exists(SharedVariables.ThumbnailsFolder))
        {
            lock (_lockObject)
            {
                if (!Directory.Exists(SharedVariables.ThumbnailsFolder))
                {
                    Directory.CreateDirectory(SharedVariables.ThumbnailsFolder);
                }
            }
        }

        var thumbnailFolder = Path.Combine(SharedVariables.ThumbnailsFolder, videoProcess.VideoId.ToString());

        if (!Directory.Exists(thumbnailFolder))
        {
            Directory.CreateDirectory(thumbnailFolder);
        }

        var smallThumbnailFileName = Path.Combine(thumbnailFolder, SharedVariables.SmallThumbnailFileName);
        var largeThumbnailFileName = Path.Combine(thumbnailFolder, SharedVariables.LargeThumbnailFileName);

        var isSmallExists = File.Exists(smallThumbnailFileName);
        var isLargeExists = File.Exists(largeThumbnailFileName);

        if (isSmallExists && isLargeExists)
        {
            return true;
        }

        logger.LogInformation("Запущено создание превью для файла {FilePath}",
            Path.GetFileName(videoProcess.VideoPath));

        videoProcess.Duration = await GetVideoDuration(videoProcess.VideoPath);
        var thumbnailTime = TimeSpan.FromSeconds(videoProcess.Duration * 0.5).ToString(@"hh\:mm\:ss");
        var sourceVideoPath = videoProcess.VideoPath;

        try
        {
            string ffmpegArguments;

            if (!isSmallExists)
            {
                ffmpegArguments = string.Format(FFMPEG_THUMBNAIL_ARGUMENTS_TEMPLATE, thumbnailTime,
                    sourceVideoPath, FFMPEG_SMALL_SCALE_ARGUMENT, smallThumbnailFileName);

                await StartFfmpegProcess(ffmpegArguments);
            }

            if (!isLargeExists)
            {
                ffmpegArguments = string.Format(FFMPEG_THUMBNAIL_ARGUMENTS_TEMPLATE, thumbnailTime,
                    sourceVideoPath, null, largeThumbnailFileName);

                await StartFfmpegProcess(ffmpegArguments);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Создание превью завершилось с ошибкой. Сообщение ошибки: {Message}", e.Message);

            return false;
        }

        logger.LogInformation("Создание превью для файла {Path} завершено", Path.GetFileName(videoProcess.VideoPath));
        return true;
    }

    private bool IsFullyCopied(string videoPath)
    {
        try
        {
            using var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task StartFfmpegProcess(string parameters)
    {
        await FFmpeg.Conversions.New().Start(parameters);
    }

    private async Task<double> GetVideoDuration(string filePath)
    {
        await CheckFfmpegFiles();

        var mediaInfo = await FFmpeg.GetMediaInfo(filePath);
        return mediaInfo.Duration.TotalSeconds;
    }

    private async Task CheckFfmpegFiles()
    {
        if (!Directory.Exists(SharedVariables.FfmpegFolder))
        {
            Directory.CreateDirectory(SharedVariables.FfmpegFolder);
        }

        FFmpeg.SetExecutablesPath(SharedVariables.FfmpegFolder);

        if (!File.Exists(SharedVariables.FfmpegFileName) || !File.Exists(SharedVariables.FfprobeFileName))
        {
            Console.WriteLine(
                "Не найден один или несколько необходимых файлов FFmpeg для работы сервера. Запущена загрузка недостающих файлов");

            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, SharedVariables.FfmpegFolder);

            Console.WriteLine("Все недостающие файлы FFmpeg успешно загружены");
        }
    }
}