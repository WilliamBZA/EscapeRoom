#include <WiFi.h>
#include <AsyncTCP.h>
#include <AsyncMqttClient.h>
#include <ESPAsyncWebServer.h>
#include <Dictionary.h>
#include <HTTPClient.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "Timer.h"
#include "PlayCorrectPasswordTimer.h"

extern "C" {
  #include "freertos/FreeRTOS.h"
  #include "freertos/timers.h"
}

#define LEFT_BUTTON_PIN 22
#define RIGHT_BUTTON_PIN 23
#define REDLED_PIN 14
#define GREENLED_PIN 15

Settings* settings = new Settings();
WiFiConnectionManager wifiManager(settings);
AsyncWebServer server(80);

TimerHandle_t wifiReconnectTimer;
TimerHandle_t mqttReconnectTimer;
TimerHandle_t startupTimer;
AsyncMqttClient mqttClient;
int topicCount = 0;

Timer deviceHeartbeatTimer(2000);
Dictionary &deviceList = *(new Dictionary());
String masterName;

PlayCorrectPasswordTimer* correctGuessPlayer = new PlayCorrectPasswordTimer(PublishMqtt);
Timer incorrectGuessTimer(1500);
Timer puzzleSolvedTimer(1500);

long ledsOffTime = -1;
int currentSolveIndex = 0;
String unlockOrder[12] = {"simonsays30", "simonsays31", "simonsays10", "simonsays21", "simonsays10", "simonsays20", "simonsays11"};

void displayCorrectPassword() {
  // start timer 
  correctGuessPlayer->start();
}

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

  topicCount--;
  if (topicCount == 0) {
    clearStartupTimer();
    Serial.println("Subscribing to all topics complete, publishing deviceonline message");

    deviceList(settings->deviceName, WiFi.localIP().toString());
    masterName = settings->deviceName;

    String payload = "{\"deviceId\": \"" + settings->deviceName + "\", \"ip\": \"" + String(WiFi.localIP().toString()) + "\"}";
    PublishMqtt("escaperoom/puzzles/simonsays/deviceonline", (char*)payload.c_str());
  }
}

void subscribeTo(const char* topic) {
  uint16_t packetIdSub = mqttClient.subscribe(topic, 1);
  Serial.print("Subscribing to topic '"); Serial.print(topic); Serial.print("' at QoS 1, packetId: "); Serial.println(packetIdSub);
  topicCount++;
}

void SuscribeMqtt() {
  startupTimer = xTimerCreate("startupdiagnostics", 300, pdTRUE, (void*)123, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);

  String topic = "escaperoom/puzzles/simonsays/" + settings->deviceName + "/";
  subscribeTo(topic.c_str());
  
  subscribeTo("escaperoom/puzzles/startroom");
  subscribeTo("escaperoom/puzzles/simonsays/deviceonline");
  subscribeTo("escaperoom/puzzles/simonsays/currentdevices");
  subscribeTo("escaperoom/puzzles/simonsays/startpuzzle");
  subscribeTo("escaperoom/puzzles/simonsays/deviceoffline");
  subscribeTo("escaperoom/puzzles/simonsays/buttonpushed");
  subscribeTo("escaperoom/puzzles/simonsays/incorrectguess");
  subscribeTo("escaperoom/puzzles/simonsays/puzzlesolved");
  subscribeTo((topic + "0").c_str());
  subscribeTo((topic + "1").c_str());
}

void PublishMqtt(char* topic, char* payload) {
  Serial.print("Publishing to topic '"); Serial.print(topic); Serial.print("' with payload: '"); Serial.print(payload); Serial.println("'");
  mqttClient.publish(topic, 1, false, payload);
}

bool isMaster() {
  return masterName == settings->deviceName;
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
  mqttClient.onMessage(OnMqttReceived);

  mqttClient.setServer("192.168.88.114", 1883);
}

