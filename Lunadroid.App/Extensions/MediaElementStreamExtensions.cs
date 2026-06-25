#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
#elif ANDROID
using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Source;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using System.Runtime.CompilerServices;
using Object = Java.Lang.Object;

#elif IOS || MACCATALYST
using AVFoundation;
using Foundation;
using CoreFoundation;
#endif

namespace Lunadroid.App.Extensions;

/// <summary>
///     全平台通用
///     提供了 MediaElement 类的扩展方法，用于将流设置为 MediaElement 的源。
/// </summary>
public static class MediaElementStreamExtensions
{
    /// <summary>
    ///     Sets a Stream as the Source of a MediaElement.
    /// </summary>
    /// <param name="mediaElement">The MediaElement whose Source will be changed to a Stream.</param>
    /// <param name="stream">A seekable data stream that will be set as the source of the MediaElement.</param>
    /// <param name="contentType">The MIME type of the content of the stream.</param>
    /// <param name="throwIfNotSeekable">If true, an exception will be thrown if the stream is not seekable.</param>
    /// <param name="maxMemoryStreamBufferSize">
    ///     The maximum size in bytes of the buffer to use when copying a non-seekable
    ///     stream to a temporary seekable stream. Default is 1 MB.
    /// </param>
    /// <param name="cancellationToken">A CancellationToken to cancel the operation.</param>
    /// <returns></returns>
    /// <exception cref="Exception">The Stream is not seekable</exception>
    /// <exception cref="NullReferenceException">A required element is null.</exception>
    public static async ValueTask SetStreamMediaSource(this MediaElement? mediaElement, Stream? stream,
#if IOS || MACCATALYST
            string contentType = "public.mpeg-4",
#else
        string contentType = "video/mp4",
#endif
        bool throwIfNotSeekable = false, int maxMemoryStreamBufferSize = 1_048_576, CancellationToken cancellationToken = default)
    {
        if (mediaElement is null || stream is null)
        {
            throw new NullReferenceException();
        }

        if (!stream.CanSeek)
        {
            if (throwIfNotSeekable)
            {
                throw new Exception("The provided stream is not seekable.");
            }

            byte[] buffer = GC.AllocateUninitializedArray<byte>(maxMemoryStreamBufferSize);

            int result = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, cancellationToken);

            if (result < buffer.Length)
            {
                // The stream is small enough to fit in memory
                var memoryStream = new MemoryStream(buffer, 0, result, false, true);
                stream = memoryStream;
            }
            else
            {
                string tempPath = Path.GetTempFileName();

                var fileOptions = new FileStreamOptions
                {
                    Access = FileAccess.ReadWrite,
                    Mode = FileMode.Create,
                    Share = FileShare.None,
                    Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous
                };
                var tmpStream = new FileStream(tempPath, fileOptions);
                await tmpStream.WriteAsync(buffer, cancellationToken);
                await stream.CopyToAsync(tmpStream, cancellationToken);
                stream = tmpStream;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

#if WINDOWS
            if (mediaElement.Handler is MediaElementHandler mediaElementHandler)
            {
                var mediaManager = GetMediaManager(mediaElementHandler);
                if (mediaManager is null)
                    throw new NullReferenceException(nameof(mediaManager));
                
                var nativePlayer = GetPlayer(mediaManager);
                if (nativePlayer is null)
                    throw new NullReferenceException(nameof(nativePlayer));

                await GetDispatcher(mediaManager).DispatchAsync(() => nativePlayer.PosterSource = new BitmapImage());
                var iMediaElement = GetMediaElement(mediaManager);
                SetPosition(iMediaElement, TimeSpan.Zero);
                SetDuration(iMediaElement, TimeSpan.Zero);
                nativePlayer.AutoPlay = iMediaElement.ShouldAutoPlay;
                nativePlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream.AsRandomAccessStream(), contentType);
            }
#elif ANDROID
        if (mediaElement.Handler is MediaElementHandler mediaElementHandler)
        {
            MediaManager? mediaManager = GetMediaManager(mediaElementHandler);
            if (mediaManager is null)
            {
                throw new NullReferenceException(nameof(mediaManager));
            }

            IExoPlayer? nativePlayer = GetPlayer(mediaManager);
            if (nativePlayer is null)
            {
                throw new NullReferenceException(nameof(nativePlayer));
            }

#if false
                if (GetConnection(mediaManager) is null)
                {
                    StartService(mediaManager);
                }
#endif

            IMediaElement iMediaElement = GetMediaElement(mediaManager);
            CurrentStateChanged(iMediaElement, MediaElementState.Opening);
            nativePlayer.PlayWhenReady = iMediaElement.ShouldAutoPlay;
            GetCancellationTokenSource(mediaManager) ??= new CancellationTokenSource();
            MediaItem.Builder builder = await CreateMediaItem(mediaManager, "stream://asset", GetCancellationTokenSource(mediaManager).Token).ConfigureAwait(false);
            builder.SetMimeType(contentType);
            MediaItem? item = builder.Build();
            if (item?.MediaMetadata is not null)
            {
                var dataSourceFactory = new StreamDataSourceFactory(stream);
                var mediaSourceFactory = new ProgressiveMediaSource.Factory(dataSourceFactory);

                nativePlayer.SetMediaSource(mediaSourceFactory.CreateMediaSource(item));
                nativePlayer.Prepare();

                if (nativePlayer.PlayerError is null)
                {
                    iMediaElement.MediaOpened();
                    UpdateNotifications(mediaManager);
                }
            }

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "cancellationTokenSource")]
            static extern ref CancellationTokenSource GetCancellationTokenSource(MediaManager mauiMediaElement);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CreateMediaItem")]
            static extern Task<MediaItem.Builder> CreateMediaItem(MediaManager mauiMediaElement, string url, CancellationToken cancellationToken = default);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdateNotifications")]
            static extern void UpdateNotifications(MediaManager mauiMediaElement);

#if false
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "connection")]
                [return: UnsafeAccessorType("CommunityToolkit.Maui.Services.BoundServiceConnection, CommunityToolkit.Maui.MediaElement")]
                extern static ref object GetConnection(MediaManager mauiMediaElement);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "StartService")]
                extern static void StartService(MediaManager mauiMediaElement);
