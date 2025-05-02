using System.Collections.Generic;
using System.Text;
using JARVIS.Models;

namespace JARVIS.Core
{
    public class ConversationEngine
    {
        private readonly List<Message> _messages = new List<Message>();
        private const int MaxMessages = 20;
        private readonly PromptEngine _promptEngine;
        private string _lastUserMessage = "";
        private string _lastAssistantResponse = "";

        public ConversationEngine(PromptEngine promptEngine)
        {
            _promptEngine = promptEngine;
        }

        public void AddUserMessage(string message)
        {
            _messages.Add(new Message { Role = "user", Content = message });
            TrimIfNeeded();
        }

        public bool IsRepeatedInput(string input)
        {
            return _lastUserMessage.Equals(input, StringComparison.OrdinalIgnoreCase);
        }

        public void TrackConversation(string userMessage, string assistantResponse)
        {
            _lastUserMessage = userMessage;
            _lastAssistantResponse = assistantResponse;
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

        public string BuildPrompt()
        {
            return _promptEngine.BuildPrompt(_messages);
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