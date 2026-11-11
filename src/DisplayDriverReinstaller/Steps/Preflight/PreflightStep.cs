using System.Globalization;
using System.Security;

namespace DisplayDriverReinstaller.Steps.Preflight;

/// <summary>
/// Read-only preflight. It evaluates system prerequisites and both user-supplied local tools and never
/// invokes either tool or changes drivers, boot settings, scheduled tasks, or the running system.
/// </summary>
public sealed class PreflightStep(IPreflightProbe probe, PreflightCriteria criteria)
{
    private const string UndocumentedPrefix = "The supported criteria have not been documented, so this cannot be verified. ";

    public PreflightResult Run(PreflightRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<PreflightCheck> checks = [];

        OperatingSystemInfo? operatingSystem = Query(probe.GetOperatingSystem);
        IReadOnlyList<DisplayAdapterInfo>? adapters = Query(probe.GetDisplayAdapters);

        checks.Add(CheckWindowsVersion(operatingSystem));
        checks.Add(CheckWindowsEdition(operatingSystem));
        checks.Add(CheckAmdAdapterPresent(adapters));
        checks.Add(CheckAmdDeviceSupport(adapters));
        checks.Add(CheckReadiness(Query(probe.GetSystemReadiness)));
        checks.AddRange(CheckTool("DDU", request.DduPath, criteria.Ddu));
        checks.AddRange(CheckTool("AMD installer", request.AmdInstallerPath, criteria.AmdInstaller));

        return new PreflightResult(checks);
    }

    private static PreflightCheck Passed(string name, string detail) => new(name, CheckOutcome.Passed, detail);

    private static PreflightCheck Failed(string name, string detail) => new(name, CheckOutcome.Failed, detail);

    private static PreflightCheck Unverifiable(string name, string detail) => new(name, CheckOutcome.Unverifiable, detail);

    private static T? Query<T>(Func<T?> query)
        where T : class
    {
        try
        {
            return query();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            return null;
        }
    }

    private PreflightCheck CheckWindowsVersion(OperatingSystemInfo? operatingSystem)
    {
        const string Name = "Windows version and architecture";
        if (operatingSystem is null)
        {
            return Unverifiable(Name, "The Windows version and architecture could not be determined.");
        }

        Version supported = criteria.SupportedWindowsVersion;
        if (operatingSystem.Version.Major != supported.Major || operatingSystem.Version.Minor != supported.Minor
            || operatingSystem.Architecture != criteria.SupportedArchitecture)
        {
            return Failed(Name, $"Detected Windows {operatingSystem.Version} ({operatingSystem.Architecture}); supported is Windows 10 or 11 (version {supported.Major}.{supported.Minor}) on {criteria.SupportedArchitecture}.");
        }

        return Passed(Name, $"Windows {operatingSystem.Version} ({operatingSystem.Architecture}) is a supported version and architecture.");
    }

    private PreflightCheck CheckWindowsEdition(OperatingSystemInfo? operatingSystem)
    {
        const string Name = "Windows edition";
        if (criteria.SupportedWindowsEditions is null)
        {
            return Unverifiable(Name, UndocumentedPrefix + "Supported Windows editions need to be documented.");
        }

        if (string.IsNullOrWhiteSpace(operatingSystem?.Edition))
        {
            return Unverifiable(Name, "The Windows edition could not be determined.");
        }

        return criteria.SupportedWindowsEditions.Contains(operatingSystem.Edition, StringComparer.OrdinalIgnoreCase)
            ? Passed(Name, $"Windows edition '{operatingSystem.Edition}' is supported.")
            : Failed(Name, $"Windows edition '{operatingSystem.Edition}' is not supported.");
    }

    private static PreflightCheck CheckAmdAdapterPresent(IReadOnlyList<DisplayAdapterInfo>? adapters)
    {
        const string Name = "AMD display adapter";
        if (adapters is null)
        {
            return Unverifiable(Name, "The installed display adapters could not be enumerated.");
        }

        int count = adapters.Count(IsAmd);
        return count > 0
            ? Passed(Name, $"Detected {count.ToString(CultureInfo.InvariantCulture)} AMD display adapter(s).")
            : Failed(Name, "No AMD display adapter was detected.");
    }

    private PreflightCheck CheckAmdDeviceSupport(IReadOnlyList<DisplayAdapterInfo>? adapters)
    {
        const string Name = "AMD device support";
        if (criteria.SupportedAmdDeviceIds is null)
        {
            return Unverifiable(Name, UndocumentedPrefix + "Supported AMD devices and driver prerequisites need to be documented.");
        }

        if (adapters is null)
        {
            return Unverifiable(Name, "The installed display adapters could not be enumerated.");
        }

        List<DisplayAdapterInfo> amdAdapters = [.. adapters.Where(IsAmd)];
        if (amdAdapters.Count == 0)
        {
            return Failed(Name, "No AMD display adapter was detected.");
        }

        if (amdAdapters.Any(adapter => adapter.DeviceId is null))
        {
            return Unverifiable(Name, "The device ID of a detected AMD display adapter could not be determined.");
        }

        string[] unsupported = [.. amdAdapters
            .Where(adapter => !criteria.SupportedAmdDeviceIds.Contains(adapter.DeviceId!.Value))
            .Select(adapter => $"0x{adapter.DeviceId!.Value:X4}")];
        return unsupported.Length == 0
            ? Passed(Name, "Every detected AMD display adapter is a supported device.")
            : Failed(Name, $"Unsupported AMD device ID(s): {string.Join(", ", unsupported)}.");
    }

