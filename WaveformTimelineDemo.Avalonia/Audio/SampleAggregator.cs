using System;
using System.Diagnostics;
using NAudio.Dsp;

namespace WaveformTimelineDemo.Avalonia.Audio;

internal class SampleAggregator : ISampleAggregator
{
    private readonly Complex[] _channelData;
    private readonly int _bufferSize;
    private readonly int _binaryExponentitation;
    protected float volumeLeftMaxValue;
    protected float volumeLeftMinValue;
    protected float volumeRightMaxValue;
    protected float volumeRightMinValue;
    protected int channelDataPosition;

    public SampleAggregator(int bufferSize)
    {
        _bufferSize = bufferSize;
        _binaryExponentitation = (int)Math.Log(bufferSize, 2);
        _channelData = new Complex[bufferSize];
    }

    public float LeftMaxVolume => volumeLeftMaxValue;
    public float LeftMinVolume => volumeLeftMinValue;
    public float RightMaxVolume => volumeRightMaxValue;
    public float RightMinVolume => volumeRightMinValue;

    public void Add(float leftValue, float rightValue)
    {
        if (channelDataPosition == 0)
        {
            volumeLeftMaxValue = float.MinValue;
            volumeRightMaxValue = float.MinValue;
            volumeLeftMinValue = float.MaxValue;
            volumeRightMinValue = float.MaxValue;
        }

        Debug.Assert(channelDataPosition < _channelData.Length);
        _channelData[channelDataPosition].X = (leftValue + rightValue) / 2.0f;
        _channelData[channelDataPosition].Y = 0;
        channelDataPosition++;

        volumeLeftMaxValue = Math.Max(volumeLeftMaxValue, leftValue);
        volumeLeftMinValue = Math.Min(volumeLeftMinValue, leftValue);
        volumeRightMaxValue = Math.Max(volumeRightMaxValue, rightValue);
        volumeRightMinValue = Math.Min(volumeRightMinValue, rightValue);

        if (channelDataPosition >= _channelData.Length)
        {
            channelDataPosition = 0;
        }
    }

    public void GetFFTResults(float[] fftBuffer)
    {
        Complex[] channelDataClone = new Complex[_bufferSize];
        _channelData.CopyTo(channelDataClone, 0);
        FastFourierTransform.FFT(true, _binaryExponentitation, channelDataClone);
        for (int i = 0; i < channelDataClone.Length / 2; i++)
        {
            fftBuffer[i] = (float)Math.Sqrt(channelDataClone[i].X * channelDataClone[i].X + channelDataClone[i].Y * channelDataClone[i].Y);
        }
    }

    public void Clear()
    {
        volumeLeftMaxValue = float.MinValue;
        volumeRightMaxValue = float.MinValue;
        volumeLeftMinValue = float.MaxValue;
        volumeRightMinValue = float.MaxValue;
        channelDataPosition = 0;
    }
}
