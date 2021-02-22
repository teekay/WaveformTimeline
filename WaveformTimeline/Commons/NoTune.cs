#nullable enable
using System;
using System.Reactive.Disposables;
using WaveformTimeline.Contracts;

namespace WaveformTimeline.Commons
{
    /// <summary>
    /// Fall-back "null" implementation of ITune.
    /// </summary>
    public sealed class NoTune: ITune
    {
        public NoTune() : this(0)
        {
        }

        public NoTune(double durationInSeconds)
        {
            _durationInSeconds = durationInSeconds;
        }

        private readonly double _durationInSeconds;

        public string Name() => "No tune";

        public TimeSpan CurrentTime() => TimeSpan.Zero;

        public TimeSpan TotalTime() => TimeSpan.FromSeconds(_durationInSeconds);

        public TimeSpan Duration() => TotalTime();

        public byte[] WaveformData() => new byte[0];
        public IAudioWaveformStream WaveformStream()
        {
            return new DummyWaveformObservable();
        }

        public double[] Cues() => new double[0];

        public double Tempo() => 100;

        public bool PlaybackOn() => false;

        public void Seek(TimeSpan position)
        {
            //no-op   
        }

        public void TrimStart(TimeSpan start)
        {
            //no-op   
        }

        public void TrimEnd(TimeSpan end)
        {
            //no-op   
        }

        public event EventHandler<EventArgs>? Transitioned;
        public event EventHandler<EventArgs>? TempoShifted;
        public event EventHandler<EventArgs>? CuesChanged;

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
