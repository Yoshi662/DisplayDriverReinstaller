using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using DisplayDriverReinstaller.Steps.Preflight;
using Microsoft.Win32;

namespace DisplayDriverReinstaller.Platform.Windows;

/// <summary>
/// Read-only Windows implementation of <see cref="IPreflightProbe"/>. It only reads system information and
/// file metadata; it never starts either tool. Authenticode verification is not implemented, so signatures
/// are always reported as <see cref="SignatureState.Unverified"/>.
/// </summary>
public sealed class WindowsPreflightProbe : IPreflightProbe
{
    public OperatingSystemInfo? GetOperatingSystem()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        string? edition = null;
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            edition = key?.GetValue("EditionID") as string;
        }
        catch (Exception exception) when (exception is SecurityException or UnauthorizedAccessException or IOException)
        {
            // The edition stays unknown and is reported as unverifiable.
        }

        return new OperatingSystemInfo(Environment.OSVersion.Version, RuntimeInformation.OSArchitecture, edition);
    }

    public IReadOnlyList<DisplayAdapterInfo>? GetDisplayAdapters()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using ManagementObjectSearcher searcher = new("SELECT Name, PNPDeviceID, DriverVersion FROM Win32_VideoController");
            using ManagementObjectCollection results = searcher.Get();
            List<DisplayAdapterInfo> adapters = [];
            foreach (ManagementBaseObject result in results)
            {
                using (result)
                {
                    string? pnpDeviceId = result["PNPDeviceID"] as string;
                    int? vendorId = null;
                    int? deviceId = null;
                    if (PciHardwareId.TryParse(pnpDeviceId, out int vendor, out int device))
                    {
                        vendorId = vendor;
                        deviceId = device;
                    }

                    adapters.Add(new DisplayAdapterInfo(vendorId, deviceId, result["Name"] as string, result["DriverVersion"] as string));
                }
            }

            return adapters;
        }
        catch (Exception exception) when (exception is ManagementException or COMException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public SystemReadinessInfo? GetSystemReadiness()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new SystemReadinessInfo(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator));
    }

    public FileInspection InspectFile(string path)
    {
        FileAccessState access = GetAccess(path);
        string? version = null;
        if (access == FileAccessState.Readable)
        {
            try
            {
                version = FileVersionInfo.GetVersionInfo(path).FileVersion;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // The version stays unknown and is reported as unverifiable.
            }
        }

        return new FileInspection(access, version, new FileSignature(SignatureState.Unverified, null));
    }

    private static FileAccessState GetAccess(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                return FileAccessState.NotAFile;
            }

            if (!File.Exists(path))
            {
                return FileAccessState.NotFound;
            }

            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return FileAccessState.Readable;
        }
        catch (UnauthorizedAccessException)
        {
            return FileAccessState.AccessDenied;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or NotSupportedException or SecurityException)
        {
            return FileAccessState.Unknown;
        }
    }
}
