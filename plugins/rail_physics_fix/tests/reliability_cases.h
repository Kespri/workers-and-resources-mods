// Fault-path fixtures. Only included in the offline test executable.
struct GuardPage
{
    BYTE* memory;
    GuardPage()
    {
        memory=(BYTE*)VirtualAlloc(NULL,8192,MEM_RESERVE|MEM_COMMIT,PAGE_READWRITE);
        assert(memory); DWORD old;
        assert(VirtualProtect(memory+4096,4096,PAGE_NOACCESS,&old));
    }
    ~GuardPage() { VirtualFree(memory,0,MEM_RELEASE); }
};
struct ZoneFixture
{
    std::vector<BYTE> station, customs, stType, cuType, table1, table2, segment, nodeA, nodeB;
    BYTE* chains[2]; BYTE** vectorPair[2]; BYTE* attached[1];
    ZoneFixture():station(0xA20),customs(0xA20),stType(0x370),cuType(0x370),
        table1(0x60),table2(0x60),segment(0x200),nodeA(0x40),nodeB(0x40)
    {
        Put(stType,0x360,0); Put(cuType,0x360,CHAIN_TYPE_CUSTOMHOUSE);
        Put(station,CHAIN_TYPE_DESC,stType.data()); Put(customs,CHAIN_TYPE_DESC,cuType.data());
        Put(station,CHAIN_TABLE,table1.data()); Put(station,CHAIN_TABLE+8,table1.data()+0x60);
        Put(customs,CHAIN_TABLE,table2.data()); Put(customs,CHAIN_TABLE+8,table2.data()+0x60);
        Put(table1,0x20,segment.data()); Put(table2,0x20,segment.data()); Put(table2,0x30,nodeA.data());
        Put(segment,S_NODE0,nodeA.data()); Put(segment,S_NODE1,nodeB.data()); Put(segment,S_LEN,100.0f);
        Put(nodeB,4,100.0f); attached[0]=segment.data();
        Put(nodeA,N_SEGS,attached); Put(nodeA,N_SEGS+8,attached+1);
        Put(nodeB,N_SEGS,attached); Put(nodeB,N_SEGS+8,attached+1);
        chains[0]=station.data(); chains[1]=customs.data();
        vectorPair[0]=chains; vectorPair[1]=chains+2; g_chainVec=vectorPair;
        g.smoothStop=g.customStop=g.stations=1; g.logCurves=0;
    }
    void Scan() { g_statScanTick=GetTickCount()-30000; RescanStations(); }
};
static void ClearTestZones() { ZoneData d=DetachZones(); FreeZones(d); }
static void AllocationTests()
{
    ZoneFixture world;
    world.Scan();
    assert(g_statSegCount==1 && g_statNodeCount==2 && g_custCompCount==1 && g_corrSegCount==1);
    int steps=g_testAllocCalls,kept=0,partial=0;
    assert(steps>=15);
    for(int fail=1;fail<=steps;++fail)
    {
        ClearTestZones(); assert(g_testLiveAllocations==0);
        g_testAllocFailAt=-1; world.Scan(); // populated previous tables
        assert(g_corrSegCount==1);
        int liveBefore=g_testLiveAllocations;
        g_testAllocCalls=0; g_testAllocFailAt=fail;
        world.Scan();
        // 1.3.4: a failed rebuild keeps the previous complete tables; a stop inside
        // the corridor build publishes the partial corridors. Never a zone-less world,
        // never an inconsistent table, never a leaked incomplete table.
        assert((g_statSegs!=NULL)==(g_statSegCount>0) && (g_corrSegs!=NULL)==(g_corrSegCount>0));
        assert((g_custComps!=NULL)==(g_custCompCount>0) && (g_custSeeds!=NULL)==(g_custSeedCount>0));
        assert(g_statSegCount==1 && g_custCompCount==1 && g_corrSegCount<=1);
        if(g_corrSegCount==1) ++kept; else ++partial;
        assert(g_testLiveAllocations<=liveBefore);
        g_testAllocFailAt=-1; world.Scan(); assert(g_corrSegCount==1 && g_statSegCount==1); // recovery
        ClearTestZones(); assert(g_testLiveAllocations==0);
    }
    assert(kept>0);
    ClearTestZones(); assert(g_testLiveAllocations==0); g_chainVec=NULL;
    void** set=NULL; int count=0,cap=0;
    assert(SegSetAdd(&set,&count,&cap,(void*)8));
    void** previous=set; int previousCap=cap; count=cap;
    g_testAllocFailAt=g_testAllocCalls+1;
    assert(!SegSetAdd(&set,&count,&cap,(void*)16));
    assert(set==previous && cap==previousCap && count==previousCap);
    g_testAllocFailAt=-1; RpFree(set); assert(g_testLiveAllocations==0);
    // Empty valid worlds replace old tables with a complete empty snapshot.
    world.vectorPair[1]=world.chains; g_chainVec=world.vectorPair; world.Scan();
    assert(!g_statSegCount && !g_custCompCount);
    printf("PASS: all %d required zone-allocation sites fail cleanly (%d kept the previous tables, %d published partial corridors), no leak, retry succeeds; realloc preserves owner\n",steps,kept,partial);
}
static int nativeCalls;
static float CountMass(void*) { ++nativeCalls; return 100; }
static float CountPower(void*,char) { ++nativeCalls; return 1000; }
static float CountDivisor(void*) { ++nativeCalls; return 123; }
static float CountFuel(void*,float,unsigned char) { ++nativeCalls; return 0.75f; }
static float NanFuel(void*,float,unsigned char) { ++nativeCalls; return NAN; }
static void GuardTests()
{
    std::vector<BYTE> v(0x1800),type(0x9000);
    Put(v,V_TYPE,type.data()); Put(type,T_CATEGORY,4);
    Put(type,T_POWER,1000.0f); Put(type,T_WEIGHT,50.0f); Put(v,V_SPEED,50.0f);
    g_TotalMass=CountMass; g_TotalPower=CountPower; g_OrigDivisor=CountDivisor; g_OrigFuel=CountFuel;
    GuardPage edge; Physics ph;
    ComputePhysics(edge.memory+4096-16,&ph); assert(!ph.ok&&!ph.nativeSafe&&!nativeCalls);
    BYTE** badSpan=(BYTE**)(edge.memory+4096-8); *badSpan=v.data();
    Put(v,V_WAGON_VEC,badSpan); Put(v,V_WAGON_VEC+8,badSpan+2);
    ComputePhysics(v.data(),&ph); assert(!ph.ok&&!ph.nativeSafe&&!nativeCalls);
    t_phVeh=NULL; assert(RailDivisor(v.data())==1e9f && !nativeCalls);
    t_phVeh=NULL; assert(RailFuel(v.data(),1,0)==0 && !nativeCalls);
    Put(v,V_WAGON_VEC,(BYTE**)NULL); Put(v,V_WAGON_VEC+8,(BYTE**)NULL);
    ComputePhysics(v.data(),&ph); assert(ph.ok&&ph.nativeSafe);
    // 1.3.4: an inactive wagon is skipped before its type is read (upstream behaviour);
    // an active wagon with the same dead type pointer still keeps the native calls away.
    std::vector<BYTE> wagon(0x1800); BYTE* wagons[]={wagon.data()};
    Put(wagon,V_INACTIVE,1); Put(wagon,V_TYPE,(BYTE*)NULL);
    Put(v,V_WAGON_VEC,wagons); Put(v,V_WAGON_VEC+8,wagons+1);
    ComputePhysics(v.data(),&ph); assert(ph.ok&&ph.nativeSafe);
    Put(wagon,V_INACTIVE,0); nativeCalls=0;
    ComputePhysics(v.data(),&ph); assert(!ph.ok&&!ph.nativeSafe&&!nativeCalls);
    Put(v,V_WAGON_VEC,(BYTE**)NULL); Put(v,V_WAGON_VEC+8,(BYTE**)NULL);
    Put(v,V_SLOPE,NAN); ComputePhysics(v.data(),&ph); assert(!ph.ok);
    Put(v,V_SLOPE,0.0f); Put(type,T_WEIGHT,FLT_MAX);
    ComputePhysics(v.data(),&ph); assert(!ph.ok);
    Put(type,T_WEIGHT,50.0f); Put(v,V_SLOPE,-0.5f); ComputePhysics(v.data(),&ph);
    assert(ph.ok&&ph.F_g<0); // negative grades are explicitly valid
    g_OrigFuel=NanFuel; nativeCalls=0; t_phVeh=NULL;
    assert(RailFuel(v.data(),1,0)==0 && nativeCalls==3); // mass + power + ONE fuel call
    assert(isfinite(rp_brake_helper(v.data(),NAN,2,10,80)));
    assert(isfinite(rp_slope_helper(v.data(),NAN,1,10)));
    // Previously N_SEGS+8 admitted the begin pointer but not the end pointer.
    void* seeds[]={edge.memory+4096-(N_SEGS+8)}; int comps[]={0};
    g_custSeeds=seeds; g_custSeedComp=comps; g_custSeedCount=1; g_custConnCount=0;
    assert(BuildCorridors() && g_corrSegCount==0);
    g_custSeeds=NULL; g_custSeedComp=NULL; g_custSeedCount=0;
    // Route buffer allocation fails before any partial speed limit is published.
    std::vector<BYTE> sg(0x200),a(0x40),b(0x40);
    Put(sg,S_NODE0,a.data()); Put(sg,S_NODE1,b.data()); Put(sg,S_LEN,100.0f);
    Put(b,12,100.0f); BYTE* route[]={sg.data()};
    Put(v,V_CUR_SEG,sg.data()); Put(v,V_ROUTE_SEGS,route); Put(v,V_ROUTE_SEGS+8,route+1);
    g.smoothStop=g.customStop=g.stations=0;
    g_testAllocFailAt=g_testAllocCalls+1; CorrInfo ci; CurveSlot slot={}; float radius=0;
    assert(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0)==FLT_MAX && g_curveScanFailed);
    assert(!g_pts && g_ptsCap==0);
    g_testAllocFailAt=-1;
    assert(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0)==FLT_MAX && !g_curveScanFailed);
    RpFree(g_pts); g_pts=NULL; g_ptsCap=0;
    // 1.3.4: a torn second leg (guard page) ends the walk with the first leg's points
    // instead of failing the scan; the native limit passes through unchanged when NaN.
    BYTE* tornRoute[]={sg.data(),edge.memory+4096};
    Put(v,V_ROUTE_SEGS,tornRoute); Put(v,V_ROUTE_SEGS+8,tornRoute+2);
    assert(ComputeCurveLimit(v.data(),&radius,&ci,&slot,0)==FLT_MAX && !g_curveScanFailed && g_pts);
    Put(v,V_ROUTE_SEGS,route); Put(v,V_ROUTE_SEGS+8,route+1);
    RpFree(g_pts); g_pts=NULL; g_ptsCap=0;
    g.curves=1; assert(isnan(rp_curve_helper(v.data(),NAN))); g.curves=0;
    // A failed rescan keeps an existing limit/clamp on the same route.
    InitializeCriticalSection(&g_lock);
    CurveSlot* cached=&g_curveCache[CurveHash(v.data())&(CURVE_CACHE_N-1)];
    memset(cached,0,sizeof(*cached)); cached->veh=v.data(); cached->limit=60;
    cached->routeVec=route; cached->routeN=1; cached->curSeg=sg.data();
    cached->tick=GetTickCount()-11000; cached->corrClamp=150; cached->zeroClamp=-1;
    CurveSlot oldCache=*cached; g_testAllocFailAt=g_testAllocCalls+1;
    assert(CurveLimitFor(v.data())==60 && !memcmp(cached,&oldCache,sizeof(oldCache)));
    g_testAllocFailAt=-1; assert(isfinite(CurveLimitFor(v.data())));
    DeleteCriticalSection(&g_lock); RpFree(g_pts); g_pts=NULL; g_ptsCap=0;
    assert(g_testLiveAllocations==0);
    puts("PASS: guard-page vehicle/vector/node bounds; numeric fallbacks; single fuel call; inactive wagon skipped; torn route leg keeps partial lookahead; NaN limit passes through; route-allocation recovery");
}
static void PatchFailureTests()
{
    BYTE* site=image+0x6A7970; BYTE* orig=site+5+*(INT32*)(site+1);
    BYTE original[5]; memcpy(original,site,5); g_exeBase=image;
    const char* protectionFailures[]={"call thunk RX","call site write"};
    for(const char* action:protectionFailures)
    {
        g_testProtectFailAction=action;
        assert(!PatchCallSite(site,orig,(void*)TestDivisor,"fault"));
        assert(!memcmp(original,site,5));
    }
    g_testProtectFailAction=NULL; g_testFlushFailAction="call thunk publish";
    assert(!PatchCallSite(site,orig,(void*)TestDivisor,"fault")&&!memcmp(original,site,5));
    g_testFlushFailAction=NULL; g_testProtectFailAction="call site restore";
    assert(PatchCallSite(site,orig,(void*)TestDivisor,"published fault")&&g_patchMaintenanceFailed);
    assert(memcmp(original,site,5));
    memcpy(site,original,5); g_patchMaintenanceFailed=false;
    g_testProtectFailAction=NULL; g_testFlushFailAction="call site publish";
    assert(PatchCallSite(site,orig,(void*)TestDivisor,"published fault")&&g_patchMaintenanceFailed);
    g_testFlushFailAction=NULL;
    g_siteSlopeBranch=image+0x6A8655; BYTE before=g_siteSlopeBranch[0];
    g_testProtectFailAction="slope branch write";
    assert(!PatchSlopeBranch()&&g_siteSlopeBranch[0]==before);
    g_testProtectFailAction=NULL; g_testFlushFailAction="slope branch publish";
    g_patchMaintenanceFailed=false;
    assert(PatchSlopeBranch()&&g_patchMaintenanceFailed&&g_siteSlopeBranch[0]==0xEB);
    g_testFlushFailAction=NULL;
    g_testProtectFailAction="bridges RX"; assert(!PrepareStubs()&&!g_stubMemory);
    g_testProtectFailAction=NULL; g_testFlushFailAction="bridges publish";
    assert(!PrepareStubs()&&!g_stubMemory);
    g_testFlushFailAction=NULL; assert(PrepareStubs());
    MEMORY_BASIC_INFORMATION mbi;
    assert(VirtualQuery(g_stubMemory,&mbi,sizeof(mbi))&&mbi.Protect==PAGE_EXECUTE_READ);
    puts("PASS: protect/flush failures before publication leave site untouched; after publication hook stays counted/alive; RX bridges recover");
}
static void WarningTests()
{
    memset(g_rpWarnings,0,sizeof(g_rpWarnings)); warningMessages=rawMessages=0;
    for(int i=0;i<1000;++i) RpWarn(RP_NUMERIC,"repeat test");
    assert(warningMessages==1 && g_rpWarnings[RP_NUMERIC].suppressed==999);
    g_rpWarnings[RP_NUMERIC].last=GetTickCount()-30000;
    RpWarn(RP_NUMERIC,"repeat test"); assert(warningMessages==2);
    ZoneFixture world; world.Scan(); assert(rawMessages==0);
    g.logCurves=1; world.Scan(); assert(rawMessages>0);
    ClearTestZones(); g_chainVec=NULL;
    assert(g_rpLog!=INVALID_HANDLE_VALUE);
    puts("PASS: warnings limited per rule, warning recovery, debug-off customs silence, debug-on diagnostics, separate plugin log");
}
