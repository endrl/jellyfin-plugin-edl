using System;

namespace Jellyfin.Plugin.Edl;

/// <summary>
/// Media queued for analysis.
/// </summary>
public class QueuedMedia
{
    /// <summary>
    /// Gets or sets the Series name.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the media id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this media is an episode, part of a tv show.
    /// </summary>
    public bool IsEpisode { get; set; } = true;

    /// <summary>
    /// Gets or sets the full path to episode.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the media, episode or movie with source quality/name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total duration of this media file (in seconds).
    /// </summary>
    public int Duration { get; set; }
}
