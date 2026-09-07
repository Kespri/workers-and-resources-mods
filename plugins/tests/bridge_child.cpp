// Stub hook plugin for the workshop_bridge offline test. Records Init and
// Start as marker files in the host's baseDir and publishes one service, so
// the test can see that the bridge called both phases with the real host.

#include "../../src/tesmio_plugin.h"

static char g_marker[MAX_PATH];
static int  g_answer = 42;

static void Mark(const char* what)
{
    char path[MAX_PATH];
    _snprintf_s(path, sizeof(path), _TRUNCATE, "%s\\%s.%s.txt", g_baseDir, g_marker, what);
    FILE* f = NULL; fopen_s(&f, path, "wb"); if (f) { fputs(what, f); fclose(f); }
}

extern "C" __declspec(dllexport) unsigned TsmPluginApiVersion(void) { return TSM_API_VERSION; }

extern "C" __declspec(dllexport) int TsmPluginInit(const TsmHost* host, TsmPluginInfo* info)
{
    TsmBind(host);
    // The marker is the DLL's own file name, so three copies of this stub under
    // three names leave three distinct traces.
    static const char anchor = 0; HMODULE self = NULL; char path[MAX_PATH] = "";
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, &anchor, &self))
        GetModuleFileNameA(self, path, sizeof(path));
    const char* file = strrchr(path, '\\') ? strrchr(path, '\\') + 1 : path;
    strncpy_s(g_marker, sizeof(g_marker), file, _TRUNCATE);
    if (char* dot = strrchr(g_marker, '.')) *dot = 0;

    info->name = "bridge_child"; info->version = "1.0";
    if (H->configInt("plugins\\bridge_child.ini", "child", "decline", 0)) return 7;
    H->provide("bridge.test", 1u, &g_answer);
    Mark("init");
    return 0;
}

extern "C" __declspec(dllexport) int TsmPluginStart(void)
{
    const int* seen = (const int*)H->consume("bridge.test", 1u);
    Mark(seen && *seen == 42 ? "start" : "start_noservice");
    return 0;
}
