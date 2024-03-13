#include <FS.h>
#include "settings.h"

#if defined(ESP32)
#include <SPIFFS.h>
#endif

Settings::Settings() {
  SPIFFS.begin();
}

void Settings::saveCurrentSettings() {
  File settingsFile = SPIFFS.open("/settings.json", "w");
  StaticJsonDocument<1024> settingsDoc;
  
  settingsDoc["devicename"] = deviceName;
  settingsDoc["ssid"] = ssid;
  settingsDoc["wifipassword"] = wifiPassword;

  if (serializeJson(settingsDoc, settingsFile) == 0) {
    Serial.println("Failed to write to file");
  }

  settingsFile.close();
}

void Settings::loadDeviceSettings() {
  File settingsFile = SPIFFS.open("/settings.json", "r");
  if (!settingsFile) {
    Serial.println("No settings file found");
    deviceName = "BalancePuzzle";

    return;
  }

  StaticJsonDocument<384> doc;

  DeserializationError error = deserializeJson(doc, settingsFile);
  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    return;
  }

  const char* devicename = doc["devicename"];
  const char* settingsSSID = doc["ssid"];
  const char* settingsWifiPassword = doc["wifipassword"];

  deviceName = devicename;
  ssid = settingsSSID;
  wifiPassword = settingsWifiPassword;

  Serial.print("Device name: "); Serial.println(deviceName);
  Serial.print("ssid: "); Serial.println(ssid);
  Serial.print("wifiPassword: "); Serial.println(wifiPassword);

  settingsFile.close();
}

void Settings::resetSettings() {
  deviceName = "";
  ssid = "";
  wifiPassword = "";
}

void Settings::setCalibration(int targetNumber, int xGyroOffset, int yGyroOffset, int zGyroOffset, int xAccelOffset, int yAccelOffset, int zAccelOffset) {
  offsets[targetNumber] = {};
  
  offsets[targetNumber].xGyroOffset = xGyroOffset;
  offsets[targetNumber].yGyroOffset = yGyroOffset;
  offsets[targetNumber].zGyroOffset = zGyroOffset;
  offsets[targetNumber].xAccelOffset = xAccelOffset;
  offsets[targetNumber].yAccelOffset = yAccelOffset;
  offsets[targetNumber].zAccelOffset = zAccelOffset;
}

void Settings::saveCalibration() {
  File calibrationFile = SPIFFS.open("/calibration.json", "w");

  JsonDocument doc;
  JsonArray data = doc["data"].to<JsonArray>();

  for (auto x = 0; x < 3; x++) {
    JsonObject offsetData = data.add<JsonObject>();
    offsetData["xGyroOffset"] = offsets[x].xGyroOffset;
    offsetData["yGyroOffset"] = offsets[x].yGyroOffset;
    offsetData["zGyroOffset"] = offsets[x].zGyroOffset;
    offsetData["xAccelOffset"] = offsets[x].xAccelOffset;
    offsetData["yAccelOffset"] = offsets[x].yAccelOffset;
    offsetData["zAccelOffset"] = offsets[x].zAccelOffset;
  }

  doc.shrinkToFit();  // optional

  if (serializeJson(doc, calibrationFile) == 0) {
    Serial.println("Failed to write calibration to file");
  }

  calibrationFile.close();
}

bool Settings::loadCalibration() {
  File settingsFile = SPIFFS.open("/calibration.json", "r");
  if (!settingsFile) {
    Serial.println("No calibration settings file found.");
    return false;
  }

  JsonDocument doc;
  DeserializationError error = deserializeJson(doc, settingsFile);

  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    return false;
  }

  int x = 0;
  for (JsonObject data_item : doc["data"].as<JsonArray>()) {
    offsets[x].xGyroOffset = data_item["xGyroOffset"];
    offsets[x].yGyroOffset = data_item["yGyroOffset"];
    offsets[x].zGyroOffset = data_item["zGyroOffset"];
    offsets[x].xAccelOffset = data_item["xAccelOffset"];
    offsets[x].yAccelOffset = data_item["yAccelOffset"];
    offsets[x].zAccelOffset = data_item["zAccelOffset"];

    x++;
  }

  numberOfTargets = x;
  
  return true;
}
