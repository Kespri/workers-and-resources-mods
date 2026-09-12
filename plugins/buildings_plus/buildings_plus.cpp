// buildings_plus - new buildings declared in a config file, as a TesmioLoader plugin.
//
// A new building needs no reverse engineering. The game's own Workshop format
// describes one completely: a folder of seven files, five of which are byte
// copies of a base-game asset and one of which is the donor's own building.ini
// with a few lines changed. This plugin writes that folder from one section of
// plugins\buildings_plus.ini before the game has run a single instruction:
//
//   [salt_mine]
//   donor  = bauxite_mine
//   object = SaltMine
//   name   = Salt Mine
//   line   = $PRODUCTION raw_salt 1.0
//
// The Workshop id of such a section is assigned by the plugin (9300000000
// upwards, the highest number found plus one) and kept in
// plugins\buildings_plus.ids.ini under the section name, so it never changes
// once given; an explicit `id =` still wins.
//
// It patches nothing: no hook, no import swap, no game structure. The loader is
// injected into a suspended process and plugins are initialised from DllMain,
// so a folder generated at startup is a folder the game finds when it scans
// media_soviet\workshop_wip.
//
// Fork of the buildings plugin from TesmioLoader (MaxLegend, GPL v3) with the
// same section format that Soviet Mod Loader reads from a mod's
// tesmio\buildings.ini, so one declaration serves both loaders. Differences of
// this fork: configuration falls back to the INI beside the DLL, a detail log
// under logs\, growable limits, validation of every declared value, atomic
// file writes, cleanup of stale object folders and - on request - of folders
// whose declaration disappeared (prune), and wide-character paths so a game
// folder with non-ASCII characters works.
//
// WHAT IS COPIED AND WHAT IS REWRITTEN
//
//   media_soviet\buildings\<donor>.nmf          -> <item>\<object>\model.nmf
//   media_soviet\buildings_types\<donor>.bbox   -> <item>\<object>\building.bbox
//   media_soviet\buildings_types\<donor>.fire   -> <item>\<object>\building.fire
//   media_soviet\editor\tool_<donor>.png        -> <item>\<object>\imagegui.png
//                                                  and <item>\previewimage.png
//   media_soviet\buildings\<donor>.mtl          -> <item>\material.mtl
//   media_soviet\buildings\<donor>_e.mtl        -> <item>\material_e.mtl
//   media_soviet\buildings_types\<donor>.ini    -> <item>\<object>\building.ini
//
// The two .mtl copies are rewritten: $TEXTURE_MTL resolves next to the .mtl,
// which in a Workshop item is the mod's own folder, so every such line becomes
// `$TEXTURE <slot> buildings/<path>`, which resolves against media_soviet.
//
// building.ini starts from the donor's own file and changes only the economy.
// Everything geometric ($CONNECTION_*, $COST_WORK*, $VEHICLE_STATION,
// $PARTICLE, $TEXT_CAPTION) is measured against the copied mesh and survives
// verbatim; a donor line is dropped only when a declared line replaces it.
// Most tokens replace only themselves. Four families replace each other:
//   $NAME / $NAME_STR                      a name is a name
//   $TYPE_*                                exactly one may be in effect
//   $STORAGE* + $RESOURCE_VISUALIZATION    the visualisation counts storages
//                                          from zero, so re-declaring them
//                                          would silently move every pile
//   $PRODUCTION, $CONSUMPTION,             a recipe is replaced whole
//   $CONSUMPTION_PER_SECOND
// `strip = $TOKEN` drops anything else. A building.ini has no comment syntax:
// the first $TOKEN anywhere in a line is what the game's parser matches.
//
// WHERE THE FOLDER GOES
//
// media_soviet\workshop_wip\<id>, the folder the game scans for unpublished
// Workshop items. It cannot be served from the loader's VFS: the game finds
// items by listing that directory, and the VFS redirects opens, not listings.
// Every generated folder carries tesmioloader.stamp; a folder without one is
// never touched, so an id that collides with a real subscription is refused.

#include "../../src/tesmio_plugin.h"
#include <string>
#include <vector>
#include <cstdint>

#define PLUGIN_VERSION    "0.1.2"
#define PLUGIN_INI        "plugins\\buildings_plus.ini"
#define PLUGIN_LOG_NAME   "tesmioloader.buildings_plus.log"
#define STAMP_NAME        "tesmioloader.stamp"
#define STAMP_MARK        "buildings_plus generated this folder"
#define GENERATOR_VERSION 3
#define MAX_DECLS         256
#define MAX_LINES         512
#define MAX_LINE_LEN      4096
#define MAX_TEXT_FILE     (4u * 1024 * 1024)
#define MIN_ITEM_ID       9000000000ULL
#define MAX_ITEM_ID       9999999999ULL
// Sections without `id =` get one from this range, chosen clear of Soviet Mod
// Loader (9100000000..9199999999) and of the numbers in Tesmio's examples.
#define AUTO_MIN_ID       9300000000ULL
#define AUTO_MAX_ID       9399999999ULL
// Assigned ids are remembered here, next to the plugin INI, so a building keeps
// its number for as long as its section name exists. The number is part of the
// folder path a saved game refers to.
#define CATALOG_NAME      "buildings_plus.ids.ini"

// ---------------------------------------------------------------- declarations

struct Decl
{
    std::string section;
    std::string id;        // the Workshop item id, and the folder name
    std::string object;    // the object subfolder, named by $OBJECT_BUILDING
    std::string donor;     // a base-game buildings_types name, no extension
    std::string name;      // $NAME_STR, and the Workshop item name
    std::string desc;      // the Workshop description, lines joined with \n
    int  life = 3000;      // renderconfig LIFE
    bool enabled = true;
    bool valid = true;
    std::vector<std::string> lines;    // building.ini lines to put in
    std::vector<std::string> strips;   // extra tokens to take out
    size_t configLine = 0;
};

static std::vector<Decl> g_decls;

static bool g_enabled = true;
static bool g_always  = false;   // regenerate even when the stamp matches
static bool g_verbose = false;   // one detail-log line per copied file and dropped donor line
static bool g_prune   = false;   // remove our stamped folders whose declaration is gone
static std::wstring g_outDir = L"media_soviet\\workshop_wip";
static std::wstring g_gameDir;
static std::wstring g_media;
#ifdef BUILDINGS_PLUS_TEST
static std::wstring g_testGameDir;
#endif

static HANDLE g_detail = INVALID_HANDLE_VALUE;
static int g_warnings = 0;
static int g_errors = 0;

enum GenResult { GEN_GENERATED, GEN_UP_TO_DATE, GEN_SKIPPED, GEN_FAILED };

// ---------------------------------------------------------------- text and paths

static std::wstring Wide(const std::string& utf8)
{
    if (utf8.empty()) return std::wstring();
    int n = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), (int)utf8.size(), NULL, 0);
    std::wstring w((size_t)n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), (int)utf8.size(), &w[0], n);
    return w;
}

static std::wstring WideAnsi(const char* ansi)
{
    if (!ansi || !*ansi) return std::wstring();
    int n = MultiByteToWideChar(CP_ACP, 0, ansi, -1, NULL, 0);
    if (n <= 0) return std::wstring();
    std::wstring w((size_t)n - 1, L'\0');
    MultiByteToWideChar(CP_ACP, 0, ansi, -1, &w[0], n);
    return w;
}

static std::string Narrow(const std::wstring& w)
{
    if (w.empty()) return std::string();
    int n = WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), NULL, 0, NULL, NULL);
    std::string s((size_t)n, '\0');
    WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), &s[0], n, NULL, NULL);
    return s;
}

static std::string TrimCopy(const std::string& s)
{
    size_t b = 0, e = s.size();
    while (b < e && (s[b] == ' ' || s[b] == '\t' || s[b] == '\r' || s[b] == '\n')) b++;
    while (e > b && (s[e - 1] == ' ' || s[e - 1] == '\t' || s[e - 1] == '\r' || s[e - 1] == '\n')) e--;
    return s.substr(b, e - b);
}

static std::string FormatV(const char* fmt, va_list ap)
{
    int n = _vscprintf(fmt, ap);
    if (n < 0) return std::string();
    std::string s((size_t)n, '\0');
    if (n > 0) vsnprintf(&s[0], (size_t)n + 1, fmt, ap);
    return s;
}

