#include <WiFiManager.h>
#define WEBSERVER_H
#include <ESPAsyncWebServer.h>
#include <ArduinoOTA.h>
#include <FS.h>
#include <ArduinoJson.h>
#include <AsyncJson.h>
#include <DNSServer.h>
#include "time.h"
#include "Timer.h"
#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps20.h"

#include <FastLED.h>

#define LED_COUNT 82
#define LED_PIN 14

CRGB leds[LED_COUNT];

#if defined(ESP8266)
#include <ESP8266mDNS.h>
#endif

#if defined(ESP32)
#include <SPIFFS.h>
#include <ESPmDNS.h>
#endif

#define INTERRUPT_PIN 15 // use pin 15 on ESP8266
bool dmpReady = false;  // set true if DMP init was successful

const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 7200;

DNSServer dnsServer;
AsyncWebServer server(80);

String deviceName = "LevelPuzzle";
String ssid = "";
String wifiPassword = "";
bool isConnected = false;

int degree = 0;

int brightness = 100;
int colour = 0xFFFFFF;

MPU6050 mpu;

int oldDegree = -1;
int outterDegreeOffset = 8; // 3, 10
int middleDegreeOffset = 3;
int innerDegreeOffset = 0;

double crossDegreeOverlap = 15 / 2.0;
int crossDegreeOffset = 10;
int crossLeftDegree = 90;
int crossBottomDegree = 180;
int crossRightDegree = 270;
int crossTopDegree = 360;

// Total: 82                 4
// Outer ring: 33        3  2/5  1
// Second ring: 25           6
// Inner ring: 18
// Cross: 6

int outerRing = 33;
int secondRing = 25;
int innerRing = 18;
int cross = 6;

int degreesFarFromZero = 45; // the tilt away
int maxDegreesFromZero = 45;
int minDegreesFromZero = 5;

bool connectToWifi() {
  WiFi.disconnect();
  WiFi.mode(WIFI_OFF);

  if (ssid != "") {
    Serial.println("Using saved SSID and Password to attempt WiFi Connection.");
    Serial.print("Saved SSID is "); Serial.println(ssid);
    Serial.print("Saved Password is "); Serial.println(wifiPassword);

    WiFi.mode(WIFI_STA);
    Serial.println("\nConnecting to WiFi Network ..");
    WiFi.begin(ssid, wifiPassword);

    int attempt = 0;
    while(WiFi.status() != WL_CONNECTED){
      attempt++;
      Serial.print(".");
      delay(100);

      if (attempt++ >= 200) {
        WiFi.disconnect();
        return false;
      }
    }

    Serial.println("\nConnected to the WiFi network");
    Serial.print("Local IP: ");
    Serial.println(WiFi.localIP());
    isConnected = true;
    return true;
  }

  return false;
}

void configureCaptivePortal() {
  Serial.println("Starting captive portal");

  char portalSSID[23];
  snprintf(portalSSID, 23, "smartlights-%X", ESP.getChipId());
  Serial.print("SSID will be: "); Serial.println(portalSSID);
  
  WiFi.mode(WIFI_AP);
  WiFi.softAP(portalSSID);
  Serial.print("AP IP address: ");Serial.println(WiFi.softAPIP());
    
  Serial.println("Starting DNS Server");
  dnsServer.start(53, "*", WiFi.softAPIP());
}

void configureOTA() {
  // Make sure the flash size matches. For ESP8266 12F it should be 4MB.
  ArduinoOTA.setHostname(deviceName.c_str());

  ArduinoOTA.onStart([]() {
    String type;
    if (ArduinoOTA.getCommand() == U_FLASH) {
      type = "sketch";
    } else {
      // U_SPIFFS
      type = "filesystem";
    }

    // NOTE: if updating SPIFFS this would be the place to unmount SPIFFS using SPIFFS.end()
    Serial.println("Start updating " + type);
  });

  ArduinoOTA.onEnd([]() {
  });

  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
  });

  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("End Failed");
  });

  ArduinoOTA.begin();
}

