#include <Arduino.h>
#include "Timer.h"

Timer::Timer(long interval) {
  lastTriggerTime = -1;
  triggerInterval = interval;
}

boolean Timer:: Check() {
  if (IsRunning() && (millis() - lastTriggerTime >= triggerInterval)) {
    lastTriggerTime = millis();
    return true;
  }

  return false;
}

boolean Timer::IsRunning() {
  return lastTriggerTime != -1;
}

void Timer::Start() {
  lastTriggerTime = millis();
}

void Timer::Stop() {
  lastTriggerTime = -1;
}
