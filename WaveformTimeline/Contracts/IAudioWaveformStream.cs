using System;

namespace WaveformTimeline.Contracts
{
    public interface IAudioWaveformStream: IObservable<float>
    {
        void Waveform(int resolution);
    }
}
