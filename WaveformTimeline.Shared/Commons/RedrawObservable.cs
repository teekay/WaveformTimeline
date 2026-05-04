using System;
using System.Collections.Generic;

namespace WaveformTimeline.Commons
{
    /// <summary>
    /// Helper used in the main control to observe, and throttle, events that can trigger re-rendering.
    /// </summary>
    internal sealed class RedrawObservable: IObservable<int>
    {
        private int _counter;
        private readonly List<IObserver<int>> _observers = new List<IObserver<int>>();
        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<int>> _allObservers;
            private readonly IObserver<int> _observer;

            public Unsubscriber(List<IObserver<int>> allObservers, IObserver<int> observer)
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

        public void Increment()
        {
            _counter++;
            _observers.ForEach(o => o.OnNext(_counter));
        }

        public IDisposable Subscribe(IObserver<int> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

    }
}
