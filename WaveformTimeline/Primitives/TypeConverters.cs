using System;
using System.ComponentModel;
using System.Globalization;

namespace WaveformTimeline.Primitives
{
    public class ZeroToOneTypeConverter: TypeConverter
    {
        public ZeroToOne ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, string value)
        {
            try
            {
                return new ZeroToOne(value);
            }
            catch
            {
                return new ZeroToOne(0);
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue) return ConvertFrom(context, culture, stringValue);
            return base.ConvertFrom(context, culture, value);
        }

        public string ConvertTo(ITypeDescriptorContext context, CultureInfo culture, ZeroToOne value, Type destinationType) => value.ToString();

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is ZeroToOne zto) return ConvertTo(context, culture, zto, destinationType);
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
}
