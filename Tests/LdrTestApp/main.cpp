#include <windows.h>

#include <stdio.h>


typedef struct _PEB* PPEB;

typedef enum _PROCESSINFOCLASS {
    ProcessBasicInformation = 0,
    ProcessQuotaLimits = 1,
    ProcessIoCounters = 2,
    ProcessVmCounters = 3,
    ProcessTimes = 4,
    ProcessBasePriority = 5,
    ProcessRaisePriority = 6,
    ProcessDebugPort = 7,
    ProcessExceptionPort = 8,
    ProcessAccessToken = 9,
    ProcessLdtInformation = 10,
    ProcessLdtSize = 11,
    ProcessDefaultHardErrorMode = 12,
    ProcessIoPortHandlers = 13,   // Note: this is kernel mode only
    ProcessPooledUsageAndLimits = 14,
    ProcessWorkingSetWatch = 15,
    ProcessUserModeIOPL = 16,
    ProcessEnableAlignmentFaultFixup = 17,
    ProcessPriorityClass = 18,
    ProcessWx86Information = 19,
    ProcessHandleCount = 20,
    ProcessAffinityMask = 21,
    ProcessPriorityBoost = 22,
    ProcessDeviceMap = 23,
    ProcessSessionInformation = 24,
    ProcessForegroundInformation = 25,
    ProcessWow64Information = 26,
    ProcessImageFileName = 27,
    ProcessLUIDDeviceMapsEnabled = 28,
    ProcessBreakOnTermination = 29,
    ProcessDebugObjectHandle = 30,
    ProcessDebugFlags = 31,
    ProcessHandleTracing = 32,
    ProcessIoPriority = 33,
    ProcessExecuteFlags = 34,
    ProcessTlsInformation = 35,
    ProcessCookie = 36,
    ProcessImageInformation = 37,
    ProcessCycleTime = 38,
    ProcessPagePriority = 39,
    ProcessInstrumentationCallback = 40,
    ProcessThreadStackAllocation = 41,
    ProcessWorkingSetWatchEx = 42,
    ProcessImageFileNameWin32 = 43,
    ProcessImageFileMapping = 44,
    ProcessAffinityUpdateMode = 45,
    ProcessMemoryAllocationMode = 46,
    ProcessGroupInformation = 47,
    ProcessTokenVirtualizationEnabled = 48,
    ProcessOwnerInformation = 49,
    ProcessWindowInformation = 50,
    ProcessHandleInformation = 51,
    ProcessMitigationPolicy = 52,
    ProcessDynamicFunctionTableInformation = 53,
    ProcessHandleCheckingMode = 54,
    ProcessKeepAliveCount = 55,
    ProcessRevokeFileHandles = 56,
    ProcessWorkingSetControl = 57,
    ProcessHandleTable = 58,
    ProcessCheckStackExtentsMode = 59,
    ProcessCommandLineInformation = 60,
    ProcessProtectionInformation = 61,
    ProcessMemoryExhaustion = 62,
    ProcessFaultInformation = 63,
    ProcessTelemetryIdInformation = 64,
    ProcessCommitReleaseInformation = 65,
    ProcessReserved1Information = 66,
    ProcessReserved2Information = 67,
    ProcessSubsystemProcess = 68,
    ProcessInPrivate = 70,
    ProcessRaiseUMExceptionOnInvalidHandleClose = 71,
    ProcessSubsystemInformation = 75,
    ProcessWin32kSyscallFilterInformation = 79,
    ProcessEnergyTrackingState = 82,
    MaxProcessInfoClass                             // MaxProcessInfoClass should always be the last enum
} PROCESSINFOCLASS;


