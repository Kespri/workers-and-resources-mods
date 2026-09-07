// Offline tests only. Never load the game DLLs or call executable game code.
#define RAIL_PHYSICS_TESTING
#include "../rail_physics_fix.cpp"
#include <vector>
#include <string>
#include <cassert>

static std::string iniPath, testMode;
static int hookCalls, allocations;
static BYTE* image;
static size_t extraUsed;
static int warningMessages, rawMessages;
static void TestLog(const char* fmt, ...)
{
    char line[4096]; va_list copy; va_start(copy, fmt);
    vsnprintf_s(line, sizeof(line), _TRUNCATE, fmt, copy); va_end(copy);
    if (strstr(line,"[WARN]")) ++warningMessages;
    if (strstr(line,"customs:")) ++rawMessages;
    va_list a; va_start(a, fmt); vprintf(fmt, a); va_end(a); puts("");
}
static int TestReadable(const void* p, size_t n)
{
    if (!p || n > ~(UINT_PTR)0 - (UINT_PTR)p) return 0;
    UINT_PTR at = (UINT_PTR)p, end = at+n;
    while (at < end)
    {
        MEMORY_BASIC_INFORMATION mbi;
        if (!VirtualQuery((void*)at, &mbi, sizeof(mbi)) || mbi.State != MEM_COMMIT ||
            mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) return 0;
        at = (UINT_PTR)mbi.BaseAddress + mbi.RegionSize;
    }
    return 1;
}
static int configCalls = 0;
static int TestInt(const char* path, const char* section, const char* key, int fallback)
{
    ++configCalls;
    assert(strcmp(path, "plugins\\rail_physics_fix.ini") == 0);
    if (testMode == "disabled" && !strcmp(key, "enabled")) return 0;
    return GetPrivateProfileIntA(section, key, fallback, iniPath.c_str());
}
static int TestString(const char*, const char* section, const char* key, char* out, int size, const char* fallback)
{
    ++configCalls;
    if (testMode == "bad_numeric" && !strcmp(key, "curve_lateral_ms2"))
        return sprintf_s(out, size, "nan");
    return GetPrivateProfileStringA(section, key, fallback, out, size, iniPath.c_str());
}
static BYTE* TestAlloc(BYTE*, SIZE_T size)
{
    ++allocations;
    assert(extraUsed+size < 65536);
    BYTE* p = image+0xA9D000+extraUsed;
    extraUsed += (size+4095)&~(size_t)4095; // separate protection page per host allocation
    return p;
}
static int TestHook(void* target, void*, void** trampoline, const BYTE* expect, size_t n, const char*)
{
    ++hookCalls;
    assert(memcmp(target, expect, n) == 0);
    if (testMode == "hook_failure") return 0;
    // Record a patch in the PRIVATE data-only copy, never the installed game.
    memset(target, 0x90, n);
    *trampoline = target;
    return 1;
}
static void MapGameAsData(const char* file)
{
    FILE* f = NULL; assert(fopen_s(&f, file, "rb") == 0);
    fseek(f, 0, SEEK_END); long len = ftell(f); rewind(f);
    std::vector<BYTE> bytes((size_t)len);
    assert(fread(bytes.data(), 1, bytes.size(), f) == bytes.size()); fclose(f);
    auto dos = (IMAGE_DOS_HEADER*)bytes.data();
    auto nt = (IMAGE_NT_HEADERS64*)(bytes.data()+dos->e_lfanew);
    image = (BYTE*)VirtualAlloc(NULL, 0xA9D000+65536, MEM_RESERVE|MEM_COMMIT, PAGE_READWRITE);
    assert(image);
    memcpy(image, bytes.data(), nt->OptionalHeader.SizeOfHeaders);
    auto sec = IMAGE_FIRST_SECTION(nt);
    for (int i=0; i<nt->FileHeader.NumberOfSections; ++i)
        memcpy(image+sec[i].VirtualAddress, bytes.data()+sec[i].PointerToRawData, sec[i].SizeOfRawData);
}
static bool Near(float a, float b) { return fabsf(a-b) <= 0.0001f*(1+fabsf(b)); }
template<class T> static void Put(std::vector<BYTE>& b, size_t at, T v) { memcpy(b.data()+at, &v, sizeof(v)); }
static float TestMass(void*) { return 100.0f; }
static float TestPower(void*, char) { return 1000.0f; }
static float TestDivisor(void*) { return 123.0f; }
static float fuelLoad;
static float TestFuel(void*, float load, unsigned char) { fuelLoad=load; return 0.75f; }

