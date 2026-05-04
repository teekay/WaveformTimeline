#nullable enable
using System;

namespace WaveformTimelineDemo.Avalonia.Toolbox;

internal class FloatsAsBytes
{
    public FloatsAsBytes(float[] floats)
    {
        _floats = floats ?? throw new ArgumentNullException(nameof(floats));
    }

    private readonly float[] _floats;

    public byte[] Bytes()
    {
        byte[] bd = new byte[_floats.Length * 4];
        Buffer.BlockCopy(_floats, 0, bd, 0, bd.Length);
        return bd;
    }
}