static std::vector<std::string> SplitLines(const std::string& text)
{
    std::vector<std::string> out;
    size_t start = 0;
    while (start <= text.size())
    {
        size_t nl = text.find('\n', start);
        std::string line = text.substr(start, nl == std::string::npos ? std::string::npos : nl - start);
        if (!line.empty() && line[line.size() - 1] == '\r') line.erase(line.size() - 1);
        out.push_back(line);
        if (nl == std::string::npos) break;
        start = nl + 1;
    }
    // A trailing newline produces one empty last line; the game's files end that way.
    if (!out.empty() && out.back().empty() && text.size() && text[text.size() - 1] == '\n') out.pop_back();
    return out;
}

static bool FileExists(const std::wstring& p)
{
    DWORD a = GetFileAttributesW(p.c_str());
    return a != INVALID_FILE_ATTRIBUTES && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

static bool DirExists(const std::wstring& p)
{
    DWORD a = GetFileAttributesW(p.c_str());
    return a != INVALID_FILE_ATTRIBUTES && (a & FILE_ATTRIBUTE_DIRECTORY);
}

static bool EnsureDir(const std::wstring& p)
{
    if (CreateDirectoryW(p.c_str(), NULL)) return true;
    return GetLastError() == ERROR_ALREADY_EXISTS && DirExists(p);
}

// ---------------------------------------------------------------- logging

static void DetailLine(const char* level, const std::string& text)
{
    if (g_detail == INVALID_HANDLE_VALUE) return;
    SYSTEMTIME t;
    GetLocalTime(&t);
    char stamp[64];
    _snprintf_s(stamp, sizeof(stamp), _TRUNCATE, "[%04d-%02d-%02d %02d:%02d:%02d.%03d] %s",
                t.wYear, t.wMonth, t.wDay, t.wHour, t.wMinute, t.wSecond, t.wMilliseconds, level);
    DWORD put = 0;
    WriteFile(g_detail, stamp, (DWORD)strlen(stamp), &put, NULL);
    WriteFile(g_detail, text.c_str(), (DWORD)text.size(), &put, NULL);
    WriteFile(g_detail, "\r\n", 2, &put, NULL);
}

// Detail log only.
static void Info(const char* fmt, ...)
{
    va_list ap; va_start(ap, fmt); std::string s = FormatV(fmt, ap); va_end(ap);
    DetailLine("INFO  ", s);
}

// Detail log and tesmioloader.log: the lines a player should see without opening the detail log.
static void Note(const char* fmt, ...)
{
    va_list ap; va_start(ap, fmt); std::string s = FormatV(fmt, ap); va_end(ap);
    DetailLine("INFO  ", s);
    Logf("buildings_plus  %s", s.c_str());
}

static void Warn(const char* fmt, ...)
{
    va_list ap; va_start(ap, fmt); std::string s = FormatV(fmt, ap); va_end(ap);
    g_warnings++;
    DetailLine("WARN: ", s);
    Logf("buildings_plus  WARN: %s", s.c_str());
}

static void Error(const char* fmt, ...)
{
    va_list ap; va_start(ap, fmt); std::string s = FormatV(fmt, ap); va_end(ap);
    g_errors++;
    DetailLine("ERROR: ", s);
    Logf("buildings_plus  ERROR: %s", s.c_str());
}

// ---------------------------------------------------------------- files

// Reads a whole text file. Every file this touches is an ini or an mtl of a few
// kilobytes; the cap keeps a wrong path from asking for a gigabyte.
static bool ReadTextFile(const std::wstring& path, std::string* out)
{
    out->clear();
    HANDLE h = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL,
                           OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (h == INVALID_HANDLE_VALUE) return false;
    LARGE_INTEGER size;
    if (!GetFileSizeEx(h, &size) || size.QuadPart < 0 || size.QuadPart > MAX_TEXT_FILE) { CloseHandle(h); return false; }
    out->resize((size_t)size.QuadPart);
    DWORD got = 0;
    BOOL ok = size.QuadPart == 0 ? TRUE : ReadFile(h, &(*out)[0], (DWORD)size.QuadPart, &got, NULL);
    CloseHandle(h);
    if (!ok || got != (DWORD)size.QuadPart) { out->clear(); return false; }
    // A UTF-8 byte order mark is not part of the text.
    if (out->size() >= 3 && (unsigned char)(*out)[0] == 0xEF && (unsigned char)(*out)[1] == 0xBB && (unsigned char)(*out)[2] == 0xBF) out->erase(0, 3);
    return true;
}

// Written to a temporary name first and moved into place, so a crash halfway
// never leaves the game a half-written building.ini.
static bool WriteTextFileAtomic(const std::wstring& path, const std::string& text)
{
    std::wstring tmp = path + L".buildings_plus.tmp";
    HANDLE h = CreateFileW(tmp.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (h == INVALID_HANDLE_VALUE) return false;
    DWORD wrote = 0;
    BOOL ok = text.empty() ? TRUE : WriteFile(h, text.c_str(), (DWORD)text.size(), &wrote, NULL);
    if (ok) ok = FlushFileBuffers(h);
    CloseHandle(h);
    if (!ok || wrote != (DWORD)text.size()) { DeleteFileW(tmp.c_str()); return false; }
    if (!MoveFileExW(tmp.c_str(), path.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
    { DeleteFileW(tmp.c_str()); return false; }
    return true;
}

static bool CopyAsset(const std::wstring& src, const std::wstring& dst, bool required, const char* what)
{
    if (!FileExists(src))
    {
        if (required) Error("%s not found: %s", what, Narrow(src).c_str());
        else if (g_verbose) Info("no %s (%s) - skipped", what, Narrow(src).c_str());
        return !required;
    }
    if (!CopyFileW(src.c_str(), dst.c_str(), FALSE))
    {
        Error("could not copy %s -> %s (%lu)", Narrow(src).c_str(), Narrow(dst).c_str(), GetLastError());
        return false;
    }
    if (g_verbose) Info("%s <- %s", Narrow(dst).c_str(), Narrow(src).c_str());
    return true;
}

// Removes a folder tree we wrote ourselves. Junctions and symbolic links are
// removed as links, never followed.
static bool DeleteTree(const std::wstring& dir)
{
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW((dir + L"\\*").c_str(), &fd);
    if (h != INVALID_HANDLE_VALUE)
    {
        do
        {
            std::wstring n = fd.cFileName;
            if (n == L"." || n == L"..") continue;
            std::wstring p = dir + L"\\" + n;
            if ((fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) && !(fd.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)) DeleteTree(p);
            else if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) RemoveDirectoryW(p.c_str());
            else { SetFileAttributesW(p.c_str(), FILE_ATTRIBUTE_NORMAL); DeleteFileW(p.c_str()); }
        } while (FindNextFileW(h, &fd));
        FindClose(h);
    }
    return RemoveDirectoryW(dir.c_str()) != 0;
}

static std::vector<std::wstring> SubDirs(const std::wstring& dir)
{
    std::vector<std::wstring> out;
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW((dir + L"\\*").c_str(), &fd);
    if (h == INVALID_HANDLE_VALUE) return out;
    do
    {
        std::wstring n = fd.cFileName;
        if (n == L"." || n == L"..") continue;
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) out.push_back(n);
    } while (FindNextFileW(h, &fd));
    FindClose(h);
    return out;
}

// FNV-1a over everything that decides the output, so a folder is rebuilt when
// its declaration or a donor file changed and left alone when nothing did.
static unsigned long long HashBytes(unsigned long long h, const void* p, size_t n)
{
    const unsigned char* b = (const unsigned char*)p;
    for (size_t i = 0; i < n; i++) { h ^= b[i]; h *= 1099511628211ULL; }
    return h;
}

static unsigned long long HashStr(unsigned long long h, const std::string& s)
{
    return HashBytes(h, s.c_str(), s.size() + 1);
}

static unsigned long long HashFileStamp(unsigned long long h, const std::wstring& path)
{
    WIN32_FILE_ATTRIBUTE_DATA fa;
    if (GetFileAttributesExW(path.c_str(), GetFileExInfoStandard, &fa))
    {
        h = HashBytes(h, &fa.nFileSizeLow, sizeof(fa.nFileSizeLow));
        h = HashBytes(h, &fa.ftLastWriteTime, sizeof(fa.ftLastWriteTime));
    }
    return h;
}

// ---------------------------------------------------------------- ini tokens

// The first $TOKEN anywhere in the line, which is what the game's own parser
// matches - a `//` in front of one does not make it a comment. Only the
// upper-case-digits-underscore run is taken.
static std::string FirstToken(const std::string& line)
{
    size_t p = line.find('$');
    if (p == std::string::npos) return std::string();
    std::string out = "$";
    for (size_t q = p + 1; q < line.size(); q++)
    {
        char c = line[q];
        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') out += c;
        else break;
    }
    return out.size() > 1 ? out : std::string();
}

static bool StartsWith(const std::string& s, const char* prefix)
{
    return s.compare(0, strlen(prefix), prefix) == 0;
}

static bool IsRecipeToken(const std::string& t)
{
    return t == "$PRODUCTION" || t == "$CONSUMPTION" || t == "$CONSUMPTION_PER_SECOND";
}

// Does a declared line opening with `mine` replace a donor line opening with
// `theirs`? Equality plus the four families from the header comment.
static bool Replaces(const std::string& mine, const std::string& theirs)
{
    if (mine == theirs) return true;
    if (StartsWith(mine, "$TYPE_") && StartsWith(theirs, "$TYPE_")) return true;
    if (StartsWith(mine, "$NAME") && StartsWith(theirs, "$NAME")) return true;
    if (StartsWith(mine, "$STORAGE") && (StartsWith(theirs, "$STORAGE") || theirs == "$RESOURCE_VISUALIZATION")) return true;
    if (IsRecipeToken(mine) && IsRecipeToken(theirs)) return true;
    return false;
}

static bool DonorLineIsReplaced(const Decl& d, const std::string& token)
{
    // `name =` is emitted as a $NAME_STR without being a `line =`.
    if (!d.name.empty() && Replaces("$NAME_STR", token)) return true;
    for (size_t i = 0; i < d.lines.size(); i++)
    {
        std::string mine = FirstToken(d.lines[i]);
        if (!mine.empty() && Replaces(mine, token)) return true;
    }
    for (size_t i = 0; i < d.strips.size(); i++)
        if (Replaces(d.strips[i], token)) return true;
    return false;
}

// ---------------------------------------------------------------- generation

// $TEXTURE_MTL resolves next to the .mtl file; in a Workshop item that folder
// is the mod's own and the textures are not found. Each such line becomes
// `$TEXTURE <slot> buildings/<path>`, which resolves against media_soviet.
static bool RewriteMaterial(const std::wstring& src, const std::wstring& dst, int* rewritten)
{
    *rewritten = 0;
    std::string text;
    if (!ReadTextFile(src, &text)) { Error("could not read material %s", Narrow(src).c_str()); return false; }
    std::string out;
    std::vector<std::string> lines = SplitLines(text);
    for (size_t i = 0; i < lines.size(); i++)
    {
        const std::string& line = lines[i];
        size_t at = line.find("$TEXTURE_MTL");
        if (at == std::string::npos) { out += line; out += "\r\n"; continue; }
        std::string rest = TrimCopy(line.substr(at + strlen("$TEXTURE_MTL")));
        size_t sp = rest.find_first_of(" \t");
        std::string slot = sp == std::string::npos ? rest : rest.substr(0, sp);
        std::string path = sp == std::string::npos ? std::string() : TrimCopy(rest.substr(sp));
        if (slot.empty()) slot = "0";
        out += line.substr(0, at);
        out += "$TEXTURE " + slot + " buildings/" + path + "\r\n";
        (*rewritten)++;
    }
    if (!WriteTextFileAtomic(dst, out)) { Error("could not write material %s", Narrow(dst).c_str()); return false; }
    return true;
}

// The donor's building.ini with the declared block in front and every line the
// block replaces taken out. In front, because a few base files end in a bare
// `end` and nothing is known about whether the parser stops there.
static bool WriteBuildingIni(const Decl& d, const std::wstring& donorIni, const std::wstring& dst, int* dropped)
{
    *dropped = 0;
    std::string text;
    if (!ReadTextFile(donorIni, &text)) { Error("could not read donor %s", Narrow(donorIni).c_str()); return false; }
    std::string out;
    // No dollar sign anywhere in these header lines: the parser would read it as a keyword.
    out += "; generated by TesmioLoader plugins\\buildings_plus.dll - section [" + d.section + "]\r\n";
    out += "; donor: media_soviet\\buildings_types\\" + d.donor + ".ini\r\n";
    out += "; edits here are overwritten on the next launch - change buildings_plus.ini instead\r\n\r\n";
    if (!d.name.empty()) out += "$NAME_STR \"" + d.name + "\"\r\n";
    for (size_t i = 0; i < d.lines.size(); i++) out += d.lines[i] + "\r\n";
    out += "\r\n";
    std::vector<std::string> lines = SplitLines(text);
    bool dropping = false;
    for (size_t i = 0; i < lines.size(); i++)
    {
        std::string token = FirstToken(lines[i]);
        if (!token.empty())
        {
            dropping = DonorLineIsReplaced(d, token);
            if (dropping) { (*dropped)++; if (g_verbose) Info("dropped donor line: %s", lines[i].c_str()); continue; }
        }
        else if (dropping)
        {
            // A token's data lines follow it without a token of their own: the
            // points of a $CONNECTION_*, the placement of a $RESOURCE_VISUALIZATION.
            // They go with the token; a blank or separator line ends the block.
            std::string bare = TrimCopy(lines[i]);
            if (!bare.empty() && bare[0] != '-' && bare[0] != ';') { (*dropped)++; if (g_verbose) Info("dropped donor line: %s", lines[i].c_str()); continue; }
            dropping = false;
        }
        out += lines[i] + "\r\n";
    }
    if (!WriteTextFileAtomic(dst, out)) { Error("could not write %s", Narrow(dst).c_str()); return false; }
    return true;
}

static bool WriteWorkshopConfig(const Decl& d, const std::wstring& dst)
{
    std::string out;
    out += "$ITEM_ID " + d.id + "\r\n\r\n";
    out += "$OWNER_ID 0\r\n\r\n";
    out += "$ITEM_TYPE WORKSHOP_ITEMTYPE_BUILDING\r\n\r\n";
    out += "$VISIBILITY 0\r\n";
    out += "$OBJECT_BUILDING " + d.object + "\r\n\r\n";
    out += "$ITEM_NAME \"" + (d.name.empty() ? d.object : d.name) + "\"\r\n\r\n";
    out += "$ITEM_DESC \"" + (d.desc.empty() ? std::string("Generated by buildings_plus.") : d.desc) + "\"\r\n\r\n";
    out += "$END\r\n";
    return WriteTextFileAtomic(dst, out);
}

// MATERIALEMISSIVE is not optional when the donor has an _e.mtl: a mesh built
// for the lit-window glow renders with a null node array without it and takes
// the process down on the first frame.
static bool WriteRenderConfig(const Decl& d, const std::wstring& dst, bool emissive)
{
    std::string out;
    out += "$TYPE_WORKSHOP\r\n";
    out += " MODEL model.nmf\r\n";
    out += " MATERIAL ../material.mtl\r\n";
    if (emissive) out += " MATERIALEMISSIVE ../material_e.mtl\r\n";
    char life[64];
    _snprintf_s(life, sizeof(life), _TRUNCATE, " LIFE %d.000000\r\n", d.life);
    out += life;
    out += " EXPLOSION_GROUP 0\r\n";
    out += " DERBIS_FALLING_FX buildingfall1 1.000000\r\n";
    out += " DERBIS_FALLED_FX buildingfall2 1.400000\r\n";
    out += " DERBIS_FALLED_SFX collapse\r\n";
    out += " DERBIS_NUM 20\r\n";
    out += " DERBIS_FALLING_FX_MAXTIME 3.000000\r\n";
    out += " DERBIS_SCALE 1.000000\r\n";
    out += " DERBIS_MESH buildings/buildingwreck1.nmf buildings/buildingwreck.mtl\r\n";
    out += " DERBIS_MESH buildings/buildingwreck2.nmf buildings/buildingwreck.mtl\r\n";
    out += " END\r\n";
    return WriteTextFileAtomic(dst, out);
}

static unsigned long long DeclHash(const Decl& d)
{
    unsigned long long h = 14695981039346656037ULL;
    int ver = GENERATOR_VERSION;
    h = HashBytes(h, &ver, sizeof(ver));
    h = HashStr(h, d.id);
    h = HashStr(h, d.object);
    h = HashStr(h, d.donor);
    h = HashStr(h, d.name);
    h = HashStr(h, d.desc);
    h = HashBytes(h, &d.life, sizeof(d.life));
    for (size_t i = 0; i < d.lines.size(); i++) h = HashStr(h, d.lines[i]);
    for (size_t i = 0; i < d.strips.size(); i++) h = HashStr(h, d.strips[i]);
    // The donor's own files, so a game patch that changes them regenerates too.
    std::wstring donor = Wide(d.donor);
    h = HashFileStamp(h, g_media + L"\\buildings_types\\" + donor + L".ini");
    h = HashFileStamp(h, g_media + L"\\buildings\\" + donor + L".mtl");
    h = HashFileStamp(h, g_media + L"\\buildings\\" + donor + L"_e.mtl");
    h = HashFileStamp(h, g_media + L"\\buildings\\" + donor + L".nmf");
    return h;
}

static std::wstring OutRoot()
{
    bool absolute = (g_outDir.size() > 1 && g_outDir[1] == L':') || (!g_outDir.empty() && (g_outDir[0] == L'\\' || g_outDir[0] == L'/'));
    return absolute ? g_outDir : g_gameDir + L"\\" + g_outDir;
}

static bool ReadStamp(const std::wstring& item, std::string* text)
{
    return ReadTextFile(item + L"\\" + Wide(STAMP_NAME), text);
}

static bool StampMatches(const std::string& stamp, unsigned long long want)
{
    size_t at = stamp.find("hash=");
    if (at == std::string::npos) return false;
    unsigned long long have = _strtoui64(stamp.c_str() + at + 5, NULL, 16);
    return have == want;
}

static GenResult Generate(const Decl& d)
{
    std::wstring donor = Wide(d.donor);
    std::wstring donorIni = g_media + L"\\buildings_types\\" + donor + L".ini";
    if (!FileExists(donorIni))
    {
        Error("[%s] donor \"%s\" has no %s - nothing generated", d.section.c_str(), d.donor.c_str(), Narrow(donorIni).c_str());
        return GEN_FAILED;
    }

    std::wstring outRoot = OutRoot();
    std::wstring item = outRoot + L"\\" + Wide(d.id);
    std::wstring obj = item + L"\\" + Wide(d.object);
    unsigned long long want = DeclHash(d);

    // An id collision with a real Workshop item would otherwise overwrite
    // somebody's subscription. A folder we did not write has no stamp.
    if (DirExists(item))
    {
        std::string stamp;
        if (!ReadStamp(item, &stamp))
        {
            Error("[%s] %s exists and was not written by this plugin (no %s) - refusing to touch it; give the section another id",
                  d.section.c_str(), Narrow(item).c_str(), STAMP_NAME);
            return GEN_FAILED;
        }
        if (!g_always && StampMatches(stamp, want))
        {
            Info("[%s] -> %s up to date", d.section.c_str(), d.id.c_str());
            return GEN_UP_TO_DATE;
        }
        // A renamed object leaves its old subfolder behind otherwise.
        std::vector<std::wstring> subs = SubDirs(item);
        for (size_t i = 0; i < subs.size(); i++)
            if (_wcsicmp(subs[i].c_str(), Wide(d.object).c_str()) != 0)
            {
                Info("[%s] removing stale object folder %s", d.section.c_str(), Narrow(subs[i]).c_str());
                DeleteTree(item + L"\\" + subs[i]);
            }
    }

    // media_soviet\workshop_wip may itself be absent on a clean install.
    EnsureDir(outRoot);
    if (!EnsureDir(item)) { Error("[%s] could not create %s (%lu)", d.section.c_str(), Narrow(item).c_str(), GetLastError()); return GEN_FAILED; }
    if (!EnsureDir(obj))  { Error("[%s] could not create %s (%lu)", d.section.c_str(), Narrow(obj).c_str(), GetLastError()); return GEN_FAILED; }

    bool ok = true;
    ok &= CopyAsset(g_media + L"\\buildings\\" + donor + L".nmf", obj + L"\\model.nmf", true, "mesh");
    ok &= CopyAsset(g_media + L"\\buildings_types\\" + donor + L".bbox", obj + L"\\building.bbox", false, "collision box");
    ok &= CopyAsset(g_media + L"\\buildings_types\\" + donor + L".fire", obj + L"\\building.fire", false, "fire points");
    std::wstring icon = g_media + L"\\editor\\tool_" + donor + L".png";
    ok &= CopyAsset(icon, obj + L"\\imagegui.png", false, "build-menu icon");
    CopyAsset(icon, item + L"\\previewimage.png", false, "preview image");

    int rewrote = 0, rewroteE = 0;
    std::wstring mtl = g_media + L"\\buildings\\" + donor + L".mtl";
    if (FileExists(mtl)) ok &= RewriteMaterial(mtl, item + L"\\material.mtl", &rewrote);
    else { Error("[%s] no material %s", d.section.c_str(), Narrow(mtl).c_str()); ok = false; }

    bool emissive = false;
    std::wstring mtlE = g_media + L"\\buildings\\" + donor + L"_e.mtl";
    if (FileExists(mtlE)) { emissive = RewriteMaterial(mtlE, item + L"\\material_e.mtl", &rewroteE); ok &= emissive; }

    ok &= WriteWorkshopConfig(d, item + L"\\workshopconfig.ini");
    ok &= WriteRenderConfig(d, obj + L"\\renderconfig.ini", emissive);
    int dropped = 0;
    ok &= WriteBuildingIni(d, donorIni, obj + L"\\building.ini", &dropped);

    if (!ok)
    {
        Error("[%s] -> %s INCOMPLETE - the game may refuse it or crash on it", d.section.c_str(), d.id.c_str());
        return GEN_FAILED;
    }

    char hash[32];
    _snprintf_s(hash, sizeof(hash), _TRUNCATE, "%016llX", want);
    std::string stampText = std::string(STAMP_MARK) + " (TesmioLoader plugins\\buildings_plus.dll).\r\n"
        "section=" + d.section + " donor=" + d.donor + " object=" + d.object + "\r\n"
        "hash=" + hash + "\r\n"
        "Delete this file to make the plugin leave the folder alone; delete the folder to have it written again.\r\n";
    WriteTextFileAtomic(item + L"\\" + Wide(STAMP_NAME), stampText);

    Note("[%s] -> %s\\%s from \"%s\": %d line(s) in, %d donor line(s) out, %d texture path(s) rewritten%s",
         d.section.c_str(), d.id.c_str(), d.object.c_str(), d.donor.c_str(),
         (int)d.lines.size() + (d.name.empty() ? 0 : 1), dropped, rewrote + rewroteE,
         emissive ? ", emissive material" : "");
    return GEN_GENERATED;
}

// __try needs a frame without objects that unwind; Generate keeps its own.
static void GenerateGuarded(const Decl* d, GenResult* out)
{
    __try { *out = Generate(*d); }
    __except (FaultFilter("buildings_plus generate", GetExceptionInformation()))
    {
        Error("[%s] faulted while generating - skipped", d->section.c_str());
        *out = GEN_FAILED;
    }
}

// Removes our own stamped folders whose declaration no longer exists or is
// switched off. Folders without a stamp, and stamped folders of another
// generator, are never touched.
static int Prune(const std::vector<std::string>& keep)
{
    int removed = 0;
    std::wstring outRoot = OutRoot();
    std::vector<std::wstring> subs = SubDirs(outRoot);
    for (size_t i = 0; i < subs.size(); i++)
    {
        std::string name = Narrow(subs[i]);
        bool kept = false;
        for (size_t k = 0; k < keep.size(); k++) if (keep[k] == name) { kept = true; break; }
        if (kept) continue;
        std::string stamp;
        if (!ReadStamp(outRoot + L"\\" + subs[i], &stamp)) continue;
        if (stamp.find(STAMP_MARK) == std::string::npos) continue;
        if (DeleteTree(outRoot + L"\\" + subs[i])) { Note("prune: removed %s (no declaration for it any more)", name.c_str()); removed++; }
        else Warn("prune: could not remove %s (%lu)", name.c_str(), GetLastError());
    }
    return removed;
}

// ---------------------------------------------------------------- validation

static bool AllDigits(const std::string& s)
{
    if (s.empty()) return false;
    for (size_t i = 0; i < s.size(); i++) if (s[i] < '0' || s[i] > '9') return false;
    return true;
}

static bool SafeName(const std::string& s)
{
    if (s.empty() || s.size() > 64) return false;
    for (size_t i = 0; i < s.size(); i++)
    {
        char c = s[i];
        if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) return false;
    }
    return true;
}

static bool PlainText(const std::string& s)
{
    for (size_t i = 0; i < s.size(); i++)
    {
        unsigned char c = (unsigned char)s[i];
        if (c == '"' || (c < 32 && c != '\n')) return false;
    }
    return true;
}

static bool ValidToken(const std::string& t)
{
    return t.size() > 1 && t[0] == '$' && FirstToken(t) == t;
}

static bool ValidateDecl(const Decl& d, std::string* why)
{
    char buf[256];
    if (d.id.empty()) { *why = "has no id and none could be assigned"; return false; }
    if (!AllDigits(d.id) || d.id.size() > 19) { *why = "id must be a number"; return false; }
    unsigned long long id = _strtoui64(d.id.c_str(), NULL, 10);
    if (id < MIN_ITEM_ID || id > MAX_ITEM_ID)
    {
        _snprintf_s(buf, sizeof(buf), _TRUNCATE, "id must lie between %llu and %llu to stay clear of real Workshop items", MIN_ITEM_ID, MAX_ITEM_ID);
        *why = buf; return false;
    }
    if (d.donor.empty()) { *why = "has no donor"; return false; }
    if (!SafeName(d.donor)) { *why = "donor must be a plain buildings_types name (letters, digits, _ and -)"; return false; }
    if (!SafeName(d.object)) { *why = "object must be letters, digits, _ or - (at most 64)"; return false; }
    if (d.name.size() > 128 || !PlainText(d.name)) { *why = "name must be plain text without quotes (at most 128 characters)"; return false; }
    if (d.desc.size() > 4096 || !PlainText(d.desc)) { *why = "desc must be plain text without quotes (at most 4096 characters)"; return false; }
    if (d.life < 1 || d.life > 1000000) { *why = "life must lie between 1 and 1000000"; return false; }
    if (d.lines.size() > MAX_LINES) { *why = "too many line keys"; return false; }
    for (size_t i = 0; i < d.lines.size(); i++)
    {
        if (d.lines[i].size() > MAX_LINE_LEN) { *why = "a line is too long"; return false; }
        if (FirstToken(d.lines[i]).empty()) { _snprintf_s(buf, sizeof(buf), _TRUNCATE, "line %u has no $TOKEN: %.60s", (unsigned)(i + 1), d.lines[i].c_str()); *why = buf; return false; }
        if (!PlainText(d.lines[i]) && d.lines[i].find('"') == std::string::npos) { *why = "a line contains control characters"; return false; }
    }
    for (size_t i = 0; i < d.strips.size(); i++)
        if (!ValidToken(d.strips[i])) { _snprintf_s(buf, sizeof(buf), _TRUNCATE, "strip must be a single $TOKEN: %.60s", d.strips[i].c_str()); *why = buf; return false; }
    return true;
}

// ---------------------------------------------------------------- the config

// [buildings_plus] holds the switches ([buildings] is accepted for the same
// purpose); every other section is a building. Parsed by hand, because `line`
// and `desc` are deliberately repeatable.
static bool IsSwitchSection(const std::string& name)
{
    return _stricmp(name.c_str(), "buildings_plus") == 0 || _stricmp(name.c_str(), "buildings") == 0;
}

static bool ParseBool(const std::string& v, bool* out)
{
    if (v == "0") { *out = false; return true; }
    if (v == "1") { *out = true; return true; }
    return false;
}

static void LoadRegistry(const std::string& text)
{
    g_decls.clear();
    std::vector<std::string> lines = SplitLines(text);
    Decl* d = NULL;
    bool inSwitches = false;
    for (size_t li = 0; li < lines.size(); li++)
    {
        std::string line = TrimCopy(lines[li]);
        size_t no = li + 1;
        if (line.empty() || line[0] == ';' || line[0] == '#') continue;
        if (line[0] == '[')
        {
            d = NULL; inSwitches = false;
            size_t end = line.find(']');
            if (end == std::string::npos) { Warn("line %u: section header without ]", (unsigned)no); continue; }
            std::string name = TrimCopy(line.substr(1, end - 1));
            if (IsSwitchSection(name)) { inSwitches = true; continue; }
            if (!SafeName(name)) { Warn("line %u: section name [%s] must be letters, digits, _ or - - ignored", (unsigned)no, name.c_str()); continue; }
            if (g_decls.size() >= MAX_DECLS) { Warn("line %u: at most %d building sections - [%s] ignored", (unsigned)no, MAX_DECLS, name.c_str()); continue; }
            g_decls.push_back(Decl());
            d = &g_decls.back();
            d->section = name;
            d->object = name;
            d->configLine = no;
            continue;
        }
        size_t eq = line.find('=');
        if (eq == std::string::npos) { Warn("line %u: no key = value", (unsigned)no); continue; }
        std::string key = TrimCopy(line.substr(0, eq));
        std::string val = TrimCopy(line.substr(eq + 1));
        if (inSwitches)
        {
            bool b;
            if      (_stricmp(key.c_str(), "enabled") == 0) { if (ParseBool(val, &b)) g_enabled = b; else Warn("line %u: enabled must be 0 or 1", (unsigned)no); }
            else if (_stricmp(key.c_str(), "always")  == 0) { if (ParseBool(val, &b)) g_always  = b; else Warn("line %u: always must be 0 or 1", (unsigned)no); }
            else if (_stricmp(key.c_str(), "verbose") == 0) { if (ParseBool(val, &b)) g_verbose = b; else Warn("line %u: verbose must be 0 or 1", (unsigned)no); }
            else if (_stricmp(key.c_str(), "prune")   == 0) { if (ParseBool(val, &b)) g_prune   = b; else Warn("line %u: prune must be 0 or 1", (unsigned)no); }
            else if (_stricmp(key.c_str(), "out")     == 0) { if (!val.empty() && val.find("..") == std::string::npos) g_outDir = Wide(val); else Warn("line %u: out must be a folder without ..", (unsigned)no); }
            else Warn("line %u: unknown switch \"%s\" - ignored", (unsigned)no, key.c_str());
            continue;
        }
        if (!d) { Warn("line %u: key outside any section - ignored", (unsigned)no); continue; }
        if      (_stricmp(key.c_str(), "id")      == 0) d->id = val;
        else if (_stricmp(key.c_str(), "object")  == 0) d->object = val;
        else if (_stricmp(key.c_str(), "donor")   == 0) d->donor = val;
        else if (_stricmp(key.c_str(), "name")    == 0) d->name = val;
        else if (_stricmp(key.c_str(), "desc")    == 0) { if (!d->desc.empty()) d->desc += "\n"; d->desc += val; }
        else if (_stricmp(key.c_str(), "life")    == 0) d->life = atoi(val.c_str());
        else if (_stricmp(key.c_str(), "enabled") == 0) { bool b; if (ParseBool(val, &b)) d->enabled = b; else Warn("line %u: [%s] enabled must be 0 or 1", (unsigned)no, d->section.c_str()); }
        else if (_stricmp(key.c_str(), "line")    == 0) { if (!val.empty()) d->lines.push_back(val); }
        else if (_stricmp(key.c_str(), "strip")   == 0) { if (!val.empty()) d->strips.push_back(val); }
        else Warn("line %u: [%s] unknown key \"%s\" - ignored", (unsigned)no, d->section.c_str(), key.c_str());
    }
    // Two sections with one id would fight over one folder.
    for (size_t i = 0; i < g_decls.size(); i++)
        for (size_t k = i + 1; k < g_decls.size(); k++)
            if (!g_decls[i].id.empty() && g_decls[i].id == g_decls[k].id && g_decls[i].valid && g_decls[k].valid)
            {
                Error("[%s] and [%s] share id %s - both skipped", g_decls[i].section.c_str(), g_decls[k].section.c_str(), g_decls[i].id.c_str());
                g_decls[i].valid = false; g_decls[k].valid = false;
            }
}

// plugins\buildings_plus.ini when it exists (the classic install, or the
// effective INI Republic Mod Manager writes), otherwise the INI beside the DLL
// (the Workshop package under Soviet Mod Loader or the Workshop Bridge).
static std::wstring ConfigPath()
{
    std::wstring ini = WideAnsi(H->pluginDir) + L"\\buildings_plus.ini";
    if (FileExists(ini)) return ini;
    static const char anchor = 0;
    HMODULE self = NULL;
    wchar_t own[MAX_PATH];
    if (GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCWSTR)&anchor, &self) &&
        GetModuleFileNameW(self, own, MAX_PATH) > 0)
    {
        std::wstring beside = own;
        size_t slash = beside.find_last_of(L'\\');
        if (slash != std::wstring::npos)
        {
            beside = beside.substr(0, slash) + L"\\buildings_plus.ini";
            if (FileExists(beside)) return beside;
        }
    }
    return ini;
}