#include "reliability_cases.h"

static void ModelTests()
{
    std::vector<BYTE> v(0x1800), type(0x9000);
    Put(v, V_TYPE, type.data()); Put(type, T_CATEGORY, 4);
    Put(type, T_POWER, 1000.0f); Put(type, T_WEIGHT, 50.0f);
    Put(v, V_SPEED, 36.0f); Put(v, V_SLOPE, 0.0f);
    g_TotalMass=TestMass; g_TotalPower=TestPower; g_OrigDivisor=TestDivisor; g_OrigFuel=TestFuel;
    Physics ph; ComputePhysics(v.data(), &ph);
    assert(ph.ok && Near(ph.mass_t, 100) && Near(ph.power_kW, 1500));
    assert(Near(ph.F_tr, 0.3f*50000*9.81f));
    assert(Near(ph.R, 100*(1.5f+0.006f*36)+0.4f*36*36));
    assert(Near(ph.a_ms2, (ph.F_tr-ph.R)/100000));
    Put(v, V_SLOPE, 0.5f); ComputePhysics(v.data(), &ph);
    assert(Near(ph.F_g, 100000*9.81f*0.5f*0.06f));
    Put(v, V_SLOPE, -0.5f); ComputePhysics(v.data(), &ph); assert(ph.F_g < 0);
    Put(v, V_SPEED, 0.0f); t_phVeh=NULL;
    assert(Near(RailFuel(v.data(), 0.8f, 0), 0.75f)); assert(Near(fuelLoad, 0.05f));
    Put(v, V_SPEED, 36.0f); t_phVeh=NULL; RailFuel(v.data(), 0.8f, 0);
    assert(fuelLoad > 0.05f && fuelLoad <= 1);
    Put(type, T_CATEGORY, 1); t_phVeh=NULL; assert(Near(RailDivisor(v.data()),123));
    g.curves=0; assert(Near(rp_brake_helper(v.data(), 100, 2, 10, 80), 3.456f));
    Put(v, V_EMERGENCY, (BYTE)1);
    assert(Near(rp_brake_helper(v.data(),100,2,10,80),5.616f));
    assert(SpanCount((void*)8,(void*)24,8)==2);
    assert(SpanCount((void*)24,(void*)8,8)==-1);
    assert(SpanCount((void*)8,(void*)25,8)==2);   // 1.3.4: remainder truncated like upstream
    assert(!ReadableFast(NULL,8) && !ReadableFast((void*)~(UINT_PTR)0,8));
    puts("PASS: native mass/power, traction, resistance, signed grade, fuel, braking, span guards");
}

static void RouteTests()
{
    std::vector<BYTE> v(0x1800), sg(0x200), a(0x40), b(0x40), points(99*0x18);
    Put(a, 4, 0.0f); Put(a, 12, 0.0f);
    Put(b, 4, 0.0f); Put(b, 12, 200.0f);
    Put(sg,S_NODE0,a.data()); Put(sg,S_NODE1,b.data()); Put(sg,S_LEN,200.0f);
    Put(sg,S_POLY,points.data()); Put(sg,S_POLY+8,points.data()+points.size());
    for (int i=0;i<99;++i) { Put(points,i*0x18+8,float((i+1)*2)); Put(points,i*0x18+0x14,float((i+1)*2)); }
    BYTE* route[]={sg.data()};
    Put(v,V_CUR_SEG,sg.data()); Put(v,V_ROUTE_SEGS,route); Put(v,V_ROUTE_SEGS+8,route+1);
    Put(v,V_SPEED,100.0f);
    g.curves=1; g.stations=0; g.customStop=0; g.smoothStop=0;
    CurveSlot slot={}; slot.corrClamp=slot.zeroClamp=slot.corrCand=slot.zeroCand=-1;
    CorrInfo ci; float radius;
    assert(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0)==FLT_MAX);
    void* stations[]={sg.data()}; g_statSegs=stations; g_statSegCount=1; g.stations=1;
    assert(Near(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0),60));
    g.stations=0; g.smoothStop=1;
    float stop[]={0,0,200}; g_statNodePos=stop; g_statNodeCount=1;
    float lim=ComputeCurveLimit(v.data(),&radius,&ci,&slot,0);
    assert(Near(lim, (float)(sqrt(2.0*(1.6/1.25)*175)*3.6)));
    assert(radius==-2.0f);
    g.smoothStop=0; slot.zeroClamp=-1; slot.zeroSpent=0;
    const float R=100.0f;
    for(int i=0;i<99;++i)
    { float angle=(i+1)*2/R; Put(points,i*0x18,R*(1-cosf(angle))); Put(points,i*0x18+8,R*sinf(angle)); }
    Put(b,4,R*(1-cosf(2))); Put(b,12,R*sinf(2));
    lim=ComputeCurveLimit(v.data(),&radius,&ci,&slot,0);
    assert(lim>0 && lim<100 && radius>=10 && radius<FLT_MAX);
    g_statSegs=NULL; g_statSegCount=0; g_statNodePos=NULL; g_statNodeCount=0;
    puts("PASS: straight route, station zone, smooth-stop parabola, curved route");
}

