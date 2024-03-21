#include <Arduino.h>

#include "settings.h"
#include "WifiConnectionManager.h"
#include "swarm.h"

#define LED_PIN 04
#define BUTTON_PIN 13
#define BUZZER_PIN 14

Settings* settings = new Settings();
WifiConnectionManager wifiManager(settings);
Swarm swarm;

void setup() {
  Serial.begin(115200);
  while (!Serial) { }

  Serial.println("Starting up...");

  settings->loadDeviceSettings();

  if (wifiManager.ConnectToWifi()) {
    Serial.println("Wifi connected");
  } else {
    Serial.println("Couldn't connect to wifi");
  }

  swarm.InitializeEspNow();
  
  pinMode(LED_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT);
}

bool b = false;

void loop() {
  wifiManager.wifi_loop();
  
  digitalWrite(LED_PIN, digitalRead(BUTTON_PIN));

  if (digitalRead(BUTTON_PIN)) {
    Serial.println("On");

    swarm.BroadcastAvailability();

    tone(BUZZER_PIN, 262, 1000);
  }
}
