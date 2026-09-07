"""Compare the port with the supplied RailPhysics source and INI, read-only."""
import argparse
import configparser
import re
from pathlib import Path


def function(text, name):
    # Mask comments and literals (including braces in diagnostic strings).
    clean = re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"', lambda m: ' '*len(m[0]), text, flags=re.S)
    matches = list(re.finditer(r'\b(?:float|int|void)\s+'+name+r'\([^;{}]*\)\s*\{', clean))
    assert len(matches) == 1, (name, len(matches))
    start = matches[0].start()
    b = clean.index('{', start)
    level = 1
    end = b+1
    while level:
        if clean[end] == '{': level+=1
        elif clean[end] == '}': level-=1
        end+=1
    return re.sub(r'\s+', '', text[start:end])


def main():
    p=argparse.ArgumentParser()
    p.add_argument('original_source',type=Path)
    p.add_argument('original_ini',type=Path)
    a=p.parse_args()
    root=Path(__file__).resolve().parents[1]
    original=a.original_source.read_text(encoding='utf-8-sig')
    adapted=(root/'rail_physics_fix.cpp').read_text(encoding='utf-8-sig')
    # Explicitly account for the validated 64-bit span-to-count port changes;
    # every other character of these functions (including constants) must agree.
    original=original.replace('long   n    = se - segs;', 'long   n    = SpanCount(segs, se, 8);')
    original=original.replace('long  np = (pe - pb) / 0x18;', 'long  np = SpanCount(pb, pe, 0x18);')
    original=original.replace('long  routeN   = *(BYTE***)(v + V_ROUTE_SEGS + 8) - (BYTE**)routeVec;',
                              'long  routeN   = SpanCount(routeVec, *(void**)(v + V_ROUTE_SEGS + 8), 8);')
    original=original.replace('long  on  = oe - ob;', 'long  on  = SpanCount(ob, oe, 8);')
    # Reliability guards intentionally change the other functions in 1.3.2.
    # Their valid-input behavior is compared by tests/compare_reference.bat;
    # do not claim textual parity for them or strip away arbitrary guards.
    names=['ComputePhysicsCached','CorridorDist','SegmentIsStation','SegmentIsCustoms']
    for name in names:
        assert function(original,name)==function(adapted,name), f'Unexpected model change: {name}'
    old=configparser.ConfigParser(interpolation=None)
    new=configparser.ConfigParser(interpolation=None)
    old.read(a.original_ini,encoding='utf-8-sig')
    new.read(root/'rail_physics_fix.ini',encoding='utf-8-sig')
    assert dict(old['railphysics'])==dict(new['railphysics']), 'INI values changed'
    print(f'PASS: {len(names)} unchanged cache/table-query function bodies preserved')
    print('Model/route runtime parity requires tests/compare_reference.bat (separate test).')
    print(f'PASS: all {len(old["railphysics"])} supplied INI entries preserved')


if __name__=='__main__': main()
