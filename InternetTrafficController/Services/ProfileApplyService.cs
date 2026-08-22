using InternetTrafficController.Models;


namespace InternetTrafficController.Services;


public class ProfileApplyService
{

    private readonly DnsManagerService dns =
        new();


    private readonly MtuService mtu =
        new();


    private readonly TcpSettingsService tcp =
        new();




    public void Apply(
        NetworkProfile profile,
        string adapter)
    {


        // DNS

        dns.SetDns(
            adapter,
            profile.DnsPrimary,
            profile.DnsSecondary);



        // MTU

        mtu.SetMtu(
            adapter,
            profile.Mtu);



        // TCP

        tcp.SetAutoTuning(
            profile.AutoTuning);



        tcp.SetCongestionProvider(
            profile.TcpCongestion);



        tcp.EnableRss(
            profile.EnableRss);



        tcp.EnableEcn(
            profile.EnableEcn);


    }

}