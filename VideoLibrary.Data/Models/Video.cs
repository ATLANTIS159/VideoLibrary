using VideoLibrary.Data.Enums;

namespace VideoLibrary.Data.Models;

public class Video
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public double Duration { get; set; }
    public VideoStatusEnum Status { get; set; }
    public virtual Folder Folder { get; set; }
}