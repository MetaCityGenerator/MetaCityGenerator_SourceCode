// MetaCityGenerator.Native.h : main header file for the MetaCityGenerator.Native DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'pch.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CMetaCityGeneratorNativeApp
// See MetaCityGenerator.Native.cpp for the implementation of this class
//

class CMetaCityGeneratorNativeApp : public CWinApp
{
public:
	CMetaCityGeneratorNativeApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
