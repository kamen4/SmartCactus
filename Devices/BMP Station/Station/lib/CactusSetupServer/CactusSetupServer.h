#pragma once

#include <ESP8266WebServer.h>
#include <DNSServer.h>
#include <EEPROM.h>
#include <ArduinoJson.h>
#include <functional>
#include <CBase64.h>

class CactusSetupServer
{
public:
    struct Response
    {
        String ssid;
        String password;
        String mqttBase64;
    };

    CactusSetupServer(uint8_t dnsPort = 53);

    void beginOrStartPortal(const char *apSsid, const char *apPassword);
    void loop();
    void onConfigured(std::function<void(const Response &)> cb);
    bool isConfigured() const;

private:
    void tryConnect();
    bool loadConfig(Response &out);
    bool saveConfig(const Response &in);
    void startPortal(const char *apSsid, const char *apPassword);
    void stopPortal();
    void handleRoot();
    void handleSubmit();
    void handleNotFound();

    const uint8_t DNS_PORT;
    IPAddress apIP;
    DNSServer dnsServer;
    ESP8266WebServer server;

    bool configured;
    bool portalActive;
    Response response;
    std::function<void(const Response &)> configCallback;

    static const char *FORM_HTML;
    static const char *DONE_HTML;
};
