#pragma once

#include <cstdio>
#include <embree3/rtcore.h>

namespace MetaCitynative {
	inline void errorFunction(void* userPtr, enum RTCError error, const char* str) {
		printf("error %d: %s\n", error, str);
	}

    inline RTCDevice initializeDevice() {
        RTCDevice device = rtcNewDevice(NULL);

        if (!device)
            printf("error %d: cannot create Embree device\n", rtcGetDeviceError(NULL));

        rtcSetDeviceErrorFunction(device, errorFunction, NULL);
        return device;
    }
}