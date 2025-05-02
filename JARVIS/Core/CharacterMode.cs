using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JARVIS.Core
{
    public enum CharacterMode
    {
        Advisor,
        Companion,
        Emergency,
        Silent
    }

    public class CharacterModeController
    {
        public CharacterMode CurrentMode { get; set; } = CharacterMode.Advisor;

        public string GetSignatureResponse()
        {
            return CurrentMode switch
            {
                CharacterMode.Advisor => "Very good, sir.",
                CharacterMode.Companion => "You got it. Always happy to help.",
                CharacterMode.Emergency => "Directive acknowledged. Executing now.",
                CharacterMode.Silent => string.Empty,
                _ => "At your service."
            };
        }

        public string GetPreamble()
        {
            return CurrentMode switch
            {
                CharacterMode.Advisor => "As you requested, sir:",
                CharacterMode.Companion => "Sure thing, here's what I found:",
                CharacterMode.Emergency => "Priority instruction received:",
                CharacterMode.Silent => "",
                _ => ""
            };
        }


        public string DescribeMode()
        {
            return CurrentMode switch
            {
                CharacterMode.Advisor => "Strategic, formal, and precise.",
                CharacterMode.Companion => "Friendly, humorous, and casual.",
                CharacterMode.Emergency => "Tactical, fast, and serious.",
                CharacterMode.Silent => "Silent mode engaged. No speech output.",
                _ => "Neutral operational mode."
            };
        }
    }
}
