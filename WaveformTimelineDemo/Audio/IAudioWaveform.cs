using System.Collections.Generic;

namespace WaveformTimelineDemo.Audio
{
    internal interface IAudioWaveform
    {
        IEnumerable<float> Waveform(int resolution);
    }
}