#pragma once

#ifndef _INC_WINDOWS
#include <Windows.h>

#endif

#ifndef _VECTOR_
#include <vector>
#endif

#include  "DiverComMessages.h"

// debugger replies to the exception call
enum DEBUGGER_RESPONSE_FLAGS : unsigned int
{
	// no special modifications
	NoResponse = 0,
	// Detoured Function does not call original and instead returns struct.Arg1
	ForceReturn = 2

};

/// Debugger may also modify arguments already passes as seen fit.
typedef struct DEBUGGER_RESPONSE
{
	// Set to sizeof(DEBUGGER_RESPONSE) by detoured function
	DWORD SizeOfThis;
	// Set to sizeof(VOID*) By detoured function
	DWORD SizeOfPtr;
	// must be set by debugger to indicate that this was seen and modified.
	BOOL DebuggerSeenThis;
	// Flags to change stuff
	DEBUGGER_RESPONSE_FLAGS Flags;
	// Argument for certain flags
	DWORD Arg1;
};
extern "C"
{
	/// <summary>
	/// Test function to see if SEH is properly raise, and returns true if the debugger sets a pointer. The debugger should not set this pointer if the protocal is not supported
	/// </summary>
	/// <returns>returns true if debugger set the response pointer to a non-zero value</returns>
	BOOL WINAPI RaiseExceptionDiverCheck();

	/// <summary>
	// purpose is to test if the DebugDotNet code is extracting parameters right.
	// if so then the list should be   list containing numbers 0 to 15
	/// </summary>
	/// <returns>nothing</returns>
	void WINAPI TestRaiseExceptionSize();

	
	/// <summary>
	/// Raise an exception with the passed message. Essentially an OutputDebugMessage but with a context 'channel' that allows the debugger to filter messages into different groups if supported
	/// </summary>
	/// <param name="String">the message to send</param>
	/// <param name="Channel">the context for debugger (channel 0 means OutpuDebugStringW) (generated code uses this to allow debugger to filter types of messages)</param>
	/// <param name="FallBackToDebugString"> if set and the debugger does not set its response value, we fall back to OuputDebugStringW() regardless </param>
	/// <returns>returns TRUE if debugger understood the message and FALSE if it did not (setting FallBackToDebugString to non-zero will trigger a call to OuputDebugStringW() if debugger does not set the understand flag) </returns>
	BOOL WINAPI RaiseExceptionSpecialDebugMessage(wchar_t* String, int Channel, BOOL FallBackToDebugString);
}

/// <summary>
/// Raise an exception to inform the debugger of a function that we detoured is being called and provide the debugger with information
/// </summary>
/// <param name="FuncName">Unicode name string of the function</param>
/// <param name="ArgPtrs">a CPP vector containing pointers to arguments</param>
/// <param name="TypeHint">a CPP vector containing argument context</param>
/// <param name="RetVal">passed but not fully implemented. indented to be pointer to return value</param>
/// <param name="RetHint">passed as context for retval </param>
/// <param name="Debugger">pointer to a debugger response structure</param>
/// <returns>true if the debugger set the response value to indicate that it understood the SEH signal</returns>
/// <remarks>This is not set with the extern "C" prompt due to the use of vectors </remarks>
BOOL WINAPI RaiseExceptionTrackFunc(const wchar_t* FuncName,
	std::vector<ULONG_PTR>& ArgPtrs,
	std::vector<ULONG_PTR>& TypeHint,
	ULONG_PTR* RetVal,
	ULONG_PTR* RetHint,
	DEBUGGER_RESPONSE* Debugger);
