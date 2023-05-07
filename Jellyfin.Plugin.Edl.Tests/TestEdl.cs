using System;
using Xunit;

namespace Jellyfin.Plugin.Edl.Tests;

public class TestEdl
{
    // Test data is from https://kodi.wiki/view/Edit_decision_list#MPlayer_EDL
    [Theory]
    [InlineData(5.3, 7.1, EdlAction.Cut, "5.3 7.1 0")]
    [InlineData(15, 16.7, EdlAction.Mute, "15 16.7 1")]
    [InlineData(420, 822, EdlAction.CommercialBreak, "420 822 3")]
    [InlineData(1, 255.3, EdlAction.SceneMarker, "1 255.3 2")]
    [InlineData(1.123456789, 5.654647987, EdlAction.CommercialBreak, "1.12 5.65 3")]
    public void TestEdlSerialization(double Start, double End, EdlAction action, string expected)
    {
        var list = new List<MediaSegment> { new MediaSegment { Start, End } };
        var actual = EdlManager.ToEdl(list);

        Assert.Equal(expected, actual);
    }
}
