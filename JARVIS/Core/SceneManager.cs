using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Shared;

namespace JARVIS.Core
{
    public class SceneManager
    {
        private readonly SmartHomeController _smartHome;

        public SceneManager(SmartHomeController smartHome)
        {
            _smartHome = smartHome;
        }

        public void ExecuteScene(string sceneDefinition)
        {
            var actions = sceneDefinition.Split(",", StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawAction in actions)
            {
                var action = rawAction.Trim().ToLower();
/**
                if (action == "lights off") _smartHome.TurnOffLights();
                else if (action == "lights on") _smartHome.TurnOnLights();
                else if (action == "fan on") _smartHome.TurnOnFan();
                else if (action == "fan off") _smartHome.TurnOffFan();
                else if (action == "volume low") _smartHome.SetVolume(20);
                else if (action == "volume high") _smartHome.SetVolume(80);
                else Console.WriteLine($"[SceneManager] Unknown action: {action}");
**/
            }
        }
    }
}
