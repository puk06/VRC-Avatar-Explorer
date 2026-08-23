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

## Disclaimer

This application supports automatic item retrieval from Booth, but it was developed using the Pixiv platform and is not an application created or distributed by Pixiv.

## Differences from KonoAsset

| Item | Avatar Explorer | KonoAsset |
|------|----------------|-----------|
| Explorer replacement | Explorer-style with file management capabilities | Specialized for asset management |
| Supported avatar management | Managed as avatar items (temporary avatar registration supported) | String-based management |
| Unitypackage import | Supports automatic path rewriting and batch import | Not supported |

## Installation

1. Open the [latest release page](https://github.com/puk06/VRC-Avatar-Explorer/releases/latest).
2. Download the file for your operating system.
3. For non-Windows platforms, run `AvatarExplorer` in the extracted folder.
4. On Windows, run the downloaded `setup` (`.exe`) and complete the installation.
5. If Windows SmartScreen appears, select `More info` and then `Run anyway` to continue.

For release-related notices, see [RELEASE_NOTICES.md](RELEASE_NOTICES.md).
Contribution and PR rules are summarized in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This software is distributed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**. See [LICENSE.txt](LICENSE.txt) for the full license text.

For the licenses of third-party libraries used, see [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

## Development Environment

- Development uses the **.NET 10.0 SDK**.
- Download and install the .NET 10.0 SDK before starting development.

## Project Structure

- **AvatarExplorer.Core**: The core part of AvatarExplorer. You can build new AvatarExplorer clients, such as a CLI, by operating this library. It is a simple class library and does not depend on the UI.
- **AvatarExplorer.UI**: The application layer used to operate AvatarExplorer.Core through a UI. Built with Avalonia UI.
- **Tools/LocalizationKeyGenerator**: A tool that generates `AvatarExplorer.Core/Localization/LocalizationKeys.g.cs` from `AvatarExplorer.Core/Data/Localization/ja-JP.json`. It runs automatically during `AvatarExplorer.Core` build.

---
