// (c) Copyright Jacob Johnston.
// This source is subject to Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using NAudio.Dsp;

namespace WaveformTimelineDemo.Audio
{
    internal class SampleAggregator: ISampleAggregator
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

        /// <summary>
        /// Add a sample value to the aggregator.
        /// </summary>
        /// <param name="leftValue">The value of the left sample.</param>
        /// <param name="rightValue">The value of the right sample.</param>
        public void Add(float leftValue, float rightValue)
        {
            if (channelDataPosition == 0)
            {
                volumeLeftMaxValue = float.MinValue;
                volumeRightMaxValue = float.MinValue;
                volumeLeftMinValue = float.MaxValue;
                volumeRightMinValue = float.MaxValue;
            }

            // Make stored channel data stereo by averaging left and right values.
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

        /// <summary>
        /// Performs an FFT calculation on the channel data upon request.
        /// </summary>
        /// <param name="fftBuffer">A buffer where the FFT data will be stored.</param>
        public void GetFFTResults(float[] fftBuffer)
        {
            Complex[] channelDataClone = new Complex[_bufferSize];
            _channelData.CopyTo(channelDataClone, 0);
            FastFourierTransform.FFT(true, _binaryExponentitation, channelDataClone);
            for (int i = 0; i < channelDataClone.Length / 2; i++)
            {
                // Calculate actual intensities for the FFT results.
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
}
