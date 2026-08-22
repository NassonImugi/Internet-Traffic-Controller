using System;
using System.Diagnostics;

namespace InternetTrafficController.Services;

public class MtuService
{
    public void SetMtu(string adapterName, int mtu)
    {
        RunOrThrow(
            $"interface ipv4 set subinterface \"{adapterName}\" mtu={mtu} store=persistent");
    }

    private static void RunOrThrow(string arguments)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "netsh.exe",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string message =
                string.IsNullOrWhiteSpace(error)
                    ? output
                    : error;

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(message)
                    ? $"netsh failed with exit code {process.ExitCode}."
                    : message.Trim());
        }
    }
}