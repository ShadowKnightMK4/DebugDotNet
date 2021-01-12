#include <Windows.h>
#include <iostream>
#include <vector>
#include <process.h>
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
enum DEBUGGER_RESPONSE_FLAGS : unsigned int
{
	NoResponse = 0,
	// Detoured Function does not call original and instead returns struct.Arg1
	ForceReturn = 2,
	

};

/// struct that a debugger fills out to control response.
/// Debugger may also modify arguments already passes as seen fit.
typedef struct DEBUGGER_RESPONSE
{
	// Set to sizeof(DEBUGGER_RESPONSE) by detoured function
	DWORD SizeOfThis;
	// Set to sizeof(VOID*) By detoured function
	DWORD SizeOfPtr;
	// must be set by debugger to indicate that this was seen and modidied.
	BOOL DebuggerSeenThis;
	// Flags to change stuff
	DEBUGGER_RESPONSE_FLAGS Flags;
	// Argument for certain flags
	DWORD Arg1;
};


using namespace std;

BOOL(WINAPI* IsDebuggerPresentPtr)();



BOOL (WINAPI* RaiseExceptionDiverCheck)();

BOOL(WINAPI* RaiseExceptionTrackFunc)(const wchar_t* FuncName,
	vector<ULONG_PTR>& ArgPtrs,
	vector<ULONG_PTR>& TypeHint,
	ULONG_PTR* RetVal,
	ULONG_PTR* RetHint,
	DEBUGGER_RESPONSE* Debugger);


LONG (WINAPI* DetourUpdateThread)(
	_In_ HANDLE hThread
);
LONG(WINAPI* DetourTransactionCommit)(VOID);
LONG(WINAPI* DetourDetach)(
	_Inout_ PVOID* ppPointer,
	_In_    PVOID pDetour
);
LONG(WINAPI* DetourTransactionBegin)(VOID);

LONG (WINAPI* DetourAttach)(
	_Inout_ PVOID* ppPointer,
	_In_    PVOID pDetour
	);

using namespace std;
HMODULE DiverDll;
HMODULE NTDLL;

float TestsPassed;
float TestsFailed;
bool Fatal = false;


VOID* OriginalPtr = 0;
VOID* FixedPtr;
void _cdecl KernelJump()
{
	unsigned long reg;
	__asm
	{
		mov reg, eax
		call OriginalPtr;
	}
	if (reg == 0x55)
	{
		cout << "File System was accessed" << endl;
	}
}

BOOL WINAPI MyIsDebuggerPresent()
{
	vector<ULONG_PTR> ArgPtrs;
	vector<ULONG_PTR> TypeHints;
	BOOL RetVal;
	BOOL CallVal;
	DEBUGGER_RESPONSE res;
	ZeroMemory(&res, sizeof(DEBUGGER_RESPONSE));
	res.SizeOfPtr = sizeof(ULONG_PTR);
	res.SizeOfThis = sizeof(DEBUGGER_RESPONSE);

	// there are no args to pack.
	CallVal = RaiseExceptionTrackFunc(L"IsDebuggerPresent", ArgPtrs, TypeHints, (ULONG_PTR*)&RetVal, 0, &res);

	res.DebuggerSeenThis = TRUE;
	res.Flags = ForceReturn;

	cout << "For normal debug purpose. Value of Debugger is " << res.DebuggerSeenThis << endl;

	if (res.DebuggerSeenThis == FALSE)
	{
		cout << "Debugger did not understand this exception. Doing Defaults" << endl;
		return IsDebuggerPresentPtr();
	}
	else
	{
		cout << "Debugger understood the track func exception " << endl;
		if (res.Flags & (ForceReturn ))
		{
			cout << "Debugger forced return value of " << res.Arg1 << " with disabling the actuall call" << endl;
			return res.Arg1;
		}
		else
		{
			return IsDebuggerPresentPtr();
		}
	}

}