void publishDroneList() {
  auto &publishList = *(new Dictionary());
  publishList("primary", masterName);
  publishList("primaryIp", WiFi.localIP().toString());
  publishList("drones", deviceList.json());

  PublishMqtt("escaperoom/puzzles/simonsays/currentdevices", (char*)publishList.json().c_str());

  delete &publishList;
}

void OnMqttReceived(char* cTopic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
  String topic = (String)cTopic;
  topic.trim();

  String content = ((String)payload).substring(0, len);

  Serial.print("Publish received on topic: "); Serial.println(topic);

  if (topic == "escaperoom/puzzles/simonsays/" + settings->deviceName + "/0") {
    digitalWrite(REDLED_PIN, HIGH);
    ledsOffTime = millis() + 750;
  } else if (topic == "escaperoom/puzzles/simonsays/" + settings->deviceName + "/1") {
    digitalWrite(GREENLED_PIN, HIGH);
    ledsOffTime = millis() + 750;
  } else if (topic == "escaperoom/puzzles/simonsays/deviceonline") {
    auto &newDevice = *(new Dictionary());
    newDevice.jload(content);

    auto newDeviceName = newDevice["deviceId"];
    auto newDeviceIp = newDevice["ip"];

    if (newDeviceName != settings->deviceName) {
      Serial.print("New device online: '"); Serial.print(newDeviceName); Serial.print("' on IP: '"); Serial.print(newDeviceIp); Serial.println("'");

      if (isMaster()) {
        deviceList(newDeviceName, newDeviceIp);

        publishDroneList();
      }
    }
    
    delete &newDevice;
  } else if (topic == "escaperoom/puzzles/simonsays/currentdevices") {
    Serial.print("Before: '"); Serial.print(deviceList.json().c_str()); Serial.println("'");

    auto &deviceInfo = *(new Dictionary());
    deviceInfo.jload(content);

    masterName = deviceInfo["primary"];

    deviceList(deviceInfo["primary"], deviceInfo["primaryIp"]);

    auto &drones = *(new Dictionary());
    drones.jload(deviceInfo["drones"]);

    for (int x = 0; x < drones.count(); x++) {
      deviceList(drones(x), drones[x]);
    }

    Serial.print("After: '"); Serial.print(deviceList.json().c_str()); Serial.println("'");

    delete &deviceInfo;
    delete &drones;
  } else if (topic == "escaperoom/puzzles/simonsays/startpuzzle") {
    if (isMaster()) {
      Serial.println("Starting puzzle, involving these devices:");

      for (int x = 0; x < deviceList.count(); x++) {
        Serial.print("\t'"); Serial.print(deviceList(x)); Serial.print("' on IP: '"); Serial.print(deviceList[x]); Serial.println("'");
      }
    }
  } else if (topic == "escaperoom/puzzles/simonsays/deviceoffline") {
    Serial.println("Removing device");
    Serial.print("Before: '"); Serial.print(deviceList.json().c_str()); Serial.println("'");

    auto &deadDevice = *(new Dictionary());
    deadDevice.jload(content);

    auto deadDeviceName = deadDevice["deviceId"];
    auto deadDeviceIp = deadDevice["ip"];

    deviceList.remove(deadDeviceName);

    delete &deadDevice;

    Serial.print("After: '"); Serial.print(deviceList.json().c_str()); Serial.println("'");
  } else if (topic == "escaperoom/puzzles/simonsays/buttonpushed" && isMaster()) {
    auto &buttonInfo = *(new Dictionary());
    buttonInfo.jload(content);
    
    Serial.print("Button pushed: "); Serial.print(buttonInfo["deviceId"]); Serial.print(" and button "); Serial.println(buttonInfo["buttonNumber"]);
    if (unlockOrder[currentSolveIndex] == buttonInfo["deviceId"] + buttonInfo["buttonNumber"]) {
      Serial.println("Good next guess");
      currentSolveIndex++;

      if (currentSolveIndex >= 7) {
        PublishMqtt("escaperoom/puzzles/simonsays/puzzlesolved", "");
        currentSolveIndex = 0;
      }
    } else {
      Serial.println("Incorrect button pushed!");
      incorrectPassword();
    }

    delete &buttonInfo;
  } else if (topic == "escaperoom/puzzles/simonsays/incorrectguess") {
    digitalWrite(REDLED_PIN, LOW);
    digitalWrite(GREENLED_PIN, LOW);
    
    incorrectGuessTimer.Start();
  } else if (topic == "escaperoom/puzzles/simonsays/puzzlesolved") {
    puzzleSolvedTimer.Start();
  }
}

