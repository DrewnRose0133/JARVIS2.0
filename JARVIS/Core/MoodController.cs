using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JARVIS
{
    public enum Mood
    {
        Serious,
        Lighthearted,
        Emergency
    }

    public class MoodController
    {
        public Mood CurrentMood { get; set; } = Mood.Lighthearted;
        public bool SarcasmEnabled { get; set; } = true;
    }
}
