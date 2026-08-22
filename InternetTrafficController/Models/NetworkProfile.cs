namespace InternetTrafficController.Models;

public class NetworkProfile
{
    public string Name { get; set; } = "";

    public string DnsPrimary { get; set; } = "";

    public string DnsSecondary { get; set; } = "";

    public int Mtu { get; set; } = 1500;

    public string TcpCongestion { get; set; } = "default";

    public string AutoTuning { get; set; } = "normal";

    public bool EnableRss { get; set; }

    public bool EnableEcn { get; set; }
}