// ---------------------------------------------------------------- id catalog

// plugins\buildings_plus.ids.ini: `[ids]` with one `<section> = <id>` per
// building that ever had a number assigned or declared. Append-only in spirit:
// an entry is never removed here, so a section that disappears and comes back
// gets its old folder, and a number is never handed out twice.
struct CatalogEntry { std::string key; std::string id; };
static std::vector<CatalogEntry> g_catalog;
static bool g_catalogDirty = false;

static std::string LowerCopy(const std::string& s)
{
    std::string out = s;
    for (size_t i = 0; i < out.size(); i++) if (out[i] >= 'A' && out[i] <= 'Z') out[i] = (char)(out[i] - 'A' + 'a');
    return out;
}

static std::wstring CatalogPath()
{
    return WideAnsi(H->pluginDir) + L"\\" + Wide(CATALOG_NAME);
}

static void LoadCatalog()
{
    g_catalog.clear();
    g_catalogDirty = false;
    std::string text;
    if (!ReadTextFile(CatalogPath(), &text)) return;
    std::vector<std::string> lines = SplitLines(text);
    bool inIds = false;
    for (size_t i = 0; i < lines.size(); i++)
    {
        std::string line = TrimCopy(lines[i]);
        if (line.empty() || line[0] == ';' || line[0] == '#') continue;
        if (line[0] == '[')
        {
            size_t close = line.find(']');
            std::string name = close == std::string::npos ? line.substr(1) : line.substr(1, close - 1);
            inIds = _stricmp(TrimCopy(name).c_str(), "ids") == 0;
            continue;
        }
        if (!inIds) continue;
        size_t eq = line.find('=');
        if (eq == std::string::npos) continue;
        std::string key = LowerCopy(TrimCopy(line.substr(0, eq)));
        std::string val = TrimCopy(line.substr(eq + 1));
        if (key.empty() || !AllDigits(val) || val.size() > 19) continue;
        CatalogEntry e; e.key = key; e.id = val;
        g_catalog.push_back(e);
    }
}

