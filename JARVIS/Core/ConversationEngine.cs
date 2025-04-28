using System.Collections.Generic;
using System.Text;
using JARVIS.Models;

namespace JARVIS.Core
{
    public class ConversationEngine
    {
        private readonly List<Message> _messages = new List<Message>();
        private const int MaxMessages = 20;

        public void AddUserMessage(string message)
        {
            _messages.Add(new Message { Role = "user", Content = message });
            TrimIfNeeded();
        }

        public void AddAssistantMessage(string message)
        {
            _messages.Add(new Message { Role = "assistant", Content = message });
            TrimIfNeeded();
        }

        public void AddKnowledgeFact(string fact)
        {
            _messages.Add(new Message { Role = "system", Content = $"Fact: {fact}" });
            TrimIfNeeded();
        }

        public string BuildPrompt(MoodController moodController)
        {
            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine("You are JARVIS, a highly intelligent AI assistant with a dry wit and a refined British tone (Paul Bettany style).");

            if (moodController.CurrentMood == Mood.Lighthearted)
                systemPrompt.AppendLine("Maintain a charming and humorous lighthearted tone.");

            if (moodController.CurrentMood == Mood.Emergency)
                systemPrompt.AppendLine("Emergency mode: Speak seriously, directly, and without humor.");

            if (moodController.SarcasmEnabled)
                systemPrompt.AppendLine("Use subtle, dry sarcasm where appropriate.");

            systemPrompt.AppendLine();

            var promptBuilder = new StringBuilder(systemPrompt.ToString());

            foreach (var message in _messages)
            {
                if (message.Role == "user")
                    promptBuilder.AppendLine($"User: {message.Content}");
                else if (message.Role == "assistant")
                    promptBuilder.AppendLine($"JARVIS: {message.Content}");
                else if (message.Role == "system")
                    promptBuilder.AppendLine($"{message.Content}");
            }

            promptBuilder.AppendLine("JARVIS:");

            return promptBuilder.ToString();
        }

        public void Reset()
        {
            _messages.Clear();
        }

        private void TrimIfNeeded()
        {
            if (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }

        public int MessageCount => _messages.Count;
        public List<Message> GetMessages() => _messages;
    }
}
