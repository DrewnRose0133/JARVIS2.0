// === MemoryEngine.cs ===
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JARVIS.Memory
{
    public class MemoryEngine
    {
        private readonly string _memoryFile = "jarvis_memory.json";
        private readonly Dictionary<string, string> _facts = new();

        public MemoryEngine()
        {
            Load();
        }

        public void Remember(string key, string value)
        {
            _facts[key.ToLower()] = value;
            Save();
        }

        public string? Recall(string key)
        {
            return _facts.TryGetValue(key.ToLower(), out var value) ? value : null;
        }

        public IEnumerable<string> Keys => _facts.Keys;

        private void Save()
        {
            var json = JsonSerializer.Serialize(_facts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_memoryFile, json);
        }

        private void Load()
        {
            if (File.Exists(_memoryFile))
            {
                var json = File.ReadAllText(_memoryFile);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                        _facts[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
