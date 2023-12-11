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
      default = pkgs.callPackage ./default.nix {};
    });
  };
}
