using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using JARVIS.Core;

namespace JARVIS.Service
{
    public class JarvisHostedService : IHostedService
    {
        private readonly ConversationEngine _engine;

        public JarvisHostedService(ConversationEngine engine)
        {
            _engine = engine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Example interaction
            _engine.AddUserMessage("Hello, JARVIS.");
            var prompt = _engine.BuildPrompt(new MoodController()); // Ensure MoodController exists
            Console.WriteLine(prompt);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
