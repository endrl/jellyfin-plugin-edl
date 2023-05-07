namespace Jellyfin.Plugin.Edl;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

/// <summary>
/// Common code shared by all edl creator tasks.
/// </summary>
public class BaseEdlTask
{
    private readonly ILogger _logger;

    private readonly ILoggerFactory _loggerFactory;

    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEdlTask"/> class.
    /// </summary>
    /// <param name="logger">Task logger.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    public BaseEdlTask(
        ILogger logger,
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _libraryManager = libraryManager;

        EdlManager.Initialize(_logger);
    }

    /// <summary>
    /// Create edls for all Segments on the server.
    /// </summary>
    /// <param name="progress">Progress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void CreateEdls(
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        var segmentsQueue = Plugin.Instance!.GetAllMediaSegments();
        var sortedSegments = new Dictionary<Guid, List<MediaSegment>>();

        foreach (var segment in segmentsQueue)
        {
            if (sortedSegments.TryGetValue(segment.ItemId, out var list))
            {
                sortedSegments[segment.ItemId] = (List<MediaSegment>)list.Append(segment);
            }
            else
            {
                sortedSegments.Add(segment.ItemId, new List<MediaSegment> { segment });
            }
        }

        var totalQueued = sortedSegments.Count;

        EdlManager.LogConfiguration();

        var totalProcessed = 0;
        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Plugin.Instance!.Configuration.MaxParallelism
        };

        Parallel.ForEach(sortedSegments, options, (segment) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            EdlManager.UpdateEDLFile(segment);
            Interlocked.Add(ref totalProcessed, 1);

            progress.Report((totalProcessed * 100) / totalQueued);
        });

        if (Plugin.Instance!.Configuration.OverwriteEdlFiles)
        {
            _logger.LogInformation("Turning EDL file regeneration flag off");
            Plugin.Instance!.Configuration.OverwriteEdlFiles = false;
            Plugin.Instance!.SaveConfiguration();
        }
    }
}
