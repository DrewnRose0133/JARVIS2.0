using System.Collections.Generic;
using System.Text;
using JARVIS.Models;

namespace JARVIS.Core
{
    public class ConversationEngine
    {
        private readonly List<Message> _messages = new List<Message>();
        private const int MaxMessages = 20;
        private readonly PersonalityCore _personality;

        public ConversationEngine Initialize()
        {
            var personality = new PersonalityCore
            {
                Tone = "Sarcastic",
                Mood = "Curious",
                CharacterMode = "Companion"
            };

            var engine = new ConversationEngine(personality);

            engine.AddKnowledgeFact("The core AI personality module was initialized.");
            return engine;
        }

        // Constructor with personality injection
        public ConversationEngine(PersonalityCore personality)
        {
            _personality = personality;
        }

        // Optional fallback constructor (default witty personality)
        public ConversationEngine() : this(new PersonalityCore()) { }

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

            // Use mood/tone/personality to influence prompt style
            systemPrompt.AppendLine($"You are {_personality.Name}, a highly intelligent AI assistant.");
            systemPrompt.AppendLine($"Current Tone: {_personality.Tone}");
            systemPrompt.AppendLine($"Mood: {_personality.Mood}, Character Mode: {_personality.CharacterMode}");

            if (moodController.CurrentMood == Mood.Lighthearted)
                systemPrompt.AppendLine("Maintain a charming and humorous lighthearted tone.");

            if (moodController.CurrentMood == Mood.Emergency)
                systemPrompt.AppendLine("Emergency mode: Speak seriously, directly, and without humor.");

            if (moodController.SarcasmEnabled || _personality.Tone == "Sarcastic")
                systemPrompt.AppendLine("Use subtle, dry sarcasm where appropriate.");

            systemPrompt.AppendLine();

            var promptBuilder = new StringBuilder(systemPrompt.ToString());

            foreach (var message in _messages)
            {
                if (message.Role == "user")
                    promptBuilder.AppendLine($"User: {message.Content}");
                else if (message.Role == "assistant")
                    promptBuilder.AppendLine($"{_personality.Name}: {message.Content}");
                else if (message.Role == "system")
                    promptBuilder.AppendLine($"{message.Content}");
            }

            promptBuilder.AppendLine($"{_personality.Name}:");

            return promptBuilder.ToString();
        }

        public void Reset() => _messages.Clear();

        private void TrimIfNeeded()
        {
            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);
        }

        public int MessageCount => _messages.Count;
        public List<Message> GetMessages() => _messages;
    }
}
