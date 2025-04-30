using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using JARVIS.Modules.Devices.Interfaces;

namespace JARVIS.Modules.Devices
{
    public class MqttThermostatService : IThermostatService
    {
        private readonly IMqttClient _mqttClient;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        public MqttThermostatService(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        public async Task SetThermostatAsync(string zone, double temperature, CancellationToken cancellationToken = default)
        {
            var topic = $"home/thermostat/{zone}/set";
            var payload = Encoding.UTF8.GetBytes(temperature.ToString());
            await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build(), cancellationToken);
        }

        public Task RaiseThermostatAsync(string zone, CancellationToken cancellationToken = default)
            => AdjustThermostatAsync(zone, "+1", cancellationToken);

        public Task LowerThermostatAsync(string zone, CancellationToken cancellationToken = default)
            => AdjustThermostatAsync(zone, "-1", cancellationToken);

        private async Task AdjustThermostatAsync(string zone, string delta, CancellationToken cancellationToken)
        {
            var topic = $"home/thermostat/{zone}/adjust";
            var payload = Encoding.UTF8.GetBytes(delta);
            await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build(), cancellationToken);
        }

        public async Task<double?> GetThermostatTempAsync(string zone, CancellationToken cancellationToken = default)
        {
            var requestTopic = $"home/thermostat/{zone}/get";
            var responseTopic = $"home/thermostat/{zone}/state";

            var tcs = new TaskCompletionSource<double?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Subscribe to the response topic
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(responseTopic)
                .Build(), cancellationToken);

            // Handler for incoming state messages
            Func<MqttApplicationMessageReceivedEventArgs, Task> handler = null;
            handler = e =>
            {
                if (e.ApplicationMessage.Topic == responseTopic)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    if (double.TryParse(payload, out var temp))
                    {
                        tcs.TrySetResult(temp);
                        _mqttClient.ApplicationMessageReceivedAsync -= handler; // unsubscribe upon receiving
                    }
                }
                return Task.CompletedTask;
            };

            _mqttClient.ApplicationMessageReceivedAsync += handler; // use async event citeturn2search1

            // Publish get request
            await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(requestTopic)
                .WithPayload(Array.Empty<byte>())
                .Build(), cancellationToken);

            // Wait for response or timeout
            var delayTask = Task.Delay(_timeout, cancellationToken);
            var completed = await Task.WhenAny(tcs.Task, delayTask);
            _mqttClient.ApplicationMessageReceivedAsync -= handler;

            return completed == tcs.Task ? await tcs.Task : (double?)null;
        }

        public Task SetTemperatureAsync(string zoneId, double temp)
        {
            throw new NotImplementedException();
        }

        public Task<double?> GetCurrentTemperatureAsync(string zoneId)
        {
            throw new NotImplementedException();
        }
    }
}