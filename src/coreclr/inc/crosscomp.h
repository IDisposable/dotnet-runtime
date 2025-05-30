// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// crosscomp.h - cross-compilation enablement structures.
//

#pragma once

#include <minipal/mutex.h>

#if (!defined(HOST_64BIT) && defined(TARGET_64BIT)) || (defined(HOST_64BIT) && !defined(TARGET_64BIT))
#define CROSSBITNESS_COMPILE

#ifndef CROSS_COMPILE
#define CROSS_COMPILE
#endif // !CROSS_COMPILE

#endif

#if defined(TARGET_WINDOWS) && !defined(HOST_WINDOWS) && !defined(CROSS_COMPILE)
#define CROSS_COMPILE
#endif // TARGET_WINDOWS && !HOST_WINDOWS && !CROSS_COMPILE

#if defined(TARGET_UNIX) && !defined(HOST_UNIX) && !defined(CROSS_COMPILE)
#define CROSS_COMPILE
#endif // TARGET_UNIX && !HOST_UNIX && !CROSS_COMPILE

// Target platform-specific library naming
//
#ifdef TARGET_WINDOWS
#define MAKE_TARGET_DLLNAME_W(name) name W(".dll")
#define MAKE_TARGET_DLLNAME_A(name) name ".dll"
#else // TARGET_WINDOWS
#ifdef TARGET_APPLE
#define MAKE_TARGET_DLLNAME_W(name) W("lib") name W(".dylib")
#define MAKE_TARGET_DLLNAME_A(name)  "lib" name  ".dylib"
#else
#define MAKE_TARGET_DLLNAME_W(name) W("lib") name W(".so")
#define MAKE_TARGET_DLLNAME_A(name)  "lib" name  ".so"
#endif
#endif // TARGET_WINDOWS

#ifdef UNICODE
#define MAKE_TARGET_DLLNAME(name) MAKE_TARGET_DLLNAME_W(name)
#else
#define MAKE_TARGET_DLLNAME(name) MAKE_TARGET_DLLNAME_A(name)
#endif

#if !defined(HOST_ARM) && defined(TARGET_ARM) // Non-ARM Host managing ARM related code

#ifndef CROSS_COMPILE
#define CROSS_COMPILE
#endif

#define ARM_MAX_BREAKPOINTS     8
#define ARM_MAX_WATCHPOINTS     1

#ifndef CONTEXT_UNWOUND_TO_CALL
#define CONTEXT_UNWOUND_TO_CALL 0x20000000
#endif

#if !defined(HOST_ARM64)
typedef struct _NEON128 {
    ULONGLONG Low;
    LONGLONG High;
} NEON128, *PNEON128;
#endif // !defined(HOST_ARM64)

typedef struct DECLSPEC_ALIGN(8) _T_CONTEXT {
    //
    // Control flags.
    //

    DWORD ContextFlags;

    //
    // Integer registers
    //

    DWORD R0;
    DWORD R1;
    DWORD R2;
    DWORD R3;
    DWORD R4;
    DWORD R5;
    DWORD R6;
    DWORD R7;
    DWORD R8;
    DWORD R9;
    DWORD R10;
    DWORD R11;
    DWORD R12;

    //
    // Control Registers
    //

    DWORD Sp;
    DWORD Lr;
    DWORD Pc;
    DWORD Cpsr;

    //
    // Floating Point/NEON Registers
    //

    DWORD Fpscr;
    DWORD Padding;
    union {
        NEON128 Q[16];
        ULONGLONG D[32];
        DWORD S[32];
    };

    //
    // Debug registers
    //

    DWORD Bvr[ARM_MAX_BREAKPOINTS];
    DWORD Bcr[ARM_MAX_BREAKPOINTS];
    DWORD Wvr[ARM_MAX_WATCHPOINTS];
    DWORD Wcr[ARM_MAX_WATCHPOINTS];

    DWORD Padding2[2];

} T_CONTEXT, *PT_CONTEXT;

//
// Define function table entry - a function table entry is generated for
// each frame function.
//

#if defined(HOST_WINDOWS)
typedef struct _T_RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD UnwindData;
} T_RUNTIME_FUNCTION, *PT_RUNTIME_FUNCTION;
#else // HOST_WINDOWS
#define T_RUNTIME_FUNCTION RUNTIME_FUNCTION
#define PT_RUNTIME_FUNCTION PRUNTIME_FUNCTION
#endif // HOST_WINDOWS

//
// Nonvolatile context pointer record.
//

