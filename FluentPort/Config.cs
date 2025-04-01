using System.Text.Json;
using System.IO;

namespace FluentPort;

public class Config
{
    public string? Theme { get; set; } = "Dark";
    public string? Language { get; set; } = "English";

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true }));
    }
}
