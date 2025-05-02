using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JARVIS.Service
{
    public static class LocalAIAgent
    {
        public static async Task<string> GetResponseAsync(HttpClient http, string modelId, string prompt)
        {
            var request = new
            {
                model = modelId,
                prompt = prompt,
                max_tokens = 800,
                temperature = 0.9,
                stop = new[] { "User:", "JARVIS:" }
            };

            var response = await http.PostAsJsonAsync("/v1/completions", request);
            if (!response.IsSuccessStatusCode)
            {
                return $"[LocalAI error {response.StatusCode}]";
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var text = json.GetProperty("choices")[0].GetProperty("text").GetString();
            return text?.Trim() ?? "[No response]";
        }
    }
}
