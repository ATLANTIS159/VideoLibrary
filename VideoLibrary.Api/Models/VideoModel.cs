namespace VideoLibrary.Api.Models;

public class VideoModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public double Duration { get; set; }
    public string FolderName { get; set; }
}