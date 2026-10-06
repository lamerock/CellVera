# Contributing to CellVera

Thanks for your interest in improving CellVera. Contributions that improve reliability, hardware compatibility, accessibility, documentation, and the Windows battery experience are welcome.

## Before you start

Please:

- Search existing issues before opening a duplicate.
- Keep changes focused; smaller pull requests are easier to review and test.
- Avoid vendor-specific or undocumented battery-control behavior unless there is a strong portability and safety case.
- Never commit secrets, signing certificates, private keys, tokens, or personal battery-report data.

## Development setup

You will need:

- Windows 10 or Windows 11 x64
- .NET 8 SDK
- Visual Studio 2022 with **.NET desktop development**, or the .NET CLI

Clone and build:

```powershell
git clone https://github.com/lamerock/CellVera.git
cd CellVera
dotnet restore
dotnet build -c Release
```

Run from Visual Studio or with the generated Release output.

## Branches

Create a short-lived branch from the latest `main`:

```powershell
git switch main
git pull
git switch -c feature/short-description
```

Suggested prefixes:

- `feature/` — new functionality
- `fix/` — bug fixes
- `docs/` — documentation-only changes
- `refactor/` — internal code improvements without intended behavior changes

## Coding guidelines

- Follow the existing C# and XAML style in the project.
- Keep WPF and Windows Forms namespaces isolated; WinForms should remain limited to tray-specific behavior where possible.
- Prefer explicit, readable code over clever abstractions.
- Keep UI work accessible: adequate contrast, keyboard focus, meaningful labels, and sensible scaling.
- Do not fabricate unavailable battery values. Show an unavailable state when Windows or firmware does not expose a metric.
- Avoid blocking the WPF UI thread with battery/WMI queries.
- Keep persistent writes lightweight and bounded.

## Testing checklist

Before opening a pull request, verify the relevant items below:

- `dotnet build -c Release` succeeds.
- CellVera launches without requiring a separately installed .NET runtime when using the published self-contained build.
- System, Light, and Dark themes render correctly.
- Refresh works and does not freeze the UI.
- Minimize-to-tray, tray restore, Refresh, and Exit work.
- Notification changes do not cause repeated alerts on every refresh.
- Charge-history changes do not produce excessive disk writes.
- Battery report generation and Power & battery settings still work.
- Missing hardware metrics degrade gracefully to **Not available**.

Hardware-dependent changes should be tested on more than one laptop when practical.

## Commit messages

Use short, descriptive commit messages in the imperative style where practical:

```text
Fix tray restore behavior
Improve charge history rendering
Add battery notification preference
```

## Pull requests

A good pull request should include:

- What changed and why
- Screenshots for visible UI changes
- Steps used to test the change
- Any known hardware limitations or follow-up work
- A linked issue when applicable

Keep unrelated formatting or refactoring out of feature/bug-fix pull requests unless it is required for the change.

## Reporting bugs

Please include:

- CellVera version
- Windows version
- Laptop manufacturer/model when relevant
- Whether you used the installer or portable build
- Expected behavior
- Actual behavior
- Reproduction steps
- Relevant error text or logs

Do not upload raw battery reports or diagnostic files without reviewing them for private information first.

## Feature requests

Describe the problem you want to solve before proposing a specific implementation. For hardware-control features, explain how the behavior would remain safe and portable across laptop manufacturers.

## License

By submitting a contribution to CellVera, you agree that your contribution may be distributed under the project's [MIT License](LICENSE).

