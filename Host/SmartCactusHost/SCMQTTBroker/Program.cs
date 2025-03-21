using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace SCMQTTBroker;

internal class Program
{
    static async Task Main(string[] args)
    {
        var certificatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "certificate.pfx");
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
        mqttServer.ApplicationMessageEnqueuedOrDroppedAsync += e =>
        {
            var a = e.ApplicationMessage.Payload.ToArray();
            var b= string.Concat(a.Select(x => (char)x));

            Console.WriteLine($"[{DateTime.Now}] Payload {b} for Client: {e.ReceiverClientId}, from: {e.SenderClientId}, Topic: {e.ApplicationMessage.Topic}");
            return Task.CompletedTask;
        };

        Console.WriteLine("Starting...");
        await mqttServer.StartAsync();


        Console.WriteLine("Broker started. Pres any key to exit...");

        Console.ReadKey();

        await mqttServer.StopAsync();
        Console.WriteLine("Broker finished.");
    }
}