using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JARVIS.Services
{
    public class WeatherCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherCollector(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetWeatherAsync(string city)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=imperial");
                var weatherData = JsonDocument.Parse(response);

                var temp = weatherData.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
                var description = weatherData.RootElement.GetProperty("weather")[0].GetProperty("description").GetString();
                var cityName = weatherData.RootElement.GetProperty("name").GetString();

                return $"It is currently {temp:F1} degrees Fahrenheit and {description} in {cityName}.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherCollector Error]: {ex.Message}");
                return "I'm unable to retrieve the weather information at the moment, sir.";
            }
        }
    }
}
