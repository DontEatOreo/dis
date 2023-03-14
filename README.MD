# dis
**dis** is a command-line interface program engineered for compressing videos for Discord and other similar platforms. utilizing YT-DLP and FFmpeg.

## Usage
Compressing a video sourced from an online location (e.g: Discord Server, YouTube, Twitter, Reddit, etc...):
```bash
dis -i https://cdn.discordapp.com/attachments/x/x/video.webm
```
Compressing a local file while adjusting its resolution to ``480p``` and saving it to a designated directory:
```bash
dis -i video.webm -r 480p -o /home/user/Dowloads
```
Compressing a video sourced from the internet and modifying its video codec to ``h265``, audio codec to opus, resolution to ``720p``, and audio bitrate to ``128k``, with a custom CRF value:
```bash
dis -i https://youtu.be/xxxxxxx -vc h265 -a opus -r 720p -ab 128 -c 20
```

# Requirements
- [FFmpeg](https://ffmpeg.org/download.html)
- [YT-DLP](https://https://github.com/yt-dlp/yt-dlp)

## Notes
- The Constant Rate Factor (CRF) is a video compression method that grants users control over the output video's quality, with higher values resulting in lower quality and vice versa. It is recommended to avoid values below 20 or above 60.

You can run the program with ``dotnet run -- <arguments>``, compile an executable file using [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) or download a compiled version from [Releases](https://github.com/DontEatOreo/dis/releases) Tab