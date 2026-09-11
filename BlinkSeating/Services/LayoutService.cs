using System.IO;
using System.Text.Json;
using BlinkSeating.Models;

namespace BlinkSeating.Services;

public static class LayoutService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static void Save(VenueLayout layout, string path)
    {
        var json = JsonSerializer.Serialize(layout, Options);
        File.WriteAllText(path, json);
    }

    public static VenueLayout Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<VenueLayout>(json, Options)
               ?? new VenueLayout();
    }
}
