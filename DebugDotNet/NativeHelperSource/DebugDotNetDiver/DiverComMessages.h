#pragma once

#ifndef DIVERCOM_VERSION

/*
	Diver Talks to its debugger with SEH and RaiseException() it expects the debugger to set a pointer
	it gives to a different value to indicate if the message was understand / handled in addition to normal SEH stuff.

*/

// version this header file contains. Change this when breaking changes happen
#define DIVERCOM_VERSION  (1)

#ifndef MESSAGE_TEMPLATE_DEFINES
#define MESSAGE_TEMPLATE_DEFINES

// STARTER for MESSAGES from this Dll to the debugger are thrown with this as the ExceptionCode
#define DIVERCOM_BASEMSG_TEMPLATE (0xFFFFFFFF)
#define CLEAR_BIT28 (~(1 << 28))

// All Messages are made from this one
#define DIVERCOM_BASEMSG (DIVERCOM_BASEMSG_TEMPLATE & CLEAR_BIT28 )
#endif


#ifndef DIVERCOM_SET_VERSION
// tell the debugger what version we are using.. Args[0] is DIVERCOM_VERSION. Arg[1] points to an int that debugger sets to non-zero to enable the dll.
#define DIVERCOM_SETVERSION ((DIVERCOM_BASEMSG-1) & CLEAR_BIT28)

// this is the index into the ULONG_OG that contains a 4 byte pointer that the debugger needs to change with WriteProcessMemory()
#define DIVERCOM_SETVERSION_REPLY_PTR (0)
#define DIVERCOM_SETVERSION_TARGET_VERSION (1)
#endif // DIVERCOM_SET_VERSION defines


#ifndef DIVERCOM_EXCEPTION_FUNC_CALL

// is raised when a detoured function is called. Exception contains argument data in debugged process
// pointers given to the debugger are in the contect of the process rasining the exceoption
#define DIVERCOM_EXCEPTION_FUNC_CALL ((DIVERCOM_BASEMSG-2) & CLEAR_BIT28)
#endif  // DIVERCOM_EXCEPTION_FUNC_CALL


// this is a special debug message that is used as an alternative for OutputDebugString()
#define DIVERCOM_EXCEPTION_DEBUG_MSG ((DIVERCOM_BASEMSG-3) & CLEAR_BIT28)

// this is a message that informs the debugger that a Win32 Handle has been generated by a function we have detoured
#define DIVERCOM_EXCECPTION_NOTIFY_RESOURCE ((DIVERCOM_BASEMSG-4) & CLEAR_BIT28)

// ALL messages sent by Diver need the pointer this contains set to non-zero when debugger is finished
#define DIVERCOM_DEBUGSEEN_IT_ARG_ENTRY (0)
// DIVEROMCOM_EXCEPTION_FUNC_CALL Param.  This points to an array of pointers to the arguments for the function
#define DIVERCOM_ARGVECTOR_CONTENT (1)
// DIVEROMCOM_EXCEPTION_FUNC_CALL Param. Points to hints how to interpret the data 
#define DIVERCOM_ARGVECTOR_HINTS (2)
// DIVEROMCOM_EXCEPTION_FUNC_CALL. NOT A POINTER. Contains the legth of the vector for DIVERCOM_ARGVECTOR_CONTENT.  DIVERCOM_ARGVECTOR_HINTS is assumes to be the same length
#define DIVERCOM_ARGVECTOR_SIZE (3)
// DIVEROMCOM_EXCEPTION_FUNC_CALL. Subject to change. points to sizeof pointer for the debugged process
#define DIVERCOM_RETPTR_SIZE (4)
// DIVEROMCOM_EXCEPTION_FUNC_CALL. Subject to change. Debugger is expected to modifiy this.
#define DIVERCOM_DEBUGGER_PTR_STRUCT (5)


#endif
