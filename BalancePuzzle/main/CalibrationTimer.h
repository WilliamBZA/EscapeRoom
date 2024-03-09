#ifndef calibrationtimer_h_
#define calibrationtimer_h_

#include <FastLED.h>
#include "Timer.h"
#include "MPU6050_6Axis_MotionApps20.h"
#include "settings.h"

class CalibrationTimer : public Timer {
  public:
    CalibrationTimer(int numberOfTargets, MPU6050 mpu, Settings* settings, void (*displayFunction)(int numberOfLights, CRGB colour));

    void calibration_loop();
    
  protected:
    int numberOfTargets;
    void (*displayFunctionCallback)(int numberOfLights, CRGB colour);
    MPU6050 mpu;
    Settings* settings;
};

#endif // calibrationtimer_h_
