using System;
using System.Reactive.Disposables;
using WaveformTimeline.Contracts;

namespace WaveformTimelineDemo.Audio
{
    internal class NullPlayer: ICombiPlayer
    {
        void IPlayer.Seek(TimeSpan desiredTime)
        {
        }

        TimeSpan IPlayer.CurrentTime() => TimeSpan.Zero;

        TimeSpan IPlayer.TotalTime() => TimeSpan.Zero;

        TimeSpan ITimedPlayback.CurrentTime() => TimeSpan.Zero;

        TimeSpan ITimedPlayback.TotalTime() => TimeSpan.Zero;

        public TimeSpan Duration() => TimeSpan.Zero;

        public string Name() => string.Empty;

        public byte[] WaveformData() => new byte[0];

        public IAudioWaveformStream WaveformStream() => new DummyWaveformObservable();

        public double[] Cues() => new double[0];

        public double Tempo() => 0;

        public bool PlaybackOn() => false;

        public bool IsPlaying() => false;

        public bool IsPaused() => false;

        public void Play()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Stop()
        {
        }

        void ITune.Seek(TimeSpan position)
        {
        }

        public void TrimStart(TimeSpan start)
        {
        }

        public void TrimEnd(TimeSpan end)
        {
        }

        public event EventHandler<EventArgs> Transitioned;
        public event EventHandler<EventArgs> TempoShifted;

        private class DummyWaveformObservable : IAudioWaveformStream
        {
            public IDisposable Subscribe(IObserver<float> observer)
            {
                return Disposable.Create(() => { });
            }

            public void Waveform(int resolution)
            {
            }
        }
    }
}
