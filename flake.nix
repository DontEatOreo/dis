{
  inputs = {
    flake-parts.url = "github:hercules-ci/flake-parts";
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs =
    inputs:
    inputs.flake-parts.lib.mkFlake { inherit inputs; } {
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
        };
    };
}
