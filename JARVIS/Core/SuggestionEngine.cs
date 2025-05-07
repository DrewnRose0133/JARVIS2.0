using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Memory;
using JARVIS.Shared;
using JARVIS.UserSettings;

namespace JARVIS.Core
{
    public class SuggestionEngine
    {
        private bool _eveningPrompted = false;
        private bool _weatherPrompted = false;

        public string? CheckForSuggestion(DateTime now)
        {
            var userId = UserSessionManager.CurrentUserId;
            var history = CommandHistoryManager.GetRecentCommands(userId, 10);

            if (!history.Any())
                return null;

            if (!_eveningPrompted && now.Hour >= 22)
            {
                _eveningPrompted = true;
                return "It’s getting late, sir. Shall I dim the lights?";
            }

            /**  if (!_weatherPrompted && latestWeather.ToLower().Contains("rain"))
              {
                  _weatherPrompted = true;
                  return "It appears to be raining. Should I close the garage door?";
              } **/
            if (history.Count(c => c.Contains("bedtime")) >= 3)
                return "You've used bedtime mode a lot recently. Would you like to activate it now?";

            if (history.Count(c => c.Contains("music")) >= 3)
                return "It seems you enjoy music often. Shall I resume your last playlist?";

            if (history.Any(c => c.Contains("weather")) && DateTime.Now.Hour >= 7 && DateTime.Now.Hour <= 9)
                return "Would you like the morning weather update?";

            if (history.Any(c => c.Contains("scene")) && DateTime.Now.Hour >= 21)
                return "It’s getting late. Should I start the evening scene?";

            return null;
        }

        public string? GetSuggestion()
        {
            DateTime dateTime = DateTime.Now;

            CheckForSuggestion(dateTime);

            return null;
        }

        public void Reset()
        {
            _eveningPrompted = false;
            _weatherPrompted = false;
        }
    }
}
