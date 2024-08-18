using VideoLibrary.Data.Enums;

namespace VideoLibrary.Api.Models;

public class PlayerInfoModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public double Duration { get; set; }
    public VideoStatusEnum Status { get; set; }
    public string FolderName { get; set; }
}