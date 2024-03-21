#include "swarm.h"
#include <Arduino.h>

#ifdef ESP32
  #include <esp_wifi.h>
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
          Serial.print("Checking SSID: "); Serial.println(WiFi.SSID(i));
          if (!strcmp(ssid, WiFi.SSID(i).c_str())) {
            Serial.print(ssid); Serial.print(" is running on channel "); Serial.println(WiFi.channel(i));
            return WiFi.channel(i);
          }
      }
  }
  
  return 0;
}

#ifdef ESP32
static void OnDataSent(const uint8_t *mac_addr, esp_now_send_status_t status) {
#else
static void OnDataSent(uint8_t *mac_addr, uint8_t status) {
#endif
  Serial.print("\r\nLast Packet Send Status:\t");
  Serial.print("Delivery status: "); Serial.println(status);
}

#ifdef ESP32
static void OnDataRecv(const uint8_t * mac, const uint8_t *incomingData, int len) {
#else
static void OnDataRecv(uint8_t * mac, uint8_t *incomingData, uint8_t len) {
#endif
    // Copies the sender mac address to a string
  char macStr[18];
  Serial.print("Packet received from: ");
  snprintf(macStr, sizeof(macStr), "%02x:%02x:%02x:%02x:%02x:%02x", mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
  
  Serial.print("Incoming message from: "); Serial.println(macStr);
}

void Swarm::InitializeEspNow() {
  int32_t channel = getWiFiChannel(WIFI_SSID);

#ifdef ESP32
  if (esp_now_init() != ESP_OK) {
#else
  if (esp_now_init()) {
#endif
    Serial.println("Error initializing ESP-NOW");
    return;
  } else {
    Serial.println("ESP-NOW initialized");
  }

  esp_now_register_send_cb(OnDataSent);
  esp_now_register_recv_cb(OnDataRecv);

  //Add peer
  if (!AddPeer(broadcastAddress, channel)) {
    Serial.println("Failed to add peer");
    return;
  } else {
    Serial.println("Peer added");
  }
}

#ifdef ESP32
bool Swarm::AddPeer(const uint8_t * mac_addr, uint8_t channel) {
  esp_now_peer_info_t slave;
  
  memset(&slave, 0, sizeof(slave));
  const esp_now_peer_info_t *peer = &slave;
  memcpy(slave.peer_addr, mac_addr, 6);
  
  //slave.channel = channel; // pick a channel
  slave.encrypt = 0; // no encryption
  // check if the peer exists
  bool exists = esp_now_is_peer_exist(slave.peer_addr);
  if (exists) {
    // Slave already paired.
    Serial.println("Already Paired");
    return true;
  }
  else {
    esp_err_t addStatus = esp_now_add_peer(peer);
    if (addStatus == ESP_OK) {
      // Pair success
      Serial.println("Pair success");
      return true;
    } else {
      Serial.println("Pair failed");
      return false;
    }
  }
}
#else
bool Swarm::AddPeer(uint8_t * mac_addr, uint8_t channel) {
  return esp_now_add_peer(mac_addr, ESP_NOW_ROLE_SLAVE, channel, NULL, 0);
}
#endif

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
