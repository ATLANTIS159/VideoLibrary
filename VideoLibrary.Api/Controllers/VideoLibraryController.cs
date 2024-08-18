using Microsoft.AspNetCore.Mvc;
using VideoLibrary.Api.Models;
using VideoLibrary.Api.Services;
using VideoLibrary.Data.Enums;
using VideoLibrary.Data.Models;
using VideoLibrary.Data.Repository;

namespace VideoLibrary.Api.Controllers;

[ApiController]
[Route("api/video-library")]
public class VideoLibraryController(
    VideoRepository videoRepository,
    FolderRepository folderRepository,
    RabbitMqService rabbitMqService) : Controller
{
    [HttpGet]
    [Route("videos")]
    public IActionResult GetAllVideos([FromQuery] VideoStatusEnum status)
    {
        var videoModels = videoRepository.GetAll(status).Select(GenerateVideoModel).ToList();

        return Ok(videoModels);
    }

    [HttpGet]
    [Route("video/{id:guid}")]
    public IActionResult GetVideoById(Guid id)
    {
        var video = videoRepository.Get(id);

        if (video is null)
        {
            return NotFound();
        }

        return Ok(GenerateVideoModel(video));
    }

    [HttpGet]
    [Route("video/random-id")]
    public IActionResult GetRandomId(VideoStatusEnum status)
    {
        var videoId = videoRepository.GetRandomVideoId(status);

        if (videoId == Guid.Empty)
        {
            return NotFound("Random video id not found");
        }

        return Ok(videoId);
    }

    [HttpGet]
    [Route("player-video-info/{id:guid}")]
    public IActionResult GetVideoInfoForPlayer(Guid id)
    {
        var video = videoRepository.Get(id);

        if (video is null)
        {
            return NotFound($"Video with id {id} not found");
        }

        return Ok(GeneratePlayerInfoModel(video));
    }

    [HttpPost]
    [Route("update-library")]
    public IActionResult UpdateLibrary()
    {
        rabbitMqService.SendUpdateLibraryRequest();
        return Ok();
    }

    [HttpPost]
    [Route("video/{id:guid}")]
    public IActionResult ChangeVideoLikeState(Guid id, [FromQuery] VideoStatusEnum status)
    {
        videoRepository.UpdateStatus(id, status);
        return Ok();
    }

    [HttpGet]
    [Route("folders")]
    public IActionResult GetAllFolders()
    {
        var folderModels = folderRepository.GetAll().Select(GenerateFolderModel).ToList();

        return Ok(folderModels);
    }

    [HttpGet]
    [Route("folder/{id:guid}/videos")]
    public IActionResult GetVideosInFolder(Guid id)
    {
        var videoModels = videoRepository.GetByFolderId(id).Select(GenerateVideoModel).ToList();

        return Ok(videoModels);
    }

    [HttpDelete]
    [Route("folder/{id:guid}")]
    public IActionResult DeleteFolder(Guid id)
    {
        rabbitMqService.SendFolderDeletionRequest(id);
        return Ok();
    }

    [HttpDelete]
    [Route("videos")]
    public IActionResult DeleteVideos()
    {
        rabbitMqService.SendVideoDeletionRequest();
        return Ok();
    }

    private FolderModel GenerateFolderModel(Folder folder)
    {
        return new FolderModel
        {
            Id = folder.Id,
            Name = folder.Name
        };
    }

    private PlayerInfoModel GeneratePlayerInfoModel(Video video)
    {
        var filename = Path.GetFileName(video.Path);
        var folderName = video.Folder.Name;

        return new PlayerInfoModel
        {
            Id = video.Id,
            Name = video.Name,
            Path = $"~/videos/{folderName}/{filename}",
            Duration = video.Duration,
            FolderName = folderName
        };
    }

    private VideoModel GenerateVideoModel(Video video)
    {
        return new VideoModel
        {
            Id = video.Id,
            Name = video.Name,
            Duration = video.Duration,
            FolderName = video.Folder.Name
        };
    }
}