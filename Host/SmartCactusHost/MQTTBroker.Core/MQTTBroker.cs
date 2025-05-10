using System.Buffers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;
using MQTTnet.Server;
using LoggerService;
using System.Text;
using System.Net;
using Entities.Models;

namespace MQTTBroker;

public class MQTTBroker
{
    private static MQTTBroker? _instance;
    private static ILogger? _logger;
    private readonly X509Certificate2? _certificate;
    private readonly MqttServer? _mqttServer;

    public int Port { get; } = 8883;
    public Func<string, string, string, Device?> AuthorizeDevice;

    private MQTTBroker(X509Certificate2 certificate)
    {
        _certificate = certificate;
        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(Port)
            .WithEncryptionCertificate(_certificate.Export(X509ContentType.Pfx))
            .WithEncryptionSslProtocol(SslProtocols.Tls12)
            .Build();
        var mqttFactory = new MqttServerFactory();
        _mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

        _mqttServer.ValidatingConnectionAsync += ValidateConnectionHandler;
        _mqttServer.ClientConnectedAsync += ClientConnectedHandler;
        _mqttServer.InterceptingPublishAsync += MessagePublishedHandler;

        _logger?.Info("MQTTBroker|Instance initialized.");
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
            _logger?.Error("MQTTBroker|Server is not initialized.");
            throw new InvalidOperationException("Server is not initialized.");
        }

        if (!_mqttServer.IsStarted)
        {
            await _mqttServer.StartAsync();
            _logger?.Info("MQTTBroker|Server started.");
        }
        else
        {
            _logger?.Warn("MQTTBroker|Trying to start server, that alredy started.");
        }
    }
    public async void StopServer()
    {
        if (_mqttServer is null || !_mqttServer.IsStarted)
        {
            _logger?.Error("MQTTBroker|Server is not initialized or started.");
            throw new NullReferenceException("Server is not initialized or started.");
        }
        await _mqttServer.StopAsync();
        _logger?.Info("MQTTBroker|Server stopped.");
    }

    public bool IsStarted() =>_mqttServer?.IsStarted ?? false;

    private Task ValidateConnectionHandler(ValidatingConnectionEventArgs e)
    {
        _logger?.Info($"MQTTBroker|User with id: {e.ClientId} is trying to connect.");
        Device? device = AuthorizeDevice(e.ClientId, e.UserName, e.Password);
        if (device is null)
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
        _logger?.Info($"MQTTBroker|User {e.UserName} connected with id: {e.ClientId} and endpoint: {e.RemoteEndPoint}.");
#warning TODO ping
        return Task.CompletedTask;
    }

    private Task MessagePublishedHandler(InterceptingPublishEventArgs e)
    {
        var payload = PayloadToString(e.ApplicationMessage.Payload);
        var topic = e.ApplicationMessage.Topic;

        _logger?.Info($"MQTTBroker|Publish to topic '{topic}', from: {e.ClientId}. Payload: {payload}");

        return Task.CompletedTask;
    }

    private static string PayloadToString(ReadOnlySequence<byte> payload)
    {
        return EncodingExtensions.GetString(Encoding.UTF8, payload);
    }
}
