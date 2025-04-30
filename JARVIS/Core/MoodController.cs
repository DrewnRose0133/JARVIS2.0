using System;
using System.Collections.Generic;

namespace JARVIS
{
    public enum Mood
    {
        Serious,
        Lighthearted,
        Emergency,
        Professional,
        Witty,
        Sarcastic,
        Neutral,
        Happy,
        Irritated
    }

    public class MoodController
    {
        public Mood CurrentMood { get; private set; } = Mood.Lighthearted;

        // Optional: external override toggle (can disable sarcasm regardless of mood)
        public bool SarcasmEnabled { get; set; } = true;

        // Internal sarcasm setting per mood
        private readonly Dictionary<Mood, bool> _sarcasmByMood = new()
        {
            { Mood.Serious, false },
            { Mood.Lighthearted, true },
            { Mood.Emergency, false },
            { Mood.Professional, false },
            { Mood.Witty, true },
            { Mood.Sarcastic, true },
            { Mood.Neutral, false },
            { Mood.Happy, true },
            { Mood.Irritated, true }
        };

        private readonly Dictionary<string, Mood> _moodByWeather = new()
        {
            { "Clear", Mood.Happy },
            { "Sunny", Mood.Witty },
            { "Clouds", Mood.Neutral },
            { "Rain", Mood.Sarcastic },
            { "Thunderstorm", Mood.Irritated },
            { "Snow", Mood.Professional },
            { "Fog", Mood.Serious }
        };


        // Public read-only property to expose sarcasm state
        public bool UseSarcasm => SarcasmEnabled &&
            _sarcasmByMood.TryGetValue(CurrentMood, out var sarcastic) && sarcastic;

        // Optional mood setter with logging
        public void SetMood(Mood newMood)
        {
            CurrentMood = newMood;
            Console.WriteLine($"[MoodController] Mood changed to: {newMood} | Sarcasm: {UseSarcasm}");
        }

        public void SetMoodFromWeather(string weatherCondition)
        {
            if (_moodByWeather.TryGetValue(weatherCondition, out var mood))
            {
                SetMood(mood);
            }
            else
            {
                SetMood(Mood.Neutral); // default fallback
            }
        }

    }
}
