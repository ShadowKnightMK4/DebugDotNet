// TestCaseDetourCreateProcessWithDll.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <Windows.h>

const char* dummy[] = {
    "test.dll",
    "test2.dll"
};

typedef  BOOL WINAPI DetourCreateProcessWithDlls(
    _In_opt_          LPCTSTR lpApplicationName,
    _Inout_opt_       LPTSTR lpCommandLine,
    _In_opt_          LPSECURITY_ATTRIBUTES lpProcessAttributes,
    _In_opt_          LPSECURITY_ATTRIBUTES lpThreadAttributes,
    _In_              BOOL bInheritHandles,
    _In_              DWORD dwCreationFlags,
    _In_opt_          LPVOID lpEnvironment,
    _In_opt_          LPCTSTR lpCurrentDirectory,
    _In_              LPSTARTUPINFOW lpStartupInfo,
    _Out_             LPPROCESS_INFORMATION lpProcessInformation,
    _In_              DWORD nDlls,
    _In_reads_(nDlls) LPCSTR* rlpDlls,
    _In_opt_          FARPROC pfCreateProcessW
);

DetourCreateProcessWithDlls* Callback;
HMODULE Detours;
int main()
{
    wchar_t* jump = new wchar_t[25];
    ZeroMemory(jump, 24);
    std::cout << "Hello World!\n";
    Detours = LoadLibrary(L"C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\bin\\Debug\\netstandard2.0\\Detours.dll");
    if (Detours == NULL)
    {
        std::cout << "Failed to load detours. Test Failed" << std::endl;
    }
    else
    {
        PROCESS_INFORMATION Info;
        STARTUPINFO defaults;
        ZeroMemory(&defaults, sizeof(STARTUPINFO));
        ZeroMemory(&Info, sizeof(PROCESS_INFORMATION));

        defaults.cb = sizeof(STARTUPINFO);

        std::cout << "Detours Loaded Ok. Getting callback for entry #1" << std::endl;
        Callback = (DetourCreateProcessWithDlls*)GetProcAddress(Detours, (LPCSTR)2);

        if (Callback == 0)
        {
            std::cout << "Test Failed. Did not work" << GetLastError() << std:: endl;
        }
        else
        {
            Callback(TEXT("C:\\windows\\system32\\cmd.exe"), 0, 0, 0, false, 0, 0, 0, &defaults, &Info, 0, 0, 0);
        }
    }

}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