void IniApp()
{
	cout << "Loading Diver" << endl;
	DiverDll = LoadLibrary(L"C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\bin\\Debug\\netstandard2.0\\DebugDotNetDiver.dll");
	if (DiverDll == 0)
	{
		cout << "Failed to find Diver Dll" << "Last Error code is " << GetLastError() << endl;
		Fatal = true;
		return;
	}
	else
	{
		cout << "Diver Dll Ok." << " Linkning Routines" << endl;

		DetourUpdateThread = (LONG (__stdcall*)(HANDLE)) GetProcAddress(DiverDll, (LPCSTR)8);
		DetourTransactionCommit = (LONG(__stdcall*)()) GetProcAddress(DiverDll, (LPCSTR)7);
		DetourAttach = (LONG(__stdcall*)(PVOID *, PVOID))GetProcAddress(DiverDll, (LPCSTR)5);
		DetourDetach = (LONG(__stdcall*)(PVOID*, PVOID))GetProcAddress(DiverDll, (LPCSTR)6);
		DetourTransactionBegin  = (LONG(__stdcall*)()) GetProcAddress(DiverDll, (LPCSTR)4);
		RaiseExceptionTrackFunc = (BOOL(WINAPI*)(const wchar_t*,
			vector<ULONG_PTR>&,
			vector<ULONG_PTR>&,
			ULONG_PTR*,
			ULONG_PTR*,
			DEBUGGER_RESPONSE*)) GetProcAddress(DiverDll, (LPCSTR)3);
		RaiseExceptionDiverCheck = (BOOL(__stdcall*)()) GetProcAddress(DiverDll, (LPCSTR)2);

		if (RaiseExceptionDiverCheck == 0)
		{
			cout << "RaiseExceptionDiverCheck did not link" << endl;
			Fatal = true;
		}


		if (DetourUpdateThread == 0)
		{
			cout << "DetourUpdateThread did not link" << endl;
			Fatal = true;
		}


		if (DetourTransactionCommit == 0)
		{
			cout << "DetourTransactionCommit did not link" << endl;
			Fatal = true;
		}

		if (DetourAttach == 0)
		{
			cout << "DetourAttach did not link" << endl;
			Fatal = true;
		}

		if (DetourDetach == 0)
		{
			cout << "DetourDetach did not link" << endl;
			Fatal = true;
		}

		if (DetourTransactionBegin == 0)
		{
			Fatal = true;
			cout << "DetourTransactionBegin did not link" << endl;
		}

		if (RaiseExceptionTrackFunc == 0)
		{
			Fatal = true;
			cout << "RaiseExceptionTrackFunc did not link" << endl;
		}
		TestsPassed++;
	}
}

void Test1()
{
	cout << "Test1. Calling the driver version func. If an attached debugger does not handle the exception (and modidify a value), this returns false" << endl;
	auto result = RaiseExceptionDiverCheck();
	cout << result << " is the result of the call ";
	if (result != FALSE)
	{
		cout << "Debugger reconized and accepted communication via this protocal " << endl;
		TestsPassed++;
	}
	else
	{
		cout << "Debugger did not accept or reconize this protocal " << endl;
		TestsFailed++;
	}
}

void Test2()
{
	std::vector<ULONG_PTR> ArgList;
	std::vector<ULONG_PTR> ArgHint;
	DEBUGGER_RESPONSE Response;
	ZeroMemory(&Response, sizeof(DEBUGGER_RESPONSE));
	
	ULONG_PTR ret;
	ULONG_PTR Hint;
	ArgList.push_back(0);
	ArgHint.push_back(0);

	cout << "Raising an exception to se the argtype";

	RaiseExceptionTrackFunc(L"Example", ArgList, ArgHint, &ret, &Hint, &Response);
}
void _cdecl Test2NukApplyDetourDummy(void* junk)
{
	
	while (true)
	{
		OutputDebugStringW(L"Keep Alive\r\n");
		cout << "IsDebugger Present in dummy thread returns " << IsDebuggerPresent() << endl;
		Sleep(2000);
	}
}