typedef struct _T_KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD R4;
    PDWORD R5;
    PDWORD R6;
    PDWORD R7;
    PDWORD R8;
    PDWORD R9;
    PDWORD R10;
    PDWORD R11;
    PDWORD Lr;

    PULONGLONG D8;
    PULONGLONG D9;
    PULONGLONG D10;
    PULONGLONG D11;
    PULONGLONG D12;
    PULONGLONG D13;
    PULONGLONG D14;
    PULONGLONG D15;

} T_KNONVOLATILE_CONTEXT_POINTERS, *PT_KNONVOLATILE_CONTEXT_POINTERS;

//
// Define dynamic function table entry.
//
#if defined(HOST_X86)
typedef
PT_RUNTIME_FUNCTION
(*PGET_RUNTIME_FUNCTION_CALLBACK) (
    IN DWORD64 ControlPc,
    IN PVOID Context
    );
#endif // defined(HOST_X86)

typedef struct _T_DISPATCHER_CONTEXT {
    ULONG ControlPc;
    ULONG ImageBase;
    PT_RUNTIME_FUNCTION FunctionEntry;
    ULONG EstablisherFrame;
    ULONG TargetPc;
    PT_CONTEXT ContextRecord;
    PEXCEPTION_ROUTINE LanguageHandler;
    PVOID HandlerData;
    PVOID HistoryTable;
    ULONG ScopeIndex;
    BOOLEAN ControlPcIsUnwound;
    PUCHAR NonVolatileRegisters;
} T_DISPATCHER_CONTEXT, *PT_DISPATCHER_CONTEXT;


#elif defined(HOST_AMD64) && defined(TARGET_ARM64)  // Host amd64 managing ARM64 related code

#ifndef CROSS_COMPILE
#define CROSS_COMPILE
#endif

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define ARM64_MAX_BREAKPOINTS     8
#define ARM64_MAX_WATCHPOINTS     2

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

typedef union _NEON128 {
    struct {
        ULONGLONG Low;
        LONGLONG High;
    };
    double D[2];
    float S[4];
    WORD   H[8];
    BYTE  B[16];
} NEON128, *PNEON128;

typedef struct DECLSPEC_ALIGN(16) _T_CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //

    /* +0x004 */ DWORD Cpsr;       // NZVF + DAIF + CurrentEL + SPSel
    /* +0x008 */ union {
                    struct {
                        DWORD64 X0;
                        DWORD64 X1;
                        DWORD64 X2;
                        DWORD64 X3;
                        DWORD64 X4;
                        DWORD64 X5;
                        DWORD64 X6;
                        DWORD64 X7;
                        DWORD64 X8;
                        DWORD64 X9;
                        DWORD64 X10;
                        DWORD64 X11;
                        DWORD64 X12;
                        DWORD64 X13;
                        DWORD64 X14;
                        DWORD64 X15;
                        DWORD64 X16;
                        DWORD64 X17;
                        DWORD64 X18;
                        DWORD64 X19;
                        DWORD64 X20;
                        DWORD64 X21;
                        DWORD64 X22;
                        DWORD64 X23;
                        DWORD64 X24;
                        DWORD64 X25;
                        DWORD64 X26;
                        DWORD64 X27;
                        DWORD64 X28;
                    };
                    DWORD64 X[29];
                 };
    /* +0x0f0 */ DWORD64 Fp;
    /* +0x0f8 */ DWORD64 Lr;
    /* +0x100 */ DWORD64 Sp;
    /* +0x108 */ DWORD64 Pc;

    //
    // Floating Point/NEON Registers
    //

    /* +0x110 */ NEON128 V[32];
    /* +0x310 */ DWORD Fpcr;
    /* +0x314 */ DWORD Fpsr;

    //
    // Debug registers
    //

    /* +0x318 */ DWORD Bcr[ARM64_MAX_BREAKPOINTS];
    /* +0x338 */ DWORD64 Bvr[ARM64_MAX_BREAKPOINTS];
    /* +0x378 */ DWORD Wcr[ARM64_MAX_WATCHPOINTS];
    /* +0x380 */ DWORD64 Wvr[ARM64_MAX_WATCHPOINTS];
    /* +0x390 */

} T_CONTEXT, *PT_CONTEXT;

// _IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
#ifdef HOST_UNIX
typedef struct _IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY {
    DWORD BeginAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD RegF : 3;
            DWORD RegI : 4;
            DWORD H : 1;
            DWORD CR : 2;
            DWORD FrameSize : 9;
        };
    };
} IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY, * PIMAGE_ARM64_RUNTIME_FUNCTION_ENTRY;



