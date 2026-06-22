using System;
using System.Globalization;

namespace WaveformTimeline.Primitives
{
    /// <summary>
    /// A double that is coerced to the provided default value, or zero,
    /// when it happens to be NaN or infinite.
    /// </summary>
    public struct FiniteDouble: IComparable<FiniteDouble>, IEquatable<FiniteDouble>
    {
        public FiniteDouble(double value, double defaultValue = 0.0d)
        {
            _value = value;
            _defaultValue = defaultValue;
        }

        private readonly double _value;
        private readonly double _defaultValue;

        private bool IsFinite(double d) => !(double.IsNaN(d) || double.IsInfinity(d));

        public double Value()
        {
            return IsFinite(_value)
                ? _value
                : new FiniteDouble(_defaultValue, 0.0).Value();
        }

        public static implicit operator double(FiniteDouble fd) => fd.Value();

        public static implicit operator FiniteDouble(double value) => new FiniteDouble(value);

        public int CompareTo(FiniteDouble other)
        {
            return _value.CompareTo(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is FiniteDouble fd &&
                   fd.Value().Equals(Value());
        }

        public bool Equals(FiniteDouble other)
        {
            return Value().Equals(other.Value());
        }

        public override int GetHashCode()
        {
            return Value().GetHashCode();
        }

        public override string ToString() => Value().ToString(CultureInfo.InvariantCulture);
    }
}
