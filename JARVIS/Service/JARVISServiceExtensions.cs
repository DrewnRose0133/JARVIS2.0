using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using JARVIS.Config;
using JARVIS.Core;
using JARVIS.Service;
using JARVIS.Shared;

namespace JARVIS.Core
{
    public static class JarvisServiceExtensions
    {
        public static IServiceCollection AddJarvisServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind config settings from appsettings.json
            services.Configure<LocalAISettings>(configuration.GetSection("LocalAI"));

            // Core services
            services.AddSingleton<MoodController>();
            services.AddSingleton<WeatherCollector>();
            services.AddSingleton<ConversationEngine>();
         //   services.AddSingleton<PromptBuilderFactory>();

            // Infrastructure services
            services.AddHttpClient<LLMClient>(); // Injected HttpClient for LocalAI
            services.AddSingleton<JarvisInitializer>();

            // Background hosted service (run on startup)
            services.AddHostedService<JarvisHostedService>();

            return services;
        }
    }
}