String getSettings() {
  String settings = "{\"devicename\": \"";
  settings += deviceName;

  if (isConnected) { // only get the time if connected to the internet
    settings += "\", \"deviceTime\": \"";
    settings += getLocalTime();
  }

  settings += "\"}";
  return settings;
}

String valueProcessor(const String& var) {
  Serial.print("Var: "); Serial.println(var);

  if (var == "DEVICE_NAME") {
    return deviceName;
  }

  if (var == "START_SETTINGS") {
    return getSettings();
  }

  return var;
}

void configureUrlRoutes() {
  server.serveStatic("/", SPIFFS, "").setTemplateProcessor(valueProcessor).setDefaultFile("index.html");

  server.on("/api/resetsettings", HTTP_GET, [](AsyncWebServerRequest * request) {
    Serial.print("Resetting settings..."); Serial.println("");
    
    ssid = "";
    wifiPassword = "";
    deviceName = "";

    saveCurrentSettings();

    request->send(200, "text/json", "OK");
    delay(500);
    ESP.restart();
  });

  server.on("/api/currentsettings", HTTP_GET, [](AsyncWebServerRequest * request) {
    Serial.print("Sending settings..."); Serial.println("");
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
    } else if (request->url() == "/api/setcolour") {
      setColour(request, data);
      request->send(200, "text/json", "OK");
    } else if (request->url() == "calibrate") {
      calibrateMPU();
      request->send(200, "text/json", "OK");
    } else {
      request->send(404);
    }
  });

  server.onFileUpload(onUpload);

  server.onNotFound([](AsyncWebServerRequest *request) {
    request->beginResponse(SPIFFS, "/index.htm");
  });
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

void setColour(AsyncWebServerRequest *request, uint8_t *data) {
  Serial.println("Setting colour...");

  StaticJsonDocument<256> doc;

  DeserializationError error = deserializeJson(doc, (const char *)data, request->contentLength());

  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    return;
  }

  Serial.println("Extracting colours");
  
  int receivedBrightness = doc["brightness"];
  Serial.print("Brightness: "); Serial.println(receivedBrightness);
  int receivedColour = doc["colour"];
  Serial.print("Colour: "); Serial.println(receivedColour);
  
  brightness = receivedBrightness;
  colour = receivedColour;


  saveCurrentSettings();
}

void saveSettings(AsyncWebServerRequest *request, uint8_t *data) {
  Serial.println("Saving settings...");

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

  deviceName = devicename;
  ssid = settingsSSID;
  wifiPassword = settingsWifiPassword;

  Serial.print("Device name: "); Serial.println(deviceName);

  saveCurrentSettings();
}

void saveCurrentSettings() {
  File settingsFile = SPIFFS.open("/settings.json", "w");
  StaticJsonDocument<1024> settingsDoc;
  
  settingsDoc["devicename"] = deviceName;
  settingsDoc["ssid"] = ssid;
  settingsDoc["wifipassword"] = wifiPassword;
  settingsDoc["colour"] = colour;
  settingsDoc["brightness"] = brightness;

  if (serializeJson(settingsDoc, settingsFile) == 0) {
    Serial.println("Failed to write to file");
  }

  settingsFile.close();
}

void loadDeviceSettings() {
  File settingsFile = SPIFFS.open("/settings.json", "r");
  if (!settingsFile) {
    Serial.println("No settings file found");
    deviceName = "UnknownDevice";
    return;
  }

  StaticJsonDocument<384> doc;

  DeserializationError error = deserializeJson(doc, settingsFile);
  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    return;
  }

  const char* devicename = doc["devicename"];
  const char* settingsSSID = doc["ssid"];
  const char* settingsWifiPassword = doc["wifipassword"];

  deviceName = devicename;
  ssid = settingsSSID;
  wifiPassword = settingsWifiPassword;

  Serial.print("Device name: "); Serial.println(deviceName);

  settingsFile.close();
}

