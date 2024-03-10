#ifndef SETTINGS_h
#define SETTINGS_h

#include <FS.h>
#include <ArduinoJson.h>

#if defined(ESP32)
#include <SPIFFS.h>
#endif

struct CalibrationOffsets {
  int xGyroOffset;
  int yGyroOffset;
  int zGyroOffset;
  int xAccelOffset;
  int yAccelOffset;
  int zAccelOffset;
};

class Settings {
  public:
    Settings();

    void saveCurrentSettings();
    void loadDeviceSettings();
    void resetSettings();

    bool loadCalibration();
    void saveCalibration();
    void setCalibration(int targetNumber, int xGyroOffset, int yGyroOffset, int zGyroOffset, int xAccelOffset, int yAccelOffset, int zAccelOffset);

    String deviceName;
    String ssid;
    String wifiPassword;

    CalibrationOffsets offsets[3];
};

#endif
