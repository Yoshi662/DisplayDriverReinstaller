using System.Runtime.InteropServices;
using DisplayDriverReinstaller.Steps.Preflight;

namespace DisplayDriverReinstaller.Tests.Steps.Preflight;

public class PreflightStepTests
{
    private static readonly string DduPath = Path.Combine(Path.GetTempPath(), "ddu-fixture", "ddu.exe");
    private static readonly string InstallerPath = Path.Combine(Path.GetTempPath(), "amd-fixture", "installer.exe");

    [Fact]
    public void AllRequiredChecksPassingPasses()
    {
        PreflightResult result = Run(new FakeProbe(), FullCriteria());

        Assert.True(result.Passed);
        Assert.Empty(result.Failures);
        result.EnsurePassed();
    }

    [Fact]
    public void DefaultDocumentedCriteriaCannotPassBecauseVendorCriteriaAreUndocumented()
    {
        PreflightResult result = Run(new FakeProbe(), PreflightCriteria.Documented);

        Assert.False(result.Passed);
        Assert.Contains(result.Checks, check => check.Name == "Windows version and architecture" && check.Passed);
        Assert.All(
            result.Failures,
            check => Assert.Equal(CheckOutcome.Unverifiable, check.Outcome));
        Assert.Contains(result.Failures, check => check.Name == "DDU version");
        Assert.Contains(result.Failures, check => check.Name == "AMD installer authenticity");
        Assert.Contains(result.Failures, check => check.Name == "Windows edition");
        Assert.Contains(result.Failures, check => check.Name == "AMD device support");
        Assert.Contains(result.Failures, check => check.Name == "System readiness");
    }

    [Fact]
    public void FailedResultCannotAdvanceTheWorkflow()
    {
        PreflightResult result = Run(new FakeProbe { Os = new OperatingSystemInfo(new Version(6, 1, 7601), Architecture.X64, "Professional") }, FullCriteria());

        PreflightFailedException exception = Assert.Throws<PreflightFailedException>(result.EnsurePassed);
        Assert.Same(result, exception.Result);
    }

    [Fact]
    public void EmptyResultIsNotAPass()
    {
        Assert.False(new PreflightResult([]).Passed);
    }

    [Theory]
    [InlineData(10, 0, Architecture.Arm64)]
    [InlineData(10, 0, Architecture.X86)]
    [InlineData(6, 3, Architecture.X64)]
    public void UnsupportedWindowsVersionOrArchitectureFails(int major, int minor, Architecture architecture)
    {
        FakeProbe probe = new() { Os = new OperatingSystemInfo(new Version(major, minor, 0), architecture, "Professional") };

        AssertOnlyFailure(Run(probe, FullCriteria()), "Windows version and architecture", CheckOutcome.Failed);
    }

    [Fact]
    public void UnavailableSystemInformationIsUnverifiable()
    {
        PreflightResult result = Run(new FakeProbe { Os = null, Adapters = null, Readiness = null }, FullCriteria());

        Assert.False(result.Passed);
        Assert.All(
            result.Failures,
            check => Assert.Equal(CheckOutcome.Unverifiable, check.Outcome));
        Assert.Contains(result.Failures, check => check.Name == "Windows version and architecture");
        Assert.Contains(result.Failures, check => check.Name == "AMD display adapter");
        Assert.Contains(result.Failures, check => check.Name == "System readiness");
    }

    [Fact]
    public void ProbeExceptionsAreUnverifiableRatherThanPassing()
    {
        PreflightResult result = Run(new FakeProbe { ThrowOnQuery = true }, FullCriteria());

        Assert.False(result.Passed);
        Assert.Contains(result.Failures, check => check.Name == "Windows version and architecture" && check.Outcome == CheckOutcome.Unverifiable);
        Assert.Contains(result.Failures, check => check.Name == "DDU file" && check.Outcome == CheckOutcome.Unverifiable);
    }

    [Fact]
    public void UnsupportedWindowsEditionFails()
    {
        FakeProbe probe = new() { Os = new OperatingSystemInfo(new Version(10, 0, 22631), Architecture.X64, "ServerStandard") };

        AssertOnlyFailure(Run(probe, FullCriteria()), "Windows edition", CheckOutcome.Failed);
    }

