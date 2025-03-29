using System.Text.Json;

using FluentPort.SDK;

namespace FluentPort.APIServer.Models;

public static class ForwardingModel
{
    public static string? IPINFO_TOKEN { get; set; }
    public static List<Server> Servers { get; set; } = new List<Server>();

    public static int MaxCreatedTunnels => 3;

    public static Server RandomServer()
    {
        return Servers[Random.Shared.Next(0, Servers.Count)];
    }

    public static Server GetServer(string address)
    {
        foreach (Server server in Servers)
        {
            if (server.Address == address)
                return server;
        }
        return null!;
    }

    public static void Load(string path, bool print = true)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(Servers));
            return;
        }
        Servers.Clear();
        List<Server> servers = JsonSerializer.Deserialize<List<Server>>(File.ReadAllText(path))!;
        foreach (Server server in servers)
        {
            if (print)
                Logger.Log(new Log(LogLevels.Information, "ForwardingModel.Load", "Loading server: " + JsonSerializer.Serialize(server, new JsonSerializerOptions() { WriteIndented = true })));
            Servers.Add(server);
        }
    }

    public static async Task<Server> SelectServer(string ip)
    {
        IPInfo info = await get_ip_info(ip);
        if (info != null)
        {
            double lat = Convert.ToDouble(info.loc!.Split(",")[0]);
            double lon = Convert.ToDouble(info.loc.Split(",")[1]);

            Dictionary<Server, double> points = new Dictionary<Server, double>();

            foreach (Server server in Servers)
            {
                if (server.ActiveTunnels >= server.MaxTunnels)
                    continue;

                double distance = haversine_dist(lat, lon, server.Latitude, server.Longitude);

                double location_score = Math.Max(0, 100 - (distance * 2));
                double usage_score = (server.ActiveTunnels / server.MaxTunnels) * -20 + 10;

                double total_score = location_score + usage_score;
                points.Add(server, total_score);
            }

            if (points.Count == 0)
                return null!;
            Server selected = points.OrderByDescending(s => s.Value).FirstOrDefault().Key;
            return selected;
        }
        else
        {
            Logger.Log(new Log(LogLevels.Warning, "ForwardingModel.SelectServer", "Could not get geolocation, rating based on other criteria"));
            Dictionary<Server, double> points = new Dictionary<Server, double>();
            foreach (Server server in Servers)
            {
                if (server.ActiveTunnels >= server.MaxTunnels)
                    continue;

                double usage_score = (server.ActiveTunnels / server.MaxTunnels) * -20 + 10;

                double total_score = usage_score;
                points.Add(server, total_score);
            }
            if (points.Count == 0)
                return null!;
            Server selected = points.OrderByDescending(s => s.Value).FirstOrDefault().Key;
            return selected;
        }
    }

    public static Server IncrementTunnelConnected(ServerOperationRequest request)
    {
        foreach (Server server in Servers)
        {
            if (server.Target == request.Target && server.Key == request.Key)
            {
                server.ActiveTunnels++;
                return server;
            }
        }
        return null!;
    }
    public static Server DecrementTunnelConnected(ServerOperationRequest request)
    {
        foreach (Server server in Servers)
        {
            if (server.Target == request.Target && server.Key == request.Key)
            {
                server.ActiveTunnels--;
                return server;
            }
        }
        return null!;
    }

    private static async Task<IPInfo> get_ip_info(string ip)
    {
        try
        {
            string url = "http://ipinfo.io/" + ip + "?token=" + IPINFO_TOKEN;

            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);

            IPInfo ip_info = JsonSerializer.Deserialize<IPInfo>(response)!;

            return ip_info;
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Warning, "ForwardingModel.get_ip_info", "IPInfo.io API error: " + ex.Message));
            return null!;
        }
    }

    private static double haversine_dist(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double lat1Rad = deg_to_rad(lat1);
        double lon1Rad = deg_to_rad(lon1);
        double lat2Rad = deg_to_rad(lat2);
        double lon2Rad = deg_to_rad(lon2);

        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double deg_to_rad(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
