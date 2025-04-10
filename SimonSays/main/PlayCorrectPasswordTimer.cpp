#include "PlayCorrectPasswordTimer.h"

PlayCorrectPasswordTimer::PlayCorrectPasswordTimer(void (*publishFunction)(int lightNum)) {
  publishFunctionCallback = publishFunction;
  lastTriggerTime = -1;
  step = 1;
}

void PlayCorrectPasswordTimer::start() {
  lastTriggerTime = millis();

  step = 0;
}

// "simonsays30", "simonsays31", "simonsays20", "simonsays31", "simonsays20", "simonsays30", "simonsays21"
void PlayCorrectPasswordTimer::playPassword_loop() {
  if (lastTriggerTime != -1) {
    if (millis() - lastTriggerTime >= 1000) {
      switch (step) {
        case 0:
          publishFunctionCallback(0);
          break;

        case 1:
          publishFunctionCallback(1);
          break;
  
        case 2:
          publishFunctionCallback(0);
          break;
  
        case 3:
          publishFunctionCallback(1);
          break;
  
        case 4:
          publishFunctionCallback(0);
          break;
  
        case 5:
          publishFunctionCallback(0);
          break;
  
        case 6:
          publishFunctionCallback(1);
          break;
  
        case 7:
          publishFunctionCallback(0);
          break;
  
        case 8:
          publishFunctionCallback(0);
          break;
  
        case 9:
          publishFunctionCallback(1);
          break;
  
        case 10:
          publishFunctionCallback(0);
          break;
  
        case 11:
          publishFunctionCallback(0);
          break;

        case 12:
          publishFunctionCallback(1);
          break;
  
        case 13:
          publishFunctionCallback(1);
          break;
  
        case 14:
          publishFunctionCallback(0);
          break;
  
        default:
          lastTriggerTime = -1;
          break;
      }
  
      step++;
      lastTriggerTime = millis();
    }
  }
}
