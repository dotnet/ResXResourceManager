# Publishing ResX Resource Manager to WinGet

After the Azure DevOps pipeline completes, the `SignedPackages` artifact contains a signed
`ResXManager-<version>.zip`. Follow the steps below to publish that ZIP to the
[Windows Package Manager Community Repository](https://github.com/microsoft/winget-pkgs).

## Prerequisites

```powershell
winget install Microsoft.WingetCreate
```

You also need a GitHub personal access token (PAT) with `public_repo` scope so that
`wingetcreate` can open a pull request on `microsoft/winget-pkgs` on your behalf.

## Steps

### 1 — Download the signed ZIP

Download `ResXManager-<version>.zip` from the Azure DevOps pipeline's `SignedPackages`
artifact.

### 2 — Create a GitHub Release

Upload the ZIP as an asset to a new GitHub Release tagged `v<version>`
(e.g. `v1.107.0`):

```powershell
gh release create v<version> ResXManager-<version>.zip `
  --title "v<version>" `
  --notes-file "Release notes.md"
```

This produces a publicly accessible download URL:

```
https://github.com/dotnet/ResXResourceManager/releases/download/v<version>/ResXManager-<version>.zip
```

### 3 — Submit the updated manifest

Run `wingetcreate update` to compute the SHA-256, generate updated manifests and open a
pull request automatically:

```powershell
wingetcreate update dotnet.ResXResourceManager `
  --version <version> `
  --urls "https://github.com/dotnet/ResXResourceManager/releases/download/v<version>/ResXManager-<version>.zip" `
  --token "<github-pat>" `
  --submit
```

### 4 — Wait for the PR to be merged

The `wingetcreate` command prints the pull-request URL. The WinGet team typically reviews
and merges community PRs within a few days.

---

## First-time submission

If the package `dotnet.ResXResourceManager` does not yet exist in `microsoft/winget-pkgs`,
use `wingetcreate new` instead:

```powershell
wingetcreate new `
  "https://github.com/dotnet/ResXResourceManager/releases/download/v<version>/ResXManager-<version>.zip" `
  --token "<github-pat>"
```

Then review and adjust the generated manifests (use the templates in this folder as a
reference) before submitting.

## Manifest templates

The YAML files in this folder are version-independent templates. `wingetcreate` fills in
the exact version and SHA-256 automatically; the templates are provided as a reference for
the initial submission.
