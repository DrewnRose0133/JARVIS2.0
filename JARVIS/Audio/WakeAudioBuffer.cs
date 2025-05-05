// === WakeAudioBuffer.cs ===
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Wave;

namespace JARVIS.Audio
{
    public class WakeAudioBuffer
    {
        private const int SampleRate = 16000;
        private const int Channels = 1;
        private const int BufferSeconds = 3;
        private const int BytesPerSample = 2; // 16-bit mono
        private const int BytesPerSecond = SampleRate * Channels * BytesPerSample;
        private readonly int _maxBufferSize = BytesPerSecond * BufferSeconds;

        private readonly ConcurrentQueue<byte[]> _buffer = new();
        private int _currentSize = 0;

        private WaveInEvent _waveIn;
        private bool _recording = false;

        public void Start()
        {
            if (_recording) return;

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, Channels),
                BufferMilliseconds = 100
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            _recording = true;

            Console.WriteLine("[WakeAudioBuffer] Recording started (rolling buffer).");
        }

        public void Stop()
        {
            if (!_recording) return;

            _waveIn.StopRecording();
            _waveIn.Dispose();
            _recording = false;

            Console.WriteLine("[WakeAudioBuffer] Recording stopped.");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _buffer.Enqueue(e.Buffer.Take(e.BytesRecorded).ToArray());
            _currentSize += e.BytesRecorded;

            while (_currentSize > _maxBufferSize && _buffer.TryDequeue(out var removed))
            {
                _currentSize -= removed.Length;
            }
        }

        public void SaveBufferedAudio(string filePath)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            Console.Write(fullPath);
            using var writer = new WaveFileWriter(fullPath, new WaveFormat(SampleRate, Channels));
            foreach (var chunk in _buffer)
            {
                writer.Write(chunk, 0, chunk.Length);
            }

            Console.WriteLine($"[WakeAudioBuffer] Wake audio saved to {fullPath}");
        }
    }
}
