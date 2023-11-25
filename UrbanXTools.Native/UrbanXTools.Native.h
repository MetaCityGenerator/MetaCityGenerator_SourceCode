// UrbanXTools.Native.h : main header file for the UrbanXTools.Native DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'pch.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CUrbanXToolsNativeApp
// See UrbanXTools.Native.cpp for the implementation of this class
//

class CUrbanXToolsNativeApp : public CWinApp
{
public:
	CUrbanXToolsNativeApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