typedef
EXCEPTION_DISPOSITION
(*PEXCEPTION_ROUTINE) (
    PEXCEPTION_RECORD ExceptionRecord,
    ULONG64 EstablisherFrame,
    PCONTEXT ContextRecord,
    PVOID DispatcherContext
    );
#endif

typedef IMAGE_ARM64_RUNTIME_FUNCTION_ENTRY T_RUNTIME_FUNCTION, * PT_RUNTIME_FUNCTION;
//
// Define exception dispatch context structure.
//

typedef struct _T_DISPATCHER_CONTEXT {
    DWORD64 ControlPc;
    DWORD64 ImageBase;
    PT_RUNTIME_FUNCTION FunctionEntry;
    DWORD64 EstablisherFrame;
    DWORD64 TargetPc;
    PCONTEXT ContextRecord;
    PEXCEPTION_ROUTINE LanguageHandler;
    PVOID HandlerData;
    PVOID HistoryTable;
    DWORD ScopeIndex;
    BOOLEAN ControlPcIsUnwound;
    PBYTE  NonVolatileRegisters;
} T_DISPATCHER_CONTEXT, *PT_DISPATCHER_CONTEXT;



//
// Nonvolatile context pointer record.
//

typedef struct _T_KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 X19;
    PDWORD64 X20;
    PDWORD64 X21;
    PDWORD64 X22;
    PDWORD64 X23;
    PDWORD64 X24;
    PDWORD64 X25;
    PDWORD64 X26;
    PDWORD64 X27;
    PDWORD64 X28;
    PDWORD64 Fp;
    PDWORD64 Lr;

    PDWORD64 D8;
    PDWORD64 D9;
    PDWORD64 D10;
    PDWORD64 D11;
    PDWORD64 D12;
    PDWORD64 D13;
    PDWORD64 D14;
    PDWORD64 D15;

} T_KNONVOLATILE_CONTEXT_POINTERS, *PT_KNONVOLATILE_CONTEXT_POINTERS;

#if defined(HOST_UNIX) && defined(TARGET_ARM64) && !defined(HOST_ARM64)
enum
{
    UNW_AARCH64_X19 = 19,
    UNW_AARCH64_X20 = 20,
    UNW_AARCH64_X21 = 21,
    UNW_AARCH64_X22 = 22,
    UNW_AARCH64_X23 = 23,
    UNW_AARCH64_X24 = 24,
    UNW_AARCH64_X25 = 25,
    UNW_AARCH64_X26 = 26,
    UNW_AARCH64_X27 = 27,
    UNW_AARCH64_X28 = 28,
    UNW_AARCH64_X29 = 29,
    UNW_AARCH64_X30 = 30,
    UNW_AARCH64_SP = 31,
    UNW_AARCH64_PC = 32
};

#endif // TARGET_ARM64 && !HOST_ARM64

#elif defined(HOST_AMD64) && defined(TARGET_LOONGARCH64)  // Host amd64 managing LOONGARCH64 related code

#ifndef CROSS_COMPILE
#define CROSS_COMPILE
#endif

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, LOONGARCH64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define LOONGARCH64_MAX_BREAKPOINTS     8
#define LOONGARCH64_MAX_WATCHPOINTS     2

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

typedef struct DECLSPEC_ALIGN(16) _T_CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //
    DWORD64 R0;
    DWORD64 Ra;
    DWORD64 Tp;
    DWORD64 Sp;
    DWORD64 A0;
    DWORD64 A1;
    DWORD64 A2;
    DWORD64 A3;
    DWORD64 A4;
    DWORD64 A5;
    DWORD64 A6;
    DWORD64 A7;
    DWORD64 T0;
    DWORD64 T1;
    DWORD64 T2;
    DWORD64 T3;
    DWORD64 T4;
    DWORD64 T5;
    DWORD64 T6;
    DWORD64 T7;
    DWORD64 T8;
    DWORD64 X0;
    DWORD64 Fp;
    DWORD64 S0;
    DWORD64 S1;
    DWORD64 S2;
    DWORD64 S3;
    DWORD64 S4;
    DWORD64 S5;
    DWORD64 S6;
    DWORD64 S7;
    DWORD64 S8;
    DWORD64 Pc;

    //
    // Floating Point Registers: FPR64/LSX/LASX.
    //
    ULONGLONG F[4*32];
    DWORD64 Fcc;
    DWORD   Fcsr;
} T_CONTEXT, *PT_CONTEXT;

