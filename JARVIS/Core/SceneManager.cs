// === SceneManager.cs ===
using System;
using System.Threading.Tasks;
using JARVIS.Controllers;

namespace JARVIS.Core
{
    public class SceneManager
    {
        private readonly SmartHomeController _smartHome;
        private readonly string _defaultRoom;

        public SceneManager(SmartHomeController smartHome, string defaultRoom = "livingroom")
        {
            _smartHome = smartHome;
            _defaultRoom = defaultRoom;
        }

        public async Task ExecuteSceneAsync(string sceneDefinition)
        {
            var actions = sceneDefinition.Split(",", StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawAction in actions)
            {
                var parts = rawAction.Trim().ToLower().Split(":", 2);
                var command = parts[0].Trim();
                var room = parts.Length > 1 ? parts[1].Trim() : _defaultRoom;

                switch (command)
                {
                    case "lights off": await _smartHome.TurnOffLightsAsync(room); break;
                    case "lights on": await _smartHome.TurnOnLightsAsync(room); break;
                    //case "fan on": await _smartHome.TurnOnFanAsync(room); break;
                   // case "fan off": await _smartHome.TurnOffFanAsync(room); break;
                   // case "volume low": await _smartHome.SetVolumeAsync(room, 20); break;
                   // case "volume high": await _smartHome.SetVolumeAsync(room, 80); break;
                    default:
                        Console.WriteLine($"[SceneManager] Unknown action: {command}");
                        break;
                }
            }
        }

        public void PreviewScene(string sceneDefinition)
        {
            var actions = sceneDefinition.Split(",", StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("[Scene Preview]");
            foreach (var rawAction in actions)
            {
                var parts = rawAction.Trim().ToLower().Split(":", 2);
                var command = parts[0].Trim();
                var room = parts.Length > 1 ? parts[1].Trim() : _defaultRoom;
                Console.WriteLine($" - {command} in {room}");
            }
        }
    }
}
