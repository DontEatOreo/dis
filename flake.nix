{
  inputs = {
    flake-parts.url = "github:hercules-ci/flake-parts";
    devenv.url = "github:cachix/devenv";
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";
  };

  outputs =
    inputs:
    inputs.flake-parts.lib.mkFlake { inherit inputs; } {
      imports = [ inputs.devenv.flakeModule ];
      systems = [
        "aarch64-linux"
        "aarch64-darwin"
        "x86_64-linux"
        "x86_64-darwin"
      ];
      perSystem =
        {
          pkgs,
          lib,
          self',
          config,
          ...
        }:
        {
          packages.dis = self'.packages.default;
          packages.default = (pkgs.callPackage ./package.nix { }).default;
          devenv.shells.default = {
            packages = builtins.attrValues { inherit (pkgs) yt-dlp ffmpeg-full; };
            languages.dotnet.enable = true;
            languages.dotnet.package = pkgs.dotnetCorePackages.sdk_10_0;
          };
        };
    };
}