    [Fact]
    public void MissingAmdAdapterFails()
    {
        FakeProbe probe = new() { Adapters = [new DisplayAdapterInfo(0x10DE, 0x2684, "Other vendor", "1.0")] };

        PreflightResult result = Run(probe, FullCriteria());

        Assert.Contains(result.Failures, check => check.Name == "AMD display adapter" && check.Outcome == CheckOutcome.Failed);
        Assert.Contains(result.Failures, check => check.Name == "AMD device support" && check.Outcome == CheckOutcome.Failed);
    }

    [Fact]
    public void UnsupportedAmdDeviceFailsAndNamesTheDevice()
    {
        FakeProbe probe = new() { Adapters = [new DisplayAdapterInfo(0x1002, 0x1234, "Unknown AMD", "1.0")] };

        PreflightCheck failure = AssertOnlyFailure(Run(probe, FullCriteria()), "AMD device support", CheckOutcome.Failed);

        Assert.Contains("0x1234", failure.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void AmdAdapterWithUnknownDeviceIdIsUnverifiable()
    {
        FakeProbe probe = new() { Adapters = [new DisplayAdapterInfo(0x1002, null, "AMD", "1.0")] };

        AssertOnlyFailure(Run(probe, FullCriteria()), "AMD device support", CheckOutcome.Unverifiable);
    }

    [Fact]
    public void NonElevatedProcessFailsWhenElevationIsRequired()
    {
        FakeProbe probe = new() { Readiness = new SystemReadinessInfo(false) };

        AssertOnlyFailure(Run(probe, FullCriteria()), "System readiness", CheckOutcome.Failed);
    }

    [Fact]
    public void UnknownElevationIsUnverifiable()
    {
        FakeProbe probe = new() { Readiness = new SystemReadinessInfo(null) };

        AssertOnlyFailure(Run(probe, FullCriteria()), "System readiness", CheckOutcome.Unverifiable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingToolPathsFail(string? path)
    {
        PreflightResult result = new PreflightStep(new FakeProbe(), FullCriteria()).Run(new PreflightRequest(path, path));

        Assert.Contains(result.Failures, check => check.Name == "DDU file" && check.Outcome == CheckOutcome.Failed);
        Assert.Contains(result.Failures, check => check.Name == "AMD installer file" && check.Outcome == CheckOutcome.Failed);
        Assert.DoesNotContain(result.Checks, check => check.Name == "DDU version");
    }

    [Theory]
    [InlineData("ddu.exe")]
    [InlineData(@"..\ddu.exe")]
    [InlineData(@"\\server\share\ddu.exe")]
    [InlineData("//server/share/ddu.exe")]
    [InlineData(@"\\?\C:\ddu.exe")]
    public void NonLocalOrRelativePathsFailWithoutInspectingTheFile(string path)
    {
        FakeProbe probe = new();

        PreflightResult result = new PreflightStep(probe, FullCriteria()).Run(new PreflightRequest(path, InstallerPath));

        Assert.Contains(result.Failures, check => check.Name == "DDU file" && check.Outcome == CheckOutcome.Failed);
        Assert.DoesNotContain(DduPath, probe.InspectedPaths);
        Assert.DoesNotContain(path, probe.InspectedPaths);
    }

    [Fact]
    public void NonExecutableExtensionFails()
    {
        PreflightResult result = new PreflightStep(new FakeProbe(), FullCriteria())
            .Run(new PreflightRequest(Path.Combine(Path.GetTempPath(), "ddu.zip"), InstallerPath));

        Assert.Contains(result.Failures, check => check.Name == "DDU file" && check.Detail.Contains(".exe", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(FileAccessState.NotFound, CheckOutcome.Failed)]
    [InlineData(FileAccessState.NotAFile, CheckOutcome.Failed)]
    [InlineData(FileAccessState.AccessDenied, CheckOutcome.Failed)]
    [InlineData(FileAccessState.Unknown, CheckOutcome.Unverifiable)]
    public void InaccessibleToolFileIsReported(FileAccessState access, CheckOutcome expected)
    {
        FakeProbe probe = new();
        probe.Files[InstallerPath] = new FileInspection(access, null, new FileSignature(SignatureState.Unverified, null));

        PreflightResult result = Run(probe, FullCriteria());

        PreflightCheck failure = AssertOnlyFailure(result, "AMD installer file", expected);
        Assert.DoesNotContain(InstallerPath, failure.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Checks, check => check.Name == "AMD installer version");
    }

    [Fact]
    public void UnsupportedToolVersionFails()
    {
        FakeProbe probe = new();
        probe.Files[DduPath] = ValidFile("1.2.3.4");

        AssertOnlyFailure(Run(probe, FullCriteria()), "DDU version", CheckOutcome.Failed);
    }

    [Fact]
    public void UnreadableToolVersionIsUnverifiable()
    {
        FakeProbe probe = new();
        probe.Files[DduPath] = ValidFile(null);

        AssertOnlyFailure(Run(probe, FullCriteria()), "DDU version", CheckOutcome.Unverifiable);
    }

    [Theory]
    [InlineData(SignatureState.Unverified, null, CheckOutcome.Unverifiable)]
    [InlineData(SignatureState.NotSigned, null, CheckOutcome.Failed)]
    [InlineData(SignatureState.Invalid, "Trusted Vendor", CheckOutcome.Failed)]
    [InlineData(SignatureState.Valid, "Someone Else", CheckOutcome.Failed)]
    [InlineData(SignatureState.Valid, null, CheckOutcome.Failed)]
    public void AuthenticityProblemsFailTheCheck(SignatureState state, string? signer, CheckOutcome expected)
    {
        FakeProbe probe = new();
        probe.Files[InstallerPath] = new FileInspection(FileAccessState.Readable, "2.0", new FileSignature(state, signer));

        AssertOnlyFailure(Run(probe, FullCriteria()), "AMD installer authenticity", expected);
    }

    [Fact]
    public void ProbeIsNeverAskedToDoAnythingBeyondReadOnlyQueries()
    {
        FakeProbe probe = new();

        Run(probe, FullCriteria());

        Assert.Equal([DduPath, InstallerPath], probe.InspectedPaths);
    }

    private static PreflightResult Run(IPreflightProbe probe, PreflightCriteria criteria)
        => new PreflightStep(probe, criteria).Run(new PreflightRequest(DduPath, InstallerPath));

    private static PreflightCheck AssertOnlyFailure(PreflightResult result, string name, CheckOutcome outcome)
    {
        Assert.False(result.Passed);
        PreflightCheck failure = Assert.Single(result.Failures);
        Assert.Equal(name, failure.Name);
        Assert.Equal(outcome, failure.Outcome);
        return failure;
    }

    private static FileInspection ValidFile(string? version)
        => new(FileAccessState.Readable, version, new FileSignature(SignatureState.Valid, "Trusted Vendor"));

    private static PreflightCriteria FullCriteria() => new()
    {
        SupportedWindowsEditions = ["Professional", "Core"],
        SupportedAmdDeviceIds = [0x73BF],
        Readiness = new ReadinessCriteria(RequireElevatedProcess: true),
        Ddu = new ToolCriteria { SupportedVersions = ["1.0"], TrustedSigners = ["Trusted Vendor"] },
        AmdInstaller = new ToolCriteria { SupportedVersions = ["2.0"], TrustedSigners = ["Trusted Vendor"] },
    };

    private sealed class FakeProbe : IPreflightProbe
    {
        public OperatingSystemInfo? Os { get; set; } = new(new Version(10, 0, 22631), Architecture.X64, "Professional");

        public IReadOnlyList<DisplayAdapterInfo>? Adapters { get; set; } = [new DisplayAdapterInfo(0x1002, 0x73BF, "AMD GPU", "1.0")];

        public SystemReadinessInfo? Readiness { get; set; } = new(true);

        public bool ThrowOnQuery { get; set; }

        public Dictionary<string, FileInspection> Files { get; } = new()
        {
            [DduPath] = ValidFile("1.0"),
            [InstallerPath] = ValidFile("2.0"),
        };

        public List<string> InspectedPaths { get; } = [];

        public OperatingSystemInfo? GetOperatingSystem() => ThrowOnQuery ? throw new InvalidOperationException() : Os;

        public IReadOnlyList<DisplayAdapterInfo>? GetDisplayAdapters() => ThrowOnQuery ? throw new InvalidOperationException() : Adapters;

        public SystemReadinessInfo? GetSystemReadiness() => ThrowOnQuery ? throw new InvalidOperationException() : Readiness;

        public FileInspection InspectFile(string path)
        {
            InspectedPaths.Add(path);
            return ThrowOnQuery
                ? throw new UnauthorizedAccessException()
                : Files.TryGetValue(path, out FileInspection? inspection) ? inspection : FileInspection.Unavailable;
        }
    }
}
