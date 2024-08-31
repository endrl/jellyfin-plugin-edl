namespace Jellyfin.Plugin.Edl;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.MediaSegments;
using Microsoft.Extensions.Logging;

/// <summary>
/// Common code shared by all edl creator tasks.
/// </summary>
public class BaseEdlTask
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEdlTask"/> class.
    /// </summary>
    /// <param name="logger">Task logger.</param>
    public BaseEdlTask(
        ILogger logger)
    {
        _logger = logger;

        EdlManager.Initialize(_logger);
    }

    /// <summary>
    /// Create edls for all Segments on the server.
    /// </summary>
    /// <param name="progress">Progress.</param>
    /// <param name="segmentsQueue">Media segments.</param>
    /// <param name="forceOverwrite">Force the file overwrite.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void CreateEdls(
        IProgress<double> progress,
        ReadOnlyCollection<MediaSegmentDto> segmentsQueue,
        bool forceOverwrite,
        CancellationToken cancellationToken)
    {
        var sortedSegments = new Dictionary<Guid, List<MediaSegmentDto>>();

        foreach (var segment in segmentsQueue)
        {
            if (sortedSegments.TryGetValue(segment.ItemId, out List<MediaSegmentDto>? list))
            {
                sortedSegments[segment.ItemId] = list.Append(segment).ToList();
            }
            else
            {
                sortedSegments.Add(segment.ItemId, new List<MediaSegmentDto> { segment });
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

            EdlManager.UpdateEDLFile(segment, forceOverwrite);
            Interlocked.Add(ref totalProcessed, 1);

            progress.Report((totalProcessed * 100) / totalQueued);
        });
    }
}
