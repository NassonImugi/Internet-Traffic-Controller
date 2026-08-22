using InternetTrafficController.Models;
using System.IO;
using System.Text.Json;

namespace InternetTrafficController.Services;

public class ProfileService
{
    private readonly string folder =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Profiles");


    public ProfileService()
    {
        Directory.CreateDirectory(folder);
        CreateDefaults();
    }



    public List<NetworkProfile> LoadProfiles()
    {
        List<NetworkProfile> profiles = new();

        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                string json =
                    File.ReadAllText(file);

                var profile =
                    JsonSerializer.Deserialize<NetworkProfile>(json);

                if (profile != null)
                    profiles.Add(profile);
            }
            catch
            {

            }
        }

        return profiles;
    }




    public void SaveProfile(NetworkProfile profile)
    {
        string path =
            Path.Combine(
            folder,
            profile.Name + ".json");


        string json =
            JsonSerializer.Serialize(
            profile,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });


        File.WriteAllText(path, json);
    }




    private void CreateDefaults()
    {

        if (Directory.GetFiles(folder).Length > 0)
            return;



        SaveProfile(new NetworkProfile
        {
            Name = "Gaming",
            DnsPrimary = "1.1.1.1",
            DnsSecondary = "1.0.0.1",
            Mtu = 1500,
            TcpCongestion = "ctcp",
            AutoTuning = "normal",
            EnableRss = true,
            EnableEcn = false
        });



        SaveProfile(new NetworkProfile
        {
            Name = "Streaming",
            DnsPrimary = "8.8.8.8",
            DnsSecondary = "8.8.4.4",
            Mtu = 1500,
            TcpCongestion = "cubic",
            AutoTuning = "normal",
            EnableRss = true,
            EnableEcn = true
        });



        SaveProfile(new NetworkProfile
        {
            Name = "Low Latency",
            DnsPrimary = "1.1.1.1",
            DnsSecondary = "1.0.0.1",
            Mtu = 1500,
            TcpCongestion = "ctcp",
            AutoTuning = "restricted",
            EnableRss = true,
            EnableEcn = false
        });

    }

}