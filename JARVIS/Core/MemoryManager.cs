using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JARVIS.Models;

namespace JARVIS.Core
{
    public class MemoryManager
    {
        private const string MemoryFilePath = "Resources/jarvis_memory.json";

        public void SaveMemory(List<Message> messages)
        {
            try
            {
                var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MemoryFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Memory Save Error]: {ex.Message}");
            }
        }

        public List<Message> LoadMemory()
        {
            try
            {
                if (!File.Exists(MemoryFilePath))
                    return new List<Message>();

                var json = File.ReadAllText(MemoryFilePath);
                var messages = JsonSerializer.Deserialize<List<Message>>(json);
                return messages ?? new List<Message>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Memory Load Error]: {ex.Message}");
                return new List<Message>();
            }
        }
    }
}
