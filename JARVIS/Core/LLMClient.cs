using Microsoft.Extensions.Options;
using JARVIS.Config;
using System.Text.Json;

public class LLMClient
{
    private readonly HttpClient _http;
    private readonly LocalAISettings _settings;

    public LLMClient(HttpClient httpClient, IOptions<LocalAISettings> options)
    {
        _http = httpClient;
        _settings = options.Value;
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        var request = new
        {
            model = _settings.Model,
            prompt,
            options = new
            {
                temperature = _settings.Temperature,
                top_p = _settings.TopP,
                max_tokens = _settings.MaxTokens
            }
        };

        var response = await _http.PostAsJsonAsync(_settings.Endpoint, request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("text")
                  .GetString()
                  ?.Trim() ?? "[No response]";
    }
}
