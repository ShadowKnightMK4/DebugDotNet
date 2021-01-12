/*
	defines the communication protocal between the debugger and the library.
*/
#include "stdafx.h"

extern "C" {
	CRITICAL_SECTION Section;
	/*
		FUTURE: Logging mode is planned to be a bit mask allows a debugger to specify what kind of info to receive.
		CURRENTLY: 0 means off and non-zero means full
	*/
	volatile int LoggingMode = 0;

	/// <summary>
	/// Common dispatcher routine used by the DiverCom routines that throw SEH signals
	/// </summary>
	/// <param name="ExceptionCode">The one that is thrown; use GetExceptionCode() SEH to get this</param>
	/// <param name="CheckAgainst">The code to check against</param>
	/// <param name="Argv">contains the pointer list.</param>
	/// <returns>returns EXCEPTION_CONTINUE_SEARCH if the two codes differ and EXCEPTION_EXECUTE_HANDLER if they match </returns>

	int Dispatcher(DWORD ExceptionCode, DWORD CheckAgainst, const EXCEPTION_POINTERS* Argv)
	{
		if (ExceptionCode != CheckAgainst)
		{
			return EXCEPTION_CONTINUE_SEARCH;
		}
		else
		{
			return EXCEPTION_EXECUTE_HANDLER;
		}
	}



	// hints for arguments
	// May contain multiple flags
	enum TypeHints : unsigned int
	{
		CanBeNull = 1,
		AnsiCString = 2,
		UnicodeCString = 4,
		Uchar = 8,
		Schar = 16,
		Ushort = 32,
		Sshort = 64,
		Uint = 128,
		Sint = 256,
		Ulong = 512,
		SLong = 1024,
		Float = 2048,
		Double = 4096
	};

	/// struct that a debugger fills out to control response.


	/// <summary>
	/// Test function to see if SEH is properly raise, and returns true if the debugger sets a pointer. The debugger should not set this pointer if the protocal is not supported
	/// </summary>
	/// <returns>returns true if </returns>
	BOOL WINAPI RaiseExceptionDiverCheck()
	{
		BOOL Reply;
		BOOL ReplyPtr = FALSE;
		ULONG_PTR ExceptionArgs[EXCEPTION_MAXIMUM_PARAMETERS];
		DEBUGGER_RESPONSE Res;
		ZeroMemory(&ExceptionArgs, sizeof(ExceptionArgs));
		ZeroMemory(&Res, sizeof(Res));
		ExceptionArgs[DIVERCOM_SETVERSION_REPLY_PTR] = (ULONG_PTR)&ReplyPtr;
		ExceptionArgs[DIVERCOM_SETVERSION_TARGET_VERSION] = DIVERCOM_VERSION;
		
		__try
		{
			RaiseException(DIVERCOM_SETVERSION, 0, 2, (const ULONG_PTR*)&ExceptionArgs);
		}
		__except (Dispatcher(DIVERCOM_SETVERSION, DIVERCOM_SETVERSION, GetExceptionInformation()))
		{
			// 
		}
		if (ReplyPtr != 0)
		{
			return TRUE;
		}
		else
		{
			return FALSE;
		}
	}


}

/// <summary>
/// Raise an exception with the passed message. Essentially an OutputDebugMessage but with a context 'channel' that allows the debugger to filter messages into different groups if supported
/// </summary>
/// <param name="String">the message to send</param>
/// <param name="Channel">the context for debugger (channel 0 means OutpuDebugStringW) (generated code uses this to allow debugger to filter types of messages)</param>
/// <param name="FallBackToDebugString"> if set and the debugger does not set its response value, we fall back to OuputDebugStringW() regardless </param>
/// <returns>returns 1 if debugger understood the message and 0 if it did not (setting FallBackToDebugString to non-zero will trigger a call to OuputDebugStringW() if debugger does not set the understand flag) </returns>
BOOL WINAPI RaiseExceptionSpecialDebugMessage(wchar_t* String, int Channel, BOOL FallBackToDebugString)
{
	ULONG_PTR ExceptionArgs[EXCEPTION_MAXIMUM_PARAMETERS];
	DEBUGGER_RESPONSE Res;

	if (Channel == 0)
	{
		OutputDebugStringW(String);
		return FALSE;
	}

	ZeroMemory(&Res, sizeof(DEBUGGER_RESPONSE));
	ZeroMemory(&ExceptionArgs, sizeof(ExceptionArgs));

	ExceptionArgs[0] = (ULONG_PTR) &Res.DebuggerSeenThis;
	ExceptionArgs[1] = (ULONG_PTR)Channel;
	ExceptionArgs[2] = (ULONG_PTR)String;
	ExceptionArgs[3] = 0;
	if (String != NULL)
	{
		ExceptionArgs[3] = (ULONG_PTR)wcslen(String);
	}
	__try
	{
		RaiseException(DIVERCOM_EXCEPTION_DEBUG_MSG, 0, 2, (const ULONG_PTR*)&ExceptionArgs);
	}
	__except (Dispatcher(GetExceptionCode(), DIVERCOM_EXCEPTION_DEBUG_MSG, GetExceptionInformation()))
	{
	}

	if (Res.DebuggerSeenThis == 0)
	{
		OutputDebugStringW(String);
		return FALSE;
	}
	else
	{
		return TRUE;
	}


}


