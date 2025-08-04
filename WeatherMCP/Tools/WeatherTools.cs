using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WeatherMCP.Tools
{
    public class WeatherTools
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherTools> _logger;
        private readonly string? _apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");

        public WeatherTools(IHttpClientFactory httpClientFactory, ILogger<WeatherTools> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [McpServerTool]
        [Description("Gets current weather conditions for the specified city.")]
        public async Task<string> GetCurrentWeather(
            [Description("The city name to get weather for")] string city,
            [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "API key not set.";

            try
            {
                var query = $"{city}{(countryCode != null ? $",{countryCode}" : "")}";
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q={query}&appid={_apiKey}&units=metric");

                if (!response.IsSuccessStatusCode)
                    return $"Error fetching weather: {response.StatusCode}";

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var temp = root.GetProperty("main").GetProperty("temp").GetDecimal();
                var desc = root.GetProperty("weather")[0].GetProperty("description").GetString();

                return $"Current weather in {city}: {desc}, {temp}°C";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current weather");
                return "Internal error while fetching weather.";
            }
        }

        [McpServerTool]
        [Description("Gets weather forecast for the specified city (3-day minimum).")]
        public async Task<string> GetWeatherForecast(
        [Description("The city name to get forecast for")] string city,
        [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "API key not set.";

            try
            {
                var query = $"{city}{(countryCode != null ? $",{countryCode}" : "")}";
                var client = _httpClientFactory.CreateClient();
                // OpenWeatherMap free tier даёт 5-дневный прогноз с шагом 3 часа
                var response = await client.GetAsync(
                    $"https://api.openweathermap.org/data/2.5/forecast?q={query}&appid={_apiKey}&units=metric");

                if (!response.IsSuccessStatusCode)
                    return $"Error fetching forecast: {response.StatusCode}";

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var list = doc.RootElement.GetProperty("list");
                var forecasts = new List<string>();
                var days = new HashSet<string>();

                foreach (var item in list.EnumerateArray())
                {
                    var dt_txt = item.GetProperty("dt_txt").GetString();
                    var date = dt_txt.Split(' ')[0];

                    // Сохраняем только первую запись на день (обычно это полдень)
                    if (days.Count < 3 && !days.Contains(date))
                    {
                        days.Add(date);
                        var temp = item.GetProperty("main").GetProperty("temp").GetDecimal();
                        var desc = item.GetProperty("weather")[0].GetProperty("description").GetString();
                        forecasts.Add($"{date}: {desc}, {temp}°C");
                    }
                }

                if (forecasts.Count == 0)
                    return "No forecast data available.";

                return $"Weather forecast for {city}:\n" + string.Join("\n", forecasts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get weather forecast");
                return "Internal error while fetching forecast.";
            }
        }

        [McpServerTool]
        [Description("Gets weather alerts/warnings for the specified city.")]
        public async Task<string> GetWeatherAlerts(
        [Description("The city name to get alerts for")] string city,
        [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "API key not set.";

            try
            {
                var query = $"{city}{(countryCode != null ? $",{countryCode}" : "")}";
                var client = _httpClientFactory.CreateClient();
                var geoResp = await client.GetAsync(
                    $"http://api.openweathermap.org/geo/1.0/direct?q={query}&limit=1&appid={_apiKey}");

                if (!geoResp.IsSuccessStatusCode)
                    return $"Error fetching geo data: {geoResp.StatusCode}";

                var geoJson = await geoResp.Content.ReadAsStringAsync();
                var geoDoc = JsonDocument.Parse(geoJson);

                if (geoDoc.RootElement.GetArrayLength() == 0)
                    return "Location not found.";

                var location = geoDoc.RootElement[0];
                var lat = location.GetProperty("lat").GetDecimal();
                var lon = location.GetProperty("lon").GetDecimal();

                var weatherResp = await client.GetAsync(
                    $"https://api.openweathermap.org/data/3.0/onecall?lat={lat}&lon={lon}&appid={_apiKey}&units=metric");

                if (weatherResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return "Weather alerts feature requires a paid OpenWeatherMap API key (One Call 3.0).";

                if (!weatherResp.IsSuccessStatusCode)
                    return $"Error fetching weather alerts: {weatherResp.StatusCode}";

                var weatherJson = await weatherResp.Content.ReadAsStringAsync();
                var weatherDoc = JsonDocument.Parse(weatherJson);

                if (!weatherDoc.RootElement.TryGetProperty("alerts", out var alertsElem) || alertsElem.GetArrayLength() == 0)
                    return "No weather alerts currently.";

                var alerts = new List<string>();
                foreach (var alert in alertsElem.EnumerateArray())
                {
                    var eventName = alert.GetProperty("event").GetString();
                    var desc = alert.GetProperty("description").GetString();
                    alerts.Add($"* {eventName}: {desc}");
                }

                return "Weather alerts:\n" + string.Join("\n", alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get weather alerts");
                return "Internal error while fetching alerts.";
            }
        }
    }
}
