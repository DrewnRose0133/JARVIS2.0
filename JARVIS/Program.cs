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
            var visualizerServer = new VisualizerSocketServer();
            visualizerServer.Start();
            visualizerServer.Broadcast("Listening");

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var baseUrl = config["LocalAI:BaseUrl"];
            var modelId = config["LocalAI:ModelId"];
            var weatherApiKey = config["OpenWeather:ApiKey"];
            var cityName = config["OpenWeather:City"];
            var sleepTimeoutSeconds = int.TryParse(config["Settings:SleepTimeoutSeconds"], out var val) ? val : 15;

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(weatherApiKey))
            {
                Console.WriteLine("Error: Please configure LocalAI and OpenWeather settings in appsettings.json.");
                return;
            }

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var wakeListener = new WakeWordListener("hey jarvis you there");
            var activeRecognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            activeRecognizer.SetInputToDefaultAudioDevice();
            activeRecognizer.LoadGrammar(new DictationGrammar());

            using var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Volume = 100;
            synthesizer.Rate = 0;

            var moodController = new MoodController();
            moodController.ApplyPersonalityPreset("witty advisor");

            var promptEngine = new PromptEngine("JARVIS", "Witty", "Advisor", moodController);
            var conversation = new ConversationEngine(promptEngine);
            var memoryManager = new MemoryManager();
            var weatherCollector = new WeatherCollector(weatherApiKey);
            var smartHomeController = new SmartHomeController();

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Console.WriteLine("[Location] Attempting to auto-detect city...");
                cityName = await LocationHelper.GetCityAsync();
                Console.WriteLine($"[Location] Detected City: {cityName}");
                visualizerServer.Broadcast("Speaking");
            }

            string userInput = "";
            DateTime lastInputTime = DateTime.Now;
            bool isAwake = false;

            wakeListener.WakeWordDetected += () =>
            {
                try { wakeListener.Stop(); } catch { }
                synthesizer.Speak("Yes, sir?");
                visualizerServer.Broadcast("Speaking");
                Console.WriteLine("[WakeWord] Wake word detected. Switching to active listening...");
                visualizerServer.Broadcast("Listening");

                try { activeRecognizer.RecognizeAsync(RecognizeMode.Multiple); }
                catch (InvalidOperationException) { }

                isAwake = true;
            };

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

            try
            {
                var startupWeatherReport = await weatherCollector.GetWeatherAsync(cityName);
                if (!string.IsNullOrEmpty(startupWeatherReport))
                {
                    moodController.AdjustMoodBasedOnWeather(startupWeatherReport);
                    Console.WriteLine($"JARVIS (Startup Weather): {startupWeatherReport}");
                    synthesizer.Speak(startupWeatherReport);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Startup Weather Error]: {ex.Message}");
            }

            bool HandleJarvisCommand(string input)
            {
                input = input.ToLower();

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
                    var weatherReport = weatherCollector.GetWeatherAsync(cityName).Result;
                    moodController.AdjustMoodBasedOnWeather(weatherReport);
                    Console.WriteLine($"JARVIS: {weatherReport}");
                    synthesizer.Speak(weatherReport);
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

                bool handled = HandleJarvisCommand(userInput);
                if (handled)
                {
                    userInput = "";
                    continue;
                }
                else
                {
                    conversation.AddUserMessage(userInput);
                    var prompt = conversation.BuildPrompt();
                    Console.WriteLine($"Prompt:\n{prompt}");

                    visualizerServer.Broadcast("Thinking");
                    var response = await LocalAIAgent.GetResponseAsync(httpClient, modelId, prompt);

                    conversation.AddAssistantMessage(response);
                    Console.WriteLine($"JARVIS: {response}");

                    visualizerServer.Broadcast("Speaking");
                    synthesizer.Speak(response);

                    ResetRecognition();
                }
                
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
        }
    }
}