static const CatalogEntry* CatalogFind(const std::string& key)
{
    for (size_t i = 0; i < g_catalog.size(); i++) if (g_catalog[i].key == key) return &g_catalog[i];
    return NULL;
}

static void CatalogSet(const std::string& key, const std::string& id)
{
    for (size_t i = 0; i < g_catalog.size(); i++)
        if (g_catalog[i].key == key) { if (g_catalog[i].id != id) { g_catalog[i].id = id; g_catalogDirty = true; } return; }
    CatalogEntry e; e.key = key; e.id = id;
    g_catalog.push_back(e);
    g_catalogDirty = true;
}

static bool SaveCatalog()
{
    if (!g_catalogDirty) return true;
    std::string out;
    out += "; Written by TesmioLoader plugins\\buildings_plus.dll - the Workshop ids of the\r\n";
    out += "; buildings declared in buildings_plus.ini, one per section name. A number\r\n";
    out += "; stays with its section for good, because saved games refer to the folder\r\n";
    out += "; media_soviet\\workshop_wip\\<id>. Keep this file with your saved games; do not\r\n";
    out += "; edit it by hand.\r\n\r\n[ids]\r\n";
    for (size_t i = 0; i < g_catalog.size(); i++) out += g_catalog[i].key + " = " + g_catalog[i].id + "\r\n";
    if (!WriteTextFileAtomic(CatalogPath(), out)) { Error("could not write %s - assigned ids would not survive the next start", Narrow(CatalogPath()).c_str()); return false; }
    g_catalogDirty = false;
    return true;
}

