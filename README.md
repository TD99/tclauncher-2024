<h1 align="center">
  <br>
  <a href="https://tcraft.link/tclauncher"><img src=".assets/logo.png" alt="TCLauncher" width="200"></a>
  <br>
  TCLauncher Windows Edition
  <br>
</h1>

<h4 align="center">Fast, reliable switching between T-Craft and local Minecraft profiles.</h4>

TCLauncher Windows Edition is the maintained WPF launcher for Windows. It keeps the compact dark interface and T-Craft-first experience while adding transactional installs, portable profiles, backups, diagnostics, offline accounts, and Fabric/Forge/NeoForge support. It is distinct from the separate cross-platform edition.

## Highlights

- Microsoft multi-account and explicit offline profiles
- Vanilla, Fabric, Forge, and NeoForge profiles
- Guided local profile creation and native catalog discovery
- Verified, cancellable installs with staging and rollback
- Safe `.tcl` v1 imports and portable `.tcl` v2 import/export
- Manual and automatic pre-update backups
- Instance health checks and previewed, redacted support bundles
- Signed update-manifest verification with non-blocking failures
- English, German, and French UI resources

No telemetry or crash report is uploaded automatically. Diagnostics remain local until the user explicitly exports a support bundle.

## Build

Requirements: Windows 10/11 x64, the .NET Framework 4.8.1 developer pack, and a current .NET SDK/MSBuild installation.

```powershell
dotnet restore TCLauncher.sln
dotnet build TCLauncher.sln --no-restore -c Release -m:1
dotnet test TCLauncher.sln --no-build -c Release -m:1
```

The SDK-style solution contains the application, unit tests, and integration tests. Advanced Installer remains the release packaging route but is intentionally excluded from the normal application build.

## Compatibility

Existing settings, Microsoft account storage, instance directories, `config.json`, `tcl:` links, and legacy `.tcl` packages remain supported. Missing configuration schema versions are interpreted as v1; a profile is written as schema v2 only after a successful atomic save.

Protocol and package contracts are described in [docs/contracts.md](docs/contracts.md). The project is available under the MIT license.
