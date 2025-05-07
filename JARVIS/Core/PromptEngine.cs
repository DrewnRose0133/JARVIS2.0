// === PromptEngine.cs ===
using System.Text;
using JARVIS.Models;
using System.Collections.Generic;
using JARVIS.Controllers;

namespace JARVIS.Core
{
    public class PromptEngine
    {
        private readonly string _assistantName;
        private readonly string _tone;
        private readonly string _mode;
        private readonly MoodController _moodController;
        private readonly CharacterModeController _characterController;

        public PromptEngine(string assistantName, string tone, string mode, MoodController moodController, CharacterModeController characterController)
        {
            _assistantName = assistantName;
            _tone = tone;
            _mode = mode;
            _moodController = moodController;
            _characterController = characterController;
        }

        public string BuildPrompt(List<Message> messages)
        {
            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine($"You are J.A.R.V.I.S., an intelligent AI assistant modeled after the Iron Man films." +
                $"You speak like a composed British butler with subtle humor and logic.");
            systemPrompt.AppendLine($"Tone: {_tone}, Mood: {_moodController.CurrentMood}, Mode: {_mode}, Character: {_characterController.CurrentMode}");
            systemPrompt.AppendLine("// " + _characterController.DescribeMode());
            systemPrompt.AppendLine("Always reply in this format:");
            systemPrompt.AppendLine("Thought: <your reasoning>");
            systemPrompt.AppendLine("Action: <what you're doing>");
            systemPrompt.AppendLine("Response: <what to say to the user>");


            if (_moodController.CurrentMood == Mood.Lighthearted)
                systemPrompt.AppendLine("Maintain a charming and humorous lighthearted tone.");

            if (_moodController.CurrentMood == Mood.Emergency)
                systemPrompt.AppendLine("Emergency mode: Speak seriously, directly, and without humor.");

            if (_moodController.SarcasmEnabled || _tone.ToLower() == "sarcastic")
                systemPrompt.AppendLine("Use subtle, dry sarcasm where appropriate.");

            systemPrompt.AppendLine();

            foreach (var message in messages)
            {
                if (message.Role == "user")
                    systemPrompt.AppendLine($"User: {message.Content}");
                else if (message.Role == "assistant")
                    systemPrompt.AppendLine($"{_assistantName}: {message.Content}");
                else if (message.Role == "system")
                    systemPrompt.AppendLine(message.Content);
            }

            systemPrompt.AppendLine($"{_assistantName}:");
            return systemPrompt.ToString();
        }
    }
}
