{
  description = "Dis Flake";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-23.11";

  outputs = {
    self,
    nixpkgs,
  }: let
    supportedSystems = ["x86_64-linux" "aarch64-linux" "x86_64-darwin" "aarch64-darwin"];
    forEachSupportedSystem = f:
      nixpkgs.lib.genAttrs supportedSystems (system:
        f {
          pkgs = import nixpkgs {inherit system;};
        });
  in {
    devShells = forEachSupportedSystem ({pkgs}: {
      devShells.default = import ./shell.nix {inherit pkgs;};
    });
    packages = forEachSupportedSystem ({pkgs}: {
      default = pkgs.buildDotnetModule {
        pname = "dis";
        version = "9.1.1";

        src = ./.;

        projectFile = "./dis.csproj";
        nugetDeps = ./deps.nix;

        dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
        selfContainedBuild = true;

        executables = ["dis"];

        postFixup = ''
          makeWrapper ${pkgs.dotnetCorePackages.sdk_8_0}/bin/dotnet $out/bin/dis --add-flags $out/lib/dis/dis.dll
          wrapProgram "$out/bin/dis" \
            --prefix PATH : ${pkgs.ffmpeg_6-full}/bin \
            --prefix PATH : ${pkgs.yt-dlp}/bin
        '';

        runtimeDeps = [pkgs.icu];

        meta = {
          homepage = "https://github.com/DontEatOreo/dis";
          license = pkgs.lib.licenses.agpl3Plus;
          platforms = pkgs.lib.platforms.unix;
          maintainers = [pkgs.lib.maintainers.donteatoreo];
        };
      };
    });
  };
}
