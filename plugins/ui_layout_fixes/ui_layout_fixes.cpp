// ui_layout_fixes.cpp
//
// Window-specific UI corrections for Workers & Resources: Soviet Republic.
// Every module owns its addresses, validation, configuration and activation.
// There is intentionally no global row-spacing option.

// CUSTOMHOUSE module (SOVIET64.exe 1.1.1.9):
// The native resource-column renderer uses a 25-logical-pixel row pitch. The
// customs-house panel invokes that renderer once to measure its list and once
// to draw it. This module redirects the renderer's pitch operand to a neutral
// plugin-owned slot, then changes that slot only while either of those two
// verified CUSTOMHOUSE calls is executing. The measurement rectangle, scroll
// range and all sections below the list therefore continue to be positioned by
// the game's native layout code. Every other caller still reads exactly 25.0f.

#if __has_include("../../src/tesmio_plugin.h")
#include "../../src/tesmio_plugin.h"
#else
#include "../tesmio_plugin.h"
#endif

#include "../tesmio_config.h"
#include <float.h>
#include <limits.h>
#include <stdint.h>
#include <wchar.h>

#include <exception>

namespace UiLayoutFixes
{
static const char* const PLUGIN_VERSION = "1.2";
// Configuration comes through tesmio_config.h: <loader>\plugins\ui_layout_fixes.ini
static const char* const LOG_NAME = "tesmioloader.ui_layout_fixes.log";

static int g_enabled = 1;
static bool g_bound = false;
static HANDLE g_detail = INVALID_HANDLE_VALUE;
static volatile LONG g_logWarnings;
static volatile LONG g_logErrors;
static volatile LONG g_logFatals;
static ULONGLONG g_logStarted;
static ULONGLONG g_phaseStarted;
static LONG g_phaseWarnings;
static LONG g_phaseErrors;
static LONG g_phaseFatals;

// -------------------------------------------------------------------------
// Logging

static void DetailWriteLine(const char* text)
{
    if (g_detail == INVALID_HANDLE_VALUE || !text) return;

    if (g_bound) EnterCriticalSection(&g_lock);

    SYSTEMTIME time;
    GetLocalTime(&time);

    char stamp[64];
    _snprintf_s(stamp, sizeof(stamp), _TRUNCATE,
        "[%04d-%02d-%02d %02d:%02d:%02d.%03d] ",
        time.wYear, time.wMonth, time.wDay,
        time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);

    DWORD written = 0;
    WriteFile(g_detail, stamp, (DWORD)strlen(stamp), &written, nullptr);
    WriteFile(g_detail, text, (DWORD)strlen(text), &written, nullptr);
    WriteFile(g_detail, "\r\n", 2, &written, nullptr);

    if (g_bound) LeaveCriticalSection(&g_lock);
}

static void CountLevel(const char* level)
{
    if (!level) return;
    if (_stricmp(level, "WARN") == 0)
        InterlockedIncrement(&g_logWarnings);
    else if (_stricmp(level, "ERROR") == 0)
        InterlockedIncrement(&g_logErrors);
    else if (_stricmp(level, "FATAL") == 0)
        InterlockedIncrement(&g_logFatals);
}

static void ReportV(const char* level, const char* module, const char* rule,
                    const char* format, va_list args)
{
    char body[2048];
    _vsnprintf_s(body, sizeof(body), _TRUNCATE, format, args);

    char full[3072];
    _snprintf_s(full, sizeof(full), _TRUNCATE, "%s: %s [%s] %s",
        level ? level : "INFO", module ? module : "-",
        rule ? rule : "general", body);

    CountLevel(level);
    if (H) Logf("ui_layout_fixes  %s", full);
    DetailWriteLine(full);
}

static void LogInfo(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    ReportV("INFO", nullptr, "status", format, args);
    va_end(args);
}

static void LogWarning(const char* module, const char* rule,
                       const char* format, ...)
{
    va_list args;
    va_start(args, format);
    ReportV("WARN", module, rule, format, args);
    va_end(args);
}

static void LogError(const char* module, const char* rule,
                     const char* format, ...)
{
    va_list args;
    va_start(args, format);
    ReportV("ERROR", module, rule, format, args);
    va_end(args);
}

static void LogFatal(const char* module, const char* rule,
                     const char* format, ...)
{
    va_list args;
    va_start(args, format);
    ReportV("FATAL", module, rule, format, args);
    va_end(args);
}

static void ReportWindows(const char* level, const char* module,
                          const char* rule, const char* action,
                          DWORD error, const char* remedy)
{
    char systemText[512] = {};
    DWORD length = FormatMessageA(
        FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr, error, 0, systemText, (DWORD)sizeof(systemText), nullptr);
    while (length &&
           (systemText[length - 1] == '\r' || systemText[length - 1] == '\n' ||
            systemText[length - 1] == ' ' || systemText[length - 1] == '.'))
    {
        systemText[--length] = 0;
    }
    if (!length)
        strcpy_s(systemText, sizeof(systemText), "Unknown Windows error");

    char detail[1536];
    _snprintf_s(detail, sizeof(detail), _TRUNCATE,
        "%s (Windows error %lu: %s). Action: %s",
        action, error, systemText,
        remedy ? remedy : "Review the preceding context and retry");

    if (level && _stricmp(level, "WARN") == 0)
        LogWarning(module, rule, "%s", detail);
    else if (level && _stricmp(level, "FATAL") == 0)
        LogFatal(module, rule, "%s", detail);
    else
        LogError(module, rule, "%s", detail);
}

static LONG AtomicRead(volatile LONG* value)
{
    return InterlockedCompareExchange(value, 0, 0);
}

static void BeginLogPhase()
{
    g_phaseStarted = GetTickCount64();
    g_phaseWarnings = AtomicRead(&g_logWarnings);
    g_phaseErrors = AtomicRead(&g_logErrors);
    g_phaseFatals = AtomicRead(&g_logFatals);
}

static void LogSummaryStatus(const char* phase, const char* status)
{
    const ULONGLONG elapsed =
        g_phaseStarted ? GetTickCount64() - g_phaseStarted : 0;
    LogInfo("%s %s after %llu ms; %ld warning(s), %ld error(s), "
            "%ld fatal error(s)",
        phase, status, (unsigned long long)elapsed,
        AtomicRead(&g_logWarnings) - g_phaseWarnings,
        AtomicRead(&g_logErrors) - g_phaseErrors,
        AtomicRead(&g_logFatals) - g_phaseFatals);
}

static void LogSummary(const char* phase, bool success)
{
    LogSummaryStatus(phase,
        success ? "completed successfully" : "failed");
}

// -------------------------------------------------------------------------
// Configuration and executable validation

static bool ReadIniFloat(const char* section, const char* key,
                         float fallback, float minimum, float maximum,
                         float* output)
{
    if (!output) return false;

    char fallbackText[64];
    char valueText[128];
    _snprintf_s(fallbackText, sizeof(fallbackText), _TRUNCATE,
                "%.3f", fallback);
    if (!TsmConfigString(section, key, valueText, (int)sizeof(valueText), fallbackText))
    {
        *output = fallback;
        return true;
    }

    char* end = nullptr;
    errno = 0;
    const double parsed = strtod(valueText, &end);
    const int parseError = errno;
    while (end && (*end == ' ' || *end == '\t' ||
                   *end == '\r' || *end == '\n'))
    {
        ++end;
    }

    if (!end || end == valueText || *end != 0 || parseError == ERANGE ||
        _finite(parsed) == 0 ||
        parsed < minimum || parsed > maximum)
    {
        LogWarning(section, "invalid-config",
            "%s=%s is invalid; expected a finite value in %.1f..%.1f. "
            "Action: using %.1f",
            key, valueText, minimum, maximum, fallback);
        *output = fallback;
        return false;
    }

    *output = (float)parsed;
    return true;
}

static DWORD ExeTimestamp()
{
    if (!g_exeBase || g_exeSize < sizeof(IMAGE_DOS_HEADER) ||
        !ReadablePtr(g_exeBase, sizeof(IMAGE_DOS_HEADER)))
        return 0;

    const IMAGE_DOS_HEADER* dos = (const IMAGE_DOS_HEADER*)g_exeBase;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE || dos->e_lfanew <= 0)
        return 0;

