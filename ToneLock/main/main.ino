#include <WiFi.h>
#include <AsyncMqttClient.h>
#include <Keypad.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "tones.h"
#include "Timer.h"
#include "UUID.h"

extern "C" {
  #include "freertos/FreeRTOS.h"
  #include "freertos/timers.h"
}

#define BUZZER_PIN 18
#define REDLED_PIN 23
#define GREENLED_PIN 22
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
int tonePasswordLength = 4;
long debounceTimer;
int currentPasswordIndex = 0;
int topicCount = 0;

char tonePassword[9] = {"83464311"};

TimerHandle_t wifiReconnectTimer;
TimerHandle_t mqttReconnectTimer;
TimerHandle_t startupTimer;
AsyncMqttClient mqttClient;

Keypad keypad = Keypad(makeKeymap(keyMap), rowPins, colPins, ROWS, COLS);

int getTone(char key) {
  switch (key) {
    case '1': return NOTE_A4;
    case '2': return NOTE_AS4;
    case '3': return NOTE_B4;
    case '4': return NOTE_C5;
    case '5': return NOTE_CS5;
    case '6': return NOTE_D5;
    case '7': return NOTE_DS4;
    case '8': return NOTE_E5;
    case '9': return NOTE_F5;
    case '0': return NOTE_FS5;
    case '*': return 0;
    case '#': return 0;
  }
}

void clearStartupTimer() {
  xTimerStop(startupTimer, 0);
  xTimerDelete(startupTimer, 0);
  
  digitalWrite(REDLED_PIN, LOW);
  digitalWrite(GREENLED_PIN, LOW);
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

void OnMqttPublish(uint16_t packetId) {
  Serial.println("Publish acknowledged.");
  Serial.print("  packetId: ");
  Serial.println(packetId);
}

void subscribeTo(char* topic) {
  uint16_t packetIdSub = mqttClient.subscribe(topic, 1);
  Serial.print("Subscribing to topic '"); Serial.print(topic); Serial.print("' at QoS 1, packetId: "); Serial.println(packetIdSub);
  topicCount++;
}

void SuscribeMqtt() {
  startupTimer = xTimerCreate("startupdiagnostics", 300, pdTRUE, (void*)123, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);
  
  subscribeTo("escaperoom/puzzles/changedifficulty");
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
  mqttClient.onPublish(OnMqttPublish);

  mqttClient.setServer("192.168.88.114", 1883);
}

int getPasswordLengthFromDifficulty(int difficulty) {
  switch (difficulty) {
    case 10: return 8;
    case 9: return 7;
    case 8: return 6;
    case 7: return 6;
    case 6: return 5;
    case 5: return 4;
  }

  return 3;
}

void OnMqttReceived(char* cTopic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
  Serial.print("Publish received on topic: "); Serial.println(cTopic);

  String topic = (String)cTopic;
  topic.trim();
  
  String content = ((String)payload).substring(0, len);
  Serial.print("Content: '"); Serial.print(content); Serial.print("'"); Serial.println();

  if (topic == "escaperoom/puzzles/changedifficulty") {
    int difficulty = content.toInt();
    tonePasswordLength = getPasswordLengthFromDifficulty(difficulty);
    tonePasswordLength = max(min(tonePasswordLength, 8), 3);
    Serial.print("Changing difficulty to: "); Serial.print(difficulty); Serial.print("\t password length now: "); Serial.println(tonePasswordLength);
  } else if (topic == "escaperoom/puzzles/startroom") {
    Serial.println("Start new run, get ID out!");
  }
  else {
    Serial.print("no match");
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
  if (pvTimerGetTimerID(timer) == (void*)123) {
    digitalWrite(REDLED_PIN, isHigh);
  } else {
    digitalWrite(GREENLED_PIN, isHigh);
  }
  isHigh = !isHigh;
}

void setup() {
  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(REDLED_PIN, OUTPUT);
  pinMode(GREENLED_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT_PULLDOWN);

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

void wrongPassword() {
  tone(BUZZER_PIN, NOTE_A2, 200);
  debounceTimer += 500;
  digitalWrite(REDLED_PIN, HIGH);
}

void rightPassword() {
  tone(BUZZER_PIN, NOTE_C4, 133);
  tone(BUZZER_PIN, 0, 16);
  tone(BUZZER_PIN, NOTE_C4, 133);
  tone(BUZZER_PIN, 0, 16);
  tone(BUZZER_PIN, NOTE_C4, 133);
  tone(BUZZER_PIN, 0, 16);
  tone(BUZZER_PIN, NOTE_C4, 266);
  tone(BUZZER_PIN, 0, 33);
  tone(BUZZER_PIN, NOTE_G3, 266);
  tone(BUZZER_PIN, 0, 50);
  tone(BUZZER_PIN, NOTE_A3, 266);
  tone(BUZZER_PIN, 0, 50);
  tone(BUZZER_PIN, NOTE_C4, 133);
  tone(BUZZER_PIN, 0, 33);
  tone(BUZZER_PIN, NOTE_A3, 133);
  tone(BUZZER_PIN, 0, 33);
  tone(BUZZER_PIN, NOTE_C4, 266);
  
  debounceTimer += 1500;
  digitalWrite(GREENLED_PIN, HIGH);
}

void playPasswordTone() {
  tone(BUZZER_PIN, NOTE_E5, 200);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_B4, 400);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_C5, 400);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_D5, 200);
  tone(BUZZER_PIN, 0, 50);
/*
  tone(BUZZER_PIN, NOTE_C5, 400);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_B4, 400);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_A4, 200);
  tone(BUZZER_PIN, 0, 50);

  tone(BUZZER_PIN, NOTE_A4, 400);
  tone(BUZZER_PIN, 0, 50);*/
}