void incorrectPassword() {
  currentSolveIndex = 0;
  PublishMqtt("escaperoom/puzzles/simonsays/incorrectguess", "");
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

String getSettings() {
  String response = "{\"devicename\": \"";
  response += settings->deviceName;

  response += "\"}";
  return response;
}

void configureUrlRoutes() {
  server.serveStatic("/", SPIFFS, "").setDefaultFile("index.html");

  server.on("/api/resetsettings", HTTP_GET, [](AsyncWebServerRequest * request) {
    Serial.println("Resetting settings->..");
    
    settings->resetSettings();

    request->send(200, "text/json", "OK");
    delay(500);
    ESP.restart();
  });

  server.on("/api/heartbeat", HTTP_GET, [](AsyncWebServerRequest * request) {
    request->send(200, "text/json", "OK");
  });

  server.on("/api/currentsettings", HTTP_GET, [](AsyncWebServerRequest * request) {
    Serial.print("Sending settings->.."); Serial.println("");
    request->send(200, "text/json", getSettings());
  });

  server.onRequestBody([](AsyncWebServerRequest * request, uint8_t *data, size_t len, size_t index, size_t total) {
    Serial.printf("[REQUEST]\t%s\r\n", (const char*)data);
    Serial.print("URL: "); Serial.println(request->url());

    if (request->url() == "/api/savesettings") {
      saveSettings(request, data);
      request->send(200, "text/json", "OK");

      delay(500);
      ESP.restart();
    } else {
      request->send(404);
    }
  });

  server.onFileUpload(onUpload);

  server.onNotFound([](AsyncWebServerRequest *request) {
    request->beginResponse(SPIFFS, "/index.htm");
  });
}

void saveSettings(AsyncWebServerRequest *request, uint8_t *data) {
  Serial.println("Saving settings->..");

  StaticJsonDocument<256> doc;

  DeserializationError error = deserializeJson(doc, (const char *)data, request->contentLength());

  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    return;
  }

  const char* devicename = doc["devicename"];
  const char* settingsSSID = doc["ssid"];
  const char* settingsWifiPassword = doc["wifipassword"];

  settings->deviceName = devicename;
  settings->ssid = settingsSSID;
  settings->wifiPassword = settingsWifiPassword;

  Serial.print("Device name: "); Serial.println(settings->deviceName);

  settings->saveCurrentSettings();
}

// Upload files by using curl:
// curl -F 'data=@index.html' http://192.168.88.36/api/upload
// curl -F 'data=@index.html' http://192.168.4.1/api/upload
void onUpload(AsyncWebServerRequest *request, String filename, size_t index, uint8_t *data, size_t len, bool final) {
  if (request->url() == "/api/upload") {
    static unsigned long startTimer;
    if (!index) {
      startTimer = millis();
      request->_tempFile = SPIFFS.open("/" + filename, "w");
      const char* FILESIZE_HEADER{"FileSize"};
  
      Serial.printf("UPLOAD: Receiving: '%s'\n", filename.c_str());
    }
  
    if (len) {
      request->_tempFile.write(data, len);
    }

    if (final) {
      request->_tempFile.close();
      Serial.printf("UPLOAD: Done. Received %i bytes in %.2fs which is %.2f kB/s.\n", index + len, (millis() - startTimer) / 1000.0, 1.0 * (index + len) / (millis() - startTimer));
    }
  } else {
    request->send(404);
  }
}

