using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.EdlManager;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Edl.Controllers;

/// <summary>
/// PluginEdl controller.
/// </summary>
[Authorize(Policy = "RequiresElevation")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Route("PluginEdl")]
public class PluginEdlController : ControllerBase
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSegmentManager _mediaSegmentManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginEdlController"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="libraryManager">LibraryManager.</param>
    /// <param name="mediaSegmentManager">MediaSegmentsManager.</param>
    public PluginEdlController(
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IMediaSegmentManager mediaSegmentManager)
    {
        _loggerFactory = loggerFactory;
        _libraryManager = libraryManager;
        _mediaSegmentManager = mediaSegmentManager;
    }

    /// <summary>
    /// Plugin meta endpoint.
    /// </summary>
    /// <returns>The version info.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public JsonResult GetPluginMetadata()
    {
        var json = new
        {
            version = Plugin.Instance!.Version.ToString(3),
        };

        return new JsonResult(json);
    }

    /// <summary>
    /// Get Edl data based on itemId.
    /// </summary>
    /// <param name="itemId">ItemId.</param>
    /// <returns>The edl data.</returns>
    [HttpGet("Edl/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<JsonResult> GetEdlData(
        [FromRoute, Required] Guid itemId)
    {
        var queueManager = new QueueManager(_loggerFactory.CreateLogger<QueueManager>(), _libraryManager);

        var segmentsList = new List<MediaSegmentDto>();
        // get ItemIds
        var mediaItems = queueManager.GetMediaItemsById([itemId]);
        // get MediaSegments from itemIds
        foreach (var kvp in mediaItems)
        {
            foreach (var media in kvp.Value)
            {
                segmentsList.AddRange(await _mediaSegmentManager.GetSegmentsAsync(media.ItemId, null).ConfigureAwait(false));
            }
        }

        var rawstring = EdlManager.ToEdl(segmentsList.AsReadOnly());

        var json = new
        {
            itemId,
            edl = rawstring
        };

        return new JsonResult(json);
    }

    /// <summary>
    /// Force edl recreation for itemIds.
    /// </summary>
    /// <param name="itemIds">ItemIds.</param>
    /// <returns>Ok.</returns>
    [HttpPost("Edl")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<OkResult> GenerateData(
        [FromBody, Required] Guid[] itemIds)
    {
        var baseEdlTask = new BaseEdlTask(
            _loggerFactory.CreateLogger<CreateEdlTask>());

        var queueManager = new QueueManager(_loggerFactory.CreateLogger<QueueManager>(), _libraryManager);

        var segmentsList = new List<MediaSegmentDto>();
        // get ItemIds
        var mediaItems = queueManager.GetMediaItemsById(itemIds);
        // get MediaSegments from itemIds
        foreach (var kvp in mediaItems)
        {
            foreach (var media in kvp.Value)
            {
                segmentsList.AddRange(await _mediaSegmentManager.GetSegmentsAsync(media.ItemId, null).ConfigureAwait(false));
            }
        }

        IProgress<double> progress = new Progress<double>();
        CancellationToken cancellationToken = CancellationToken.None;

        // write edl files
        baseEdlTask.CreateEdls(progress, segmentsList.AsReadOnly(), true, cancellationToken);

        return new OkResult();
    }
}
