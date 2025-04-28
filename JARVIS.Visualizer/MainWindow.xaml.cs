using JARVIS.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace JARVIS.Visualizer
{
    public sealed partial class MainWindow : Window
    {
        private readonly SmartHomeController _smartHomeController;
        private readonly WeatherCollector _weatherCollector;

        public MainWindow()
        {
            this.InitializeComponent();
            _smartHomeController = new SmartHomeController();
            _weatherCollector = new WeatherCollector("your_weather_api_key");

            // Start UI updates (CPU, Memory, Weather)
            UpdateVisualizerState("Idle", "Gray");
            StartBackgroundMonitoring();
        }

        // Method to update the UI (avatar state, text, color)
        public void UpdateVisualizerState(string state, string color)
        {
            StateText.Text = $"JARVIS is {state}";
            StatusCircle.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.FromArgb(255, (byte)(color == "Green" ? 0 : 255), 255, 0));
        }

        // Method to simulate background monitoring of CPU, Memory, etc.
        public async Task StartBackgroundMonitoring()
        {
            while (true)
            {
                // Update CPU Usage
                var cpuUsage = await SystemMonitor.GetCpuUsageAsync();
                UpdateVisualizerState($"CPU: {cpuUsage:F1}%", "Green");

                // Update Memory Usage
                var memoryUsage = SystemMonitor.GetMemoryUsage();
                if (memoryUsage > 85)
                {
                    UpdateVisualizerState($"Memory: {memoryUsage:F1}%", "Red");  // Critical Memory
                }
                else
                {
                    UpdateVisualizerState($"Memory: {memoryUsage:F1}%", "Yellow");
                }

                // Update Weather
                var weather = await _weatherCollector.GetWeatherAsync("New York");
                StateText.Text += $"\nWeather: {weather}";

                await Task.Delay(2000); // Check every 2 seconds
            }
        }

        // Update Avatar and Mood based on speech mode
        public void UpdateMoodAvatar(string mood)
        {
            // Change avatar or state based on mood (e.g., angry, happy)
            if (mood == "Happy")
            {
                JarvisAvatar.Source = new Uri("path_to_happy_avatar.png");
            }
            else if (mood == "Serious")
            {
                JarvisAvatar.Source = new Uri("path_to_serious_avatar.png");
            }
            else
            {
                JarvisAvatar.Source = new Uri("path_to_neutral_avatar.png");
            }
        }
    }
}
