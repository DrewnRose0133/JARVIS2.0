// === StatusReporter.cs ===
using System;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Controllers;
using JARVIS.Services;

namespace JARVIS.Core
{
    public class StatusReporter
    {
        private readonly SmartHomeController _smartHome;

        public StatusReporter(SmartHomeController smartHome)
        {
            _smartHome = smartHome;
        }

        public async Task<string> GetSystemStatusAsync()
        {
            var report = new StringBuilder();
            report.AppendLine("System status report:");

            try
            {
               // var lightsStatus = await _smartHome.GetLightsStatusAsync();
              //  report.AppendLine($"- Lights: {lightsStatus}");

              //  var fanStatus = await _smartHome.GetFanStatusAsync();
              //  report.AppendLine($"- Fan: {fanStatus}");

               // var volumeLevel = await _smartHome.GetVolumeAsync();
               // report.AppendLine($"- Volume level: {volumeLevel}%");
            }
            catch (Exception ex)
            {
                report.AppendLine("- Error retrieving one or more statuses.");
                Console.WriteLine("[StatusReporter] Exception: " + ex.Message);
            }

            return report.ToString();
        }
    }
}
