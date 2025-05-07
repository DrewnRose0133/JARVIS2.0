// === DJModeManager.cs ===
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using NAudio.Wave;
using JARVIS.Controllers;
using JARVIS.Visuals;

namespace JARVIS.Music
{
    public class DJModeManager
    {
        private readonly string _musicDirectory;
        private readonly SpeechSynthesizer _synthesizer;
        private readonly Random _random = new();
        private List<string> _tracks;
        private int _currentIndex = -1;
        private WaveOutEvent _player;
        private AudioFileReader _reader;
        private readonly LightSyncService _lightSync;
        public DJModeManager(string musicDir, SpeechSynthesizer synthesizer, LightSyncService lightSync)
        {
            _musicDirectory = musicDir;
            _synthesizer = synthesizer;
            _lightSync = lightSync;
            LoadTracks();
        }


        private void LoadTracks()
        {
            if (Directory.Exists(_musicDirectory))
                _tracks = new List<string>(Directory.GetFiles(_musicDirectory, "*.mp3"));
            else
                _tracks = new List<string>();
        }

        public void PlayNextTrack()
        {
            if (_tracks.Count == 0) return;

            _currentIndex = (_currentIndex + 1) % _tracks.Count;
            PlayTrack(_tracks[_currentIndex]);
        }

        public void ShuffleAndPlay()
        {
            if (_tracks.Count == 0) return;

            _currentIndex = _random.Next(_tracks.Count);
            PlayTrack(_tracks[_currentIndex]);
        }

        public void RepeatCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _tracks.Count) return;
            PlayTrack(_tracks[_currentIndex]);
        }

        public void Stop()
        {
            _player?.Stop();
            _reader?.Dispose();
            _lightSync?.Stop();

        }

        public string GetCurrentTrackName()
        {
            return _currentIndex >= 0 && _currentIndex < _tracks.Count
                ? Path.GetFileNameWithoutExtension(_tracks[_currentIndex])
                : "Nothing is playing";
        }

        private void PlayTrack(string path)
        {
            Stop();
            _lightSync?.StartBeatSync(122); // or use track tempo if available
            _reader = new AudioFileReader(path);
            _player = new WaveOutEvent();
            _player.Init(_reader);
            _player.Play();

            var name = Path.GetFileNameWithoutExtension(path);
            _synthesizer.SpeakAsync($"Now playing: {name}.");
        }

        public void PlayByMood(string mood)
        {
            if (_tracks.Count == 0) return;
            var filtered = _tracks.FindAll(t => t.ToLower().Contains(mood.ToLower()));
            if (filtered.Count == 0) filtered = _tracks;
            _currentIndex = _random.Next(filtered.Count);
            PlayTrack(filtered[_currentIndex]);
        }
    }
}
