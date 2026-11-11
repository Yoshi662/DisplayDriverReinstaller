using System.Runtime.InteropServices;
using DisplayDriverReinstaller.Steps.Preflight;

namespace DisplayDriverReinstaller.Tests.Steps.Preflight;

public class PreflightCommandTests
{
    [Theory]
    [InlineData(@"PCI\VEN_1002&DEV_73BF&SUBSYS_0E3A1002&REV_C1", 0x1002, 0x73BF)]
    [InlineData(@"pci\ven_10de&dev_2684&subsys_00000000", 0x10DE, 0x2684)]
    public void PciHardwareIdIsParsed(string deviceId, int vendor, int device)
    {
        Assert.True(PciHardwareId.TryParse(deviceId, out int parsedVendor, out int parsedDevice));
        Assert.Equal(vendor, parsedVendor);
        Assert.Equal(device, parsedDevice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"ROOT\BASICDISPLAY\0000")]
    [InlineData(@"PCI\VEN_10")]
    [InlineData(@"PCI\VEN_ZZZZ&DEV_73BF")]
    public void UnparseablePciHardwareIdIsRejected(string? deviceId)
    {
        Assert.False(PciHardwareId.TryParse(deviceId, out _, out _));
    }

    [Fact]
    public void FailingPreflightReportsEachCheckAndExitsNonZero()
    {
        (int exitCode, string output, string error) = Execute("--ddu", "relative.exe");

        Assert.Equal(1, exitCode);
        Assert.Contains("[FAIL] DDU file", output, StringComparison.Ordinal);
        Assert.Contains("[FAIL] AMD installer file", output, StringComparison.Ordinal);
        Assert.Contains("[UNVERIFIED] Windows edition", output, StringComparison.Ordinal);
        Assert.Contains("Preflight failed", error, StringComparison.Ordinal);
        Assert.Contains("No system changes were made", error, StringComparison.Ordinal);
    }

    [Fact]
    public void PassingPreflightDoesNotClaimAReinstallHappened()
    {
        string ddu = Path.Combine(Path.GetTempPath(), "ddu.exe");
        string installer = Path.Combine(Path.GetTempPath(), "installer.exe");

        (int exitCode, string output, string error) = Execute(["--ddu", ddu, "--amd-installer", installer], PassingProbeAndCriteria());

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Contains("Preflight passed", output, StringComparison.Ordinal);
        Assert.Contains("no reinstall was performed", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--ddu")]
    [InlineData("--unknown", "value")]
    [InlineData("reinstall")]
    [InlineData("--ddu", "a.exe", "--ddu", "b.exe")]
    public void InvalidArgumentsExitWithUsageError(params string[] arguments)
    {
        (int exitCode, string _, string error) = Execute(arguments);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", error, StringComparison.Ordinal);
        Assert.Contains("No system changes were made", error, StringComparison.Ordinal);
    }

    private static (int ExitCode, string Output, string Error) Execute(params string[] arguments)
        => Execute(arguments, (new UnavailableProbe(), PreflightCriteria.Documented));

    private static (int ExitCode, string Output, string Error) Execute(string[] arguments, (IPreflightProbe Probe, PreflightCriteria Criteria) setup)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = PreflightCommand.Run(arguments, setup.Probe, setup.Criteria, output, error);
        return (exitCode, output.ToString(), error.ToString());
    }

    private static (IPreflightProbe, PreflightCriteria) PassingProbeAndCriteria()
    {
        PreflightCriteria criteria = new()
        {
            SupportedWindowsEditions = ["Professional"],
            SupportedAmdDeviceIds = [0x73BF],
            Readiness = new ReadinessCriteria(RequireElevatedProcess: true),
            Ddu = new ToolCriteria { SupportedVersions = ["1.0"], TrustedSigners = ["Vendor"] },
            AmdInstaller = new ToolCriteria { SupportedVersions = ["1.0"], TrustedSigners = ["Vendor"] },
        };
        return (new PassingProbe(), criteria);
    }

    private sealed class UnavailableProbe : IPreflightProbe
    {
        public OperatingSystemInfo? GetOperatingSystem() => null;

        public IReadOnlyList<DisplayAdapterInfo>? GetDisplayAdapters() => null;

        public SystemReadinessInfo? GetSystemReadiness() => null;

        public FileInspection InspectFile(string path) => FileInspection.Unavailable;
    }

    private sealed class PassingProbe : IPreflightProbe
    {
        public OperatingSystemInfo? GetOperatingSystem() => new(new Version(10, 0, 22631), Architecture.X64, "Professional");

        public IReadOnlyList<DisplayAdapterInfo>? GetDisplayAdapters() => [new DisplayAdapterInfo(0x1002, 0x73BF, "AMD GPU", "1.0")];

        public SystemReadinessInfo? GetSystemReadiness() => new(true);

        public FileInspection InspectFile(string path)
            => new(FileAccessState.Readable, "1.0", new FileSignature(SignatureState.Valid, "Vendor"));
    }
}
