using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using JARVIS.Core;
using JARVIS.Models;
using JARVIS.Services;
using JARVIS.Shared;
using Fleck;
using JARVIS.Config;

namespace JARVIS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var visualizerServer = new VisualizerSocketServer();
            visualizerServer.Start();

            // Later in code:
            visualizerServer.Broadcast("Listening");


            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var baseUrl = config["LocalAI:BaseUrl"];
            var modelId = config["LocalAI:ModelId"];
            var weatherApiKey = config["OpenWeather:ApiKey"];
            var cityName = config["OpenWeather:City"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(weatherApiKey))
            {
                Console.WriteLine("Error: Please configure LocalAI and OpenWeather settings in appsettings.json.");
                return;
            }

            // Initialize HttpClient for querying LocalAI
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var wakeListener = new WakeWordListener("hey jarvis you there");
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
            var weatherCollector = new WeatherCollector(weatherApiKey);

            // Initialize the SmartHomeController
            var smartHomeController = new SmartHomeController(); // Declare and initialize the controller

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Console.WriteLine("[Location] Attempting to auto-detect city...");
                cityName = await LocationHelper.GetCityAsync();
                Console.WriteLine($"[Location] Detected City: {cityName}");
                visualizerServer.Broadcast("Speaking");
            }

            string userInput = "";
            bool isAwake = false;

            wakeListener.WakeWordDetected += () =>
            {
                try
                {
                    wakeListener.Stop(); // Auto Pause Wake Listener
                }
                catch { }

                synthesizer.Speak("Yes, sir?");
                visualizerServer.Broadcast("Speaking");
                Console.WriteLine("[WakeWord] Wake word detected. Switching to active listening...");
                visualizerServer.Broadcast("Listening");

                try
                {
                    activeRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                }
                catch (InvalidOperationException)
                {
                    // Recognizer already running
                }

                isAwake = true;
            };

            activeRecognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null)
                {
                    userInput = e.Result.Text;
                    Console.WriteLine($"You: {userInput}");
                    visualizerServer.Broadcast("Processing");
                }
            };

            wakeListener.Start();
            Console.WriteLine("JARVIS is sleeping. Listening for wake word...");
            synthesizer.Speak("System online, sir. Awaiting activation.");
            visualizerServer.Broadcast("Idle");

            try
            {
                var startupWeatherReport = await weatherCollector.GetWeatherAsync(cityName);
                if (!string.IsNullOrEmpty(startupWeatherReport))
                {
                    Console.WriteLine($"JARVIS (Startup Weather): {startupWeatherReport}");
                    synthesizer.Speak(startupWeatherReport);

                    string? condition = await weatherCollector.GetWeatherConditionAsync(cityName);
                    if (condition != null)
                        moodController.SetMoodFromWeather(condition);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup Weather Error]: {ex.Message}");
            }

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

                // === Weather Handling
                if (userInput.ToLower().Contains("weather") || userInput.ToLower().Contains("outside"))
                {
                    var weatherReport = await weatherCollector.GetWeatherAsync(cityName);
                    Console.WriteLine($"JARVIS: {weatherReport}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(weatherReport);
                    
                    ResetRecognition();
                    continue;
                }

                // === System Monitoring Handling
                if (userInput.ToLower().Contains("status report"))
                {
                    var report = await SystemMonitor.GetStatusReportAsync();
                    Console.WriteLine($"JARVIS: {report}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(report);
                    
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("cpu usage"))
                {
                    var cpu = await SystemMonitor.GetCpuUsageAsync();
                    string cpuReport = cpu < 0 ? "Unable to retrieve CPU usage, sir." : $"Current CPU usage is {cpu:F1}%.";
                    Console.WriteLine($"JARVIS: {cpuReport}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(cpuReport);
                    
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("memory usage"))
                {
                    var memory = SystemMonitor.GetMemoryUsage();
                    string memoryReport = memory < 0 ? "Unable to retrieve memory usage, sir." : $"Current memory usage is {memory:F1}%.";
                    Console.WriteLine($"JARVIS: {memoryReport}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(memoryReport);
                    
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("disk status"))
                {
                    var diskReport = SystemMonitor.GetDiskUsage();
                    Console.WriteLine($"JARVIS: {diskReport}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(diskReport);
                    
                    ResetRecognition();
                    continue;
                }
                if (userInput.ToLower().Contains("internet status"))
                {
                    var internetReport = await SystemMonitor.GetInternetStatusAsync();
                    Console.WriteLine($"JARVIS: {internetReport}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(internetReport);
                    
                    ResetRecognition();
                    continue;
                }

                // === Smart Home Control
                if (userInput.ToLower().Contains("turn on the light") || userInput.ToLower().Contains("lights on"))
                {
                    string room = userInput.Contains("living room") ? "living room" :
                                  userInput.Contains("kitchen") ? "kitchen" :
                                  "unknown room";
                    var result = await smartHomeController.TurnOnLightAsync(room);
                    Console.WriteLine($"JARVIS: {result}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(result);
                    
                    ResetRecognition();
                    continue;
                }

                if (userInput.ToLower().Contains("turn off the light") || userInput.ToLower().Contains("lights off"))
                {
                    string room = userInput.Contains("living room") ? "living room" :
                                  userInput.Contains("kitchen") ? "kitchen" :
                                  "unknown room";
                    var result = await smartHomeController.TurnOffLightAsync(room);
                    Console.WriteLine($"JARVIS: {result}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(result);
                    
                    ResetRecognition();
                    continue;
                }

                if (userInput.ToLower().Contains("open the garage door"))
                {
                    var result = await smartHomeController.OpenGarageDoorAsync();
                    Console.WriteLine($"JARVIS: {result}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(result);
                    
                    ResetRecognition();
                    continue;
                }

                if (userInput.ToLower().Contains("close the garage door"))
                {
                    var result = await smartHomeController.CloseGarageDoorAsync();
                    Console.WriteLine($"JARVIS: {result}");
                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(result);
                    
                    ResetRecognition();
                    continue;
                }

                // === Special Commands
                if (HandleSpecialCommands(userInput, moodController, memoryManager, conversation, synthesizer))
                {
                    ResetRecognition();
                    continue;
                }

                // === Regular AI Conversation
                conversation.AddUserMessage(userInput);

                var promptBuilder = new Core.PromptBuilder(moodController);
                foreach (var message in conversation.GetMessages())
                {
                    if (message.Role == "user")
                        promptBuilder.AddUserMessage(message.Content);
                    else if (message.Role == "assistant")
                        promptBuilder.AddAssistantMessage(message.Content);
                }

                var prompt = promptBuilder.BuildPrompt();


                // Load chat history
                foreach (var msg in conversation.GetMessages())
                {
                    if (msg.Role == "user")
                        promptBuilder.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant")
                        promptBuilder.AddAssistantMessage(msg.Content);
                }

              //  string prompt = promptBuilder.BuildPrompt();


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
                visualizerServer.Broadcast("Speaking");

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

            // Helper: resets after each conversation
            void ResetRecognition()
            {
                try
                {
                    activeRecognizer.RecognizeAsyncStop();
                }
                catch { }

                isAwake = false;
                userInput = "";

                try
                {
                    wakeListener.Start(); // Auto Resume Wake Listener
                }
                catch { }

                Console.WriteLine("[WakeWord] Returning to sleep mode...");
                visualizerServer.Broadcast("Idle");
            }

            // Helper: handle sarcasm/mood/memory special commands
            bool HandleSpecialCommands(string command, MoodController mood, MemoryManager memory, ConversationEngine convo, SpeechSynthesizer synth)
            {
                string cmd = command.ToLower();

                if (cmd.Contains("enable sarcasm"))
                {
                    mood.SarcasmEnabled = true;
                    Console.WriteLine("JARVIS: Sarcasm enabled.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Sarcasm enabled, sir.");
                    
                    return true;
                }
                if (cmd.Contains("disable sarcasm"))
                {
                    mood.SarcasmEnabled = false;
                    Console.WriteLine("JARVIS: Sarcasm disabled.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Sarcasm disabled, sir.");
                    
                    return true;
                }
                if (cmd.Contains("lighthearted"))
                {
                    mood.SetMood(Mood.Lighthearted);
                    Console.WriteLine("JARVIS: Mood changed to lighthearted.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Mood changed to lighthearted, sir.");
                    
                    return true;
                }
                if (cmd.Contains("serious"))
                {
                    mood.SetMood(Mood.Serious);
                    Console.WriteLine("JARVIS: Mood changed to serious.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Mood changed to serious, sir.");
                    
                    return true;
                }
                if (cmd.Contains("emergency mode"))
                {
                    mood.SetMood(Mood.Emergency);
                    Console.WriteLine("JARVIS: Emergency mode activated.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Emergency mode activated, sir.");
                    
                    return true;
                }
                if (cmd.Contains("resume normal operations"))
                {
                    mood.SetMood(Mood.Serious);
                    Console.WriteLine("JARVIS: Resuming normal operations.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Resuming normal operations, sir.");
                    
                    return true;
                }
                if (cmd.Contains("save memory"))
                {
                    memory.SaveMemory(convo.GetMessages());
                    Console.WriteLine("JARVIS: Memory saved.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Memory saved, sir.");
                    
                    return true;
                }
                if (cmd.Contains("load memory"))
                {
                    var loadedMessages = memory.LoadMemory();
                    convo.Reset();
                    foreach (var msg in loadedMessages)
                    {
                        if (msg.Role == "user")
                            convo.AddUserMessage(msg.Content);
                        else if (msg.Role == "assistant")
                            convo.AddAssistantMessage(msg.Content);
                    }
                    Console.WriteLine("JARVIS: Memory loaded.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Memory loaded, sir.");
                    
                    return true;
                }
                if (cmd.StartsWith("remember that"))
                {
                    var fact = command.Replace("remember that", "", StringComparison.OrdinalIgnoreCase).Trim();
                    convo.AddKnowledgeFact(fact);
                    Console.WriteLine("JARVIS: Fact recorded.");
                    visualizerServer.Broadcast("Processing");
                    synth.Speak("Fact recorded, sir.");
                    
                    return true;
                }

                return false;
            }
            static async Task SetMoodBasedOnWeatherAsync(WeatherCollector weatherCollector, MoodController moodController)
            {
                var condition = await weatherCollector.GetWeatherConditionAsync("La Grange");
                if (!string.IsNullOrEmpty(condition))
                {
                    moodController.SetMoodFromWeather(condition);
                    Console.WriteLine($"[WeatherMood] Weather: {condition} → Mood: {moodController.CurrentMood}");
                }
            }


        }
    }
}
