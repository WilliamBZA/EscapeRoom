#ifndef playcorrectpasswordtimer_h_
#define playcorrectpasswordtimer_h_

#include <AsyncMqttClient.h>

class PlayCorrectPasswordTimer {
  public:
    PlayCorrectPasswordTimer(void (*publishFunction)(int lightNum));

    void playPassword_loop();
    void start();
    
  protected:
    long lastTriggerTime;
    int step;
    void (*publishFunctionCallback)(int lightNum);
};

#endif // calibrationtimer_h_
