using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using InternetTrafficController.Services;

namespace InternetTrafficController.Views;

public partial class MtuWindow : Window
{
    private readonly string adapterName;
    private readonly MtuService mtuService = new();

    public MtuWindow(string adapterName)
    {
        InitializeComponent();

        this.adapterName = adapterName;

        AdapterTextBox.Text = adapterName;

        LoadCurrentMtu();
    }

    private void LoadCurrentMtu()
    {
        try
        {
            var adapter = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(x => x.Name == adapterName);

            if (adapter != null)
            {
                var ipv4 = adapter.GetIPProperties()
                    .GetIPv4Properties();

                MtuTextBox.Text = ipv4.Mtu.ToString();
            }
            else
            {
                MtuTextBox.Text = "1500";
            }
        }
        catch
        {
            MtuTextBox.Text = "1500";
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MtuTextBox.Text, out int mtu))
        {
            MessageBox.Show(
                "Enter a valid whole-number MTU value.",
                "Invalid MTU",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (mtu < 576 || mtu > 9000)
        {
            MessageBox.Show(
                "Enter an MTU value between 576 and 9000.",
                "Invalid MTU",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            mtuService.SetMtu(adapterName, mtu);

            MessageBox.Show(
                $"MTU for {adapterName} was set to {mtu}.",
                "MTU Applied",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to apply MTU.\n\n{ex.Message}",
                "MTU Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}