{
  lib,
  buildDotnetModule,
  dotnetCorePackages,
  ffmpeg,
  yt-dlp,
  ...
}:
buildDotnetModule {
  pname = "dis";
  version = "8.1.5";

  src = ./.;

  projectFile = "./dis.csproj";
  nugetDeps = ./deps.nix;

  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  selfContainedBuild = true;

  executables = ["dis"];

  runtimeDeps = [ffmpeg yt-dlp];

  meta = with lib; {
    homepage = "https://github.com/DontEatOreo/dis";
    license = licenses.agpl3Plus;
    platforms = platforms.all;
    maintainers = with maintainers; [DontEatOreo];
  };
}
