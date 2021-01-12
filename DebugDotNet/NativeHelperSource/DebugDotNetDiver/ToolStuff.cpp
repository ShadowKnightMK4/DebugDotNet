#include "stdafx.h"


extern "C" {
	/*
	If the Check handle is not GetCurrentThread() and the currently running thread is the current thread, return GetCurrentThread()
	*/
	HANDLE DetourCurrentThreadGuard(HANDLE Check)
	{
		if (Check == INVALID_HANDLE_VALUE)
		{
			return Check;
		}
		else
		{
			HANDLE CurThreadFake;
			HANDLE TrueHandle;
			int CurThreadId;

			CurThreadFake = GetCurrentThread();
			CurThreadId = GetCurrentThreadId();

			if (DuplicateHandle(GetCurrentProcess(), CurThreadFake, GetCurrentProcess(), &TrueHandle, 0, FALSE, DUPLICATE_SAME_ACCESS) == FALSE)
			{
				OutputDebugStringW(L"A Call to DuplicateHandle() Failed in the same thread check while attaching to routines by detours");
				return CurThreadFake;
			}
			else
			{ 
				if (GetThreadId(TrueHandle) == GetCurrentThreadId())
				{
					// lets just return to fake handle to make detours not Suspend the currently running thread
					CloseHandle(TrueHandle);
					return GetCurrentThread();
				}
				else
				{
					CloseHandle(TrueHandle);
					return Check;
				}
			}
		}
	}
}