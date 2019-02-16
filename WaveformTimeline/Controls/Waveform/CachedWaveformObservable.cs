using System;
using System.Collections.Generic;
using System.Linq;
using WaveformTimeline.Contracts;

namespace WaveformTimeline.Controls.Waveform
{
    internal sealed class CachedWaveformObservable: IAudioWaveformStream
    {
        public CachedWaveformObservable(float[] waveformFloats, WaveformSection section)
        {
            _waveformFloats = waveformFloats;
            _section = section;
            _cachedIndexes = Enumerable.Range(section.Start, section.End - section.Start).ToList();
        }

        private readonly float[] _waveformFloats;
        private readonly WaveformSection _section;
        private readonly List<int> _cachedIndexes;
        private readonly List<IObserver<float>> _observers = new List<IObserver<float>>();
        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<float>> _allObservers;
            private readonly IObserver<float> _observer;

            public Unsubscriber(List<IObserver<float>> allObservers, IObserver<float> observer)
            {
                _allObservers = allObservers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _allObservers.Contains(_observer))
                    _allObservers.Remove(_observer);
            }
        }

        public IDisposable Subscribe(IObserver<float> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

        public void Waveform(int resolution)
        {
            var maxLength = _waveformFloats.Length;
            if (_section.Start > maxLength || _section.End > maxLength)
                throw new IndexOutOfRangeException("Input array does not match the provided WaveformSection");
            _cachedIndexes.ForEach(i => _observers.ForEach(o => o.OnNext(_waveformFloats[i])));
        }
    }
}
