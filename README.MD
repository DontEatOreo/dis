# dis 🎥

[![GitHub license](https://img.shields.io/github/license/DontEatOreo/dis)](https://github.com/DontEatOreo/dis/blob/master/LICENSE)
[![GitHub release](https://img.shields.io/github/release/DontEatOreo/dis)](https://github.com/DontEatOreo/dis/releases)
[![GitHub issues](https://img.shields.io/github/issues/DontEatOreo/dis)](https://github.com/DontEatOreo/dis/issues)

**dis** 🎥 is a simple tool to compress videos for platforms like Discord and likewise. It uses [YT-DLP](https://github.com/yt-dlp/yt-dlp) and [FFmpeg](https://ffmpeg.org/download.html) to download and process videos from various sources, such as YouTube, Twitter, Reddit, and more.

I created this project to make smaller videos that load faster for everyone and are more easily shareable on Discord and other platforms.

## Build

### Nix

### Simply run `nix build .#default`

Alternatively you can also invoke a `nix shell github:DontEatOreo/dis` with the required packages to build **dis** 🎥

### Updating depdendancies on Nix

1. Fetch Deps `nix build .#default.fetch-deps`
2. Run the script `./result`
3. Move the generate `deps.nix` *(the `./result` script will show where it was saved)* file to `.`
4. Now you can safely unlink the result `unlink result`

### .NET

1. `dotnet restore`
2. `dotnet build dis.csproj -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true --configuration Release --runtime <rid>` ([Know RIDs](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids))
3. Then you can find the executable at `bin/Release/net8.0/<rid>/publish/`

## Installation

To install **dis** 🎥, you need to have [FFmpeg](https://ffmpeg.org/download.html) and [YT-DLP](https://github.com/yt-dlp/yt-dlp) installed on your system. You can download them from their official websites or use your package manager of choice.

You can then download the latest release of **dis** 🎥 from the [Releases](https://github.com/DontEatOreo/dis/releases) tab on GitHub. Alternatively, you can clone this repository and build the project yourself using [dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).

### If you're using Nix

### Using `nix profile`

```bash
nix profile install github:DontEatOreo/dis
```

### Using flakes

You will need to add  **dis** 🎥 to your inputs and pass it down your outputs

```nix
{
  # ...

  inputs = {
    # ...
    dis.url = "github:DontEatOreo/dis";
    dis.inputs.nixpkgs.follows = "nixpkgs";
    # ...
  };

  outputs = {
    # ...
    dis,
    # ...
  }
}
```

After that in whichever `.nix` file is responsible for your packages you will need to add **dis** 🎥

Example:

```nix
{pkgs, inputs, ...}: {
  environment = {
    # ...
    systemPackages = builtins.attrValues {
        # ...
        dis = inputs.dis.packages.${pkgs.system}.default;
        # ...
    };
  };
}
```

## Usage

To use **dis** 🎥, you need to provide an input video source using the `-i` (`--input`) option. The input source can be either a local file or an online URL (e.g: Discord Server, YouTube, Twitter, Reddit, etc...).

You can also specify various options to customize the output video, such as:

| Option                              | Description                                                                                           | Default Value |
|-------------------------------------|-------------------------------------------------------------------------------------------------------|---------------|
| `-i`, `--input` `<input>`           | Input video source                                                                                    | None          |
| `-o`, `--output` `<output>`         | Output directory for the compressed video                                                             | Current Path  |
| `-c`, `--crf` `<crf>`               | Constant Rate Factor (CRF) Higher values mean lower quality, Lower values mean higher quality         | `25`          |
| `-r`, `--resolution` `<resolution>` | Output video resolution                                                                               | None          |
| `-t`, `--trim` `<trim>`             | Trim input using the format: ss.ms-ss.ms                                                              | None          |
| `--video-codec` `<video-codec>`     | Output video codec                                                                                    | None          |
| `--audio-bitrate` `<audio-bitrate>` | Output audio bitrate (in kbit/s) (only divisible by 2)                                                | `128k`        |
| `--multi-thread`                    | Use all available threads (for faster compression)                                                    | true          |
| `--random`                          | Randomize the output filename                                                                         | `False`       |
| `--sponsor`                         | Remove the sponsor segments from the output video using [SponsorBlock](https://sponsor.ajay.app/) API | `False`       |

The CRF is a video compression method that grants users control over the output video's quality, with higher values resulting in lower quality and vice versa. It is recommended to avoid values below 22 or above 38.

Here are some examples of how to use **dis** 🎥:

Compressing a video sourced from an online location:

```bash
dis -i https://cdn.discordapp.com/attachments/x/x/video.webm
```

Compressing a local file while adjusting its resolution to `480p` and saving it to a designated directory:

```bash
dis -i video.webm -r 480p -o /home/user/Downloads
```

Compressing a video from YouTube and change the video codec to `h265`, resolution to `720p`, and audio bitrate to `128`, with a custom CRF value:

```bash
dis -i https://youtu.be/xxxxxxx --video-codec h265 -r 720p --audio-bitrate 128 -c 28
```

Compressing a video from YouTube and trimming only a 60 seconds period of time.

```bash
dis -i https://youtu.be/xxxxxxx -t 50-110
```

## Contributing

If you want to contribute to **dis** 🎥, you are welcome to do so. You can report issues, request features, or submit pull requests on GitHub.

## License

**dis** is licensed under the [AGPLv3](https://github.com/DontEatOreo/dis/blob/master/LICENSE).
