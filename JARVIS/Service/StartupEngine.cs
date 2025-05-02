using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Services;

namespace JARVIS.Service
{
    public static class StartupEngine
    {
        public static VisualizerSocketServer InitializeVisualizer()
        {
            var server = new VisualizerSocketServer();
            server.Start();
            server.Broadcast("Idle");
            return server;
        }

        public static WakeWordListener InitializeWakeWord(string wakePhrase, Action onWake)
        {
            var listener = new WakeWordListener(wakePhrase);
            listener.WakeWordDetected += () =>
            {
                try { listener.Stop(); } catch { }
                onWake?.Invoke();
            };
            return listener;
        }
    }
}
