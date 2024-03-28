#include <Keypad.h>
#include "settings.h"
#include "WifiConnectionManager.h"
#include "tones.h"
#include "Timer.h"

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
int tonePasswordLength = 8;
long debounceTimer;
int currentPasswordIndex = 0;

char tonePassword[9] = {"55497349"};

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
