# DisplayDriverReinstaller

A planned Windows command-line tool for reinstalling AMD graphics drivers. **This repository is an engineering foundation, not a working driver reinstaller.** The current CLI only displays help and fails safely for other invocations; it does not inspect or modify drivers, boot settings, or scheduled tasks.

The eventual workflow is intended to validate locally supplied DDU and AMD installer paths, restart into Safe Mode, run DDU, return to normal Windows, install the driver, and verify and clean up. Whether this can run fully unattended remains unverified. The application must not download programs at runtime.

## Requirements and development

- Windows 10 or 11, x64, is the intended product target. Windows 10 support availability depends on the edition and applicable Microsoft support program; consider its support lifecycle before deployment.
- Install a .NET 10 SDK compatible with [`global.json`](global.json). Run `dotnet --version` from the repository root to confirm selection.
- If using a per-user SDK installation, ensure its directory precedes other `dotnet` installations on `PATH`. For this machine's current PowerShell session, use `$env:PATH = "$HOME\.dotnet;$env:PATH"` before running the commands.

```powershell
dotnet restore DisplayDriverReinstaller.slnx
dotnet build DisplayDriverReinstaller.slnx --configuration Release --no-restore
dotnet test --solution DisplayDriverReinstaller.slnx --configuration Release --no-build
dotnet run --project src\DisplayDriverReinstaller -- --help
```

CI runs the same build and test commands on Windows. The SDK and dependencies may be downloaded for development and CI; the no-download constraint applies to the application's runtime.

## Safety and status

Do not use the CLI to perform a driver reinstall: the capability does not exist yet. Safe Mode failure recovery, unattended sign-in, DDU automation, installer automation, and the final recovery procedure need research and testing on suitable environments before implementation. No dedicated AMD test machine is currently available.

See [architecture and safety](docs/architecture.md), the [backlog drafts](docs/backlog.md), [contribution guidance](CONTRIBUTING.md), and [security reporting](SECURITY.md). The project is private; its source is licensed under [MIT](LICENSE). DDU and AMD software are separate third-party products and are neither included nor covered by this license.
