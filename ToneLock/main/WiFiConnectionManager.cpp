#include <WiFiManager.h>
#include <DNSServer.h>
#include <ArduinoOTA.h>
#include <wifi.h>

#if defined(ESP32)
#include <ESPmDNS.h>
#else
#include <ESP8266mDNS.h>
#endif


#include "WiFiConnectionManager.h"
#include "settings.h"
#include "time.h"

WiFiConnectionManager::WiFiConnectionManager(Settings* settingsInstance) {
  settings = settingsInstance;
}

bool WiFiConnectionManager::connectToWifi() {
  WiFi.disconnect();
  WiFi.mode(WIFI_OFF);

  WiFi.mode(WIFI_AP_STA);

  if (settings->ssid != "") {
    Serial.println("Using saved SSID and Password to attempt WiFi Connection.");
    Serial.print("Saved SSID is "); Serial.println(settings->ssid);
    Serial.print("Saved Password is "); Serial.println(settings->wifiPassword);

    WiFi.mode(WIFI_STA);
    Serial.println("\nConnecting to WiFi Network ..");
    WiFi.begin(settings->ssid, settings->wifiPassword);

    int attempt = 0;
    while(WiFi.status() != WL_CONNECTED){
      attempt++;
      Serial.print(".");
      delay(100);

      if (attempt++ >= 200) {
        WiFi.disconnect();
        return false;
      }
    }

    Serial.print("\nConnected to the WiFi network on channel: "); Serial.println(WiFi.channel());
    Serial.print("Local IP: ");
    Serial.println(WiFi.localIP());
    isConnected = true;
    return true;
  }

  return false;
}

bool WiFiConnectionManager::ConnectToWifi() {
  if (connectToWifi()) {
    String dnsName = settings->deviceName;
    dnsName.replace(" ", "");
    if (!MDNS.begin(dnsName)) {
      Serial.println("Error starting mDNS");
    }

    char* ntpServer = "pool.ntp.org";
    long  gmtOffset_sec = 7200;

    configTime(gmtOffset_sec, 0, ntpServer);

    configure_OTA();
    
    return true;
  }

  configureCaptivePortal();

  return false;
}

void WiFiConnectionManager::configureCaptivePortal() {
  Serial.println("Starting captive portal");

  char portalSSID[23];
#if defined(ESP32)
  snprintf(portalSSID, 23, "simonsays-%llX", ESP.getEfuseMac());
#else if defined(ESP8266)
  snprintf(portalSSID, 23, "simonsays-%X", ESP.getChipId());
#endif
  Serial.print("SSID will be: "); Serial.println(portalSSID);
  
  WiFi.mode(WIFI_AP);
  WiFi.softAP(portalSSID);
  Serial.print("AP IP address: ");Serial.println(WiFi.softAPIP());
    
  Serial.println("Starting DNS Server");
  dnsServer.start(53, "*", WiFi.softAPIP());
}

void WiFiConnectionManager::configure_OTA() {
  Serial.println("Configuring OTA");
  // Make sure the flash size matches. For ESP8266 12F it should be 4MB.
  ArduinoOTA.setHostname(settings->deviceName.c_str());

  ArduinoOTA.onStart([]() {
    String type;
    if (ArduinoOTA.getCommand() == U_FLASH) {
      type = "sketch";
    } else {
      // U_SPIFFS
      type = "filesystem";
    }

    // NOTE: if updating SPIFFS this would be the place to unmount SPIFFS using SPIFFS.end()
    Serial.println("Start updating " + type);
  });

  ArduinoOTA.onEnd([]() {
  });

  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
  });

  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("End Failed");
  });

  ArduinoOTA.begin();
}

void WiFiConnectionManager::wifi_loop() {
  ArduinoOTA.handle();
  dnsServer.processNextRequest();
}
