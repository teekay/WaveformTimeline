using System;

namespace WaveformTimeline.Contracts
{
    public interface ITimedPlayback
    {
        /// <summary>
        /// Represents the current time in playback of the track.
        /// </summary>
        TimeSpan CurrentTime();

        /// <summary>
        /// The total time it takes to play back this track.
        /// </summary>
        TimeSpan TotalTime();

        /// <summary>
        /// Same as TotalTime() - TODO suspect, do we need both Duration() and TotalTime()
        /// </summary>
        TimeSpan Duration(); // 
    }
}
