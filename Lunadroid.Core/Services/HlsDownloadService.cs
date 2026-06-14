namespace Lunadroid.Core.Services;

public class HlsDownloadService
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cancellationTokenSource;

    public Action<double>? ProgressChanged;
    public Action<string>? StatusChanged;

    public HlsDownloadService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> DownloadHlsAsync(string hlsUrl, string outputDir, string fileName, CancellationToken externalToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = _cancellationTokenSource.Token;

        try
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            StatusChanged?.Invoke("Fetching playlist...");

            var playlistContent = await _httpClient.GetStringAsync(hlsUrl, token);

            var segmentUrls = ParsePlaylist(playlistContent, hlsUrl);

            if (segmentUrls.Count == 0)
                throw new InvalidOperationException("No segments found in the HLS playlist.");

            StatusChanged?.Invoke($"Found {segmentUrls.Count} segment(s). Starting download...");

            var tempDir = Path.Combine(outputDir, $".hls_temp_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var segmentFiles = new List<string>();
                var totalSegments = segmentUrls.Count;
                var downloadedCount = 0;

                foreach (var segmentUrl in segmentUrls)
                {
                    token.ThrowIfCancellationRequested();

                    downloadedCount++;
                    StatusChanged?.Invoke($"Downloading segment {downloadedCount}/{totalSegments}...");

                    var segmentPath = Path.Combine(tempDir, $"segment_{downloadedCount:D6}.ts");
                    var segmentData = await _httpClient.GetByteArrayAsync(segmentUrl, token);
                    await File.WriteAllBytesAsync(segmentPath, segmentData, token);
                    segmentFiles.Add(segmentPath);

                    var progress = (double)downloadedCount / totalSegments * 100.0;
                    ProgressChanged?.Invoke(progress);
                }

                StatusChanged?.Invoke("Merging segments...");

                var outputPath = Path.Combine(outputDir, fileName);
                using (var outputStream = File.Create(outputPath))
                {
                    foreach (var segmentFile in segmentFiles)
                    {
                        token.ThrowIfCancellationRequested();
                        var segmentData = await File.ReadAllBytesAsync(segmentFile, token);
                        await outputStream.WriteAsync(segmentData, token);
                    }
                }

                StatusChanged?.Invoke("Download completed.");
                ProgressChanged?.Invoke(100.0);

                return outputPath;
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Best-effort cleanup
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke("Download cancelled.");
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            StatusChanged?.Invoke($"Download failed: {ex.Message}");
            throw;
        }
    }

    private List<string> ParsePlaylist(string content, string baseUrl)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var segmentUrls = new List<string>();

        var baseUri = new Uri(baseUrl);

        // Check if this is a master playlist (contains #EXT-X-STREAM-INF)
        bool isMasterPlaylist = content.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase);

        if (isMasterPlaylist)
        {
            // Parse master playlist: find the first media playlist URL
            string? mediaPlaylistUrl = null;
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                    continue;

                // This line is a URL (media playlist)
                mediaPlaylistUrl = ResolveUrl(line, baseUri);
                break;
            }

            if (mediaPlaylistUrl != null)
            {
                // Fetch and parse the media playlist
                var mediaContent = _httpClient.GetStringAsync(mediaPlaylistUrl).GetAwaiter().GetResult();
                return ParsePlaylist(mediaContent, mediaPlaylistUrl);
            }
        }

        // Parse media playlist: extract segment URLs
        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
            {
                // Check for #EXTINF lines - the next non-comment line is a segment URL
                continue;
            }

            // Non-comment, non-empty line is a segment URL
            segmentUrls.Add(ResolveUrl(line, baseUri));
        }

        return segmentUrls;
    }

    private static string ResolveUrl(string url, Uri baseUri)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        return new Uri(baseUri, url).ToString();
    }

    public void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
        StatusChanged?.Invoke("Cancellation requested...");
    }
}
