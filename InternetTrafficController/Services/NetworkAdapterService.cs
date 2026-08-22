using System.Net.NetworkInformation;
using System.Net.Sockets;
using InternetTrafficController.Models;


namespace InternetTrafficController.Services;


public class NetworkAdapterService
{

    public IEnumerable<NetworkInterface> GetAdapters()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x =>
            x.NetworkInterfaceType !=
            NetworkInterfaceType.Loopback);
    }



    public AdapterInfo GetInfo(NetworkInterface adapter)
    {

        var props =
            adapter.GetIPProperties();


        return new AdapterInfo
        {
            Name = adapter.Name,

            Status =
            adapter.OperationalStatus.ToString(),

            Mac =
            adapter.GetPhysicalAddress()
            .ToString(),


            Speed =
            $"{adapter.Speed / 1000000} Mbps",


            IPv4 =
            props.UnicastAddresses
            .FirstOrDefault(x =>
            x.Address.AddressFamily ==
            AddressFamily.InterNetwork)
            ?.Address.ToString()
            ?? "-",


            IPv6 =
            props.UnicastAddresses
            .FirstOrDefault(x =>
            x.Address.AddressFamily ==
            AddressFamily.InterNetworkV6)
            ?.Address.ToString()
            ?? "-",


            Gateway =
            props.GatewayAddresses
            .FirstOrDefault()
            ?.Address.ToString()
            ?? "-"
        };

    }

}