    const SIZE_T ntOffset = (SIZE_T)dos->e_lfanew;
    if (ntOffset > g_exeSize ||
        sizeof(IMAGE_NT_HEADERS64) > g_exeSize - ntOffset)
    {
        return 0;
    }

    BYTE* ntAddress = g_exeBase + ntOffset;
    if (!ReadablePtr(ntAddress, sizeof(IMAGE_NT_HEADERS64)))
        return 0;

    const IMAGE_NT_HEADERS64* nt =
        (const IMAGE_NT_HEADERS64*)ntAddress;
    if (nt->Signature != IMAGE_NT_SIGNATURE ||
        nt->FileHeader.Machine != IMAGE_FILE_MACHINE_AMD64 ||
        nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
    {
        return 0;
    }
    return nt->FileHeader.TimeDateStamp;
}

static bool VerifyBytes(DWORD rva, const BYTE* expected, size_t length,
                        const char* module, const char* rule,
                        const char* label)
{
    if (!g_exeBase || !expected || length == 0 || rva > g_exeSize ||
        length > g_exeSize - rva ||
        !ReadablePtr(g_exeBase + rva, length))
    {
        LogError(module, rule,
            "%s at SOVIET64.exe+0x%X is outside the readable image",
            label, rva);
        return false;
    }

    if (memcmp(g_exeBase + rva, expected, length) != 0)
    {
        LogError(module, rule,
            "%s signature changed at SOVIET64.exe+0x%X; no patch was written",
            label, rva);
        return false;
    }
    return true;
}

static bool VerifyRelativeCall(DWORD callRva, DWORD targetRva,
                               const char* module, const char* rule,
                               const char* label)
{
    static const BYTE directCall = 0xE8;
    if (!VerifyBytes(callRva, &directCall, 1, module, rule, label))
        return false;

    if (callRva > g_exeSize || 5 > g_exeSize - callRva ||
        targetRva >= g_exeSize)
    {
        LogError(module, rule,
            "%s or its expected target is outside SOVIET64.exe",
            label);
        return false;
    }

    BYTE* call = g_exeBase + callRva;
    if (!ReadablePtr(call, 5))
    {
        LogError(module, rule,
            "%s at SOVIET64.exe+0x%X is not fully readable",
            label, callRva);
        return false;
    }

    LONG displacement = 0;
    memcpy(&displacement, call + 1, sizeof(displacement));
    const INT64 actualTarget =
        (INT64)(uintptr_t)(call + 5) + (INT64)displacement;
    const INT64 expectedTarget =
        (INT64)(uintptr_t)(g_exeBase + targetRva);
    if (actualTarget != expectedTarget)
    {
        LogError(module, rule,
            "%s at SOVIET64.exe+0x%X no longer targets +0x%X; no patch was written",
            label, callRva, targetRva);
        return false;
    }
    return true;
}

static bool Rel32(BYTE* instruction, BYTE* target, LONG* displacement)
{
    if (!instruction || !target || !displacement) return false;
    const INT64 distance = (INT64)(uintptr_t)target -
                           (INT64)(uintptr_t)(instruction + 5);
    if (distance < INT_MIN || distance > INT_MAX) return false;
    *displacement = (LONG)distance;
    return true;
}

static bool Rip32(BYTE* instructionEnd, BYTE* target, LONG* displacement)
{
    if (!instructionEnd || !target || !displacement) return false;
    const INT64 distance = (INT64)(uintptr_t)target -
                           (INT64)(uintptr_t)instructionEnd;
    if (distance < INT_MIN || distance > INT_MAX) return false;
    *displacement = (LONG)distance;
    return true;
}

static LONG FloatBits(float value)
{
    LONG bits = 0;
    static_assert(sizeof(bits) == sizeof(value),
                  "float and LONG must both contain 32 bits");
    memcpy(&bits, &value, sizeof(bits));
    return bits;
}

// -------------------------------------------------------------------------
// CUSTOMHOUSE

namespace Customhouse
{
static const DWORD EXPECTED_IMAGE_SIZE = 0x00A9D000;
static const DWORD EXPECTED_EXE_TIMESTAMP = 0x6A3EB6AD;

static const DWORD RVA_PANEL = 0x0071B410;
static const DWORD RVA_LIST_MEASURE_CALL = 0x0071C475;
static const DWORD RVA_LIST_DRAW_CALL = 0x0071C4B5;
static const DWORD RVA_RESOURCE_COLUMNS = 0x007C6720;
static const DWORD RVA_ROW_PITCH_INSTRUCTION = 0x007C678C;
static const DWORD RVA_ROW_PITCH_DISPLACEMENT = 0x007C6791;
static const DWORD RVA_ROW_PITCH_INSTRUCTION_END = 0x007C6795;

static const float NATIVE_ROW_PITCH = 25.0f;
static const float DEFAULT_ROW_PITCH = 30.0f;

static const BYTE EXPECT_PANEL[] = {
    0x48,0x8B,0xC4,0x55,0x56,0x57,0x41,0x54,
    0x41,0x55,0x41,0x56,0x41,0x57
};
static const BYTE EXPECT_ROW_PITCH_INSTRUCTION[] = {
    0xF3,0x44,0x0F,0x59,0x35,0xD3,0x41,0x14,0x00
};

// The first four parameters use the Windows x64 register positions. The
// native function obtains resources, optional bounds and the display flag from
// stack parameters five through seven.
typedef void (__fastcall *NativeResourceColumnsFn)(
    void* ui, float x, float y, void* reserved,
    void* resources, void* bounds, bool displayPrices);

static int g_enabled = 1;
static float g_configuredRowPitch = DEFAULT_ROW_PITCH;
static volatile LONG* g_rowPitchBits = nullptr;
static NativeResourceColumnsFn g_nativeResourceColumns = nullptr;

__declspec(noinline)
static void __fastcall ScopedResourceColumns(
    void* ui, float x, float y, void* reserved,
    void* resources, void* bounds, bool displayPrices)
{
    // Both hooked sites belong exclusively to CUSTOMHOUSE. The replacement is
    // synchronous; the original value is restored before control returns to
    // the panel. WRSR renders its windows on the owning UI thread.
    const LONG customBits = FloatBits(g_configuredRowPitch);
    const LONG previousBits =
        InterlockedExchange(g_rowPitchBits, customBits);

    g_nativeResourceColumns(ui, x, y, reserved,
                            resources, bounds, displayPrices);

    InterlockedExchange(g_rowPitchBits, previousBits);
}

static bool VerifyTargetBuild()
{
    const DWORD timestamp = ExeTimestamp();
    if (g_exeSize != EXPECTED_IMAGE_SIZE ||
        timestamp != EXPECTED_EXE_TIMESTAMP)
    {
        LogError("CUSTOMHOUSE", "unsupported-build",
            "expected SOVIET64.exe 1.1.1.9 "
            "(image=0x%X timestamp=0x%08X), found image=0x%llX timestamp=0x%08X; "
            "the module remains inactive",
            EXPECTED_IMAGE_SIZE, EXPECTED_EXE_TIMESTAMP,
            (unsigned long long)g_exeSize, timestamp);
        return false;
    }

    return VerifyBytes(RVA_PANEL, EXPECT_PANEL, sizeof(EXPECT_PANEL),
                       "CUSTOMHOUSE", "panel-signature",
                       "CUSTOMHOUSE panel") &&
           VerifyRelativeCall(RVA_LIST_MEASURE_CALL, RVA_RESOURCE_COLUMNS,
                       "CUSTOMHOUSE", "measure-call",
                       "resource-list measurement call") &&
           VerifyRelativeCall(RVA_LIST_DRAW_CALL, RVA_RESOURCE_COLUMNS,
                       "CUSTOMHOUSE", "draw-call",
                       "resource-list drawing call") &&
           VerifyBytes(RVA_ROW_PITCH_INSTRUCTION,
                       EXPECT_ROW_PITCH_INSTRUCTION,
                       sizeof(EXPECT_ROW_PITCH_INSTRUCTION),
                       "CUSTOMHOUSE", "row-pitch-signature",
                       "native resource-row pitch instruction");
}

static void BuildAbsoluteJump(BYTE* destination, void* target)
{
    BYTE code[16];
    memset(code, 0xCC, sizeof(code));
    code[0] = 0x48;
    code[1] = 0xB8; // mov rax, imm64
    memcpy(code + 2, &target, sizeof(target));
    code[10] = 0xFF;
    code[11] = 0xE0; // jmp rax
    memcpy(destination, code, sizeof(code));
}

static bool Install()
{
    if (!g_enabled)
    {
        LogInfo("[CUSTOMHOUSE] disabled by configuration");
        return true;
    }

    if (!VerifyTargetBuild()) return false;

    BYTE* measureCall = g_exeBase + RVA_LIST_MEASURE_CALL;
    BYTE* drawCall = g_exeBase + RVA_LIST_DRAW_CALL;
    BYTE* rowPitchInstruction =
        g_exeBase + RVA_ROW_PITCH_INSTRUCTION;

    // One nearby page holds a shared absolute-jump bridge and the aligned
    // float operand. The loader guarantees rel32 reach from the anchor.
    BYTE* bridgePage = AllocNear(measureCall, 128);
    if (!bridgePage)
    {
        LogError("CUSTOMHOUSE", "near-allocation",
            "could not allocate the verified call bridge; no patch was written");
        return false;
    }

    BYTE* callBridge = bridgePage;
    BYTE* pitchStorage = bridgePage + 64;
    BuildAbsoluteJump(callBridge, (void*)&ScopedResourceColumns);

    const LONG nativeBits = FloatBits(NATIVE_ROW_PITCH);
    *(LONG*)pitchStorage = nativeBits;
    g_rowPitchBits = (volatile LONG*)pitchStorage;
    g_nativeResourceColumns =
        (NativeResourceColumnsFn)(g_exeBase + RVA_RESOURCE_COLUMNS);

    LONG measureDisplacement = 0;
    LONG drawDisplacement = 0;
    LONG pitchDisplacement = 0;
    if (!Rel32(measureCall, callBridge, &measureDisplacement) ||
        !Rel32(drawCall, callBridge, &drawDisplacement) ||
        !Rip32(g_exeBase + RVA_ROW_PITCH_INSTRUCTION_END,
               pitchStorage, &pitchDisplacement))
    {
        LogError("CUSTOMHOUSE", "bridge-range",
            "the allocated bridge or row-pitch slot is outside rel32/RIP32 range; no patch was written");
        g_rowPitchBits = nullptr;
        g_nativeResourceColumns = nullptr;
        return false;
    }

    if (!FlushInstructionCache(GetCurrentProcess(), bridgePage, 128))
    {
        const DWORD error = GetLastError();
        ReportWindows("ERROR", "CUSTOMHOUSE", "bridge-cache",
            "Could not publish the generated call bridge to the instruction cache",
            error,
            "Restart the game and verify that security software is not blocking the plugin; no patch was written");
        g_rowPitchBits = nullptr;
        g_nativeResourceColumns = nullptr;
        return false;
    }

    // Both call sites share a page. Make every destination writable before
    // changing a byte so a protection failure cannot leave measurement and
    // drawing with different spacing.
    DWORD callPageProtection = 0;
    DWORD pitchPageProtection = 0;
    const SIZE_T callRange =
        (SIZE_T)((drawCall + 5) - measureCall);

    if (!VirtualProtect(measureCall, callRange, PAGE_EXECUTE_READWRITE,
                        &callPageProtection))
    {
        const DWORD error = GetLastError();
        ReportWindows("ERROR", "CUSTOMHOUSE", "call-protection",
            "Could not make the two verified CUSTOMHOUSE call sites writable",
            error,
            "Restart the game and check security software or conflicting UI plugins; no patch was written");
        g_rowPitchBits = nullptr;
        g_nativeResourceColumns = nullptr;
        return false;
    }

    if (!VirtualProtect(rowPitchInstruction,
                        sizeof(EXPECT_ROW_PITCH_INSTRUCTION),
                        PAGE_EXECUTE_READWRITE, &pitchPageProtection))
    {
        const DWORD error = GetLastError();
        DWORD ignored = 0;
        if (!VirtualProtect(measureCall, callRange,
                            callPageProtection, &ignored))
        {
            const DWORD restoreError = GetLastError();
            ReportWindows("WARN", "CUSTOMHOUSE", "call-protection-restore",
                "The call-site page could not be restored after patch preparation failed",
                restoreError,
                "No code was changed, but restart the game before changing plugins");
        }
        ReportWindows("ERROR", "CUSTOMHOUSE", "pitch-protection",
            "Could not make the verified native row-pitch operand writable",
            error,
            "Restart the game and check security software or conflicting UI plugins; no patch was written");
        g_rowPitchBits = nullptr;
        g_nativeResourceColumns = nullptr;
        return false;
    }

    // Redirect only the float operand; the native mulss instruction remains.
    memcpy(g_exeBase + RVA_ROW_PITCH_DISPLACEMENT,
           &pitchDisplacement, sizeof(pitchDisplacement));
    memcpy(measureCall + 1, &measureDisplacement,
           sizeof(measureDisplacement));
    memcpy(drawCall + 1, &drawDisplacement,
           sizeof(drawDisplacement));

    DWORD ignored = 0;
    const BOOL pitchRestored = VirtualProtect(
        rowPitchInstruction, sizeof(EXPECT_ROW_PITCH_INSTRUCTION),
        pitchPageProtection, &ignored);
    const DWORD pitchRestoreError =
        pitchRestored ? ERROR_SUCCESS : GetLastError();
    const BOOL callsRestored = VirtualProtect(
        measureCall, callRange, callPageProtection, &ignored);
    const DWORD callsRestoreError =
        callsRestored ? ERROR_SUCCESS : GetLastError();

    const BOOL pitchFlushed = FlushInstructionCache(
        GetCurrentProcess(), rowPitchInstruction,
        sizeof(EXPECT_ROW_PITCH_INSTRUCTION));
    const DWORD pitchFlushError =
        pitchFlushed ? ERROR_SUCCESS : GetLastError();
    const BOOL callsFlushed = FlushInstructionCache(
        GetCurrentProcess(), measureCall, callRange);
    const DWORD callsFlushError =
        callsFlushed ? ERROR_SUCCESS : GetLastError();

    // The patch is active at this point. A restoration or cache-flush failure
    // must therefore keep the DLL loaded and be reported as a warning rather
    // than making Start claim that no patch exists.
    if (!pitchRestored)
        ReportWindows("WARN", "CUSTOMHOUSE", "pitch-protection-restore",
            "The patch is active, but the row-pitch page protection could not be restored",
            pitchRestoreError,
            "Restart the game before changing plugins");
    if (!callsRestored)
        ReportWindows("WARN", "CUSTOMHOUSE", "call-protection-restore",
            "The patch is active, but the call-site page protection could not be restored",
            callsRestoreError,
            "Restart the game before changing plugins");
    if (!pitchFlushed)
        ReportWindows("WARN", "CUSTOMHOUSE", "pitch-cache",
            "The row-pitch patch could not be flushed from the instruction cache",
            pitchFlushError,
            "Restart the game if the layout correction is not visible");
    if (!callsFlushed)
        ReportWindows("WARN", "CUSTOMHOUSE", "call-cache",
            "The redirected call sites could not be flushed from the instruction cache",
            callsFlushError,
            "Restart the game if the layout correction is not visible");

    LogInfo("[CUSTOMHOUSE] active: resource row pitch %.1f -> %.1f logical px; scope=measure+draw calls only; native layout/scroll propagation retained",
            NATIVE_ROW_PITCH, g_configuredRowPitch);
    return true;
}

static void LoadConfig()
{
    g_enabled = TsmConfigInt("customhouse", "enabled", 1) != 0;
    ReadIniFloat("customhouse", "resource_row_pitch",
                 DEFAULT_ROW_PITCH, NATIVE_ROW_PITCH, 60.0f,
                 &g_configuredRowPitch);
}
} // namespace Customhouse

// -------------------------------------------------------------------------
// VEHICLE_ROUTE_HINT module
//
// The route hint of the vehicle window ("View area where a possible issue
// exists on route! ...", game text id 1970) is stored as two long lines in
// every language and runs past the right edge of the window. The module
// hooks the game's caption lookup C3D_LANGUAGE::GetString, which SOVIET64.exe
// imports from C3DDLL64.dll, through the import table and answers that one
// id with a word-wrapped copy. Every other id passes straight through.
// No executable code is changed, so the module does not depend on the
// verified game build; only the text id may move with a game update.
//
// Other plugins may hook the same import (the resources plugin does). The
// loader hands back whatever the slot held before, so the hooks chain in
// load order and each one only answers its own ids.

namespace VehicleRouteHint
{
static const char* const MODULE = "VEHICLE_ROUTE_HINT";
static const char* const SYM_GET_STRING =
    "?GetString@C3D_LANGUAGE@@QEAAPEA_WH@Z";

static const int DEFAULT_TEXT_ID = 1970;
static const int DEFAULT_MAX_CHARS = 58;
static const int DEFAULT_MAX_LINES = 4;
static const int MIN_TEXT_ID = 1;
static const int MAX_TEXT_ID = 100000;
static const int MIN_MAX_CHARS = 20;
static const int MAX_MAX_CHARS = 200;
static const int MIN_MAX_LINES = 0;
static const int MAX_MAX_LINES = 12;
static const size_t BUFFER_CHARS = 1024;   // wrapped copy including terminator

static int g_enabled = 1;
static int g_textId = DEFAULT_TEXT_ID;
static int g_maxChars = DEFAULT_MAX_CHARS;
static int g_maxLines = DEFAULT_MAX_LINES;
static bool g_installed = false;

typedef wchar_t* (*GetStringFn)(void* self, int id);
static GetStringFn o_GetString = nullptr;

// The source the wrapped copy was built from and the copy itself. The copy is
// rebuilt only when the game hands back a different string for the id (a
// language switch); the buffer address stays the same, so pointers the UI
// still holds remain valid. Guarded by g_lock from tesmio_plugin.h.
static wchar_t g_source[BUFFER_CHARS];
static wchar_t g_wrapped[BUFFER_CHARS];
static bool g_haveWrapped = false;
static bool g_tooLongReported = false;

static bool ReadIniInt(const char* section, const char* key,
                       int fallback, int minimum, int maximum, int* output)
{
    if (!output) return false;

    char fallbackText[32];
    char valueText[128];
    _snprintf_s(fallbackText, sizeof(fallbackText), _TRUNCATE, "%d", fallback);
    if (!TsmConfigString(section, key, valueText, (int)sizeof(valueText), fallbackText))
    {
        *output = fallback;
        return true;
    }

    char* end = nullptr;
    errno = 0;
    const long parsed = strtol(valueText, &end, 10);
    const int parseError = errno;
    while (end && (*end == ' ' || *end == '\t' ||
                   *end == '\r' || *end == '\n'))
    {
        ++end;
    }

    if (!end || end == valueText || *end != 0 || parseError == ERANGE ||
        parsed < minimum || parsed > maximum)
    {
        LogWarning(section, "invalid-config",
            "%s=%s is invalid; expected a whole number in %d..%d. "
            "Action: using %d",
            key, valueText, minimum, maximum, fallback);
        *output = fallback;
        return false;
    }

    *output = (int)parsed;
    return true;
}

struct Writer
{
    wchar_t* out;
    size_t capacity;
    size_t length;
    int lines;
    size_t widest;
};

static bool Put(Writer& w, wchar_t c)
{
    if (w.length + 1 >= w.capacity) return false;
    w.out[w.length++] = c;
    return true;
}

// Greedy word wrap of one paragraph. Words are never split; a word longer
// than the width stands on a line of its own.
static bool WrapParagraph(Writer& w, const wchar_t* text, size_t length,
                          size_t width)
{
    size_t lineLength = 0;
    size_t i = 0;
    while (i < length)
    {
        while (i < length && text[i] == L' ') ++i;
        if (i >= length) break;

        const size_t wordStart = i;
        while (i < length && text[i] != L' ') ++i;
        const size_t wordLength = i - wordStart;

        if (lineLength > 0)
        {
            if (lineLength + 1 + wordLength > width)
            {
                if (!Put(w, L'\n')) return false;
                ++w.lines;
                if (lineLength > w.widest) w.widest = lineLength;
                lineLength = 0;
            }
            else
            {
                if (!Put(w, L' ')) return false;
                ++lineLength;
            }
        }

        for (size_t k = 0; k < wordLength; ++k)
            if (!Put(w, text[wordStart + k])) return false;
        lineLength += wordLength;
    }
    if (lineLength > w.widest) w.widest = lineLength;
    return true;
}

// Dry run of WrapParagraph: how many lines does the paragraph take at `width`?
static int CountLines(const wchar_t* text, size_t length, size_t width)
{
    int lines = 1;
    size_t lineLength = 0;
    size_t i = 0;
    while (i < length)
    {
        while (i < length && text[i] == L' ') ++i;
        if (i >= length) break;
        const size_t wordStart = i;
        while (i < length && text[i] != L' ') ++i;
        const size_t wordLength = i - wordStart;
        if (lineLength > 0)
        {
            if (lineLength + 1 + wordLength > width)
            {
                ++lines;
                lineLength = 0;
            }
            else
            {
                ++lineLength;
            }
        }
        lineLength += wordLength;
    }
    return lines;
}

// The narrowest width that still wraps the paragraph into the same number of
// lines as `width` does. Greedy wrapping alone leaves a long first line and a
// short tail ("...Problem auf der" / "Route besteht!"); balancing spreads the
// words evenly without adding a line.
static size_t BalancedWidth(const wchar_t* text, size_t length, size_t width)
{
    const int lines = CountLines(text, length, width);
    if (lines < 2) return width;
    size_t best = width;
    for (size_t candidate = width - 1; candidate > 0; --candidate)
    {
        if (CountLines(text, length, candidate) != lines) break;
        best = candidate;
    }
    return best;
}

// Wraps the whole text at `width`. The game's own line breaks stay as
// paragraph breaks. Returns the line count, or -1 when the copy did not fit.
static int WrapText(const wchar_t* source, size_t width, wchar_t* out,
                    size_t capacity, size_t* widest)
{
    Writer w = { out, capacity, 0, 1, 0 };
    const wchar_t* p = source;
    bool first = true;
    for (;;)
    {
        const wchar_t* newline = wcschr(p, L'\n');
        size_t length = newline ? (size_t)(newline - p) : wcslen(p);
        if (length > 0 && p[length - 1] == L'\r') --length;

        if (!first)
        {
            if (!Put(w, L'\n')) return -1;
            ++w.lines;
        }
        first = false;
        if (!WrapParagraph(w, p, length, BalancedWidth(p, length, width)))
            return -1;
        if (!newline) break;
        p = newline + 1;
    }
    out[w.length] = 0;
    if (widest) *widest = w.widest;
    return w.lines;
}

static void MeasureSource(const wchar_t* source, int* lines, size_t* longest)
{
    *lines = 1;
    *longest = 0;
    size_t current = 0;
    for (const wchar_t* p = source; *p; ++p)
    {
        if (*p == L'\n')
        {
            ++*lines;
            current = 0;
        }
        else if (*p != L'\r')
        {
            ++current;
            if (current > *longest) *longest = current;
        }
    }
}

// Returns the wrapped copy of `original`, rebuilding it when the game's
// string changed. Falls back to the original when the text is too long for
// the buffer, so the game never loses the caption.
static const wchar_t* Wrapped(const wchar_t* original)
{
    EnterCriticalSection(&g_lock);
    if (!g_haveWrapped || wcscmp(original, g_source) != 0)
    {
        g_haveWrapped = false;
        const size_t sourceLength = wcslen(original);
        if (sourceLength + 1 > BUFFER_CHARS)
        {
            if (!g_tooLongReported)
            {
                g_tooLongReported = true;
                LogWarning(MODULE, "text-length",
                    "text id %d is %u characters long, the wrap buffer holds %u. "
                    "Action: the native text is shown unchanged",
                    g_textId, (unsigned)sourceLength, (unsigned)(BUFFER_CHARS - 1));
            }
        }
        else
        {
            wcscpy_s(g_source, BUFFER_CHARS, original);

            int sourceLines = 0;
            size_t longest = 0;
            MeasureSource(g_source, &sourceLines, &longest);

            // Start at the configured width; when the line limit is exceeded
            // widen step by step, never beyond the native line length.
            size_t width = (size_t)g_maxChars;
            size_t widest = 0;
            int lines = -1;
            for (;;)
            {
                lines = WrapText(g_source, width, g_wrapped, BUFFER_CHARS, &widest);
                if (lines < 0) break;
                if (g_maxLines <= 0 || lines <= g_maxLines || width >= longest) break;
                width += 4;
                if (width > longest) width = longest;
            }

            if (lines > 0)
            {
                g_haveWrapped = true;
                LogInfo("[%s] text id %d: native %d line(s), longest %u chars -> "
                        "%d line(s), widest %u chars at width %u",
                    MODULE, g_textId, sourceLines, (unsigned)longest,
                    lines, (unsigned)widest, (unsigned)width);
            }
        }
    }
    const wchar_t* result = g_haveWrapped ? g_wrapped : original;
    LeaveCriticalSection(&g_lock);
    return result;
}

static wchar_t* h_GetString(void* self, int id)
{
    wchar_t* text = o_GetString(self, id);
    if (id == g_textId && text && text[0])
        return const_cast<wchar_t*>(Wrapped(text));
    return text;
}

static bool Install()
{
    if (!g_enabled)
    {
        LogInfo("[%s] disabled by configuration", MODULE);
        return true;
    }

    if (!PatchIat((HMODULE)g_exeBase, DLL_ENGINE, SYM_GET_STRING,
                  (void*)h_GetString, (void**)&o_GetString,
                  "C3D_LANGUAGE::GetString (ui_layout_fixes)") ||
        !o_GetString)
    {
        LogError(MODULE, "iat-patch",
            "The import %s!%s of SOVIET64.exe could not be redirected; the route hint keeps its native line length. "
            "Action: check tesmioloader.log for the loader's reason and conflicting plugins",
            DLL_ENGINE, SYM_GET_STRING);
        return false;
    }

    g_installed = true;
    LogInfo("[%s] active: text id %d wrapped at %d chars, line limit %d; "
            "scope=one caption id through the caption lookup import; executable code unchanged",
        MODULE, g_textId, g_maxChars, g_maxLines);
    return true;
}

static void LoadConfig()
{
    g_enabled = TsmConfigInt("vehicle_route_hint", "enabled", 1) != 0;
    ReadIniInt("vehicle_route_hint", "text_id", DEFAULT_TEXT_ID,
               MIN_TEXT_ID, MAX_TEXT_ID, &g_textId);
    ReadIniInt("vehicle_route_hint", "max_chars", DEFAULT_MAX_CHARS,
               MIN_MAX_CHARS, MAX_MAX_CHARS, &g_maxChars);
    ReadIniInt("vehicle_route_hint", "max_lines", DEFAULT_MAX_LINES,
               MIN_MAX_LINES, MAX_MAX_LINES, &g_maxLines);
}
} // namespace VehicleRouteHint

static void LoadConfig()
{
    g_enabled = TsmConfigInt("general", "enabled", 1) != 0;
    if (g_enabled)
    {
        Customhouse::LoadConfig();
        VehicleRouteHint::LoadConfig();
    }
}

static bool HasEnabledModule()
{
    return Customhouse::g_enabled != 0 || VehicleRouteHint::g_enabled != 0;
}

static const char* ActiveModules(char* out, size_t capacity)
{
    out[0] = 0;
    if (Customhouse::g_enabled)
        strcat_s(out, capacity, "CUSTOMHOUSE");
    if (VehicleRouteHint::g_installed)
    {
        if (out[0]) strcat_s(out, capacity, "+");
        strcat_s(out, capacity, "VEHICLE_ROUTE_HINT");
    }
    if (!out[0]) strcpy_s(out, capacity, "none");
    return out;
}
} // namespace UiLayoutFixes

