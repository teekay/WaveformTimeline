#nullable enable
using System;
using System.Collections.Concurrent;
using WaveformTimeline.Contracts;
using WaveformTimelineDemo.Avalonia.Toolbox;

namespace WaveformTimelineDemo.Avalonia.Audio;

internal class Tune : ICombiPlayer
{
    public Tune(string uri)
    {
        _uri = uri;
        _meta = new Metadata(uri);
        _player = new Player(uri);
    }

    private readonly string _uri;
    private readonly Metadata _meta;
    private readonly IPlayer _player;
    private readonly ConcurrentQueue<float> _waveformFloats = new();
    private byte[] _waveformBytes = Array.Empty<byte>();
    private IDisposable? _waveformUpdated;

    public bool IsPlaying() => _player.IsPlaying();
    public bool IsPaused() => _player.IsPaused();
    public bool PlaybackOn() => _player.IsPlaying();

    public void Play()
    {
        _player.Play();
        Transitioned?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _player.Pause();
        Transitioned?.Invoke(this, EventArgs.Empty);
    }

    public void Resume()
    {
        _player.Resume();
        Transitioned?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        _player.Stop();
        Transitioned?.Invoke(this, EventArgs.Empty);
    }

    public TimeSpan CurrentTime() => _player.CurrentTime();
    public TimeSpan TotalTime() => _player.TotalTime();
    public TimeSpan Duration() => _player.TotalTime();
    public string Name() => _meta.Title() ?? string.Empty;
    public byte[] WaveformData() => _waveformBytes;

    public IAudioWaveformStream WaveformStream()
    {
        var observable = new StreamingAudioWaveform(new AudioWaveform(_uri, FFTDataSize.FFT2048));
        _waveformUpdated = observable.Subscribe(
            AppendWaveformPoint,
            OnWaveformDataReady);
        return observable;
    }

    private void AppendWaveformPoint(float f) => _waveformFloats.Enqueue(f);

    private void OnWaveformDataReady()
    {
        var wfarr = _waveformFloats.ToArray();
        _waveformBytes = new FloatsAsBytes(wfarr).Bytes();
        _waveformUpdated?.Dispose();
    }

    public double[] Cues() => new double[] { 0, 1 };
    public double Tempo() => 100;
    public void Seek(TimeSpan position) => _player.Seek(position);
    public void TrimStart(TimeSpan start) { }
    public void TrimEnd(TimeSpan end) { }

    public event EventHandler<EventArgs>? Transitioned;
    public event EventHandler<EventArgs>? TempoShifted;
    public event EventHandler<EventArgs>? CuesChanged;
}
