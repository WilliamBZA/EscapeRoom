#ifndef SETTINGS_h
#define SETTINGS_h

#include <FS.h>
#include <ArduinoJson.h>

#if defined(ESP32)
#include <SPIFFS.h>
#endif

class Settings {
  public:
    Settings();

    void saveCurrentSettings();
    void loadDeviceSettings();
    void resetSettings();

    String deviceName;
    String ssid;
    String wifiPassword;
};

#endif