static unsigned long long IdValue(const std::string& id)
{
    return AllDigits(id) && !id.empty() && id.size() <= 19 ? _strtoui64(id.c_str(), NULL, 10) : 0ULL;
}

// Sections without `id =` get the highest number in the automatic range seen
// anywhere - catalog, explicit ids, folders under workshop_wip (foreign ones
// too) - plus one, unless the catalog already knows the section.
static void AssignIds()
{
    LoadCatalog();
    unsigned long long highest = 0;
    std::vector<unsigned long long> taken;
    for (size_t i = 0; i < g_catalog.size(); i++)
    {
        unsigned long long v = IdValue(g_catalog[i].id);
        taken.push_back(v);
        if (v >= AUTO_MIN_ID && v <= AUTO_MAX_ID && v > highest) highest = v;
    }
    for (size_t i = 0; i < g_decls.size(); i++)
    {
        if (!g_decls[i].valid || g_decls[i].id.empty()) continue;
        unsigned long long v = IdValue(g_decls[i].id);
        if (!v) continue;
        taken.push_back(v);
        if (v >= AUTO_MIN_ID && v <= AUTO_MAX_ID && v > highest) highest = v;
        CatalogSet(LowerCopy(g_decls[i].section), g_decls[i].id);
    }
    std::vector<std::wstring> subs = SubDirs(OutRoot());
    for (size_t i = 0; i < subs.size(); i++)
    {
        unsigned long long v = IdValue(Narrow(subs[i]));
        if (!v) continue;
        taken.push_back(v);
        if (v >= AUTO_MIN_ID && v <= AUTO_MAX_ID && v > highest) highest = v;
    }
    for (size_t i = 0; i < g_decls.size(); i++)
    {
        Decl& d = g_decls[i];
        if (!d.valid || !d.id.empty()) continue;
        std::string key = LowerCopy(d.section);
        const CatalogEntry* known = CatalogFind(key);
        if (known)
        {
            // The catalog's number, unless another section now declares it explicitly.
            bool clash = false;
            for (size_t k = 0; k < g_decls.size(); k++)
                if (k != i && g_decls[k].valid && g_decls[k].id == known->id) { clash = true; break; }
            if (!clash) { d.id = known->id; Info("[%s] id %s from the catalog", d.section.c_str(), d.id.c_str()); continue; }
            Warn("[%s] its catalog id %s is now declared by another section - assigning a new one", d.section.c_str(), known->id.c_str());
        }
        unsigned long long next = highest < AUTO_MIN_ID ? AUTO_MIN_ID : highest + 1;
        for (;;)
        {
            bool used = false;
            for (size_t k = 0; k < taken.size(); k++) if (taken[k] == next) { used = true; break; }
            if (!used) break;
            next++;
        }
        if (next > AUTO_MAX_ID)
        {
            Error("[%s] no free id left between %llu and %llu - skipped", d.section.c_str(), AUTO_MIN_ID, AUTO_MAX_ID);
            d.valid = false;
            continue;
        }
        char buf[32];
        _snprintf_s(buf, sizeof(buf), _TRUNCATE, "%llu", next);
        d.id = buf;
        highest = next;
        taken.push_back(next);
        CatalogSet(key, d.id);
        Note("[%s] assigned id %s (kept in plugins\\%s)", d.section.c_str(), d.id.c_str(), CATALOG_NAME);
    }
    SaveCatalog();
}

