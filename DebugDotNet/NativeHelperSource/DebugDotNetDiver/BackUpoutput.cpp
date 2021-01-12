#include <windows.h>
#include <iostream>
#include <sstream>
#include "stdafx.h"

using namespace std;
// The Below typedef and variable receive a pointer to the original function of IsDebuggerPresent
typedef BOOL (WINAPI* IsDebuggerPresent_PtrType)  ();
IsDebuggerPresent_PtrType IsDebuggerPresent_Ptr;
BOOL WINAPI Detoured_IsDebuggerPresent  ()
{
	BOOL result;
	// Code was generated to include diver protocol. 
	DEBUGGER_RESPONSE DebugResponse;
	vector<ULONG_PTR> Arguments;
	vector<ULONG_PTR> ArgHints;
	ZeroMemory   (&DebugResponse,sizeof(DEBUGGER_RESPONSE));
	/* 
	// The Variable DebugMsgBuffer is used to buffer Arguments send to OutputDebugStringW().
	// With no Arguments defined for this detour routine. The Variable is not needed
	wstringstream DebugMsgBuffer;
	 */ 
	OutputDebugStringW   (L"IsDebuggerPresent reached OK.\r\n");
	// Code was generated to pack arguments into unicode string but function requires no arguments. Skipping that
	// DiverCode is emitted. This emits a buffer and a call to raise a special exeption that allows the debugger to modify arguments
	/* The Bool Below stores the call to the RaiseExceptionTrackFunc() that diver protocal uses */	
	BOOL DiverExceptionReturnVal = FALSE;
	// The format of the arguments that the debugger will get is from left to right i.e. the Source code difination left to right
	// The number of  entries in the vector pairs will always be equal to the number of arguments plus 1
	// The first element in the vector is always the number of remaining arguments.
	Arguments.push_back(0);
	ArgHints.push_back(0);
	DiverExceptionReturnVal= 	RaiseExceptionTrackFunc   (L"IsDebuggerPresent\0\0",Arguments,ArgHints,(ULONG_PTR*)&result,(ULONG_PTR*)0,&DebugResponse);
	if (DiverExceptionReturnVal == FALSE)
	{
		// Debugger Did not Set the seen it val in the DEUGGER_RESONSE struct datatype. The attached debugger (if any) likely does not understand our exception
		// So We just call the routine, assign the value and return
		OutputDebugStringW(L"-------->The Debugger Attached did not set the responst dword.\r\n");
		result= 		IsDebuggerPresent_Ptr   ();
		return result;
	}
	OutputDebugStringW(L"-------->The Debugger Attached SET THE RESPONSE DWORD.\r\n");

	// If we reach here. We need to examine the debugger response struct for modifications
	if (DebugResponse.Flags == ForceReturn)
	{
		OutputDebugStringW(L"-------->The Debugger Attached Forced a return value\r\n");
		return ((BOOL)DebugResponse.Arg1);
	}
	// Debugger was free to change the arguments during the exception All that's lest is to call the function with the arguments
	// TODO: Modify generator to copy arguments to new memory so that we guard access access violations 
	result= 	IsDebuggerPresent_Ptr   ();
	return result;
}
// Routine to Attach To function IsDebuggerPresent
BOOL WINAPI DetourAttach_IsDebuggerPresent  ()
{
	LONG DetourResultValue = 0;
	wstringstream MsgBuffer;
	OutputDebugStringW   (L"Starting to Attach To DetourAttach_IsDebuggerPresent\r\n");
	// Code was generated to statically  use the original IsDebuggerPresent and let the C/C++ linker do the rest
	IsDebuggerPresent_Ptr = IsDebuggerPresent;
	DetourSetIgnoreTooSmall   (FALSE);
	DetourResultValue= 	DetourTransactionBegin   ();
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourTransactionBegin() for Attach To function IsDebuggerPresent failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		return FALSE;
	}
	DetourResultValue = DetourUpdateThread(GetCurrentThread());
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourUpdateThread() for Attach To function IsDebuggerPresent failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	DetourResultValue = DetourAttach(&(PVOID&)IsDebuggerPresent_Ptr,  Detoured_IsDebuggerPresent);
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourAttach() for Attach To function IsDebuggerPresent failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	DetourResultValue= 	DetourTransactionCommit   ();
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourTransactionCommit() for Attach To function IsDebuggerPresent failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	return TRUE;
}
// The Below typedef and variable receive a pointer to the original function of CreateFileA
typedef HANDLE (WINAPI* CreateFileA_PtrType)  (LPCSTR,DWORD,DWORD,LPSECURITY_ATTRIBUTES,DWORD,DWORD,HANDLE);
CreateFileA_PtrType CreateFileA_Ptr;
HANDLE WINAPI Detoured_CreateFileA  (LPCSTR lpFileName,DWORD dwDesiredAccess,DWORD dwShareMode,LPSECURITY_ATTRIBUTES lpSecurityAttributes,DWORD dwCreationDisposition,DWORD dwFlagsAndAttributes,HANDLE TemplateFile)
{
	HANDLE result;
	// Code was generated to include diver protocol. 
	DEBUGGER_RESPONSE DebugResponse;
	vector<ULONG_PTR> Arguments;
	vector<ULONG_PTR> ArgHints;
	ZeroMemory   (&DebugResponse,sizeof(DEBUGGER_RESPONSE));
	// This variable buffers Unicode Strings to send to OutputDebugStringW\r\n
	wstringstream DebugMsgBuffer;
	OutputDebugStringW   (L"CreateFileA reached OK.\r\n");
	// Pack the argument list into an Unicode Code string and make a call to OutpuDebugStringW()
	DebugMsgBuffer << (lpFileName) << (dwDesiredAccess) << (dwShareMode) << (lpSecurityAttributes) << (dwCreationDisposition) << (dwFlagsAndAttributes) << (TemplateFile) << endl;
	OutputDebugStringW   (DebugMsgBuffer.str().c_str());
	DebugMsgBuffer.str ( L"" );
	DebugMsgBuffer.clear();
	// DiverCode is emitted. This emits a buffer and a call to raise a special exeption that allows the debugger to modify arguments
	/* The Bool Below stores the call to the RaiseExceptionTrackFunc() that diver protocal uses */	
	BOOL DiverExceptionReturnVal = FALSE;
	// The format of the arguments that the debugger will get is from left to right i.e. the Source code difination left to right
	// The number of  entries in the vector pairs will always be equal to the number of arguments plus 1
	// The first element in the vector is always the number of remaining arguments.
	Arguments.push_back(7);
	ArgHints.push_back(7);
	Arguments.push_back((ULONG_PTR)&lpFileName);
	Arguments.push_back((ULONG_PTR)&dwDesiredAccess);
	Arguments.push_back((ULONG_PTR)&dwShareMode);
	Arguments.push_back((ULONG_PTR)&lpSecurityAttributes);
	Arguments.push_back((ULONG_PTR)&dwCreationDisposition);
	Arguments.push_back((ULONG_PTR)&dwFlagsAndAttributes);
	Arguments.push_back((ULONG_PTR)&TemplateFile);
	ArgHints.push_back((ULONG_PTR)20);
	ArgHints.push_back((ULONG_PTR)8);
	ArgHints.push_back((ULONG_PTR)8);
	ArgHints.push_back((ULONG_PTR)43);
	ArgHints.push_back((ULONG_PTR)8);
	ArgHints.push_back((ULONG_PTR)8);
	ArgHints.push_back((ULONG_PTR)8);
	DiverExceptionReturnVal= 	RaiseExceptionTrackFunc   (L"CreateFileA\0\0",Arguments,ArgHints,(ULONG_PTR*)&result,(ULONG_PTR*)0,&DebugResponse);
	if (DiverExceptionReturnVal == FALSE)
	{
		// Debugger Did not Set the seen it val in the DEUGGER_RESONSE struct datatype. The attached debugger (if any) likely does not understand our exception
		// So We just call the routine, assign the value and return
		result= 		CreateFileA_Ptr   (lpFileName,dwDesiredAccess,dwShareMode,lpSecurityAttributes,dwCreationDisposition,dwFlagsAndAttributes,TemplateFile);
		return result;
	}
	// If we reach here. We need to examine the debugger response struct for modifications
	if (DebugResponse.Flags == ForceReturn)
	{
		return ((HANDLE)DebugResponse.Arg1);
	}
	// Debugger was free to change the arguments during the exception All that's lest is to call the function with the arguments
	// TODO: Modify generator to copy arguments to new memory so that we guard access access violations 
	result= 	CreateFileA_Ptr   (lpFileName,dwDesiredAccess,dwShareMode,lpSecurityAttributes,dwCreationDisposition,dwFlagsAndAttributes,TemplateFile);
	return result;
}
// Routine to Attach To function CreateFileA
BOOL WINAPI DetourAttach_CreateFileA  ()
{
	LONG DetourResultValue = 0;
	wstringstream MsgBuffer;
	OutputDebugStringW   (L"Starting to Attach To DetourAttach_CreateFileA\r\n");
	// Code was generated to statically  use the original CreateFileA and let the C/C++ linker do the rest
	CreateFileA_Ptr = CreateFileA;
	DetourSetIgnoreTooSmall   (FALSE);
	DetourResultValue= 	DetourTransactionBegin   ();
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourTransactionBegin() for Attach To function CreateFileA failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		return FALSE;
	}
	DetourResultValue = DetourUpdateThread(GetCurrentThread());
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourUpdateThread() for Attach To function CreateFileA failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	DetourResultValue = DetourAttach(&(PVOID&)CreateFileA_Ptr,  Detoured_CreateFileA);
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourAttach() for Attach To function CreateFileA failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	DetourResultValue= 	DetourTransactionCommit   ();
	if (DetourResultValue != NO_ERROR)
	{
		MsgBuffer << L"Call to DetourTransactionCommit() for Attach To function CreateFileA failed with error code " << DetourResultValue << endl;
		OutputDebugStringW   (MsgBuffer.str().c_str());
		DetourTransactionAbort   ();
		return FALSE;
	}
	return TRUE;
}
// Code generated with 'WantThreadDetourAttachCalls enabled (set to 1).
// This causes the DetourAttach() transactions to be made in DLL_THREAD_ATTACH instead of DLL_PROCESS_ATTACH
// Code generated with 'DetourHelperCodeFlag enabled (set to 1).
// This means a call to DetourIsHelperProcess will be make and DllMainwill return TRUE without futher processing if DetourIsHelperProcess() routines TRUE
// Code generated with 'PinCode enabled (set to 1).
// DllMain will include code with  a call to GetModuleHandleExW() with GET_MODULE_HANDLE_EX_FLAG_PIN
// This can assist in preventing access violations should Diver be prematurely unmapped without restoring hooks
// Once PinSelfInMemory is called, this gets a handle to the dll this code is in.
HMODULE SelfId = 0;
BOOL WINAPI PinSelfInMemory  (HINSTANCE DllSelfAddress)
{
	BOOL Result;
	wstringstream DebugStringBuffer;
	SetLastError   (0);
	Result= 	GetModuleHandleExW   ((GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |  GET_MODULE_HANDLE_EX_FLAG_PIN),((LPCWSTR) DllSelfAddress),&SelfId);
	if (Result == TRUE)
	{
		DebugStringBuffer << "GetModuleHandleExW() Reports success with pinning Diver Dll in memory with LastErrorCode of " << GetLastError() << endl;
		OutputDebugStringW   (DebugStringBuffer.str().c_str());
	}
	else	{
		DebugStringBuffer << "GetModuleHandleExW() Reports failure with pinning Diver Dll in memory with LastErrorCode of " << GetLastError() << endl;
		OutputDebugStringW   (DebugStringBuffer.str().c_str());
	}
	return Result;
}

BOOL WINAPI DllMain  (HINSTANCE hinstDLL,DWORD fdwReason,LPVOID lpReserved)
{
	BOOL Result = FALSE;
	Result= 	DetourIsHelperProcess   ();
	if (Result == TRUE)
	{
		return Result;
	}

	PinSelfInMemory   (hinstDLL);
	DetourRestoreAfterWith   ();

	switch ( fdwReason )
	{
		case DLL_PROCESS_ATTACH:
		{
Result = TRUE;			break;
		}
		case DLL_PROCESS_DETACH:
		{
			break;
		}
		case DLL_THREAD_ATTACH:
		{
Result= DetourAttach_IsDebuggerPresent   ();
if (Result == FALSE)
{
	return Result;
}
Result= DetourAttach_CreateFileA   ();
if (Result == FALSE)
{
	return Result;
}
			break;
		}
		case DLL_THREAD_DETACH:
		{
			break;
		}
	}
	return Result;
}

