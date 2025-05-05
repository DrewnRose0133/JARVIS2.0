// === VoiceAuthenticator.cs ===
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace JARVIS.Python
{
    public class VoiceAuthenticator
    {
        private readonly string _pythonPath;
        private readonly string _scriptPath;

        public VoiceAuthenticator(string pythonPath = "python", string scriptRelativePath = "..\\..\\..\\Python\\identify_user.py")
        {
            _pythonPath = pythonPath;
            _scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptRelativePath));

            if (!File.Exists(_scriptPath))
                Console.WriteLine($"[ERROR] Python script not found at: {_scriptPath}");
        }

        public string RecordAndIdentifyUser(string outputPath, int recordSeconds = 5)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outputPath);
            RecordToWav(fullPath, recordSeconds);
            return IdentifyUserFromWav(fullPath);
        }

        public void RecordToWav(string outputFile, int seconds)
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

        public string IdentifyUserFromWav(string wavPath)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, wavPath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Audio file not found at: {fullPath}");
                return "unknown";
            }

            var psi = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\" \"{fullPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd().Trim();
            string stderr = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine($"[Python Error] {stderr}");

            if (string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine("[VoiceAuthenticator] No output from Python script.");
                return "unknown";
            }

            return output;
        }
    }
}
