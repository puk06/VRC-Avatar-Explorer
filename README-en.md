# VRC Avatar Explorer

Japanese README: [README.md](README.md)

![GitHub Release](https://img.shields.io/github/v/release/puk06/VRC-Avatar-Explorer?label=Stable)
![GitHub Release](https://img.shields.io/github/v/release/puk06/VRC-Avatar-Explorer?include_prereleases&label=Pre-Release)

A simple yet powerful cross-platform asset management tool for VRChat users.

By linking asset files with Booth item information, Avatar Explorer can automatically organize and manage assets in an explorer-style interface.

---

## Features

- **Automatic asset file organization**: Organizes assets clearly in an explorer-style layout based on item information.
- **Automatic extraction for compressed files**: Supports automatic extraction for multiple archive formats (zip, rar, 7z, gz, tar).
- **Asset search**: Quickly finds required assets from large datasets by title, author, category, and more.
- **Supported avatar management**: Manages which avatars each asset is for.
- **Shared body group management**: Groups avatars that share a common body base for better efficiency.
- **Custom Unitypackage import**: Supports automatic path rewriting and batch import for multiple files.
- **Detailed status management**: Manages item tags, notes, and avatar implementation status.
- **Background customization**: Lets you set your preferred image as the application background.
- **Direct drag and drop from app to external tools**: Drag files directly from the app to tools like Unity.
- **Unregistered avatar handling**: Temporarily adds unregistered avatars as supported targets and links them later to official items.

## Differences from KonoAsset

| Item | Avatar Explorer | KonoAsset |
|------|----------------|-----------|
| Explorer replacement | Explorer-style with file management capabilities | Specialized for asset management |
| Supported avatar management | Managed as avatar items (temporary avatar registration supported) | String-based management |
| Unitypackage import | Supports automatic path rewriting and batch import | Not supported |
| Settings when adding items | Configure tags and other details after adding (speed-focused) | Configure everything while adding (accuracy-focused) |

## Installation

1. Open the [latest release page](https://github.com/puk06/VRC-Avatar-Explorer/releases/latest).
2. Download the file for your operating system.
3. For non-Windows platforms, run `AvatarExplorer` in the extracted folder.
4. On Windows, run the downloaded `setup` (`.exe`) and complete the installation.
5. If Windows SmartScreen appears, select `More info` and then `Run anyway` to continue.

For release-related notices, see [RELEASE_NOTICES.md](RELEASE_NOTICES.md).

### Version Tag Policy

This project uses `v<version>` as the base tag format, and only the following suffixes are allowed.

- `-beta.X` (pre-release)
- `-stable` (stable release tag)

Examples:

- `v2.0.0-beta.1`
- `v2.0.0-stable`

No other suffixes are used.

#### Semantic Versioning Guidelines

This project determines versions based on the following criteria.

| Version | Criteria | Examples |
|---------|----------|----------|
| **v2.0.0** | Complete rewrite from V1 (fixed). This version remains unchanged. | v2.0.0, v2.0.0-beta.1 |
| **v0.x.0** (MINOR) | Feature additions or specification changes that users notice. | New features, significant improvements to existing features |
| **v0.0.x** (PATCH) | Code-only changes with no direct user impact. Bug fixes, internal refactoring, performance improvements, etc. | FileNameUtils security hardening, security fixes |

---

## Development Environment

- Development uses the **.NET 10.0 SDK**.
- Download and install the .NET 10.0 SDK before starting development.

## Project Structure

- **AvatarExplorer.Core**: The core part of AvatarExplorer. You can build new AvatarExplorer clients, such as a CLI, by operating this library. It is a simple class library and does not depend on the UI.
- **AvatarExplorer.UI**: The application layer used to operate AvatarExplorer.Core through a UI. Built with Avalonia UI.
- **Tools/LocalizationKeyGenerator**: A tool that generates `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` from `AvatarExplorer.Core/Data/Localization/ja-JP.json`. It runs automatically during `AvatarExplorer.Core` build.

---

## Commit and Branch Policy

The following rules apply to contributions and maintenance.

### `main` branch
- **Language**: English only.
- **Commit format**: Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
- **Merge method**: Squash merge only.

### Other branches (`dev`, `feature/*`, etc.)
- **Language**: No restriction (Japanese or English).
- **Commit format**: Free style.

### Version Bump Rule (Standardized)
- **Branch name**: `chore/bump-version-<version>`
- **Commit message**: `chore: bump version to <version>`

Examples:
- `chore/bump-version-0.3.2`
- `chore: bump version to 0.3.2`

### Pull Requests

**Title format (required)**

Because the PR title becomes the commit message on squash merge, write it in [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format.

```text
<type>[(<scope>)]: <subject>
```

- **type**: Commit type (required)
- **scope**: Affected area (optional) - module or component name
- **subject**: Short description of the change (required)

**Available types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation update
- `ci`: CI/CD pipeline or GitHub Actions changes
- `refactor`: Code structure improvement (no behavior change)
- `perf`: Performance improvement
- `test`: Test additions or updates
- `chore`: Build configuration, tooling updates, etc.

**Title examples**:
- `feat(avatar): add support for custom avatar names` (with scope)
- `feat: improve search performance` (without scope)
- `fix(ui): resolve overlay display bug on startup`
- `docs: update contribution guidelines`
- `ci: add automated release workflow`

**Description**:
- Briefly describe the purpose and background of the PR (Japanese is acceptable)
- Reference related issues, for example `Closes #123`
- Include notable implementation details or cautions if needed

**Pre-PR checklist (required)**:
- Build succeeds: Build `AvatarExplorer.UI` or `AvatarExplorer.Core` and confirm there are no errors
- Localization keys regenerated: Run `Tools/LocalizationKeyGenerator` to auto-generate `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` (run even if there are no changes)

These checks are also executed in CI, but running them beforehand helps avoid rework before merge.

**Individual commits in feature branches**:
- Commit messages inside feature branches are flexible
- Since branch history is squashed when merging into main, you can focus on development speed

## AI Usage Guidelines

Using AI tools (such as GitHub Copilot) is allowed when contributing to this project. Please keep the following in mind.

**Most important**:
- Always review and validate every code change yourself. Even if code is AI-generated, this is required to prevent bugs and maintain project quality.

**Examples of acceptable usage**:
- Partial completion of functions and methods
- Logic snippet generation
- CI/CD configuration such as GitHub Actions
- Documentation writing
- Refactoring suggestions

**Review checklist**:
- Is the logic correct?
- Are edge cases considered?
- Does it follow project coding conventions?
- Are there performance concerns?

## Architecture and Future Direction

In the current UI layer, overlay-related processing is concentrated in the MainWindow class.
This is a known design issue. Migration to MVVM will be handled in a separate branch,
so for now please implement changes in a way that follows the current design.

### Naming Rules (temporary)

Because logic is currently centralized in MainWindow,
we use the following naming rules to make per-overlay responsibilities clearer.

These rules are expected to be removed after migration to MVVM.

- Members dedicated to an overlay should follow `<OverlayName>_<MemberName>`
- Private fields should use `_` + camelCase

Examples:
- `_hogeOverlay_foo`
- `HogeOverlay_DoSomething`

---
