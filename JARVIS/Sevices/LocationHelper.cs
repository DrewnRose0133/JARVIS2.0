using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JARVIS.Services
{
    public static class LocationHelper
    {
        public static async Task<string> GetCityAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync("http://ip-api.com/json/");

                var locationData = JsonDocument.Parse(response);
                var city = locationData.RootElement.GetProperty("city").GetString();

                return city ?? "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocationHelper Error]: {ex.Message}");
                return "Unknown";
            }
        }
    }
}
