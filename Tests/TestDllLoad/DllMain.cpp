
#include <Windows.h>
extern "C" {
    // already in the detours static lib
    BOOL WINAPI DetourRestoreAfterWith(VOID);
    BOOL WINAPI DetourIsHelperProcess(VOID);

}

extern "C" {

    __declspec(dllexport) bool WINAPI DllMain(IN HINSTANCE hDllHandle,
    IN DWORD     nReason,
    IN LPVOID    Reserved)
{
    BOOLEAN bSuccess = TRUE;


    //  Perform global initialization.
    if (DetourIsHelperProcess())
        return bSuccess;
    

    switch (nReason)
    {
    case DLL_PROCESS_ATTACH:
        DetourRestoreAfterWith();
        OutputDebugString(L"-------------->>>>>>>>>>TestDll Was Loaded<<<<<<<<<<<<<<<<--------");
        //  For optimization.

        DisableThreadLibraryCalls(hDllHandle);

        break;

    case DLL_PROCESS_DETACH:
        OutputDebugString(L"-------------->>>>>>>>>>TestDll Was UNloaded<<<<<<<<<<<<<<<<--------");
        break;
    }



    return bSuccess;

}
//  end DllMain

}