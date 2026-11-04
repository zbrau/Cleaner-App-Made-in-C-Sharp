<div align="center">

<img src="Assets/logo.png" alt="AppCleaner Pro Logo" width="100" height="100" />

# AppCleaner Pro

**Advanced System Maintenance, Diagnostic Utilities, and Storage Optimization for Windows**

[![Release](https://img.shields.io/github/v/release/zbrau/Cleaner-App-Made-in-C-Sharp?style=flat-square&color=2563EB)](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D7?style=flat-square&logo=windows)](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp)
[![Framework](https://img.shields.io/badge/Framework-.NET%2010.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%2013-239120?style=flat-square&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-x64-gray?style=flat-square)](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)

[Download Installer](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp/releases/latest) • [Download Portable](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp/releases/latest) • [Key Features](#key-features) • [Build Instructions](#build-instructions) • [Architecture](#architecture)

</div>

---

## Overview

AppCleaner Pro is a high-performance system maintenance, optimization, and privacy application designed natively for Windows operating systems using .NET 10 and Windows Presentation Foundation (WPF). 

Engineered for speed and minimal resource footprint, AppCleaner Pro interacts directly with low-level Windows APIs, providing comprehensive deep-cleaning mechanisms (including DISM and browser SQLite database defragmentation), real-time memory monitoring, forensic-grade file destruction, and visual storage diagnostics without relying on external bloatware.

---

## Key Features

### System Cleaning and Optimization
- **Dashboard Overview**: Displays real-time PC Health Score (0-100), storage allocation metrics, and quick-action maintenance cards.
- **Turbo and Gaming Mode**: One-click termination of non-essential background processes, memory working set reclamation via native Windows APIs, aggressive garbage collection, and temporary file clearance.
- **System Junk Removal**: Identifies and purges Windows temporary directories, user logs, memory dumps, prefetch caches, and thumbnail databases across ten configurable categories.
- **Component Store Deeper Cleanup (DISM)**: Executes native Windows Imaging and Servicing Management routines (`dism.exe /Online /Cleanup-Image /StartComponentCleanup /NoRestart`) to reclaim space from superseded operating system updates.
- **Browser SQLite Database Optimization**: Scans, compacts, and re-indexes (`VACUUM; REINDEX;`) SQLite storage structures across Google Chrome, Microsoft Edge, Brave, and Mozilla Firefox.
- **RAM Booster**: Real-time memory pressure monitoring with a 60-second graphical timeline and instant working set minimization (`EmptyWorkingSet`).
- **Registry Maintenance**: Safely diagnoses and cleans obsolete registry keys, invalid startup links, broken file extensions, and corrupted MUI caches.

### Storage and Disk Diagnostics
- **Interactive Disk Analyzer**: Hierarchical directory exploration to drill down and identify heavy storage consumers.
- **Large File Hunter**: Fast discovery and sorting of the largest single files occupying disk volume.
- **Cryptographic Duplicate Finder**: MD5 byte-level hash verification to pinpoint exact duplicate files regardless of file names.
- **Disk Treemap**: Responsive visual proportional representation of folder disk usage.

### Privacy and Security Tools
- **File Shredder**: Multi-pass overwrite algorithms ensuring confidential documents cannot be restored using standard forensic recovery software.
- **Privacy Scanner**: Detection and clearance of recent file histories, clipboard contents, DNS resolver records, and unencrypted local credentials.
- **TCP/UDP Network Monitor**: Real-time active socket inspection with process attribution, port mapping, and protocol auditing.
- **Firewall Inspector**: Unified viewer and management console for active Windows Defender Firewall rules and network policies.

### Hardware Diagnostics and Customization
- **Hardware Specifications (WMI)**: Detailed technical audit of installed CPU, GPU, RAM banks, motherboard, and operating system build.
- **Dynamic Theming Engine**: Instant runtime theme switching supporting:
  - Nova Dark (Deep Space Navy)
  - Nova Light (Minimalist White)
  - Cyberpunk Neon (Purple and Electric Violet)
  - Emerald Matrix (Stealth Dark Green)
- **Procedural Sound Synthesis**: Mathematical algorithmic generation of harmonic WAV chime chords computed in memory at runtime without bundled audio files.

---

## Installation

Precompiled binaries and automated installers are available on the [Releases Page](https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp/releases/latest).

| Distribution | Package | Description |
| :--- | :--- | :--- |
| **Installer** | `AppCleaner_Setup_v2.0.0.exe` | Standard Windows installer with desktop and start menu shortcuts, uninstaller integration, and multilingual setup. |
| **Portable** | `AppCleanerPro_v2.0.0_Portable.zip` | Standalone archive. Extract to any directory and launch `AppCleanerPro.exe` directly. |

---

## Technical Specifications

| Parameter | Specification |
| :--- | :--- |
| **Target Runtime** | .NET 10.0 Windows (`net10.0-windows`) |
| **Language Version** | C# 13 |
| **UI Subsystem** | Windows Presentation Foundation (WPF) with hardware-accelerated XAML |
| **Packaging & Deployment** | x64 Single-Folder / Inno Setup 6 |
| **Supported Operating Systems** | Windows 10 (1809+), Windows 11 (21H2+) |
| **Execution Context** | Requires Administrator privileges (`requireAdministrator` application manifest) |

---

## Build Instructions

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (version 10.0.100 or later)
- Visual Studio 2022 / 2025, JetBrains Rider, or VS Code with C# Dev Kit
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (optional, required only for building installer binaries)

### Compiling from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp.git
   cd Cleaner-App-Made-in-C-Sharp
   ```

2. Restore NuGet dependencies and build the solution:
   ```bash
   dotnet build AppCleanerPro.sln -c Release
   ```

3. Publish the standalone application:
   ```bash
   dotnet publish AppCleanerPro.csproj -c Release -r win-x64 --self-contained false -o publish/
   ```

4. Build installer executable (requires Inno Setup):
   ```bash
   iscc installer.iss
   ```

---

## Architecture and Repository Structure

```
AppCleanerPro/
├── Assets/
│   ├── Icons/                  # Pre-compiled high-DPI application icons
│   ├── app.ico                 # Multi-resolution application icon (16x16 to 256x256)
│   └── logo.png                # Brand identity logo
├── Converters/                 # XAML Value Converters (Image pack URI, Boolean, Formatters)
├── Core/
│   ├── AnalyzerEngine.cs       # File analysis, disk space indexing, duplicate hashing
│   ├── BrowserSqliteOptimizer.cs# SQLite VACUUM and REINDEX execution engine
│   ├── CleanerEngine.cs        # Temp, log, update cache, and trash purging engine
│   ├── DismEngine.cs           # WinSxS component store deep clean orchestrator
│   ├── SecurityEngine.cs       # Secure multi-pass file shredder and privacy scanning
│   ├── SystemEngine.cs         # Settings serialization and WMI hardware interrogations
│   └── ToolsEngine.cs          # Process management and registry repair logic
├── Models/                     # Strongly-typed data contracts and system models
├── Services/
│   ├── AudioService.cs         # Procedural runtime sound synthesizer (PCM WAV generation)
│   ├── NotificationService.cs  # Native Windows toast and background notification delivery
│   └── ThemeService.cs         # Dynamic XAML palette management and runtime theme swaps
├── Views/                      # Modular WPF UserControls and View implementations
├── App.xaml                    # Application resources and global design token definitions
├── AppCleanerPro.csproj        # Modern SDK-style project file
├── AppCleanerPro.sln           # Visual Studio / Rider solution definition
├── MainWindow.xaml             # Host window with animated sidebar navigation
├── installer.iss               # Inno Setup installation compiler definition
└── sign.ps1                    # Authenticode code signing automation script
```

---

## Security and Privileges

- **Elevated Privileges**: AppCleaner Pro interacts with system processes, the Windows Registry (`HKEY_LOCAL_MACHINE` and `HKEY_CURRENT_USER`), and administrative directories (e.g., `WinSxS`, `%SystemRoot%\Prefetch`, `%TEMP%`). It declares `<requestedExecutionLevel level="requireAdministrator" />` within `app.manifest`.
- **Authenticode Signature**: Binaries are signed using SHA-256 Authenticode signatures with RFC 3161 compliant timestamping to verify origin and code integrity.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.
