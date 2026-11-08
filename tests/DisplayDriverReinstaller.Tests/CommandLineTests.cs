using System.Diagnostics;

namespace DisplayDriverReinstaller.Tests;

public class CommandLineTests
{
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task HelpRequestedDescribesCurrentLimitations(string argument)
    {
        using Process process = StartApplication(argument);

        string output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
        Assert.Contains("not implemented", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData()]
    [InlineData("reinstall")]
    public async Task ReinstallUnavailableFailsWithoutClaimingSuccess(params string[] arguments)
    {
        using Process process = StartApplication(arguments);

        string error = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("not implemented", error, StringComparison.Ordinal);
        Assert.Contains("No system changes were made", error, StringComparison.Ordinal);
    }

    private static Process StartApplication(params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--runtimeconfig");
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DisplayDriverReinstaller.Tests.runtimeconfig.json"));
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DisplayDriverReinstaller.dll"));
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the CLI.");
    }
}