// _IMAGE_LOONGARCH64_RUNTIME_FUNCTION_ENTRY (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
typedef struct _T_RUNTIME_FUNCTION {
    DWORD BeginAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD RegF : 3;
            DWORD RegI : 4;
            DWORD H : 1;
            DWORD CR : 2;
            DWORD FrameSize : 9;
        } PackedUnwindData;
    };
} T_RUNTIME_FUNCTION, *PT_RUNTIME_FUNCTION;

//
// Define exception dispatch context structure.
//

typedef struct _T_DISPATCHER_CONTEXT {
    DWORD64 ControlPc;
    DWORD64 ImageBase;
    PT_RUNTIME_FUNCTION FunctionEntry;
    DWORD64 EstablisherFrame;
    DWORD64 TargetPc;
    PCONTEXT ContextRecord;
    PEXCEPTION_ROUTINE LanguageHandler;
    PVOID HandlerData;
    PVOID HistoryTable;
    DWORD ScopeIndex;
    BOOLEAN ControlPcIsUnwound;
    PBYTE  NonVolatileRegisters;
} T_DISPATCHER_CONTEXT, *PT_DISPATCHER_CONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _T_KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 S0;
    PDWORD64 S1;
    PDWORD64 S2;
    PDWORD64 S3;
    PDWORD64 S4;
    PDWORD64 S5;
    PDWORD64 S6;
    PDWORD64 S7;
    PDWORD64 S8;
    PDWORD64 Fp;
    PDWORD64 Ra;

    PDWORD64 F24;
    PDWORD64 F25;
    PDWORD64 F26;
    PDWORD64 F27;
    PDWORD64 F28;
    PDWORD64 F29;
    PDWORD64 F30;
    PDWORD64 F31;
} T_KNONVOLATILE_CONTEXT_POINTERS, *PT_KNONVOLATILE_CONTEXT_POINTERS;

#elif defined(HOST_AMD64) && defined(TARGET_RISCV64)  // Host amd64 managing RISCV64 related code

#ifndef CROSS_COMPILE
#define CROSS_COMPILE
#endif

//
// Specify the number of breakpoints and watchpoints that the OS
// will track.
//

#define RISCV64_MAX_BREAKPOINTS     8
#define RISCV64_MAX_WATCHPOINTS     2

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

typedef struct DECLSPEC_ALIGN(16) _T_CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //
    DWORD64 R0;
    DWORD64 Ra;
    DWORD64 Sp;
    DWORD64 Gp;
    DWORD64 Tp;
    DWORD64 T0;
    DWORD64 T1;
    DWORD64 T2;
    DWORD64 Fp;
    DWORD64 S1;
    DWORD64 A0;
    DWORD64 A1;
    DWORD64 A2;
    DWORD64 A3;
    DWORD64 A4;
    DWORD64 A5;
    DWORD64 A6;
    DWORD64 A7;
    DWORD64 S2;
    DWORD64 S3;
    DWORD64 S4;
    DWORD64 S5;
    DWORD64 S6;
    DWORD64 S7;
    DWORD64 S8;
    DWORD64 S9;
    DWORD64 S10;
    DWORD64 S11;
    DWORD64 T3;
    DWORD64 T4;
    DWORD64 T5;
    DWORD64 T6;
    DWORD64 Pc;

    //
    // Floating Point Registers
    //
    //TODO-RISCV64: support the SIMD.
    ULONGLONG F[32];
    DWORD Fcsr;
} T_CONTEXT, *PT_CONTEXT;

// _IMAGE_RISCV64_RUNTIME_FUNCTION_ENTRY (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
typedef struct _T_RUNTIME_FUNCTION {
    DWORD BeginAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD RegF : 3;
            DWORD RegI : 4;
            DWORD H : 1;
            DWORD CR : 2;
            DWORD FrameSize : 9;
        } PackedUnwindData;
    };
} T_RUNTIME_FUNCTION, *PT_RUNTIME_FUNCTION;

//
// Define exception dispatch context structure.
//

