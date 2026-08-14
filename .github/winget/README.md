# Publishing ResX Resource Manager to WinGet

After the Azure DevOps pipeline completes, the `SignedPackages` artifact contains a signed
`ResXManager.zip`. Follow the steps below to publish that ZIP to the
[Windows Package Manager Community Repository](https://github.com/microsoft/winget-pkgs).

## Prerequisites

```powershell
winget install Microsoft.WingetCreate
```

No GitHub token is required for the manifest-generation step below. A token is only needed
if you want `wingetcreate` to submit the pull request for you.

## Steps

### 1 — Download the signed ZIP

Download `ResXManager.zip` from the Azure DevOps pipeline's `SignedPackages`
artifact.

### 2 — Create a GitHub Release

Upload the ZIP as an asset to a new GitHub Release tagged `v<version>`
(e.g. `v1.107.0`):

```powershell
gh release create v<version> ResXManager.zip `
  --title "<version>" `
  --notes-file "Release notes.md"
```

This produces a publicly accessible download URL:

```
https://github.com/dotnet/ResXResourceManager/releases/download/<version>/ResXManager.zip
```

### 3 — Generate the updated manifest files

Run `wingetcreate update` in prepare-only mode to compute the SHA-256 and generate the
updated manifest files locally without opening a pull request:

```powershell
wingetcreate update dotnet.ResXResourceManager `
  --version <version> `
  --urls "https://github.com/dotnet/ResXResourceManager/releases/download/<version>/ResXManager.zip" `
  --out .\winget-manifests
```

This creates the updated YAML files in `./winget-manifests`.

### 4 — Submit the PR manually

1. Fork `microsoft/winget-pkgs`.
2. Copy the generated manifest files into the matching package path in your fork.
3. Commit and push the changes.
4. Open a pull request against `microsoft/winget-pkgs`.

The WinGet team typically reviews and merges community PRs within a few days.

---

## First-time submission

If the package `dotnet.ResXResourceManager` does not yet exist in `microsoft/winget-pkgs`,
use `wingetcreate new` instead:

```powershell
wingetcreate new `
  "https://github.com/dotnet/ResXResourceManager/releases/download/<version>/ResXManager.zip" `
  --out .\winget-manifests
```

Then review and adjust the generated manifests (use the templates in this folder as a
reference), copy them into your fork of `microsoft/winget-pkgs`, and open the pull request
manually.

## Manifest templates

The YAML files in this folder are version-independent templates. `wingetcreate` fills in
the exact version and SHA-256 automatically; the templates are provided as a reference for
the initial submission.
