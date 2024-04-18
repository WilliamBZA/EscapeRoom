#include "PlayCorrectPasswordTimer.h"

PlayCorrectPasswordTimer::PlayCorrectPasswordTimer(void (*publishFunction)(char* topic, char* payload)) {
  publishFunctionCallback = publishFunction;
  lastTriggerTime = -1;
  step = 1;
}

void PlayCorrectPasswordTimer::start() {
  lastTriggerTime = millis();

  step = 1;
  publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays3/0", "");
}

// {"simonsays30", "simonsays31", "simonsays10", "simonsays21", "simonsays10", "simonsays20", "simonsays11"};, "simonsays30", "simonsays10", "simonsays11", "simonsays21", "simonsays10"};
void PlayCorrectPasswordTimer::playPassword_loop() {
  if (lastTriggerTime != -1) {
    if (millis() - lastTriggerTime >= 1000) {
      switch (step) {
        case 1:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays3/1", "");
          break;
  
        case 2:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/0", "");
          break;
  
        case 3:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays2/1", "");
          break;
  
        case 4:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/0", "");
          break;
  
        case 5:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays2/0", "");
          break;
  
        case 6:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/1", "");
          break;
  
        /*case 7:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays3/0", "");
          break;
  
        case 8:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/0", "");
          break;
  
        case 9:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/1", "");
          break;
  
        case 10:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays2/1", "");
          break;
  
        case 11:
          publishFunctionCallback("escaperoom/puzzles/simonsays/simonsays1/0", "");
          break;*/
  
        default:
          lastTriggerTime = -1;
          break;
      }
  
      step++;
      lastTriggerTime = millis();
    }
  }
}
