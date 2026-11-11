using System.Globalization;

namespace DisplayDriverReinstaller.Steps.Preflight;

public static class PciHardwareId
{
    /// <summary>Parses the vendor and device IDs from a PCI device ID such as <c>PCI\VEN_1002&amp;DEV_73BF&amp;...</c>.</summary>
    public static bool TryParse(string? deviceId, out int vendorId, out int deviceIdentifier)
    {
        vendorId = 0;
        deviceIdentifier = 0;
        return deviceId is not null
            && deviceId.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase)
            && TryReadHex(deviceId, "VEN_", out vendorId)
            && TryReadHex(deviceId, "DEV_", out deviceIdentifier);
    }

    private static bool TryReadHex(string value, string prefix, out int result)
    {
        result = 0;
        int start = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + prefix.Length + 4 > value.Length)
        {
            return false;
        }

        return int.TryParse(value.AsSpan(start + prefix.Length, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result);
    }
}
