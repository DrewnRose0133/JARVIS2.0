using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JARVIS
{
    public static class GeoLocationHelper
    {
        public static async Task<string> GetCityAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync("https://ipinfo.io/json");
                var json = JsonDocument.Parse(response);
                var city = json.RootElement.GetProperty("city").GetString();
                return city ?? "New York";
            }
            catch
            {
                return "New York"; // fallback
            }
        }
    }
}