// The game's own folder from the executable, not from the working directory:
// anything in the game may call SetCurrentDirectory, and the world editor does.
static bool ResolveGameDir()
{
#ifdef BUILDINGS_PLUS_TEST
    if (!g_testGameDir.empty()) { g_gameDir = g_testGameDir; g_media = g_gameDir + L"\\media_soviet"; return DirExists(g_media); }
#endif
    wchar_t exe[MAX_PATH];
    if (!GetModuleFileNameW((HMODULE)H->exeModule, exe, MAX_PATH)) return false;
    std::wstring path = exe;
    size_t slash = path.find_last_of(L'\\');
    if (slash == std::wstring::npos) return false;
    g_gameDir = path.substr(0, slash);
    g_media = g_gameDir + L"\\media_soviet";
    return DirExists(g_media);
}

// Everything happens here, and it has to: plugins are initialised from DllMain
// before the game's main thread runs, so a folder written now is found when
// the game scans workshop_wip. Returns 0 (stay listed) even when nothing is
// declared - Republic Mod Manager shows an empty registry as a normal state.
static int Run(const std::string& configText, const char* configName)
{
    g_decls.clear();
    LoadRegistry(configText);
    if (!g_enabled)
    {
        Note("enabled = 0 - no buildings generated (folders written earlier stay)");
        return 1;
    }
    if (!ResolveGameDir())
    {
        Error("could not find media_soviet beside SOVIET64.exe - nothing generated");
        return 1;
    }
    Info("configuration: %s; %u section(s); out = %s", configName, (unsigned)g_decls.size(), Narrow(OutRoot()).c_str());
    AssignIds();

    std::vector<std::string> keep;
    int generated = 0, upToDate = 0, skipped = 0, failed = 0;
    for (size_t i = 0; i < g_decls.size(); i++)
    {
        Decl& d = g_decls[i];
        std::string why;
        if (!d.valid) { skipped++; continue; }
        if (!ValidateDecl(d, &why))
        {
            Error("[%s] (line %u) %s - skipped", d.section.c_str(), (unsigned)d.configLine, why.c_str());
            d.valid = false; skipped++; continue;
        }
        if (!d.enabled) { Info("[%s] enabled = 0 - skipped", d.section.c_str()); skipped++; continue; }
        keep.push_back(d.id);
        GenResult r = GEN_FAILED;
        GenerateGuarded(&d, &r);
        if (r == GEN_GENERATED) generated++;
        else if (r == GEN_UP_TO_DATE) upToDate++;
        else failed++;
    }
    int pruned = g_prune ? Prune(keep) : 0;
    Note("%d generated, %d up to date, %d skipped, %d failed, %d pruned; %d warning(s), %d error(s)",
         generated, upToDate, skipped, failed, pruned, g_warnings, g_errors);
    return 0;
}