/// <summary>
// purpose is to test if the DebugDotNet code is extracting parameters right.
// if so then the list should be   list containing numbers 0 to 15
/// </summary>
/// <returns>nothing</returns>
void WINAPI TestRaiseExceptionSize()
{
	BOOL DebuggerSawIt = FALSE;
	ULONG_PTR ExceptionArgs[EXCEPTION_MAXIMUM_PARAMETERS];
	ZeroMemory(&ExceptionArgs, sizeof(ExceptionArgs));

	__try
	{
		for (int step = 0; step < EXCEPTION_MAXIMUM_PARAMETERS;step++)
		{
			ExceptionArgs[step] = step;
		}
		RaiseException(0, 0, 15, ExceptionArgs);
	}
	__except (Dispatcher(0, 0, NULL))
	{

	}

}

// This constant is used to inforn the debugger exactly what resource the handle is for.
enum ResourceTypeValue
{
	Unknown = 0,
	File = 1,
	Mailslot = 2,
	Mutex = 3,
	Job = 4,
};

// This is used by genberated code to notify a debugger when a new Win32 Resource has been opened / ect...
/* Generated DetourCode like CreateFile, CreateMailslot() ect... include code that raises this exception*/
BOOL WINAPI RaiseExceptionNotifyResource(HANDLE NewResource, DWORD ResourceType)
{

	ULONG_PTR ExceptionArgs[EXCEPTION_MAXIMUM_PARAMETERS];
	DEBUGGER_RESPONSE Response;
	ZeroMemory(&ExceptionArgs, sizeof(ExceptionArgs));
	ZeroMemory(&Response, sizeof(Response));

	Response.SizeOfPtr = sizeof(VOID*);
	Response.SizeOfThis = sizeof(DEBUGGER_RESPONSE);


	ExceptionArgs[0] = (ULONG_PTR)&Response.DebuggerSeenThis;
	ExceptionArgs[1] = (ULONG_PTR)NewResource;
	ExceptionArgs[2] = (ULONG_PTR)ResourceType;

	__try
	{
		RaiseException(DIVERCOM_EXCECPTION_NOTIFY_RESOURCE, 0, 3, ExceptionArgs);
	}
	__except (Dispatcher(DIVERCOM_EXCEPTION_FUNC_CALL, DIVERCOM_EXCEPTION_FUNC_CALL, GetExceptionInformation()))
	{

	}

	return Response.DebuggerSeenThis;
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
/// <remarks>This is not set with with the extern "C" prompt due to the use of vectors </remarks>
BOOL WINAPI RaiseExceptionTrackFunc(const wchar_t* FuncName,
	std::vector<ULONG_PTR>& ArgPtrs,
	std::vector<ULONG_PTR>& TypeHint,
	ULONG_PTR* RetVal,
	ULONG_PTR* RetHint,
	DEBUGGER_RESPONSE* Debugger)
{
	
	ULONG_PTR ExceptionArgs[EXCEPTION_MAXIMUM_PARAMETERS];
	ZeroMemory(&ExceptionArgs, sizeof(ExceptionArgs));


	/*
		Args format
		Arg[0] = &DebuggerSawIt;			=> Receives pointer to Debugguer->DebuggerSeenThis bool.
											-> This must be non-zero on return from exception to show that
											-> debugger actually saw and is ok with changes made to the function args.

												if a debugger does not set it, the function proceeds normally
		Arg[1] = &ArgPtrs.Data				=> should align 1 to 1 with RetHint.Data, this is a collection of all arguments the function received (and in order from first to last by prototype)
		Arg[2] = &RetHint.Data				=> should aliign with ArgPtr.Data,	this is a collection of special enums that indicate how to convert the ArgPtrs value to a unicode string
		Arg[3] = & ReturnValue				=>  ues he  arg enum to indicate what type of return value this is
		Arg[4] =& ReturnHInt					=> hint for retur nvalue
		Arg[5] = Debugger
		Arg[6] = name of function as unicode string
		*/
	__try
	{

		ExceptionArgs[DIVERCOM_DEBUGSEEN_IT_ARG_ENTRY] = (ULONG_PTR)&Debugger->DebuggerSeenThis;
		ExceptionArgs[DIVERCOM_ARGVECTOR_CONTENT] = (ULONG_PTR)ArgPtrs.data();
		ExceptionArgs[DIVERCOM_ARGVECTOR_HINTS] = (ULONG_PTR)TypeHint.data();
		ExceptionArgs[DIVERCOM_ARGVECTOR_SIZE] = (ULONG_PTR)ArgPtrs.size();
		ExceptionArgs[DIVERCOM_RETPTR_SIZE] = (ULONG_PTR)RetVal;
		ExceptionArgs[DIVERCOM_DEBUGGER_PTR_STRUCT] = (ULONG_PTR)Debugger;
		ExceptionArgs[6] = (ULONG_PTR)FuncName;
		if (FuncName != 0)
		{
			ExceptionArgs[7] = wcslen(FuncName);
		}
		else
		{
			ExceptionArgs[7] = 0;
		}

		RaiseException(DIVERCOM_EXCECPTION_NOTIFY_RESOURCE, 0, 8, ExceptionArgs);
	}
	__except (Dispatcher(DIVERCOM_EXCECPTION_NOTIFY_RESOURCE, DIVERCOM_EXCECPTION_NOTIFY_RESOURCE, GetExceptionInformation()))
	{
		
	}
	return Debugger->DebuggerSeenThis;
}