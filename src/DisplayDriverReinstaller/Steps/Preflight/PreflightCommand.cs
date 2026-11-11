namespace DisplayDriverReinstaller.Steps.Preflight;

/// <summary>
/// The read-only <c>preflight</c> command. Exit codes: 0 when preflight passed, 1 when it did not, 2 for invalid usage.
/// </summary>
public static class PreflightCommand
{
    public const string Usage = "preflight [--ddu <path>] [--amd-installer <path>]";

    public static int Run(IReadOnlyList<string> arguments, IPreflightProbe probe, PreflightCriteria criteria, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!TryParse(arguments, out PreflightRequest? request))
        {
            error.WriteLine($"Invalid arguments. Usage: {Usage}");
            error.WriteLine("No system changes were made.");
            return 2;
        }

        PreflightResult result = new PreflightStep(probe, criteria).Run(request);
        foreach (PreflightCheck check in result.Checks)
        {
            string label = check.Outcome switch
            {
                CheckOutcome.Passed => "PASS",
                CheckOutcome.Failed => "FAIL",
                _ => "UNVERIFIED",
            };
            output.WriteLine($"[{label}] {check.Name}: {check.Detail}");
        }

        if (result.Passed)
        {
            output.WriteLine("Preflight passed. This check is read-only: no reinstall was performed and a later reinstall is not guaranteed to succeed.");
            return 0;
        }

        error.WriteLine($"Preflight failed: {result.Failures.Count().ToString(System.Globalization.CultureInfo.InvariantCulture)} required check(s) need attention. The reinstall must not proceed. No system changes were made.");
        return 1;
    }

    private static bool TryParse(IReadOnlyList<string> arguments, out PreflightRequest request)
    {
        string? dduPath = null;
        string? installerPath = null;
        request = new PreflightRequest(null, null);

        for (int index = 0; index < arguments.Count; index += 2)
        {
            if (index + 1 >= arguments.Count)
            {
                return false;
            }

            switch (arguments[index])
            {
                case "--ddu" when dduPath is null:
                    dduPath = arguments[index + 1];
                    break;
                case "--amd-installer" when installerPath is null:
                    installerPath = arguments[index + 1];
                    break;
                default:
                    return false;
            }
        }

        request = new PreflightRequest(dduPath, installerPath);
        return true;
    }
}
