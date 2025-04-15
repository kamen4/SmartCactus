#include <Adafruit_BMP280.h>
#include <CactusClient.h>

#define LED D0

CactusClient cactus;
Adafruit_BMP280 bmp;

void handleLedState(ArduinoJson::JsonDocument &doc)
{
	const char *state = doc["led_state"];
	Serial.print("Handling led_state: ");
	Serial.println(state);
	if (strcmp(state, "1") == 0)
	{
		digitalWrite(LED, HIGH);
	}
	else
	{
		digitalWrite(LED, LOW);
	}
}

void setupCactus()
{
	cactus.setWiFiCredentials("varvr", "17081969");
	cactus.setMQTTSettings("192.168.100.9", 8883, "esp8266_client", "Esp8266p@ssword");

	cactus.subscribe("led_state", handleLedState);

	cactus.addSubscriptionExample("led_state", "{\"led_state\":\"1\"}");
	cactus.addPublicationExample("esp8266_data", "{\"pressure\":1013.25,\"temperature\":24.5}");

	cactus.begin();
}

void setupBMP280()
{
	if (!bmp.begin(BMP280_ADDRESS_ALT, BMP280_CHIPID))
	{
		Serial.println(F("Could not find a valid BMP280 sensor, check wiring or try a different address!"));
		while (1)
		{
			delay(10);
		}
	}
	bmp.setSampling(Adafruit_BMP280::MODE_FORCED,
					Adafruit_BMP280::SAMPLING_X2,
					Adafruit_BMP280::SAMPLING_X16,
					Adafruit_BMP280::FILTER_X16,
					Adafruit_BMP280::STANDBY_MS_500);
}

void publishBMPData()
{
	double pressure = 0.0;
	double temperature = 0.0;
	if (bmp.takeForcedMeasurement())
	{
		pressure = bmp.readPressure();
		temperature = bmp.readTemperature();
	}

	DynamicJsonDocument doc(1024);
	doc["pressure"] = pressure;
	doc["temperature"] = temperature;

	cactus.publish("esp8266_data", doc, true);
}

void setup()
{
	Serial.begin(9600);
	pinMode(LED, OUTPUT);
	setupBMP280();
	setupCactus();
}

void loop()
{
	cactus.loop();
	static unsigned long lastPublish = 0;
	if (millis() - lastPublish >= 5000)
	{
		lastPublish = millis();
		publishBMPData();
	}
}
