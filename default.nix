{
  lib,
  buildDotnetModule,
  dotnetCorePackages,
  yt-dlp,
  ffmpeg_6-full,
  icu,
  ...
}:
buildDotnetModule {
  pname = "dis";
  version = "9.0.1";

  src = ./.;

  projectFile = "./dis.csproj";
  nugetDeps = ./deps.nix;

  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  selfContainedBuild = true;

  executables = ["dis"];

  postFixup = ''
    # The makeWrapper function is used here to create a shell wrapper script for our .NET DLL.
    # This wrapper runs the 'dotnet' command with our DLL as an argument, effectively making it executable as $out/bin/dis.
    makeWrapper ${dotnetCorePackages.sdk_8_0}/bin/dotnet $out/bin/dis --add-flags $out/lib/dis/dis.dll

    # Next, we use the wrapProgram function to add the necessary runtime dependencies to the PATH of our wrapped program.
    # This is akin to setting an environment variable, allowing our program to locate the 'ffmpeg' and 'yt-dl' utilities at runtime.
    # We prefix the PATH with the paths to 'ffmpeg' and 'yt-dlp', ensuring they are available to our application when it runs.
    wrapProgram "$out/bin/dis" \
      --prefix PATH : ${ffmpeg_6-full}/bin \
      --prefix PATH : ${yt-dlp}/bin
  '';

  runtimeDeps = [icu];

  meta = with lib; {
    homepage = "https://github.com/DontEatOreo/dis";
    license = licenses.agpl3Plus;
    platforms = platforms.all;
    maintainers = with maintainers; [DontEatOreo];
  };
}
