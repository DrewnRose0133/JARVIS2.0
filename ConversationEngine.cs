using System.Collections.Generic;
using System.Text;

namespace JARVIS
{
    public class ConversationEngine
    {
        private readonly List<Message> _messages = new List<Message>();

        public void AddUserMessage(string message)
        {
            _messages.Add(new Message
            {
                Role = "user",
                Content = message
            });
        }

        public void AddAssistantMessage(string message)
        {
            _messages.Add(new Message
            {
                Role = "assistant",
                Content = message
            });
        }

        public string BuildPrompt()
        {
            var systemPrompt = "You are JARVIS, a sophisticated, witty AI assistant with a refined British demeanor, speaking politely and concisely like Paul Bettany. Always respond in a helpful, slightly dry, and intelligent tone.";
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

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

        public int MessageCount => _messages.Count;
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
