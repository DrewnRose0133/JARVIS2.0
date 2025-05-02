using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JARVIS.Core
{
    public class SuggestionEngine
    {
        private bool _eveningPrompted = false;
        private bool _weatherPrompted = false;

        public string? CheckForSuggestion(DateTime now, string latestWeather)
        {
            if (!_eveningPrompted && now.Hour >= 22)
            {
                _eveningPrompted = true;
                return "It’s getting late, sir. Shall I dim the lights?";
            }

            if (!_weatherPrompted && latestWeather.ToLower().Contains("rain"))
            {
                _weatherPrompted = true;
                return "It appears to be raining. Should I close the garage door?";
            }

            return null;
        }

        public void Reset()
        {
            _eveningPrompted = false;
            _weatherPrompted = false;
        }
    }
}
