namespace Jellyfin.Plugin.Edl;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages enqueuing library items for analysis.
/// </summary>
public class QueueManager
{
    private ILibraryManager _libraryManager;
    private ILogger<QueueManager> _logger;
    private List<string> selectedLibraries;
    private Dictionary<string, List<int>> skippedTvShows;
    private List<string> skippedMovies;
    private Dictionary<Guid, List<QueuedMedia>> _queuedMedia;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueManager"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="libraryManager">Library manager.</param>
    public QueueManager(ILogger<QueueManager> logger, ILibraryManager libraryManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;

        selectedLibraries = new();
        _queuedMedia = new();
        skippedTvShows = new();
        skippedMovies = new();
    }

    /// <summary>
    /// Gets all media items on the server.
    /// </summary>
    /// <returns>Queued media items.</returns>
    public ReadOnlyDictionary<Guid, List<QueuedMedia>> GetMediaItems()
    {
        LoadAnalysisSettings();

        // For all selected libraries, enqueue all contained episodes.
        foreach (var folder in _libraryManager.GetVirtualFolders())
        {
            // If libraries have been selected for analysis, ensure this library was selected.
            if (selectedLibraries.Count > 0 && !selectedLibraries.Contains(folder.Name))
            {
                _logger.LogDebug("Not analyzing library \"{Name}\": not selected by user", folder.Name);
                continue;
            }

            _logger.LogInformation(
                "Running enqueue of items in library {Name} ({ItemId})",
                folder.Name,
                folder.ItemId);

            try
            {
                QueueLibraryContents(folder.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to enqueue items from library {Name}: {Exception}", folder.Name, ex);
            }
        }

        return new(_queuedMedia);
    }

    /// <summary>
    /// Gets media items based on given itemId. Skips all block lists.
    /// </summary>
    /// <param name="itemIds">All item ids to lookup.</param>
    /// <returns>Queued media items.</returns>
    public ReadOnlyDictionary<Guid, List<QueuedMedia>> GetMediaItemsById(Guid[] itemIds)
    {
        foreach (var item in itemIds)
        {
            var bitem = _libraryManager.GetItemById(item);
            if (bitem != null)
            {
                if (bitem is Episode episode)
                {
                    QueueEpisode(episode);
                }

                if (bitem is Movie movie)
                {
                    foreach (var source in movie.GetMediaSources(false))
                    {
                        QueueMovie(movie, source);
                    }
                }
            }
        }

        return new(_queuedMedia);
    }

    /// <summary>
    /// Loads the list of libraries which have been selected to use or skipped.
    /// </summary>
    private void LoadAnalysisSettings()
    {
        var config = Plugin.Instance!.Configuration;

        // Get the list of library names which have been selected for analysis, ignoring whitespace and empty entries.
        selectedLibraries = config.SelectedLibraries
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Get the list movie names which should be skipped.
        skippedMovies = config.SkippedMovies
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Get the list of tvshow names and seasons which should be skipped for analysis.
        var show = config.SkippedTvShows
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        foreach (var s in show)
        {
            if (s.Contains(';', System.StringComparison.InvariantCulture))
            {
                var rseasons = s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var seasons = rseasons.Skip(1).ToArray();
                var name = rseasons.ElementAt(0);
                var seasonNumbers = new List<int>();

                foreach (var season in seasons)
                {
                    var nr = season.Substring(1);

                    try
                    {
                        seasonNumbers.Add(int.Parse(nr, CultureInfo.InvariantCulture));
                    }
                    catch (FormatException)
                    {
                        _logger.LogError("Skipping TV Shows: Failed to parse season number '{Nr}' for tv show: {Name}. Fix your config!", nr, name);
                    }
                }

                skippedTvShows.Add(name, seasonNumbers);
            }
            else
            {
                skippedTvShows.Add(s, new List<int>());
            }
        }

        // If any libraries have been selected for analysis, log their names.
        if (selectedLibraries.Count > 0)
        {
            _logger.LogInformation("Limiting analysis to the following libraries: {Selected}", selectedLibraries);
        }
        else
        {
            _logger.LogDebug("Not limiting analysis by library name");
        }
    }

    private void QueueLibraryContents(string rawId)
    {
        _logger.LogDebug("Constructing anonymous internal query");

        var includes = new BaseItemKind[] { BaseItemKind.Episode, BaseItemKind.Movie };

        var query = new InternalItemsQuery()
        {
            // Order by series name, season, and then episode number so that status updates are logged in order
            ParentId = Guid.Parse(rawId),
            OrderBy = new[]
            {
                (ItemSortBy.SeriesSortName, SortOrder.Ascending),
                (ItemSortBy.ParentIndexNumber, SortOrder.Ascending),
                (ItemSortBy.IndexNumber, SortOrder.Ascending),
            },
            IncludeItemTypes = includes,
            Recursive = true,
            IsVirtualItem = false
        };

        _logger.LogDebug("Getting items");

        var items = _libraryManager.GetItemList(query, false);

        if (items is null)
        {
            _logger.LogError("Library query result is null");
            return;
        }

        // Queue all media on the server.
        _logger.LogDebug("Iterating through library items");

        foreach (var item in items)
        {
            if (item is Episode episode)
            {
                if (SkipEpisode(episode))
                {
                    _logger.LogInformation("Skipping episode: '{EpisodeName}' of series: '{SeriesName} S{Season}'", episode.Name, episode.SeriesName, episode.AiredSeasonNumber);
                    continue;
                }

                QueueEpisode(episode);
            }
            else if (item is Movie movie)
            {
                if (skippedMovies.Contains(movie.Name))
                {
                    _logger.LogInformation("Skipping Movie: '{Name}'", movie.Name);
                    continue;
                }

                // Movie can have multiple MediaSources like 1080p and a 4k file, they have different ids
                foreach (MediaSourceInfo source in movie.GetMediaSources(false))
                {
                    _logger.LogInformation("Adding movie: '{Name} ({Format})'", movie.Name, source.Name);
                    QueueMovie(movie, source);
                }
            }
            else
            {
                _logger.LogDebug("Item {Name} is not an episode or movie", item.Name);
                continue;
            }
        }

        _logger.LogDebug("Queued {Count} media items", items.Count);
    }

    // Test if should skip the episode
    private bool SkipEpisode(Episode episode)
    {
        if (skippedTvShows.TryGetValue(episode.SeriesName, out var seasons))
        {
            return (episode.AiredSeasonNumber != null && seasons.Contains(episode.AiredSeasonNumber.GetValueOrDefault())) ? true : false;
        }

        return false;
    }

    private void QueueEpisode(Episode episode)
    {
        if (Plugin.Instance is null)
        {
            throw new InvalidOperationException("plugin instance was null");
        }

        if (string.IsNullOrEmpty(episode.Path))
        {
            _logger.LogWarning(
                "Not queuing episode \"{Name}\" from series \"{Series}\" ({Id}) as no path was provided by Jellyfin",
                episode.Name,
                episode.SeriesName,
                episode.Id);
            return;
        }

        if (episode.RunTimeTicks is null)
        {
            _logger.LogWarning(
                "Not queuing episode \"{Name}\" from series \"{Series}\" ({Id}) as no duration was provided by Jellyfin",
                episode.Name,
                episode.SeriesName,
                episode.Id);
            return;
        }

        // Allocate a new list for each new season
        _queuedMedia.TryAdd(episode.SeasonId, new List<QueuedMedia>());

        _queuedMedia[episode.SeasonId].Add(new QueuedMedia()
        {
            SeriesName = episode.SeriesName,
            SeasonNumber = episode.AiredSeasonNumber ?? 0,
            ItemId = episode.Id,
            Name = episode.Name,
            Path = episode.Path,
        });
    }

    private void QueueMovie(Movie movie, MediaSourceInfo source)
    {
        if (Plugin.Instance is null)
        {
            throw new InvalidOperationException("plugin instance was null");
        }

        if (string.IsNullOrEmpty(source.Path))
        {
            _logger.LogWarning(
                "Not queuing movie '{Name} ({Source})' ({Id}) as no path was provided by Jellyfin",
                movie.Name,
                source.Name,
                source.Id);
            return;
        }

        if (movie.RunTimeTicks is null)
        {
            _logger.LogWarning(
                "Not queuing movie '{Name} ({Source})' ({Id}) as no duration was provided by Jellyfin",
                movie.Name,
                source.Name,
                source.Id);
            return;
        }

        // Allocate a new list for each movie
        _queuedMedia.TryAdd(Guid.Parse(source.Id), new List<QueuedMedia>());

        _queuedMedia[Guid.Parse(source.Id)].Add(new QueuedMedia()
        {
            SeriesName = movie.Name,
            SeasonNumber = 0,
            ItemId = Guid.Parse(source.Id),
            Name = $"{movie.Name} ({source.Name})",
            Path = source.Path,
            IsEpisode = false,
        });
    }

    /// <summary>
    /// Verify that a collection of queued media items still exist in Jellyfin and in storage.
    /// This is done to ensure that we don't use items that were deleted between the call to GetMediaItems() and popping them from the queue.
    /// </summary>
    /// <param name="candidates">Queued media items.</param>
    /// <returns>Media items that have been verified to exist in Jellyfin and in storage.</returns>
    public ReadOnlyCollection<QueuedMedia>
        VerifyQueue(ReadOnlyCollection<QueuedMedia> candidates)
    {
        var verified = new List<QueuedMedia>();

        foreach (var candidate in candidates)
        {
            try
            {
                if (File.Exists(candidate.Path))
                {
                    verified.Add(candidate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    "Skipping queue of {Name} ({Id}): {Exception}",
                    candidate.Name,
                    candidate.ItemId,
                    ex);
            }
        }

        return verified.AsReadOnly();
    }
}
