{ pkgs, ... }: {
  channel = "unstable"; # Use unstable channel for .NET 9.0 preview
  packages = [
    (pkgs.dotnet-sdk_9.overrideAttrs (oldAttrs: {
      version = "9.0.100-preview.7.24407.12"; # Latest .NET 9.0 preview as of Sep 2025
    }))
  ];
  env = {};
  idx = {
    extensions = [];
    previews = {
      enable = true;
      previews = {};
    };
    workspace = {
      onCreate = {};
      onStart = {};
    };
  };
}