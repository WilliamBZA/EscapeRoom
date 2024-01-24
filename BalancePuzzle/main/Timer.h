#ifndef Timer_h
#define Timer_h

#include <Arduino.h>

class Timer {
  public:
    Timer(long interval);
    boolean Check();
    boolean IsRunning();
    void Start();
    void Stop();

  private:
    long lastTriggerTime;
    long triggerInterval;
};

#endif
