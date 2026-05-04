using System.Collections.Generic;
using System.Linq;
using TagLib;

namespace WaveformTimelineDemo.Avalonia.Audio;

internal class Metadata
{
    public Metadata(string uri)
    {
        var file = File.Create(new File.LocalFileAbstraction(uri));
        var tag = MaybeTag(file);
        _title = tag?.Title ?? string.Empty;
    }

    private static readonly IList<TagTypes> TagTypesPref = new List<TagTypes>
    {
        TagTypes.Id3v2, TagTypes.Id3v1,
        TagTypes.Apple, TagTypes.FlacMetadata,
        TagTypes.Xiph
    };

    public static Tag? MaybeTag(File file)
    {
        var pref = TagTypesPref.FirstOrDefault(tt => file.TagTypes.HasFlag(tt));
        return file.GetTag(pref);
    }

    private readonly string _title;
    public string Title() => _title;
}
