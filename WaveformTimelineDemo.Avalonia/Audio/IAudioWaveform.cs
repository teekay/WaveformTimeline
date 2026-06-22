using System.Collections.Generic;

namespace WaveformTimelineDemo.Avalonia.Audio;

internal interface IAudioWaveform
{
    IEnumerable<float> Waveform(int resolution);
}
