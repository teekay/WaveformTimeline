using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NAudio.Wave;

namespace WaveformTimelineDemo.Audio
{
    internal sealed class AudioWaveform: IDisposable, IAudioWaveform
    {
        public AudioWaveform(string uri): this(uri, FFTDataSize.FFT1024)
        {
        }

        public AudioWaveform(string uri, FFTDataSize fftDataSize)
        {
            _fftDataSize = (int) fftDataSize;
            Uri = uri;
        }

        private readonly int _fftDataSize;
        private bool _disposed;

        private string Uri { get; }
        private const int NumChannels = 2;

        public IEnumerable<float> Waveform(int resolution)
        {
            var audioReader = Reader();
            var waveformInputStream = SampleNotifier(audioReader);
            var sampleAggregator = SampleAggregator(_fftDataSize);
            void SampleReceiver(SampleEventArgs e) => sampleAggregator.Add(e.Left, e.Right);
            using (SampleNotifications(waveformInputStream, SampleReceiver))
            {
                var waveformLength = (int) ((double) waveformInputStream.Length / _fftDataSize) * NumChannels;
                var readBuffer = new byte[_fftDataSize];
                var maxLeftPointLevel = float.MinValue;
                var maxRightPointLevel = float.MinValue;

                var currentPointIndex = 0;
                var waveMaxPointIndexes = Enumerable.Range(1, resolution).Select(i =>
                        (int) Math.Round(waveformLength * (i / (double) resolution), 0))
                    .ToList();
                var readCount = 0;
                while (currentPointIndex * 2 < resolution)
                {
                    var bytesRead = waveformInputStream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead <= 0)
                        break;

                    if (sampleAggregator.LeftMaxVolume > maxLeftPointLevel)
                        maxLeftPointLevel = sampleAggregator.LeftMaxVolume;
                    if (sampleAggregator.RightMaxVolume > maxRightPointLevel)
                        maxRightPointLevel = sampleAggregator.RightMaxVolume;

                    if (readCount > waveMaxPointIndexes[currentPointIndex])
                    {
                        yield return maxLeftPointLevel;
                        yield return maxRightPointLevel;
                        maxLeftPointLevel = float.MinValue;
                        maxRightPointLevel = float.MinValue;
                        currentPointIndex++;
                    }

                    readCount++;
                    if (_disposed) break;
                }
            }

            sampleAggregator.Clear();
        }

        private IDisposable SampleNotifications(ISampleNotifier waveformInputStream,
            Action<SampleEventArgs> onNotified) =>
            Observable.Create<SampleEventArgs>(o =>
            {
                EventHandler<SampleEventArgs> h = (s, e) => o.OnNext(e);
                waveformInputStream.Sample += h;
                return Disposable.Create(() => waveformInputStream.Sample -= h);
            }).Subscribe(onNotified);

        private MediaFoundationReader Reader() => new MediaFoundationReader(Uri);
        private SampleNotifier SampleNotifier(WaveStream source) => new SampleNotifier(source);
        private SampleAggregator SampleAggregator(int fftDataSize) => new SampleAggregator(fftDataSize);
        public void Dispose()
        {
            _disposed = true;
        }

    }

    internal enum FFTDataSize
    {
        /// <summary>
        /// A 256 point FFT. Real data will be 128 floating point values.
        /// </summary>
        FFT256 = 256,
        /// <summary>
        /// A 512 point FFT. Real data will be 256 floating point values.
        /// </summary>
        FFT512 = 512,
        /// <summary>
        /// A 1024 point FFT. Real data will be 512 floating point values.
        /// </summary>
        FFT1024 = 1024,
        /// <summary>
        /// A 2048 point FFT. Real data will be 1024 floating point values.
        /// </summary>
        FFT2048 = 2048,
        /// <summary>
        /// A 4096 point FFT. Real data will be 2048 floating point values.
        /// </summary>
        FFT4096 = 4096,
        /// <summary>
        /// A 8192 point FFT. Real data will be 4096 floating point values.
        /// </summary>
        FFT8192 = 8192,
        /// <summary>
        /// A 16384 point FFT. Real data will be 8192 floating point values.
        /// </summary>
        FFT16384 = 16384
    }

}
