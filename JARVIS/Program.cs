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
using JARVIS.Audio;
using JARVIS.Python;
using JARVIS.UserPermissions;

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
            var voiceStyle = new VoiceStyleController(characterController);           
            var sceneManager = new SceneManager(smartHomeController);
            var memoryEngine = new MemoryEngine();
            var statusReporter = new StatusReporter(smartHomeController);
            var commandHandler = new CommandHandler(moodController, characterController, memoryEngine, weatherCollector, sceneManager, synthesizer, voiceStyle, statusReporter, cityName );

            string userId = "unknown"; // Default until recognized
            PermissionLevel permissionLevel = PermissionLevel.Guest;

            var wakeBuffer = new WakeAudioBuffer();
            wakeBuffer.Start();


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

            var wakeListener = StartupEngine.InitializeWakeWord("hey jarvis you there", () =>
            {
                wakeBuffer.SaveBufferedAudio("wake_word.wav");

                Console.WriteLine("Checking user voiceprint");
                var voiceAuthenticator = new VoiceAuthenticator();
                var userId = voiceAuthenticator.IdentifyUserFromWav("wake_word.wav");
                userId = userId.Split('\n').Last().Trim().ToLower();



                Console.WriteLine("Checking for user authorization");
                var permissionManager = new UserPermissionManager();
                var level = permissionManager.GetPermission(userId);

                Console.WriteLine($"Access level for {userId}: {level}");


                Console.WriteLine($"Recognized speaker: {userId}");


                lastInputTime = DateTime.Now;
                synthesizer.Speak(characterController.GetPreamble());
                visualizerServer.Broadcast("Speaking");
                Console.WriteLine("[WakeWord] Wake word detected. Switching to active listening...");
                visualizerServer.Broadcast("Listening");
                try { activeRecognizer.RecognizeAsync(RecognizeMode.Multiple); } catch { }
                isAwake = true;
            });

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Console.WriteLine("[Location] Attempting to auto-detect city...");
                cityName = await LocationHelper.GetCityAsync();
                Console.WriteLine($"[Location] Detected City: {cityName}");
                visualizerServer.Broadcast("Speaking");
            }

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

                if (await commandHandler.Handle(userInput))
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
                //Console.WriteLine($"Prompt:\n{prompt}");

                visualizerServer.Broadcast("Thinking");
                var response = await LocalAIAgent.GetResponseAsync(httpClient, modelId, prompt);

                var thought = Extract(response, "Thought:");
                var action = Extract(response, "Action:");
                var reply = Extract(response, "Response:");

                if (!string.IsNullOrEmpty(thought)) Console.WriteLine($"[Thought] {thought}");
                if (!string.IsNullOrEmpty(action)) Console.WriteLine($"[Action] {action}");

                conversation.AddAssistantMessage(reply);
                Console.WriteLine($"JARVIS: {reply}");

                var suggestion = suggestionEngine.CheckForSuggestion(DateTime.Now, latestWeather);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    Console.WriteLine($"JARVIS: {suggestion}");
                    synthesizer.Speak(suggestion);
                }

                visualizerServer.Broadcast("Speaking");
                voiceStyle.ApplyStyle(synthesizer);
                synthesizer.Speak(characterController.GetPreamble());
                synthesizer.Speak(reply);
                conversation.TrackConversation(userInput, reply);
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

            static string? Extract(string input, string label)
            {
                var lines = input.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                        return line.Replace(label, "", StringComparison.OrdinalIgnoreCase).Trim();
                }
                return null;
            }
        }
    }
}