String getLocalTime() {
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    Serial.println("Failed to obtain time");
    return "00:00";
  }

  char timeHour[6];
  strftime(timeHour, 6, "%R", &timeinfo);
  Serial.print("Hour: ");
  Serial.println(timeHour);
  return timeHour;
}

void setup() {
  Serial.begin(115200);

  SPIFFS.begin();

  FastLED.addLeds<NEOPIXEL, LED_PIN>(leds, LED_COUNT);
  FastLED.setBrightness(10);

  loadDeviceSettings();

  if (connectToWifi()) {
    String dnsName = deviceName;
    dnsName.replace(" ", "");
    if (!MDNS.begin(dnsName)) {
      Serial.println("Error starting mDNS");
    }

    configTime(gmtOffset_sec, 0, ntpServer);
  } else {
    configureCaptivePortal();
  }

  configureUrlRoutes();
  configureOTA();

  mpu_setup();

  server.begin();
  Serial.println("Setup complete");
}

volatile bool mpuInterrupt = false;     // indicates whether MPU interrupt pin has gone high
void ICACHE_RAM_ATTR dmpDataReady() {
    mpuInterrupt = true;
}

void mpu_setup() {
  uint8_t devStatus;      // return status after each device operation (0 = success, !0 = error)
  uint8_t mpuIntStatus;   // holds actual interrupt status byte from MPU

  // join I2C bus (I2Cdev library doesn't do this automatically)
#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
  Wire.begin();
  Wire.setClock(400000); // 400kHz I2C clock. Comment this line if having compilation difficulties
#elif I2CDEV_IMPLEMENTATION == I2CDEV_BUILTIN_FASTWIRE
  Fastwire::setup(400, true);
#endif

  // initialize device
  Serial.println(F("Initializing I2C devices..."));
  mpu.initialize();
  pinMode(INTERRUPT_PIN, INPUT);

  // verify connection
  Serial.println(F("Testing device connections..."));
  Serial.println(mpu.testConnection() ? F("MPU6050 connection successful") : F("MPU6050 connection failed"));

  // load and configure the DMP
  Serial.println(F("Initializing DMP..."));
  devStatus = mpu.dmpInitialize();

  // supply your own gyro offsets here, scaled for min sensitivity
  mpu.setXGyroOffset(220);
  mpu.setYGyroOffset(76);
  mpu.setZGyroOffset(-85);
  mpu.setZAccelOffset(1788); // 1688 factory default for my test chip

  // make sure it worked (returns 0 if so)
  if (devStatus == 0) {
    // turn on the DMP, now that it's ready
    Serial.println(F("Enabling DMP..."));
    mpu.setDMPEnabled(true);

    // enable Arduino interrupt detection
    Serial.println(F("Enabling interrupt detection (Arduino external interrupt 0)..."));
    attachInterrupt(digitalPinToInterrupt(INTERRUPT_PIN), dmpDataReady, RISING);
    mpuIntStatus = mpu.getIntStatus();

    // set our DMP Ready flag so the main loop() function knows it's okay to use it
    Serial.println(F("DMP ready! Waiting for first interrupt..."));
    dmpReady = true;
  } else {
    // ERROR!
    // 1 = initial memory load failed
    // 2 = DMP configuration updates failed
    // (if it's going to break, usually the code will be 1)
    Serial.print(F("DMP Initialization failed (code "));
    Serial.print(devStatus);
    Serial.println(F(")"));
  }
}

