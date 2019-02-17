using System.Collections.Generic;
using System.Linq;
using TagLib;

namespace WaveformTimelineDemo.Audio
{
    internal class Metadata
    {
        public Metadata(string uri)
        {
            var file = File.Create(new File.LocalFileAbstraction(uri));
            var tag = MaybeTag(file);
            _title = tag?.Title ?? string.Empty;
        }

        /// <summary>
        /// Supported TagTypes
        /// </summary>
        private static readonly IList<TagTypes> TagTypesPref = new List<TagTypes>
        {
            TagTypes.Id3v2, TagTypes.Id3v1,
            TagTypes.Apple, TagTypes.FlacMetadata,
            TagTypes.Xiph
        };

        /// <summary>
        /// Returns a Tag from File if supported
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Tag MaybeTag(File file)
        {
            var pref = TagTypesPref.FirstOrDefault(tt => file.TagTypes.HasFlag(tt));
            return file.GetTag(pref);
        }

        private readonly string _title;
        public string Title() => _title;
    }
}