extern "C" __declspec(dllexport) unsigned TsmPluginApiVersion(void)
{
    return TSM_API_VERSION;
}

extern "C" __declspec(dllexport) int TsmPluginInit(const TsmHost* host, TsmPluginInfo* info)
{
    TsmBind(host);
    info->name    = "buildings_plus";
    info->version = PLUGIN_VERSION;
    g_detail = TsmOpenLog(PLUGIN_LOG_NAME);
    Note("TesmioLoader buildings_plus %s starting; detail log: logs\\%s", PLUGIN_VERSION, PLUGIN_LOG_NAME);

    std::wstring cfg = ConfigPath();
    std::string text;
    if (!ReadTextFile(cfg, &text))
    {
        Note("no %s - nothing declared", Narrow(cfg).c_str());
        return 0;
    }
    return Run(text, Narrow(cfg).c_str());
}

BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID) { return TRUE; }

// ---------------------------------------------------------------- self-test

#ifdef BUILDINGS_PLUS_TEST
#include <stdio.h>

static void TestLog(const char* fmt, ...) { va_list ap; va_start(ap, fmt); vprintf(fmt, ap); va_end(ap); printf("\n"); }
static long TestFault(const char*, void*) { return EXCEPTION_EXECUTE_HANDLER; }

static bool Has(const std::string& text, const char* wanted) { return text.find(wanted) != std::string::npos; }
static int g_failed = 0;
static void Check(bool ok, const char* what) { if (ok) printf("PASS %s\n", what); else { printf("FAIL %s\n", what); g_failed++; } }
static void Put(const std::wstring& path, const std::string& text) { WriteTextFileAtomic(path, text); }

// --run <ini> <game folder> <out folder>: generates the sections of a real INI
// from a real game folder into an out folder of your choice (never into the
// game), so a declaration can be checked before it goes into the game.
static int RunConfig(const std::wstring& ini, const std::wstring& game, const std::wstring& out)
{
    static TsmHost host;
    memset(&host, 0, sizeof(host));
    host.apiVersion = TSM_API_VERSION; host.structSize = sizeof(host);
    static std::string baseA, pluginsA;
    EnsureDir(out);
    baseA = Narrow(out); pluginsA = Narrow(out + L"\\plugins");
    host.baseDir = baseA.c_str(); host.pluginDir = pluginsA.c_str();
    host.log = TestLog; host.faultFilter = TestFault;
    TsmBind(&host);
    g_testGameDir = game;
    g_detail = TsmOpenLog(PLUGIN_LOG_NAME);
    std::string text;
    if (!ReadTextFile(ini, &text)) { printf("cannot read %ls\n", ini.c_str()); return 2; }
    // The out folder of the command line wins over any `out` in the file.
    text += "\n[buildings_plus]\nout = " + Narrow(out) + "\n";
    int rc = Run(text, Narrow(ini).c_str());
    printf("RESULT rc=%d, %d warning(s), %d error(s); output under %ls\n", rc, g_warnings, g_errors, out.c_str());
    return g_errors ? 1 : 0;
}

