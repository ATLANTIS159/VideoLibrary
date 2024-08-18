using Microsoft.EntityFrameworkCore;
using VideoLibrary.Data.Enums;
using VideoLibrary.Data.Models;
using VideoLibrary.Shared.Models;

namespace VideoLibrary.Data.Context;

public class VideoLibraryDbContext : DbContext
{
    private const string CONNECTION_STRING =
        "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Database=VideoLibrary";

    public DbSet<Folder> Folders { get; init; }
    public DbSet<Video> Videos { get; init; }
    public DbSet<VideoProcess> VideoProcesses { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(CONNECTION_STRING);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Folder>()
            .HasMany(folder => folder.Videos)
            .WithOne(video => video.Folder)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Video>()
            .Property(video => video.Status)
            .HasConversion(status => status.ToString(),
                status => (VideoStatusEnum)Enum.Parse(typeof(VideoStatusEnum), status));
    }
}