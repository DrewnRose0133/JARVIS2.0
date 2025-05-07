// === MoodController.cs ===
using System;

namespace JARVIS.Controllers
{
    public enum Mood
    {
        Serious,
        Lighthearted,
        Emergency
    }

    public class MoodController
    {
        public Mood CurrentMood { get; set; } = Mood.Lighthearted;

        public event Action<Mood> MoodChanged;
        public bool SarcasmEnabled { get; set; } = true;

        public void AdjustMoodBasedOnWeather(string weatherDescription)
        {
            if (string.IsNullOrWhiteSpace(weatherDescription)) return;
            weatherDescription = weatherDescription.ToLower();

            if (weatherDescription.Contains("storm") || weatherDescription.Contains("alert") || weatherDescription.Contains("tornado"))
            {
                CurrentMood = Mood.Emergency;
            }
            else if (weatherDescription.Contains("rain") || weatherDescription.Contains("cloud"))
            {
                CurrentMood = Mood.Serious;
            }
            else if (weatherDescription.Contains("sunny") || weatherDescription.Contains("clear"))
            {
                CurrentMood = Mood.Lighthearted;
            }
        }

        public void AdjustToneBasedOnAttitude(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return;
            userInput = userInput.ToLower();

            if (userInput.Contains("please") || userInput.Contains("thank you"))
            {
                SarcasmEnabled = false; // respectful input
            }
            else if (userInput.Contains("hurry") || userInput.Contains("now") || userInput.Contains("idiot"))
            {
                SarcasmEnabled = true; // user is aggressive
            }
        }

        public void SetMood(Mood newMood)
        {
            if (CurrentMood != newMood)
            {
                CurrentMood = newMood;
                MoodChanged?.Invoke(newMood);
                Console.WriteLine($"Mood updated to {newMood}.");
            }
        }

        public string DescribeMood()
        {
            return CurrentMood switch
            {
            /**    Mood.Cheerful => "I’m feeling cheerful and optimistic.",
                Mood.Calm => "I’m in a calm and peaceful state.",
                Mood.Focused => "I’m fully focused on your tasks.",
                Mood.Witty => "Expect sharp and clever remarks.",
                Mood.Sarcastic => "My responses might come with a smirk.",
                Mood.Dramatic => "Everything is a scene, and I’m the star.",
                Mood.Curious => "I’m inquisitive and ready to explore.",
                Mood.Alert => "Heightened awareness mode is active.",
                Mood.Sleepy => "Energy saving mode... almost dreaming.", **/
                _ => "Just your average helpful assistant."
            };
        }

        public void ApplyPersonalityPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "witty advisor":
                    CurrentMood = Mood.Lighthearted;
                    SarcasmEnabled = true;
                    break;
                case "formal assistant":
                    CurrentMood = Mood.Serious;
                    SarcasmEnabled = false;
                    break;
                case "emergency mode":
                    CurrentMood = Mood.Emergency;
                    SarcasmEnabled = false;
                    break;
                default:
                    CurrentMood = Mood.Serious;
                    SarcasmEnabled = false;
                    break;
            }
        }
    }
}
