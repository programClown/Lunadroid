using FFmpeg.AutoGen;
using N_m3u8DL_RE.Common.Enum;
using N_m3u8DL_RE.Common.Log;
using Spectre.Console;
using System.Runtime.InteropServices;

namespace N_m3u8DL_RE.Util;

internal static unsafe class FFmpegRemuxer
{
    public static bool ConcatMerge(string[] inputFiles, string outputPath, string muxFormat,
        bool useAACFilter, bool fastStart, bool writeDate,
        string poster, string ddpAudio, string audioName, string title,
        string copyright, string comment, string encodingTool, string dateString)
    {
        AVFormatContext** inputs = null;
        AVFormatContext* outCtx = null;
        var pkt = ffmpeg.av_packet_alloc();
        int n = inputFiles.Length;

        try
        {
            inputs = AllocateContextArray(n);
            if (!OpenAllInputs(inputFiles, inputs, n))
            {
                return false;
            }

            string fmtName = MapOutputFormat(muxFormat);
            if (ffmpeg.avformat_alloc_output_context2(&outCtx, null, fmtName, outputPath) < 0 || outCtx == null)
            {
                return false;
            }

            int[][] mapping = new int[n][];
            var outStreams = new List<IntPtr>();

            bool hasPoster = !string.IsNullOrEmpty(poster);
            bool hasDdpAudio = !string.IsNullOrEmpty(ddpAudio);

            if (!SetupConcatStreamsWithMapping(inputs, n, outCtx, mapping, outStreams,
                    muxFormat, hasPoster, hasDdpAudio))
            {
                return false;
            }

            EnsureStreamCompatibility(outCtx, outStreams, muxFormat);

            ApplyOutputMetadata(outCtx, writeDate, dateString, title, copyright, comment, encodingTool);

            ApplyAudioMetadata(outStreams, hasDdpAudio, audioName, ddpAudio);

            if (hasPoster)
            {
                ApplyPosterDisposition(outStreams);
            }

            if (!OpenOutputFile(outCtx, outputPath))
            {
                return false;
            }

            AVDictionary* opts = null;
            if (fastStart && muxFormat.Equals("MP4", StringComparison.OrdinalIgnoreCase))
            {
                ffmpeg.av_dict_set(&opts, "movflags", "+faststart", 0);
            }

            EnsureVideoExtradata(inputs, n, outStreams);

            int ret = ffmpeg.avformat_write_header(outCtx, &opts);
            ffmpeg.av_dict_free(&opts);
            if (ret < 0) return false;

            WriteConcatPackets(inputs, n, outCtx, mapping, outStreams, pkt,
                useAACFilter, muxFormat);

            ffmpeg.av_write_trailer(outCtx);
            return true;
        }
        catch (Exception ex)
        {
            Logger.WarnMarkUp($"[grey]Remux error: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
        finally
        {
            CleanupResources(inputs, n, outCtx, pkt);
        }
    }

    public static bool MuxStreams(string[] inputPaths,
        string outputPath, string muxFormat, bool dateInfo,
        string dateString, string[][] streamLangs, string[][] streamTitles,
        int[][] streamDispositions, MediaType?[] mediaTypes,
        bool[] hasSrtSubtitle, bool clearMetadata = true)
    {
        AVFormatContext** inputs = null;
        AVFormatContext* outCtx = null;
        var pkt = ffmpeg.av_packet_alloc();
        int n = inputPaths.Length;

        try
        {
            inputs = AllocateContextArray(n);
            if (!OpenAllInputs(inputPaths, inputs, n))
            {
                return false;
            }

            string fmtName = MapOutputFormat(muxFormat);
            if (ffmpeg.avformat_alloc_output_context2(&outCtx, null, fmtName, outputPath) < 0 || outCtx == null)
            {
                return false;
            }

            var allOutStreams = new List<IntPtr>();
            var sourceMap = new List<(int fileIdx, int streamIdx)>();

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < inputs[i]->nb_streams; j++)
                {
                    var inStream = inputs[i]->streams[j];
                    AVMediaType codecType = inStream->codecpar->codec_type;

                    if (codecType == AVMediaType.AVMEDIA_TYPE_DATA)
                    {
                        continue;
                    }

                    var outStream = ffmpeg.avformat_new_stream(outCtx, null);
                    if (outStream == null) return false;

                    ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar);
                    outStream->time_base = inStream->time_base;

                    ApplySubtitleCodecOverride(outStream, inStream, muxFormat,
                        hasSrtSubtitle != null && i < hasSrtSubtitle.Length && hasSrtSubtitle[i]);

                    allOutStreams.Add((IntPtr)outStream);
                    sourceMap.Add((i, j));
                }
            }

            if (clearMetadata)
            {
                ClearMetadata(outCtx);
            }

            if (dateInfo && !string.IsNullOrEmpty(dateString))
            {
                ffmpeg.av_dict_set(&outCtx->metadata, "date", dateString, 0);
            }

            ApplyStreamMetadata(allOutStreams, streamLangs, streamTitles);
            ApplyStreamDispositions(allOutStreams, streamDispositions, inputs, n, mediaTypes);

            if (!OpenOutputFile(outCtx, outputPath))
            {
                return false;
            }

            int ret = ffmpeg.avformat_write_header(outCtx, null);
            if (ret < 0) return false;

            WriteMuxPackets(inputs, n, outCtx, allOutStreams, sourceMap, pkt);

            ffmpeg.av_write_trailer(outCtx);
            return true;
        }
        catch (Exception ex)
        {
            Logger.WarnMarkUp($"[grey]Mux error: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
        finally
        {
            CleanupResources(inputs, n, outCtx, pkt);
        }
    }

    private static AVFormatContext** AllocateContextArray(int count)
    {
        return (AVFormatContext**)ffmpeg.av_mallocz((ulong)(count * IntPtr.Size));
    }

    private static bool OpenAllInputs(string[] paths, AVFormatContext** inputs, int n)
    {
        for (int i = 0; i < n; i++)
        {
            AVFormatContext* ctx = null;
            int ret = ffmpeg.avformat_open_input(&ctx, paths[i], null, null);
            if (ret < 0)
            {
                LogFFmpegError("open input", ret);
                return false;
            }
            inputs[i] = ctx;
            ret = ffmpeg.avformat_find_stream_info(inputs[i], null);
            if (ret < 0)
            {
                LogFFmpegError("find stream info", ret);
                return false;
            }
        }
        return true;
    }

    private static bool SetupConcatStreamsWithMapping(AVFormatContext** inputs, int n,
        AVFormatContext* outCtx, int[][] mapping, List<IntPtr> outStreams,
        string muxFormat, bool hasPoster, bool hasDdpAudio)
    {
        bool isAudioOnly = muxFormat.ToUpper() is "EAC3" or "AAC" or "AC3";
        bool isTsFormat = muxFormat.Equals("TS", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < n; i++)
        {
            int nbStreams = (int)inputs[i]->nb_streams;
            mapping[i] = new int[nbStreams];

            for (int j = 0; j < nbStreams; j++)
            {
                var inStream = inputs[i]->streams[j];
                AVMediaType codecType = inStream->codecpar->codec_type;

                if (isAudioOnly)
                {
                    if (codecType != AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        mapping[i][j] = -1;
                        continue;
                    }
                }
                else if (codecType != AVMediaType.AVMEDIA_TYPE_AUDIO &&
                         codecType != AVMediaType.AVMEDIA_TYPE_VIDEO &&
                         codecType != AVMediaType.AVMEDIA_TYPE_SUBTITLE)
                {
                    mapping[i][j] = -1;
                    continue;
                }

                if (i == 0)
                {
                    bool shouldMap = true;
                    if (!isAudioOnly &&
                        !muxFormat.Equals("TS", StringComparison.OrdinalIgnoreCase) &&
                        !muxFormat.Equals("MKV", StringComparison.OrdinalIgnoreCase) &&
                        !muxFormat.Equals("FLV", StringComparison.OrdinalIgnoreCase) &&
                        !muxFormat.Equals("M4A", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldMap = codecType == AVMediaType.AVMEDIA_TYPE_VIDEO ||
                                    codecType == AVMediaType.AVMEDIA_TYPE_AUDIO ||
                                    codecType == AVMediaType.AVMEDIA_TYPE_SUBTITLE;
                    }

                    var outStream = ffmpeg.avformat_new_stream(outCtx, null);
                    if (outStream == null) return false;

                    ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar);
                    outStream->time_base = inStream->time_base;
                    mapping[i][j] = outStreams.Count;
                    outStreams.Add((IntPtr)outStream);
                }
                else
                {
                    int matched = FindMatchingStream(outStreams, inputs[i], j, outCtx);
                    mapping[i][j] = matched;
                }
            }
        }
        return true;
    }

    private static int FindMatchingStream(List<IntPtr> outStreams,
        AVFormatContext* input, int streamIdx, AVFormatContext* outCtx)
    {
        var par = input->streams[streamIdx]->codecpar;

        for (int k = 0; k < outStreams.Count; k++)
        {
            var outPar = ((AVStream*)outStreams[k])->codecpar;
            if (outPar->codec_type == par->codec_type &&
                outPar->codec_id == par->codec_id)
            {
                return k;
            }
        }

        var newOut = ffmpeg.avformat_new_stream(outCtx, null);
        if (newOut == null) return -1;

        ffmpeg.avcodec_parameters_copy(newOut->codecpar, par);
        newOut->time_base = input->streams[streamIdx]->time_base;
        outStreams.Add((IntPtr)newOut);
        return outStreams.Count - 1;
    }

    private static void ApplyOutputMetadata(AVFormatContext* outCtx,
        bool writeDate, string dateString, string title, string copyright,
        string comment, string encodingTool)
    {
        if (writeDate && !string.IsNullOrEmpty(dateString))
        {
            ffmpeg.av_dict_set(&outCtx->metadata, "date", dateString, 0);
        }
        if (!string.IsNullOrEmpty(title))
        {
            ffmpeg.av_dict_set(&outCtx->metadata, "title", title, 0);
        }
        if (!string.IsNullOrEmpty(copyright))
        {
            ffmpeg.av_dict_set(&outCtx->metadata, "copyright", copyright, 0);
        }
        if (!string.IsNullOrEmpty(comment))
        {
            ffmpeg.av_dict_set(&outCtx->metadata, "comment", comment, 0);
        }
        if (!string.IsNullOrEmpty(encodingTool))
        {
            ffmpeg.av_dict_set(&outCtx->metadata, "encoding_tool", encodingTool, 0);
        }
    }

    private static void ApplyAudioMetadata(List<IntPtr> outStreams,
        bool hasDdpAudio, string audioName, string ddpAudio)
    {
        int audioIdx = 0;
        for (int i = 0; i < outStreams.Count; i++)
        {
            var stream = (AVStream*)outStreams[i];
            if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                if (hasDdpAudio && audioIdx == 0)
                {
                    if (!string.IsNullOrEmpty(ddpAudio))
                    {
                        ffmpeg.av_dict_set(&stream->metadata, "title", "DD+", 0);
                        ffmpeg.av_dict_set(&stream->metadata, "handler", "DD+", 0);
                    }
                    audioIdx++;
                    continue;
                }

                if (!string.IsNullOrEmpty(audioName))
                {
                    ffmpeg.av_dict_set(&stream->metadata, "title", audioName, 0);
                    ffmpeg.av_dict_set(&stream->metadata, "handler", audioName, 0);
                }
                audioIdx++;
            }
        }
    }

