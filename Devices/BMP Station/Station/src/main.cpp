#include <Arduino.h>
#include <Adafruit_BMP280.h>
#include "CactusClient.h"
#include "CactusSetupServer.h"
#include <ArduinoJson.h>
#include "CBase64.h"

#define AP_SSID "BMP_STATION"
#define AP_PASS "4PHBBfqBZf9iRN9l"
#define BMP_TOPIC "bmp280/data"
#define PUBLISH_INTERVAL 30'000UL

Adafruit_BMP280 bmp;
CactusSetupServer setupServer;
CactusClient cactus;

bool bmpReady = false;
unsigned long lastPublish = 0;

void initBMP()
{
	Serial.println("[BMP] Initializing BMP280 sensor...");
	while (!bmp.begin(BMP280_ADDRESS_ALT, BMP280_CHIPID))
	{
		Serial.println("[BMP][ERROR] BMP280 not found, retrying in 1s");
		delay(1000);
	}
	bmp.setSampling(
		Adafruit_BMP280::MODE_FORCED,
		Adafruit_BMP280::SAMPLING_X2,
		Adafruit_BMP280::SAMPLING_X16,
		Adafruit_BMP280::FILTER_X16,
		Adafruit_BMP280::STANDBY_MS_500);
	bmpReady = true;
	Serial.println("[BMP] BMP280 initialized successfully");
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
	cactus.addPublicationExample(
		BMP_TOPIC,
		"{\"pressure\":{\"type\":\"number\"},\"temperature\":{\"type\":\"number\"}}");

	Serial.println("[SETUP] Starting CactusClient...");
	cactus.begin();

	initBMP();
	Serial.println("[SETUP] Ready to publish sensor data");
}

void setup()
{
	Serial.begin(115200);
	Serial.println("\n\n[SYSTEM] Starting Cactus BMP Station");

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

	if (bmpReady && millis() - lastPublish >= PUBLISH_INTERVAL)
	{
		lastPublish = millis();
		Serial.println("[PUBLISH] Taking BMP280 measurement...");
		if (bmp.takeForcedMeasurement())
		{
			float pressure = bmp.readPressure();
			float temperature = bmp.readTemperature();

			Serial.print("[PUBLISH] Pressure: ");
			Serial.print(pressure);
			Serial.print(" Pa, Temperature: ");
			Serial.print(temperature);
			Serial.println(" °C");

			StaticJsonDocument<128> doc;
			doc["pressure"] = pressure;
			doc["temperature"] = temperature;

			Serial.println("[PUBLISH] Publishing to MQTT...");
			cactus.publish(BMP_TOPIC, doc, true);
			Serial.println("[PUBLISH] Data published successfully");
		}
		else
		{
			Serial.println("[PUBLISH][ERROR] Failed to read BMP280 data");
		}
	}
}
