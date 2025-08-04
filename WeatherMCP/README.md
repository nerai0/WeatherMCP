# Real Weather MCP Server

Test assignment for FastMCP:
A functional MCP server providing real weather data (current conditions, 3-day forecast, weather alerts) for AI assistants via the Model Context Protocol.

---

## 🚀 Quick Start

### 1. Get an OpenWeatherMap API Key

* Register at: [https://home.openweathermap.org/users/sign\_up](https://home.openweathermap.org/users/sign_up)
* Confirm your email
* Copy your API key from your account page

### 2. Set API Key as Environment Variable

**Windows (PowerShell):**

```powershell
$env:WEATHER_API_KEY="your_api_key_here"
```

Or permanently:

```cmd
setx WEATHER_API_KEY "your_api_key_here"
```

**Linux/macOS (bash):**

```bash
export WEATHER_API_KEY="your_api_key_here"
```

### 3. Build and Run

Clone or download this repository, open in terminal:

```bash
dotnet restore
dotnet run
```

By default, the MCP server starts in `Production` mode and listens for protocol messages on stdin/stdout.

---

## 🛠️ Manual Testing (for reviewers)

For direct testing (without MCP tools), edit `Program.cs` to add:

```csharp
using (var scope = host.Services.CreateScope())
{
    var tools = scope.ServiceProvider.GetRequiredService<WeatherTools>();
    var weather = await tools.GetCurrentWeather("Astana", "KZ");
    Console.Error.WriteLine($"[TEST] {weather}");

    var forecast = await tools.GetWeatherForecast("Astana", "KZ");
    Console.Error.WriteLine($"[TEST_FORECAST] {forecast}");

    var alerts = await tools.GetWeatherAlerts("Astana", "KZ");
    Console.Error.WriteLine($"[TEST_ALERTS] {alerts}");
}
```

**Expected output:**

```
[TEST] Current weather in Astana: scattered clouds, 17.97°C
[TEST_FORECAST] Weather forecast for Astana:
2025-08-04: scattered clouds, 17.97°C
2025-08-05: few clouds, 9.15°C
2025-08-06: scattered clouds, 17.27°C
[TEST_ALERTS] Weather alerts feature requires a paid OpenWeatherMap API key (One Call 3.0).
```

---

## 🧩 Features

* **GetCurrentWeather** — current weather for any city (supports country code)
* **GetWeatherForecast** — 3-day forecast for any city
* **GetWeatherAlerts** — weather warnings for a city (requires paid OpenWeatherMap key; gracefully handled if not available)
* Robust error handling (invalid city, missing/invalid API key, API errors)
* Modular, extensible MCP tool structure

---

## 🔍 Implementation Notes

* Uses `.NET 8`, `HttpClientFactory`, `Microsoft.Extensions.AI.Abstractions`
* Weather data fetched from [OpenWeatherMap API](https://openweathermap.org/api)
* Alerts fetched via OpenWeatherMap One Call API 3.0 (**not available on free tier**)
* All API keys must be provided as environment variables (`WEATHER_API_KEY`)
* Logs are sent to stderr (MCP protocol uses stdout)

---

## 📝 Limitations

* Weather alerts require a **paid OpenWeatherMap key** ("One Call 3.0" access).
  On the free plan, you will see:
  `"Weather alerts feature requires a paid OpenWeatherMap API key (One Call 3.0)."`
* Forecast uses 5-day/3-hour API, but outputs only one record per day (3 days).
* No HTTP interface; server runs via stdin/stdout protocol only.

---

## 📄 Example Tool Methods

See `WeatherTools.cs` for:

* `GetCurrentWeather(city, countryCode?)`
* `GetWeatherForecast(city, countryCode?)`
* `GetWeatherAlerts(city, countryCode?)`

---

## 💡 How it works

* For city lookups, [OpenWeatherMap Geocoding API](https://openweathermap.org/api/geocoding-api) is used to get coordinates.
* Main weather and forecast endpoints provide data by city.
* Alerts (if available) are fetched by coordinates via One Call 3.0 API.

---

## 🙌 Author

Maksat Jambyluly (Ermakhanov)