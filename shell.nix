let
  pkgs = import <nixpkgs> {};
  ffmpeg-patched =
    # This is a temporary fix for broken H.264 codec on darwin (macOS)
    if pkgs.stdenv.isDarwin
    then
      pkgs.ffmpeg.override {
        x264 = pkgs.x264.overrideAttrs (old: {
          postPatch =
            old.postPatch
            + ''
              substituteInPlace Makefile --replace '$(if $(STRIP), $(STRIP) -x $@)' '$(if $(STRIP), $(STRIP) -S $@)'
            '';
        });
      }
    else pkgs.ffmpeg;
in
  pkgs.mkShell {
    name = "dotnet-env";
    packages = with pkgs; [
      dotnet-sdk_8
      yt-dlp
      ffmpeg-patched
    ];
  }
