#include "CactusSetupServer.h"

const char *CactusSetupServer::FORM_HTML = R"rawliteral(
<!DOCTYPE html><html lang=ru><head><meta charset=UTF-8><meta name=viewport content="width=device-width,initial-scale=1"><title>Setup Station</title><style>body{font-family:sans-serif;background:#d0f0c0;padding:2em}.form-box{background:white;padding:2em;max-width:400px;margin:auto;border-radius:10px;box-shadow:0 0 10px rgba(0,0,0,0.2)}input,textarea{width:100%;padding:.5em;margin-top:.5em;margin-bottom:1em;border:1px solid #ccc;border-radius:5px}button{background:#4caf50;color:white;padding:.7em 1.5em;border:0;border-radius:5px;cursor:pointer}</style></head><body><div class=form-box><h2>Setup WiFi and MQTT</h2><form action=/submit method=POST><label>WiFi SSID</label><input type=text name=ssid required><label>WiFi Password</label><input type=password name=password required><label>MQTT Settings (Base64)</label><textarea name=mqtt rows=5 required></textarea><button type=submit>Save</button></form></div></body></html>
)rawliteral";

const char *CactusSetupServer::DONE_HTML = R"rawliteral(
<!DOCTYPE html><html lang="en"><head> <meta charset="UTF-8"> <meta name="viewport" content="width=device-width, initial-scale=1"> <title>Success</title> <style> body { margin: 0; padding: 2em; font-family: sans-serif; background: #d0f0c0; text-align: center; } div { background: #fff; padding: 1.5em; border-radius: 8px; display: inline-block; box-shadow: 0 0 8px rgba(0, 0, 0, 0.15); } h1 { margin: 0.2em 0; color: #4caf50; } p { margin: 1em 0; } </style></head><body> <div> <h1>Success!</h1> <p>Settings saved.</p> </div></body></html>
)rawliteral";

CactusSetupServer::CactusSetupServer(uint8_t dnsPort)
    : DNS_PORT(dnsPort),
      apIP(192, 168, 4, 1),
      server(80),
      configured(false),
      portalActive(false)
{
    Serial.println("[SetupServer] Constructor called");
}

void CactusSetupServer::beginOrStartPortal(const char *apSsid, const char *apPassword)
{
    Serial.println("[SetupServer] beginOrStartPortal()");
    tryConnect();
    if (!configured)
    {
        Serial.println("[SetupServer] No saved config, starting portal");
        startPortal(apSsid, apPassword);
    }
    else
    {
        Serial.println("[SetupServer] Connected using saved config");
    }
}

void CactusSetupServer::loop()
{
    if (configured)
        return;

    dnsServer.processNextRequest();
    server.handleClient();
}

void CactusSetupServer::onConfigured(std::function<void(const Response &)> cb)
{
    Serial.println("[SetupServer] onConfigured() callback registered");
    configCallback = cb;
}

bool CactusSetupServer::isConfigured() const
{
    return configured;
}

void CactusSetupServer::tryConnect()
{
    Serial.println("[SetupServer] tryConnect()");
    Response r;
    if (!loadConfig(r))
    {
        Serial.println("[SetupServer] loadConfig() failed");
        return;
    }

    Serial.print("[SetupServer] Saved config found: ");
    Serial.print(r.ssid);
    Serial.print(" ");
    Serial.print(r.password);
    Serial.print(" ");
    Serial.println(r.mqttBase64);

    Serial.println("[SetupServer] Decoding MQTT settings");
    size_t len;
    uint8_t *buf = CBase64::decode(r.mqttBase64.c_str(), r.mqttBase64.length(), len);
    if (!buf)
    {
        Serial.println("[SetupServer][ERROR] Base64 decode failed");
        return;
    }

    buf[len] = '\0';
    DynamicJsonDocument doc(len + 1);
    if (deserializeJson(doc, (char *)buf) != DeserializationError::Ok)
    {
        Serial.println("[SetupServer][ERROR] JSON parse failed");
        free(buf);
        return;
    }

    free(buf);
    Serial.println("[SetupServer] Configuration decoded, invoking callback");
    configured = true;
    response = r;
    if (configCallback)
        configCallback(response);
}

