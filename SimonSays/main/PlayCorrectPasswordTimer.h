#ifndef playcorrectpasswordtimer_h_
#define playcorrectpasswordtimer_h_

#include <AsyncMqttClient.h>

class PlayCorrectPasswordTimer {
  public:
    PlayCorrectPasswordTimer(void (*publishFunction)(char* topic, char* payload));

    void playPassword_loop();
    void start();
    
  protected:
    long lastTriggerTime;
    int step;
    void (*publishFunctionCallback)(char* topic, char* payload);
};

#endif // calibrationtimer_h_
