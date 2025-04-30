
using Microsoft.Extensions.DependencyInjection;
using JARVIS.Shared;

namespace JARVIS.Core
{
    public static class JarvisServiceExtensions
    {
        public static void AddJarvisServices(this IServiceCollection services)
        {
            services.AddSingleton<MoodController>();
            services.AddSingleton<WeatherCollector>();
            services.AddSingleton<ConversationEngine>();
           // services.AddSingleton<PromptBuilderFactory>();
           // services.AddHostedService<JarvisHostedService>();
        }
    }
}
