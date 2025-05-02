using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JARVIS.Core;
using JARVIS.Shared;

namespace JARVIS
{
    public class JarvisInitializer
    {
        private readonly WeatherCollector _weatherCollector;
        private readonly MoodController _moodController;
        private readonly ILogger<JarvisInitializer> _logger;

        public JarvisInitializer(
            WeatherCollector weatherCollector,
            MoodController moodController,
            ILogger<JarvisInitializer> logger)
        {
            _weatherCollector = weatherCollector;
            _moodController = moodController;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("[Startup] Initializing JARVIS...");

            try
            {
                var condition = await _weatherCollector.GetWeatherConditionAsync("La Grange");
                if (!string.IsNullOrEmpty(condition))
                {
                    _moodController.SetMoodFromWeather(condition);
                    _logger.LogInformation($"[Mood] Initialized from weather: {condition} → {_moodController.CurrentMood}");
                }
                else
                {
                    _moodController.SetMood(Mood.Neutral);
                    _logger.LogWarning("[Mood] Weather unavailable, defaulting to Neutral mood.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Startup] Failed to initialize mood from weather.");
                _moodController.SetMood(Mood.Neutral);
            }

            _logger.LogInformation($"[Startup] JARVIS mood: {_moodController.CurrentMood}, Sarcasm: {_moodController.UseSarcasm}");
        }
    }
}
