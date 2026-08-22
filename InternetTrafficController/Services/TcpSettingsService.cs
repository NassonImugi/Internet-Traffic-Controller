using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InternetTrafficController.Services;

public class TcpSettingsService
{
    public void SetAutoTuning(string mode)
    {
        RunOrThrow(
            $"int tcp set global autotuninglevel={mode}");
    }

    public void SetCongestionProvider(string provider)
    {
        RunOrThrow(
            $"int tcp set supplemental template=internet congestionprovider={provider}");
    }

    public void EnableRss(bool enabled)
    {
        RunOrThrow(
            $"int tcp set global rss={(enabled ? "enabled" : "disabled")}");
    }

    public void EnableEcn(bool enabled)
    {
        RunOrThrow(
            $"int tcp set global ecncapability={(enabled ? "enabled" : "disabled")}");
    }

    public TcpSettings GetCurrentSettings()
    {
        string output = RunAndRead("int tcp show global");

        var settings = new TcpSettings
        {
            AutoTuning = "normal",
            CongestionProvider = "ctcp",
            RssEnabled = true,
            EcnEnabled = false
        };

        foreach (string line in output.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries))
        {
            string normalized = line.Trim();

            if (normalized.StartsWith(
                "Receive Window Auto-Tuning Level",
                StringComparison.OrdinalIgnoreCase))
            {
                settings.AutoTuning = GetValue(normalized);
            }

            if (normalized.StartsWith(
                "Receive-Side Scaling State",
                StringComparison.OrdinalIgnoreCase))
            {
                settings.RssEnabled =
                    GetValue(normalized)
                    .Equals("enabled",
                        StringComparison.OrdinalIgnoreCase);
            }

            if (normalized.StartsWith(
                "ECN Capability",
                StringComparison.OrdinalIgnoreCase))
            {
                settings.EcnEnabled =
                    GetValue(normalized)
                    .Equals("enabled",
                        StringComparison.OrdinalIgnoreCase);
            }
        }

        string supplemental =
            RunAndRead(
                "int tcp show supplemental template=internet");

        foreach (string line in supplemental.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries))
        {
            string normalized = line.Trim();

            if (normalized.StartsWith(
                "Congestion Provider",
                StringComparison.OrdinalIgnoreCase))
            {
                settings.CongestionProvider =
                    GetValue(normalized);
            }
        }

        return settings;
    }

    private static string GetValue(string line)
    {
        int colon = line.IndexOf(':');

        if (colon >= 0)
            return line[(colon + 1)..].Trim();

        return "";
    }

    private static string RunAndRead(string arguments)
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
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? output
                    : error);
        }

        return output;
    }

    private static void RunOrThrow(string arguments)
    {
        RunAndRead(arguments);
    }
}

public class TcpSettings
{
    public string AutoTuning { get; set; } = "normal";

    public string CongestionProvider { get; set; } = "ctcp";

    public bool RssEnabled { get; set; } = true;

    public bool EcnEnabled { get; set; } = false;
}