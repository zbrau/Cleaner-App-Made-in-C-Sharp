# See Releases to download the installer
## https://github.com/zbrau/Cleaner-App-Made-in-C-/releases

# AppCleanerPro (NovaDash Design)

AppCleanerPro is a comprehensive optimization, cleaning, and security solution for Windows operating systems, built on the .NET 10 framework. The application combines diagnostic tools, system cleanup utilities, and advanced management features to keep your PC running fast and secure.

![App Status](https://img.shields.io/badge/Status-Development-blueviolet?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-0078d7?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-.NET%2010-512bd4?style=for-the-badge&logo=dotnet)

## Features and Full Functionality

### Cleaning & Optimization Module
- **Dashboard**: Control panel with a real-time summary of the system health status.
- **System Cleanup**: Scans and removes temporary files, application cache, logs, and leftover Windows junk.
- **Registry Cleaner**: Identifies and repairs null file extensions, corrupted associations, and obsolete entries in the Windows Registry.
- **RAM Booster**: Dynamic monitoring of memory usage and real-time resource release to improve system responsiveness.

### Storage Analysis
- **Disk Analyzer**: Deep exploration tool to identify folders consuming the most space.
- **Large File Finder**: Filters and shows the 100 largest files to enable selective space cleanup.
- **Duplicate Finder**: Bit-by-bit comparison algorithm to detect exact duplicate files regardless of name.
- **Disk Treemap**: Graphical proportional visualization of disk usage for an intuitive understanding of storage.

### Advanced Security & Privacy
- **File Shredder**: Permanently deletes sensitive data using overwrite algorithms, preventing recovery with forensic software.
- **Privacy Scanner**: Identifies activity traces, text files with exposed credentials, and potential local security risks.
- **Network Monitor (TCP/UDP)**: Real-time supervision of all incoming and outgoing connections to detect suspicious traffic.
- **Firewall Manager**: Simplified interface for managing network traffic rules and policies.

### System Management Tools
- **Process Manager**: Advanced control of active processes with the ability to stop stuck services and optimize CPU usage.
- **Startup Manager**: Manage apps and services that start automatically with Windows to speed up boot time.
- **Program Uninstaller**: Complete app removal, including cleanup of registry entries and residual folders after uninstall.
- **System Information**: Detailed hardware diagnostics (CPU, GPU, RAM) and technical specifications of installed software.

### Administration & Support
- **Cleaning History**: Detailed log of all maintenance actions performed by the user.
- **Settings**: Customize application behavior, visual themes, and scanning preferences.
- **Legal & Security Notice**: Section dedicated to technical transparency, detailing the use of administrator privileges and registry interaction to build user trust.

## Technical & Security Requirements
The application is designed to interact with deep Windows layers (Microsoft.Win32 and System.Diagnostics), ensuring a thorough cleanup, but it requires the following considerations:

- **Elevated Privileges**: The executable must be run as administrator to manage system processes and access protected directories.
- **Heuristic Scanning**: Because the application reads active processes and modifies the Registry without a commercial digital signature (development phase), tools like Windows Defender may flag it as suspicious.

## Development Specs
- **Language**: C#
- **UI**: WPF (Windows Presentation Foundation)
- **Rendering**: XAML with hardware acceleration
- **Target**: .NET 10.0-windows

---
AppCleanerPro: Professionalism, transparency, and power for Windows maintenance.