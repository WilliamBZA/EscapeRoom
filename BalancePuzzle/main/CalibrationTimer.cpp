#include "CalibrationTimer.h"
#include "Timer.h";
#include "MPU6050_6Axis_MotionApps20.h"

Timer countdownTimer(3500);
Timer putTheDeviceDownTimer(10000);
int currentTargetNumber = -1;

CalibrationTimer::CalibrationTimer(int targets, MPU6050 mpuInstance, Settings* settingsFile, void (*displayFunction)(int numberOfLights, CRGB colour)) : Timer(10000) {
  numberOfTargets = targets;
  displayFunctionCallback = displayFunction;
  mpu = mpuInstance;
  settings = settingsFile;
}

void CalibrationTimer::calibration_loop() {
  if (currentTargetNumber == -1) {
    currentTargetNumber = 0;

    countdownTimer.Start();
  }

  if (putTheDeviceDownTimer.IsRunning()) {
    if (putTheDeviceDownTimer.Check()) {
      putTheDeviceDownTimer.Stop();
      Stop();
    } else {
      displayFunctionCallback(putTheDeviceDownTimer.GetCurrentProgress() * 100 / putTheDeviceDownTimer.GetTriggerInterval(), CRGB::Green);
    }
  } else if (!countdownTimer.Check()) {
    if (countdownTimer.IsRunning()) {
      displayFunctionCallback(countdownTimer.GetCurrentProgress() * 100 / countdownTimer.GetTriggerInterval(), CRGB::White);
    }
  } else {
    mpu.CalibrateAccel(6);
    mpu.CalibrateGyro(6);

    auto xGyroOffset = mpu.getXGyroOffset();
    auto yGyroOffset = mpu.getYGyroOffset();
    auto zGyroOffset = mpu.getZGyroOffset();
    auto xAccelOffset = mpu.getXAccelOffset();
    auto yAccelOffset = mpu.getYAccelOffset();
    auto zAccelOffset = mpu.getZAccelOffset();

    settings->saveCalibration(currentTargetNumber, xGyroOffset, yGyroOffset, zGyroOffset, xAccelOffset, yAccelOffset, zAccelOffset);
    
    countdownTimer.Start();

    if (++currentTargetNumber >= numberOfTargets) {
      countdownTimer.Stop();
      putTheDeviceDownTimer.Start();
    }
  }
}
