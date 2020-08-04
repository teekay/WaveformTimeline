using System;
using JetBrains.Annotations;

namespace WaveformTimelineDemo.Toolbox
{
    /// <summary>
    /// Encapsulates an array of floats, and provides it as an array of bytes.
    /// </summary>
    internal class FloatsAsBytes
    {
        public FloatsAsBytes([NotNull] float[] floats)
        {
            _floats = floats ?? throw new ArgumentNullException(nameof(floats));
        }

        private readonly float[] _floats;

        /// <summary>
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
