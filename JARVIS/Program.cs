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
using JARVIS.Core;
using JARVIS.Models;
using JARVIS.Services;

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
                Console.WriteLine("Error: Please configure LocalAI:BaseUrl and ModelId in appsettings.json.");
                return;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var wakeListener = new WakeWordListener("hey jarvis");
            var activeRecognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            activeRecognizer.SetInputToDefaultAudioDevice();
            activeRecognizer.LoadGrammar(new DictationGrammar());

            using var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Volume = 100;
            synthesizer.Rate = 0;

            var conversation = new ConversationEngine();
            var moodController = new MoodController();
            var memoryManager = new MemoryManager();

            string userInput = "";
            bool isAwake = false;

            wakeListener.WakeWordDetected += () =>
            {
                synthesizer.Speak("Yes, sir?");
                Console.WriteLine("[WakeWord] Wake word detected. Active listening...");
                activeRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                isAwake = true;
            };

            activeRecognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null)
                {
                    userInput = e.Result.Text;
                    Console.WriteLine($"You: {userInput}");
                }
            };

            wakeListener.Start();
            Console.WriteLine("JARVIS is sleeping. Listening for wake word...");
            synthesizer.Speak("System online, sir. Awaiting activation.");

            while (true)
            {
                if (!isAwake)
                {
                    await Task.Delay(500);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    await Task.Delay(500);
                    continue;
                }

                // === Special Commands
                if (userInput.ToLower().Contains("enable sarcasm"))
                {
                    moodController.SarcasmEnabled = true;
                    Console.WriteLine("JARVIS: Sarcasm enabled.");
                    synthesizer.Speak("Sarcasm enabled, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("disable sarcasm"))
                {
                    moodController.SarcasmEnabled = false;
                    Console.WriteLine("JARVIS: Sarcasm disabled.");
                    synthesizer.Speak("Sarcasm disabled, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("lighthearted"))
                {
                    moodController.CurrentMood = Mood.Lighthearted;
                    Console.WriteLine("JARVIS: Mood changed to lighthearted.");
                    synthesizer.Speak("Mood changed to lighthearted, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("serious"))
                {
                    moodController.CurrentMood = Mood.Serious;
                    Console.WriteLine("JARVIS: Mood changed to serious.");
                    synthesizer.Speak("Mood changed to serious, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("emergency mode"))
                {
                    moodController.CurrentMood = Mood.Emergency;
                    Console.WriteLine("JARVIS: Emergency mode activated.");
                    synthesizer.Speak("Emergency mode activated, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("resume normal operations"))
                {
                    moodController.CurrentMood = Mood.Serious;
                    Console.WriteLine("JARVIS: Resuming normal operations.");
                    synthesizer.Speak("Resuming normal operations, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("save memory"))
                {
                    memoryManager.SaveMemory(conversation.GetMessages());
                    Console.WriteLine("JARVIS: Memory saved.");
                    synthesizer.Speak("Memory saved, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("load memory"))
                {
                    var loadedMessages = memoryManager.LoadMemory();
                    conversation.Reset();
                    foreach (var msg in loadedMessages)
                    {
                        if (msg.Role == "user")
                            conversation.AddUserMessage(msg.Content);
                        else if (msg.Role == "assistant")
                            conversation.AddAssistantMessage(msg.Content);
                    }
                    Console.WriteLine("JARVIS: Memory loaded.");
                    synthesizer.Speak("Memory loaded, sir.");
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().StartsWith("remember that"))
                {
                    var fact = userInput.Replace("remember that", "", StringComparison.OrdinalIgnoreCase).Trim();
                    conversation.AddKnowledgeFact(fact);
                    Console.WriteLine("JARVIS: Fact recorded.");
                    synthesizer.Speak("Fact recorded, sir.");
                    ResetRecognition();
                    continue;
                }

                // === Normal Conversation
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
                    ResetRecognition();
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
                    synthesizer.Rate = jarvisReply.Length < 50 ? 2 : 0;
                    try
                    {
                        synthesizer.Speak(jarvisReply);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Speech Error]: {ex.Message}");
                    }
                }

                ResetRecognition();
            }

            void ResetRecognition()
            {
                try
                {
                    activeRecognizer.RecognizeAsyncStop();
                }
                catch { }

                isAwake = false;
                userInput = "";
                wakeListener.Start();
                Console.WriteLine("[WakeWord] Returning to sleep mode...");
            }
        }
    }
}
