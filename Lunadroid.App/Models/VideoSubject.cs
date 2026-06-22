using System.Text.Json.Serialization;
using Lunadroid.App.Converters;

namespace Lunadroid.App.Models;

public class VideoSubject
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("msg")] public string? Msg { get; set; }
    [JsonPropertyName("pagecount")] public int PageCount { get; set; }

    [JsonConverter(typeof(FlexibleIntConverter))]
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")] public int Total { get; set; }

    [JsonConverter(typeof(FlexibleIntConverter))]
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("list")] public List<VideoSubSubject>? List { get; set; }
}

public class VideoSubSubject
{
    [JsonConverter(typeof(FlexibleStringConverter))]
    [JsonPropertyName("vod_id")]
    public string VodId { get; set; }

    [JsonPropertyName("type_id")] public int TypeId { get; set; }
    [JsonPropertyName("type_id_1")] public int TypeId1 { get; set; }
    [JsonPropertyName("group_id")] public int GroupId { get; set; }
    [JsonPropertyName("vod_name")] public string? VodName { get; set; }
    [JsonPropertyName("vod_sub")] public string? VodSub { get; set; }

    // [JsonPropertyName("vod_en")] public string? VodEn { set; get; }
    [JsonPropertyName("vod_status")] public int VodStatus { get; set; }

    [JsonPropertyName("vod_letter")] public string? VodLetter { get; set; }

    // [JsonPropertyName("vod_color")] public string? VodColor { set; get; }
    // [JsonPropertyName("vod_tag")] public string? VodTag { set; get; }
    [JsonPropertyName("vod_class")] public string? VodClass { get; set; }

    [JsonPropertyName("vod_pic")] public string? VodPic { get; set; }
    // [JsonPropertyName("vod_pic_thumb")] public string? VodPicThumb { set; get; }
    //
    // [JsonPropertyName("vod_pic_slide")] public string? VodPicSlide { set; get; }
    //
    // [JsonPropertyName("vod_pic_screenshot")]
    // public string? VodPicScreenshot { set; get; }

    [JsonPropertyName("vod_actor")] public string? VodActor { get; set; }

    [JsonPropertyName("vod_director")] public string? VodDirector { get; set; }

    [JsonPropertyName("vod_writer")] public string? VodWriter { get; set; }

    // [JsonPropertyName("vod_behind")] public string? VodBehind { set; get; }
    [JsonPropertyName("vod_blurb")] public string? VodBlurb { get; set; }
    [JsonPropertyName("vod_remarks")] public string? VodRemarks { get; set; }

    [JsonPropertyName("vod_pubdate")] public string? VodPubdate { get; set; }

    [JsonPropertyName("vod_total")] public int VodTotal { get; set; }

    // [JsonPropertyName("vod_serial")] public string? VodSerial { set; get; }
    // [JsonPropertyName("vod_tv")] public string? VodTv { set; get; }
    // [JsonPropertyName("vod_weekday")] public string? VodWeekday { set; get; }
    [JsonPropertyName("vod_area")] public string? VodArea { get; set; }
    [JsonPropertyName("vod_lang")] public string? VodLang { get; set; }
    [JsonPropertyName("vod_year")] public string? VodYear { get; set; }

    // [JsonPropertyName("vod_version")] public string? VodVersion { set; get; }
    // [JsonPropertyName("vod_state")] public string? VodState { set; get; }
    // [JsonPropertyName("vod_author")] public string? VodAuthor { set; get; }
    // [JsonPropertyName("vod_score")] public string? VodScore { set; get; }
    // [JsonPropertyName("vod_score_all")] public string? VodScoreAll { set; get; }
    // [JsonPropertyName("vod_score_num")] public string? VodScoreNum { set; get; }
    // [JsonPropertyName("vod_time")] public string? VodTime { set; get; }
    // [JsonPropertyName("vod_time_add")] public int VodTimeAdd { set; get; }
    // [JsonPropertyName("vod_time_hits")] public int VodTimeHits { set; get; }
    // [JsonPropertyName("vod_time_make")] public int VodTimeMake { set; get; }
    // [JsonPropertyName("vod_trysee")] public int VodTrysee { set; get; }
    // [JsonPropertyName("vod_douban_id")] public int VodDoubanId { set; get; }
    // [JsonPropertyName("vod_douban_score")] public string? VodDoubanScore { set; get; }
    // [JsonPropertyName("vod_reurl")] public string? VodReurl { set; get; }
    // [JsonPropertyName("vod_rel_vod")] public string? VodRelVod { set; get; }
    // [JsonPropertyName("vod_rel_art")] public string? VodRelArt { set; get; }
    // [JsonPropertyName("vod_pwd")] public string? VodPwd { set; get; }
    // [JsonPropertyName("vod_pwd_url")] public string? VodPwdUrl { set; get; }
    // [JsonPropertyName("vod_pwd_play")] public string? VodPwdPlay { set; get; }
    // [JsonPropertyName("vod_pwd_play_url")] public string? VodPwdPlayUrl { set; get; }
    // [JsonPropertyName("vod_pwd_down")] public string? VodPwdDown { set; get; }
    // [JsonPropertyName("vod_pwd_down_url")] public string? VodPwdDownUrl { set; get; }
    [JsonPropertyName("vod_content")] public string? VodContent { get; set; }

    [JsonPropertyName("vod_play_from")] public string? VodPlayFrom { get; set; }

    [JsonPropertyName("vod_play_server")] public string? VodPlayServer { get; set; }

    [JsonPropertyName("vod_play_note")] public string? VodPlayNote { get; set; }

    [JsonPropertyName("vod_play_url")] public string? VodPlayUrl { get; set; }

    // [JsonPropertyName("vod_plot")] public int VodPlot { set; get; }
    // [JsonPropertyName("vod_plot_name")] public string? VodPlotName { set; get; }
    // [JsonPropertyName("vod_plot_detail")] public string? VodPlotDetail { set; get; }
    [JsonPropertyName("type_name")] public string? TypeName { get; set; }
}