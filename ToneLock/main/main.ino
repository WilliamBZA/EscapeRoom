#include <WiFi.h>
#include <AsyncMqttClient.h>
#include <Keypad.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "tones.h"
#include "Timer.h"

extern "C" {
  #include "freertos/FreeRTOS.h"
  #include "freertos/timers.h"
}

#define BUZZER_PIN 18
#define BUTTON_PIN 13
#define ROWS  4
#define COLS  3

uint8_t rowPins[ROWS] = {12, 14, 27, 26};
uint8_t colPins[COLS] = {25, 33, 32};

char keyMap[ROWS][COLS] = {
  {'1','2','3'},
  {'4','5','6'},
  {'7','8','9'},
  {'*','0','#'}
};

Settings* settings = new Settings();
WiFiConnectionManager wifiManager(settings);
Timer resetInputTimer(3000);
int tonePasswordLength = 4; // difficulty of 50%
long debounceTimer;
int currentPasswordIndex = 0;

char tonePassword[9] = {"55497349"};

TimerHandle_t wifiReconnectTimer;
TimerHandle_t mqttReconnectTimer;
AsyncMqttClient mqttClient;

Keypad keypad = Keypad(makeKeymap(keyMap), rowPins, colPins, ROWS, COLS);

int getTone(char key) {
  switch (key) {
    case '1': return NOTE_C4;
    case '2': return NOTE_CS4;
    case '3': return NOTE_D4;
    case '4': return NOTE_DS4;
    case '5': return NOTE_E4;
    case '6': return NOTE_F4;
    case '7': return NOTE_FS4;
    case '8': return NOTE_G4;
    case '9': return NOTE_GS4;
    case '*': return NOTE_A4;
    case '0': return NOTE_AS4;
    case '#': return NOTE_B4;
  }
}

void ConnectToMqtt() {
  Serial.println("Connecting to MQTT...");
  mqttClient.connect();
}

void OnMqttConnect(bool sessionPresent) {
  Serial.println("Connected to MQTT.");
  Serial.print("Session present: ");
  Serial.println(sessionPresent);
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
}

void OnMqttUnsubscribe(uint16_t packetId) {
  Serial.println("Unsubscribe acknowledged.");
  Serial.print("  packetId: ");
  Serial.println(packetId);
}

void OnMqttPublish(uint16_t packetId) {
  Serial.println("Publish acknowledged.");
  Serial.print("  packetId: ");
  Serial.println(packetId);
}

void subscribeTo(char* topic) {
  uint16_t packetIdSub = mqttClient.subscribe(topic, 1);
  Serial.print("Subscribing to topic '"); Serial.print(topic); Serial.print("' at QoS 1, packetId: "); Serial.println(packetIdSub);
}

void SuscribeMqtt() {
  subscribeTo("escaperoom/puzzles/changedifficulty");
  subscribeTo("escaperoom/puzzles/startroom");
}

void PublishMqtt(char* topic, char* payload) {
  Serial.print("Publishing to topic '"); Serial.print(topic); Serial.print("' with payload: '"); Serial.print(payload); Serial.println("'");
  mqttClient.publish(topic, 1, true, payload);
}

void InitMqtt() {
  mqttReconnectTimer = xTimerCreate("mqttTimer", pdMS_TO_TICKS(2000), pdFALSE, (void*)0, reinterpret_cast<TimerCallbackFunction_t>(ConnectToMqtt));

  mqttClient.onConnect(OnMqttConnect);
  mqttClient.onDisconnect(OnMqttDisconnect);

  mqttClient.onSubscribe(OnMqttSubscribe);
  mqttClient.onUnsubscribe(OnMqttUnsubscribe);

  mqttClient.onMessage(OnMqttReceived);
  mqttClient.onPublish(OnMqttPublish);

  mqttClient.setServer("192.168.88.114", 1883);
}

String GetPayloadContent(char* data, size_t len) {
  String content = "";
  for(size_t i = 0; i < len; i++) {
    content.concat(data[i]);
  }
  return content;
}

void OnMqttReceived(char* cTopic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
  Serial.print("Publish received on topic: "); Serial.println(cTopic);

  String topic = (String)cTopic;
  topic.trim();
  
  String content = ((String)payload).substring(0, len);
  Serial.print(content); Serial.println();

  if (topic == "escaperoom/puzzles/changedifficulty") {
    int difficulty = content.toInt();
    tonePasswordLength = 8 * difficulty / 10;
    tonePasswordLength = max(min(tonePasswordLength, 8), 3);
    Serial.print("Changing difficulty to: "); Serial.print(difficulty); Serial.print("\t password length now: "); Serial.println(tonePasswordLength);
  } else {
    Serial.print("no match");
  }
}

void setup() {
  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT);

  Serial.begin(115200);
  while (!Serial) { }

  Serial.println("Starting up...");

  settings->loadDeviceSettings();

  if (wifiManager.ConnectToWifi()) {
    Serial.println("Wifi connected");
  } else {
    Serial.println("Couldn't connect to wifi");
  }

  InitMqtt();
  ConnectToMqtt();
}

void wrongPassword() {
  tone(BUZZER_PIN, NOTE_A2, 1000);
  debounceTimer += 1000;
}

void rightPassword() {
  tone(BUZZER_PIN, NOTE_E6, 1500);
  debounceTimer += 1500;
}

void loop() {
  wifiManager.wifi_loop();
  if (resetInputTimer.Check()) {
    currentPasswordIndex = 0;
    
    wrongPassword();
    resetInputTimer.Stop();
  }

  char key = keypad.getKey();

  if (key && debounceTimer <= millis()) {
    Serial.println(key);

    debounceTimer = millis() + 500;
    tone(BUZZER_PIN, getTone(key), 500);

    if (key != tonePassword[currentPasswordIndex]) {
      currentPasswordIndex = 0;
      wrongPassword();
      resetInputTimer.Stop();
    } else {
      currentPasswordIndex++;
      resetInputTimer.Start();

      if (currentPasswordIndex == tonePasswordLength) {
        rightPassword();
        currentPasswordIndex = 0;
        
        // Publish Unlocked
        PublishMqtt("escaperoom/puzzles/tonelock/puzzlesolved", "");
      }
    }
  }

  if (digitalRead(BUTTON_PIN) && debounceTimer <= millis()) {
    debounceTimer = millis() + (525 * tonePasswordLength);

    for (int x = 0; x < tonePasswordLength; x++) {
      tone(BUZZER_PIN, getTone(tonePassword[x]), 500);
      tone(BUZZER_PIN, 0, 25);
    }
  }
}
