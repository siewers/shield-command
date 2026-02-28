# Shield Commander

<p align="center">
  <img src="src/ShieldCommander.UI/Assets/app-icon.png" width="128" alt="Shield Commander icon" />
</p>

<p align="center">
  <strong>Monitoring and app management for your NVIDIA Shield TV</strong>
</p>

Shield Commander connects to your NVIDIA Shield TV over Wi-Fi and gives you full visibility into what's happening on your device — real-time performance charts, running processes, and complete control over installed apps.

## Features

**Activity Monitor** — Live charts for CPU, memory, disk I/O, network, and thermals with configurable refresh rates.

**Processes** — See every running process sorted by CPU usage, updated in real time. Right-click or double-click any process to view detailed info from `/proc` and `dumpsys package`.

**App Management** — Browse installed apps, uninstall what you don't need, and install APKs with drag-and-drop. Double-click any app for full package details including version, SDK levels, install dates, and size.

**System Info** — Device model, Android version, RAM, storage, and more at a glance.

**Device Discovery** — Automatically finds Shield devices on your network. Saves your devices for quick reconnection.

## Screenshots

*Coming soon*

## Installation

Download the latest release from the [Releases page](https://github.com/siewers/shield-commander/releases).

### macOS

The app is not signed with an Apple Developer certificate. macOS will block it the first time you open it. To bypass this:

1. Open Terminal
2. Run: `xattr -cr "/Applications/Shield Commander.app"`
3. Open the app normally

### ADB path

Shield Commander auto-detects ADB from common install locations. If it can't find it (e.g. when running from a `.app` bundle), open the connection dialog and set the path manually under **ADB Path**.

## Requirements

- macOS, Windows, or Linux
- [ADB (Android Debug Bridge)](https://developer.android.com/tools/adb) installed
- An NVIDIA Shield TV with **Developer Options** and **Network Debugging** enabled

## Built With

- [Avalonia UI](https://avaloniaui.net/) with [Fluent Avalonia](https://github.com/amwx/FluentAvalonia)
- [LiveCharts2](https://livecharts.dev/) for real-time charting
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) for MVVM

## License

[MIT](LICENSE)
