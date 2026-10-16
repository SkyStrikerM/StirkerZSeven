# -*- coding: utf-8 -*-
"""VARCO 보스 3D 메시 → Assets/StrikerZ/Bosses/<id>/model.fbx + albedo.png
사용: python fetch_bosses.py boss_urls.json
boss_urls.json = { "R001": {"fbx": URL, "glb": URL}, ... }   (URL 은 /api/objects/... 도 가능)
이후 Unity 메뉴 StrikerZ/레이드 보스 3D 프리팹 만들기 (BossModelBuilder.BuildAll) 로 Resources/StrikerZ/Bosses/<id>.prefab 생성."""
import sys, os, json, struct, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
BOSSES = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Bosses/'
RAW = os.path.join(HERE, 'raw')
os.makedirs(RAW, exist_ok=True)


def get(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 1000: return
    if not url.startswith('http'): url = 'https://3d.varco.ai' + url
    for i in range(4):
        try:
            open(path, 'wb').write(urllib.request.urlopen(url, timeout=300).read()); return
        except Exception:
            if i == 3: raise


def glb_texture(glb, out_png):
    d = open(glb, 'rb').read()
    jl = struct.unpack('<I', d[12:16])[0]
    js = json.loads(d[20:20 + jl])
    off = 20 + jl
    bl = struct.unpack('<I', d[off:off + 4])[0]
    binc = d[off + 8:off + 8 + bl]
    if not js.get('images'): return False
    bv = js['bufferViews'][js['images'][0]['bufferView']]
    o = bv.get('byteOffset', 0)
    open(out_png, 'wb').write(binc[o:o + bv['byteLength']])
    return True


U = json.load(open(sys.argv[1], encoding='utf-8'))
for bid, u in U.items():
    dst = BOSSES + bid + '/'
    os.makedirs(dst, exist_ok=True)
    get(u['fbx'], dst + 'model.fbx')
    g = os.path.join(RAW, bid + '.glb')
    get(u['glb'], g)
    ok = glb_texture(g, dst + 'albedo.png')
    print(bid, 'fbx', os.path.getsize(dst + 'model.fbx'), 'tex', ok)
