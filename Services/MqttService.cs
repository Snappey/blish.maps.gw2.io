using Blish_HUD;
using GW2IO.Maps.Static;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace GW2IO.Maps.Services
{
    internal class MqttService : IDisposable
    {
        private Logger _logger;
        private Settings _settings;

        private IMqttClient _mqtt;
        private MqttClientOptions _mqttClientOptions;

        public MqttService(Logger logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;

            _mqtt = new MqttFactory().CreateMqttClient();
            _mqtt.ConnectedAsync += clientConnected;
            _mqtt.ConnectingAsync += clientConnecting;
            _mqtt.DisconnectedAsync += clientDisconnected;

        }

        public bool IsConnected() => _mqtt.IsConnected;

        public Task Disconnect() => _mqtt.DisconnectAsync();
        public Task Reconnect() => _mqtt.ReconnectAsync();
        public Task PublishString(string topic, string data) => _mqtt.PublishStringAsync(topic, data);

        public async Task<MqttClientConnectResult> Connect(string accountName, string password)
        {
            if (_mqtt.IsConnected)
            {
                _logger.Debug("attempting to connect to broker that's already connected, disconnecting before reconnecting");
                await _mqtt.DisconnectAsync();
            }

            var clientId = accountName;
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer(_settings.MqttServer.Value)
                .WithClientId($"{clientId}-blish")
                .WithCredentials(clientId, password)
                .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                })
                .Build();

            return await _mqtt.ConnectAsync(_mqttClientOptions);
        }

        public void UpdateWill(string topic, string data)
        {
            _mqtt.Options.WillTopic = topic;
            _mqtt.Options.WillPayload = Encoding.UTF8.GetBytes(data);
        }

        private Task clientDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            _logger.Info("disconnected from broker: {args}", arg.Exception);

            return Task.CompletedTask;
        }

        private Task clientConnecting(MqttClientConnectingEventArgs arg)
        {
            _logger.Info("connecting to broker: {args}", arg.ClientOptions.ClientId);

            return Task.CompletedTask;
        }

        private Task clientConnected(MqttClientConnectedEventArgs arg)
        {
            _logger.Info("connected to broker: {args}", arg.ConnectResult);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _mqtt.Dispose();
        }
    }
}
