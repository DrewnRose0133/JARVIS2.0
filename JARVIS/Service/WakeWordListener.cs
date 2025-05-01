using System;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;

namespace JARVIS.Services
{
    public class WakeWordListener
    {
        private readonly string _wakeWord;
        private SpeechRecognitionEngine _wakeRecognizer;
        private bool _isListening;

        public event Action WakeWordDetected;

        public WakeWordListener(string wakeWord = "jarvis you there?")
        {
            _wakeWord = wakeWord.ToLower();
        }

        public void Start()
        {
            if (_isListening)
                return;

            _wakeRecognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            _wakeRecognizer.SetInputToDefaultAudioDevice();

            var choices = new Choices();
            choices.Add(_wakeWord);

            var gb = new GrammarBuilder();
            gb.Append(choices);

            var grammar = new Grammar(gb);
            _wakeRecognizer.LoadGrammar(grammar);

            _wakeRecognizer.SpeechRecognized += WakeRecognizer_SpeechRecognized;
            _wakeRecognizer.RecognizeAsync(RecognizeMode.Multiple);

            _isListening = true;
            Console.WriteLine("[WakeWordListener] Listening for wake word...");
        }

        public void Stop()
        {
            if (!_isListening)
                return;

            _wakeRecognizer.RecognizeAsyncStop();
            _wakeRecognizer.Dispose();
            _isListening = false;
            Console.WriteLine("[WakeWordListener] Stopped listening for wake word.");
        }

        private void WakeRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.ToLower().Contains(_wakeWord))
            {
                Console.WriteLine("[WakeWordListener] Wake word detected!");
                WakeWordDetected?.Invoke();
            }
        }
    }
}
