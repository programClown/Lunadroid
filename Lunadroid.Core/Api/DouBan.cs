namespace Lunadroid.Core.Api;

// 豆瓣排行榜 API 类型 ID 映射
public record DouBanChartGenreIds(string Genre, int Id)
{
    public static List<DouBanChartGenreIds> AllIds = new()
    {
        new DouBanChartGenreIds("剧情", 11),
        new DouBanChartGenreIds("喜剧", 24),
        new DouBanChartGenreIds("动作", 5),
        new DouBanChartGenreIds("爱情", 13),
        new DouBanChartGenreIds("科幻", 17),
        new DouBanChartGenreIds("动画", 25),
        new DouBanChartGenreIds("悬疑", 10),
        new DouBanChartGenreIds("惊悚", 19),
        new DouBanChartGenreIds("恐怖", 20),
        new DouBanChartGenreIds("纪录片", 1),
        new DouBanChartGenreIds("短片", 23),
        new DouBanChartGenreIds("情色", 6),
        new DouBanChartGenreIds("同性", 26),
        new DouBanChartGenreIds("音乐", 14),
        new DouBanChartGenreIds("歌舞", 7),
        new DouBanChartGenreIds("家庭", 28),
        new DouBanChartGenreIds("儿童", 8),
        new DouBanChartGenreIds("传记", 2),
        new DouBanChartGenreIds("历史", 4),
        new DouBanChartGenreIds("战争", 22)
        // IDs 9 和 21 似乎不存在
    };

    public int GetIdByGenre(string genre)
    {
        var select = AllIds.FirstOrDefault(a => a.Genre == genre);

        return select?.Id ?? -1;
    }
}