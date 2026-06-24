﻿using Android.Content.PM;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using UraniumUI.Icons.MaterialSymbols;

namespace Lunadroid.App.Controls;

public partial class VideoPlayer : ContentView
{
    private static readonly FontImageSource PlayIcon = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Play_arrow, Size = 22, Color = Colors.White };

    private static readonly FontImageSource PauseIcon = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Pause, Size = 22, Color = Colors.White };

    private static readonly FontImageSource PlayIconLarge = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Play_arrow, Size = 28, Color = Colors.White };

    private static readonly FontImageSource PauseIconLarge = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Pause, Size = 28, Color = Colors.White };

    private static readonly FontImageSource VolumeUpIcon = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Volume_up, Size = 22, Color = Colors.White };

    private static readonly FontImageSource VolumeOffIcon = new()
        { FontFamily = "MaterialOutlined", Glyph = MaterialOutlined.Volume_off, Size = 22, Color = Colors.White };

    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(VideoPlayer), null,
            propertyChanged: OnSourceChanged);

    public static readonly BindableProperty VideoTitleProperty =
        BindableProperty.Create(nameof(VideoTitle), typeof(string), typeof(VideoPlayer), string.Empty,
            propertyChanged: OnVideoTitleChanged);

    public static readonly BindableProperty ShouldAutoPlayProperty =
        BindableProperty.Create(nameof(ShouldAutoPlay), typeof(bool), typeof(VideoPlayer), false);

    private readonly double[] _speedOptions = [0.5, 0.75, 1.0, 1.25, 1.5, 2.0];
    private bool _controlsVisible = true;
    private CancellationTokenSource? _hideCts;
    private bool _isUserSeeking;
    private int _speedIndex = 2;
    private double _volumeBeforeMute = 1.0;

    public VideoPlayer()
    {
        InitializeComponent();
        VolumeSlider.Value = 1.0;
    }

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string VideoTitle
    {
        get => (string)GetValue(VideoTitleProperty);
        set => SetValue(VideoTitleProperty, value);
    }

    public bool ShouldAutoPlay
    {
        get => (bool)GetValue(ShouldAutoPlayProperty);
        set => SetValue(ShouldAutoPlayProperty, value);
    }

    public bool IsFullscreen { get; private set; }

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var player = (VideoPlayer)bindable;
        var url = (string?)newValue;

        if (string.IsNullOrWhiteSpace(url))
        {
            player.MediaPlayer.Stop();
            player.MediaPlayer.Source = null;
            return;
        }

        player.MediaPlayer.Source = MediaSource.FromUri(url);

        if (player.ShouldAutoPlay)
        {
            player.MediaPlayer.Play();
        }
    }

    private static void OnVideoTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var player = (VideoPlayer)bindable;
        player.TitleLabel.Text = (string)newValue;
    }

    private void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var isPlaying = e.NewState == MediaElementState.Playing;
            var isBuffering = e.NewState == MediaElementState.Buffering;
            var isOpening = e.NewState == MediaElementState.Opening;

            PlayPauseButton.Source = isPlaying ? PauseIcon : PlayIcon;
            CenterPlayButton.Source = isPlaying ? PauseIconLarge : PlayIconLarge;
            CenterPlayButton.IsVisible = !isPlaying && !isBuffering && !isOpening;
            BufferingIndicator.IsRunning = isBuffering || isOpening;
            BufferingIndicator.IsVisible = isBuffering || isOpening;

            if (isPlaying)
            {
                ResetHideTimer();
            }
            else
            {
                CancelHideTimer();
                ShowControls();
            }
        });
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        if (_isUserSeeking) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var position = e.Position;
            var duration = MediaPlayer.Duration;

            if (duration > TimeSpan.Zero)
            {
                ProgressSlider.Maximum = duration.TotalSeconds;
                ProgressSlider.Value = position.TotalSeconds;
            }

            CurrentTimeLabel.Text = FormatTime(position);
            DurationLabel.Text = FormatTime(duration);
        });
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayPauseButton.Source = PlayIcon;
            CenterPlayButton.Source = PlayIconLarge;
            CenterPlayButton.IsVisible = true;
            ShowControls();
        });
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BufferingIndicator.IsRunning = false;
            BufferingIndicator.IsVisible = false;
            CenterPlayButton.IsVisible = true;
        });
    }

    private void OnSeekCompleted(object? sender, EventArgs e)
    {
        _isUserSeeking = false;
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        TogglePlayPause();
    }

    private void OnCenterPlayClicked(object? sender, EventArgs e)
    {
        TogglePlayPause();
    }

    private void TogglePlayPause()
    {
        if (MediaPlayer.CurrentState == MediaElementState.Playing)
        {
            MediaPlayer.Pause();
        }
        else
        {
            MediaPlayer.Play();
        }
    }

    private void OnProgressSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isUserSeeking)
        {
            CurrentTimeLabel.Text = FormatTime(TimeSpan.FromSeconds(e.NewValue));
        }
    }

    private void OnSeekDragStarted(object? sender, EventArgs e)
    {
        _isUserSeeking = true;
        CancelHideTimer();
    }

    private void OnSeekDragCompleted(object? sender, EventArgs e)
    {
        _isUserSeeking = false;
        MediaPlayer.SeekTo(TimeSpan.FromSeconds(ProgressSlider.Value));
        ResetHideTimer();
    }

    private void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
    {
        MediaPlayer.Volume = e.NewValue;
        UpdateMuteIcon(e.NewValue);
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        if (MediaPlayer.Volume > 0)
        {
            _volumeBeforeMute = MediaPlayer.Volume;
            MediaPlayer.Volume = 0;
            VolumeSlider.Value = 0;
        }
        else
        {
            MediaPlayer.Volume = _volumeBeforeMute;
            VolumeSlider.Value = _volumeBeforeMute;
        }

        UpdateMuteIcon(MediaPlayer.Volume);
    }

    private void UpdateMuteIcon(double volume)
    {
        MuteButton.Source = volume <= 0 ? VolumeOffIcon : VolumeUpIcon;
    }

    private void OnSpeedClicked(object? sender, EventArgs e)
    {
        _speedIndex = (_speedIndex + 1) % _speedOptions.Length;
        var speed = _speedOptions[_speedIndex];
        MediaPlayer.Speed = speed;
        SpeedButton.Text = $"{speed}x";
    }

    public void ExitFullscreen()
    {
        if (!IsFullscreen) return;
        var activity = Platform.CurrentActivity;
        if (activity == null) return;
        activity.RequestedOrientation = ScreenOrientation.Portrait;
        IsFullscreen = false;
    }

    private void OnFullscreenClicked(object? sender, EventArgs e)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        if (IsFullscreen)
        {
            activity.RequestedOrientation = ScreenOrientation.Portrait;
            IsFullscreen = false;
        }
        else
        {
            activity.RequestedOrientation = ScreenOrientation.Landscape;
            IsFullscreen = true;
        }
    }

    private async void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        if (_controlsVisible)
        {
            await HideControls();
        }
        else
        {
            await ShowControls();
            if (MediaPlayer.CurrentState == MediaElementState.Playing)
            {
                ResetHideTimer();
            }
        }
    }

    private async Task ShowControls()
    {
        _controlsVisible = true;
        ControlsOverlay.IsVisible = true;
        await ControlsOverlay.FadeToAsync(1, 200);
    }

    private async Task HideControls()
    {
        _controlsVisible = false;
        await ControlsOverlay.FadeToAsync(0, 300);
        if (!_controlsVisible)
        {
            ControlsOverlay.IsVisible = false;
        }
    }

    private void ResetHideTimer()
    {
        CancelHideTimer();
        _hideCts = new CancellationTokenSource();
        _ = AutoHideAsync(_hideCts.Token);
    }

    private void CancelHideTimer()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    private async Task AutoHideAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(4000, ct);
            if (MediaPlayer.CurrentState == MediaElementState.Playing)
            {
                MainThread.BeginInvokeOnMainThread(async () => await HideControls());
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time == TimeSpan.Zero) return "00:00";
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    public void Play()
    {
        MediaPlayer.Play();
    }

    public void Pause()
    {
        MediaPlayer.Pause();
    }

    public void Stop()
    {
        MediaPlayer.Stop();
    }

    public void SeekTo(TimeSpan position)
    {
        MediaPlayer.SeekTo(position);
    }
}