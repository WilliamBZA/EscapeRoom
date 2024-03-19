#include "swarm.h"
#include <Arduino.h>

#ifdef ESP32
  #include <WiFi.h>
  #include <esp_now.h>
#else
  #include <ESP8266WiFi.h>
  #include <espnow.h>
#endif

typedef struct struct_message {
    int id;
    long millis_offset;
};

uint8_t broadcastAddress[] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
constexpr char WIFI_SSID[] = "dropitlikeaSquat";

int32_t getWiFiChannel(const char *ssid) {
  if (int32_t n = WiFi.scanNetworks()) {
      for (uint8_t i=0; i<n; i++) {
          if (!strcmp(ssid, WiFi.SSID(i).c_str())) {
              return WiFi.channel(i);
          }
      }
  }
  return 0;
}

static void OnDataSent(uint8_t *mac_addr, uint8_t status) {
  Serial.print("\r\nLast Packet Send Status:\t");
  Serial.print("Delivery status: "); Serial.println(status);
}

static void OnDataRecv(uint8_t * mac, uint8_t *incomingData, uint8_t len) {
    // Copies the sender mac address to a string
  char macStr[18];
  Serial.print("Packet received from: ");
  snprintf(macStr, sizeof(macStr), "%02x:%02x:%02x:%02x:%02x:%02x", mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
  
  Serial.print("Incoming message from: "); Serial.println(macStr);
}

void Swarm::InitializeEspNow() {
  int32_t channel = getWiFiChannel(WIFI_SSID);
  
  WiFi.printDiag(Serial); // Uncomment to verify channel number before
  wifi_promiscuous_enable(1);
  wifi_set_channel(channel);
  wifi_promiscuous_enable(0);
  WiFi.printDiag(Serial); // Uncomment to verify channel change after
  
  if (esp_now_init()) {
    Serial.println("Error initializing ESP-NOW");
    return;
  } else {
    Serial.println("ESP-NOW initialized");
  }

  esp_now_register_send_cb(OnDataSent);
  esp_now_register_recv_cb(OnDataRecv);

  //Add peer
  if (esp_now_add_peer(broadcastAddress, ESP_NOW_ROLE_SLAVE, 1, NULL, 0)){
    Serial.println("Failed to add peer");
    return;
  }
}

bool Swarm::BroadcastAvailability() {
  struct_message message;
  message.millis_offset = millis();
  
  auto result = esp_now_send(broadcastAddress, (uint8_t *) &message, sizeof(message));
  if (result) {
    Serial.println("Sent with success");
    return true;
  }

  Serial.println("Error sending the data");
  return false;
}
