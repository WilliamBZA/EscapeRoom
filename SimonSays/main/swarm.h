#ifndef SWARM_h
#define SWARM_h

#ifdef ESP32
  #include <WiFi.h>
  #include <esp_now.h>
#else
  #include <ESP8266WiFi.h>
  #include <espnow.h>
#endif

class Swarm {
  public:
    void InitializeEspNow();
    bool BroadcastAvailability();
};

#endif
