using System.Collections.Generic;
using System.Text;

namespace JARVIS
{
    public class ConversationEngine
    {
        private readonly List<Message> _messages = new List<Message>();
        private const int MaxMessages = 20; // Auto-trim after 20 messages

        public void AddUserMessage(string message)
        {
            _messages.Add(new Message
            {
                Role = "user",
                Content = message
            });

            TrimIfNeeded();
        }

        public void AddAssistantMessage(string message)
        {
            _messages.Add(new Message
            {
                Role = "assistant",
                Content = message
            });

            TrimIfNeeded();
        }

        public string BuildPrompt(MoodController moodController)
        {
            var systemPrompt = new StringBuilder();

            systemPrompt.AppendLine("You are JARVIS, a highly intelligent, witty AI assistant with a refined British demeanor. You speak politely, concisely, and sound like Paul Bettany.");

            if (moodController.CurrentMood == Mood.Lighthearted)
                systemPrompt.AppendLine("Maintain a lighthearted and charming tone, occasionally making clever, tasteful jokes.");

            if (moodController.SarcasmEnabled)
                systemPrompt.AppendLine("Feel free to use subtle, dry sarcasm when appropriate, but always remain polite.");

            systemPrompt.AppendLine();

            var promptBuilder = new StringBuilder(systemPrompt.ToString());

            foreach (var message in _messages)
            {
                if (message.Role == "user")
                    promptBuilder.AppendLine($"User: {message.Content}");
                else if (message.Role == "assistant")
                    promptBuilder.AppendLine($"JARVIS: {message.Content}");
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
                _messages.RemoveAt(0); // Remove the oldest message
            }
        }

        public int MessageCount => _messages.Count;
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
