namespace DisplayDriverReinstaller.Steps.Preflight;

/// <summary>The user-supplied local tool paths to validate. Null or empty values fail the corresponding checks.</summary>
public sealed record PreflightRequest(string? DduPath, string? AmdInstallerPath);

public enum CheckOutcome
{
    Passed,
    Failed,
    Unverifiable,
}

public sealed record PreflightCheck(string Name, CheckOutcome Outcome, string Detail)
{
    public bool Passed => Outcome == CheckOutcome.Passed;
}

/// <summary>
/// The outcome of the preflight step. It passes only when at least one check ran and every check passed.
/// A pass does not mean that a reinstall was performed or that one is guaranteed to succeed.
/// </summary>
public sealed record PreflightResult(IReadOnlyList<PreflightCheck> Checks)
{
    public bool Passed => Checks.Count > 0 && Checks.All(check => check.Passed);

    public IEnumerable<PreflightCheck> Failures => Checks.Where(check => !check.Passed);

    /// <summary>Gate for later lifecycle steps: throws unless the preflight passed.</summary>
    public void EnsurePassed()
    {
        if (!Passed)
        {
            throw new PreflightFailedException(this);
        }
    }
}

public sealed class PreflightFailedException : InvalidOperationException
{
    public PreflightFailedException(PreflightResult result)
        : base("Preflight did not pass; the workflow must not continue.")
    {
        Result = result;
    }

    public PreflightResult Result { get; }
}
