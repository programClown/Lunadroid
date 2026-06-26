using FFmpeg.AutoGen;
using N_m3u8DL_RE.Entity;

namespace N_m3u8DL_RE.Util;

internal static unsafe class FFmpegProbe
{
    public static List<Mediainfo> Probe(string filePath)
    {
        AVFormatContext* rawCtx = null;

        int ret = ffmpeg.avformat_open_input(&rawCtx, filePath, null, null);
        if (ret != 0 || rawCtx == null)
        {
            return CreateFallback();
        }

        try
        {
            ret = ffmpeg.avformat_find_stream_info(rawCtx, null);
            if (ret < 0)
            {
                return CreateFallback();
            }

            return ExtractStreams(rawCtx);
        }
        finally
        {
            ffmpeg.avformat_close_input(&rawCtx);
        }
    }

    private static List<Mediainfo> ExtractStreams(AVFormatContext* ctx)
    {
        var results = new List<Mediainfo>();

        for (int i = 0; i < ctx->nb_streams; i++)
        {
            var stream = ctx->streams[i];
            var par = stream->codecpar;

            var info = new Mediainfo
            {
                Id = $"[0x{i:x}]",
                Type = MapMediaType(par->codec_type),
                BaseInfo = ffmpeg.avcodec_get_name(par->codec_id)
            };

            ApplyStreamDetails(info, stream, par);
            results.Add(info);
        }

        return results.Count == 0 ? CreateFallback() : results;
    }

    private static void ApplyStreamDetails(Mediainfo info, AVStream* stream, AVCodecParameters* par)
    {
        switch (par->codec_type)
        {
            case AVMediaType.AVMEDIA_TYPE_VIDEO:
                ApplyVideoDetails(info, stream, par);
                break;

            case AVMediaType.AVMEDIA_TYPE_AUDIO:
                ApplyAudioDetails(info, par);
                break;
        }

        DetectStartTime(info, stream);
    }

    private static void ApplyVideoDetails(Mediainfo info, AVStream* stream, AVCodecParameters* par)
    {
        info.Resolution = $"{par->width}x{par->height}";
        info.Text = $"{info.BaseInfo}, {info.Resolution}";

        if (par->bit_rate > 0)
        {
            info.Bitrate = $"{par->bit_rate / 1000} kb/s";
        }

        double fps = ffmpeg.av_q2d(stream->avg_frame_rate);
        if (fps > 0)
        {
            info.Fps = $"{fps:F2} fps";
        }

        DetectHdr(info, par);
        DetectDolbyVision(info, par);
    }

    private static void ApplyAudioDetails(Mediainfo info, AVCodecParameters* par)
    {
        info.Text = $"{info.BaseInfo}, {par->sample_rate} Hz";

        if (par->bit_rate > 0)
        {
            info.Bitrate = $"{par->bit_rate / 1000} kb/s";
        }
    }

    private static void DetectHdr(Mediainfo info, AVCodecParameters* par)
    {
        info.HDR = par->color_trc == AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084 ||
                   par->color_trc == AVColorTransferCharacteristic.AVCOL_TRC_ARIB_STD_B67 ||
                   par->color_primaries == AVColorPrimaries.AVCOL_PRI_BT2020 ||
                   par->color_space == AVColorSpace.AVCOL_SPC_BT2020_NCL ||
                   par->color_space == AVColorSpace.AVCOL_SPC_BT2020_CL;
    }

    private static void DetectDolbyVision(Mediainfo info, AVCodecParameters* par)
    {
        string codecName = info.BaseInfo ?? "";
        if (codecName is "dvhe" or "dvh1" or "dvvideo")
        {
            info.DolbyVison = true;
            return;
        }

        for (int j = 0; j < par->nb_coded_side_data; j++)
        {
            if (par->coded_side_data[j].type == AVPacketSideDataType.AV_PKT_DATA_DOVI_CONF)
            {
                info.DolbyVison = true;
                return;
            }
        }
    }

    private static void DetectStartTime(Mediainfo info, AVStream* stream)
    {
        if (stream->start_time == ffmpeg.AV_NOPTS_VALUE)
        {
            return;
        }

        double seconds = stream->start_time * ffmpeg.av_q2d(stream->time_base);
        info.StartTime = TimeSpan.FromSeconds(seconds);
    }

    private static string MapMediaType(AVMediaType type)
    {
        return type switch
        {
            AVMediaType.AVMEDIA_TYPE_VIDEO => "Video",
            AVMediaType.AVMEDIA_TYPE_AUDIO => "Audio",
            AVMediaType.AVMEDIA_TYPE_SUBTITLE => "Subtitle",
            AVMediaType.AVMEDIA_TYPE_DATA => "Data",
            AVMediaType.AVMEDIA_TYPE_ATTACHMENT => "Attachment",
            _ => "Unknown"
        };
    }

    private static List<Mediainfo> CreateFallback()
    {
        return [new Mediainfo { Type = "Unknown" }];
    }
}