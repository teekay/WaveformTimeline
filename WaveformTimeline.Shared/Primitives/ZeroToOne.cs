using System;
using System.ComponentModel;

namespace WaveformTimeline.Primitives
{
    [TypeConverter(typeof(ZeroToOneTypeConverter))]
    public struct ZeroToOne: IComparable<ZeroToOne>, IEquatable<ZeroToOne>
    {
        public ZeroToOne(double value)
        {
            _value = double.IsNaN(value) ? 0 : value;
        }

        public ZeroToOne(string value): this(double.Parse(value))
        {
        }

        private readonly double _value;

        public double Value() => Math.Max(0, Math.Min(1, _value));

        public static implicit operator double(ZeroToOne zto) => zto.Value();

        public static implicit operator ZeroToOne(double value) => new ZeroToOne(value);

        public int CompareTo(ZeroToOne other) => Value().CompareTo(other.Value());

        public static bool operator ==(ZeroToOne one, ZeroToOne two) => one.Value().Equals(two.Value());

        public static bool operator !=(ZeroToOne one, ZeroToOne two) => !(one == two);

        public override bool Equals(object obj) => obj is ZeroToOne x && x.Value().Equals(Value());

        public bool Equals(ZeroToOne other) => Value().Equals(other.Value());

        public override int GetHashCode() => Value().GetHashCode();

        public override string ToString() => $"{Value()}";
    }
}
