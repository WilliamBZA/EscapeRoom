#ifndef FILEMANAGER_h
#define FILEMANAGER_h

#include <FS.h>

#if defined(ESP32)
#include <SPIFFS.h>
#endif

class FileManager {
  public:
    FileManager();

    File getFile(String path);

  private:
  
};

#endif
