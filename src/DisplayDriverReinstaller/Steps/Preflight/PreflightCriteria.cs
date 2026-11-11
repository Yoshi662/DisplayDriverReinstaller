using System.Runtime.InteropServices;

namespace DisplayDriverReinstaller.Steps.Preflight;

/// <summary>
/// Supported-input criteria for one local tool. A null member means the criterion has not been documented
/// or researched; the corresponding check is then reported as unverifiable and fails the preflight.
/// </summary>
public sealed record ToolCriteria
{
    public IReadOnlyCollection<string>? SupportedVersions { get; init; }

    public IReadOnlyCollection<string>? TrustedSigners { get; init; }
}

public sealed record ReadinessCriteria(bool RequireElevatedProcess);

/// <summary>
/// The criteria preflight evaluates. Only the Windows 10/11 x64 target in docs/architecture.md is established.
/// Null members are undocumented criteria: they are never assumed to pass.
/// </summary>
public sealed record PreflightCriteria
{
    public const int AmdVendorId = 0x1002;

    /// <summary>Criteria currently documented by the project; everything else is unverifiable.</summary>
    public static PreflightCriteria Documented { get; } = new();

    public Version SupportedWindowsVersion { get; init; } = new(10, 0);

    public Architecture SupportedArchitecture { get; init; } = Architecture.X64;

    public IReadOnlyCollection<string>? SupportedWindowsEditions { get; init; }

    public IReadOnlyCollection<int>? SupportedAmdDeviceIds { get; init; }

    public ReadinessCriteria? Readiness { get; init; }

    public ToolCriteria Ddu { get; init; } = new();

    public ToolCriteria AmdInstaller { get; init; } = new();
}