// Modified version of Adafruit BN0555 library to convert Quaternion to world angles the way we need
// The math is a little different here compared to Adafruit's version to work the way I needed for this project
VectorFloat QtoEulerAngle(Quaternion qt) {
  VectorFloat ret;
  double sqw = qt.w * qt.w;
  double sqx = qt.x * qt.x;
  double sqy = qt.y * qt.y;
  double sqz = qt.z * qt.z;

  ret.x = atan2(2.0 * (qt.x * qt.y + qt.z * qt.w), (sqx - sqy - sqz + sqw));
  ret.y = asin(2.0 * (qt.x * qt.z - qt.y * qt.w) / (sqx + sqy + sqz + sqw));  //Adafruit uses -2.0 *..
  ret.z = atan2( 2.0 * (qt.y * qt.z + qt.x * qt.w), (-sqx - sqy + sqz + sqw));

  // Added to convert Radian to Degrees
  ret.x = ret.x * 180 / PI;
  ret.y = ret.y * 180 / PI;
  ret.z = ret.z * 180 / PI;
  
  return ret;
}

void mpu_loop() {
  Quaternion quaternion;           // [w, x, y, z]         quaternion container
  uint8_t fifoBuffer[64]; // FIFO storage buffer
  
  // if programming failed, don't try to do anything
  if (!dmpReady) return;

  // Get the Quaternion values from DMP buffer
  if (mpu.dmpGetCurrentFIFOPacket(fifoBuffer)) {
    mpu.dmpGetQuaternion(&quaternion, fifoBuffer);

    // Calc angles converting Quaternion to Euler this was giving more stable acurate results compared to
    // getting Euler directly from DMP. I think Quaternion conversion takes care of gimble lock.
    VectorFloat ea = QtoEulerAngle(quaternion);

    double x = cos(ea.y * PI / 180) * cos(ea.z * PI / 180);
    double z = sin(ea.y * PI / 180) * cos(ea.y * PI / 180);
    double y = sin(ea.z * PI / 180);

    degreesFarFromZero = 300 * (y*y + z*z);
    degree = round(90 + atan(y / z) * 180 / PI);

    if (z < 0) {
      degree += 180;
    }
  }
}

bool mpuCalibrated = false;
void calibrateMPU() {
  mpu.CalibrateAccel(6);
  mpu.CalibrateGyro(6);

  mpuCalibrated = true;

  Serial.println("Calibration complete.");
}

int CalculateOffset(int currentLightNumber, int numberOfLightsInRing, int ringStartCount, int distanceOffCurrentLight) {
    if (numberOfLightsInRing < 0) {
        distanceOffCurrentLight = 0 - distanceOffCurrentLight;
        numberOfLightsInRing = 0 - numberOfLightsInRing;
    }

    int offset = currentLightNumber - ringStartCount + distanceOffCurrentLight;
    if (offset < 0) {
        offset += numberOfLightsInRing;
    }

    if (offset == 0) {
        return ringStartCount;
    }

    return offset % numberOfLightsInRing + ringStartCount;
}

