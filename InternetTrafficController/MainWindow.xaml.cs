using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using InternetTrafficController.Services;
using InternetTrafficController.Views;

namespace InternetTrafficController;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer trafficTimer;

    private long lastBytesReceived;
    private long lastBytesSent;

    private long totalReceived;
    private long totalSent;

    private string? monitoredAdapterId;

    private readonly ProfileService profileService = new();
    private readonly ProfileApplyService profileApplyService = new();
    private readonly DnsManagerService dnsService = new();
    private readonly MtuService mtuService = new();
    private readonly TcpSettingsService tcpService = new();

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        trafficTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        trafficTimer.Tick += TrafficTimer_Tick;
    }

    private void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        CheckAdministrator();
        LoadAdapters();
        LoadProfiles();
        ConfigureDnsPresets();

        trafficTimer.Start();

        StatusBarTextBlock.Text =
            "Internet Traffic Controller ready.";
    }

    private void CheckAdministrator()
    {
        bool admin =
            new WindowsPrincipal(
                WindowsIdentity.GetCurrent())
            .IsInRole(
                WindowsBuiltInRole.Administrator);

        AdministratorTextBlock.Text =
            admin
                ? "Administrator"
                : "Standard User";
    }

    private void LoadAdapters(
        string? preferredAdapterId = null)
    {
        string? adapterToRestore =
            preferredAdapterId ??
            CurrentAdapter()?.Id;

        var adapters =
            NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x =>
                x.NetworkInterfaceType !=
                NetworkInterfaceType.Loopback)
            .OrderBy(x => x.Name)
            .ToList();

        AdapterComboBox.SelectionChanged -=
            AdapterComboBox_SelectionChanged;

        AdapterComboBox.Items.Clear();

        foreach (var adapter in adapters)
        {
            AdapterComboBox.Items.Add(
                new ComboBoxItem
                {
                    Content = adapter.Name,
                    Tag = adapter.Id
                });
        }

        int selectedIndex =
            adapters.FindIndex(
                x => x.Id == adapterToRestore);

        if (selectedIndex >= 0)
        {
            AdapterComboBox.SelectedIndex =
                selectedIndex;
        }
        else
        {
            int wifiIndex =
                adapters.FindIndex(x =>
                    x.NetworkInterfaceType ==
                    NetworkInterfaceType.Wireless80211 &&
                    x.OperationalStatus ==
                    OperationalStatus.Up);

            int activeIndex =
                adapters.FindIndex(x =>
                    x.OperationalStatus ==
                    OperationalStatus.Up);

            if (wifiIndex >= 0)
                AdapterComboBox.SelectedIndex =
                    wifiIndex;
            else if (activeIndex >= 0)
                AdapterComboBox.SelectedIndex =
                    activeIndex;
            else if (AdapterComboBox.Items.Count > 0)
                AdapterComboBox.SelectedIndex = 0;
        }

        AdapterComboBox.SelectionChanged +=
            AdapterComboBox_SelectionChanged;

        UpdateAdapterInfo();
        ResetTrafficBaseline();
    }

    private void AdapterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
            return;

        monitoredAdapterId = adapter.Id;

        UpdateAdapterInfo();
        ResetTrafficBaseline();

        StatusBarTextBlock.Text =
            $"Selected adapter: {adapter.Name}";
    }

    private NetworkInterface? CurrentAdapter()
    {
        if (AdapterComboBox.SelectedItem
            is not ComboBoxItem item)
        {
            return null;
        }

        string? id = item.Tag?.ToString();

        return NetworkInterface
            .GetAllNetworkInterfaces()
            .FirstOrDefault(x => x.Id == id);
    }

    private void UpdateAdapterInfo()
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
            return;

        var properties =
            adapter.GetIPProperties();

        StatusTextBox.Text =
            adapter.OperationalStatus.ToString();

        MediaStateTextBox.Text =
            adapter.OperationalStatus ==
            OperationalStatus.Up
                ? "Connected"
                : "Disconnected";

        LinkSpeedTextBox.Text =
            adapter.Speed > 0
                ? $"{adapter.Speed / 1_000_000.0:F0} Mbps"
                : "-";

        MacAddressTextBox.Text =
            FormatMacAddress(
                adapter.GetPhysicalAddress()
                .ToString());

        IPv4TextBox.Text =
            properties.UnicastAddresses
            .FirstOrDefault(x =>
                x.Address.AddressFamily ==
                AddressFamily.InterNetwork)
            ?.Address.ToString()
            ?? "-";

        IPv6TextBox.Text =
            properties.UnicastAddresses
            .FirstOrDefault(x =>
                x.Address.AddressFamily ==
                AddressFamily.InterNetworkV6)
            ?.Address.ToString()
            ?? "-";

        GatewayTextBox.Text =
            string.Join(", ",
                properties.GatewayAddresses
                .Select(x =>
                    x.Address.ToString()));

        if (string.IsNullOrWhiteSpace(
            GatewayTextBox.Text))
        {
            GatewayTextBox.Text = "-";
        }

        DnsServersTextBox.Text =
            string.Join(", ",
                properties.DnsAddresses
                .Select(x => x.ToString()));

        if (string.IsNullOrWhiteSpace(
            DnsServersTextBox.Text))
        {
            DnsServersTextBox.Text = "-";
        }

        DriverTextBox.Text =
            adapter.Description;

        ProviderTextBox.Text =
            adapter.NetworkInterfaceType.ToString();

        DriverDateTextBox.Text = "-";
        DriverVersionTextBox.Text = "-";
    }

    private void ResetTrafficBaseline()
    {
        var adapter = CurrentAdapter();

        lastBytesReceived = 0;
        lastBytesSent = 0;

        monitoredAdapterId = adapter?.Id;

        if (adapter == null)
            return;

        try
        {
            var stats =
                adapter.GetIPv4Statistics();

            lastBytesReceived =
                stats.BytesReceived;

            lastBytesSent =
                stats.BytesSent;

            DownloadSpeedTextBox.Text =
                "0.00 Mbps";

            UploadSpeedTextBox.Text =
                "0.00 Mbps";

            DownloadProgressBar.Value = 0;
            UploadProgressBar.Value = 0;
        }
        catch
        {
        }
    }

    private void TrafficTimer_Tick(
        object? sender,
        EventArgs e)
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
            return;

        try
        {
            if (adapter.Id != monitoredAdapterId)
            {
                ResetTrafficBaseline();
                return;
            }

            var stats =
                adapter.GetIPv4Statistics();

            long received =
                stats.BytesReceived;

            long sent =
                stats.BytesSent;

            long downBytes =
                Math.Max(
                    0,
                    received -
                    lastBytesReceived);

            long upBytes =
                Math.Max(
                    0,
                    sent -
                    lastBytesSent);

            lastBytesReceived =
                received;

            lastBytesSent =
                sent;

            totalReceived += downBytes;
            totalSent += upBytes;

            double downMbps =
                downBytes * 8.0 /
                1_000_000.0;

            double upMbps =
                upBytes * 8.0 /
                1_000_000.0;

            DownloadSpeedTextBox.Text =
                $"{downMbps:F2} Mbps";

            UploadSpeedTextBox.Text =
                $"{upMbps:F2} Mbps";

            DownloadProgressBar.Value =
                Math.Min(downMbps, 100);

            UploadProgressBar.Value =
                Math.Min(upMbps, 100);

            TotalDownloadedTextBlock.Text =
                FormatBytes(totalReceived);

            TotalUploadedTextBlock.Text =
                FormatBytes(totalSent);
        }
        catch
        {
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] units =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        double value =
            Math.Max(0, bytes);

        int index = 0;

        while (value >= 1024 &&
               index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value:F2} {units[index]}";
    }

    private string FormatMacAddress(
        string mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            return "-";

        return string.Join(
            ":",
            Enumerable.Range(
                0,
                mac.Length / 2)
            .Select(i =>
                mac.Substring(i * 2, 2)));
    }

    private void LoadProfiles()
    {
        ProfileComboBox.Items.Clear();

        foreach (var profile in
            profileService.LoadProfiles())
        {
            ProfileComboBox.Items.Add(
                profile.Name);
        }

        if (ProfileComboBox.Items.Count > 0)
            ProfileComboBox.SelectedIndex = 0;
    }

    private void ConfigureDnsPresets()
    {
        DnsPresetComboBox.SelectionChanged -=
            DnsPresetComboBox_SelectionChanged;

        DnsPresetComboBox.SelectionChanged +=
            DnsPresetComboBox_SelectionChanged;

        ApplySelectedDnsPreset();
    }

    private void DnsPresetComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ApplySelectedDnsPreset();
    }

    private void ApplySelectedDnsPreset()
    {
        string preset =
            GetSelectedComboBoxText(
                DnsPresetComboBox);

        switch (preset)
        {
            case "Cloudflare DNS":

                PrimaryDnsTextBox.Text =
                    "1.1.1.1";

                SecondaryDnsTextBox.Text =
                    "1.0.0.1";

                break;


            case "Google DNS":

                PrimaryDnsTextBox.Text =
                    "8.8.8.8";

                SecondaryDnsTextBox.Text =
                    "8.8.4.4";

                break;


            case "Quad9 DNS":

                PrimaryDnsTextBox.Text =
                    "9.9.9.9";

                SecondaryDnsTextBox.Text =
                    "149.112.112.112";

                break;


            case "Automatic (DHCP)":

                PrimaryDnsTextBox.Text =
                    "";

                SecondaryDnsTextBox.Text =
                    "";

                break;
        }
    }

    private string GetSelectedComboBoxText(
        ComboBox comboBox)
    {
        return
            (comboBox.SelectedItem
            as ComboBoxItem)
            ?.Content?.ToString()
            ?? comboBox.SelectedItem?.ToString()
            ?? "";
    }

    private void ApplyProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string? name =
            ProfileComboBox.SelectedItem?
            .ToString();

        var adapter = CurrentAdapter();

        if (string.IsNullOrWhiteSpace(name) ||
            adapter == null)
        {
            return;
        }

        var profile =
            profileService.LoadProfiles()
            .FirstOrDefault(x =>
                x.Name == name);

        if (profile == null)
            return;

        try
        {
            profileApplyService.Apply(
                profile,
                adapter.Name);

            PrimaryDnsTextBox.Text =
                profile.DnsPrimary;

            SecondaryDnsTextBox.Text =
                profile.DnsSecondary;

            StatusBarTextBlock.Text =
                $"Applied {profile.Name} profile.";
        }
        catch (Exception ex)
        {
            ShowError(
                "Profile Error",
                ex.Message);
        }
    }

    private void ApplyDnsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
            return;

        string preset =
            GetSelectedComboBoxText(
                DnsPresetComboBox);

        try
        {
            if (preset == "Automatic (DHCP)")
            {
                dnsService.ResetDns(adapter.Name);

                PrimaryDnsTextBox.Text = "";
                SecondaryDnsTextBox.Text = "";

                StatusBarTextBlock.Text =
                    $"Automatic DNS restored for {adapter.Name}.";
            }
            else
            {
                string primary =
                    PrimaryDnsTextBox.Text.Trim();

                string secondary =
                    SecondaryDnsTextBox.Text.Trim();

                if (!IPAddress.TryParse(
                        primary,
                        out _) ||
                    !IPAddress.TryParse(
                        secondary,
                        out _))
                {
                    ShowError(
                        "DNS Error",
                        "Enter valid primary and secondary DNS addresses.");

                    return;
                }

                dnsService.SetDns(
                    adapter.Name,
                    primary,
                    secondary);

                StatusBarTextBlock.Text =
                    $"DNS applied to {adapter.Name}.";
            }

            UpdateAdapterInfo();
        }
        catch (Exception ex)
        {
            ShowError(
                "DNS Error",
                ex.Message);
        }
    }

    private void RestoreDefaultsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
        {
            ShowError(
                "Restore Defaults",
                "Select a network adapter first.");

            return;
        }

        var result =
            MessageBox.Show(
                "This will restore DNS to automatic (DHCP), set MTU to 1500, and restore the recommended TCP defaults.\n\nContinue?",
                "Restore Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            dnsService.ResetDns(adapter.Name);

            mtuService.SetMtu(
                adapter.Name,
                1500);

            tcpService.SetAutoTuning(
                "normal");

            tcpService.EnableRss(true);

            tcpService.EnableEcn(false);

            PrimaryDnsTextBox.Text = "";
            SecondaryDnsTextBox.Text = "";

            SelectComboBoxValue(
                DnsPresetComboBox,
                "Automatic (DHCP)");

            UpdateAdapterInfo();

            StatusBarTextBlock.Text =
                "Network defaults restored successfully.";

            MessageBox.Show(
                "Defaults restored successfully.",
                "Internet Traffic Controller",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(
                "Restore Defaults Error",
                ex.Message);
        }
    }

    private void SelectComboBoxValue(
        ComboBox comboBox,
        string value)
    {
        foreach (ComboBoxItem item
            in comboBox.Items
            .OfType<ComboBoxItem>())
        {
            if (string.Equals(
                item.Content?.ToString(),
                value,
                StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void ResetCountersButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        totalReceived = 0;
        totalSent = 0;

        TotalDownloadedTextBlock.Text =
            "0.00 B";

        TotalUploadedTextBlock.Text =
            "0.00 B";

        ResetTrafficBaseline();

        StatusBarTextBlock.Text =
            "Traffic counters reset.";
    }

    private void ReinstallDriverButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string? currentId =
            CurrentAdapter()?.Id;

        LoadAdapters(currentId);

        StatusBarTextBlock.Text =
            "Adapter list refreshed.";
    }

    private void TcpSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window =
            new AdvancedWindow
            {
                Owner = this
            };

        window.ShowDialog();
    }

    private void MtuSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var adapter = CurrentAdapter();

        if (adapter == null)
        {
            ShowError(
                "MTU Settings",
                "Select a network adapter first.");

            return;
        }

        var window =
            new MtuWindow(adapter.Name)
            {
                Owner = this
            };

        if (window.ShowDialog() == true)
        {
            StatusBarTextBlock.Text =
                $"MTU updated for {adapter.Name}.";
        }
    }

    private void PingTestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartCommandPrompt(
            "/k ping 8.8.8.8");
    }

    private void DnsTestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartCommandPrompt(
            "/k nslookup google.com");
    }

    private void GatewayTestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string? gateway =
            CurrentAdapter()?
            .GetIPProperties()
            .GatewayAddresses
            .FirstOrDefault()
            ?.Address
            .ToString();

        if (string.IsNullOrWhiteSpace(gateway))
        {
            ShowError(
                "Gateway Test",
                "No gateway was found.");

            return;
        }

        StartCommandPrompt(
            $"/k ping {gateway}");
    }

    private void OpenCommandPromptButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartCommandPrompt("");
    }

    private void AdapterPropertiesLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartProcessSafely(
            "control.exe",
            "ncpa.cpl",
            "Network Adapter Properties");
    }

    private void WindowsNetworkSettingsLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartProcessSafely(
            "ms-settings:network",
            null,
            "Windows Network Settings",
            true);
    }

    private void DeviceManagerLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartProcessSafely(
            "devmgmt.msc",
            null,
            "Device Manager");
    }

    private void NetworkHelpLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        StartProcessSafely(
            "https://support.microsoft.com/windows",
            null,
            "Network Adapter Help",
            true);
    }

    private void StartCommandPrompt(
        string arguments)
    {
        StartProcessSafely(
            "cmd.exe",
            arguments,
            "Command Prompt");
    }

    private void StartProcessSafely(
        string fileName,
        string? arguments,
        string operationName,
        bool useShellExecute = true)
    {
        try
        {
            var info =
                new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute =
                        useShellExecute
                };

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                info.Arguments =
                    arguments;
            }

            Process.Start(info);
        }
        catch (Exception ex)
        {
            ShowError(
                operationName,
                $"Could not open {operationName}.\n\n{ex.Message}");
        }
    }

    private void ShowError(
        string title,
        string message)
    {
        StatusBarTextBlock.Text =
            message;

        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}