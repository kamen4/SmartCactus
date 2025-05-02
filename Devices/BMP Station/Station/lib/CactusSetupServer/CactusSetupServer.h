#pragma once
#include <ESP8266WebServer.h>
#include <DNSServer.h>
#include <Arduino.h>

class CactusSetupServer
{
public:
    struct Response
    {
        String ssid;
        String password;
        String mqttSettingsBase64;
    };

    CactusSetupServer(uint8_t dnsPort = 53);
    void begin(const char *apSsid, const char *apPassword);
    void handleClient();
    bool isFinished() const;
    Response getResponse() const;
    void stop();

private:
    void handleRoot();
    void handleSubmit();
    void handleNotFound();

    const uint8_t DNS_PORT;
    IPAddress apIP;
    DNSServer dnsServer;
    ESP8266WebServer server;
    bool done;
    Response response;
};
