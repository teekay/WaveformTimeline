namespace WaveformTimelineDemo.Audio
{
    internal interface ISampleAggregator
    {
        float LeftMaxVolume { get; }
        float LeftMinVolume { get; }
        float RightMaxVolume { get; }
        float RightMinVolume { get; }
        void Add(float leftValue, float rightValue);
        void GetFFTResults(float[] fftBuffer);
        void Clear();
    }
}
