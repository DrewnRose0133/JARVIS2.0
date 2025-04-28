using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JARVIS.Shared
{
    public static class SystemMonitor
    {
        public static async Task<float> GetCpuUsageAsync()
        {
            try
            {
                // Create a PerformanceCounter to measure CPU usage
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // First call initializes the counter, so we need to call it once to get a baseline.
                cpuCounter.NextValue();
                await Task.Delay(500); // Allow some time for the counter to gather data.

                // Get the actual CPU usage
                return cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMonitor CPU Error]: {ex.Message}");
                return -1;
            }
        }

        public static float GetMemoryUsage()
        {
            try
            {
                var gcInfo = GC.GetGCMemoryInfo();
                var totalMemory = gcInfo.TotalAvailableMemoryBytes;
                var usedMemory = totalMemory - GC.GetTotalMemory(false);
                var percentUsed = (float)usedMemory / totalMemory * 100;
                return percentUsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMonitor Memory Error]: {ex.Message}");
                return -1;
            }
        }

        public static async Task<string> GetStatusReportAsync()
        {
            var cpu = await GetCpuUsageAsync();
            var memory = GetMemoryUsage();

            if (cpu < 0 || memory < 0)
                return "Unable to retrieve system status at this time, sir.";

            return $"CPU usage is {cpu:F1}% and memory usage is {memory:F1}%.";
        }

        public static string GetDiskUsage()
        {
            try
            {
                var drive = DriveInfo.GetDrives();
                foreach (var d in drive)
                {
                    if (d.IsReady && d.Name == "C:\\")
                    {
                        var totalGB = d.TotalSize / (1024 * 1024 * 1024);
                        var freeGB = d.TotalFreeSpace / (1024 * 1024 * 1024);
                        var usedGB = totalGB - freeGB;
                        var usedPercent = (float)usedGB / totalGB * 100;
                        return $"C drive is {usedPercent:F1}% full, with {freeGB} gigabytes free, sir.";
                    }
                }
                return "Unable to read disk information, sir.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMonitor Disk Error]: {ex.Message}");
                return "Unable to retrieve disk status, sir.";
            }
        }

        public static async Task<string> GetInternetStatusAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var result = await client.GetAsync("http://www.google.com");

                if (result.IsSuccessStatusCode)
                    return "Internet connection is active, sir.";
                else
                    return "Internet connection seems offline, sir.";
            }
            catch
            {
                return "Internet connection seems offline, sir.";
            }
        }
    }
}
