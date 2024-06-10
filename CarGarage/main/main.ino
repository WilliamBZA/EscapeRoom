#include <WiFi.h>
#include <AsyncMqttClient.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "Timer.h"
#include <ESP32Servo.h>
#include <WS2812FX.h>

extern "C" {
  #include "freertos/FreeRTOS.h"
  #include "freertos/timers.h"
}

#define SONIC_TRIGGER_PIN 5
#define SONIC_ECHO_PIN 18
#define SOUND_SPEED 0.034

#define LED_COUNT 54
#define LED_PIN 13

Settings* settings = new Settings();
WiFiConnectionManager wifiManager(settings);
int topicCount = 0;

TimerHandle_t wifiReconnectTimer;
TimerHandle_t mqttReconnectTimer;
TimerHandle_t startupTimer;
AsyncMqttClient mqttClient;

Timer lightsOnTimer(3000);
WS2812FX ws2812fx = WS2812FX(LED_COUNT, LED_PIN, NEO_RGB);

long duration;
float distanceCm;

void clearStartupTimer() {
  xTimerStop(startupTimer, 0);
  xTimerDelete(startupTimer, 0);
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

  subscribeTo("escaperoom/puzzles/startroom");
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

  Serial.print("Publish received on topic: "); Serial.println(cTopic);

  if (topic == "escaperoom/puzzles/startroom") {
  }
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
  isHigh = !isHigh;
}

void setup() {
  pinMode(SONIC_TRIGGER_PIN, OUTPUT);
  pinMode(SONIC_ECHO_PIN, INPUT_PULLDOWN);

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

  ws2812fx.init();
  ws2812fx.setBrightness(0);
  ws2812fx.setSpeed(2000);
  ws2812fx.setMode(FX_MODE_CHASE_COLOR);
  ws2812fx.setColor(BLUE);
  ws2812fx.start();

  InitMqtt();
  ConnectToMqtt();
}

void sonicDistance_loop() {
  // Clears the trigPin
  digitalWrite(SONIC_TRIGGER_PIN, LOW);
  delayMicroseconds(2);
  // Sets the trigPin on HIGH state for 10 micro seconds
  digitalWrite(SONIC_TRIGGER_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(SONIC_TRIGGER_PIN, LOW);
  
  // Reads the echoPin, returns the sound wave travel time in microseconds
  duration = pulseIn(SONIC_ECHO_PIN, HIGH);
  
  // Calculate the distance
  distanceCm = duration * SOUND_SPEED/2;

  Serial.print("Distance (cm): ");
  Serial.println(distanceCm);

  if (distanceCm <= 9 && !lightsOnTimer.IsRunning() && distanceCm > 2) {
    lightsOnTimer.Start();
    ws2812fx.setBrightness(100);
  }
}

void loop() {
  wifiManager.wifi_loop();

  if (lightsOnTimer.IsRunning()) {
    if (lightsOnTimer.Check()) {
      lightsOnTimer.Stop();

      ws2812fx.setBrightness(0);
    }

    ws2812fx.service();
  }

  if (millis() % 50 == 0) {
    sonicDistance_loop();
  }
}
