using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;
using JARVIS;

namespace JARVIS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var baseUrl = config["LocalAI:BaseUrl"];
            var modelId = config["LocalAI:ModelId"];
            var weatherApiKey = config["Weather:ApiKey"];
            bool debugEnabled = bool.TryParse(config["LocalAI:Debug"], out var dbg) && dbg;

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(weatherApiKey))
            {
                Console.WriteLine("Error: Please configure LocalAI:BaseUrl, LocalAI:ModelId, and Weather:ApiKey in appsettings.json.");
                return;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(10) };
            using var recognizer = new SpeechRecognitionEngine();
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(new DictationGrammar());

            using var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            var voice = synthesizer.GetInstalledVoices().Select(v => v.VoiceInfo.Name).FirstOrDefault();
            if (voice != null)
                synthesizer.SelectVoice(voice);

            var conversation = new ConversationEngine();
            var weatherCollector = new WeatherCollector(weatherApiKey);

            if (debugEnabled)
                Console.WriteLine("Testing TTS output...");
            synthesizer.Speak("JARVIS is online and ready to speak.");
            Console.WriteLine("JARVIS is online via LocalAI with conversation memory and weather access. Say something...");

            while (true)
            {
                var speechInput = recognizer.Recognize();
                if (speechInput == null)
                    continue;

                var userInput = speechInput.Text;
                Console.WriteLine($"You: {userInput}");

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.ToLower().Contains("reset memory") || userInput.ToLower().Contains("forget everything"))
                {
                    conversation.Reset();
                    Console.WriteLine("JARVIS: Memory reset.");
                    synthesizer.Speak("Memory has been reset, sir.");
                    continue;
                }

                if (userInput.ToLower().Contains("weather") || userInput.ToLower().Contains("temperature") || userInput.ToLower().Contains("raining"))
                {
                    string city = "New York";

                    var words = userInput.Split(' ');
                    int inIndex = Array.FindIndex(words, w => w.Equals("in", StringComparison.OrdinalIgnoreCase));
                    if (inIndex != -1 && inIndex + 1 < words.Length)
                    {
                        city = words[inIndex + 1];
                    }

                    var weatherInfo = await weatherCollector.GetWeatherAsync(city);
                    Console.WriteLine($"JARVIS (Weather): {weatherInfo}");
                    synthesizer.Speak(weatherInfo);
                    continue;
                }

                conversation.AddUserMessage(userInput);

                var prompt = conversation.BuildPrompt();

                var completionPayload = new
                {
                    model = modelId,
                    prompt = string.IsNullOrWhiteSpace(userInput) ? "Please clarify your question." : prompt,
                    max_tokens = 64,
                    temperature = 0.5,
                    stream = true
                };

                HttpResponseMessage response;
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "/v1/completions")
                    {
                        Content = JsonContent.Create(completionPayload)
                    };
                    response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error querying LocalAI: {ex.Message}");
                    continue;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                string jarvisReply = string.Empty;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                        continue;

                    var json = line.Substring("data: ".Length);
                    if (json.Trim() == "[DONE]")
                        break;

                    try
                    {
                        if (json.Contains("\"choices\""))
                        {
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("choices", out var choices))
                            {
                                var chunk = choices[0].GetProperty("text").GetString() ?? string.Empty;
                                jarvisReply += chunk;
                                if (debugEnabled)
                                    Console.Write(chunk);
                            }
                        }
                        else if (json.Contains("\"error\""))
                        {
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("error", out var error))
                            {
                                var errorMsg = error.GetProperty("message").GetString();
                                Console.WriteLine($"[LocalAI error]: {errorMsg}");
                                synthesizer.Speak($"There was an error. {errorMsg}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Warning] Unknown response format: {json}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (debugEnabled)
                            Console.WriteLine($"\nStream parse error: {ex.Message}");
                    }
                }

                jarvisReply = jarvisReply.Trim();
                Console.WriteLine($"\nJARVIS: {jarvisReply}");

                if (!string.IsNullOrEmpty(jarvisReply))
                {
                    conversation.AddAssistantMessage(jarvisReply);
                    synthesizer.Speak(jarvisReply);
                }
            }
        }
    }
}