extern "C" __declspec(dllexport)
unsigned TsmPluginApiVersion(void)
{
    return TSM_API_VERSION;
}

extern "C" __declspec(dllexport)
int TsmPluginInit(const TsmHost* host, TsmPluginInfo* info)
{
    if (!host || !info) return 1;
    TsmBind(host);
    UiLayoutFixes::g_bound = true;

    info->name = "UI Layout Fixes";
    info->version = UiLayoutFixes::PLUGIN_VERSION;

    UiLayoutFixes::g_logStarted = GetTickCount64();
    UiLayoutFixes::BeginLogPhase();
    UiLayoutFixes::g_detail = TsmOpenLog(UiLayoutFixes::LOG_NAME);
    if (UiLayoutFixes::g_detail == INVALID_HANDLE_VALUE)
    {
        const DWORD error = GetLastError();
        UiLayoutFixes::ReportWindows("WARN", "core", "log-open",
            "The detail log could not be opened", error,
            "Verify write access to the TesmioLoader directory; the shared loader log remains available");
    }
    UiLayoutFixes::LogInfo(
        "TesmioLoader ui_layout_fixes %s starting; detail log: %s",
        UiLayoutFixes::PLUGIN_VERSION, UiLayoutFixes::LOG_NAME);
    TsmConfigInit("ui_layout_fixes");
    UiLayoutFixes::LogInfo("configuration: base=%s (%s); overlay=%s",
        g_tsmConfig.hasBase ? g_tsmConfig.base : "(none; compiled defaults)",
        g_tsmConfig.baseIsLocal ? "loader folder" : "beside the DLL",
        g_tsmConfig.hasOverlay ? g_tsmConfig.overlay : "(none)");

    try
    {
        UiLayoutFixes::LoadConfig();
        UiLayoutFixes::LogInfo(
            "initialized; architecture=window-specific modules; global layout changes=0");

        if (!UiLayoutFixes::g_enabled)
        {
            UiLayoutFixes::LogInfo("disabled by [general] enabled=0; no patch was written");
            UiLayoutFixes::LogSummaryStatus(
                "Initialization", "was skipped because the plugin is disabled");
            return 1;
        }

        if (!UiLayoutFixes::HasEnabledModule())
        {
            UiLayoutFixes::LogInfo(
                "all window modules are disabled; no patch was written");
            UiLayoutFixes::LogSummaryStatus(
                "Initialization", "was skipped because no module is enabled");
            return 1;
        }

        UiLayoutFixes::LogSummary("Initialization", true);
        return 0;
    }
    catch (const std::exception& error)
    {
        UiLayoutFixes::LogFatal("core", "exception",
            "C++ exception in TsmPluginInit: %s. Action: preserve the log and report the failure; no patch was written",
            error.what());
        UiLayoutFixes::LogSummary("Initialization", false);
        return 1;
    }
    catch (...)
    {
        UiLayoutFixes::LogFatal("core", "exception",
            "Unknown C++ exception in TsmPluginInit. Action: preserve the log and report the failure; no patch was written");
        UiLayoutFixes::LogSummary("Initialization", false);
        return 1;
    }
}

