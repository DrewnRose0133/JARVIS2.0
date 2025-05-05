// === CommandHandler.cs ===
using System;
using System.Speech.Synthesis;
using JARVIS.Core;
using JARVIS.Services;
using JARVIS.Shared;

namespace JARVIS
{
    public class CommandHandler
    {
        private readonly MoodController _moodController;
        private readonly CharacterModeController _characterController;
        private readonly MemoryEngine _memoryEngine;
        private readonly WeatherCollector _weatherCollector;
        private readonly SceneManager _sceneManager;
        private readonly SpeechSynthesizer _synthesizer;
        private readonly string _city;
        private readonly VoiceStyleController _voiceStyle;
        private readonly StatusReporter _statusReporter;


        public CommandHandler(
            MoodController moodController,
            CharacterModeController characterController,
            MemoryEngine memoryEngine,
            WeatherCollector weatherCollector,
            SceneManager sceneManager,
            SpeechSynthesizer synthesizer,
            VoiceStyleController voiceStyle,
            StatusReporter statusReporter,
            string city)
        {
            _moodController = moodController;
            _characterController = characterController;
            _memoryEngine = memoryEngine;
            _weatherCollector = weatherCollector;
            _sceneManager = sceneManager;
            _synthesizer = synthesizer;
            _city = city;
            _voiceStyle = voiceStyle;
            _statusReporter = statusReporter;
        }

        public async Task<bool> Handle(string input)

        {
            input = input.ToLower();

            // Commands go below here


            if (input.StartsWith("mode "))
            {
                var modeName = input.Replace("mode", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (Enum.TryParse<CharacterMode>(modeName, true, out var newMode))
                {
                    _characterController.CurrentMode = newMode;
                    var description = _characterController.DescribeMode();
                    Console.WriteLine($"JARVIS: Character mode set to {newMode}.");
                    _voiceStyle.ApplyStyle(_synthesizer);
                    _synthesizer.Speak($"Character mode set to {newMode}, sir. {description}");
                    return true;
                }
            }
            if (input.Contains("cpu usage"))
            {
                var cpu = SystemMonitor.GetCpuUsageAsync().Result;
                var response = cpu >= 0 ? $"Current CPU usage is {cpu:F1} percent." : "Unable to retrieve CPU usage, sir.";
                Console.WriteLine($"JARVIS: {response}");
                _voiceStyle.ApplyStyle(_synthesizer);
                _synthesizer.Speak(response);
                return true;
            }

            if (input.Contains("memory usage"))
            {
                var memory = SystemMonitor.GetMemoryUsage();
                var response = memory >= 0 ? $"Current memory usage is {memory:F1} percent." : "Unable to retrieve memory usage, sir.";
                Console.WriteLine($"JARVIS: {response}");
                _synthesizer.Speak(response);
                return true;
            }

            if (input.Contains("internet status") || input.Contains("network status"))
            {
                var net = SystemMonitor.GetInternetStatusAsync().Result;
                Console.WriteLine($"JARVIS: {net}");
                _voiceStyle.ApplyStyle(_synthesizer);
                _synthesizer.Speak(net);
                return true;
            }
            if (input.StartsWith("mode "))
            {
                var modeName = input.Replace("mode", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (Enum.TryParse<CharacterMode>(modeName, true, out var newMode))
                {
                    _characterController.CurrentMode = newMode;
                    var description = _characterController.DescribeMode();
                    _synthesizer.Speak($"Character mode set to {newMode}, sir. {description}");
                    return true;
                }
            }

            if (input.StartsWith("personality "))
            {
                var preset = input.Replace("personality", "", StringComparison.OrdinalIgnoreCase).Trim();
                _moodController.ApplyPersonalityPreset(preset);
                _synthesizer.Speak($"Personality preset changed to {preset}, sir.");
                return true;
            }

            if (input.Contains("weather") || input.Contains("forecast") || input.Contains("outside"))
            {
                var weather = _weatherCollector.GetWeatherAsync(_city).Result;
                _moodController.AdjustMoodBasedOnWeather(weather);
                _synthesizer.Speak(weather);
                return true;
            }

            if (input.StartsWith("remember scene "))
            {
                var trimmed = input.Replace("remember scene", "", StringComparison.OrdinalIgnoreCase).Trim();
                var parts = trimmed.Split(" is ", 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    _memoryEngine.Remember($"scene:{parts[0].Trim()}", parts[1].Trim());
                    _synthesizer.Speak($"Scene {parts[0].Trim()} saved, sir.");
                    return true;
                }
            }

            if (input.StartsWith("run scene "))
            {
                var name = input.Replace("run scene", "", StringComparison.OrdinalIgnoreCase).Trim();
                var definition = _memoryEngine.Recall($"scene:{name}");
                if (!string.IsNullOrEmpty(definition))
                {
                    _synthesizer.Speak($"Executing {name} scene.");
                    _sceneManager.ExecuteSceneAsync(definition);
                }
                else _synthesizer.Speak($"Scene {name} not found, sir.");
                return true;
            }

            if (input.StartsWith("what is the") && input.Contains("scene"))
            {
                var name = input.Replace("what is the", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("scene", "", StringComparison.OrdinalIgnoreCase)
                                .Trim();
                var definition = _memoryEngine.Recall($"scene:{name}");
                _synthesizer.Speak(!string.IsNullOrEmpty(definition) ? $"Scene {name} includes: {definition}" : $"Scene {name} not found, sir.");
                return true;
            }

            if (input.StartsWith("remember "))
            {
                var trimmed = input.Replace("remember", "", StringComparison.OrdinalIgnoreCase).Trim();
                var parts = trimmed.Split(" is ", 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    _memoryEngine.Remember(parts[0].Trim(), parts[1].Trim());
                    _synthesizer.Speak($"I'll remember that {parts[0].Trim()} is {parts[1].Trim()}, sir.");
                    return true;
                }
            }

            if (input.StartsWith("what is ") || input.StartsWith("what's "))
            {
                var key = input.Replace("what is", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("what's", "", StringComparison.OrdinalIgnoreCase)
                               .Trim().TrimEnd('?');
                var value = _memoryEngine.Recall(key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _synthesizer.Speak($"You told me {key} is {value}, sir.");
                    return true;
                }
            }

            if (input.Contains("system status") || input.Contains("status report"))
            {
                var status = await _statusReporter.GetSystemStatusAsync();
                _voiceStyle.ApplyStyle(_synthesizer);
                _synthesizer.Speak(status);
                return true;
            }


            return false;
        }
    }
}
