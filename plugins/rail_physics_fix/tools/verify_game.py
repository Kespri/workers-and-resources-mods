"""Read-only verification of the exact 1.1.1.9 PE and all RailPhysics signatures."""
import argparse
import hashlib
import re
import struct
from pathlib import Path


class PE:
    def __init__(self, path):
        self.data = Path(path).read_bytes()
        d = self.data
        assert d[:2] == b'MZ', 'Not a PE file'
        pe = struct.unpack_from('<I', d, 0x3c)[0]
        assert d[pe:pe+4] == b'PE\0\0'
        machine, count, self.timestamp = struct.unpack_from('<HHI', d, pe+4)
        assert machine == 0x8664, 'Not x64'
        optional_size = struct.unpack_from('<H', d, pe+20)[0]
        assert struct.unpack_from('<H', d, pe+24)[0] == 0x20b
        self.image_size = struct.unpack_from('<I', d, pe+24+56)[0]
        self.sections = {}
        for i in range(count):
            off = pe + 24 + optional_size + 40*i
            name, vs, va, rs, raw = struct.unpack_from('<8sIIII', d, off)
            self.sections[name.rstrip(b'\0').decode()] = (va, vs, raw, rs)

    def at(self, rva, size):
        for va, vs, raw, rs in self.sections.values():
            if va <= rva and rva + size <= va + rs:
                return self.data[raw+rva-va:raw+rva-va+size]
        raise AssertionError(f'Unmapped RVA {rva:x}+{size:x}')


def verify(exe, source):
    pe = PE(exe)
    assert pe.timestamp == 0x6A3EB6AD, 'Wrong executable timestamp'
    assert pe.image_size == 0xA9D000, 'Wrong executable image size'
    digest = hashlib.sha256(pe.data).hexdigest().upper()
    assert digest == '296644A9F207D609031FC2AE73FED2DCB34619A1D55A35D1C7B51965CE6841B8', 'Unknown executable SHA256'
    va, vs, raw, rs = pe.sections['.text']
    code = pe.data[raw:raw+min(vs, rs)]
    source_text = Path(source).read_text(encoding='utf-8-sig')
    signatures = re.findall(r'static const short (SIG_\w+)\[\]\s*=\s*\{(.*?)\};', source_text, re.S)
    assert len(signatures) == 12, f'Expected 12 signatures, found {len(signatures)}'
    hits = {}
    for name, body in signatures:
        body = re.sub(r'//[^\n]*', '', body)
        values = [x.strip() for x in body.split(',') if x.strip()]
        pattern = b''.join(b'.' if v == 'W' else re.escape(bytes([int(v, 0)])) for v in values)
        matches = [va+m.start() for m in re.finditer(b'(?='+pattern+b')', code, re.S)]
        if name == 'SIG_CHAIN_VEC':
            targets = {m+7+struct.unpack('<i', pe.at(m+3, 4))[0] for m in matches}
            assert targets == {0x9E6A18}, f'{name}: inconsistent targets {targets}'
            print(f'{name}: {len(matches)} references -> 0x9E6A18')
        else:
            assert len(matches) == 1, f'{name}: {len(matches)} matches'
            hits[name] = matches[0]
            print(f'{name}: unique +0x{matches[0]:X}')
    expected = {'SIG_CURVE': (7, 0x6A8436, 17), 'SIG_BRAKE': (0, 0x6A8553, 16),
                'SIG_SLOPE_A': (0x29, 0x6A862D, 21), 'SIG_SLOPE_B': (0x25, 0x6A8676, 21),
                'SIG_GRID': (0, 0x1BDF8C, 16)}
    for name, (delta, site, count) in expected.items():
        assert hits[name]+delta == site
        print(f'{name} stolen: {pe.at(site, count).hex(" ")}')
    for name, delta, target in [('SIG_CALL_DIV', 3, 0x698C50),
                                ('SIG_CALL_FUEL1', 9, 0x6B3B50),
                                ('SIG_CALL_FUEL2', 11, 0x6B3B50)]:
        site = hits[name]+delta
        assert pe.at(site, 1) == b'\xe8'
        assert site+5+struct.unpack('<i', pe.at(site+1, 4))[0] == target
    assert hits['SIG_FN_DIVISOR'] == 0x698C50
    assert hits['SIG_FN_POWER'] == 0x698CF0
    assert hits['SIG_FN_MASS'] == 0x698F80
    for site in [0x6A8655, 0x6A843D]:
        b = pe.at(site, 2)
        print(f'Branch +0x{site:X}: {b.hex(" ")} -> +0x{site+2+struct.unpack("b", b[1:])[0]:X}')
    print(f'PASS: WRSR 1.1.1.9, SHA256 {digest}; no game files changed')


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('exe')
    p.add_argument('--source', type=Path, default=Path(__file__).resolve().parents[1]/)
    a = p.parse_args()
    verify(a.exe, a.source)
