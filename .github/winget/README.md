# Publishing ResX Resource Manager to WinGet

After the Azure DevOps pipeline completes, the `SignedPackages` artifact contains a signed
`ResXManager.zip`. Follow the steps below to publish that ZIP to the
[Windows Package Manager Community Repository](https://github.com/microsoft/winget-pkgs).

## Steps

### 1 — Download the signed ZIP

Download `ResXManager.zip` from the Azure DevOps pipeline's `SignedPackages` artifact.

### 2 — Create a GitHub Release

Upload the ZIP as an asset to a new GitHub Release tagged `v<version>`
(e.g. `v1.107.0`):

```powershell
gh release create v<version> ResXManager.zip `
  --title "v<version>" `
  --notes-file "Release notes.md"
```

### 3 — Generate the updated manifest files

Before running the script, make sure your fork of
[`microsoft/winget-pkgs`](https://github.com/microsoft/winget-pkgs) is cloned at:

```
D:\dev\GitHub\winget-pkgs
```

Run the helper script from the repo root. It reads the version from
`src/Directory.Build.props`, downloads the release ZIP to compute its SHA-256, and writes
ready-to-use manifest YAMLs directly into your fork at:

```
D:\dev\GitHub\winget-pkgs\manifests\d\dotnet\ResXResourceManager\<version>\
```

```powershell
.\.github\winget\Update-WingetManifest.ps1
```

To override the version:

```powershell
.\.github\winget\Update-WingetManifest.ps1 -Version 1.108.0
```

### 4 — Submit the PR manually

Commit the generated files in your `winget-pkgs` fork, push, and open a pull request
against `microsoft/winget-pkgs`.

The WinGet team typically reviews and merges community PRs within a few days.

## Manifest templates

The `*.yaml` files in this folder are the version-independent templates that
`Update-WingetManifest.ps1` uses as its source. The script stamps the correct
`PackageVersion`, `InstallerUrl` and `InstallerSha256` into the output copies;
the templates themselves are never modified.
