#include <Arduino.h>
#include <EEPROM.h>
#include <Adafruit_BMP280.h>
#include <ArduinoJson.h>
#include "CactusClient.h"
#include "CactusSetupServer.h"
#include "CBase64.h"

#define LED_PIN D0

#define AP_SSID "BMP_STATION"
#define AP_PASS "4PHBBfqBZf9iRN9l"

CactusClient cactus;
Adafruit_BMP280 bmp;
CactusSetupServer setupServer(53);
bool configured = false;
unsigned long lastPublish = 0;
const unsigned long PUBLISH_INTERVAL = 5000;

struct EConfig
{
	char ssid[32];
	char pass[32];
	char mqtt[128];
};

void startBMP()
{
	Serial.println("Initializing BMP280 sensor...");
	while (!bmp.begin(BMP280_ADDRESS_ALT, BMP280_CHIPID))
	{
		Serial.println("BMP280 not found. Try Again in 1s.");
		delay(1000);
	}
	Serial.println("BMP280 initialized successfully");
	bmp.setSampling(Adafruit_BMP280::MODE_FORCED, Adafruit_BMP280::SAMPLING_X2,
					Adafruit_BMP280::SAMPLING_X16, Adafruit_BMP280::FILTER_X16,
					Adafruit_BMP280::STANDBY_MS_500);
}

void publishBMP()
{
	Serial.println("Taking BMP280 measurement...");
	if (bmp.takeForcedMeasurement())
	{
		StaticJsonDocument<256> doc;
		float pressure = bmp.readPressure();
		float temperature = bmp.readTemperature();
		doc["pressure"] = pressure;
		doc["temperature"] = temperature;

		Serial.print("Publishing data - Pressure: ");
		Serial.print(pressure);
		Serial.print(" Pa, Temperature: ");
		Serial.print(temperature);
		Serial.println(" °C");

		cactus.publish("esp8266_data", doc, true);
	}
	else
	{
		Serial.println("Failed to take BMP280 measurement");
	}
}

void saveConfig(const CactusSetupServer::Response &r)
{
	Serial.println("Saving configuration to EEPROM...");
	EEPROM.begin(512);
	EConfig cfg;
	strncpy(cfg.ssid, r.ssid.c_str(), sizeof(cfg.ssid));
	strncpy(cfg.pass, r.password.c_str(), sizeof(cfg.pass));
	strncpy(cfg.mqtt, r.mqttSettingsBase64.c_str(), sizeof(cfg.mqtt));
	EEPROM.write(0, 1);
	EEPROM.put(1, cfg);
	EEPROM.commit();
	Serial.println("Configuration saved successfully");
}

bool loadConfig(CactusSetupServer::Response &r)
{
	Serial.println("Loading configuration from EEPROM...");
	EEPROM.begin(512);
	if (EEPROM.read(0) != 1)
	{
		Serial.println("No valid configuration found in EEPROM");
		return false;
	}
	EConfig cfg;
	EEPROM.get(1, cfg);
	r.ssid = String(cfg.ssid);
	r.password = String(cfg.pass);
	r.mqttSettingsBase64 = String(cfg.mqtt);
	Serial.println("Configuration loaded successfully");
	return true;
}

bool setupCactusClient(const CactusSetupServer::Response &r)
{
	Serial.println("Setting up Cactus client...");

	Serial.println("Decoding MQTT settings...");
	size_t decLen;
	uint8_t *decB = CBase64::decode(r.mqttSettingsBase64.c_str(),
									r.mqttSettingsBase64.length(), decLen);
	if (!decB)
	{
		Serial.println("Failed to decode Base64 MQTT settings");
		return false;
	}

	char *json = (char *)malloc(decLen + 1);
	memcpy(json, decB, decLen);
	json[decLen] = '\0';

	Serial.println("Parsing MQTT settings JSON...");
	StaticJsonDocument<256> m;
	if (deserializeJson(m, json) != DeserializationError::Ok)
	{
		Serial.println("Failed to parse MQTT settings JSON");
		free(decB);
		free(json);
		return false;
	}

	Serial.println("Configuring WiFi credentials...");
	cactus.setWiFiCredentials(r.ssid.c_str(), r.password.c_str());

	Serial.println("Configuring MQTT settings...");
	cactus.setMQTTSettings(m["host"], m["port"],
						   m["clientId"].as<const char *>(),
						   m["password"].as<const char *>());

	Serial.println("Setting up subscriptions...");
	cactus.subscribe("led_state", [&](JsonDocument &d)
					 { 
                         digitalWrite(LED_PIN, strcmp(d["led_state"], "1") == 0); 
                         Serial.println("LED state updated"); });
	cactus.addSubscriptionExample("led_state", "{\"led_state\":\"1\"}");
	cactus.addPublicationExample("esp8266_data", "{\"pressure\":1013.25,\"temperature\":24.5}");

	Serial.println("Starting Cactus client...");
	cactus.begin();

	startBMP();

	free(decB);
	free(json);

	Serial.println("Cactus client setup completed successfully");
	return true;
}

bool connectSavedConfig()
{
	Serial.println("Attempting to connect using saved configuration...");
	CactusSetupServer::Response r;
	if (!loadConfig(r))
	{
		Serial.println("No saved configuration available");
		return false;
	}

	if (!setupCactusClient(r))
	{
		Serial.println("Failed to setup client with saved configuration");
		return false;
	}

	configured = true;
	lastPublish = millis();
	Serial.println("Successfully connected using saved configuration");
	return true;
}

void processConfiguration()
{
	setupServer.handleClient();
	if (!setupServer.isFinished())
		return;

	Serial.println("Configuration received from portal");
	auto r = setupServer.getResponse();
	if (setupCactusClient(r))
	{
		Serial.println("New configuration applied successfully");
		saveConfig(r);
		setupServer.stop();
		configured = true;
		lastPublish = millis();
		Serial.println("Configuration portal stopped");
	}
	else
	{
		Serial.println("Failed to apply new configuration");
	}
}

void setup()
{
	Serial.begin(9600);
	Serial.println("\n\nStarting Cactus BMP Station...");

	pinMode(LED_PIN, OUTPUT);
	digitalWrite(LED_PIN, LOW);

	if (!connectSavedConfig())
	{
		Serial.println("Initializing configuration portal...");
		setupServer.begin(AP_SSID, AP_PASS);
		Serial.println("Configuration portal started");
	}
}

void loop()
{
	if (!configured)
	{
		processConfiguration();
		return;
	}

	cactus.loop();

	if (millis() - lastPublish >= PUBLISH_INTERVAL)
	{
		lastPublish = millis();
		publishBMP();
	}
}