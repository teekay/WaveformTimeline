using System;

namespace WaveformTimelineDemo.Avalonia.Audio;

public interface IPlayer
{
    bool IsPlaying();
    bool IsPaused();
    void Play();
    void Pause();
    void Resume();
    void Stop();
    void Seek(TimeSpan desiredTime);
    TimeSpan CurrentTime();
    TimeSpan TotalTime();
}
