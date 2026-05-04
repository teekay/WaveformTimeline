using System;

namespace WaveformTimeline.Primitives
{
    /// <summary>
    /// An integer that must be even
    /// </summary>
    public struct Even : IComparable<Even>, IEquatable<Even>
    {
        public Even(int value): this(value, 1) {}

        public Even(int value, int offset)
        {
            _value = value;
            _offset = Math.Abs(offset) == 1
                ? offset
                : 1;
        }

        private readonly int _value;
        private readonly int _offset;

        public int Value() => _value % 2 == 0
            ? _value
            : _value + _offset;

        public int CompareTo(Even other) => Value().CompareTo(other.Value());

        public bool Equals(Even other) => Value().Equals(other.Value());

        public override bool Equals(object obj) => obj is Even even && Value().Equals(even.Value());

        public override int GetHashCode() => Value().GetHashCode();

        public static bool operator ==(Even one, Even two) => one.Value().Equals(two.Value());

        public static bool operator !=(Even one, Even two) => !(one == two);

        public static implicit operator int(Even even) => even.Value();

        public static implicit operator Even(int value) => new Even(value);

        public override string ToString() => Value().ToString();
    }
}
