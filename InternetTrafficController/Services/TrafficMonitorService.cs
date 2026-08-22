using System.Net.NetworkInformation;


namespace InternetTrafficController.Services;


public class TrafficMonitorService
{

    private long lastDown;
    private long lastUp;


    public (double download, double upload)
        GetSpeed(NetworkInterface adapter)
    {

        var stats =
            adapter.GetIPv4Statistics();


        long down =
            stats.BytesReceived;


        long up =
            stats.BytesSent;


        if (lastDown == 0)
        {
            lastDown = down;
            lastUp = up;

            return (0, 0);
        }


        double download =
            (down - lastDown)
            * 8
            / 1000000.0;


        double upload =
            (up - lastUp)
            * 8
            / 1000000.0;



        lastDown = down;
        lastUp = up;



        return
        (
            Math.Round(download, 2),
            Math.Round(upload, 2)
        );

    }

}