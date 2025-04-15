#ifndef CACTUSCLIENT_H
#define CACTUSCLIENT_H

#include <ESP8266WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <functional>

class CactusClient
{
public:
    // Type for JSON message callback handlers.
    typedef std::function<void(ArduinoJson::JsonDocument &)> MsgCallback;

    // Structure to store topic examples.
    struct TopicExample
    {
        String topic;
        String jsonExample;
    };

    static const int MAX_SUBSCRIPTIONS = 10;
    static const int MAX_TOPIC_EXAMPLES = 10;

    CactusClient();
    ~CactusClient();

    // Set WiFi credentials.
    void setWiFiCredentials(const char *ssid, const char *password);

    // Set MQTT broker settings.
    void setMQTTSettings(const char *server, int port, const char *username, const char *password);

    // Initialize WiFi and MQTT connections.
    void begin();

    // Must be called in the main loop.
    void loop();

    // Add a subscription topic with its JSON callback handler.
    void subscribe(const char *topic, MsgCallback callback);

    // Register a subscription topic with an example JSON message.
    void addSubscriptionExample(const char *topic, const char *jsonExample);

    // Register a publication topic with an example JSON message.
    void addPublicationExample(const char *topic, const char *jsonExample);

    // Publish a JSON document on an MQTT topic.
    // If retained is true, the broker will keep the message.
    bool publish(const char *topic, ArduinoJson::JsonDocument &doc, boolean retained = false);

private:
    // Initialize the WiFi connection.
    void _setupWiFi();

    // Ensure MQTT is connected.
    void _reconnect();

    // Static MQTT callback for PubSubClient; dispatches to the instance callback.
    static void _mqttCallback(char *topic, byte *payload, unsigned int length);

    // WiFi & MQTT settings.
    const char *_ssid;
    const char *_wifiPassword;
    const char *_mqtt_server;
    int _mqtt_port;
    const char *_mqtt_username;
    const char *_mqtt_password;

    // Underlying secure WiFi client and PubSubClient for MQTT.
    WiFiClientSecure _espClient;
    PubSubClient _client;

    // Structure for keeping subscription callbacks.
    struct Subscription
    {
        String topic;
        MsgCallback callback;
    };

    Subscription _subscriptions[MAX_SUBSCRIPTIONS];
    int _subscriptionCount;

    // Registered topic examples for subscriptions.
    TopicExample _subExamples[MAX_TOPIC_EXAMPLES];
    int _subExampleCount;

    // Registered topic examples for publications.
    TopicExample _pubExamples[MAX_TOPIC_EXAMPLES];
    int _pubExampleCount;

    // Built-in "ping" topic name (all devices are subscribed to "ping").
    static const char *PING_TOPIC;
    // Topic to publish the response to a "ping".
    static const char *PING_RESPONSE_TOPIC;

    // Static pointer to the singleton instance for use in the static callback.
    static CactusClient *_instance;
};

#endif // CACTUSCLIENT_H
