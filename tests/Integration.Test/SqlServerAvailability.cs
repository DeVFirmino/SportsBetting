using System.Diagnostics;

namespace Integration.Test;

/// <summary>
/// These tests need a real SQL Server, which is started as a container. A machine without a
/// reachable container runtime cannot run them, and failing there would say nothing about the
/// code, so the tests report as skipped instead.
/// </summary>
internal static class SqlServerAvailability
{
    private static readonly Lazy<bool> Available = new(ProbeContainerRuntime);

    internal const string SkipReason =
        "No reachable container runtime, so the SQL Server container cannot be started.";

    internal static bool IsAvailable => Available.Value;

    private static bool ProbeContainerRuntime()
    {
        try
        {
            ProcessStartInfo startInfo = new("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using Process? process = Process.Start(startInfo);

            if (process is null)
                return false;

            if (process.WaitForExit(TimeSpan.FromSeconds(20)) is false)
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// A <see cref="FactAttribute"/> that skips itself when no container runtime is reachable.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (SqlServerAvailability.IsAvailable is false)
            Skip = SqlServerAvailability.SkipReason;
    }
}
