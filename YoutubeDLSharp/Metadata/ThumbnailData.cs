using System.Text.Json.Serialization;

namespace dis.YoutubeDLSharp.Metadata;

public class ThumbnailData
{
    public ThumbnailData(string id, string url, int? preference, int? width, int? height, string resolution, int? filesize)
    {
        Id = id;
        Url = url;
        Preference = preference;
        Width = width;
        Height = height;
        Resolution = resolution;
        Filesize = filesize;
    }

    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("preference")]
    public int? Preference { get; set; }
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    [JsonPropertyName("height")]
    public int? Height { get; set; }
    [JsonPropertyName("resolution")]
    public string Resolution { get; set; }
    [JsonPropertyName("filesize")]
    public int? Filesize { get; set; }
}