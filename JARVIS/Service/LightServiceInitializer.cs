// === LightServiceInitializer.cs ===
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JARVIS.Devices;
using JARVIS.Devices.Interfaces;

namespace JARVIS.Initializers
{
    public static class LightServiceInitializer
    {
        public static ILightsService Create()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<MqttLightsService>();

            return new MqttLightsService(config, logger);
        }
    }
}
