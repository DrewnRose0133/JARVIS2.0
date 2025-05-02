using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JARVIS.Core;
using JARVIS.Shared;
using Microsoft.Extensions.Configuration;

namespace JARVIS.Service
{
    public static class ServiceInitializer
    {
        public static (WeatherCollector, SmartHomeController, SuggestionEngine, string city) InitializeServices(IConfiguration config)
        {
            var weatherApiKey = config["OpenWeather:ApiKey"];
            var city = config["OpenWeather:City"];

            var weatherCollector = new WeatherCollector(weatherApiKey);
            var smartHomeController = new SmartHomeController();
            var suggestionEngine = new SuggestionEngine();

            return (weatherCollector, smartHomeController, suggestionEngine, city);
        }
    }
}
