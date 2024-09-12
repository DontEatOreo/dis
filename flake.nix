{
  inputs = {
    nixpkgs.url = "github:cachix/devenv-nixpkgs/rolling";
    systems.url = "github:nix-systems/default";
    devenv.url = "github:cachix/devenv";
    devenv.inputs.nixpkgs.follows = "nixpkgs";
  };

  nixConfig = {
    extra-trusted-public-keys = "devenv.cachix.org-1:w1cLUi8dv3hnoSPGAuibQv+f9TZLr6cv/Hm9XgU50cw=";
    extra-substituters = "https://devenv.cachix.org";
  };

  outputs =
    {
      nixpkgs,
      devenv,
      systems,
      ...
    }@inputs:
    let
      forEachSystem = nixpkgs.lib.genAttrs (import systems);
    in
    {
      packages = forEachSystem (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.buildDotnetModule {
            pname = "dis";
            version = "9.2.1";

            src = ./.;

            projectFile = "./dis.csproj";
            nugetDeps = ./deps.nix;

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            selfContainedBuild = true;

            executables = [ "dis" ];

            postFixup = ''
              makeWrapper ${pkgs.dotnetCorePackages.sdk_8_0}/bin/dotnet $out/bin/dis --add-flags $out/lib/dis/dis.dll
              wrapProgram "$out/bin/dis" \
                --prefix PATH : ${pkgs.ffmpeg_6-full}/bin \
                --prefix PATH : ${pkgs.yt-dlp}/bin
            '';

            runtimeDeps = [ pkgs.icu ];

            meta = {
              homepage = "https://github.com/DontEatOreo/dis";
              license = pkgs.lib.licenses.agpl3Plus;
              platforms = pkgs.lib.platforms.unix;
              maintainers = [ pkgs.lib.maintainers.donteatoreo ];
            };
          };
        }
      );
      devShells = forEachSystem (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = devenv.lib.mkShell {
            inherit inputs pkgs;
            modules = [
              {
                languages.dotnet = {
                  enable = true;
                  package = pkgs.dotnetCorePackages.combinePackages (
                    builtins.attrValues {
                      inherit (pkgs.dotnetCorePackages) sdk_8_0;
                    }
                  );
                };
                packages = builtins.attrValues { inherit (pkgs) ffmpeg_6-full yt-dlp; };
              }
            ];
          };
        }
      );
    };
}
