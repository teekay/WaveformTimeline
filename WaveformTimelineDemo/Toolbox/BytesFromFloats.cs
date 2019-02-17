using System;
using JetBrains.Annotations;

namespace WaveformTimelineDemo.Toolbox
{
    internal class BytesFromFloats
    {
        public BytesFromFloats([NotNull] float[] floats)
        {
            _floats = floats ?? throw new ArgumentNullException(nameof(floats));
        }

        private readonly float[] _floats;

        /// <summary>
        /// Takes a floats array of arbitrary floats, and returns is as an array of bytes
        /// </summary>
        /// <returns></returns>
        public byte[] Bytes()
        {
            byte[] bd = new byte[_floats.Length * 4];
            Buffer.BlockCopy(_floats, 0, bd, 0, bd.Length);
            return bd;
        }
    }
}
