using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NAudio.Wave;

namespace WaveformTimelineDemo.Audio
{
    internal class SampleNotifier: WaveStream, ISampleNotifier, IDisposable
    {
        /// <summary>
        /// Takes an IAbstractedWaveStream and notifies about samples read
        /// </summary>
        /// <param name="source"></param>
        public SampleNotifier(WaveStream source)
        {
            _source = source;
            _waveChannel32 = new WaveChannel32((WaveStream)source);
            Observable.Create<SampleEventArgs>(o =>
            {
                EventHandler<SampleEventArgs> h = (s, e) => o.OnNext(e);
                _waveChannel32.Sample += h;
                return Disposable.Create(() => _waveChannel32.Sample -= h);
            }).Subscribe(OnSampleObserved);
        }

        private readonly WaveStream _source;
        private readonly WaveChannel32 _waveChannel32;

        public override int Read(byte[] buffer, int offset, int count) =>
            _waveChannel32.Read(buffer, offset, count);

        public override WaveFormat WaveFormat => _waveChannel32.WaveFormat;
        public override long Length => (int)_waveChannel32.Length;
        public override long Position { get; set; }

        private void OnSampleObserved(SampleEventArgs e) =>
            Sample?.Invoke(this, new SampleEventArgs(e.Left, e.Right));

        public event EventHandler<SampleEventArgs> Sample;

        public new void Dispose()
        {
            _waveChannel32.Dispose();
            _source.Dispose();
        }
    }
}
