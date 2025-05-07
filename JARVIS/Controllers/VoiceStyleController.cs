using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Core;

namespace JARVIS.Controllers
{
    public class VoiceStyleController
    {
        private readonly CharacterModeController _characterMode;

        public VoiceStyleController(CharacterModeController characterMode)
        {
            _characterMode = characterMode;
        }

        public void ApplyStyle(SpeechSynthesizer synthesizer)
        {
            switch (_characterMode.CurrentMode)
            {
                case CharacterMode.Advisor:
                    synthesizer.Rate = -1;
                    synthesizer.Volume = 90;
                    break;
                case CharacterMode.Companion:
                    synthesizer.Rate = 0;
                    synthesizer.Volume = 100;
                    break;
                case CharacterMode.Emergency:
                    synthesizer.Rate = 2;
                    synthesizer.Volume = 100;
                    break;
                case CharacterMode.Silent:
                    synthesizer.Volume = 0;
                    break;
                default:
                    synthesizer.Rate = 0;
                    synthesizer.Volume = 100;
                    break;
            }
        }
    }
}
