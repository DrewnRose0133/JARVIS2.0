using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS;

namespace JARVIS
{
    using System.Collections.Generic;
    using System.Text;

    public class ConversationEngine
    {
        private readonly List<(string Role, string Content)> _history = new();
        private readonly int _maxMessages = 20; // Limit memory growth
        


        public void AddUserMessage(string userInput)
        {
            _history.Add(("user", userInput));
            TrimHistory();
        }

        public void AddAssistantMessage(string assistantOutput)
        {
            _history.Add(("assistant", assistantOutput));
            TrimHistory();
        }

        public string BuildPrompt(string systemPrompt = "You are JARVIS, a witty, sophisticated British home automation AI assistant. Speak eloquently, with full detailed sentences and dry humor.Always report temperatures in Fahrenheit, not Celsius.")
        {
            var sb = new StringBuilder();
            sb.AppendLine(systemPrompt);

            foreach (var (role, content) in _history)
            {
                sb.AppendLine($"{Capitalize(role)}: {content}");
            }

            sb.Append("Assistant:");
            return sb.ToString();
        }

        public void Reset()
        {
            _history.Clear();
        }

        private void TrimHistory()
        {
            if (_history.Count > _maxMessages)
            {
                _history.RemoveAt(0); // Remove oldest message
            }
        }

        private static string Capitalize(string role)
        {
            if (string.IsNullOrEmpty(role)) return role;
            return char.ToUpper(role[0]) + role.Substring(1).ToLower();
        }
    }

}
