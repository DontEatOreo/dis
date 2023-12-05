with import <nixpkgs> {};
  mkShell {
    name = "dotnet-env";
    packages = [
      dotnet-sdk_8
      yt-dlp
      ffmpeg
    ];
  }