    private PreflightCheck CheckReadiness(SystemReadinessInfo? readiness)
    {
        const string Name = "System readiness";
        if (criteria.Readiness is null)
        {
            return Unverifiable(Name, UndocumentedPrefix + "System-readiness requirements need to be documented.");
        }

        if (criteria.Readiness.RequireElevatedProcess)
        {
            if (readiness?.IsElevated is not bool isElevated)
            {
                return Unverifiable(Name, "Whether the process is elevated could not be determined.");
            }

            if (!isElevated)
            {
                return Failed(Name, "The process is not elevated; run it as an administrator.");
            }
        }

        return Passed(Name, "The documented system-readiness requirements are met.");
    }

    private IEnumerable<PreflightCheck> CheckTool(string tool, string? path, ToolCriteria toolCriteria)
    {
        string fileName = $"{tool} file";
        PreflightCheck? pathProblem = ValidatePath(fileName, tool, path);
        if (pathProblem is not null)
        {
            yield return pathProblem;
            yield break;
        }

        FileInspection inspection = InspectFile(path!);
        PreflightCheck access = CheckAccess(fileName, tool, inspection.Access);
        yield return access;
        if (!access.Passed)
        {
            yield break;
        }

        yield return CheckVersion($"{tool} version", tool, inspection, toolCriteria);
        yield return CheckAuthenticity($"{tool} authenticity", tool, inspection.Signature, toolCriteria);
    }

    private static PreflightCheck? ValidatePath(string name, string tool, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Failed(name, $"No {tool} path was supplied.");
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0
            || path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || !Path.IsPathFullyQualified(path))
        {
            return Failed(name, $"The {tool} path must be a fully qualified local path (not relative, UNC, or a device path).");
        }

        if (!string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return Failed(name, $"The {tool} path must point to an .exe file.");
        }

        return null;
    }

    private FileInspection InspectFile(string path)
    {
        try
        {
            return probe.InspectFile(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            return FileInspection.Unavailable;
        }
    }

    private static PreflightCheck CheckAccess(string name, string tool, FileAccessState access) => access switch
    {
        FileAccessState.Readable => Passed(name, $"The {tool} file exists and is readable."),
        FileAccessState.NotFound => Failed(name, $"The {tool} file was not found."),
        FileAccessState.NotAFile => Failed(name, $"The {tool} path is not a file."),
        FileAccessState.AccessDenied => Failed(name, $"The {tool} file cannot be read; check its permissions."),
        _ => Unverifiable(name, $"The {tool} file could not be inspected."),
    };

    private static PreflightCheck CheckVersion(string name, string tool, FileInspection inspection, ToolCriteria toolCriteria)
    {
        if (toolCriteria.SupportedVersions is null)
        {
            return Unverifiable(name, UndocumentedPrefix + $"Supported {tool} versions need to be documented.");
        }

        if (string.IsNullOrWhiteSpace(inspection.Version))
        {
            return Unverifiable(name, $"The {tool} file version could not be read.");
        }

        return toolCriteria.SupportedVersions.Contains(inspection.Version, StringComparer.Ordinal)
            ? Passed(name, $"{tool} version {inspection.Version} is supported.")
            : Failed(name, $"{tool} version {inspection.Version} is not a supported version.");
    }

    private static PreflightCheck CheckAuthenticity(string name, string tool, FileSignature signature, ToolCriteria toolCriteria)
    {
        if (toolCriteria.TrustedSigners is null)
        {
            return Unverifiable(name, UndocumentedPrefix + $"Authenticity criteria for {tool} need to be documented.");
        }

        return signature.State switch
        {
            SignatureState.Unverified => Unverifiable(name, $"The {tool} signature could not be verified."),
            SignatureState.NotSigned => Failed(name, $"The {tool} file is not signed."),
            SignatureState.Invalid => Failed(name, $"The {tool} file has an invalid signature."),
            _ => !string.IsNullOrWhiteSpace(signature.Signer) && toolCriteria.TrustedSigners.Contains(signature.Signer, StringComparer.OrdinalIgnoreCase)
                ? Passed(name, $"The {tool} file has a valid signature from a trusted signer.")
                : Failed(name, $"The {tool} file is signed by an untrusted or unknown signer."),
        };
    }

    private static bool IsAmd(DisplayAdapterInfo adapter) => adapter.VendorId == PreflightCriteria.AmdVendorId;
}
