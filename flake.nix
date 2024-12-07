{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    devenv.url = "github:cachix/devenv";
    devenv.inputs.nixpkgs.follows = "nixpkgs";
    devenv.inputs.flake-compat.follows = "";
  };

  nixConfig = {
    extra-trusted-public-keys = "devenv.cachix.org-1:w1cLUi8dv3hnoSPGAuibQv+f9TZLr6cv/Hm9XgU50cw=";
    extra-substituters = "https://devenv.cachix.org";
  };

  outputs =
    inputs:
    let
      systems = [
        "aarch64-darwin"
        "aarch64-linux"
        "x86_64-darwin"
        "x86_64-linux"
      ];
      lib = inputs.nixpkgs.lib;
      pkgs = lib.genAttrs systems (
        system:
        import inputs.nixpkgs {
          inherit system;
          config = { };
        }
      );
      forEachSystem = f: lib.genAttrs systems (system: f pkgs.${system} system);
    in
    {
      packages = forEachSystem (
        pkgs: system: {
          default = pkgs.buildDotnetModule {
            pname = "dis";
            version = "10.0.0";

            src = ./.;

            projectFile = "./dis.csproj";
            nugetDeps = ./deps.nix;

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            selfContainedBuild = true;

            executables = [ "dis" ];

            postFixup = ''
              makeWrapper ${pkgs.dotnetCorePackages.sdk_8_0}/bin/dotnet "$out/bin/dis" --add-flags "$out/lib/dis/dis.dll"
              wrapProgram "$out/bin/dis" \
                --prefix PATH : ${
                  lib.makeBinPath [
                    pkgs.ffmpeg-full
                    pkgs.yt-dlp
                  ]
                } \
                --prefix LD_LIBRARY_PATH : ${pkgs.icu}/lib
            '';

            meta = {
              homepage = "https://github.com/DontEatOreo/dis";
              license = lib.licenses.agpl3Plus;
              platforms = lib.platforms.unix;
              maintainers = [ lib.maintainers.donteatoreo ];
            };
          };
        }
      );

      devShells = forEachSystem (
        pkgs: system: {
          default = inputs.devenv.lib.mkShell {
            inherit pkgs inputs;
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
                packages = builtins.attrValues { inherit (pkgs) ffmpeg-full yt-dlp; };
              }
            ];
          };
        }
      );
    };
}
