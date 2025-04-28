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

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Please configure LocalAI:BaseUrl and LocalAI:ModelId in appsettings.json.");
                return;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            using var recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(new DictationGrammar());

            using var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Volume = 100;
            synthesizer.Rate = 0;

            var conversation = new ConversationEngine();
            var moodController = new MoodController();
            string userInput = "";

            recognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null)
                {
                    userInput = e.Result.Text;
                    Console.WriteLine($"You: {userInput}");
                }
            };

            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            Console.WriteLine("JARVIS is online, sir. I await your command.");
            synthesizer.Speak("JARVIS is online, sir. I await your command.");

            while (true)
            {
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    await Task.Delay(500);
                    continue;
                }

                if (userInput.ToLower().Contains("enable sarcasm"))
                {
                    moodController.SarcasmEnabled = true;
                    Console.WriteLine("JARVIS: Sarcasm mode enabled.");
                    synthesizer.Speak("Sarcasm mode enabled, sir.");
                    userInput = "";
                    continue;
                }
                else if (userInput.ToLower().Contains("disable sarcasm"))
                {
                    moodController.SarcasmEnabled = false;
                    Console.WriteLine("JARVIS: Sarcasm mode disabled.");
                    synthesizer.Speak("Sarcasm mode disabled, sir.");
                    userInput = "";
                    continue;
                }
                else if (userInput.ToLower().Contains("lighthearted"))
                {
                    moodController.CurrentMood = Mood.Lighthearted;
                    Console.WriteLine("JARVIS: Mood set to lighthearted.");
                    synthesizer.Speak("Mood adjusted to lighthearted, sir.");
                    userInput = "";
                    continue;
                }
                else if (userInput.ToLower().Contains("serious"))
                {
                    moodController.CurrentMood = Mood.Serious;
                    Console.WriteLine("JARVIS: Mood set to serious.");
                    synthesizer.Speak("Mood adjusted to serious, sir.");
                    userInput = "";
                    continue;
                }

                conversation.AddUserMessage(userInput);
                var prompt = conversation.BuildPrompt(moodController);

                var completionPayload = new
                {
                    model = modelId,
                    prompt = prompt,
                    max_tokens = 150,
                    temperature = 0.6,
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
                    userInput = "";
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
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("choices", out var choices))
                        {
                            var choice = choices[0];

                            if (choice.TryGetProperty("text", out var textElement))
                            {
                                var chunk = textElement.GetString() ?? string.Empty;
                                jarvisReply += chunk;
                            }
                            else if (choice.TryGetProperty("message", out var messageElement) &&
                                     messageElement.TryGetProperty("content", out var contentElement))
                            {
                                var chunk = contentElement.GetString() ?? string.Empty;
                                jarvisReply += chunk;
                            }
                            else
                            {
                                Console.WriteLine("[Warning] 'choices' did not contain 'text' or 'message.content'.");
                            }
                        }
                        else if (root.TryGetProperty("error", out var error))
                        {
                            var errorMsg = error.GetProperty("message").GetString();
                            Console.WriteLine($"[LocalAI error]: {errorMsg}");
                        }
                        else
                        {
                            Console.WriteLine($"[Warning] Unknown response format: {json}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Stream parse error: {ex.Message}");
                    }
                }

                jarvisReply = jarvisReply.Trim();
                Console.WriteLine($"\nJARVIS: {jarvisReply}");

                if (!string.IsNullOrEmpty(jarvisReply))
                {
                    conversation.AddAssistantMessage(jarvisReply);

                    if (jarvisReply.Length < 50)
                        synthesizer.Rate = 2; // Faster for short replies
                    else
                        synthesizer.Rate = 0; // Normal for long answers

                    try
                    {
                        synthesizer.Speak(jarvisReply);
                    }
                    catch (Exception ex)
                    {
                      //  Console.WriteLine($"[Speech Error]: {ex.Message}");
                    }
                }

                userInput = "";
            }
        }
    }
}