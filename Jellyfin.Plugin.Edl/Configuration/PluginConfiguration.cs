using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Edl.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        IntroEdlAction = EdlAction.None;
        OutroEdlAction = EdlAction.None;
        PreviewEdlAction = EdlAction.None;
        RecapEdlAction = EdlAction.None;
        CommercialEdlAction = EdlAction.CommercialBreak;
    }

    /// <summary>
    /// Gets or sets an Intro Action option.
    /// </summary>
    public EdlAction IntroEdlAction { get; set; }

    /// <summary>
    /// Gets or sets an Outro Action option.
    /// </summary>
    public EdlAction OutroEdlAction { get; set; }

    /// <summary>
    /// Gets or sets an Preview Action option.
    /// </summary>
    public EdlAction PreviewEdlAction { get; set; }

    /// <summary>
    /// Gets or sets an Recap Action option.
    /// </summary>
    public EdlAction RecapEdlAction { get; set; }

    /// <summary>
    /// Gets or sets an Commercial Action option.
    /// </summary>
    public EdlAction CommercialEdlAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing edl files. Which keeps the file in sync with media segment edits.
    /// </summary>
    public bool OverwriteEdlFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets the max degree of parallelism used when creating edl files.
    /// </summary>
    public int MaxParallelism { get; set; } = 2;
}
