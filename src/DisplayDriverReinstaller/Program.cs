if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(
        "DisplayDriverReinstaller\n" +
        "Usage: DisplayDriverReinstaller [--help]\n\n" +
        "Driver reinstallation is not implemented. No system changes will be made.");
    return 0;
}

Console.Error.WriteLine(
    "Driver reinstallation is not implemented. Use --help for more information. No system changes were made.");
return 1;
