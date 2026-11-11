using DisplayDriverReinstaller.Platform.Windows;
using DisplayDriverReinstaller.Steps.Preflight;

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(
        "DisplayDriverReinstaller\n" +
        $"Usage: DisplayDriverReinstaller [--help] | {PreflightCommand.Usage}\n\n" +
        "preflight is a read-only check of prerequisites and the local DDU and AMD installer files.\n" +
        "Driver reinstallation is not implemented. No system changes will be made.");
    return 0;
}

if (args.Length > 0 && args[0] == "preflight")
{
    return PreflightCommand.Run(args[1..], new WindowsPreflightProbe(), PreflightCriteria.Documented, Console.Out, Console.Error);
}

Console.Error.WriteLine(
    "Driver reinstallation is not implemented. Use --help for more information. No system changes were made.");
return 1;
