using System.Buffers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;
using MQTTnet.Server;
using LoggerService;

namespace MQTTBroker.Core;

public class MQTTBroker
{
    private static MQTTBroker? _instance;
    private static ILogger? _logger;
    private readonly X509Certificate2? _certificate;
    private readonly MqttServer? _mqttServer;

    private MQTTBroker(X509Certificate2 certificate)
    {
        _certificate = certificate;
        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(8883)
            .WithEncryptionCertificate(_certificate.Export(X509ContentType.Pfx))
            .WithEncryptionSslProtocol(SslProtocols.Tls12)
            .Build();
        var mqttFactory = new MqttServerFactory();
        _mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

        _mqttServer.ValidatingConnectionAsync += ValidateConnectionHandler;
        _mqttServer.ClientConnectedAsync += ClientConnectedHandler;
        _mqttServer.InterceptingPublishAsync += MessagePublishedHandler;

        _logger?.Info("MQTTBroker instance initialized.");
    }

    public static MQTTBroker InitializeInstance(X509Certificate2 certificate, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        _logger = logger;
        _instance ??= new(certificate);

        return _instance;
    }

    public async void StartServer()
    {
        if (_mqttServer is null)
        {
            throw new NullReferenceException("Server is not initialized.");
        }
        if (_mqttServer.IsStarted)
        {
            await _mqttServer.StartAsync();
            _logger?.Info("MQTT server started.");
        }
        else
        {
            _logger?.Warn("Trying to start MQTT server, that alredy started.");
        }
    }
    public async Task StopServer()
    {
        if (_mqttServer is null || !_mqttServer.IsStarted)
        {
            throw new NullReferenceException("Server is not initialized or started.");
        }
        await _mqttServer.StopAsync();
        _logger?.Info("MQTT server stopped.");
    }

    private Task ValidateConnectionHandler(ValidatingConnectionEventArgs e)
    {
        _logger?.Info($"User with id: {e.ClientId} is trying to connect.");
        if (string.IsNullOrEmpty(e.ClientId))
        {
            e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
        }
        else
        {
            e.ReasonCode = MqttConnectReasonCode.Success;
        }
        return Task.CompletedTask;
    }
    private Task ClientConnectedHandler(ClientConnectedEventArgs e)
    {
        _logger?.Info($"User {e.UserName} connected with id: {e.ClientId} and endpoint: {e.RemoteEndPoint}.");
        return Task.CompletedTask;
    }
    private Task MessagePublishedHandler(InterceptingPublishEventArgs e)
    {
        var payload = PayloadToString(e.ApplicationMessage.Payload);
        var topic = e.ApplicationMessage.Topic;

        _logger?.Info($"Publish to topic '{topic}', from: {e.ClientId}. Payload: {payload}");

        return Task.CompletedTask;
    }

    private static string PayloadToString(ReadOnlySequence<byte> payload)
    {
        return string.Concat(payload.ToArray().Select(x => (char)x));
    }
}
