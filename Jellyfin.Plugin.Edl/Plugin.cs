using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Edl.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Edl;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private ILibraryManager _libraryManager;
    private IMediaSegmentsManager _mediaSegmentsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="mediaSegmentsManager">Segments manager.</param>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager,
        IMediaSegmentsManager mediaSegmentsManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        _libraryManager = libraryManager;
        _mediaSegmentsManager = mediaSegmentsManager;
    }

    /// <inheritdoc />
    public override string Name => "EDL Creator";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("6B0E323A-4AEE-4B10-813F-1E060488AE90");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }

    /// <summary>
    /// Gets all media segments from db.
    /// </summary>
    /// <returns>All media segments.</returns>
    public ReadOnlyCollection<MediaSegment> GetAllMediaSegments()
    {
        return _mediaSegmentsManager.GetAllMediaSegments().AsReadOnly();
    }

    internal BaseItem GetItem(Guid id)
    {
        return _libraryManager.GetItemById(id);
    }

    /// <summary>
    /// Gets the full path for an item.
    /// </summary>
    /// <param name="id">Item id.</param>
    /// <returns>Full path to item.</returns>
    internal string GetItemPath(Guid id)
    {
        return GetItem(id).Path;
    }
}
