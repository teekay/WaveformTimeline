#nullable enable
using System;

namespace WaveformTimeline.Contracts
{
    /// <summary>
    /// Represents the characteristics and behavior of a track that is being visualized and (potentially) manipulated by the waveform control.
    /// </summary>
    public interface ITune: ITimedPlayback
    {
        /// <summary>
        /// Human-friendly name of the track. Could be the track title from the metadata.
        /// </summary>
        string Name();

        /// <summary>
        /// An array of bytes representing the waveform data
        /// </summary>
        /// <returns></returns>
        byte[] WaveformData();

        /// <summary>
        /// Provides an observable stream of waveform data
        /// </summary>
        IAudioWaveformStream WaveformStream(); // this makes it take a dependency on MusicLibrary :(

        /// <summary>
        /// An array of starting / ending "cues", positions in time where the track should start and finish playback,
        /// expressed as percentages (0-1)
        /// </summary>
        double[] Cues();

        /// <summary>
        /// Value of time-shifted playback speed; 100 = original rate of speed.
        /// </summary>
        /// <returns></returns>
        double Tempo();

        /// <summary>
        /// Tells whether or not is playback going on.
        /// </summary>
        /// <returns></returns>
        bool PlaybackOn();

        /// <summary>
        /// Seeks to a given position in the audio stream - the client should navigate to that position.
        /// </summary>
        /// <param name="position"></param>
        void Seek(TimeSpan position);

        /// <summary>
        /// Tells the client to skip the provided value of Time at the start of playback (=trim leading section).
        /// </summary>
        /// <param name="start"></param>
        void TrimStart(TimeSpan start);

        /// <summary>
        /// Tells the client to ignore the provided value of Time at the end of playback (=trim trailing section).
        /// </summary>
        /// <param name="end"></param>
        void TrimEnd(TimeSpan end);

        /// <summary>
        /// The client ought to raise this event whenever something important happens to playback (start, pause, unpause, stop) - anything
        /// that could change the values of: PlaybackOn(), CurrentTime(), TotalTime().
        /// </summary>
        event EventHandler<EventArgs>? Transitioned;

        /// <summary>
        /// The client ought to raise this event whenever the value of Tempo() might change.
        /// </summary>
        event EventHandler<EventArgs>? TempoShifted;

        /// <summary>
        /// Raise this event when the start / end cues change in the background as opposed by the user on the UI
        /// </summary>
        event EventHandler<EventArgs>? CuesChanged;
    }
}