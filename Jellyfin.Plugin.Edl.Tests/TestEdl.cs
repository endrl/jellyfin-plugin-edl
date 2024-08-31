using System;
using Xunit;

namespace Jellyfin.Plugin.Edl.Tests;

public class TestEdl
{
    [Theory]
    [InlineData(53000000, 71000000, EdlAction.Cut, "5.3 7.1 0 \n")]
    [InlineData(150000000, 167000000, EdlAction.Mute, "15 16.7 1 \n")]
    [InlineData(4200000000, 8220000000, EdlAction.CommercialBreak, "420 822 3 \n")]
    [InlineData(10000009, 2553000000, EdlAction.SceneMarker, "1 255.3 2 \n")]
    [InlineData(11234568, 56546475, EdlAction.CommercialBreak, "1.123 5.655 3 \n")]
    public void TestEdlSerialization(long start, long end, EdlAction action, string expected)
    {
        var actual = EdlManager.ToEdlString(start, end, action);

        Assert.Equal(expected, actual);
    }
}
