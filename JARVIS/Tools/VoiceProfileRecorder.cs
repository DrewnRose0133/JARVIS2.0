// === VoiceProfileRecorder.cs ===
using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace JARVIS.Tools
{
    public class VoiceProfileRecorder
    {
        public static void Launch()
        {
            Console.WriteLine("=== Voice Profile Recorder ===");
            Console.Write("Enter a user ID (e.g., andrew): ");
            var userId = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Invalid user ID. Aborting.");
                return;
            }

            var profileDir = Path.Combine("Profiles");
            Directory.CreateDirectory(profileDir);

            var filePath = Path.Combine(profileDir, userId + ".wav");

            Console.WriteLine("\nPlease say the following clearly after the beep:");
            Console.WriteLine("\"This is {0}, initializing voice recognition.\"", userId);
            Console.WriteLine("Recording starts in 3 seconds...");
            Thread.Sleep(3000);

            RecordToWav(filePath, 5);
            Console.WriteLine($"Recording complete. Saved as {filePath}\n");
        }

        private static void RecordToWav(string outputFile, int seconds)
        {
            using var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            };

            using var writer = new WaveFileWriter(outputFile, waveIn.WaveFormat);
            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
            };

            waveIn.StartRecording();
            Thread.Sleep(seconds * 1000);
            waveIn.StopRecording();
        }
    }
}
