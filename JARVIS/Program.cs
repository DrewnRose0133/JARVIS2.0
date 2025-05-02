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
using JARVIS.Service;

namespace JARVIS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var visualizerServer = StartupEngine.InitializeVisualizer();
            bool isAwake = false;
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var baseUrl = config["LocalAI:BaseUrl"];
            var modelId = config["LocalAI:ModelId"];
            var sleepTimeoutSeconds = int.TryParse(config["Settings:SleepTimeoutSeconds"], out var val) ? val : 15;

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Please configure LocalAI settings in appsettings.json.");
                return;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

            var (weatherCollector, smartHomeController, suggestionEngine, cityName) = ServiceInitializer.InitializeServices(config);
            var activeRecognizer = AudioEngine.InitializeRecognizer();
            using var synthesizer = AudioEngine.InitializeSynthesizer();
            var (conversation, promptEngine, moodController, characterController) = JarvisInitializer.InitializeConversation();

            var wakeListener = StartupEngine.InitializeWakeWord("hey jarvis you there", () =>
            {
                synthesizer.Speak(characterController.GetPreamble());
                visualizerServer.Broadcast("Speaking");
                Console.WriteLine("[WakeWord] Wake word detected. Switching to active listening...");
                visualizerServer.Broadcast("Listening");
                try { activeRecognizer.RecognizeAsync(RecognizeMode.Multiple); } catch { }
                isAwake = true;
            });

            var memoryManager = new MemoryManager();

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Console.WriteLine("[Location] Attempting to auto-detect city...");
                cityName = await LocationHelper.GetCityAsync();
                Console.WriteLine($"[Location] Detected City: {cityName}");
                visualizerServer.Broadcast("Speaking");
            }

            string userInput = "";
            DateTime lastInputTime = DateTime.Now;
            Task.Run(() =>
            {
                while (true)
                {
                    var typedInput = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(typedInput))
                    {
                        userInput = typedInput;
                        isAwake = true;
                        lastInputTime = DateTime.Now;
                    }
                }
            });
            string latestWeather = await weatherCollector.GetWeatherAsync(cityName);

            if (!string.IsNullOrEmpty(latestWeather))
            {
                moodController.AdjustMoodBasedOnWeather(latestWeather);
                Console.WriteLine($"JARVIS (Startup Weather): {latestWeather}");
                synthesizer.Speak(latestWeather);

                var suggestion = suggestionEngine.CheckForSuggestion(DateTime.Now, latestWeather);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    Console.WriteLine($"JARVIS: {suggestion}");
                    synthesizer.Speak(suggestion);
                }
            }

            activeRecognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null)
                {
                    lastInputTime = DateTime.Now;
                    userInput = e.Result.Text;
                    Console.WriteLine($"You: {userInput}");
                    moodController.AdjustToneBasedOnAttitude(userInput);
                    visualizerServer.Broadcast("Processing");
                }
            };

            wakeListener.Start();
            Console.WriteLine("JARVIS is sleeping. Listening for wake word...");
            synthesizer.Speak("System online, sir. Awaiting activation.");
            visualizerServer.Broadcast("Idle");

            while (true)
            {
                if (isAwake && (DateTime.Now - lastInputTime).TotalSeconds > sleepTimeoutSeconds)
                {
                    Console.WriteLine("No input received. Returning to sleep mode.");
                    synthesizer.Speak($"No input received for {sleepTimeoutSeconds} seconds. Returning to sleep mode, sir.");
                    ResetRecognition();
                    continue;
                }

                if (!isAwake || string.IsNullOrWhiteSpace(userInput))
                {
                    await Task.Delay(500);
                    continue;
                }

                if (HandleJarvisCommand(userInput))
                {
                    userInput = "";
                    continue;
                }

                if (conversation.IsRepeatedInput(userInput))
                {
                    var interruption = "We've already discussed that, sir. Shall I repeat myself?";
                    Console.WriteLine($"JARVIS: {interruption}");
                    synthesizer.Speak(interruption);
                    ResetRecognition();
                    continue;
                }

                conversation.AddUserMessage(userInput);
                var prompt = conversation.BuildPrompt();
                Console.WriteLine($"Prompt:\n{prompt}");

                visualizerServer.Broadcast("Thinking");
                var response = await LocalAIAgent.GetResponseAsync(httpClient, modelId, prompt);
                conversation.AddAssistantMessage(response);
                Console.WriteLine($"JARVIS: {response}");

                var suggestion = suggestionEngine.CheckForSuggestion(DateTime.Now, latestWeather);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    Console.WriteLine($"JARVIS: {suggestion}");
                    synthesizer.Speak(suggestion);
                }

                visualizerServer.Broadcast("Speaking");
                synthesizer.Speak(characterController.GetPreamble());
                synthesizer.Speak(response);
                conversation.TrackConversation(userInput, response);
                ResetRecognition();
            }

            void ResetRecognition()
            {
                try { activeRecognizer.RecognizeAsyncStop(); } catch { }
                isAwake = false;
                userInput = "";
                try { wakeListener.Start(); } catch { }
                Console.WriteLine("[WakeWord] Returning to sleep mode...");
                visualizerServer.Broadcast("Idle");
            }

            bool HandleJarvisCommand(string input)
            {
                input = input.ToLower();

                if (input.StartsWith("mode "))
                {
                    var modeName = input.Replace("mode", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (Enum.TryParse<CharacterMode>(modeName, true, out var newMode))
                    {
                        characterController.CurrentMode = newMode;
                        var description = characterController.DescribeMode();
                        Console.WriteLine($"JARVIS: Character mode set to {newMode}.");
                        synthesizer.Speak($"Character mode set to {newMode}, sir. {description}");
                        return true;
                    }
                }

                if (input.StartsWith("personality "))
                {
                    var preset = input.Replace("personality", "", StringComparison.OrdinalIgnoreCase).Trim();
                    moodController.ApplyPersonalityPreset(preset);
                    Console.WriteLine($"JARVIS: Personality preset changed to {preset}.");
                    synthesizer.Speak($"Personality preset changed to {preset}, sir.");
                    return true;
                }

                if (input.Contains("weather") || input.Contains("forecast") || input.Contains("outside"))
                {
                    latestWeather = weatherCollector.GetWeatherAsync(cityName).Result;
                    moodController.AdjustMoodBasedOnWeather(latestWeather);
                    Console.WriteLine($"JARVIS: {latestWeather}");
                    synthesizer.Speak(latestWeather);
                    return true;
                }

                if (input.Contains("cpu usage"))
                {
                    var cpu = SystemMonitor.GetCpuUsageAsync().Result;
                    var response = cpu >= 0 ? $"Current CPU usage is {cpu:F1} percent." : "Unable to retrieve CPU usage, sir.";
                    Console.WriteLine($"JARVIS: {response}");
                    synthesizer.Speak(response);
                    return true;
                }

                if (input.Contains("memory usage"))
                {
                    var memory = SystemMonitor.GetMemoryUsage();
                    var response = memory >= 0 ? $"Current memory usage is {memory:F1} percent." : "Unable to retrieve memory usage, sir.";
                    Console.WriteLine($"JARVIS: {response}");
                    synthesizer.Speak(response);
                    return true;
                }

                if (input.Contains("internet status") || input.Contains("network status"))
                {
                    var net = SystemMonitor.GetInternetStatusAsync().Result;
                    Console.WriteLine($"JARVIS: {net}");
                    synthesizer.Speak(net);
                    return true;
                }

                return false;
            }
        }
    }
}
