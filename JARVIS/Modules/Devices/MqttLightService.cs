using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JARVIS.Modules.Devices.Interfaces;

namespace JARVIS.Modules.Devices
{
    /// <summary>
    /// MQTT-based implementation of ILightsService using MQTTnet v5.
    /// </summary>
    public class MqttLightsService : ILightsService, IAsyncDisposable
    {
        private readonly IMqttClient _client;
        private readonly ILogger<MqttLightsService> _logger;

        public MqttLightsService(
            IConfiguration configuration,
            ILogger<MqttLightsService> logger)
        {
            _logger = logger;

            // Build client options from configuration
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(
                    configuration["SmartHome:Mqtt:Broker"],
                    int.Parse(configuration["SmartHome:Mqtt:Port"] ?? "1883"))
                .WithCredentials(
                    configuration["SmartHome:Mqtt:Username"],
                    configuration["SmartHome:Mqtt:Password"])
                .WithCleanSession()
                .Build();

            // Create and connect the MQTT client
            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();
            _client.ConnectAsync(mqttOptions).GetAwaiter().GetResult();
            _logger.LogInformation("Connected to MQTT broker at {Broker}:{Port}",
                configuration["SmartHome:Mqtt:Broker"],
                configuration["SmartHome:Mqtt:Port"]);
        }

        /// <summary>
        /// Turns a light on or off by publishing to the appropriate topic.
        /// </summary>
        public async Task SetLightStateAsync(string lightId, bool on)
        {
            var topic = $"home/lights/{lightId}/set";
            var payload = on ? "ON" : "OFF";
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithRetainFlag(false)
                .Build();

            await _client.PublishAsync(message);
            _logger.LogInformation("Published payload '{Payload}' to topic '{Topic}'", payload, topic);
        }

        /// <summary>
        /// Retrieves the last known state of a light by subscribing to its state topic.
        /// This method waits for the next message on that topic.
        /// </summary>
        public async Task<bool> GetLightStateAsync(string lightId)
        {
            var stateTopic = $"home/lights/{lightId}/state";
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task Handler(MqttApplicationMessageReceivedEventArgs args)
            {
                if (args.ApplicationMessage.Topic == stateTopic)
                {
                    var sequence = args.ApplicationMessage.Payload;
                    byte[] payloadBytes;
                    if (sequence.IsEmpty)
                    {
                        payloadBytes = Array.Empty<byte>();
                    }
                    else
                    {
                        payloadBytes = new byte[sequence.Length];
                        sequence.CopyTo(payloadBytes);
                    }

                    var msg = Encoding.UTF8.GetString(payloadBytes);
                    tcs.TrySetResult(msg);
                }
                return Task.CompletedTask;
            }

            _client.ApplicationMessageReceivedAsync += Handler;
            await _client.SubscribeAsync(stateTopic);
            _logger.LogInformation("Subscribed to state topic '{Topic}'", stateTopic);

            // Wait for the state message or timeout
            var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            _client.ApplicationMessageReceivedAsync -= Handler;

            if (result == tcs.Task)
            {
                return string.Equals(tcs.Task.Result, "ON", StringComparison.OrdinalIgnoreCase);
            }

            _logger.LogWarning("Timeout waiting for state on topic '{Topic}'", stateTopic);
            return false;
        }

        /// <summary>
        /// Disconnects and disposes the MQTT client.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_client.IsConnected)
                {
                    await _client.DisconnectAsync();
                    _logger.LogInformation("Disconnected MQTT client.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disconnecting MQTT client.");
            }
            _client.Dispose();
        }
    }
}
