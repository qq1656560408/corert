;; Licensed to the.NET Foundation under one or more agreements.
;; The.NET Foundation licenses this file to you under the MIT license.
;; See the LICENSE file in the project root for more information.

;; OS provided macros
#include <ksarm.h>
;; generated by the build from AsmOffsets.cpp
#include "AsmOffsets.inc"

;;
;; CONSTANTS -- INTEGER
;;
TSF_Attached                    equ 0x01
TSF_SuppressGcStress            equ 0x08
TSF_DoNotTriggerGc              equ 0x10
TSF_SuppressGcStress__OR__TSF_DoNotTriggerGC equ 0x18

;; GC type flags
GC_ALLOC_FINALIZE               equ 1
GC_ALLOC_ALIGN8_BIAS            equ 4
GC_ALLOC_ALIGN8                 equ 8

;; GC minimal sized object. We use this to switch between 4 and 8 byte alignment in the GC heap (see AllocFast.asm).
SIZEOF__MinObject               equ 12
    ASSERT (SIZEOF__MinObject :MOD: 8) == 4

;; Note: these must match the defs in PInvokeTransitionFrameFlags
PTFF_SAVE_R4            equ 0x00000001
PTFF_SAVE_R5            equ 0x00000002
PTFF_SAVE_R6            equ 0x00000004
PTFF_SAVE_R7            equ 0x00000008
PTFF_SAVE_R8            equ 0x00000010
PTFF_SAVE_R9            equ 0x00000020
PTFF_SAVE_R10           equ 0x00000040
PTFF_SAVE_ALL_PRESERVED equ 0x00000077  ;; NOTE: FP is not included in this set!
PTFF_SAVE_SP            equ 0x00000100
PTFF_SAVE_R0            equ 0x00000200  ;; R0 is saved if it contains a GC ref and we're in hijack handler
PTFF_SAVE_ALL_SCRATCH   equ 0x00003e00  ;; R0-R3,LR (R12 is trashed by the helpers anyway, but LR is relevant for loop hijacking
PTFF_R0_IS_GCREF        equ 0x00004000  ;; iff PTFF_SAVE_R0: set -> r0 is Object, clear -> r0 is scalar
PTFF_R0_IS_BYREF        equ 0x00008000  ;; iff PTFF_SAVE_R0: set -> r0 is ByRef, clear -> r0 is Object or scalar


#ifndef CORERT; Hardcoded TLS offsets are not compatible with static linking
;;
;; This constant, unfortunately, cannot be validated at build time.
;;
OFFSETOF__TLS__tls_CurrentThread                    equ  0x10
#endif

;;
;; Rename fields of nested structs
;;
OFFSETOF__Thread__m_alloc_context__alloc_ptr        equ OFFSETOF__Thread__m_rgbAllocContextBuffer + OFFSETOF__alloc_context__alloc_ptr
OFFSETOF__Thread__m_alloc_context__alloc_limit      equ OFFSETOF__Thread__m_rgbAllocContextBuffer + OFFSETOF__alloc_context__alloc_limit


__tls_array     equ 0x2C    ;; offsetof(TEB, ThreadLocalStoragePointer)


;;
;; Offset from FP (r7) where the managed callout thunk (ManagedCallout2 and possibly others in the future)
;; store a pointer to a transition frame.
;;
MANAGED_CALLOUT_THUNK_TRANSITION_FRAME_POINTER_OFFSET equ -4

;;
;; MACROS
;;

    MACRO
        INLINE_GETTHREAD $destReg, $trashReg
        EXTERN _tls_index

        ldr         $destReg, =_tls_index
        ldr         $destReg, [$destReg]
        mrc         p15, 0, $trashReg, c13, c0, 2
        ldr         $trashReg,[$trashReg, #__tls_array]
        ldr         $destReg, [$trashReg, $destReg, lsl #2]
        add         $destReg, #OFFSETOF__TLS__tls_CurrentThread
    MEND

    MACRO
        INLINE_THREAD_UNHIJACK $threadReg, $trashReg1, $trashReg2
        ;;
        ;; Thread::Unhijack()
        ;;
        ldr         $trashReg1, [$threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cbz         $trashReg1, %ft0

        ldr         $trashReg2, [$threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         $trashReg1, [$trashReg2]
        mov         $trashReg1, #0
        str         $trashReg1, [$threadReg, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         $trashReg1, [$threadReg, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
0
    MEND

DEFAULT_FRAME_SAVE_FLAGS equ PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_SP

;;
;; Macro used from unmanaged helpers called from managed code where the helper does not transition immediately
;; into pre-emptive mode but may cause a GC and thus requires the stack is crawlable. This is typically the
;; case for helpers that meddle in GC state (e.g. allocation helpers) where the code must remain in
;; cooperative mode since it handles object references and internal GC state directly but a garbage collection
;; may be inevitable. In these cases we need to be able to transition to pre-meptive mode deep within the
;; unmanaged code but still be able to initialize the stack iterator at the first stack frame which may hold
;; interesting GC references. In all our helper cases this corresponds to the most recent managed frame (e.g.
;; the helper's caller).
;;
;; This macro builds a frame describing the current state of managed code and stashes a pointer to this frame
;; on the current thread, ready to be used if and when the helper needs to transition to pre-emptive mode.
;;
;; INVARIANTS
;; - The macro assumes it defines the method prolog, it should typically be the first code in a method and
;;   certainly appear before any attempt to alter the stack pointer.
;; - This macro uses r4 and r5 (after their initial values have been saved in the frame) and upon exit r4
;;   will contain the current Thread*.
;;
    MACRO
        COOP_PINVOKE_FRAME_PROLOG

        PROLOG_STACK_ALLOC 4        ; Save space for caller's SP
        PROLOG_PUSH {r4-r6,r8-r10}  ; Save preserved registers
        PROLOG_STACK_ALLOC 8        ; Save space for flags and Thread*
        PROLOG_PUSH {r7}            ; Save caller's FP
        PROLOG_PUSH {r11,lr}        ; Save caller's frame-chain pointer and PC

        ; Compute SP value at entry to this method and save it in the last slot of the frame (slot #11).
        add         r4, sp, #(12 * 4)
        str         r4, [sp, #(11 * 4)]

        ; Record the bitmask of saved registers in the frame (slot #4).
        mov         r4, #DEFAULT_FRAME_SAVE_FLAGS
        str         r4, [sp, #(4 * 4)]

        ; Save the current Thread * in the frame (slot #3).
        INLINE_GETTHREAD r4, r5
        str         r4, [sp, #(3 * 4)]

        ; Store the frame in the thread
        str         sp, [r4, #OFFSETOF__Thread__m_pHackPInvokeTunnel]
    MEND

;; Pop the frame and restore register state preserved by COOP_PINVOKE_FRAME_PROLOG but don't return to the
;; caller (typically used when we want to tail call instead).
    MACRO
        COOP_PINVOKE_FRAME_EPILOG_NO_RETURN

        ;; We do not need to clear m_pHackPInvokeTunnel here because it is 'on the side' information.
        ;; The actual transition to/from preemptive mode is done elsewhere (HackEnablePreemptiveMode,
        ;; HackDisablePreemptiveMode) and m_pHackPInvokeTunnel need only be valid when that happens,
        ;; so as long as we always set it on the way into a "cooperative pinvoke" method, we're fine
        ;; because it is only looked at inside these "cooperative pinvoke" methods.

        EPILOG_POP  {r11,lr}        ; Restore caller's frame-chain pointer and PC (return address)
        EPILOG_POP  {r7}            ; Restore caller's FP
        EPILOG_STACK_FREE 8         ; Discard flags and Thread*
        EPILOG_POP  {r4-r6,r8-r10}  ; Restore preserved registers
        EPILOG_STACK_FREE 4         ; Discard caller's SP
    MEND

;; The same as COOP_PINVOKE_FRAME_EPILOG_NO_RETURN but return to the location specified by the restored LR.
    MACRO
        COOP_PINVOKE_FRAME_EPILOG

        COOP_PINVOKE_FRAME_EPILOG_NO_RETURN
        EPILOG_RETURN
    MEND


; Macro used to assign an alternate name to a symbol containing characters normally disallowed in a symbol
; name (e.g. C++ decorated names).
    MACRO
      SETALIAS   $name, $symbol
        GBLS    $name
$name   SETS    "|$symbol|"
    MEND


        ;
        ; Helper macro: create a global label for the given name,
        ; decorate it, and export it for external consumption.
        ;

        MACRO
        __ExportLabelName $FuncName

        LCLS    Name
Name    SETS    "|$FuncName|"
        EXPORT  $Name
$Name
        MEND

        ;
        ; Macro for indicating an alternate entry point into a function.
        ;

        MACRO
        LABELED_RETURN_ADDRESS $ReturnAddressName

        ; export the return address name, but do not perturb the code by forcing alignment
        __ExportLabelName $ReturnAddressName

        ; flush any pending literal pool stuff
        ROUT

        MEND

;-----------------------------------------------------------------------------
; Macro used to check (in debug builds only) whether the stack is 64-bit aligned (a requirement before calling
; out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
; before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
; is misaligned.
;
    MACRO
        CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        push    {r0}
        add     r0, sp, #4
        tst     r0, #7
        pop     {r0}
        beq     %F0
        EMIT_BREAKPOINT
0
#endif
    MEND


;;
;; CONSTANTS -- SYMBOLS
;;

        SETALIAS PALDEBUGBREAK, ?PalDebugBreak@@YAXXZ
        SETALIAS REDHAWKGCINTERFACE__ALLOC, ?Alloc@RedhawkGCInterface@@SAPAXPAVThread@@IIPAVEEType@@@Z
        SETALIAS REDHAWKGCINTERFACE__GARBAGECOLLECT, ?GarbageCollect@RedhawkGCInterface@@SAXII@Z
        SETALIAS G_LOWEST_ADDRESS, g_lowest_address
        SETALIAS G_HIGHEST_ADDRESS, g_highest_address
        SETALIAS G_EPHEMERAL_LOW, g_ephemeral_low
        SETALIAS G_EPHEMERAL_HIGH, g_ephemeral_high
        SETALIAS G_CARD_TABLE, g_card_table
        SETALIAS THREADSTORE__ATTACHCURRENTTHREAD, ?AttachCurrentThread@ThreadStore@@SAXXZ
        SETALIAS G_FREE_OBJECT_EETYPE, ?g_pFreeObjectMethodTable@@3PAVMethodTable@@A
#ifdef FEATURE_GC_STRESS
        SETALIAS THREAD__HIJACKFORGCSTRESS, ?HijackForGcStress@Thread@@SAXPAUPAL_LIMITED_CONTEXT@@@Z
        SETALIAS REDHAWKGCINTERFACE__STRESSGC, ?StressGc@RedhawkGCInterface@@SAXXZ
#endif ;; FEATURE_GC_STRESS
;;
;; IMPORTS
;;
        EXTERN $REDHAWKGCINTERFACE__ALLOC
        EXTERN $REDHAWKGCINTERFACE__GARBAGECOLLECT
        EXTERN $THREADSTORE__ATTACHCURRENTTHREAD
        EXTERN $PALDEBUGBREAK
        EXTERN RhpPInvokeWaitEx
        EXTERN RhpPInvokeReturnWaitEx
        EXTERN RhExceptionHandling_FailedAllocation
        EXTERN RhpPublishObject
        EXTERN RhpCalculateStackTraceWorker


        EXTERN $G_LOWEST_ADDRESS
        EXTERN $G_HIGHEST_ADDRESS
        EXTERN $G_EPHEMERAL_LOW
        EXTERN $G_EPHEMERAL_HIGH
        EXTERN $G_CARD_TABLE
        EXTERN RhpTrapThreads
        EXTERN $G_FREE_OBJECT_EETYPE

        EXTERN RhThrowHwEx
        EXTERN RhThrowEx
        EXTERN RhRethrow

#ifdef FEATURE_GC_STRESS
        EXTERN $REDHAWKGCINTERFACE__STRESSGC
        EXTERN $THREAD__HIJACKFORGCSTRESS
#endif ;; FEATURE_GC_STRESS