int wmain(int argc, wchar_t** argv)
{
    if (argc == 5 && wcscmp(argv[1], L"--run") == 0) return RunConfig(argv[2], argv[3], argv[4]);
    if (argc < 2) { printf("usage: buildings_plus_test <fresh scratch folder>\n       buildings_plus_test --run <ini> <game folder> <out folder>\n"); return 2; }
    std::wstring root = argv[1];
    if (DirExists(root)) { printf("scratch folder must not exist yet: %ls\n", root.c_str()); return 2; }
    std::wstring game = root + L"\\game", build = root + L"\\game\\tesmioloader\\build", plugins = build + L"\\plugins";
    EnsureDir(root); EnsureDir(game); EnsureDir(game + L"\\tesmioloader"); EnsureDir(build); EnsureDir(plugins);
    std::wstring media = game + L"\\media_soviet";
    EnsureDir(media); EnsureDir(media + L"\\buildings"); EnsureDir(media + L"\\buildings_types"); EnsureDir(media + L"\\editor");
    Put(media + L"\\buildings_types\\donor_mine.ini",
        "$NAME 6160\n$TYPE_FACTORY\n$WORKERS_NEEDED 10\n$PRODUCTION coal 1.0\n$CONSUMPTION eletric 0.1\n"
        "$STORAGE_EXPORT RESOURCE_TRANSPORT_GRAVEL 20\n$RESOURCE_VISUALIZATION 0\npositon 5 0 5\nscale 1 1 1\n\n$CONNECTION_ROAD\n1 0 0\n2 0 0\n"
        "$COST_WORK SOVIET_CONSTRUCTION_GROUNDWORKS 1.0\n$PARTICLE smoke 1 2 3\nend\n");
    Put(media + L"\\buildings_types\\donor_mine.bbox", "bbox");
    Put(media + L"\\buildings_types\\donor_mine.fire", "fire");
    Put(media + L"\\buildings\\donor_mine.nmf", "mesh");
    Put(media + L"\\buildings\\donor_mine.mtl", "$SUBMATERIAL x\n$TEXTURE_MTL 0 donor_mine.dds\n$TEXTURE 1 buildings/other.dds\n");
    Put(media + L"\\buildings\\donor_mine_e.mtl", "$TEXTURE_MTL 0 donor_mine_e.dds\n");
    Put(media + L"\\editor\\tool_donor_mine.png", "png");

    static TsmHost host;
    memset(&host, 0, sizeof(host));
    host.apiVersion = TSM_API_VERSION; host.structSize = sizeof(host);
    std::string baseA = Narrow(build), pluginsA = Narrow(plugins);
    host.baseDir = baseA.c_str(); host.pluginDir = pluginsA.c_str();
    host.log = TestLog; host.faultFilter = TestFault;
    TsmBind(&host);
    g_testGameDir = game;
    g_detail = TsmOpenLog(PLUGIN_LOG_NAME);

    std::string ini =
        "[buildings_plus]\nenabled = 1\nverbose = 1\nprune = 1\n\n"
        "[salt_mine]\nid = 9100000021\ndonor = donor_mine\nobject = SaltMine\nname = Salt Mine\ndesc = First line\ndesc = Second line\n"
        "line = $PRODUCTION raw_salt 1.0\nline = $STORAGE_EXPORT RESOURCE_TRANSPORT_OPEN 20\nstrip = $WORKERS_NEEDED\n\n"
        "[bad_id]\nid = 123\ndonor = donor_mine\n\n"
        "[bad_name]\nid = 9100000022\ndonor = donor_mine\nname = Say \"hi\"\n\n"
        "[no_donor]\nid = 9100000023\ndonor = missing_donor\n\n"
        "[off]\nid = 9100000024\ndonor = donor_mine\nenabled = 0\n\n"
        "[collide]\nid = 9100000025\ndonor = donor_mine\n";
    std::wstring out = media + L"\\workshop_wip";
    EnsureDir(out);
    // A folder that is not ours must never be touched, and a stale stamped folder of ours goes away with prune.
    EnsureDir(out + L"\\9100000025"); Put(out + L"\\9100000025\\workshopconfig.ini", "$ITEM_ID 9100000025\n");
    EnsureDir(out + L"\\9100000099"); Put(out + L"\\9100000099\\" + Wide(STAMP_NAME), std::string(STAMP_MARK) + "\nhash=0\n");
    EnsureDir(out + L"\\9100000098"); Put(out + L"\\9100000098\\" + Wide(STAMP_NAME), "tesmioloader plugins\\buildings.dll generated this folder.\nhash=0\n");

    g_warnings = 0; g_errors = 0;
    int rc = Run(ini, "test");
    Check(rc == 0, "run returns 0 with a registry");
    std::string b;
    std::wstring item = out + L"\\9100000021", obj = item + L"\\SaltMine";
    Check(ReadTextFile(obj + L"\\building.ini", &b), "building.ini written");
    Check(Has(b, "$NAME_STR \"Salt Mine\"") && !Has(b, "$NAME 6160"), "name replaces the donor's $NAME");
    Check(Has(b, "$PRODUCTION raw_salt 1.0") && !Has(b, "$PRODUCTION coal") && !Has(b, "$CONSUMPTION eletric"), "a declared recipe replaces the donor recipe whole");
    Check(Has(b, "$STORAGE_EXPORT RESOURCE_TRANSPORT_OPEN 20") && !Has(b, "RESOURCE_TRANSPORT_GRAVEL") && !Has(b, "$RESOURCE_VISUALIZATION"), "storages replace storages and the visualisation");
    Check(!Has(b, "positon 5 0 5") && !Has(b, "scale 1 1 1"), "the data lines of a dropped token go with it");
    Check(!Has(b, "$WORKERS_NEEDED"), "strip drops a donor line");
    Check(Has(b, "$CONNECTION_ROAD\r\n1 0 0\r\n2 0 0") && Has(b, "$COST_WORK SOVIET_CONSTRUCTION_GROUNDWORKS 1.0") && Has(b, "$PARTICLE smoke 1 2 3") && Has(b, "$TYPE_FACTORY"), "geometry, costs and type survive");
    std::string m;
    Check(ReadTextFile(item + L"\\material.mtl", &m) && Has(m, "$TEXTURE 0 buildings/donor_mine.dds") && Has(m, "$TEXTURE 1 buildings/other.dds") && !Has(m, "$TEXTURE_MTL"), "$TEXTURE_MTL is rewritten, $TEXTURE kept");
    std::string r;
    Check(ReadTextFile(obj + L"\\renderconfig.ini", &r) && Has(r, "MATERIALEMISSIVE ../material_e.mtl") && Has(r, "LIFE 3000.000000"), "renderconfig names the emissive material");
    std::string w;
    Check(ReadTextFile(item + L"\\workshopconfig.ini", &w) && Has(w, "$OBJECT_BUILDING SaltMine") && Has(w, "$ITEM_NAME \"Salt Mine\"") && Has(w, "First line\nSecond line"), "workshopconfig carries object, name and both desc lines");
    Check(FileExists(obj + L"\\model.nmf") && FileExists(obj + L"\\building.bbox") && FileExists(obj + L"\\building.fire") && FileExists(obj + L"\\imagegui.png") && FileExists(item + L"\\previewimage.png"), "assets copied");
    std::string stamp;
    Check(ReadStamp(item, &stamp) && Has(stamp, STAMP_MARK) && Has(stamp, "hash="), "stamp written");
    Check(!FileExists(out + L"\\9100000022\\workshopconfig.ini"), "a name with a quote is skipped");
    Check(!DirExists(out + L"\\9100000021\\bad_id"), "an id outside the range is skipped");
    Check(!FileExists(out + L"\\9100000025\\" + Wide(STAMP_NAME)) && FileExists(out + L"\\9100000025\\workshopconfig.ini") && !DirExists(out + L"\\9100000025\\collide"), "a folder without a stamp is refused and left alone");
    Check(!DirExists(out + L"\\9100000023"), "a missing donor generates nothing");
    Check(!DirExists(out + L"\\9100000099"), "prune removes our stamped folder without a declaration");
    Check(DirExists(out + L"\\9100000098"), "prune leaves a stamped folder of another generator");
    Check(!DirExists(out + L"\\9100000024"), "a disabled section generates nothing");
    Check(g_errors == 4, "exactly the four bad sections (id, name, donor, no-stamp folder) are errors");

    // Second run: up to date, nothing rewritten.
    WIN32_FILE_ATTRIBUTE_DATA before, after;
    GetFileAttributesExW((obj + L"\\building.ini").c_str(), GetFileExInfoStandard, &before);
    Sleep(30);
    g_warnings = 0; g_errors = 0;
    Run(ini, "test");
    GetFileAttributesExW((obj + L"\\building.ini").c_str(), GetFileExInfoStandard, &after);
    Check(CompareFileTime(&before.ftLastWriteTime, &after.ftLastWriteTime) == 0, "an unchanged declaration is left alone");

    // A renamed object removes the stale subfolder.
    std::string ini2 = ini;
    size_t at = ini2.find("object = SaltMine");
    ini2.replace(at, strlen("object = SaltMine"), "object = SaltMine2");
    Run(ini2, "test");
    Check(DirExists(item + L"\\SaltMine2") && !DirExists(item + L"\\SaltMine"), "renaming the object replaces the object folder");

    // Switching the plugin off touches nothing.
    Run("[buildings_plus]\nenabled = 0\n", "test");
    Check(DirExists(item), "enabled = 0 keeps generated folders");

    // Sections without an id: highest number in the automatic range plus one,
    // counting explicit ids and any folder under workshop_wip, kept in the catalog.
    EnsureDir(out + L"\\9300000003"); Put(out + L"\\9300000003\\workshopconfig.ini", "$ITEM_ID 9300000003\n");
    std::string ini3 =
        "[buildings_plus]\nenabled = 1\n\n"
        "[Auto_One]\ndonor = donor_mine\n\n"
        "[auto_two]\ndonor = donor_mine\n\n"
        "[fixed]\nid = 9300000007\ndonor = donor_mine\n";
    g_warnings = 0; g_errors = 0;
    Run(ini3, "test");
    Check(g_errors == 0 && DirExists(out + L"\\9300000008\\Auto_One") && DirExists(out + L"\\9300000009\\auto_two") && DirExists(out + L"\\9300000007\\fixed"),
          "ids are assigned above the highest number seen (explicit id and foreign folder included)");
    std::string cat;
    Check(ReadTextFile(plugins + L"\\" + Wide(CATALOG_NAME), &cat) && Has(cat, "[ids]") && Has(cat, "auto_one = 9300000008") && Has(cat, "auto_two = 9300000009") && Has(cat, "fixed = 9300000007") && Has(cat, "salt_mine = 9100000021"),
          "the catalog records assigned and explicit ids by section name");
    // Removing an earlier section and adding a new one leaves the old numbers alone.
    std::string ini4 =
        "[buildings_plus]\nenabled = 1\n\n"
        "[auto_two]\ndonor = donor_mine\n\n"
        "[auto_three]\ndonor = donor_mine\n";
    g_warnings = 0; g_errors = 0;
    Run(ini4, "test");
    Check(g_errors == 0 && DirExists(out + L"\\9300000009\\auto_two") && DirExists(out + L"\\9300000010\\auto_three"),
          "a section keeps its catalog id; a new section gets the next number, never a freed one");
    Check(ReadTextFile(plugins + L"\\" + Wide(CATALOG_NAME), &cat) && Has(cat, "auto_one = 9300000008") && Has(cat, "auto_three = 9300000010"),
          "the catalog keeps the entry of a removed section");
    // An explicit id that takes a cataloged number moves the other section to a new one.
    std::string ini5 =
        "[buildings_plus]\nenabled = 1\n\n"
        "[auto_two]\ndonor = donor_mine\n\n"
        "[grabber]\nid = 9300000009\ndonor = donor_mine\n";
    g_warnings = 0; g_errors = 0;
    Run(ini5, "test");
    Check(g_warnings == 1 && g_errors == 0 && DirExists(out + L"\\9300000011\\auto_two") && DirExists(out + L"\\9300000009\\grabber"),
          "an explicit id wins over the catalog and the displaced section is renumbered with a warning");

    printf(g_failed ? "RESULT %d check(s) FAILED\n" : "RESULT all buildings_plus self-tests passed\n", g_failed);
    return g_failed ? 1 : 0;
}
#endif
