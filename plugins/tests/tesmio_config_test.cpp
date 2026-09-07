// Offline test for tesmio_config.h: base/overlay resolution and value reading,
// against a fake host in a temporary folder. No game code runs.
//
//   run_config_test.bat   (builds and runs; exit code 0 = all checks passed)

#include "../../src/tesmio_plugin.h"
#include "../tesmio_config.h"
#include <stdarg.h>

static int g_failed = 0;
#define CHECK(cond) do { if (!(cond)) { printf("FAIL line %d: %s\n", __LINE__, #cond); g_failed++; } else printf("PASS %s\n", #cond); } while (0)

static void TestLog(const char* fmt, ...)
{
    va_list args; va_start(args, fmt); printf("  log: "); vprintf(fmt, args); printf("\n"); va_end(args);
}

static void WriteFile_(const char* path, const char* text)
{
    FILE* f = NULL; fopen_s(&f, path, "wb"); if (!f) { printf("cannot write %s\n", path); exit(2); }
    fputs(text, f); fclose(f);
}

int main()
{
    char temp[MAX_PATH]; GetTempPathA(sizeof(temp), temp);
    char root[MAX_PATH]; _snprintf_s(root, sizeof(root), _TRUNCATE, "%stesmio_config_test_%lu", temp, GetCurrentProcessId());
    CreateDirectoryA(root, NULL);
    char plugins[MAX_PATH], userConfig[MAX_PATH];
    _snprintf_s(plugins, sizeof(plugins), _TRUNCATE, "%s\\plugins", root); CreateDirectoryA(plugins, NULL);
    _snprintf_s(userConfig, sizeof(userConfig), _TRUNCATE, "%s\\user_config", root); CreateDirectoryA(userConfig, NULL);

    TsmHost host; memset(&host, 0, sizeof(host));
    host.apiVersion = TSM_API_VERSION; host.structSize = sizeof(host); host.baseDir = root; host.log = TestLog;
    TsmBind(&host);

    // 1. Nothing anywhere: compiled defaults.
    CHECK(!TsmConfigInit("cfgtest"));
    CHECK(TsmConfigInt("s", "n", 7) == 7);
    char text[64];
    CHECK(!TsmConfigString("s", "t", text, sizeof(text), "fb") && strcmp(text, "fb") == 0);

    // 2. Only the INI beside this module (the Workshop package case).
    char own[MAX_PATH]; CHECK(TsmConfigOwnDirectory(own, sizeof(own)));
    char adjacent[MAX_PATH]; _snprintf_s(adjacent, sizeof(adjacent), _TRUNCATE, "%s\\cfgtest.ini", own);
    WriteFile_(adjacent, "[s]\nn = 480   ; (stock 480)\nt = auto\nf = 1.5\n");
    CHECK(TsmConfigInit("cfgtest") && !g_tsmConfig.baseIsLocal && strcmp(g_tsmConfig.base, adjacent) == 0);
    CHECK(TsmConfigInt("s", "n", 7) == 480);
    CHECK(TsmConfigString("s", "t", text, sizeof(text), "fb") && strcmp(text, "auto") == 0);
    CHECK(TsmConfigFloat("s", "f", 0.0f) == 1.5f);
    CHECK(!TsmConfigIsPersonal("s", "n"));

    // 3. The loader folder wins over the package INI.
    char local[MAX_PATH]; _snprintf_s(local, sizeof(local), _TRUNCATE, "%s\\cfgtest.ini", plugins);
    WriteFile_(local, "[s]\nn = 1000\nt = calendar\n");
    CHECK(TsmConfigInit("cfgtest") && g_tsmConfig.baseIsLocal);
    CHECK(TsmConfigInt("s", "n", 7) == 1000);
    CHECK(TsmConfigFloat("s", "f", 2.5f) == 2.5f);          // absent in the local base: fallback, never the package INI

    // 4. The overlay wins over everything, key by key.
    char overlay[MAX_PATH]; _snprintf_s(overlay, sizeof(overlay), _TRUNCATE, "%s\\cfgtest.ini", userConfig);
    WriteFile_(overlay, "; Personal overrides.\n[s]\nn = 900\n");
    CHECK(TsmConfigInit("cfgtest") && g_tsmConfig.hasOverlay);
    CHECK(TsmConfigInt("s", "n", 7) == 900);
    CHECK(TsmConfigString("s", "t", text, sizeof(text), "fb") && strcmp(text, "calendar") == 0);
    CHECK(TsmConfigIsPersonal("s", "n") && !TsmConfigIsPersonal("s", "t"));

    // 5. Delete the overlay: back to the base values, no state kept.
    DeleteFileA(overlay);
    CHECK(TsmConfigInit("cfgtest") && !g_tsmConfig.hasOverlay);
    CHECK(TsmConfigInt("s", "n", 7) == 1000);

    // 6. Malformed numbers fall back instead of becoming zero.
    WriteFile_(local, "[s]\nn = abc\n");
    TsmConfigInit("cfgtest");
    CHECK(TsmConfigInt("s", "n", 7) == 7);
    CHECK(TsmConfigFloat("s", "n", 3.0f) == 3.0f);

    DeleteFileA(local); DeleteFileA(adjacent); RemoveDirectoryA(plugins); RemoveDirectoryA(userConfig); RemoveDirectoryA(root);
    printf("RESULT %s (%d failed)\n", g_failed ? "FAILED" : "OK", g_failed);
    return g_failed ? 1 : 0;
}
