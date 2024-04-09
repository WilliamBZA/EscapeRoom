#ifndef WIFIHELPER_h
#define WIFIHELPER_h

#include <DNSServer.h>
#include "settings.h"

class WiFiConnectionManager {
  public:
    WiFiConnectionManager(Settings* settings);
    
    bool ConnectToWifi(void (*eventCallback)(WiFiEvent_t event));
    bool isConnected;

    void wifi_loop();

    Settings* settings;
    DNSServer dnsServer;

  protected:
    bool connectToWifi(void (*eventCallback)(WiFiEvent_t event));
    void configureCaptivePortal();
    void configure_OTA();
};

#endif
