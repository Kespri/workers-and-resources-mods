// Offline test for my_plugins\workshop_bridge: a fake host, a fake Workshop
// folder with several packages, and the real workshop_bridge.dll loaded into
// this process. No game code runs.
//
//   run_bridge_test.bat   (builds and runs; exit code 0 = all checks passed)

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
    char line[1024]; va_list args; va_start(args, fmt); vsnprintf(line, sizeof(line), fmt, args); va_end(args);
    printf("  log: %s\n", line); strcat_s(g_log, sizeof(g_log), line); strcat_s(g_log, sizeof(g_log), "\n");
}
static bool Logged(const char* text) { return strstr(g_log, text) != NULL; }

struct Service { char name[32]; unsigned version; const void* iface; };
static Service g_service[16]; static int g_serviceCount;
static int Provide(const char* name, unsigned version, const void* iface)
{
    strncpy_s(g_service[g_serviceCount].name, sizeof(g_service[0].name), name, _TRUNCATE);
    g_service[g_serviceCount].version = version; g_service[g_serviceCount].iface = iface; g_serviceCount++; return 1;
}
static const void* Consume(const char* name, unsigned version)
{
    for (int i = 0; i < g_serviceCount; i++) if (_stricmp(g_service[i].name, name) == 0 && g_service[i].version == version) return g_service[i].iface;
    return NULL;
}
static long Filter(const char*, void*) { return EXCEPTION_EXECUTE_HANDLER; }

static char g_root[MAX_PATH];
static int api_ConfigInt(const char* ini, const char* section, const char* key, int fallback)
{
    char path[MAX_PATH]; _snprintf_s(path, sizeof(path), _TRUNCATE, "%s\\%s", g_root, ini);
    return GetPrivateProfileIntA(section, key, fallback, path);
}
static int api_ConfigString(const char* ini, const char* section, const char* key, char* out, int size, const char* fallback)
{
    char path[MAX_PATH]; _snprintf_s(path, sizeof(path), _TRUNCATE, "%s\\%s", g_root, ini);
    return (int)GetPrivateProfileStringA(section, key, fallback ? fallback : "", out, (DWORD)size, path);
}

static void WriteText(const char* path, const char* text)
{
    FILE* f = NULL; fopen_s(&f, path, "wb"); if (!f) { printf("cannot write %s\n", path); exit(2); }
    fputs(text, f); fclose(f);
}
static bool Exists(const char* path) { return GetFileAttributesA(path) != INVALID_FILE_ATTRIBUTES; }
static void Mkdir(const char* path) { CreateDirectoryA(path, NULL); }
static char* P(char* buffer, const char* fmt, const char* a, const char* b = "")
{
    _snprintf_s(buffer, MAX_PATH, _TRUNCATE, fmt, a, b); return buffer;
}

