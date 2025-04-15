using System.Buffers;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace SCMQTTBroker;

internal class Program
{
    static async Task Main(string[] args)
    {
        var certificatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)??"", "certificate.pfx");
        var certificatePassword = "hello";

        if (!File.Exists(certificatePath))
        {
            Console.WriteLine($"Sertificate not found: {certificatePath}");
            return;
        }

        var certificate = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable);

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(8883)
            .WithEncryptionCertificate(certificate.Export(X509ContentType.Pfx))
            .WithEncryptionSslProtocol(SslProtocols.Tls12)
            .Build();

        var mqttFactory = new MqttServerFactory();  
        var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);
        mqttServer.ValidatingConnectionAsync += e =>
        {
            if (string.IsNullOrEmpty(e.ClientId))
            {
                e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }
            else
            {
                e.ReasonCode = MqttConnectReasonCode.Success;
            }
            return Task.CompletedTask;
        };
        mqttServer.ClientConnectedAsync += e =>
        {
            Console.WriteLine($"### {e.UserName} connected with id: {e.ClientId}, endpoint: {e.Endpoint}");
            return Task.CompletedTask;
        };
        mqttServer.InterceptingPublishAsync += e =>
        {
            var a = e.ApplicationMessage.Payload.ToArray();
            var b = string.Concat(a.Select(x => (char)x));

            Console.WriteLine($"\n[{DateTime.Now}] Payload: \n---\n{b}\n---\nfrom: {e.ClientId}, Topic: {e.ApplicationMessage.Topic}");
            return Task.CompletedTask;
        };
        Console.WriteLine("Starting...");
        await mqttServer.StartAsync();
        Console.WriteLine("Broker started. Press any key to exit...");

        await Task.Delay(2500);
        await mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(new MqttApplicationMessage()
        {
            Payload = new ReadOnlySequence<byte>("Test payload".ToCharArray().Select(x => (byte)x).ToArray()),
            Topic = "test/test",
            QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
        })
        {
            SenderClientId = "Telegram"
        });


        Console.ReadKey();

        await mqttServer.StopAsync();
        Console.WriteLine("Broker finished.");
    }
}