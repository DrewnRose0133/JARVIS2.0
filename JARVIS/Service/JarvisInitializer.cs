using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Core;

namespace JARVIS.Service
{
    public class JarvisInitializer
    {
        public static (ConversationEngine, PromptEngine, MoodController, CharacterModeController) InitializeConversation()
        {
            var mood = new MoodController();
            var character = new CharacterModeController();
            var prompt = new PromptEngine("JARVIS", "Witty", "Advisor", mood, character);
            var engine = new ConversationEngine(prompt);
            return (engine, prompt, mood, character);
        }
    }

}
