# See Releases to download the installer
## https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp/releases

# AppCleaner Pro

AppCleaner Pro is a comprehensive optimization, cleaning, and security solution for Windows operating systems, built on the modern **.NET 10** framework. The application combines hardware diagnostics, system cleanup utilities, and advanced management features wrapped in a sleek, glassmorphism-inspired dark/light theme interface to keep your PC running fast, lightweight, and secure.

![App Status](https://img.shields.io/badge/Status-Development-blueviolet?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-0078d7?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-.NET%2010-512bd4?style=for-the-badge&logo=dotnet)

---

## Features and Full Functionality

### 🧹 Cleaning & Optimization Module
- **Dashboard**: Real-time system health score (0-100), quick-action cards, live storage meters, and system summary.
- **Turbo / Gaming Mode**: Instantly terminates non-essential background processes, frees RAM working sets, triggers aggressive garbage collection, and clears temporary files with a single click.
- **System Cleanup**: Scans and removes Windows temporary files, thumbnail cache, system logs, prefetch data, and leftover installation packages.
- **DISM Component Store Cleanup (WinSxS)**: Deep system cleanup using `dism.exe /Online /Cleanup-Image /StartComponentCleanup /NoRestart` to purge obsolete Windows update backups.
- **Browser SQLite Database Optimizer**: Compacts and re-indexes (`VACUUM; REINDEX;`) SQLite databases across **Chrome, Edge, Brave, and Firefox** to boost browser responsiveness.
- **RAM Booster**: Real-time 60-second graphical monitoring of memory usage with one-click memory release using native Windows API (`EmptyWorkingSet`).
- **Registry Cleaner**: Safely identifies and repairs missing shared DLLs, obsolete extensions, invalid startup entries, and broken MUI cache references.

### 📊 Storage & Disk Analysis
- **Disk Analyzer**: Interactive drill-down folder explorer to pinpoint heavy directories.
- **Large File Finder**: Quickly sorts and displays the largest files on disk for selective removal.
- **Duplicate Finder**: MD5 cryptographic hash comparison algorithm to locate exact duplicate files regardless of filename.
- **Disk Treemap**: High-performance graphical visualization representing proportional disk consumption.

### 🛡️ Advanced Security & Privacy
- **File Shredder**: Multi-pass secure data destruction algorithms to prevent forensic recovery of sensitive files and folders.
- **Privacy Scanner**: Cleans recent document lists, clipboard cache, DNS resolver cache, and detects unencrypted credentials in local files.
- **Network Monitor (TCP/UDP)**: Real-time traffic monitoring of incoming/outgoing connections with process attribution.
- **Firewall Manager**: Streamlined interface to inspect and audit active Windows Firewall rules and policies.

### ⚙️ System Management & Customization
- **Process Manager**: Real-time task manager to monitor CPU/RAM usage and safely terminate unresponsive tasks.
- **Startup Manager**: Control apps that start with Windows to accelerate boot time.
- **Program Uninstaller**: Full application uninstaller with search and residual file tracking.
- **Hardware Diagnostics (WMI)**: Detailed CPU, GPU, RAM, Motherboard, and OS specifications.
- **Cleaning History**: Persistent log of space reclaimed over time.
- **Multi-Theme Engine**: Runtime switching between **Nova Dark**, **Nova Light**, **Cyberpunk Neon**, and **Emerald Matrix**.
- **Procedural Audio Synthesizer**: Pure C# algorithmic audio feedback (WAV generation in memory) without external media files.

---

## Technical & Security Requirements
AppCleaner Pro interacts with native Windows subsystem APIs (`psapi.dll`, `Microsoft.Win32`, and `System.Diagnostics`) to perform low-level maintenance:

- **Elevated Privileges**: The executable must run as Administrator to manage system services and access protected directories.
- **Code Signing**: Built-in automated signing workflow (`sign.ps1`) to ensure binary integrity and minimize false positives from heuristic scanners.

---

## Development Specs
- **Language**: C# 13 / .NET 10.0-windows
- **UI Framework**: WPF (Windows Presentation Foundation) with XAML hardware acceleration
- **Architecture**: Modular MVVM/Service-oriented architecture with custom XAML design tokens

---
*AppCleaner Pro: Modern, transparent, and powerful Windows maintenance.*
