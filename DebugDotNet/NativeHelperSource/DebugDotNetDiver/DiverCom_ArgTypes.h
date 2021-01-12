#pragma once
/*
	These flags are containing in the ArgType hint vector
*/

/// specifies that the argument is reserved in the routine's specifications and leave it alone
#define DIVERARG_RESERVED (0)

///specifies that the argument is a 4 byte value that is a series of flags
/// this flag must contain DIVERCOM_ACCESSFLAG_TYPE value and may contain a DIVER_ACCESSFLAG_MODIFIER value

#define DIVERARG_ACCESSFLAGS (1)

// tells the debugger that this is access flags for a file handle
#define DIVERARG_ACCCESSFLAG_FILE_TYPE (32)

/// tells the debugger that the argument flags belong to the process type
#define DIVERARG_ACCESSFLAG_PROCESS_TYPE (2)

// tells the diver call to strip delete flag before the function call
#define DIVERARG_ACCESSFLAG_MODIFER_NO_DELETE (4)

// tells diver call to strip write flags before the function call
#define DIVERARG_ACCESSFLAG_MODIFIER_NO_WRITE (8)


// tells the diver call to AND_BITs with the DebuggerResponse.Arg2 value.
#define DIVERACC_ACCESSFLAG_MODIFIER_ARG2 (16)

#define DIVE