#endif
        }
#elif IOS || MACCATALYST
            if (mediaElement.Handler is MediaElementHandler mediaElementHandler)
            {
                var mediaManager = GetMediaManager(mediaElementHandler);
                if (mediaManager is null)
                    throw new NullReferenceException(nameof(mediaManager));

                var nativePlayer = GetPlayer(mediaManager);
                if (nativePlayer is null)
                    throw new NullReferenceException(nameof(nativePlayer));

                mediaElement.Source = null;
                var iMediaElement = GetMediaElement(mediaManager);
                CurrentStateChanged(iMediaElement, MediaElementState.Opening);

                var url = new NSUrl("stream://asset");
                var asset = new AVUrlAsset(url);
                var loader = new StreamLoader(stream, contentType);
                asset.ResourceLoader.SetDelegate(loader, DispatchQueue.DefaultGlobalQueue);
                var item = new AVPlayerItem(asset);

                nativePlayer.ReplaceCurrentItemWithPlayerItem(item);

                if (item.Error is null)
                {
                    iMediaElement.MediaOpened();
                    (int Width, int Height) dimensions = GetVideoDimensions(mediaManager, item);
                    SetMediaWidth(iMediaElement, dimensions.Width);
                    SetMediaHeight(iMediaElement, dimensions.Height);
                    if (iMediaElement.ShouldAutoPlay)
                        nativePlayer.Play();
                }

                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetVideoDimensions")]
                extern static (int Width, int Height) GetVideoDimensions(MediaManager mediaManager, AVPlayerItem avPlayerItem);
            }
#else
                throw new PlatformNotSupportedException();
#endif

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MediaManager")]
        static extern MediaManager? GetMediaManager(MediaElementHandler mediaElementHandler);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Dispatcher")]
        static extern IDispatcher GetDispatcher(MediaManager mediaManager);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MediaElement")]
        static extern IMediaElement GetMediaElement(MediaManager mediaManager);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Position")]
        static extern void SetPosition(IMediaElement mediaElement, TimeSpan position);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Duration")]
        static extern void SetDuration(IMediaElement mediaElement, TimeSpan duration);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MediaWidth")]
        static extern void SetMediaWidth(IMediaElement mediaElement, int mediaWidth);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MediaHeight")]
        static extern void SetMediaHeight(IMediaElement mediaElement, int mediaHeight);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CurrentStateChanged")]
        static extern void CurrentStateChanged(IMediaElement mediaElement, MediaElementState duration);

