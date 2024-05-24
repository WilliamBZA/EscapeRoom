#include <WiFi.h>
#include <AsyncMqttClient.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "Timer.h"

extern "C" {
  #include "freertos/FreeRTOS.h"
  #include "freertos/timers.h"
}

#define RELAY_PIN 22
#define REDLED_PIN 23

Settings* settings = new Settings();
WiFiConnectionManager wifiManager(settings);
Timer relockMaglockTimer(3000);
int topicCount = 0;

TimerHandle_t wifiReconnectTimer;
TimerHandle_t mqttReconnectTimer;
TimerHandle_t startupTimer;
AsyncMqttClient mqttClient;

void clearStartupTimer() {
  xTimerStop(startupTimer, 0);
  xTimerDelete(startupTimer, 0);
  
  digitalWrite(REDLED_PIN, LOW);
}

void ConnectToMqtt() {
  Serial.println("Connecting to MQTT...");
  mqttClient.connect();
}

void OnMqttConnect(bool sessionPresent) {
  Serial.println("Connected to MQTT.");
  Serial.print("Session present: ");
  Serial.println(sessionPresent);

  clearStartupTimer();
  
  SuscribeMqtt();
}

void OnMqttDisconnect(AsyncMqttClientDisconnectReason reason) {
  Serial.println("Disconnected from MQTT.");

  xTimerStart(mqttReconnectTimer, 0);
}

void OnMqttSubscribe(uint16_t packetId, uint8_t qos) {
  Serial.println("Subscribe acknowledged.");
  Serial.print("  packetId: ");
  Serial.println(packetId);
  Serial.print("  qos: ");
  Serial.println(qos);

  topicCount--;
  if (topicCount == 0) {
    clearStartupTimer();
  }
}

void OnMqttUnsubscribe(uint16_t packetId) {
  Serial.println("Unsubscribe acknowledged.");
  Serial.print("  packetId: ");
  Serial.println(packetId);
}

void subscribeTo(const char* topic) {
  uint16_t packetIdSub = mqttClient.subscribe(topic, 1);
  Serial.print("Subscribing to topic '"); Serial.print(topic); Serial.print("' at QoS 1, packetId: "); Serial.println(packetIdSub);
  topicCount++;
}

void SuscribeMqtt() {
  startupTimer = xTimerCreate("startupdiagnostics", 300, pdTRUE, (void*)123, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);

  String topic = "escaperoom/puzzles/" + settings->deviceName + "/unlock";
  subscribeTo(topic.c_str());
}

void PublishMqtt(char* topic, char* payload) {
  Serial.print("Publishing to topic '"); Serial.print(topic); Serial.print("' with payload: '"); Serial.print(payload); Serial.println("'");
  mqttClient.publish(topic, 1, false, payload);
}

void ReconnectToWifi() {
  if (wifiManager.ConnectToWifi(WiFiEvent)) {
    Serial.println("Wifi connected");
    clearStartupTimer();

    xTimerStop(wifiReconnectTimer, 0); // Stop reconnecting to wifi
    ConnectToMqtt();
  } else {
    Serial.println("Couldn't connect to wifi");
  }
}

void InitMqtt() {
  startupTimer = xTimerCreate("startupdiagnostics", 100, pdTRUE, (void*)100, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);
  
  wifiReconnectTimer = xTimerCreate("wifiReconnectTimer", pdMS_TO_TICKS(2000), pdFALSE, (void*)0, reinterpret_cast<TimerCallbackFunction_t>(ConnectToMqtt));

  mqttClient.onConnect(OnMqttConnect);
  mqttClient.onDisconnect(OnMqttDisconnect);

  mqttClient.onSubscribe(OnMqttSubscribe);
  mqttClient.onUnsubscribe(OnMqttUnsubscribe);

  mqttClient.onMessage(OnMqttReceived);

  mqttClient.setServer("192.168.88.114", 1883);
}

void OnMqttReceived(char* cTopic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
  String topic = (String)cTopic;
  topic.trim();

  Serial.print("Publish received on topic: "); Serial.println(topic);
  
  String content = ((String)payload).substring(0, len);
  Serial.print(content); Serial.println();

  digitalWrite(RELAY_PIN, LOW);
  relockMaglockTimer.Start();
}

void WiFiEvent(WiFiEvent_t event) {
  Serial.printf("[WiFi-event] event: %d\n", event);
  switch(event) {
    case SYSTEM_EVENT_STA_DISCONNECTED:
      Serial.println("WiFi lost connection");
      xTimerStart(wifiReconnectTimer, 0);
      break;
  }
}

bool isHigh = true;
void diganosticsTimerCallback(TimerHandle_t timer) {
  digitalWrite(REDLED_PIN, isHigh);

  isHigh = !isHigh;
}

void setup() {
  pinMode(RELAY_PIN, OUTPUT);
  pinMode(REDLED_PIN, OUTPUT);

  digitalWrite(RELAY_PIN, HIGH);

  Serial.begin(115200);
  while (!Serial) { }

  Serial.println("Starting up...");

  settings->loadDeviceSettings();

  startupTimer = xTimerCreate("startupdiagnostics", 100, pdTRUE, (void*)123, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);

  if (wifiManager.ConnectToWifi(WiFiEvent)) {
    Serial.println("Wifi connected");
    clearStartupTimer();
  } else {
    Serial.println("Couldn't connect to wifi");
  }

  InitMqtt();
  ConnectToMqtt();
}

long ledsOffTime = -1;
void loop() {
  wifiManager.wifi_loop();

  if (relockMaglockTimer.Check()) {
    relockMaglockTimer.Stop();
    digitalWrite(RELAY_PIN, HIGH);
  }

  if (millis() > ledsOffTime) {
    digitalWrite(REDLED_PIN, LOW);
  }
}