static void CorridorTests()
{
    // Force several hash-table growths while walking a 5.5 km track graph.
    // Capturing sourceComponent before growth is important: the old 'un'
    // pointer can belong to the table freed by CorrMapSlot's rehash.
    const int n=1100;
    std::vector<std::vector<BYTE>> nodes(n,std::vector<BYTE>(0x40));
    std::vector<std::vector<BYTE>> segs(n-1,std::vector<BYTE>(0x200));
    std::vector<std::vector<BYTE*>> adjacent(n);
    for(int i=0;i<n-1;++i)
    {
        Put(segs[i],S_NODE0,nodes[i].data()); Put(segs[i],S_NODE1,nodes[i+1].data());
        Put(segs[i],S_LEN,5.0f);
        adjacent[i].push_back(segs[i].data()); adjacent[i+1].push_back(segs[i].data());
    }
    for(int i=0;i<n;++i)
    {
        Put(nodes[i],N_SEGS,adjacent[i].data());
        Put(nodes[i],N_SEGS+8,adjacent[i].data()+adjacent[i].size());
        Put(nodes[i],4,i*5.0f);
    }
    void* customs[]={segs[0].data()}; g_custSegs=customs; g_custSegCount=1; g_custConnCount=0;
    BuildCustomsComponents(); BuildCorridors();
    assert(g_custCompCount==1 && g_corrSegCount==n-1);
    for(int i=1;i<n-1;++i)
    {
        float a,b; int ca,cb; CorridorDist(segs[i].data(),&a,&b,&ca,&cb);
        assert(ca==0 && cb==0 && Near(a,(i-1)*5.0f) && Near(b,i*5.0f));
    }
    g_custSegs=NULL; g_custSegCount=0;
    puts("PASS: 1100-node customs corridor, distances/component IDs stable through hash growth");
}

