using VideoLibrary.Data.Enums;
using VideoLibrary.Data.Models;
using VideoLibrary.Data.Repository;
using VideoLibrary.FilesystemWatcher.Services;
using VideoLibrary.Shared;
using VideoLibrary.Shared.Enums;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.FilesystemWatcher.Helper;

public class FilesystemHelper(
    RabbitMqService rabbitMqService,
    FolderRepository folderRepository,
    VideoRepository videoRepository,
    VideoProcessRepository videoProcessRepository)
{
    private bool _isExporting;

    private bool _isScanning;

    public void StartUpdateLibrary()
    {
        if (_isScanning)
        {
            return;
        }

        _isScanning = true;

        CheckFoldersExists();

        var foldersInDb = folderRepository.GetAllWithVideos();
        var foldersInSourceFolder = GetFoldersInFolder(SharedVariables.UnsortedVideosFolder);
        var foldersInThumbnailFolder = GetFoldersInFolder(SharedVariables.ThumbnailsFolder);

        CheckMissingFolders(foldersInSourceFolder, foldersInDb);
        CheckNewFolders(foldersInSourceFolder, foldersInDb);

        var videosInDb = videoRepository.GetAll();

        CheckMissingFiles(foldersInSourceFolder, videosInDb);
        CheckMissingThumbnailFolders(foldersInThumbnailFolder, videosInDb);
        CheckMissingThumbnails(foldersInThumbnailFolder, videosInDb);
        CheckNewVideosInFolders(foldersInSourceFolder, videosInDb);

        _isScanning = false;
    }

    public void DeleteVideosMarkedAsDeleted()
    {
        var videos = videoRepository.GetByStatus(VideoStatusEnum.Deleted);

        DeleteVideos(videos);

        videoRepository.DeleteMany(videos);
    }

    private void DeleteVideos(List<Video> videos)
    {
        foreach (var video in videos)
            if (File.Exists(video.Path))
            {
                File.Delete(video.Path);
            }
    }

    public void DeleteFolderWithVideos(Guid folderId)
    {
        var folder = folderRepository.Get(folderId);

        if (folder is null)
        {
            throw new NullReferenceException($"Folder deletion failed. Folder with id {folderId} not found");
        }

        var folderPath = Path.Combine(SharedVariables.UnsortedVideosFolder, folder.Name);

        DeleteFolder(folderPath);

        folderRepository.Delete(folder);
    }

    private void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    private void CheckFoldersExists()
    {
        if (!Directory.Exists(SharedVariables.UnsortedVideosFolder))
        {
            Directory.CreateDirectory(SharedVariables.UnsortedVideosFolder);
        }

        if (!Directory.Exists(SharedVariables.ThumbnailsFolder))
        {
            Directory.CreateDirectory(SharedVariables.ThumbnailsFolder);
        }
    }

    private List<string> GetFoldersInFolder(string folder)
    {
        return Directory
            .GetDirectories(folder, "*", SearchOption.TopDirectoryOnly)
            .Select(GetFolderNameFromFolderPath)
            .ToList();
    }

    private List<string> GetVideosInFolder(string folder)
    {
        var folderPath = Path.Combine(SharedVariables.UnsortedVideosFolder, folder);
        return Directory.EnumerateFiles(folderPath, "*.mp4", SearchOption.AllDirectories).ToList();
    }

    private string GetFolderNameFromFolderPath(string folderPath)
    {
        return Path.GetFileName(folderPath);
    }

    private void CheckMissingFolders(List<string> foldersInSourceFolder, List<Folder> foldersInDb)
    {
        var missingFolders = foldersInDb
            .FindAll(folder => !foldersInSourceFolder
                .Contains(folder.Name));

        if (missingFolders.Count != 0)
        {
            return;
        }

        foreach (var video in missingFolders.SelectMany(missingFolder => missingFolder.Videos))
            DeleteThumbnailFolder(video.Id);

        folderRepository.Delete(missingFolders);
    }

    private void CheckNewFolders(List<string> foldersInSourceFolder, List<Folder> foldersInDb)
    {
        var newFolders =
            foldersInSourceFolder
                .FindAll(folder => !foldersInDb
                    .Select(folderInDb => folderInDb.Name)
                    .Contains(folder));

        if (newFolders.Count != 0)
        {
            ProcessNewFolders(newFolders);
        }
    }

    private void DeleteThumbnailFolder(string folder)
    {
        var thumbnailFolder = Path.Combine(SharedVariables.ThumbnailsFolder, folder);

        DeleteFolder(thumbnailFolder);
    }

    private void DeleteThumbnailFolder(Guid id)
    {
        var thumbnailFolder = Path.Combine(SharedVariables.ThumbnailsFolder, id.ToString());

        if (Directory.Exists(thumbnailFolder))
        {
            Directory.Delete(thumbnailFolder, true);
        }
    }

    private void ProcessNewFolders(List<string> newFolders)
    {
        foreach (var newFolder in newFolders)
        {
            var folder = folderRepository.Get(GetFolderNameFromFolderPath(newFolder)) ?? folderRepository.Add(
                new Folder
                {
                    Name = GetFolderNameFromFolderPath(newFolder),
                    Videos = []
                });

            var videoInfos = GetVideosInFolder(newFolder)
                .Select(video => new VideoProcess
                {
                    VideoPath = video,
                    Duration = 0,
                    FolderId = folder.Id,
                    VideoId = Guid.Empty,
                    VideoProcessingType = VideoProcessingType.NewThumbnail
                })
                .ToList();

            videoInfos = videoProcessRepository.AddMany(videoInfos);

            foreach (var videoProcessInfo in videoInfos) rabbitMqService.SendVideoProcessRequest(videoProcessInfo);
        }
    }

    private void CheckMissingFiles(List<string> foldersInSourceFolder, List<Video> videosInDb)
    {
        var videosInSourceFolder = foldersInSourceFolder
            .Select(GetVideosInFolder)
            .SelectMany(video => video)
            .ToList();
        var missingFiles = videosInDb.FindAll(video => !videosInSourceFolder.Contains(video.Path));

        if (missingFiles.Count == 0)
        {
            return;
        }

        videoRepository.DeleteMany(missingFiles);
    }

    private void CheckMissingThumbnailFolders(List<string> foldersInThumbnailFolder, List<Video> videosInDb)
    {
        var missingThumbnailFolder = foldersInThumbnailFolder
            .FindAll(folder => !videosInDb
                .Select(dbFolder => dbFolder.Id.ToString())
                .Contains(folder));

        if (missingThumbnailFolder.Count == 0)
        {
            return;
        }

        foreach (var folder in missingThumbnailFolder) DeleteThumbnailFolder(folder);
    }

    private void CheckMissingThumbnails(List<string> foldersInThumbnailFolder, List<Video> videosInDb)
    {
        var missingThumbnails = videosInDb
            .FindAll(video => !foldersInThumbnailFolder
                .Contains(video.Id.ToString()))
            .ToList();

        if (missingThumbnails.Count == 0)
        {
            return;
        }

        var videoInfos = missingThumbnails.Select(video => new VideoProcess
            {
                VideoPath = video.Path,
                FolderId = video.Folder.Id,
                VideoId = video.Id,
                VideoProcessingType = VideoProcessingType.MissingThumbnail,
                Duration = video.Duration
            })
            .ToList();

        videoInfos = videoProcessRepository.AddMany(videoInfos);

        foreach (var videoProcessInfo in videoInfos) rabbitMqService.SendVideoProcessRequest(videoProcessInfo);
    }

    private void CheckNewVideosInFolders(List<string> foldersInSourceFolder, List<Video> videosInDb)
    {
        List<VideoProcess> videoInfos = [];

        foreach (var sourceFolder in foldersInSourceFolder)
        {
            var folder = folderRepository.Get(sourceFolder) ??
                         folderRepository.Add(
                             new Folder
                             {
                                 Name = sourceFolder,
                                 Videos = []
                             });

            var videosInFolder = GetVideosInFolder(sourceFolder);
            var newVideos = videosInFolder
                .FindAll(video =>
                    !videosInDb
                        .Select(videoInDb => videoInDb.Path)
                        .Contains(video));

            if (newVideos.Count == 0)
            {
                continue;
            }

            videoInfos.AddRange(newVideos.Select(video => new VideoProcess
            {
                VideoPath = video,
                FolderId = folder.Id,
                VideoId = Guid.Empty,
                VideoProcessingType = VideoProcessingType.NewThumbnail,
                Duration = 0
            }));

            videoInfos = videoProcessRepository.AddMany(videoInfos);

            foreach (var videoProcessInfo in videoInfos) rabbitMqService.SendVideoProcessRequest(videoProcessInfo);
        }
    }
}