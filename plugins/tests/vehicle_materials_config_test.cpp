// Offline test for vehicle_materials 1.2.0: base INI plus user_config overlay.
// Drives TsmPluginInit of the real DLL against a fake host. Init only parses
// and validates the configuration (the hook is installed in Start, which is
// never called here), so no game code runs.
//
//   run_vehicle_materials_test.bat   (builds and runs; exit code 0 = all passed)

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include "../../src/tesmio_api.h"

static int g_failed = 0;
#define CHECK(cond) do { if (!(cond)) { printf("FAIL line %d: %s\n", __LINE__, #cond); g_failed++; } else printf("PASS %s\n", #cond); } while (0)

static char g_log[64 * 1024];
static void TestLog(const char* fmt, ...)
{
    char line[2048]; va_list args; va_start(args, fmt); vsnprintf(line, sizeof(line), fmt, args); va_end(args);
    printf("  log: %s\n", line); strcat_s(g_log, sizeof(g_log), line); strcat_s(g_log, sizeof(g_log), "\n");
}
static bool Logged(const char* text) { return strstr(g_log, text) != NULL; }
static int Dummy(void*, void*, void**, const unsigned char*, size_t, const char*) { return 0; }
static int DummyReadable(const void*, size_t) { return 1; }
static long DummyFilter(const char*, void*) { return EXCEPTION_EXECUTE_HANDLER; }
static const void* DummyConsume(const char*, unsigned) { return NULL; }
static int DummyInt(const char*, const char*, const char*, int f) { return f; }
static int DummyString(const char*, const char*, const char*, char* out, int size, const char* fb) { strncpy_s(out, (size_t)size, fb ? fb : "", _TRUNCATE); return 0; }

static void WriteText(const char* path, const char* text)
{
    FILE* f = NULL; fopen_s(&f, path, "wb"); if (!f) { printf("cannot write %s\n", path); exit(2); }
    fputs(text, f); fclose(f);
}
static void Mkdir(const char* p) { CreateDirectoryA(p, NULL); }

static const char* BASE_EMPTY =
    "[general]\nenabled = 0\ndebug = 0\ndebug_limit = 80\n[resources]\ncount = 0\n[road]\n[rail]\n[ship]\n[airplane]\n[mapping]\ntype0 = -1\n";
static const char* BASE_TWO =
    "[general]\nenabled = 1\ndebug = 1\ndebug_limit = 80\n[resources]\ncount = 2\nresource0 = glass\nresource1 = cable\n"
    "[road]\nglass = 0.030\ncable = 0.004\n[rail]\nglass = 0.025\n[ship]\n[airplane]\n[mapping]\n";

