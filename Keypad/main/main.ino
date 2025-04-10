#include <Key.h>
#include <Keypad.h>

const byte ROWS = 4;
const byte COLS = 3;

char keys[ROWS][COLS] = {
  {'1','2','3'},
  {'4','5','6'},
  {'7','8','9'},
  {'*','0','#'}
};

byte rowPins[ROWS] = {5, 4, 0, 2};
byte colPins[COLS] = {16, 14, 12};

int ledPin = 15;
int signalPin = 13;

long debounceTimer;
long ledOffTime;
int currentPasswordIndex = 0;
int passwordLength = 3;
char password[9] = {"358"};
bool solved = false;

Keypad keypad = Keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS);

void setup() {
  Serial.begin(115200);
  while (!Serial) { }

  pinMode(ledPin, OUTPUT);
  pinMode(signalPin, OUTPUT);

  digitalWrite(signalPin, LOW);
  digitalWrite(ledPin, HIGH);
}

void loop() {
  if (millis() >= ledOffTime) {
    digitalWrite(ledPin, LOW);
  }
  
  char key = keypad.getKey();

  if (!solved && key && debounceTimer <= millis()) {
    debounceTimer = millis() + 500;
    ledOffTime = millis() + 250;

    digitalWrite(ledPin, HIGH);
    
    Serial.println(key);

    if (key != password[currentPasswordIndex]) {
      currentPasswordIndex = 0;
      Serial.println("Wrong password. Starting again");
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
      digitalWrite(ledPin, HIGH);
      delay(250);
      digitalWrite(ledPin, LOW);
      delay(250);
    } else {
      // digitalWrite(GREENLED_PIN, HIGH);
      currentPasswordIndex++;

      if (currentPasswordIndex == passwordLength) {
        currentPasswordIndex = 0;

        // Password correct
        Serial.println("Yay!");
        solved = true;
        digitalWrite(signalPin, HIGH);
      }
    }
  }
}
