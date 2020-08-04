namespace WaveformTimelineDemo.Toolbox
{
    internal sealed class StringWithPlaceholder
    {
        public StringWithPlaceholder(string value) : this(value, string.Empty)
        {
        }

        public StringWithPlaceholder(string value, string placeholder)
        {
            _value = value;
            _placeholder = !string.IsNullOrEmpty(placeholder)
                ? placeholder
                : string.Empty;
        }

        private readonly string _value;
        private readonly string _placeholder;

        public string Value()
        {
            return !string.IsNullOrEmpty(_value)
                ? _value
                : _placeholder;
        }
    }
}