// One scenario = one copy of the DLL under its own name in its own folder,
// so each Init starts from a fresh module with the INI beside it.
static int Drive(const char* root, const char* dll, const char* name, const char* baseIni, const char* overlayIni)
{
    char folder[MAX_PATH], copy[MAX_PATH], ini[MAX_PATH], overlay[MAX_PATH];
    _snprintf_s(folder, MAX_PATH, _TRUNCATE, "%s\\%s", root, name); Mkdir(folder);
    _snprintf_s(copy, MAX_PATH, _TRUNCATE, "%s\\%s.dll", folder, name);
    if (!CopyFileA(dll, copy, FALSE)) { printf("cannot copy dll\n"); exit(3); }
    _snprintf_s(ini, MAX_PATH, _TRUNCATE, "%s\\vehicle_materials.ini", folder); WriteText(ini, baseIni);
    _snprintf_s(overlay, MAX_PATH, _TRUNCATE, "%s\\user_config\\vehicle_materials.ini", root);
    if (overlayIni) WriteText(overlay, overlayIni); else DeleteFileA(overlay);

    static unsigned char fakeExe[64];
    TsmHost host; memset(&host, 0, sizeof(host));
    host.apiVersion = TSM_API_VERSION; host.structSize = sizeof(host); host.baseDir = root; host.log = TestLog;
    host.exeModule = (void*)fakeExe; host.exeBase = fakeExe; host.exeSize = sizeof(fakeExe);
    host.installInlineHook = Dummy; host.readablePtr = DummyReadable; host.faultFilter = DummyFilter; host.consume = DummyConsume;
    host.configInt = DummyInt; host.configString = DummyString;

    g_log[0] = 0;
    HMODULE mod = LoadLibraryExA(copy, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    if (!mod) { printf("cannot load %s (%lu)\n", copy, GetLastError()); exit(4); }
    TsmPluginInitFn init = (TsmPluginInitFn)GetProcAddress(mod, TSM_EXPORT_INIT);
    TsmPluginInfo info; memset(&info, 0, sizeof(info));
    int rc = init(&host, &info);
    printf("  -> %s: init rc=%d version=%s\n", name, rc, info.version ? info.version : "?");
    return rc;
}

int main(int argc, char** argv)
{
    if (argc < 2) { printf("usage: vehicle_materials_config_test <vehicle_materials.dll>\n"); return 2; }
    char temp[MAX_PATH]; GetTempPathA(sizeof(temp), temp);
    char root[MAX_PATH]; _snprintf_s(root, sizeof(root), _TRUNCATE, "%svm_config_test_%lu", temp, GetCurrentProcessId());
    char sub[MAX_PATH];
    Mkdir(root); _snprintf_s(sub, MAX_PATH, _TRUNCATE, "%s\\plugins", root); Mkdir(sub);
    _snprintf_s(sub, MAX_PATH, _TRUNCATE, "%s\\user_config", root); Mkdir(sub);

    // 1. Empty base beside the DLL, no overlay: disabled, declines with 1.
    int rc = Drive(root, argv[1], "vm_a", BASE_EMPTY, NULL);
    CHECK(rc == 1 && Logged("beside plugin DLL") && Logged("enabled=0") && !Logged("overlay"));

    // 2. Empty base, overlay switches on and lists one material: Init succeeds.
    rc = Drive(root, argv[1], "vm_b", BASE_EMPTY,
        "; personal\n[general]\nenabled = 1\ndebug = 1\n[resources]\ncount = 1\nresource0 = glass\n[road]\nglass = 0.030\n[rail]\nglass = 0.025\n");
    CHECK(rc == 0 && Logged("Personal overlay applied: 6 value(s)") && Logged("active_materials=1") && Logged("material=glass road=0.0300 rail=0.0250"));

    // 3. Base lists two materials, overlay shortens the list to glass: cable
    //    entries from the base are superseded with a warning, not an error.
    rc = Drive(root, argv[1], "vm_c", BASE_TWO, "[resources]\ncount = 1\nresource0 = glass\n");
    CHECK(rc == 0 && Logged("resource-superseded") && Logged("active_materials=1") && !Logged("unknown-resource"));

    // 4. Overlay with an unknown key is rejected, naming the overlay file.
    rc = Drive(root, argv[1], "vm_d", BASE_TWO, "[general]\nbogus = 1\n");
    CHECK(rc == 1 && Logged("unknown-key") && Logged("user_config\\vehicle_materials.ini"));

    // 5. plugins\vehicle_materials.ini in the loader folder wins over the INI beside the DLL.
    _snprintf_s(sub, MAX_PATH, _TRUNCATE, "%s\\plugins\\vehicle_materials.ini", root); WriteText(sub, BASE_TWO);
    rc = Drive(root, argv[1], "vm_e", BASE_EMPTY, NULL);
    CHECK(rc == 0 && Logged("in the loader folder") && Logged("active_materials=2"));
    DeleteFileA(sub);

    // 6. Overlay without [resources] leaves the base list alone; a stale base
    //    coefficient for an unlisted material is still an error.
    rc = Drive(root, argv[1], "vm_f", "[general]\nenabled = 1\ndebug = 0\ndebug_limit = 80\n[resources]\ncount = 0\n[road]\nglass = 0.03\n[rail]\n[ship]\n[airplane]\n[mapping]\n", "[general]\ndebug = 1\n");
    CHECK(rc == 1 && Logged("unknown-resource"));

    printf("%s: %d check(s) failed. Fixture: %s\n", g_failed ? "FAILED" : "ALL PASSED", g_failed, root);
    return g_failed ? 1 : 0;
}
