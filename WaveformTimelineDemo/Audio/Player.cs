using System;
using NAudio.Wave;

namespace WaveformTimelineDemo.Audio
{
    /// <summary>
    /// Simple audo stream player for demonstration purposes only
    /// </summary>
    internal class Player : IPlayer
    {
        public Player(string uri)
        {
            _reader = new MediaFoundationReader(uri);
        }

        private readonly MediaFoundationReader _reader;
        private WaveOutEvent _device;

        public bool IsPlaying() => _device?.PlaybackState == PlaybackState.Playing;
        public bool IsPaused() => _device?.PlaybackState == PlaybackState.Paused;

        public void Play()
        {
            if (IsPaused())
            {
                Resume();
                return;
            }
            _device = new WaveOutEvent {DeviceNumber = 0, DesiredLatency = 100};
            _device.Init(_reader);
            _device.Play();
        }

        public void Pause()
        {
            _device?.Pause();
        }

        public void Resume() => _device?.Play();

        public void Stop()
        {
            _device?.Stop();
            _device?.Dispose();
            _reader?.Dispose();
        }

        public void Seek(TimeSpan desiredTime)
        {
            if (_reader == null) return;
            _reader.CurrentTime = desiredTime;
        }

        public TimeSpan CurrentTime() => _reader?.CurrentTime ?? TimeSpan.Zero;

        public TimeSpan TotalTime() => _reader?.TotalTime ?? TimeSpan.Zero;
    }
}
