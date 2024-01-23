#include <FS.h>
#include "filemanager.h"

#if defined(ESP32)
#include <SPIFFS.h>
#endif

FileManager::FileManager() {
  SPIFFS.begin();
}

File FileManager::getFile(String path) {
  return SPIFFS.open(path.c_str(), "r");
}