extern "C" void run_bridge(void*,void*,void*);
extern "C" void test_back(), test_branch(), mock_brake(), mock_slope(), mock_curve();
extern "C" { int test_branch_taken; }
static void* expectedVehicle;
extern "C" float test_brake_helper(void* v,float speed,float increment,float divisor,float limit)
{ assert(v==expectedVehicle && speed==100 && increment==2 && divisor==10 && limit==80); return 5; }
extern "C" float test_slope_helper(void* v,float increment,float slope,float divisor)
{ assert(v==expectedVehicle && increment==2 && slope==-0.5f && divisor==10); return -3; }
extern "C" float test_curve_helper(void* v,float limit)
{ assert(v==expectedVehicle && limit==80); return 60; }
struct Snapshot { float xmm[8]; UINT64 regs[7]; UINT64 flags; };
static void AbiTests()
{
    std::vector<BYTE> v(0x1800); expectedVehicle=v.data();
    BYTE* mem=(BYTE*)VirtualAlloc(NULL,sizeof(kRailStubCode),MEM_RESERVE|MEM_COMMIT,PAGE_READWRITE);
    assert(mem); memcpy(mem,kRailStubCode,sizeof(kRailStubCode));
#define SET_SLOT(n,p) do { void* q=(void*)p; memcpy(mem+k_##n,&q,8); } while(0)
    SET_SLOT(rp_brake_back,test_back); SET_SLOT(rp_slope_back,test_back); SET_SLOT(rp_slope_back2,test_back);
    SET_SLOT(rp_curve_back,test_back); SET_SLOT(rp_curve_branch,test_branch); SET_SLOT(rp_grid_back,test_back);
    SET_SLOT(rp_brake_helper_ptr,mock_brake); SET_SLOT(rp_slope_helper_ptr,mock_slope); SET_SLOT(rp_curve_helper_ptr,mock_curve);
#undef SET_SLOT
    float boost=2; memcpy(mem+k_rp_grid_k,&boost,4);
    DWORD old; assert(VirtualProtect(mem,sizeof(kRailStubCode),PAGE_EXECUTE_READ,&old));
    assert(FlushInstructionCache(GetCurrentProcess(),mem,sizeof(kRailStubCode)));
    const size_t entries[]={k_rp_brake_stub,k_rp_slope_stub,k_rp_slope_stub2,k_rp_curve_stub,k_rp_curve_stub,k_rp_grid_stub};
    const UINT64 sentinels[]={0x1111222233334444,0x2222333344445555,0x3333444455556666,
        0x4444555566667777,0x5555666677778888,0x6666777788889999,0x777788889999AAAA};
    for(int i=0;i<6;++i)
    {
        Snapshot s={}; Put(v,V_SPEED,123.0f); Put(v,0xD41,(BYTE)(i==4?0:1));
        Put(v,0x970,(UINT64)0xABCDEF); Put(v,0x8C,3.0f); test_branch_taken=0;
        run_bridge(mem+entries[i],v.data(),&s);
        for(int j=0;j<7;++j) assert(s.regs[j]==((i==5&&j==0)?0xABCDEF:sentinels[j]));
        for(int j=3;j<8;++j) assert(s.xmm[j]==float(j+8));
        if(i==0) assert(s.xmm[1]==95 && *(float*)(v.data()+V_SPEED)==95);
        if(i==1||i==2) assert(s.xmm[0]==120 && (s.flags&1));
        if(i==3||i==4) assert(s.xmm[0]==2 && s.xmm[2]==60 && test_branch_taken==(i==4));
        if(i==3) assert(s.xmm[1]==123);
        if(i==4) assert(s.xmm[1]==100);
        if(i==5) assert(s.xmm[0]==12 && (s.flags&1));
    }
    VirtualFree(mem,0,MEM_RELEASE);
    puts("PASS: all 5 x64 bridges, fifth stack argument, shadow-space clobber, registers, flags, curve branch, grid SIB");
}

