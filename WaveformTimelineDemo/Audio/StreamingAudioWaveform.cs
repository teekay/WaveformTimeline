using System;
using System.Collections.Generic;
using System.Linq;
using WaveformTimeline.Contracts;

namespace WaveformTimelineDemo.Audio
{
    internal class StreamingAudioWaveform: IAudioWaveformStream
    {
        public StreamingAudioWaveform(IAudioWaveform source)
        {
            _source = source;
        }

        private readonly IAudioWaveform _source;
        private readonly List<IObserver<float>> _observers = new List<IObserver<float>>();

        public IDisposable Subscribe(IObserver<float> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

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

        public void Waveform(int resolution)
        {
            foreach (var maxVolume in _source.Waveform(resolution))
            {
                if (_observers.Count <= 0) break;
                _observers.ForEach(o => o.OnNext(maxVolume));
            }
            _observers.ToList().ForEach(o => o.OnCompleted());
            _observers.Clear();
        }
    }
}