bool CactusSetupServer::saveConfig(const Response &in)
{
    StaticJsonDocument<512> doc;
    doc["ssid"] = in.ssid;
    doc["password"] = in.password;
    doc["mqttBase64"] = in.mqttBase64;

    char buf[512];
    size_t len = serializeJson(doc, buf, sizeof(buf));
    if (len == 0 || len >= sizeof(buf))
    {
        Serial.println("[SetupServer][ERROR] Config JSON too large");
        return false;
    }

    EEPROM.begin(512);
    EEPROM.write(0, 1);
    EEPROM.write(1, (uint8_t)(len & 0xFF));
    EEPROM.write(2, (uint8_t)(len >> 8));
    for (size_t i = 0; i < len; i++)
    {
        EEPROM.write(3 + i, buf[i]);
    }
    bool ok = EEPROM.commit();
    Serial.print("[SetupServer] saveConfig: commit ");
    Serial.println(ok ? "succeeded" : "failed");
    return ok;
}

bool CactusSetupServer::loadConfig(Response &out)
{
    EEPROM.begin(512);
    if (EEPROM.read(0) != 1)
    {
        Serial.println("[SetupServer] loadConfig: no valid flag");
        return false;
    }
    uint16_t len = EEPROM.read(1) | (EEPROM.read(2) << 8);
    if (len == 0 || len > 508)
    {
        Serial.print("[SetupServer] loadConfig: bad length=");
        Serial.println(len);
        return false;
    }
    StaticJsonDocument<512> doc;
    char buf[512];
    for (uint16_t i = 0; i < len; i++)
    {
        buf[i] = EEPROM.read(3 + i);
    }
    buf[len] = '\0';

    auto err = deserializeJson(doc, buf);
    if (err)
    {
        Serial.print("[SetupServer][ERROR] deserializeJson: ");
        Serial.println(err.c_str());
        return false;
    }

    out.ssid = String((const char *)doc["ssid"]);
    out.password = String((const char *)doc["password"]);
    out.mqttBase64 = String((const char *)doc["mqttBase64"]);

    Serial.print("[SetupServer] loadConfig: SSID=");
    Serial.println(out.ssid);
    return true;
}

void CactusSetupServer::startPortal(const char *apSsid, const char *apPassword)
{
    Serial.println("[SetupServer] startPortal()");
    WiFi.mode(WIFI_AP);
    WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0));
    WiFi.softAP(apSsid, apPassword);
    dnsServer.start(DNS_PORT, "*", apIP);

    server.on("/", HTTP_GET, std::bind(&CactusSetupServer::handleRoot, this));
    server.on("/submit", HTTP_POST, std::bind(&CactusSetupServer::handleSubmit, this));
    server.onNotFound(std::bind(&CactusSetupServer::handleNotFound, this));
    server.begin();

    portalActive = true;
    Serial.print("[SetupServer] AP started: ");
    Serial.print(apSsid);
    Serial.println(" (portal active)");
}

void CactusSetupServer::stopPortal()
{
    Serial.println("[SetupServer] stopPortal()");
    if (!portalActive)
    {
        Serial.println("[SetupServer] Portal was not active, skipping");
        return;
    }
    WiFi.softAPdisconnect(true);
    server.stop();
    dnsServer.stop();
    portalActive = false;
    Serial.println("[SetupServer] Portal stopped");
}

void CactusSetupServer::handleRoot()
{
    Serial.println("[SetupServer] HTTP GET /");
    server.send(200, "text/html", FORM_HTML);
}

void CactusSetupServer::handleSubmit()
{
    Serial.println("[SetupServer] HTTP POST /submit");
    response.ssid = server.arg("ssid");
    response.password = server.arg("password");
    response.mqttBase64 = server.arg("mqtt");

    Serial.print("[SetupServer] Received SSID: ");
    Serial.println(response.ssid);
    Serial.print("[SetupServer] Received MQTT Base64 length: ");
    Serial.println(response.mqttBase64.length());

    saveConfig(response);
    stopPortal();
    tryConnect();

    server.sendHeader("Connection", "close");
    server.send(200, "text/html", DONE_HTML);
    Serial.println("[SetupServer] Responded with DONE_HTML");
}

void CactusSetupServer::handleNotFound()
{
    Serial.println("[SetupServer] HTTP 404 Not Found");
    server.send(404, "text/plain", "404: Not Found");
}
