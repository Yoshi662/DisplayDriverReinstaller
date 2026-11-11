using System.Runtime.InteropServices;

namespace DisplayDriverReinstaller.Steps.Preflight;

public sealed record OperatingSystemInfo(Version Version, Architecture Architecture, string? Edition);

public sealed record DisplayAdapterInfo(int? VendorId, int? DeviceId, string? Name, string? DriverVersion);

public sealed record SystemReadinessInfo(bool? IsElevated);

public enum FileAccessState
{
    Readable,
    NotFound,
    NotAFile,
    AccessDenied,
    Unknown,
}

public enum SignatureState
{
    Valid,
    Invalid,
    NotSigned,
    Unverified,
}

public sealed record FileSignature(SignatureState State, string? Signer);

public sealed record FileInspection(FileAccessState Access, string? Version, FileSignature Signature)
{
    public static FileInspection Unavailable { get; } = new(FileAccessState.Unknown, null, new FileSignature(SignatureState.Unverified, null));
}

/// <summary>
/// Read-only system effects needed by preflight. Implementations must not change the system.
/// A null result means the information could not be determined, which preflight treats as unverifiable.
/// </summary>
public interface IPreflightProbe
{
    OperatingSystemInfo? GetOperatingSystem();

    IReadOnlyList<DisplayAdapterInfo>? GetDisplayAdapters();

    SystemReadinessInfo? GetSystemReadiness();

    FileInspection InspectFile(string path);
}
