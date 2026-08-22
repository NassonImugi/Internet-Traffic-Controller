using System;
using System.Diagnostics;

namespace InternetTrafficController.Services;

public class DnsManagerService
{
    public void SetDns(
        string adapter,
        string primary,
        string secondary)
    {
        RunOrThrow(
            $"interface ipv4 set dnsservers name=\"{adapter}\" static {primary} primary");

        RunOrThrow(
            $"interface ipv4 add dnsservers name=\"{adapter}\" {secondary} index=2");
    }

    public void ResetDns(string adapter)
    {
        RunOrThrow(
            $"interface ipv4 set dnsservers name=\"{adapter}\" source=dhcp");
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