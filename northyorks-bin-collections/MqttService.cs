using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using northyorks_bin_collections.interfaces;
using System.Text;

namespace northyorks_bin_collections;


public class MqttService : IMqttService
{
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly string _username;
    private readonly string _password;
    private readonly string _clientId;
    private readonly ILogger<MqttService> _logger;
    private IMqttClient? _mqttClient;
    private MqttFactory? _mqttFactory;
    private bool _isConnected;
    private readonly Dictionary<string, Func<string, Task>> _subscriptions = new();

    public MqttService(ILogger<MqttService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _brokerHost = configuration["Mqtt:BrokerHost"] ?? throw new ArgumentNullException("Mqtt:BrokerHost", "MQTT Broker host must be configured in appsettings.json");
        _brokerPort = configuration.GetValue<int?>("Mqtt:BrokerPort") ?? 1883;
        _username = configuration["Mqtt:Username"] ?? throw new ArgumentNullException("Mqtt:Username", "MQTT username must be configured in appsettings.json");
        _password = configuration["Mqtt:Password"] ?? throw new ArgumentNullException("Mqtt:Password", "MQTT password must be configured in appsettings.json");
        _clientId = configuration["Mqtt:ClientId"] ?? "northyorks-bin-collections";
        _isConnected = false;
    }

    public async Task ConnectAsync()
    {
        if (_isConnected && _mqttClient != null && _mqttClient.IsConnected)
        {
            return;
        }

        try
        {
            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerHost, _brokerPort)
                .WithCredentials(_username, _password)
                .WithClientId(_clientId)
                .WithCleanSession()
                .Build();

            var result = await _mqttClient.ConnectAsync(options, CancellationToken.None);
            
            if (result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _isConnected = true;
                Console.WriteLine($"Successfully connected to MQTT broker at {_brokerHost}:{_brokerPort}");
            }
            else
            {
                _logger.LogError("Failed to connect to MQTT broker: {ResultCode}", result.ResultCode);
                throw new Exception($"Failed to connect to MQTT broker: {result.ResultCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MQTT broker");
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (!_isConnected || _mqttClient == null)
        {
            _logger.LogWarning("MQTT client not connected. Attempting to connect before publishing to {Topic}", topic);
            await ConnectAsync();
        }

        try
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                Console.WriteLine($"Publishing to MQTT topic: {topic}");
                
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithRetainFlag(true) 
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce) // Ensure delivery
                    .Build();

                var result = await _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            }
            else
            {
                _logger.LogError("MQTT client not connected. Cannot publish to {Topic}", topic);
                throw new Exception("MQTT client not connected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to topic {Topic}", topic);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (!_isConnected || _mqttClient == null)
        {
            return;
        }

        try
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), CancellationToken.None);
            }
            
            _isConnected = false;
            Console.WriteLine("Disconnected from MQTT broker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from MQTT broker");
        }
    }
}