#if WINDOWS || ANDROID || IOS || MACCATALYST
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Player")]
#endif
#if WINDOWS
            extern static Microsoft.UI.Xaml.Controls.MediaPlayerElement? GetPlayer(MediaManager mediaManager);
#elif ANDROID
        static extern IExoPlayer? GetPlayer(MediaManager mediaManager);
#elif IOS || MACCATALYST
            extern static AVFoundation.AVPlayer? GetPlayer(MediaManager mediaManager);
#endif
    }

#if ANDROID
    private sealed class StreamDataSource(Stream s) : BaseDataSource(false)
    {
        private readonly Lock _lockObject = new();
        private readonly Stream _s = s;
        private long _remaining;
        private Uri _uri = new("stream://asset");

        public override Android.Net.Uri? Uri => Android.Net.Uri.Parse(_uri.ToString());

        public override long Open(DataSpec? dataSpec)
        {
            if (dataSpec is null)
            {
                return C.LengthUnset;
            }

            if (dataSpec.Uri is not null)
            {
                _uri = new Uri(dataSpec.Uri.ToString()!);
            }

            lock (_lockObject)
            {
                _s.Position = dataSpec.Position;
                _remaining = dataSpec.Length == C.LengthUnset
                    ? _s.Length - _s.Position
                    : dataSpec.Length;
            }
            return _remaining;
        }

        public override int Read(byte[]? buffer, int offset, int readLength)
        {
            if (buffer is null)
            {
                return 0;
            }

            lock (_lockObject)
            {
                if (_remaining == 0)
                {
                    return C.ResultEndOfInput;
                }

                int toRead = (int)Math.Min(readLength, _remaining);
                int n = _s.Read(buffer, offset, toRead);
                if (n <= 0)
                {
                    return C.ResultEndOfInput;
                }

                _remaining -= n;
                return n;
            }
        }

        public override void Close() { }
    }

    private sealed class StreamDataSourceFactory(Stream stream) : Object, IDataSourceFactory
    {
        private readonly Stream _stream = stream;

        public IDataSource? CreateDataSource()
        {
            return new StreamDataSource(_stream);
        }
    }
#endif

#if IOS || MACCATALYST
        sealed class StreamLoader(Stream stream, string type) : AVAssetResourceLoaderDelegate
        {
            const int StackAllocMax = 256;

            readonly Stream _stream = stream;
            readonly string _type = type;
            readonly Lock _lockObject = new();

            public override bool ShouldWaitForLoadingOfRequestedResource(AVAssetResourceLoader resourceLoader, AVAssetResourceLoadingRequest loadingRequest)
            {
                var info = loadingRequest.ContentInformationRequest;
                if (info is not null)
                {
                    info.ContentType = _type;
                    info.ContentLength = _stream.Length;
                    info.ByteRangeAccessSupported = true;
                }

                var dataRequest = loadingRequest.DataRequest;
                if (dataRequest is not null)
                {
                    lock (_lockObject)
                    {
                        if (dataRequest.RequestsAllDataToEndOfResource)
                        {
                            var response = NSData.FromStream(_stream);
                            if (response is not null)
                                dataRequest.Respond(response);
                        }
                        else
                        {
                            _stream.Position = dataRequest.RequestedOffset;
                            unsafe
                            {
                                byte* ptr = stackalloc byte[StackAllocMax];
                                nint requestedLength = dataRequest.RequestedLength;

                                try
                                {
                                    
                                    if (requestedLength > StackAllocMax)
                                    {
                                        ptr = (byte*)NativeMemory.Alloc((nuint)requestedLength);
                                    }
                                    Span<byte> bytes = new(ptr, (int)requestedLength);
                                    int readResult = _stream.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
                                    NSData responseNSData = NSData.FromBytes((nint)ptr, (nuint)readResult);
                                    dataRequest.Respond(responseNSData);
                                }
                                finally
                                {
                                    if (requestedLength > StackAllocMax) NativeMemory.Free(ptr);
                                }
                            }
                        }
                    }
                }
                loadingRequest.FinishLoading();
                return true;
            }
        }
#endif
}