long ledsOffTime = -1;
void loop() {
  wifiManager.wifi_loop();
  if (resetInputTimer.Check()) {
    currentPasswordIndex = 0;
    
    wrongPassword();
    resetInputTimer.Stop();
    digitalWrite(REDLED_PIN, LOW);
    digitalWrite(GREENLED_PIN, LOW);
  }

  char key = keypad.getKey();

  if (key && debounceTimer <= millis()) {
    ledsOffTime = millis() + 500;
    Serial.println(key);

    debounceTimer = millis() + 500;
    tone(BUZZER_PIN, getTone(key), 500);

    if (key != tonePassword[currentPasswordIndex]) {
      currentPasswordIndex = 0;
      wrongPassword();
      resetInputTimer.Stop();
    } else {
      digitalWrite(GREENLED_PIN, HIGH);
      currentPasswordIndex++;
      resetInputTimer.Start();

      if (currentPasswordIndex == tonePasswordLength) {
        rightPassword();
        currentPasswordIndex = 0;
        resetInputTimer.Stop();
        
        // Publish Unlocked
        UUID messageId;
        String message = "{\"Id\": \"" + String(messageId.toCharArray()) + "\",\"Headers\":{\"NServiceBus.EnclosedMessageTypes\":\"ToneLockSolved, Messages\"},\"Body\":\"eyJSdW5JZCI6IjMzMyJ9\"}";
        PublishMqtt("escaperoom/puzzles/tonelock/puzzlesolved", (char*)message.c_str());
        PublishMqtt("escaperoom/puzzles/easytreasurechest/unlock", "");
      }
    }
  }

  if (digitalRead(BUTTON_PIN) && debounceTimer <= millis()) {
    debounceTimer = millis() + (525 * tonePasswordLength);

    playPasswordTone();
  }

  if (millis() > ledsOffTime) {
    digitalWrite(REDLED_PIN, LOW);
    digitalWrite(GREENLED_PIN, LOW);
  }
}
