using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JARVIS
{
    public class WeatherCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherCollector(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<string> GetWeatherAsync(string city = "New York")
        {
            try
            {
                var response = await _httpClient.GetStringAsync(
                    $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=imperial"
                );

                var weather = JsonDocument.Parse(response);
                var temp = weather.RootElement.GetProperty("main").GetProperty("temp").GetDecimal();
                var description = weather.RootElement.GetProperty("weather")[0].GetProperty("description").GetString();

                return $"The current temperature in {city} is {temp} degrees Fahrenheit with {description}.";
            }
            catch (Exception ex)
            {
                return $"Unable to retrieve weather information: {ex.Message}";
            }
        }
    }
}
