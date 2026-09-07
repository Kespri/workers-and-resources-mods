// Shared deterministic valid-input corpus for reference/current source.
// No calls into the game; compare output bit patterns, not printed decimals.
#include <vector>
#include <cassert>
static float corpusMass,corpusPower,corpusLoad;
static unsigned long long digest=14695981039346656037ULL;
static unsigned values;
static void Value(float x)
{
    assert(isfinite(x)); unsigned bits; memcpy(&bits,&x,4);
    for(int i=0;i<4;++i) { digest^=(bits>>(i*8))&255; digest*=1099511628211ULL; }
    ++values;
}
static int CorpusReadable(const void* p,size_t n)
{
    if(!p || n>~(UINT_PTR)0-(UINT_PTR)p) return 0;
    UINT_PTR at=(UINT_PTR)p,end=at+n;
    while(at<end)
    {
        MEMORY_BASIC_INFORMATION mbi;
        if(!VirtualQuery((void*)at,&mbi,sizeof(mbi)) || mbi.State!=MEM_COMMIT ||
           (mbi.Protect&(PAGE_NOACCESS|PAGE_GUARD))) return 0;
        at=(UINT_PTR)mbi.BaseAddress+mbi.RegionSize;
    }
    return 1;
}
static float CorpusMass(void*) { return corpusMass; }
static float CorpusPower(void*,char) { return corpusPower; }
static float CorpusFuel(void*,float load,unsigned char) { corpusLoad=load; return 0.75f; }
static float CorpusDivisor(void*) { return 123; }
template<class T> static void CorpusPut(std::vector<BYTE>& b,size_t p,T v) { memcpy(b.data()+p,&v,sizeof(v)); }
int main()
{
    TsmHost host={}; host.readablePtr=CorpusReadable; TsmBind(&host);
    g_TotalMass=CorpusMass; g_TotalPower=CorpusPower; g_OrigFuel=CorpusFuel; g_OrigDivisor=CorpusDivisor;
    g.mu=.30f; g.powerScale=1.5f; g.davisA=1.5f; g.davisB=.006f; g.davisC=.40f;
    g.gradeScale=.06f; g.serviceBrake=1.6f; g.emergencyBrake=2.6f; g.brakeFloorRatio=1;
    g.idleLoad=.05f; g.loadMax=1; g.electricLoadScale=.65f;
    g.curveLat=.9f; g.curveMargin=1.25f; g.curveLookahead=1200; g.stationLimit=60; g.customsEntry=50;
    std::vector<BYTE> v(0x1800),type(0x9000),wagon(0x1800),wt(0x9000);
    BYTE* wagons[]={wagon.data()};
    CorpusPut(v,V_TYPE,type.data()); CorpusPut(type,T_CATEGORY,4);
    CorpusPut(v,V_WAGON_VEC,wagons); CorpusPut(v,V_WAGON_VEC+8,wagons+1);
    CorpusPut(wagon,V_TYPE,wt.data()); CorpusPut(wt,T_CATEGORY,4);
    for(int i=0;i<4096;++i)
    {
        corpusMass=20.0f+(i%97)*15; corpusPower=(float)(i%107)*70;
        float speed=(float)(i%331), slope=((i%41)-20)*.025f;
        CorpusPut(v,V_SPEED,speed); CorpusPut(v,V_SLOPE,slope);
        CorpusPut(type,T_POWER,corpusPower); CorpusPut(type,T_WEIGHT,20.0f+(i%13)*7);
        CorpusPut(type,0x8680,(BYTE)(i&1)); CorpusPut(v,V_EMERGENCY,(BYTE)((i>>2)&1));
        CorpusPut(wagon,V_SLOPE,-slope*.5f); CorpusPut(wagon,V_INACTIVE,(i>>1)&1);
        CorpusPut(wt,T_POWER,corpusPower*.4f); CorpusPut(wt,T_WEIGHT,15.0f+(i%11)*3);
        Physics ph; ComputePhysics(v.data(),&ph); assert(ph.ok);
        Value(ph.v_kmh); Value(ph.mass_t); Value(ph.power_kW); Value(ph.slopeAvg);
        Value(ph.F_tr); Value(ph.R); Value(ph.F_g); Value(ph.a_ms2); Value(ph.accel_kmh_s);
        t_phVeh=NULL; Value(RailDivisor(v.data()));
        t_phVeh=NULL; Value(RailFuel(v.data(),.8f,0)); Value(corpusLoad);
        Value(rp_brake_helper(v.data(),speed,.02f,10,80));
        // Unique identities avoid the deliberate same-vehicle/tick dedup.
        Value(rp_slope_helper((void*)(UINT_PTR)(i+1),.02f,slope,10));
    }
    std::vector<BYTE> sg(0x200),a(0x40),b(0x40),points(99*0x18);
    BYTE* route[]={sg.data()}; void* stations[]={sg.data()};
    CorpusPut(sg,S_NODE0,a.data()); CorpusPut(sg,S_NODE1,b.data()); CorpusPut(sg,S_LEN,200.0f);
    CorpusPut(sg,S_POLY,points.data()); CorpusPut(sg,S_POLY+8,points.data()+points.size());
    CorpusPut(v,V_CUR_SEG,sg.data()); CorpusPut(v,V_ROUTE_SEGS,route); CorpusPut(v,V_ROUTE_SEGS+8,route+1);
    float stop[3]={}; g_statSegs=stations; g_statSegCount=1; g_statNodePos=stop; g_statNodeCount=1;
    for(int i=0;i<1024;++i)
    {
        float R=50.0f+(i%100)*10;
        for(int j=0;j<99;++j)
        {
            float angle=(j+1)*2/R;
            CorpusPut(points,j*0x18,R*(1-cosf(angle))); CorpusPut(points,j*0x18+8,R*sinf(angle));
            CorpusPut(points,j*0x18+0x14,(float)((j+1)*2));
        }
        stop[0]=R*(1-cosf(200/R)); stop[2]=R*sinf(200/R);
        CorpusPut(b,4,stop[0]); CorpusPut(b,12,stop[2]);
        CorpusPut(v,V_SPEED,(float)(i%331)); CorpusPut(v,V_POS,(float)(i%190)); CorpusPut(v,V_DIR,(BYTE)(i&1));
        g.stations=(i>>1)&1; g.smoothStop=(i>>2)&1; g.customStop=(i>>3)&1;
        g_custSegs=stations; g_custSegCount=g.customStop;
        CurveSlot slot={}; slot.corrClamp=slot.corrCand=slot.zeroClamp=slot.zeroCand=-1;
        CorrInfo ci; float radius=FLT_MAX;
        Value(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0)); Value(radius);
        Value((float)ci.dist); Value((float)ci.zeroDist); Value((float)ci.statDist);
    }
    printf("cases=5120 values=%u hash=%016llX\n",values,digest);
    return 0;
}