int main(int argc,char** argv)
{
    assert(argc==4); testMode=argv[1]; iniPath=argv[3];
    TsmHost host={}; host.apiVersion=4; host.structSize=sizeof(host);
    host.log=TestLog; host.configInt=TestInt; host.configString=TestString;
    host.readablePtr=TestReadable; host.installInlineHook=TestHook; host.allocNear=TestAlloc;
    char testBase[MAX_PATH]; GetFullPathNameA("build", MAX_PATH, testBase, NULL);
    host.baseDir=testBase;
    MapGameAsData(argv[2]); host.exeBase=image; host.exeSize=0xA9D000;
    TsmPluginInfo info={};
    if(testMode=="bad_host") { host.structSize=8; assert(TsmPluginInit(&host,&info)!=0); puts("PASS: short host rejected"); return 0; }
    if(testMode=="disabled") { assert(TsmPluginInit(&host,&info)!=0); assert(!hookCalls&&!allocations); puts("PASS: disabled, no writes"); return 0; }
    if(testMode=="beside_dll")
    {
        // No plugins\rail_physics_fix.ini under this base directory: the plugin must read the
        // INI beside its own module (build\rail_physics_fix.ini here) without the host readers,
        // and arrive at the same supplied values.
        char other[MAX_PATH]; GetFullPathNameA("build\\beside_dll", MAX_PATH, other, NULL); CreateDirectoryA(other, NULL);
        host.baseDir=other; configCalls=0;
        assert(TsmPluginInit(&host,&info)==0 && configCalls==0 && !hookCalls && !allocations);
        assert(Near(g.powerScale,1.5f) && Near(g.serviceBrake,1.6f) && Near(g.stationLimit,60) && Near(g.curveLat,1.4f) && g.customStop==1);
        puts("PASS: INI beside the DLL used when plugins\\rail_physics_fix.ini is missing"); return 0;
    }
    assert(TsmPluginApiVersion()==4 && TsmPluginInit(&host,&info)==0);
    assert(configCalls>0);
    assert(!hookCalls&&!allocations && !strcmp(info.name,"rail_physics_fix"));
    assert(info.version && !strcmp(info.version,"1.3.4-beta"));
    assert(Near(g.powerScale,1.5f) && Near(g.serviceBrake,1.6f) && Near(g.stationLimit,60));
    if(testMode=="bad_numeric") { assert(Near(g.curveLat,0.9f)); puts("PASS: non-finite INI value rejected"); return 0; }
    if(testMode=="model") { ModelTests(); RouteTests(); CorridorTests(); AbiTests(); return 0; }
    if(testMode=="allocations") { AllocationTests(); return 0; }
    if(testMode=="guards") { GuardTests(); return 0; }
    if(testMode=="patch_failures") { PatchFailureTests(); return 0; }
    if(testMode=="warnings") { WarningTests(); return 0; }
    if(testMode=="dll")
    {
        HMODULE module=LoadLibraryA("build\\rail_physics_fix.dll"); assert(module);
        auto version=(TsmPluginApiVersionFn)GetProcAddress(module,TSM_EXPORT_APIVERSION);
        auto init=(TsmPluginInitFn)GetProcAddress(module,TSM_EXPORT_INIT);
        auto start=(TsmPluginStartFn)GetProcAddress(module,TSM_EXPORT_START);
        assert(version && init && start && version()==4);
        assert(init(&host,&info)==0 && hookCalls==0 && allocations==0);
        assert(info.version && !strcmp(info.version,"1.3.4-beta"));
        assert(start()==0 && hookCalls==5 && allocations==3);
        assert(start()==0 && hookCalls==5 && allocations==3);
        puts("PASS: built DLL exports, version 1.3.4-beta and actual cross-module API-4 Init/Start in offline host");
        return 0; // do not unload a module after it has published hooks
    }
    if(testMode=="duplicate")
    {
        CreateDirectoryA("build\\legacy_test",NULL);
        assert(CopyFileA("build\\rail_physics_fix.dll","build\\legacy_test\\railphysics.dll",FALSE));
        // Load our OWN newly-built DLL under the legacy name; never run the supplied DLL.
        assert(LoadLibraryA("build\\legacy_test\\railphysics.dll"));
        assert(TsmPluginStart()!=0 && !hookCalls && !allocations);
        puts("PASS: legacy-name duplicate rejected before publishing any hook"); return 0;
    }
    if(testMode=="wrong_build")
        ((IMAGE_NT_HEADERS64*)(image+((IMAGE_DOS_HEADER*)image)->e_lfanew))->FileHeader.TimeDateStamp=0;
    if(testMode=="changed_replay") image[0x6A862D]=0x90;
    if(testMode=="missing_signature") image[0x6A8553]=0x90;
    if(testMode=="ambiguous_signature") memcpy(image+0x1000,image+0x6A8553,16);
    std::vector<BYTE> before(image,image+0xA9D000);
    int result=TsmPluginStart();
    if(testMode=="wrong_build"||testMode=="changed_replay"||testMode=="missing_signature"||testMode=="ambiguous_signature")
    { assert(result!=0&&!hookCalls&&!allocations); assert(TsmPluginStart()!=0&&!hookCalls&&!allocations);
      assert(!memcmp(before.data(),image,before.size())); puts("PASS: unsupported/conflicting image rejected without writes"); }
    else
    { assert(result==0&&hookCalls==5&&allocations==3); assert(TsmPluginStart()==0&&hookCalls==5&&allocations==3);
      if(testMode=="hook_failure") assert(image[0x6A8655]==0x76);
      else assert(image[0x6A8655]==0xEB);
      MEMORY_BASIC_INFORMATION mbi; assert(VirtualQuery(g_stubMemory,&mbi,sizeof(mbi))&&mbi.Protect==PAGE_EXECUTE_READ);
      puts("PASS: Init/Start, call rewrites, inline hooks, RX bridges, repeated Start; active patches never unloaded"); }
    return 0;
}
