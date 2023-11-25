// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// add headers that you want to pre-compile here
#include "framework.h"

// Windows build
#if defined (_WIN32)
#if defined (URBANXNATIVE_DLL_EXPORTS)
#define URBANXNATIVE_CPP_CLASS __declspec(dllexport)
#define URBANXNATIVE_CPP_FUNCTION __declspec(dllexport)
#define URBANXNATIVE_C_FUNCTION extern "C" __declspec(dllexport)
#else
#define URBANXNATIVE_CPP_CLASS __declspec(dllimport)
#define URBANXNATIVE_CPP_FUNCTION __declspec(dllimport)
#define URBANXNATIVE_C_FUNCTION extern "C" __declspec(dllimport)
#endif // URBANXNATIVE_DLL_EXPORTS
#endif // _WIN32

// Apple build
#if defined(__APPLE__)
#define URBANXNATIVE_CPP_CLASS __attribute__ ((visibility ("default")))
#define URBANXNATIVE_CPP_FUNCTION __attribute__ ((visibility ("default")))
#define URBANXNATIVE_C_FUNCTION extern "C" __attribute__ ((visibility ("default")))
#endif // __APPLE__

//EMBREE Library
#include <embree3/rtcore.h>
#include <limits>
#include <iostream>
#include <vector>
#include <memory>
#include <cassert>
#include <algorithm>

#endif //PCH_H