// One run of the bridge: fresh process state is impossible inside one test
// binary, so each scenario loads the DLL, drives Init/Start, and unloads it.
// The stub children keep their markers, which are deleted between scenarios.
struct Run { int childrenInit, childrenStart; };
static Run Drive(const TsmHost* host, const char* bridgeDll, const char* build)
{
    g_log[0] = 0; g_serviceCount = 0;
    HMODULE mod = LoadLibraryExA(bridgeDll, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    if (!mod) { printf("cannot load %s (%lu)\n", bridgeDll, GetLastError()); exit(3); }
    TsmPluginApiVersionFn ver = (TsmPluginApiVersionFn)GetProcAddress(mod, TSM_EXPORT_APIVERSION);
    TsmPluginInitFn init = (TsmPluginInitFn)GetProcAddress(mod, TSM_EXPORT_INIT);
    TsmPluginStartFn start = (TsmPluginStartFn)GetProcAddress(mod, TSM_EXPORT_START);
    CHECK(ver && init && start && ver() == TSM_API_VERSION);
    TsmPluginInfo info; memset(&info, 0, sizeof(info));
    CHECK(init(host, &info) == 0 && info.name && strcmp(info.name, "workshop_bridge") == 0);
    CHECK(start() == 0);
    Run r; r.childrenInit = 0; r.childrenStart = 0;
    const char* names[] = { "child_a", "child_b", "child_c", "child_d", "child_e" };
    for (int i = 0; i < 5; i++)
    {
        char m[MAX_PATH];
        _snprintf_s(m, sizeof(m), _TRUNCATE, "%s\\%s.init.txt", build, names[i]); if (Exists(m)) { r.childrenInit++; DeleteFileA(m); }
        _snprintf_s(m, sizeof(m), _TRUNCATE, "%s\\%s.start.txt", build, names[i]); if (Exists(m)) { r.childrenStart++; DeleteFileA(m); }
        _snprintf_s(m, sizeof(m), _TRUNCATE, "%s\\%s.start_noservice.txt", build, names[i]); if (Exists(m)) { DeleteFileA(m); printf("  child %s saw no service\n", names[i]); }
    }
    // Children stay loaded in this process (the bridge never frees an accepted
    // hook); the next scenario must therefore use fresh DLL names or expect
    // the "already loaded" line.
    FreeLibrary(mod);
    return r;
}

int main(int argc, char** argv)
{
    if (argc < 3) { printf("usage: workshop_bridge_test <workshop_bridge.dll> <bridge_child.dll>\n"); return 2; }
    const char* bridgeDll = argv[1]; const char* childDll = argv[2];

    char temp[MAX_PATH]; GetTempPathA(sizeof(temp), temp);
    _snprintf_s(g_root, sizeof(g_root), _TRUNCATE, "%sbridge_test_%lu", temp, GetCurrentProcessId());
    char buf[MAX_PATH], buf2[MAX_PATH];
    Mkdir(g_root); Mkdir(P(buf, "%s\\plugins", g_root)); Mkdir(P(buf, "%s\\user_config", g_root));
    char workshop[MAX_PATH]; _snprintf_s(workshop, sizeof(workshop), _TRUNCATE, "%s\\workshop", g_root); Mkdir(workshop);

    // Packages: 111 listed, 222 unlisted, 333 listed but duplicated in plugins\,
    // 444 listed but enabled=0, 555 listed with two hooks, one of them missing,
    // 666 content only, 777 a hook path that escapes the package.
    struct Pkg { const char* folder; const char* manifest; const char* dllName; } pkgs[] = {
        { "111", "[mod]\nid=example.a\nname=A\n[hooks]\ndll=hooks\\child_a.dll\n", "child_a.dll" },
        { "222", "[mod]\nid=example.b\nname=B\n[hooks]\ndll=hooks/child_b.dll\n", "child_b.dll" },
        { "333", "\xEF\xBB\xBF[mod]\nid=example.c\nname=C\n[hooks]\ndll = hooks\\child_c.dll ; the hook\n", "child_c.dll" },
        { "444", "[mod]\nid=example.d\nname=D\nenabled=0\n[hooks]\ndll=hooks\\child_d.dll\n", "child_d.dll" },
        { "555", "[mod]\nid=example.e\nname=E\n[hooks]\ndll=hooks\\child_e.dll\ndll=hooks\\gone.dll\n", "child_e.dll" },
        { "666", "[mod]\nid=example.f\nname=F\n[content]\nresources=tesmio\\resources.ini\n", NULL },
        { "777", "[mod]\nid=example.g\nname=G\n[hooks]\ndll=..\\111\\hooks\\child_a.dll\n", NULL },
    };
    for (int i = 0; i < 7; i++)
    {
        Mkdir(P(buf, "%s\\%s", workshop, pkgs[i].folder));
        WriteText(P(buf, "%s\\%s\\soviet.mod.ini", workshop, pkgs[i].folder), pkgs[i].manifest);
        if (pkgs[i].dllName)
        {
            Mkdir(P(buf, "%s\\%s\\hooks", workshop, pkgs[i].folder));
            _snprintf_s(buf2, sizeof(buf2), _TRUNCATE, "%s\\%s\\hooks\\%s", workshop, pkgs[i].folder, pkgs[i].dllName);
            CHECK(CopyFileA(childDll, buf2, FALSE));
        }
    }
    CHECK(CopyFileA(childDll, P(buf, "%s\\plugins\\child_c.dll", g_root), FALSE));

    // The bridge's base INI beside "its" DLL is not there: the test passes the
    // DLL from the build folder, so everything comes from plugins\ and user_config\.
    char baseIni[MAX_PATH]; P(baseIni, "%s\\plugins\\workshop_bridge.ini", g_root);
    char overlay[MAX_PATH]; P(overlay, "%s\\user_config\\workshop_bridge.ini", g_root);
    char base[MAX_PATH + 64];
    _snprintf_s(base, sizeof(base), _TRUNCATE, "[bridge]\nenabled = 1\npolicy = list\nworkshop_root = %s\nlog_verbose = 1\n[packages]\n", workshop);
    WriteText(baseIni, base);
    WriteText(overlay, "[packages]\n111 = 1\n333 = 1\n444 = 1\n555 = 1\n777 = 1\n");

    TsmHost host; memset(&host, 0, sizeof(host));
    host.apiVersion = TSM_API_VERSION; host.structSize = sizeof(host); host.baseDir = g_root; host.log = TestLog;
    host.provide = Provide; host.consume = Consume; host.faultFilter = Filter; host.configInt = api_ConfigInt; host.configString = api_ConfigString;

    // 1. Normal run: 111 and 555's first hook load; 222 unlisted, 333 duplicate,
    //    444 disabled, 555's second hook missing, 666 content, 777 refused.
    Run r = Drive(&host, bridgeDll, g_root);
    CHECK(r.childrenInit == 2 && r.childrenStart == 2);
    CHECK(g_serviceCount == 2 && Consume("bridge.test", 1u) != NULL);
    CHECK(Logged("hook bridge_child") && Logged("from 111\\hooks\\child_a.dll"));
    CHECK(Logged("from 555\\hooks\\child_e.dll"));
    CHECK(Logged("example.b: not listed in [packages] - skipped"));
    CHECK(Logged("skipped - plugins\\child_c.dll exists"));
    CHECK(Logged("example.d: enabled = 0 in its manifest - skipped"));
    CHECK(Logged("\\555\\hooks\\gone.dll") && Logged("missing:"));
    CHECK(Logged("example.f: content only, no hooks"));
    CHECK(Logged("hook path refused"));
    CHECK(Logged("2 hook(s) loaded, 5 skipped, 6 package(s) with hooks"));
    CHECK(GetModuleHandleA("child_a.dll") != NULL && GetModuleHandleA("child_b.dll") == NULL && GetModuleHandleA("child_c.dll") == NULL);

    // 2. Same process, second run: the loaded hooks are reported as already
    //    loaded rather than initialised twice.
    r = Drive(&host, bridgeDll, g_root);
    CHECK(r.childrenInit == 0 && Logged("child_a.dll") && Logged("skipped - already loaded in this process"));

    // 3. Overlay entry 0 wins over policy all; policy all loads the unlisted 222.
    WriteText(baseIni, base); // unchanged
    _snprintf_s(buf2, sizeof(buf2), _TRUNCATE, "[bridge]\npolicy = all\n[packages]\n111 = 0\n333 = 1\n");
    WriteText(overlay, buf2);
    r = Drive(&host, bridgeDll, g_root);
    CHECK(Logged("example.a: off in [packages] - skipped"));
    CHECK(Logged("from 222\\hooks\\child_b.dll") && r.childrenInit == 1);

    // 4. A hook that declines is unloaded and counted as skipped, never started.
    WriteText(P(buf, "%s\\plugins\\bridge_child.ini", g_root), "[child]\ndecline = 1\n");
    WriteText(overlay, "[packages]\n444 = 1\n");
    WriteText(P(buf, "%s\\444\\soviet.mod.ini", workshop), "[mod]\nid=example.d\nname=D\n[hooks]\ndll=hooks\\child_d.dll\n");
    r = Drive(&host, bridgeDll, g_root);
    CHECK(Logged("child_d.dll") && Logged("declined to install (7)") && r.childrenInit == 0 && r.childrenStart == 0 && GetModuleHandleA("child_d.dll") == NULL);
    DeleteFileA(P(buf, "%s\\plugins\\bridge_child.ini", g_root));

    // 5. Soviet Mod Loader installed and on: the bridge is idle.
    CHECK(CopyFileA(childDll, P(buf, "%s\\plugins\\soviet_mod_loader.dll", g_root), FALSE));
    WriteText(P(buf, "%s\\tesmioloader.ini", g_root), "[plugins]\nsoviet_mod_loader=1\n");
    r = Drive(&host, bridgeDll, g_root);
    CHECK(Logged("idle - plugins\\soviet_mod_loader.dll is installed and on in tesmioloader.ini") && r.childrenInit == 0);
    WriteText(P(buf, "%s\\tesmioloader.ini", g_root), "[plugins]\nsoviet_mod_loader=0\n");
    r = Drive(&host, bridgeDll, g_root);
    CHECK(!Logged("idle -") && Logged("Workshop "));

    // 6. enabled = 0 in the overlay switches the whole bridge off.
    WriteText(overlay, "[bridge]\nenabled = 0\n");
    r = Drive(&host, bridgeDll, g_root);
    CHECK(Logged("disabled in workshop_bridge.ini") && !Logged("Workshop "));

    // 7. Unknown workshop_root: a clear line, nothing loaded.
    WriteText(overlay, "[bridge]\nworkshop_root = Q:\\nowhere\\at\\all\n");
    r = Drive(&host, bridgeDll, g_root);
    CHECK(Logged("Workshop folder not found (Q:\\nowhere\\at\\all)"));

    printf("%s: %d check(s) failed. Fixture: %s\n", g_failed ? "FAILED" : "ALL PASSED", g_failed, g_root);
    return g_failed ? 1 : 0;
}