    private static void ApplyPosterDisposition(List<IntPtr> outStreams)
    {
        for (int i = 0; i < outStreams.Count; i++)
        {
            var stream = (AVStream*)outStreams[i];
            if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                stream->disposition = ffmpeg.AV_DISPOSITION_ATTACHED_PIC;
                break;
            }
        }
    }

    private static void ApplySubtitleCodecOverride(AVStream* outStream, AVStream* inStream,
        string muxFormat, bool hasSrt)
    {
        if (outStream->codecpar->codec_type != AVMediaType.AVMEDIA_TYPE_SUBTITLE)
        {
            return;
        }

        AVCodecID codecId = inStream->codecpar->codec_id;
        if (muxFormat.Equals("MP4", StringComparison.OrdinalIgnoreCase))
        {
            if (codecId != AVCodecID.AV_CODEC_ID_MOV_TEXT)
            {
                outStream->codecpar->codec_id = AVCodecID.AV_CODEC_ID_MOV_TEXT;
            }
        }
        else if (muxFormat.Equals("MKV", StringComparison.OrdinalIgnoreCase))
        {
            if (hasSrt && codecId != AVCodecID.AV_CODEC_ID_SUBRIP)
            {
                outStream->codecpar->codec_id = AVCodecID.AV_CODEC_ID_SUBRIP;
            }
            else if (!hasSrt && codecId != AVCodecID.AV_CODEC_ID_WEBVTT)
            {
                outStream->codecpar->codec_id = AVCodecID.AV_CODEC_ID_WEBVTT;
            }
        }
    }

    private static void ClearMetadata(AVFormatContext* outCtx)
    {
        if (outCtx->metadata != null)
        {
            ffmpeg.av_dict_free(&outCtx->metadata);
            outCtx->metadata = null;
        }
    }

    private static void ApplyStreamMetadata(List<IntPtr> streams,
        string[][] langs, string[][] titles)
    {
        int globalIdx = 0;
        for (int i = 0; i < langs.Length && globalIdx < streams.Count; i++)
        {
            for (int j = 0; j < langs[i].Length && globalIdx < streams.Count; j++)
            {
                if (!string.IsNullOrEmpty(langs[i][j]))
                {
                    ffmpeg.av_dict_set(&((AVStream*)streams[globalIdx])->metadata, "language", langs[i][j], 0);
                }
                if (titles != null && i < titles.Length && j < titles[i].Length && !string.IsNullOrEmpty(titles[i][j]))
                {
                    ffmpeg.av_dict_set(&((AVStream*)streams[globalIdx])->metadata, "title", titles[i][j], 0);
                }
                globalIdx++;
            }
        }
    }

    private static void ApplyStreamDispositions(List<IntPtr> outStreams,
        int[][] streamDispositions, AVFormatContext** inputs, int n,
        MediaType?[] mediaTypes)
    {
        if (mediaTypes == null || mediaTypes.Length == 0)
        {
            return;
        }

        var videoStreamIndices = new List<int>();
        var audioStreamIndices = new List<int>();
        var subStreamIndices = new List<int>();

        for (int i = 0; i < outStreams.Count; i++)
        {
            var stream = (AVStream*)outStreams[i];
            AVMediaType codecType = stream->codecpar->codec_type;
            if (codecType == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                videoStreamIndices.Add(i);
            }
            else if (codecType == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                audioStreamIndices.Add(i);
            }
            else if (codecType == AVMediaType.AVMEDIA_TYPE_SUBTITLE)
            {
                subStreamIndices.Add(i);
            }
        }

        if (videoStreamIndices.Count > 0)
        {
            var firstVideo = (AVStream*)outStreams[videoStreamIndices[0]];
            firstVideo->disposition |= ffmpeg.AV_DISPOSITION_DEFAULT;
        }

        foreach (int subIdx in subStreamIndices)
        {
            var subStream = (AVStream*)outStreams[subIdx];
            subStream->disposition &= ~ffmpeg.AV_DISPOSITION_DEFAULT;
        }

        if (audioStreamIndices.Count > 0)
        {
            var firstAudio = (AVStream*)outStreams[audioStreamIndices[0]];
            firstAudio->disposition |= ffmpeg.AV_DISPOSITION_DEFAULT;

            for (int i = 1; i < audioStreamIndices.Count; i++)
            {
                var audioStream = (AVStream*)outStreams[audioStreamIndices[i]];
                audioStream->disposition &= ~ffmpeg.AV_DISPOSITION_DEFAULT;
            }
        }
    }

    private static bool OpenOutputFile(AVFormatContext* outCtx, string outputPath)
    {
        if ((outCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
        {
            int ret = ffmpeg.avio_open(&outCtx->pb, outputPath, ffmpeg.AVIO_FLAG_WRITE);
            if (ret < 0)
            {
                LogFFmpegError("open output", ret);
                return false;
            }
        }
        return true;
    }

    private static void WriteConcatPackets(AVFormatContext** inputs, int n,
        AVFormatContext* outCtx, int[][] mapping, List<IntPtr> outStreams, AVPacket* pkt,
        bool useAACFilter, string muxFormat)
    {
        bool isTsFormat = muxFormat.Equals("TS", StringComparison.OrdinalIgnoreCase);
        bool isMp4Format = muxFormat.Equals("MP4", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < n; i++)
        {
            while (ffmpeg.av_read_frame(inputs[i], pkt) >= 0)
            {
                int si = pkt->stream_index;
                if (si >= mapping[i].Length || mapping[i][si] < 0)
                {
                    ffmpeg.av_packet_unref(pkt);
                    continue;
                }

                int outIdx = mapping[i][si];
                var inStream = inputs[i]->streams[si];
                var outStream = (AVStream*)outStreams[outIdx];

                pkt->stream_index = outIdx;
                pkt->pts = RescaleTimestamp(pkt->pts, inStream, outStream);
                pkt->dts = RescaleTimestamp(pkt->dts, inStream, outStream);
                if (pkt->duration > 0)
                {
                    pkt->duration = ffmpeg.av_rescale_q(pkt->duration, inStream->time_base, outStream->time_base);
                }
                pkt->pos = -1;

                if (useAACFilter && outStream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    ApplyAacAdtsToAscFilter(pkt);
                }

                if (isTsFormat && outStream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    ApplyH264Mp4ToAnnexbFilter(pkt, outStream);
                }

                // 新增：MP4 输出时，将 Annex B NAL 转换为长度前缀格式
                if (isMp4Format &&
                    outStream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO &&
                    (outStream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_H264 ||
                     outStream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_HEVC))
                {
                    ApplyAnnexBToMp4Filter(pkt, outStream);
                }

                int ret = ffmpeg.av_interleaved_write_frame(outCtx, pkt);
                if (ret < 0)
                {
                    Logger.WarnMarkUp($"[grey]Write frame error: {ret}[/]");
                }

                ffmpeg.av_packet_unref(pkt);
            }
        }
    }

    private static void ApplyAacAdtsToAscFilter(AVPacket* pkt)
    {
        // AAC ADTS to ASC bitstream filter: strip ADTS header, write ASC config
        // For raw AAC in ADTS format, we need to convert to raw AAC packets
        // This is equivalent to -bsf:a aac_adtstoasc
        // The filter removes the 7-byte ADTS header if present
        if (pkt->size >= 7)
        {
            byte* data = pkt->data;
            // Check for ADTS sync word (0xFFF)
            if (data[0] == 0xFF && (data[1] & 0xF0) == 0xF0)
            {
                int headerSize = (data[1] & 0x01) != 0 ? 9 : 7;
                if (pkt->size > headerSize)
                {
                    int newSize = pkt->size - headerSize;
                    var filteredPkt = ffmpeg.av_packet_alloc();
                    ffmpeg.av_packet_ref(filteredPkt, pkt);
                    ffmpeg.av_packet_make_writable(filteredPkt);
                    byte* src = pkt->data + headerSize;
                    for (int k = 0; k < newSize; k++)
                    {
                        filteredPkt->data[k] = src[k];
                    }
                    filteredPkt->size = newSize;
                    ffmpeg.av_packet_unref(pkt);
                    ffmpeg.av_packet_ref(pkt, filteredPkt);
                    ffmpeg.av_packet_free(&filteredPkt);
                }
            }
        }
    }

    private static void ApplyH264Mp4ToAnnexbFilter(AVPacket* pkt, AVStream* outStream)
    {
        // H.264 MP4 to Annex B bitstream filter: convert MP4 NAL units to Annex B format
        // This is equivalent to -bsf:v h264_mp4toannexb
        // For TS output format, H.264 needs to be in Annex B format
        // Read extradata to get SPS/PPS, then prepend start codes
        if (outStream->codecpar->extradata_size > 0 && pkt->size > 0)
        {
            byte* extraData = outStream->codecpar->extradata;
            int extraSize = outStream->codecpar->extradata_size;

            // Check if this is AVCC format (starts with 0x01 for configuration version)
            if (extraData[0] == 0x01 && extraSize >= 8)
            {
                int spsOffset = 6;
                if (spsOffset < extraSize)
                {
                    int numSps = extraData[spsOffset] & 0x1F;
                    spsOffset++;
                    var annexB = new List<byte>();
                    byte[] startCode = { 0x00, 0x00, 0x00, 0x01 };

                    int pos = spsOffset;
                    for (int i = 0; i < numSps && pos + 2 <= extraSize; i++)
                    {
                        int nalLen = extraData[pos] << 8 | extraData[pos + 1];
                        pos += 2;
                        if (pos + nalLen > extraSize) break;
                        annexB.AddRange(startCode);
                        for (int k = 0; k < nalLen; k++)
                        {
                            annexB.Add(extraData[pos + k]);
                        }
                        pos += nalLen;
                    }

                    if (pos + 1 <= extraSize)
                    {
                        int numPps = extraData[pos];
                        pos++;
                        for (int i = 0; i < numPps && pos + 2 <= extraSize; i++)
                        {
                            int nalLen = extraData[pos] << 8 | extraData[pos + 1];
                            pos += 2;
                            if (pos + nalLen > extraSize) break;
                            annexB.AddRange(startCode);
                            for (int k = 0; k < nalLen; k++)
                            {
                                annexB.Add(extraData[pos + k]);
                            }
                            pos += nalLen;
                        }
                    }

                    // Convert NAL units in the packet from length-prefixed to start-code-prefixed
                    ConvertNalUnitsToAnnexB(pkt, annexB);
                }
            }
        }
    }

    private static void ConvertNalUnitsToAnnexB(AVPacket* pkt, List<byte> prefixData)
    {
        // Convert MP4-style length-prefixed NAL units to Annex B start-code-prefixed
        int srcLen = pkt->size;
        if (srcLen < 4) return;

        byte[] startCode = { 0x00, 0x00, 0x00, 0x01 };
        var result = new List<byte>();
        result.AddRange(prefixData);

        int offset = 0;
        byte* data = pkt->data;
        while (offset + 4 <= srcLen)
        {
            int nalLen = data[offset] << 24 |
                         data[offset + 1] << 16 |
                         data[offset + 2] << 8 |
                         data[offset + 3];
            if (nalLen <= 0 || offset + 4 + nalLen > srcLen) break;

            result.AddRange(startCode);
            for (int k = 0; k < nalLen; k++)
            {
                result.Add(data[offset + 4 + k]);
            }

            offset += 4 + nalLen;
        }

        if (result.Count > 0)
        {
            ffmpeg.av_packet_make_writable(pkt);
            fixed (byte* pResult = result.ToArray())
            {
                for (int k = 0; k < result.Count && k < pkt->size; k++)
                {
                    pkt->data[k] = pResult[k];
                }
            }
            pkt->size = result.Count;
        }
    }

    private static void WriteMuxPackets(AVFormatContext** inputs, int n,
        AVFormatContext* outCtx, List<IntPtr> outStreams,
        List<(int fileIdx, int streamIdx)> sourceMap, AVPacket* pkt)
    {
        for (int i = 0; i < n; i++)
        {
            while (ffmpeg.av_read_frame(inputs[i], pkt) >= 0)
            {
                int inStreamIdx = pkt->stream_index;
                int outIdx = -1;

                for (int k = 0; k < sourceMap.Count; k++)
                {
                    if (sourceMap[k].fileIdx == i && sourceMap[k].streamIdx == inStreamIdx)
                    {
                        outIdx = k;
                        break;
                    }
                }

                if (outIdx < 0)
                {
                    ffmpeg.av_packet_unref(pkt);
                    continue;
                }

                var inStream = inputs[i]->streams[inStreamIdx];
                var outStream = (AVStream*)outStreams[outIdx];

                pkt->stream_index = outIdx;
                pkt->pts = RescaleTimestamp(pkt->pts, inStream, outStream);
                pkt->dts = RescaleTimestamp(pkt->dts, inStream, outStream);
                if (pkt->duration > 0)
                {
                    pkt->duration = ffmpeg.av_rescale_q(pkt->duration, inStream->time_base, outStream->time_base);
                }
                pkt->pos = -1;

                ffmpeg.av_interleaved_write_frame(outCtx, pkt);
                ffmpeg.av_packet_unref(pkt);
            }
        }
    }

    private static long RescaleTimestamp(long ts, AVStream* inStream, AVStream* outStream)
    {
        if (ts == ffmpeg.AV_NOPTS_VALUE) return ts;
        return ffmpeg.av_rescale_q_rnd(ts, inStream->time_base, outStream->time_base,
            AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
    }

    private static void CleanupResources(AVFormatContext** inputs, int n,
        AVFormatContext* outCtx, AVPacket* pkt)
    {
        if (pkt != null)
        {
            ffmpeg.av_packet_free(&pkt);
        }

        if (inputs != null)
        {
            for (int i = 0; i < n; i++)
            {
                if (inputs[i] != null)
                {
                    ffmpeg.avformat_close_input(&inputs[i]);
                }
            }
            ffmpeg.av_free(inputs);
        }

        if (outCtx != null)
        {
            if ((outCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0 && outCtx->pb != null)
            {
                ffmpeg.avio_closep(&outCtx->pb);
            }
            ffmpeg.avformat_free_context(outCtx);
        }
    }

    private static void EnsureStreamCompatibility(AVFormatContext* outCtx,
        List<IntPtr> outStreams, string muxFormat)
    {
        bool isMp4 = muxFormat.Equals("MP4", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < outStreams.Count; i++)
        {
            var stream = (AVStream*)outStreams[i];
            var codecPar = stream->codecpar;

            if (isMp4)
            {
                if (codecPar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO &&
                    codecPar->codec_id == AVCodecID.AV_CODEC_ID_AAC &&
                    (codecPar->extradata == null || codecPar->extradata_size == 0))
                {
                    GenerateAacExtradata(codecPar);
                }


                if (codecPar->codec_type == AVMediaType.AVMEDIA_TYPE_SUBTITLE &&
                    codecPar->codec_id != AVCodecID.AV_CODEC_ID_MOV_TEXT)
                {
                    if (codecPar->extradata != null)
                    {
                        ffmpeg.av_free(codecPar->extradata);
                        codecPar->extradata = null;
                        codecPar->extradata_size = 0;
                    }
                    codecPar->codec_id = AVCodecID.AV_CODEC_ID_MOV_TEXT;
                }
            }
        }
    }

    private static void GenerateAacExtradata(AVCodecParameters* codecPar)
    {
        int audioObjectType;
        int coreSampleRate = codecPar->sample_rate;
        int nbChannels = codecPar->ch_layout.nb_channels;
        bool isHeAac = false;
        int sbrFreqIndex = 0;

        switch (codecPar->profile)
        {
            case 0: audioObjectType = 1; break; // Main

            case 1: audioObjectType = 2; break; // LC

            case 2: audioObjectType = 3; break; // SSR

            case 3: audioObjectType = 4; break; // LTP

            case 4: // HE-AAC (SBR)
            case 5: // HE-AACv2 (SBR+PS)
                audioObjectType = 2;
                isHeAac = true;
                coreSampleRate /= 2;
                sbrFreqIndex = GetAacSamplingFrequencyIndex(codecPar->sample_rate);
                break;

            case 7: audioObjectType = 23; break; // ELD

            default: audioObjectType = 2; break; // 默认 LC
        }

        int freqIndex = GetAacSamplingFrequencyIndex(coreSampleRate);
        int channelConfig = isHeAac && codecPar->profile == 5
            ? 1
            : Math.Max(1, Math.Min(nbChannels, 7));

        if (isHeAac)
        {
            byte[] config = new byte[5];
            config[0] = (byte)(audioObjectType << 3 | freqIndex >> 1);
            config[1] = (byte)((freqIndex & 1) << 7 | channelConfig << 3 | 0x01);
            config[2] = 0x56;
            config[3] = 0xE5;
            config[4] = (byte)(0x80 | sbrFreqIndex << 3 | (codecPar->profile == 5 ? 0x04 : 0x00));
            SetCodecExtradata(codecPar, config);
        }
        else
        {
            byte[] config = new byte[2];
            config[0] = (byte)(audioObjectType << 3 | freqIndex >> 1);
            config[1] = (byte)((freqIndex & 1) << 7 | channelConfig << 3);
            SetCodecExtradata(codecPar, config);
        }
    }

    private static void SetCodecExtradata(AVCodecParameters* codecPar, byte[] data)
    {
        if (codecPar->extradata != null)
        {
            ffmpeg.av_free(codecPar->extradata);
            codecPar->extradata = null;
            codecPar->extradata_size = 0;
        }

        int size = data.Length;
        codecPar->extradata = (byte*)ffmpeg.av_mallocz((ulong)(size + 64));
        if (codecPar->extradata != null)
        {
            Marshal.Copy(data, 0, (IntPtr)codecPar->extradata, size);
            codecPar->extradata_size = size;
        }
    }

    private static int GetAacSamplingFrequencyIndex(int sampleRate)
    {
        int[] rates = { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350 };
        for (int i = 0; i < rates.Length; i++)
        {
            if (sampleRate == rates[i]) return i;
        }
        return 3; // 默认 48000
    }


    private static string MapOutputFormat(string muxFormat)
    {
        return muxFormat.ToUpper() switch
        {
            "MP4" => "mp4",
            "MKV" => "matroska",
            "FLV" => "flv",
            "TS" => "mpegts",
            "M4A" => "mp4",
            "EAC3" => "eac3",
            "AAC" => "mp4",
            "AC3" => "ac3",
            _ => muxFormat.ToLower()
        };
    }

    private static void LogFFmpegError(string operation, int errorCode)
    {
        byte[] buf = new byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        fixed (byte* pBuf = buf)
        {
            ffmpeg.av_strerror(errorCode, pBuf, (ulong)buf.Length);
        }
        string msg = Marshal.PtrToStringAnsi(Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0)) ?? $"code {errorCode}";
        Logger.WarnMarkUp($"[grey]FFmpeg {operation} failed: {msg.EscapeMarkup()}[/]");
    }

    private static void EnsureVideoExtradata(AVFormatContext** inputs, int n,
        List<IntPtr> outStreams)
    {
        for (int i = 0; i < outStreams.Count; i++)
        {
            var stream = (AVStream*)outStreams[i];
            var codecPar = stream->codecpar;
            if (codecPar->codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO ||
                codecPar->codec_id != AVCodecID.AV_CODEC_ID_H264 &&
                codecPar->codec_id != AVCodecID.AV_CODEC_ID_HEVC ||
                codecPar->extradata != null && codecPar->extradata_size > 0)
            {
                continue;
            }

            bool isHevc = codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC;
            var spsList = new List<byte[]>();
            var ppsList = new List<byte[]>();
            var vpsList = new List<byte[]>();
            bool found = false;

            for (int fi = 0; fi < n && !found; fi++)
            {
                var tmpPkt = ffmpeg.av_packet_alloc();
                while (ffmpeg.av_read_frame(inputs[fi], tmpPkt) >= 0)
                {
                    int si = tmpPkt->stream_index;
                    var inStream = inputs[fi]->streams[si];
                    if (inStream->codecpar->codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        ffmpeg.av_packet_unref(tmpPkt);
                        continue;
                    }

                    ExtractNalUnitsFromPacket(tmpPkt, isHevc, spsList, ppsList, vpsList);

                    if (!isHevc && spsList.Count > 0 && ppsList.Count > 0)
                    {
                        found = true;
                    }
                    else if (isHevc && vpsList.Count > 0 && spsList.Count > 0 && ppsList.Count > 0)
                    {
                        found = true;
                    }

                    ffmpeg.av_packet_unref(tmpPkt);
                    if (found) break;
                }

                ffmpeg.av_seek_frame(inputs[fi], -1, 0, ffmpeg.AVSEEK_FLAG_BACKWARD);
                ffmpeg.av_packet_free(&tmpPkt);
            }

            if (!found) continue;

            if (!isHevc && spsList.Count > 0 && ppsList.Count > 0)
            {
                byte[] sps = spsList[0];
                byte[] pps = ppsList[0];
                byte[] avcc = new byte[8 + 3 + sps.Length + 1 + 3 + pps.Length];
                int idx = 0;
                avcc[idx++] = 1;
                avcc[idx++] = sps[1];
                avcc[idx++] = sps[2];
                avcc[idx++] = sps[3];
                avcc[idx++] = 0xFF;
                avcc[idx++] = 0xE1;
                avcc[idx++] = (byte)(sps.Length >> 8);
                avcc[idx++] = (byte)(sps.Length & 0xFF);
                Array.Copy(sps, 0, avcc, idx, sps.Length);
                idx += sps.Length;
                avcc[idx++] = 1;
                avcc[idx++] = (byte)(pps.Length >> 8);
                avcc[idx++] = (byte)(pps.Length & 0xFF);
                Array.Copy(pps, 0, avcc, idx, pps.Length);
                idx += pps.Length;
                SetCodecExtradata(codecPar, avcc[..idx]);
            }
            else if (isHevc && vpsList.Count > 0 && spsList.Count > 0 && ppsList.Count > 0)
            {
                BuildHevcExtradata(codecPar, vpsList, spsList, ppsList);
            }
        }
    }

    private static void ExtractNalUnitsFromPacket(AVPacket* pkt, bool isHevc,
        List<byte[]> spsList, List<byte[]> ppsList, List<byte[]> vpsList)
    {
        byte* data = pkt->data;
        int srcLen = pkt->size;
        int offset = 0;

        while (offset < srcLen)
        {
            int scLen = 0;
            if (offset + 3 <= srcLen && data[offset] == 0 && data[offset + 1] == 0 && data[offset + 2] == 1)
            {
                scLen = 3;
            }
            else if (offset + 4 <= srcLen && data[offset] == 0 && data[offset + 1] == 0 && data[offset + 2] == 0 && data[offset + 3] == 1)
            {
                scLen = 4;
            }
            else
            {
                offset++;
                continue;
            }

            int nalStart = offset + scLen;
            int nalEnd = srcLen;

            for (int k = nalStart + 1; k + 2 < srcLen; k++)
            {
                if (data[k] == 0 && data[k + 1] == 0 && data[k + 2] == 1)
                {
                    if (k > 0 && data[k - 1] == 0)
                    {
                        nalEnd = k - 1;
                    }
                    else
                    {
                        nalEnd = k;
                    }
                    break;
                }
            }

            int nalSize = nalEnd - nalStart;
            if (nalSize > 0)
            {
                byte[] nal = new byte[nalSize];
                for (int k = 0; k < nalSize; k++)
                {
                    nal[k] = data[nalStart + k];
                }

                if (!isHevc)
                {
                    int nalType = nal[0] & 0x1F;
                    if (nalType == 7)
                    {
                        spsList.Add(nal);
                    }
                    else if (nalType == 8) ppsList.Add(nal);
                }
                else
                {
                    int nalType = nal[0] >> 1 & 0x3F;
                    if (nalType == 32)
                    {
                        vpsList.Add(nal);
                    }
                    else if (nalType == 33)
                    {
                        spsList.Add(nal);
                    }
                    else if (nalType == 34) ppsList.Add(nal);
                }
            }

            offset = nalEnd;
        }
    }

    private static void BuildHevcExtradata(AVCodecParameters* codecPar,
        List<byte[]> vpsList, List<byte[]> spsList, List<byte[]> ppsList)
    {
        byte[] vps = vpsList[0];
        byte[] sps = spsList[0];
        byte[] pps = ppsList[0];

        int totalSize = 23 + 5 + vps.Length + 5 + sps.Length + 5 + pps.Length;
        byte[] hvcc = new byte[totalSize];
        int idx = 0;

        hvcc[idx++] = 1;
        hvcc[idx++] = (byte)(1 << 6 | sps[1] >> 1 & 0x3F);
        hvcc[idx++] = sps[1];
        hvcc[idx++] = sps[2];
        hvcc[idx++] = sps[3];
        hvcc[idx++] = 0xFF;
        hvcc[idx++] = 0x80;
        hvcc[idx++] = 0x80;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0xFC;
        hvcc[idx++] = 0xF8;
        hvcc[idx++] = 0x80;
        hvcc[idx++] = 0x80;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 0;
        hvcc[idx++] = 3;

        hvcc[idx++] = 0x20;
        hvcc[idx++] = 1;
        hvcc[idx++] = (byte)(vps.Length >> 8);
        hvcc[idx++] = (byte)(vps.Length & 0xFF);
        Array.Copy(vps, 0, hvcc, idx, vps.Length);
        idx += vps.Length;

        hvcc[idx++] = 0x21;
        hvcc[idx++] = 1;
        hvcc[idx++] = (byte)(sps.Length >> 8);
        hvcc[idx++] = (byte)(sps.Length & 0xFF);
        Array.Copy(sps, 0, hvcc, idx, sps.Length);
        idx += sps.Length;

        hvcc[idx++] = 0x22;
        hvcc[idx++] = 1;
        hvcc[idx++] = (byte)(pps.Length >> 8);
        hvcc[idx++] = (byte)(pps.Length & 0xFF);
        Array.Copy(pps, 0, hvcc, idx, pps.Length);
        idx += pps.Length;

        SetCodecExtradata(codecPar, hvcc[..idx]);
    }

    private static void ApplyAnnexBToMp4Filter(AVPacket* pkt, AVStream* outStream)
    {
        if (pkt->size < 4) return;

        var codecPar = outStream->codecpar;
        bool isHevc = codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC;

        byte[] startCode3 = { 0x00, 0x00, 0x01 };
        byte[] startCode4 = { 0x00, 0x00, 0x00, 0x01 };

        var naluList = new List<byte[]>();
        byte* data = pkt->data;
        int srcLen = pkt->size;
        int offset = 0;

        while (offset < srcLen)
        {
            int scLen = 0;
            if (offset + 3 <= srcLen && data[offset] == 0 && data[offset + 1] == 0 && data[offset + 2] == 1)
            {
                scLen = 3;
            }
            else if (offset + 4 <= srcLen && data[offset] == 0 && data[offset + 1] == 0 && data[offset + 2] == 0 && data[offset + 3] == 1)
            {
                scLen = 4;
            }
            else
            {
                offset++;
                continue;
            }

            int nalStart = offset + scLen;
            int nalEnd = srcLen;

            for (int k = nalStart + 1; k + 2 < srcLen; k++)
            {
                if (data[k] == 0 && data[k + 1] == 0 && data[k + 2] == 1)
                {
                    if (k > 0 && data[k - 1] == 0)
                    {
                        nalEnd = k - 1;
                    }
                    else
                    {
                        nalEnd = k;
                    }
                    break;
                }
            }

            int nalSize = nalEnd - nalStart;
            if (nalSize > 0)
            {
                byte[] nal = new byte[nalSize];
                for (int k = 0; k < nalSize; k++)
                {
                    nal[k] = data[nalStart + k];
                }
                naluList.Add(nal);
            }

            offset = nalEnd;
        }

        if (naluList.Count == 0) return;

        // 如果 extradata 还没有，从 SPS/PPS 构建 AVCC extradata
        if (codecPar->extradata == null || codecPar->extradata_size == 0)
        {
            if (!isHevc)
            {
                var spsNals = naluList.Where(n => (n[0] & 0x1F) == 7).ToList();
                var ppsNals = naluList.Where(n => (n[0] & 0x1F) == 8).ToList();
                if (spsNals.Count > 0 && ppsNals.Count > 0)
                {
                    byte[] sps = spsNals[0];
                    byte[] pps = ppsNals[0];
                    byte[] avcc = new byte[8 + 3 + sps.Length + 1 + 3 + pps.Length];
                    int idx = 0;
                    avcc[idx++] = 1;
                    avcc[idx++] = sps[1];
                    avcc[idx++] = sps[2];
                    avcc[idx++] = sps[3];
                    avcc[idx++] = 0xFF;
                    avcc[idx++] = 0xE1;
                    avcc[idx++] = (byte)(sps.Length >> 8);
                    avcc[idx++] = (byte)(sps.Length & 0xFF);
                    Array.Copy(sps, 0, avcc, idx, sps.Length);
                    idx += sps.Length;
                    avcc[idx++] = 1;
                    avcc[idx++] = (byte)(pps.Length >> 8);
                    avcc[idx++] = (byte)(pps.Length & 0xFF);
                    Array.Copy(pps, 0, avcc, idx, pps.Length);
                    idx += pps.Length;
                    SetCodecExtradata(codecPar, avcc[..idx]);
                }
            }
        }

        // 将 Annex B NAL 转换为长度前缀格式，跳过 SPS/PPS/AUD
        var result = new List<byte>();
        foreach (byte[] nal in naluList)
        {
            if (!isHevc)
            {
                int nalType = nal[0] & 0x1F;
                if (nalType == 7 || nalType == 8 || nalType == 9)
                {
                    continue;
                }
            }
            else
            {
                int nalType = nal[0] >> 1 & 0x3F;
                if (nalType == 32 || nalType == 33 || nalType == 34 || nalType == 39)
                {
                    continue;
                }
            }

            byte[] len = BitConverter.GetBytes(nal.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(len);
            result.AddRange(len);
            result.AddRange(nal);
        }

        if (result.Count > 0)
        {
            ffmpeg.av_packet_make_writable(pkt);
            fixed (byte* pResult = result.ToArray())
            {
                int copyLen = Math.Min(result.Count, pkt->size);
                for (int k = 0; k < copyLen; k++)
                {
                    pkt->data[k] = pResult[k];
                }
            }
            pkt->size = result.Count;
        }
    }
}