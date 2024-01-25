{pkgs, ...}:
pkgs.mkShell {
  name = "dotnet-env";
  packages = with pkgs; [
    dotnet-sdk_8
    yt-dlp
    ffmpeg_6-full
  ];
}
