using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InternetTrafficController.Services;

namespace InternetTrafficController.Views;

public partial class AdvancedWindow : Window
{
    private readonly TcpSettingsService tcp = new();

    public AdvancedWindow()
    {
        InitializeComponent();

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        try
        {
            TcpSettings settings =
                tcp.GetCurrentSettings();

            SelectComboBoxValue(
                AutoTuneBox,
                settings.AutoTuning);

            SelectComboBoxValue(
                CongestionBox,
                settings.CongestionProvider);

            RssCheck.IsChecked =
                settings.RssEnabled;

            EcnCheck.IsChecked =
                settings.EcnEnabled;
        }
        catch
        {
            AutoTuneBox.SelectedIndex = 0;
            CongestionBox.SelectedIndex = 0;

            RssCheck.IsChecked = true;
            EcnCheck.IsChecked = false;
        }
    }

    private static void SelectComboBoxValue(
        ComboBox comboBox,
        string value)
    {
        foreach (ComboBoxItem item
            in comboBox.Items.OfType<ComboBoxItem>())
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

        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
    }

    private void Apply_Click(
        object sender,
        RoutedEventArgs e)
    {
        string tuning =
            (AutoTuneBox.SelectedItem as ComboBoxItem)
            ?.Content?.ToString()
            ?? "normal";

        string congestion =
            (CongestionBox.SelectedItem as ComboBoxItem)
            ?.Content?.ToString()
            ?? "ctcp";

        try
        {
            tcp.SetAutoTuning(tuning);

            tcp.SetCongestionProvider(congestion);

            tcp.EnableRss(
                RssCheck.IsChecked == true);

            tcp.EnableEcn(
                EcnCheck.IsChecked == true);

            MessageBox.Show(
                "Advanced TCP settings applied successfully.",
                "Internet Traffic Controller",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            LoadCurrentSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to apply TCP settings.\n\n{ex.Message}",
                "TCP Settings Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}