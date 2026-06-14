using Lunadroid.Core.Helpers;

namespace Lunadroid.Tests;

public class RelativeTimeHelperTests
{
    #region ToRelativeTime Tests

    [Fact]
    public void ToRelativeTime_JustNow_ShouldReturnGangGang()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddSeconds(-10));
        Assert.Equal("刚刚", result);
    }

    [Fact]
    public void ToRelativeTime_MinutesAgo_ShouldReturnMinutes()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddMinutes(-5));
        Assert.Equal("5分钟前", result);
    }

    [Fact]
    public void ToRelativeTime_HoursAgo_ShouldReturnHours()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddHours(-3));
        Assert.Equal("3小时前", result);
    }

    [Fact]
    public void ToRelativeTime_DaysAgo_ShouldReturnDays()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddDays(-7));
        Assert.Equal("7天前", result);
    }

    [Fact]
    public void ToRelativeTime_MonthsAgo_ShouldReturnMonths()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddDays(-60));
        Assert.Equal("2个月前", result);
    }

    [Fact]
    public void ToRelativeTime_YearsAgo_ShouldReturnYears()
    {
        var result = RelativeTimeHelper.ToRelativeTime(DateTime.Now.AddDays(-400));
        Assert.Equal("1年前", result);
    }

    #endregion

    #region FormatDuration Tests

    [Fact]
    public void FormatDuration_Zero_ShouldReturnDoubleZero()
    {
        Assert.Equal("00:00", RelativeTimeHelper.FormatDuration(0));
    }

    [Fact]
    public void FormatDuration_Negative_ShouldReturnDoubleZero()
    {
        Assert.Equal("00:00", RelativeTimeHelper.FormatDuration(-5));
    }

    [Fact]
    public void FormatDuration_SecondsOnly_ShouldReturnMmSs()
    {
        Assert.Equal("00:30", RelativeTimeHelper.FormatDuration(30));
    }

    [Fact]
    public void FormatDuration_MinutesAndSeconds()
    {
        Assert.Equal("05:30", RelativeTimeHelper.FormatDuration(330));
    }

    [Fact]
    public void FormatDuration_OverOneHour_ShouldReturnHMmSs()
    {
        Assert.Equal("1:30:00", RelativeTimeHelper.FormatDuration(5400));
    }

    [Fact]
    public void FormatDuration_TwoHours_ShouldReturnCorrectFormat()
    {
        Assert.Equal("2:05:30", RelativeTimeHelper.FormatDuration(7530));
    }

    #endregion

    #region FormatFileSize Tests

    [Fact]
    public void FormatFileSize_Bytes_ShouldReturnB()
    {
        Assert.Equal("500 B", RelativeTimeHelper.FormatFileSize(500));
    }

    [Fact]
    public void FormatFileSize_Kilobytes_ShouldReturnKB()
    {
        var result = RelativeTimeHelper.FormatFileSize(2048);
        Assert.Equal("2.0 KB", result);
    }

    [Fact]
    public void FormatFileSize_Megabytes_ShouldReturnMB()
    {
        var result = RelativeTimeHelper.FormatFileSize(5 * 1024 * 1024);
        Assert.Equal("5.0 MB", result);
    }

    [Fact]
    public void FormatFileSize_Gigabytes_ShouldReturnGB()
    {
        var result = RelativeTimeHelper.FormatFileSize(2L * 1024 * 1024 * 1024);
        Assert.Equal("2.00 GB", result);
    }

    [Fact]
    public void FormatFileSize_Zero_ShouldReturn0B()
    {
        Assert.Equal("0 B", RelativeTimeHelper.FormatFileSize(0));
    }

    [Fact]
    public void FormatFileSize_Exactly1KB()
    {
        Assert.Equal("1.0 KB", RelativeTimeHelper.FormatFileSize(1024));
    }

    #endregion
}