void checkIfDevicesAreResponding() {
  for (int x = 0; x < deviceList.count(); x++) {
    if (deviceList(x) != settings->deviceName) {
      HTTPClient http;
      http.begin("http://" + deviceList[x] + "/api/heartbeat");

      int httpCode = http.GET();
      if (httpCode <= 0 && deviceList(x) != "" && deviceList[x] != "") {
        String payload = "{\"deviceId\": \"" + deviceList(x) + "\", \"ip\": \"" + deviceList[x] + "\"}";
        PublishMqtt("escaperoom/puzzles/simonsays/deviceoffline", (char*)payload.c_str());

        if (masterName == deviceList(x)) {
          Serial.println("Dead device was previous master. Assuming master role.");
          masterName = settings->deviceName;

          publishDroneList();
        }
      }

      http.end();
    }
  }
}

void setup() {
  pinMode(LEFT_BUTTON_PIN, INPUT_PULLDOWN);
  pinMode(RIGHT_BUTTON_PIN, INPUT_PULLDOWN);

  pinMode(REDLED_PIN, OUTPUT);
  pinMode(GREENLED_PIN, OUTPUT);

  Serial.begin(115200);
  while (!Serial) { }

  Serial.println("Starting up...");

  settings->loadDeviceSettings();

  startupTimer = xTimerCreate("startupdiagnostics", 100, pdTRUE, (void*)123, reinterpret_cast<TimerCallbackFunction_t>(diganosticsTimerCallback));
  xTimerStart(startupTimer, 0);

  if (wifiManager.ConnectToWifi(WiFiEvent)) {
    Serial.println("Wifi connected");
    configureUrlRoutes();
    server.begin();
    clearStartupTimer();
  } else {
    Serial.println("Couldn't connect to wifi");
  }

  InitMqtt();
  ConnectToMqtt();

  deviceHeartbeatTimer.Start();
}

long debounceTimer;
void loop() {
  wifiManager.wifi_loop();
  correctGuessPlayer->playPassword_loop();

/*  if (deviceHeartbeatTimer.Check()) {
    checkIfDevicesAreResponding();
  }*/

  if (incorrectGuessTimer.IsRunning()) {
    digitalWrite(REDLED_PIN, (int)floor(millis() / 100.0) % 2 == 0);
  } else if (puzzleSolvedTimer.IsRunning()) {
    digitalWrite(GREENLED_PIN, (int)floor(millis() / 100.0) % 2 == 0);
  } else if (millis() > ledsOffTime) {
    digitalWrite(REDLED_PIN, LOW);
    digitalWrite(GREENLED_PIN, LOW);
  }

  if (puzzleSolvedTimer.Check()) {
    puzzleSolvedTimer.Stop();
  }

  if (incorrectGuessTimer.Check()) {
    incorrectGuessTimer.Stop();

    if (isMaster()) {
      displayCorrectPassword();
    }
  }

  if (digitalRead(LEFT_BUTTON_PIN) && debounceTimer <= millis()) {
    debounceTimer = millis() + 350;
    digitalWrite(REDLED_PIN, HIGH);
    ledsOffTime = debounceTimer;
    
    String payload = "{\"deviceId\": \"" + settings->deviceName + "\", \"buttonNumber\": \"0\"}";
    PublishMqtt("escaperoom/puzzles/simonsays/buttonpushed", (char*)payload.c_str());
  }

  if (digitalRead(RIGHT_BUTTON_PIN) && debounceTimer <= millis()) {
    debounceTimer = millis() + 350;
    digitalWrite(GREENLED_PIN, HIGH);
    ledsOffTime = debounceTimer;
    
    String payload = "{\"deviceId\": \"" + settings->deviceName + "\", \"buttonNumber\": \"1\"}";
    PublishMqtt("escaperoom/puzzles/simonsays/buttonpushed", (char*)payload.c_str());
  }
}
