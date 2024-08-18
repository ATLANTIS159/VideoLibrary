using Microsoft.EntityFrameworkCore;
using VideoLibrary.Data.Context;
using VideoLibrary.Data.Models;

namespace VideoLibrary.Data.Repository;

public class FolderRepository(VideoLibraryDbContext dbContext)
{
    public List<Folder> GetAll()
    {
        return dbContext.Folders.ToList();
    }

    public List<Folder> GetAllWithVideos()
    {
        return dbContext.Folders.Include(folder => folder.Videos).ToList();
    }

    public Folder? Get(string folderName)
    {
        return dbContext.Folders.FirstOrDefault(folder => folder.Name == folderName);
    }

    public Folder? Get(Guid id)
    {
        return dbContext.Folders.FirstOrDefault(folder => folder.Id == id);
    }

    public Folder Add(Folder folder)
    {
        dbContext.Folders.Add(folder);

        dbContext.SaveChanges();

        return folder;
    }

    public void Delete(List<Folder> folders)
    {
        dbContext.Folders.RemoveRange(folders);
        dbContext.SaveChanges();
    }

    public void Delete(Guid id)
    {
        var folder = Get(id);

        if (folder is null)
        {
            throw new NullReferenceException($"Folder deleting failed. Folder with id {id} not found");
        }

        dbContext.Folders.Remove(folder);
        dbContext.SaveChanges();
    }

    public void Delete(Folder folder)
    {
        dbContext.Folders.Remove(folder);
        dbContext.SaveChanges();
    }
}