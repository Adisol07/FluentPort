using System.Text.Json;

namespace FluentPort.SDK;

public static class Logger
{
    public static List<Log> Logs { get; set; } = new List<Log>();
    public static Action<Log>? OnLog { get; set; }

    public static void Log(Log log)
    {
        Logs.Add(log);
        if (OnLog != null)
            OnLog.Invoke(log);
    }

    public static void Save(string path, bool clear = true)
    {
        string p = path;
        if (!p.EndsWith(".json"))
            p += ".json";
        lock (Logs)
        {
            File.WriteAllText(p, JsonSerializer.Serialize(Logs));
            if (clear)
                Logs.Clear();
        }
    }
}
