using System;
using System.Collections.Generic;
using System.Linq;
using WaveformTimeline.Contracts;

namespace WaveformTimelineDemo.Avalonia.Audio;

internal class StreamingAudioWaveform : IAudioWaveformStream
{
    public StreamingAudioWaveform(IAudioWaveform source)
    {
        _source = source;
    }

    private readonly IAudioWaveform _source;
    private readonly List<IObserver<float>> _observers = new();

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
            if (_allObservers.Contains(_observer))
                _allObservers.Remove(_observer);
        }
    }

    public void Waveform(int resolution)
    {
        foreach (var maxVolume in _source.Waveform(resolution))
        {
            if (_observers.Count <= 0) break;
            foreach (var o in _observers)
                o.OnNext(maxVolume);
        }
        foreach (var o in _observers.ToList())
            o.OnCompleted();
        _observers.Clear();
    }
}
