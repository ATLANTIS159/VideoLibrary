using Microsoft.EntityFrameworkCore;
using VideoLibrary.Data.Context;
using VideoLibrary.Data.Enums;
using VideoLibrary.Data.Models;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.Data.Repository;

public class VideoRepository(VideoLibraryDbContext dbContext, FolderRepository folderRepository)
{
    public List<Video> GetByFolderId(Guid folderId)
    {
        return dbContext.Videos.Include(video => video.Folder).Where(video => video.Folder.Id == folderId).ToList();
    }

    public List<Video> GetByStatus(VideoStatusEnum status)
    {
        return dbContext.Videos.Where(video => video.Status == status).ToList();
    }

    public Video? Get(Guid videoId)
    {
        return dbContext.Videos.Include(video => video.Folder).FirstOrDefault(video => video.Id == videoId);
    }

    public Guid GetRandomVideoId(VideoStatusEnum status)
    {
        return dbContext.Videos
            .Include(video => video.Folder)
            .Where(video => video.Status == status)
            .OrderBy(video => Guid.NewGuid())
            .Select(video => video.Id)
            .FirstOrDefault();
    }

    public List<Video> GetAll()
    {
        return dbContext.Videos.Include(video => video.Folder).ToList();
    }

    public List<Video> GetAll(VideoStatusEnum status)
    {
        return dbContext.Videos.Include(video => video.Folder).Where(video => video.Status == status).ToList();
    }

    public void DeleteMany(List<Video> videoList)
    {
        dbContext.RemoveRange(videoList);
        dbContext.SaveChanges();
    }

    public void Add(VideoProcess videoProcess)
    {
        var folder = folderRepository.Get(videoProcess.FolderId);

        if (folder is null)
        {
            throw new NullReferenceException(
                $"Error when process new video file/ Folder mot found. Folder id: {videoProcess.FolderId}");
        }

        var video = new Video
        {
            Id = videoProcess.VideoId,
            Name = Path.GetFileNameWithoutExtension(videoProcess.VideoPath),
            Path = videoProcess.VideoPath,
            Duration = videoProcess.Duration,
            Status = VideoStatusEnum.Unsorted,
            Folder = folder
        };

        dbContext.Videos.Add(video);
        dbContext.SaveChanges();
    }

    public void UpdateStatus(Guid videoId, VideoStatusEnum status)
    {
        var video = Get(videoId);

        if (video is null)
        {
            throw new NullReferenceException($"Can't change like state. Video with id {videoId} not found");
        }

        video.Status = status;

        dbContext.SaveChanges();
    }
}