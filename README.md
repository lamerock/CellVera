# CellVera

CellVera is a focused Windows laptop battery dashboard for battery health, charge status, capacity, electrical details, and Windows power tools. It is OEM-neutral and does not depend on Lenovo or other manufacturer-specific services.

## Highlights

- Live charge percentage, power source, battery state, and estimated runtime.
- Battery health and estimated wear when design/full-charge capacity are exposed by the laptop firmware.
- Design capacity, full-charge capacity, cycle count, temperature, voltage, charge rate, and discharge rate when available.
- Generates the built-in Windows HTML battery report.
- Opens Windows Power & battery settings.
- Refreshes automatically every 15 seconds without blocking the UI thread during WMI reads.
- Custom CellVera application icon is embedded in the window and `CellVera.exe`.

## Appearance

CellVera includes three explicit appearance modes in the header:

- **System** — follows the Windows app theme. If Windows changes between light and dark while CellVera is open, the interface updates automatically.
- **Light** — always uses CellVera's high-contrast light palette.
- **Dark** — always uses CellVera's high-contrast dark palette.

The selected mode is saved to:

`%LOCALAPPDATA%\CellVera\settings.json`

System is the default on first launch.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK to build the project
- Visual Studio 2022 with **.NET desktop development**, or the .NET CLI

The release script publishes a self-contained Windows x64 executable, so the target PC does not need a separate .NET runtime for the published build.

## Build in Visual Studio

1. Open `CellVera.csproj`.
2. Allow NuGet restore to complete.
3. Choose **Build > Rebuild Solution**.
4. Run with **F5** or **Ctrl+F5**.

## Build a standalone EXE from PowerShell

If PowerShell blocks local scripts for the current session, you can use:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Then build:

```powershell
cd D:\CellVera
.\build-release.ps1
```

Each release is written to a fresh timestamped folder, for example:

`dist\CellVera-20261006-120000\CellVera.exe`

Using a new publish directory prevents a running older copy of `CellVera.exe` from locking the output file and breaking the next publish.

## Hardware support

Windows exposes basic battery state on most laptops, but richer values depend on the firmware and battery driver. Cycle count, temperature, charge/discharge rate, design capacity, or full-charge capacity may not be exposed on every machine. CellVera displays **Not available** instead of estimating unsupported values.

Charge thresholds and conservation modes are manufacturer-specific. CellVera intentionally does not attempt to change OEM charging limits through undocumented interfaces.

## Project structure

- `MainWindow.xaml` — single-dashboard interface and appearance selector
- `MainWindow.xaml.cs` — refresh workflow and Windows tool actions
- `Services/BatteryService.cs` — Windows/WMI battery data retrieval
- `Services/ThemeService.cs` — System/Light/Dark theme selection and persistence
- `Models/BatterySnapshot.cs` — battery data model and health calculations
- `Assets/CellVera.ico` — executable/window icon
- `build-release.ps1` — self-contained x64 release publisher
