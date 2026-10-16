"""VARCO Animate GLB → Unity 용 애니메이션 JSON (+ 텍스처 PNG).
사용: python glb2anim.py <in.glb> <out.json> [texture_out.png]
좌표 변환 (VARCO FBX 를 Unity 가 불러온 본 기준, Root 아래):
  회전 (x,y,z,w) → (x, z, -y, w),  위치 (x,y,z) → (-x, -z, y)
"""
import sys, json, struct
import numpy as np

src, dst = sys.argv[1], sys.argv[2]
tex_out = sys.argv[3] if len(sys.argv) > 3 else None
d = open(src, 'rb').read()
jl = struct.unpack('<I', d[12:16])[0]
js = json.loads(d[20:20 + jl])
off = 20 + jl
bl, bt = struct.unpack('<II', d[off:off + 8])
binchunk = d[off + 8:off + 8 + bl]

COMP = {5126: ('f', 4), 5123: ('H', 2), 5125: ('I', 4), 5121: ('B', 1)}
NCOMP = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}


def accessor(i):
    a = js['accessors'][i]
    bv = js['bufferViews'][a['bufferView']]
    fmt, size = COMP[a['componentType']]
    n = NCOMP[a['type']]
    start = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
    stride = bv.get('byteStride', size * n)
    out = []
    for k in range(a['count']):
        o = start + k * stride
        out.append(struct.unpack('<' + fmt * n, binchunk[o:o + size * n]))
    arr = np.array(out, dtype=np.float64)
    if a.get('normalized') and fmt != 'f':
        arr /= float(2 ** (8 * size) - 1)
    return arr


nodes = js['nodes']
parent = {}
for i, n in enumerate(nodes):
    for c in n.get('children', []):
        parent[c] = i


def path(i):
    p = []
    while i is not None:
        p.append(nodes[i]['name'])
        i = parent.get(i)
    return '/'.join(reversed(p))


out = {'length': 0.0, 'curves': []}
anim = js['animations'][0]
for ch in anim['channels']:
    s = anim['samplers'][ch['sampler']]
    t = accessor(s['input'])[:, 0]
    v = accessor(s['output'])
    tp = ch['target']['path']
    node = ch['target']['node']
    if tp == 'rotation':
        v = np.stack([v[:, 0], v[:, 2], -v[:, 1], v[:, 3]], axis=1)
    elif tp == 'translation':
        if node not in parent:      # 최상위(Root): Unity 쪽에서 Z-up 회전 바깥이라 Y 축 그대로
            v = np.stack([-v[:, 0], v[:, 1], -v[:, 2]], axis=1)
        else:
            v = np.stack([-v[:, 0], -v[:, 2], v[:, 1]], axis=1)
    else:
        continue
    t = t - t[0]      # 첫 키를 0초로
    out['length'] = max(out['length'], float(t[-1]))
    out['curves'].append({'path': path(node), 'prop': tp, 'dim': int(v.shape[1]), 'times': [round(x, 5) for x in t.tolist()],
                          'values': [round(c, 6) for c in v.reshape(-1).tolist()]})
json.dump(out, open(dst, 'w'))
print(dst, 'curves', len(out['curves']), 'length', round(out['length'], 2))

if tex_out and js.get('images'):
    img = js['images'][0]
    bv = js['bufferViews'][img['bufferView']]
    o = bv.get('byteOffset', 0)
    open(tex_out, 'wb').write(binchunk[o:o + bv['byteLength']])
    print('texture', tex_out, img.get('mimeType'), bv['byteLength'])
