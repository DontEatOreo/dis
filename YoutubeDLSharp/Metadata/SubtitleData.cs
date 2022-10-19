using System.Text.Json.Serialization;

namespace dis.YoutubeDLSharp.Metadata;

public class SubtitleData
{
    public SubtitleData(string ext, string data, string url)
    {
        Ext = ext;
        Data = data;
        Url = url;
    }

    [JsonPropertyName("ext")]
    public string Ext { get; set; }
    [JsonPropertyName("data")]
    public string Data { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
}