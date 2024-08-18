namespace VideoLibrary.Data.Models;

public class Folder
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public virtual List<Video> Videos { get; set; }
}