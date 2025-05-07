#include "CactusSetupServer.h"

static const char *FORM_HTML = R"rawliteral(
<!DOCTYPE html><html lang=ru><head><meta charset=UTF-8><meta name=viewport content="width=device-width,initial-scale=1"><title>Setup Station</title><style>body{font-family:sans-serif;background:#d0f0c0;padding:2em}.form-box{background:white;padding:2em;max-width:400px;margin:auto;border-radius:10px;box-shadow:0 0 10px rgba(0,0,0,0.2)}input,textarea{width:100%;padding:.5em;margin-top:.5em;margin-bottom:1em;border:1px solid #ccc;border-radius:5px}button{background:#4caf50;color:white;padding:.7em 1.5em;border:0;border-radius:5px;cursor:pointer}</style></head><body><div class=form-box><h2>Setup WiFi and MQTT</h2><form action=/submit method=POST><label>WiFi SSID</label><input type=text name=ssid required><label>WiFi Password</label><input type=password name=password required><label>MQTT Settings (Base64)</label><textarea name=mqtt rows=5 required></textarea><button type=submit>Save</button></form></div></body></html>
)rawliteral";

static const char *DONE_HTML = R"rawliteral(
<!DOCTYPE html><html lang="en"><head> <meta charset="UTF-8"> <meta name="viewport" content="width=device-width, initial-scale=1"> <title>Success</title> <style> body { margin: 0; padding: 2em; font-family: sans-serif; background: #d0f0c0; text-align: center; } div { background: #fff; padding: 1.5em; border-radius: 8px; display: inline-block; box-shadow: 0 0 8px rgba(0, 0, 0, 0.15); } h1 { margin: 0.2em 0; color: #4caf50; } p { margin: 1em 0; } </style></head><body> <div> <h1>Success!</h1> <p>Settings saved.</p> </div></body></html>
)rawliteral";

CactusSetupServer::CactusSetupServer(uint8_t dnsPort)
    : DNS_PORT(dnsPort),
      apIP(192, 168, 4, 1),
      dnsServer(),
      server(80),
      done(false) {}

void CactusSetupServer::begin(const char *apSsid, const char *apPassword)
{
    WiFi.mode(WIFI_AP);
    WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0));
    WiFi.softAP(apSsid, apPassword);
    dnsServer.start(DNS_PORT, "*", apIP);

    server.on("/", HTTP_GET, std::bind(&CactusSetupServer::handleRoot, this));
    server.on("/submit", HTTP_POST, std::bind(&CactusSetupServer::handleSubmit, this));
    server.onNotFound(std::bind(&CactusSetupServer::handleNotFound, this));
    server.begin();
}

void CactusSetupServer::handleClient()
{
    if (!done)
    {
        dnsServer.processNextRequest();
        server.handleClient();
    }
}

bool CactusSetupServer::isFinished() const
{
    return done;
}

CactusSetupServer::Response CactusSetupServer::getResponse() const
{
    return response;
}

void CactusSetupServer::handleRoot()
{
    server.send(200, "text/html", FORM_HTML);
}

void CactusSetupServer::handleSubmit()
{
    response.ssid = server.arg("ssid");
    response.password = server.arg("password");
    response.mqttSettingsBase64 = server.arg("mqtt");
    done = true;

    server.sendHeader("Connection", "close");
    server.send(200, "text/html", DONE_HTML);
}

void CactusSetupServer::handleNotFound()
{
    server.send(404, "text/plain", "404: Not found");
}

void CactusSetupServer::stop()
{
    WiFi.softAPdisconnect(true);
    server.stop();
    dnsServer.stop();
}
