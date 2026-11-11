# DisplayDriverReinstaller

A planned Windows command-line tool for reinstalling AMD graphics drivers. **This repository is an engineering foundation, not a working driver reinstaller.** The current CLI displays help, offers a read-only `preflight` command, and fails safely for every other invocation; it does not modify drivers, boot settings, or scheduled tasks.

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

## Preflight

`preflight` checks prerequisites and the two local tools you supply. It is read-only: it never starts DDU or the AMD installer and changes nothing. A pass does **not** mean a reinstall was performed or will succeed.

```powershell
dotnet run --project src\DisplayDriverReinstaller -- preflight --ddu C:\Tools\DDU\DDU.exe --amd-installer C:\Tools\AMD\installer.exe
```

Each check is reported as `PASS`, `FAIL`, or `UNVERIFIED`. The command exits with 0 only when every check passes, 1 when any check fails or cannot be verified, and 2 for invalid arguments. A failing preflight must never advance a reinstall.

Checks and limitations:

- Windows 10/11 (version 10.0) on x64, and the presence of an AMD display adapter (PCI vendor ID `0x1002`) reported by WMI. These are the only criteria currently documented.
- Windows edition, supported AMD devices and driver prerequisites, system readiness, and each tool's supported versions and authenticity criteria are **not yet researched or documented**. The built-in criteria do not guess them, so those checks are `UNVERIFIED` and the shipped command cannot currently pass.
- Tool paths must be fully qualified local `.exe` paths (no relative, UNC, or device paths) pointing to a readable file. Paths are not echoed in results.
- File versions are read from the file's version resource. Authenticode signature verification is not implemented: signatures are always reported as unverified.
- Validation is a point-in-time check; it does not prevent the file from being replaced before later execution.

## Safety and status

Do not use the CLI to perform a driver reinstall: the capability does not exist yet. Safe Mode failure recovery, unattended sign-in, DDU automation, installer automation, and the final recovery procedure need research and testing on suitable environments before implementation. No dedicated AMD test machine is currently available.

See [architecture and safety](docs/architecture.md), the [backlog drafts](docs/backlog.md), [contribution guidance](CONTRIBUTING.md), and [security reporting](SECURITY.md). The project is public; its source is licensed under [MIT](LICENSE). DDU and AMD software are separate third-party products and are neither included nor covered by this license.