HMODULE user32, NTDLL;

 typedef int (WINAPI* NtQueryInformationProcess)(
	IN HANDLE           ProcessHandle,
	IN PROCESSINFOCLASS ProcessInformationClass,
	OUT void*           ProcessInformation,
	IN unsigned long            ProcessInformationLength,
	OUT PULONG          ReturnLength
);
 
 typedef long KPRIORITY;
 typedef struct _PROCESS_BASIC_INFORMATION {
     NTSTATUS ExitStatus;
     PPEB PebBaseAddress;
     ULONG_PTR AffinityMask;
     KPRIORITY BasePriority;
     ULONG_PTR UniqueProcessId;
     ULONG_PTR InheritedFromUniqueProcessId;
 } PROCESS_BASIC_INFORMATION, * PPROCESS_BASIC_INFORMATION;
 typedef struct _UNICODE_STRING {
     USHORT Length;
     USHORT MaximumLength;
     PWSTR  Buffer;
 } UNICODE_STRING, * PUNICODE_STRING;



 typedef struct _RTL_USER_PROCESS_PARAMETERS {
     BYTE           Reserved1[16];
     PVOID          Reserved2[10];
     UNICODE_STRING ImagePathName;
     UNICODE_STRING CommandLine;
 } RTL_USER_PROCESS_PARAMETERS, * PRTL_USER_PROCESS_PARAMETERS;

 typedef struct _PEB_LDR_DATA {
     BYTE       Reserved1[8];
     PVOID      Reserved2[3];
     LIST_ENTRY InMemoryOrderModuleList;
 } PEB_LDR_DATA, * PPEB_LDR_DATA;



 typedef struct _PEB {
     BYTE                          Reserved1[2];
     BYTE                          BeingDebugged;
     BYTE                          Reserved2[1];
     PVOID                         Reserved3[2];
     PPEB_LDR_DATA                 Ldr;
     PRTL_USER_PROCESS_PARAMETERS  ProcessParameters;
     PVOID                         Reserved4[3];
     PVOID                         AtlThunkSListPtr;
     PVOID                         Reserved5;
     ULONG                         Reserved6;
     PVOID                         Reserved7;
     ULONG                         Reserved8;
     ULONG                         AtlThunkSListPtr32;
     PVOID                         Reserved9[45];
     BYTE                          Reserved10[96];
     VOID* PostProcessInitRoutine;
     BYTE                          Reserved11[128];
     PVOID                         Reserved12[1];
     ULONG                         SessionId;
 } PEB, * PPEB;

 PROCESS_BASIC_INFORMATION Self;


 NtQueryInformationProcess QueryNt;

 typedef struct _LDR_DATA_TABLE_ENTRY {
     PVOID Reserved1[2];
     LIST_ENTRY InMemoryOrderLinks;
     PVOID Reserved2[2];
     PVOID DllBase;
     PVOID EntryPoint;
     PVOID Reserved3;
     UNICODE_STRING FullDllName;
     BYTE Reserved4[8];
     PVOID Reserved5[3];
     union {
         ULONG CheckSum;
         PVOID Reserved6;
     };
     ULONG TimeDateStamp;
 } LDR_DATA_TABLE_ENTRY, * PLDR_DATA_TABLE_ENTRY;

 void RemoveList(const wchar_t* remove_me)
 {
     PEB* SelfBlock = Self.PebBaseAddress;

     LIST_ENTRY* List = &SelfBlock->Ldr->InMemoryOrderModuleList;
     LIST_ENTRY* LastList = NULL;
     LDR_DATA_TABLE_ENTRY* CurrentEntry;
     while (true)
     {
         if (List->Flink != NULL)
         {
             CurrentEntry = (LDR_DATA_TABLE_ENTRY*)List->Flink;
             LastList = List;
             List = List->Flink;
             wprintf(L"%s\r\n", CurrentEntry->FullDllName.Buffer);
             if (remove_me != 0)
             {
                 if (CurrentEntry->FullDllName.Buffer != NULL)
                 {
                     if (wcsstr(CurrentEntry->FullDllName.Buffer, remove_me) != 0)
                     {
                         LastList->Flink = List->Flink;
                         
                     }
                 }
                 else
                 {
                     break;
                 }
             }
             else
             {
                 if (CurrentEntry->FullDllName.Buffer == 0)
                 {
                     break;
                 }
             }
         }
         else
         {
             break;
         }


     }

     
 }
int main()
{
	user32 = LoadLibrary(L"user32.dll");
	NTDLL = LoadLibrary(L"ntdll.dll");
	QueryNt = (NtQueryInformationProcess)GetProcAddress(NTDLL, "NtQueryInformationProcess");
    DWORD Len = 0;
    QueryNt(GetCurrentProcess(), ProcessBasicInformation, &Self, sizeof(Self), &Len);


    RemoveList(L"user32.dll");

    RemoveList(0);
	return 0;
}