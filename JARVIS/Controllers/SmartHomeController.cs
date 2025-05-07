using System;
using System.Threading.Tasks;

namespace JARVIS.Controllers
{
    public class SmartHomeController
    {
        public async Task<string> TurnOnLightsAsync(string room)
        {
            // Replace with actual API calls to SmartThings or MQTT server
            Console.WriteLine($"Turning on {room} light...");
            await Task.Delay(500); // Simulate network delay
            return $"{room} light is now ON.";
        }

        public async Task<string> TurnOffLightsAsync(string room)
        {
            // Replace with actual API calls to SmartThings or MQTT server
            Console.WriteLine($"Turning off {room} light...");
            await Task.Delay(500); // Simulate network delay
            return $"{room} light is now OFF.";
        }

        public async Task<string> OpenGarageDoorAsync()
        {
            // Replace with actual API calls to SmartThings or MQTT server
            Console.WriteLine("Opening the garage door...");
            await Task.Delay(500); // Simulate network delay
            return "Garage door is now OPEN.";
        }

        public async Task<string> CloseGarageDoorAsync()
        {
            // Replace with actual API calls to SmartThings or MQTT server
            Console.WriteLine("Closing the garage door...");
            await Task.Delay(500); // Simulate network delay
            return "Garage door is now CLOSED.";
        }
    }
}