extern "C" __declspec(dllexport)
int TsmPluginStart(void)
{
    UiLayoutFixes::BeginLogPhase();
    try
    {
        if (!H || !UiLayoutFixes::g_bound || !g_exeBase ||
            !UiLayoutFixes::g_enabled)
        {
            UiLayoutFixes::LogFatal("core", "startup-state",
                "Start was called without a valid initialized plugin state. Action: preserve the log and restart the game; no patch was written");
            UiLayoutFixes::LogSummary("Startup", false);
            return 1;
        }

        if (!UiLayoutFixes::Customhouse::Install())
        {
            UiLayoutFixes::LogFatal(
                "CUSTOMHOUSE", "startup",
                "Module activation failed. Action: verify the supported game build and conflicting UI plugins, then restart the game");
            UiLayoutFixes::LogSummary("Startup", false);
            return 1;
        }

        // A failed import redirection leaves this module inactive but hooks
        // nothing, so the other modules and the startup result are unaffected.
        UiLayoutFixes::VehicleRouteHint::Install();

        char active[64];
        UiLayoutFixes::LogInfo(
            "startup complete; active modules=%s; unrelated windows=unchanged; total_elapsed_ms=%llu",
            UiLayoutFixes::ActiveModules(active, sizeof(active)),
            (unsigned long long)(GetTickCount64() - UiLayoutFixes::g_logStarted));
        UiLayoutFixes::LogSummary("Startup", true);
        return 0;
    }
    catch (const std::exception& error)
    {
        UiLayoutFixes::LogFatal("core", "exception",
            "C++ exception in TsmPluginStart: %s. Action: preserve the log and restart the game; patch state may be incomplete",
            error.what());
        UiLayoutFixes::LogSummary("Startup", false);
        return 1;
    }
    catch (...)
    {
        UiLayoutFixes::LogFatal("core", "exception",
            "Unknown C++ exception in TsmPluginStart. Action: preserve the log and restart the game; patch state may be incomplete");
        UiLayoutFixes::LogSummary("Startup", false);
        return 1;
    }
}

BOOL APIENTRY DllMain(HMODULE, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_DETACH)
    {
        if (UiLayoutFixes::g_detail != INVALID_HANDLE_VALUE)
        {
            CloseHandle(UiLayoutFixes::g_detail);
            UiLayoutFixes::g_detail = INVALID_HANDLE_VALUE;
        }
        if (UiLayoutFixes::g_bound)
        {
            DeleteCriticalSection(&g_lock);
            UiLayoutFixes::g_bound = false;
        }
    }
    return TRUE;
}
