#include "CactusClient.h"

const char *CactusClient::DEVICE_ID = "RELAY-00000001";

const char *CactusClient::PING_TOPIC = "ping/all";
const char *CactusClient::PING_TOPIC_OWN = "ping/RELAY-00000001";
const char *CactusClient::PING_RESPONSE_TOPIC = "ping-response";

CactusClient *CactusClient::_instance = nullptr;

CactusClient::CactusClient()
    : _ssid(nullptr),
      _wifiPassword(nullptr),
      _mqtt_server(nullptr),
      _mqtt_port(1883),
      _mqtt_username(nullptr),
      _mqtt_password(nullptr),
      _client(_espClient),
      _subscriptionCount(0),
      _subExampleCount(0),
      _pubExampleCount(0)
{
    _espClient.setInsecure();
    _client.setCallback(_mqttCallback);
    subscribe(PING_TOPIC, nullptr);
    subscribe(PING_TOPIC_OWN, nullptr);
    _instance = this;
}

CactusClient::~CactusClient()
{
    _instance = nullptr;
}

void CactusClient::setWiFiCredentials(const char *ssid, const char *password)
{
    _ssid = ssid;
    _wifiPassword = password;
}

void CactusClient::setMQTTSettings(const char *server, int port, const char *username, const char *password)
{
    strncpy(_mqtt_server_buffer, server, sizeof(_mqtt_server_buffer) - 1);
    _mqtt_server_buffer[sizeof(_mqtt_server_buffer) - 1] = '\0';
    strncpy(_mqtt_username_buffer, username, sizeof(_mqtt_username_buffer) - 1);
    _mqtt_username_buffer[sizeof(_mqtt_username_buffer) - 1] = '\0';
    strncpy(_mqtt_password_buffer, password, sizeof(_mqtt_password_buffer) - 1);
    _mqtt_password_buffer[sizeof(_mqtt_password_buffer) - 1] = '\0';

    _mqtt_server = _mqtt_server_buffer;
    _mqtt_username = _mqtt_username_buffer;
    _mqtt_password = _mqtt_password_buffer;
    _mqtt_port = port;
    _client.setServer(_mqtt_server, _mqtt_port);
}

void CactusClient::_setupWiFi()
{
    delay(10);
    Serial.print("\nConnecting to ");
    Serial.println(_ssid);
    WiFi.mode(WIFI_STA);
    WiFi.begin(_ssid, _wifiPassword);

    // Wait for connection.
    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        Serial.print(".");
    }
    randomSeed(micros());
    Serial.println("\nWiFi connected\nIP address: ");
    Serial.println(WiFi.localIP());
}

void CactusClient::_reconnect()
{
    while (!_client.connected())
    {
        String clientId = DEVICE_ID;

        Serial.print("Attempting MQTT connection: ");
        Serial.print(clientId);
        Serial.print(" ");
        Serial.print(_mqtt_username);
        Serial.print(" ");
        Serial.print(_mqtt_password);

        bool connected = false;
        if (_mqtt_username && _mqtt_password)
        {
            connected = _client.connect(clientId.c_str(), _mqtt_username, _mqtt_password);
        }
        else
        {
            connected = _client.connect(clientId.c_str());
        }

        if (connected)
        {
            Serial.println(" connected");
            for (int i = 0; i < _subscriptionCount; i++)
            {
                _client.subscribe(_subscriptions[i].topic.c_str());
            }
        }
        else
        {
            Serial.print(" failed, rc=");
            Serial.print(_client.state());
            Serial.println(" try again in 5 seconds");
            delay(5000);
        }
    }
}

void CactusClient::begin()
{
    _setupWiFi();
    _client.setCallback(_mqttCallback);
    _reconnect();
}

void CactusClient::loop()
{
    if (!_client.connected())
    {
        _reconnect();
    }
    _client.loop();
}

void CactusClient::subscribe(const char *topic, MsgCallback callback)
{
    if (_subscriptionCount < MAX_SUBSCRIPTIONS)
    {
        _subscriptions[_subscriptionCount].topic = topic;
        _subscriptions[_subscriptionCount].callback = callback;
        _subscriptionCount++;

        if (_client.connected())
        {
            _client.subscribe(topic);
        }
    }
    else
    {
        Serial.println("Max subscription count reached");
    }
}

void CactusClient::addSubscriptionExample(const char *topic, const char *jsonExample)
{
    if (_subExampleCount < MAX_TOPIC_EXAMPLES)
    {
        _subExamples[_subExampleCount].topic = topic;
        _subExamples[_subExampleCount].jsonSchema = jsonExample;
        _subExampleCount++;
    }
    else
    {
        Serial.println("Max subscription example count reached");
    }
}

void CactusClient::addPublicationExample(const char *topic, const char *jsonExample)
{
    if (_pubExampleCount < MAX_TOPIC_EXAMPLES)
    {
        _pubExamples[_pubExampleCount].topic = topic;
        _pubExamples[_pubExampleCount].jsonSchema = jsonExample;
        _pubExampleCount++;
    }
    else
    {
        Serial.println("Max publication example count reached");
    }
}

bool CactusClient::publish(const char *topic, ArduinoJson::JsonDocument &doc, boolean retained)
{
    // Serialize the JSON document into a buffer.
    char buffer[256];
    size_t n = serializeJson(doc, buffer, sizeof(buffer));
    bool result = _client.publish(topic, buffer, retained);
    if (result)
    {
        Serial.print("Published to ");
        Serial.print(topic);
        Serial.print(": ");
        Serial.println(buffer);
    }
    return result;
}

void CactusClient::_mqttCallback(char *topic, byte *payload, unsigned int length)
{
    String incoming;
    for (unsigned int i = 0; i < length; i++)
    {
        incoming += (char)payload[i];
    }
    Serial.print("Message arrived [");
    Serial.print(topic);
    Serial.print("]: ");
    Serial.println(incoming);

    if (_instance)
    {
        // Handle "ping" messages specially.
        if (String(topic) == PING_TOPIC || String(topic) == PING_TOPIC_OWN)
        {
            DynamicJsonDocument pingDoc(2048);
            JsonArray subs = pingDoc.createNestedArray("subscriptions");
            for (int i = 0; i < _instance->_subExampleCount; i++)
            {
                JsonObject obj = subs.createNestedObject();
                obj["topic"] = _instance->_subExamples[i].topic;
                obj["jsonSchema"] = _instance->_subExamples[i].jsonSchema;
            }

            JsonArray pubs = pingDoc.createNestedArray("publications");
            for (int i = 0; i < _instance->_pubExampleCount; i++)
            {
                JsonObject obj = pubs.createNestedObject();
                obj["topic"] = _instance->_pubExamples[i].topic;
                obj["jsonSchema"] = _instance->_pubExamples[i].jsonSchema;
            }
            // Publish the response on the defined response topic.
            _instance->publish(PING_RESPONSE_TOPIC, pingDoc, false);
            // Do not process further if "ping" is solely for device info.
            return;
        }

        // For other topics, check for a matching subscription callback.
        for (int i = 0; i < _instance->_subscriptionCount; i++)
        {
            if (_instance->_subscriptions[i].topic.equals(topic))
            {
                // If a callback was registered, process the JSON payload.
                if (_instance->_subscriptions[i].callback != nullptr)
                {
                    DynamicJsonDocument doc(1024);
                    DeserializationError error = deserializeJson(doc, incoming);
                    if (!error)
                    {
                        _instance->_subscriptions[i].callback(doc);
                    }
                    else
                    {
                        Serial.print("JSON parse error: ");
                        Serial.println(error.c_str());
                    }
                }
            }
        }
    }
}
