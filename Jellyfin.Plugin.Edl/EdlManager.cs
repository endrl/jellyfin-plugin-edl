namespace Jellyfin.Plugin.Edl;

using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;

/// <summary>
/// Update EDL files associated with a list of episodes.
/// </summary>
public static class EdlManager
{
    private static ILogger? _logger;

    /// <summary>
    /// Initialize EDLManager with a logger.
    /// </summary>
    /// <param name="logger">ILogger.</param>
    public static void Initialize(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the configuration that will be used during EDL file creation.
    /// </summary>
    public static void LogConfiguration()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException("Logger must not be null");
        }

        var config = Plugin.Instance!.Configuration;

        _logger.LogDebug("Overwrite EDL files: {Regenerate}", config.OverwriteEdlFiles);
        _logger.LogDebug("Intro EdlAction: {Action}", config.IntroEdlAction);
        _logger.LogDebug("Outro EdlAction: {Action}", config.OutroEdlAction);
        _logger.LogDebug("Preview EdlAction: {Action}", config.PreviewEdlAction);
        _logger.LogDebug("Recap EdlAction: {Action}", config.RecapEdlAction);
        _logger.LogDebug("Commercial EdlAction: {Action}", config.CommercialEdlAction);
        _logger.LogDebug("Max Parallelism: {Action}", config.MaxParallelism);
    }

    /// <summary>
    /// Update EDL file for the provided segments.
    /// </summary>
    /// <param name="psegment">Key value pair of segments dictionary.</param>
    public static void UpdateEDLFile(KeyValuePair<Guid, List<MediaSegment>> psegment)
    {
        var overwrite = Plugin.Instance!.Configuration.OverwriteEdlFiles;
        var id = psegment.Key;
        var segments = psegment.Value;

        // Test if there ara any segments
        if (segments.Count > 0)
        {
            var filePath = Plugin.Instance!.GetItemPath(id);

            // guard for missing media file/folder.
            if (File.Exists(filePath))
            {
                var edlPath = GetEdlPath(filePath);
                var fexists = File.Exists(edlPath);

                // User may not want an override
                if (!fexists || (fexists && overwrite))
                {
                    var oldContent = string.Empty;
                    var edlContent = ToEdl(segments);
                    var update = false;

                    try
                    {
                        oldContent = File.ReadAllText(edlPath);
                    }
                    catch (Exception)
                    {
                    }

                    // check if we need to update
                    if (!string.IsNullOrEmpty(oldContent) && oldContent != edlContent)
                    {
                        update = true;
                    }

                    if (!fexists || update)
                    {
                        _logger?.LogDebug("{Action} EDL file '{File}'", update ? "Overwrite/Update" : "Create", edlPath);
                        File.WriteAllText(edlPath, edlContent);
                    }
                }
                else
                {
                    _logger?.LogDebug("EDL File exists, but overwrite is disabled: '{File}'", edlPath);
                }
            }
        }
    }

    /// <summary>
    /// Convert segments to a Kodi compatible EDL entry.
    /// </summary>
    /// <param name="segments">The Segments.</param>
    /// <returns>String content of edl file.</returns>
    private static string ToEdl(List<MediaSegment> segments)
    {
        var fstring = string.Empty;
        foreach (var segment in segments)
        {
            var start = Math.Round(segment.Start, 2);
            var end = Math.Round(segment.End, 2);
            var action = GetActionforType(segment.Type);

            // Skip None actions
            if (action != EdlAction.None)
            {
                fstring += string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1} {2} \n", start, end, (int)action);
            }
        }

        // remove last newline
        var newlineInd = fstring.LastIndexOf('\n');
        return newlineInd > 0 ? fstring.Substring(0, newlineInd) : fstring;
    }

    /// <summary>
    /// Convert a segments Type to an edl Action based on user settings.
    /// </summary>
    /// <param name="type">The Segments type.</param>
    /// <returns>String content of edl file.</returns>
    private static EdlAction GetActionforType(MediaSegmentType type)
    {
        switch (type)
        {
            case MediaSegmentType.Intro:
                return Plugin.Instance!.Configuration.IntroEdlAction;
            case MediaSegmentType.Outro:
                return Plugin.Instance!.Configuration.OutroEdlAction;
            case MediaSegmentType.Recap:
                return Plugin.Instance!.Configuration.RecapEdlAction;
            case MediaSegmentType.Preview:
                return Plugin.Instance!.Configuration.PreviewEdlAction;
            case MediaSegmentType.Commercial:
                return Plugin.Instance!.Configuration.CommercialEdlAction;
            default:
                return EdlAction.None;
        }
    }

    /// <summary>
    /// Given the path to an episode, return the path to the associated EDL file.
    /// </summary>
    /// <param name="mediaPath">Full path to episode.</param>
    /// <returns>Full path to EDL file.</returns>
    public static string GetEdlPath(string mediaPath)
    {
        return Path.ChangeExtension(mediaPath, "edl");
    }
}