void loop() {
  if (!mpuCalibrated) {
    calibrateMPU();
  }
  
  ArduinoOTA.handle();

  bool centerLightOn = false;
  //degree = (millis() / 20) % 360;

  if (degree != oldDegree) {
    oldDegree = degree;
  

  for (int x = 0; x < LED_COUNT; x++) {
    leds[x] = CRGB::Black;
  }

  // 45 = 3 lights; one on each side
  // 5  = half lights - 2;
  degreesFarFromZero = max(min(degreesFarFromZero, maxDegreesFromZero), minDegreesFromZero); // clamp to [5..45]

  float lightsOnRatio = abs(1.0 * (degreesFarFromZero - minDegreesFromZero - maxDegreesFromZero) / (maxDegreesFromZero - minDegreesFromZero));

  int numberOfOuterLightsOn = round(lightsOnRatio * (outerRing - 2) / 2) + 2;
  int numberOfSecondLightsOn = round(lightsOnRatio * (secondRing - 2) / 2);
  int numberOfInnerLightsOn = round(lightsOnRatio * (innerRing - 2) / 2);
  
  int outerCenter = (int)floor(33 * ((degree + outterDegreeOffset) % 360) / 360.0);
  int secondCenter = 33 + (int)floor(25 * ((degree + middleDegreeOffset) % 360) / 360.0);
  int innerCenter = 33 + 25 + 17 - (int)floor(18 * ((degree + innerDegreeOffset) % 360) / 360.0);

  for (int x = 1; x <= numberOfOuterLightsOn / 2; x++) {
    leds[CalculateOffset(outerCenter, 33, 0, x)] = CHSV(160, 255, 255 * max(1.0 / numberOfOuterLightsOn, (numberOfOuterLightsOn - x * 2.0) / numberOfOuterLightsOn));
    leds[CalculateOffset(outerCenter, 33, 0, -x)] = CHSV(160, 255, 255 * max(1.0 / numberOfOuterLightsOn, (numberOfOuterLightsOn - x * 2.0) / numberOfOuterLightsOn));
  }

  for (int x = 1; x <= numberOfSecondLightsOn / 2; x++) {
    leds[CalculateOffset(secondCenter, 25, 33, x)] = CHSV(0, 255, 255 * max(1.0 / numberOfSecondLightsOn, (numberOfSecondLightsOn - x * 2.0) / numberOfSecondLightsOn));
    leds[CalculateOffset(secondCenter, 25, 33, -x)] = CHSV(0, 255, 255 * max(1.0 / numberOfSecondLightsOn, (numberOfSecondLightsOn - x * 2.0) / numberOfSecondLightsOn));
  }

  for (int x = 1; x <= numberOfInnerLightsOn / 2; x++) {
    leds[CalculateOffset(innerCenter, -18, 58, x)] = CHSV(96, 255, 255 * max(1.0 / numberOfInnerLightsOn, (numberOfInnerLightsOn - x * 2.0) / numberOfInnerLightsOn));
    leds[CalculateOffset(innerCenter, -18, 58, -x)] = CHSV(96, 255, 255 * max(1.0 / numberOfInnerLightsOn, (numberOfInnerLightsOn - x * 2.0) / numberOfInnerLightsOn));
  }

  leds[outerCenter] = CHSV(160, 255, 255); // outer ring
  leds[secondCenter] = CHSV(0, 255, 255); // middle ring
  leds[innerCenter] = CHSV(96, 255, 255); // inner ring

/*  Cross light order
 *      4
 *   3 2/5 1
 *      6
 */

  // Across, 90 --> 180
  leds[33 + 25 + 17 + 3] = (abs(degree + crossDegreeOffset - crossLeftDegree) < crossDegreeOverlap) ? CHSV(224, 255, 255) : CHSV(224, 0, 0); // 90 degrees, left
  // leds[33 + 25 + 17 + 2] = CHSV(224, 255, 255); // center, underneath
  leds[33 + 25 + 17 + 1] = (abs(degree + crossDegreeOffset - crossRightDegree) < crossDegreeOverlap) ? CHSV(224, 255, 255) : CHSV(224, 0, 0); // 180 degrees, right

  // Vertical, 0 --> 180
  leds[33 + 25 + 17 + 4] = (abs((degree + crossDegreeOffset - crossTopDegree) % 360) < crossDegreeOverlap) ? CHSV(224, 255, 255) : CHSV(224, 0, 0); // 0 degrees, top
  leds[33 + 25 + 17 + 6] = (abs(degree + crossDegreeOffset - crossBottomDegree) < crossDegreeOverlap) ? CHSV(224, 255, 255) : CHSV(224, 0, 0); // 180 degrees, bottom

  centerLightOn = (abs((degree + crossDegreeOffset - crossTopDegree) % 360) < crossDegreeOverlap) || (abs(degree + crossDegreeOffset - crossBottomDegree) < crossDegreeOverlap) || 
                  (abs(degree + crossDegreeOffset - crossLeftDegree) < crossDegreeOverlap) || (abs(degree + crossDegreeOffset - crossRightDegree) < crossDegreeOverlap);

  if (centerLightOn) {
    leds[33 + 25 + 17 + 5] = CHSV(224, 255, 255); // center
  }

  FastLED.show();
  }

  dnsServer.processNextRequest();
  mpu_loop();
}
