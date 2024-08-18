using VideoLibrary.Data.Context;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.Data.Repository;

public class VideoProcessRepository(VideoLibraryDbContext dbContext)
{
    public VideoProcess Add(VideoProcess videoProcess)
    {
        dbContext.VideoProcesses.Add(videoProcess);
        dbContext.SaveChanges();

        return videoProcess;
    }

    public List<VideoProcess> AddMany(List<VideoProcess> videoProcesses)
    {
        var existingPaths = dbContext.VideoProcesses.Select(vp => vp.VideoPath).ToList();
        var uniqueVideoProcesses = videoProcesses.Where(vp => !existingPaths.Contains(vp.VideoPath)).ToList();

        dbContext.VideoProcesses.AddRange(uniqueVideoProcesses);
        dbContext.SaveChanges();

        return uniqueVideoProcesses;
    }

    public void Delete(VideoProcess videoProcess)
    {
        dbContext.VideoProcesses.Remove(videoProcess);
        dbContext.SaveChanges();
    }
}