void _cdecl Test2Nukk(void* junk)
{
	
	cout << "Test2 is ran in seperate thread" << endl;
	cout << "Starting Test2. This detours IsDebuggerPresent(). It also calls it before detour" << endl;
	IsDebuggerPresentPtr = IsDebuggerPresent;

	long Start, enlist, attach, commit;

	cout << "Original Debugger check of IsDebuggerPresent() returns " << IsDebuggerPresent() << endl;
	Start = DetourTransactionBegin();
	if (Start != 0)
	{
		cout << "Test Failed. DetourTransactionBegin() returned " << Start << endl;
		TestsFailed++;
		return;
	}
	else
	{
		cout << "DetourStarted" << endl;
		enlist = DetourUpdateThread((HANDLE)junk);
		if (enlist != 0)
		{
			cout << "Test Failed. DetourUpdateTHread() returned " << enlist;
			TestsFailed++;
			return;
		}
		else
		{
			attach = DetourAttach(&(PVOID&)IsDebuggerPresentPtr, MyIsDebuggerPresent);
			if (attach != 0)
			{
				cout << "Test Failed. DetourAttach did not link with IsDebuggerPresent(). Error " << attach << endl;
			}


			commit = DetourTransactionCommit();
			if (commit != 0)
			{
				cout << "Test Failed. Could not commit detour.  Error " << commit << endl;
			}

			cout << "Sucess in detouring IsDebuggerPresent(). Calling my version thru that ptr" << endl;
			cout << "IsDebuggerPresent() returns " << IsDebuggerPresent() << endl;
		}
	}
}

void RemoveRiteStuff(VOID* Target)
{
	MEMORY_BASIC_INFORMATION Info;
	ZeroMemory(&Info,sizeof(Info));
	auto result1 =VirtualQuery(Target, &Info, sizeof(Info));
	auto result2 = VirtualProtect(Target, Info.RegionSize, Info.Protect + PAGE_EXECUTE_READWRITE, 0);
	return;
}
void Test3()
{
	cout << "Test 3 is  a little different. We place a detour trampoline at 0x7FFE0300 with a pointer to KernelJump() KernalJump just calls the original ptr" << endl;
	// get a copy of the data the thing points too
	FixedPtr = (VOID*)0x7FFE0300;
	OriginalPtr = (VOID*) *((DWORD*)FixedPtr);
	auto start = DetourTransactionBegin();
	RemoveRiteStuff(OriginalPtr);
	if (start != 0)
	{
		cout << "Failed to start detour" << start;
		TestsFailed++;
		return;
	}
	auto attach = DetourAttach(&(PVOID&)OriginalPtr, KernelJump);
	if (attach != 0)
	{
		cout << "Failed to attach to target " << attach  << endl;
		TestsFailed++;
	}
	auto thread = DetourUpdateThread(GetCurrentThread());
	if (thread != 0)
	{
		cout << "Failed to attach to target " << thread << endl;
		TestsFailed++;
	}
	auto fin = DetourTransactionCommit();
	if (fin != 0)
	{
		cout << "Failed to attach to target " << fin << endl;
		TestsFailed++;
	}


	cout << "If you get here. We've modified the target pointer.";

	cout << "Next Step we do is creata a file at C:\\Windows\\TEMP\\Discard.txt" << endl;

	HANDLE fn = INVALID_HANDLE_VALUE;
	__try
	{
		fn = CreateFile(L"C:\\Windows\\TEMP\\Discard.txt", GENERIC_WRITE, FILE_SHARE_READ, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_TEMPORARY, NULL);
	}
	__finally
	{
		CloseHandle(fn);
	}

}
int main()
{
	IniApp();
	if (!Fatal)
	{

		Test1();
		Test2();
		
		
		
		if (TestsFailed + TestsPassed != 0)
		{
			cout << "Percent passed (" << TestsPassed / (TestsFailed + TestsPassed) << ") Percent failed (" << TestsPassed / (TestsFailed + TestsPassed) << endl;
		}
		else

		{
			cout << "Error evaling tests" << endl;
		}
		return 0;
	}
	return -1;
} 