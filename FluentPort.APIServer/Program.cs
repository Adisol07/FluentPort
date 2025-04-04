using FluentPort.SDK;
using FluentPort.APIServer;
using FluentPort.APIServer.Models;

string app_path = Environment.CurrentDirectory;

var builder = WebApplication.CreateBuilder(args);
#if RELEASE
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps(app_path + "/api_certificate.pfx", File.ReadAllText(app_path + "/certificate_password.txt").Trim());
    });
});
#endif

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.MapControllers();

//string app_path = AppDomain.CurrentDomain.BaseDirectory;
APIDetails details = new APIDetails()
{
    Version = "0.1.2",
    LatestDownload_osx_arm64 = "https://dl.dropboxusercontent.com/scl/fi/llp5px6qup6vkr61r3oun/osx-arm64_0.1.1.zip?rlkey=0u4d39g02deap1mc5jk4u0bu3&st=6p4ydzqa&dl=1",
    LatestDownload_win_arm64 = "https://dl.dropboxusercontent.com/scl/fi/g6iu1xfvqzpug8uyqpgd2/win-arm64_0.1.2.zip?rlkey=kojuxhdnlm6fdvorz4w3eebp6&st=itx4y9c5&dl=1",
    LatestDownload_win_x64 = "https://dl.dropboxusercontent.com/scl/fi/5u09oynkfwv3idyx2aqy5/win-x64_0.1.2.zip?rlkey=5pyxnj7xt1my40pggfby2c5pw&st=onufj5xf&dl=1"
};
Logger.OnLog += (log) =>
{
    Console.WriteLine(log);
};
Account.DataDirectory = app_path + "/data/";
Account.UsersDirectory = app_path + "/data/users";

if (!Directory.Exists(app_path + "/data"))
    Directory.CreateDirectory(app_path + "/data");
if (!Directory.Exists(app_path + "/data/logs"))
    Directory.CreateDirectory(app_path + "/data/logs");
if (!Directory.Exists(app_path + "/data/users"))
    Directory.CreateDirectory(app_path + "/data/users");
if (!Directory.Exists(app_path + "/data/deleted_users"))
    Directory.CreateDirectory(app_path + "/data/deleted_users");
if (!Directory.Exists(app_path + "/data/feedbacks"))
    Directory.CreateDirectory(app_path + "/data/feedbacks");

ForwardingModel.Load(app_path + "/data/servers.json");
ForwardingModel.IPINFO_TOKEN = File.ReadAllText(app_path + "/IpInfoToken.txt");
AuthModel.Load(app_path + "/data/users");
AuthModel.FeedbacksDir = app_path + "/data/feedbacks";
AuthModel.EmailPassword = File.ReadAllText(app_path + "/email_password.txt").Trim();

TimeSpan log_time = TimeSpan.FromMinutes(5);
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay((int)log_time.TotalSeconds * 1000);
        if (Logger.Logs.Count > 0)
            Logger.Save(app_path + "/data/logs/" + DateTime.Now.Ticks + ".json", true);
        lock (ForwardingModel.Servers!)
        {
            List<Server> backup = new List<Server>();
            backup.AddRange(ForwardingModel.Servers!);
            Dictionary<string, int> usage = new Dictionary<string, int>();
            foreach (Server server in ForwardingModel.Servers!)
                usage.Add(server.Key!, server.ActiveTunnels);
            ForwardingModel.Load(app_path + "/data/servers.json", false);
            foreach (Server server in ForwardingModel.Servers!)
                if (usage.ContainsKey(server.Key!))
                    server.ActiveTunnels = usage[server.Key!];
            if (backup.Equals(ForwardingModel.Servers))
                Logger.Log(new Log(LogLevels.Information, "LOOP", "Reloaded servers"));
            backup.Clear();
            usage.Clear();
        }
    }
});

app.MapGet("/", () =>
{
    Logger.Log(new Log(LogLevels.Information, "API Root", "Hit"));
    return details;
});

app.Run();
