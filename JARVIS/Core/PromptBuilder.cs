using System.Text;
using JARVIS;

namespace JARVIS.Core
{
    public class PromptBuilder
    {
        private readonly MoodController _mood;
        private readonly List<(string Role, string Content)> _messages = new();

        public PromptBuilder(MoodController mood)
        {
            _mood = mood;
        }

        public void AddUserMessage(string message) => _messages.Add(("user", message));
        public void AddAssistantMessage(string message) => _messages.Add(("assistant", message));
        public void Clear() => _messages.Clear();

        public string BuildPrompt()
        {
            var sb = new StringBuilder();

            // 🧠 Mood-Based Tone
            string tone = _mood.CurrentMood switch
            {
                Mood.Serious => "Respond formally and minimally, with calm authority.",
                Mood.Lighthearted => "Use cheerful, casual language and friendly tone.",
                Mood.Emergency => "Be brief, direct, and alert. Minimize distractions.",
                _ => "Speak clearly and with measured tone."
            };

            if (_mood.SarcasmEnabled && _mood.UseSarcasm)
            {
                tone += " Include dry wit, understated sarcasm, and British-style humor when appropriate.";
            }

            // 🧠 System Prompt
            sb.AppendLine("<|im_start|>system");
            sb.AppendLine($@"
                    You are J.A.R.V.I.S., an intelligent AI assistant modeled after the Iron Man films.
                    You speak like a composed British butler with subtle humor and logic.
                    {tone}
                    Never repeat the user's words. Speak aloud as if in real conversation. Stay brief and insightful."
            .Trim());
            sb.AppendLine("<|im_end|>");

            // 💬 Dialogue History
            foreach (var (role, content) in _messages)
            {
                sb.AppendLine($"<|im_start|>{role}");
                sb.AppendLine(content.Trim());
                sb.AppendLine("<|im_end|>");
            }

            sb.AppendLine("<|im_start|>assistant");

            return sb.ToString().Trim();
        }
    }
}
