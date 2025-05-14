#include <Arduino.h>
#include "CactusClient.h"
#include "CactusSetupServer.h"
#include <ArduinoJson.h>
#include "CBase64.h"

#define AP_SSID "RELAY_DEVICE"
#define AP_PASS "TESTP@SSWORD1234"

static const char *RELAY1_GET_TOPIC = "relay/1/get";
static const char *RELAY1_SET_TOPIC = "relay/1/set";
static const char *RELAY1_INF_TOPIC = "relay/1/inf";

#define RELAY1_PIN D1

CactusSetupServer setupServer;
CactusClient cactus;

void publishRelayState()
{
	StaticJsonDocument<64> doc;
	int state = digitalRead(RELAY1_PIN);
	doc["state"] = 1 - state;
	cactus.publish(RELAY1_INF_TOPIC, doc, true);
	Serial.print("[RELAY] Published state: ");
	Serial.println(state);
}

void onSet(const JsonDocument &payload)
{
	if (payload.containsKey("state"))
	{
		int req = payload["state"];
		Serial.print("[RELAY] Set request: ");
		Serial.println(req);
		digitalWrite(RELAY1_PIN, req ? LOW : HIGH);
		publishRelayState();
	}
	else
	{
		Serial.println("[MQTT][ERROR] Missing ‘state’ field");
	}
}

void onGet(const JsonDocument &payload)
{
	Serial.println("[RELAY] Get request received");
	publishRelayState();
}

void onConfigured(const CactusSetupServer::Response &r)
{
	Serial.println("[SETUP] Configuration received from portal");
	Serial.print("[SETUP] SSID: ");
	Serial.println(r.ssid);
	Serial.print("[SETUP] Password: ");
	Serial.println(r.password);
	Serial.print("[SETUP] MQTT (Base64): ");
	Serial.println(r.mqttBase64);

	Serial.println("[SETUP] Decoding MQTT settings...");
	size_t len;
	uint8_t *buf = CBase64::decode(r.mqttBase64.c_str(), r.mqttBase64.length(), len);
	if (!buf)
	{
		Serial.println("[SETUP][ERROR] Base64 decode failed");
		return;
	}
	buf[len] = '\0';

	DynamicJsonDocument doc(len + 1);
	auto err = deserializeJson(doc, (char *)buf);
	free(buf);
	if (err)
	{
		Serial.print("[SETUP][ERROR] JSON parse error: ");
		Serial.println(err.c_str());
		return;
	}

	Serial.println("[SETUP] Applying WiFi credentials and MQTT settings to CactusClient...");
	cactus.setWiFiCredentials(r.ssid.c_str(), r.password.c_str());
	cactus.setMQTTSettings(
		doc["host"].as<const char *>(),
		doc["port"].as<int>(),
		doc["username"].as<const char *>(),
		doc["password"].as<const char *>());

	cactus.addSubscriptionExample(RELAY1_SET_TOPIC, "{\"state\":{\"type\":\"number\"}}");
	cactus.addSubscriptionExample(RELAY1_GET_TOPIC, "");

	cactus.subscribe(RELAY1_SET_TOPIC, onSet);
	cactus.subscribe(RELAY1_GET_TOPIC, onGet);

	cactus.addPublicationExample(RELAY1_INF_TOPIC, "{\"state\":{\"type\":\"number\"}}");

	Serial.println("[SETUP] Starting CactusClient...");
	cactus.begin();
}

void setup()
{
	Serial.begin(115200);
	Serial.println("\n\n[SYSTEM] Starting Cactus Relay Device");

	pinMode(RELAY1_PIN, OUTPUT);

	setupServer.onConfigured(onConfigured);
	Serial.println("[SYSTEM] Attempting to connect with saved configuration...");
	setupServer.beginOrStartPortal(AP_SSID, AP_PASS);
}

void loop()
{
	setupServer.loop();

	if (!setupServer.isConfigured())
	{
		return;
	}

	cactus.loop();
}