typedef struct _T_DISPATCHER_CONTEXT {
    DWORD64 ControlPc;
    DWORD64 ImageBase;
    PT_RUNTIME_FUNCTION FunctionEntry;
    DWORD64 EstablisherFrame;
    DWORD64 TargetPc;
    PCONTEXT ContextRecord;
    PEXCEPTION_ROUTINE LanguageHandler;
    PVOID HandlerData;
    PVOID HistoryTable;
    DWORD ScopeIndex;
    BOOLEAN ControlPcIsUnwound;
    PBYTE  NonVolatileRegisters;
} T_DISPATCHER_CONTEXT, *PT_DISPATCHER_CONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _T_KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 S1;
    PDWORD64 S2;
    PDWORD64 S3;
    PDWORD64 S4;
    PDWORD64 S5;
    PDWORD64 S6;
    PDWORD64 S7;
    PDWORD64 S8;
    PDWORD64 S9;
    PDWORD64 S10;
    PDWORD64 S11;
    PDWORD64 Fp;
    PDWORD64 Gp;
    PDWORD64 Tp;
    PDWORD64 Ra;

    PDWORD64 F8;
    PDWORD64 F9;
    PDWORD64 F18;
    PDWORD64 F19;
    PDWORD64 F20;
    PDWORD64 F21;
    PDWORD64 F22;
    PDWORD64 F23;
    PDWORD64 F24;
    PDWORD64 F25;
    PDWORD64 F26;
    PDWORD64 F27;
} T_KNONVOLATILE_CONTEXT_POINTERS, *PT_KNONVOLATILE_CONTEXT_POINTERS;

#else

#define T_CONTEXT CONTEXT
#define PT_CONTEXT PCONTEXT

#define T_DISPATCHER_CONTEXT DISPATCHER_CONTEXT
#define PT_DISPATCHER_CONTEXT PDISPATCHER_CONTEXT

#if defined(HOST_WINDOWS) && defined(TARGET_X86)
typedef struct _KNONVOLATILE_CONTEXT {

    DWORD Edi;
    DWORD Esi;
    DWORD Ebx;
    DWORD Ebp;

} KNONVOLATILE_CONTEXT, *PKNONVOLATILE_CONTEXT;

typedef struct _KNONVOLATILE_CONTEXT_POINTERS_EX
{
    // The ordering of these fields should be aligned with that
    // of corresponding fields in CONTEXT
    //
    // (See FillRegDisplay in inc/regdisp.h for details)
    PDWORD Edi;
    PDWORD Esi;
    PDWORD Ebx;
    PDWORD Edx;
    PDWORD Ecx;
    PDWORD Eax;

    PDWORD Ebp;

} KNONVOLATILE_CONTEXT_POINTERS_EX, *PKNONVOLATILE_CONTEXT_POINTERS_EX;

#define KNONVOLATILE_CONTEXT_POINTERS KNONVOLATILE_CONTEXT_POINTERS_EX
#define PKNONVOLATILE_CONTEXT_POINTERS PKNONVOLATILE_CONTEXT_POINTERS_EX
#endif
#define T_KNONVOLATILE_CONTEXT_POINTERS KNONVOLATILE_CONTEXT_POINTERS
#define PT_KNONVOLATILE_CONTEXT_POINTERS PKNONVOLATILE_CONTEXT_POINTERS

#define T_RUNTIME_FUNCTION RUNTIME_FUNCTION
#define PT_RUNTIME_FUNCTION PRUNTIME_FUNCTION

#endif

#if defined(TARGET_APPLE)
#define DAC_MUTEX_MAX_SIZE 96
#elif defined(TARGET_FREEBSD)
#define DAC_MUTEX_MAX_SIZE 16
#elif defined(TARGET_LINUX) || defined(TARGET_ANDROID)
#define DAC_MUTEX_MAX_SIZE 64
#elif defined(TARGET_WINDOWS)
#ifdef TARGET_64BIT
#define DAC_MUTEX_MAX_SIZE 40
#else
#define DAC_MUTEX_MAX_SIZE 24
#endif // TARGET_64BIT
#else
// Fallback to a conservative default value
#define DAC_MUTEX_MAX_SIZE 128
#endif

#ifndef CROSS_COMPILE
static_assert(DAC_MUTEX_MAX_SIZE >= sizeof(minipal_mutex), "DAC_MUTEX_MAX_SIZE must be greater than or equal to the size of minipal_mutex");
#endif // !CROSS_COMPILE

// This type is used to ensure a consistent size of mutexes
// contained with our Crst types.
// We have this requirement for cross OS compiling the DAC.
struct tgt_minipal_mutex final
{
    union
    {
        // DAC builds want to have the data layout of the target system.
        // Make sure that the host minipal_mutex does not influence
        // the target data layout
#ifndef DACCESS_COMPILE
        minipal_mutex _mtx;
#endif // !DACCESS_COMPILE

        // This is unused padding to ensure struct size.
        alignas(void*) BYTE _dacPadding[DAC_MUTEX_MAX_SIZE];
    };
};
