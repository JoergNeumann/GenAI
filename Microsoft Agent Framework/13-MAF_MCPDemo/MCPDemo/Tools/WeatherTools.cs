using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCPServer.Tools;

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool, Description("Gibt Mock-Wetterdaten für eine Stadt zurück.")]
    public static string GetWeatherMock(
        [Description("Name der Stadt, z. B. Berlin")] string city)
    {
        city = city?.Trim() ?? "Unbekannt";

        return city.ToLowerInvariant() switch
        {
            "berlin" => "In Berlin sind es aktuell 18°C, leicht bewölkt.",
            "hamburg" => "In Hamburg sind es aktuell 15°C, windig.",
            "münchen" or "munich" => "In München sind es aktuell 20°C, sonnig.",
            _ => $"Für {city} sind aktuell 17°C und wechselhaftes Wetter gemeldet."
        };
    }
}