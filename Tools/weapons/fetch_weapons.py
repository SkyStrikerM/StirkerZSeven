# -*- coding: utf-8 -*-
"""VARCO 무기 메시 + 무기용 공격 모션 → Assets/StrikerZ/Heroes/<id>/
사용: python fetch_weapons.py urls.json
urls.json = { "H001R": {"fbx": URL, "glb": URL}, ..., "H001_attack": URL(glb), ... }
결과: weapon_<slot>.fbx · weapon_<slot>.png (glb 의 텍스처) · weapons.json (HeroModelBuilder 가 읽는 장착 설정) · attack.json (새 공격 모션)
이후 Unity 메뉴 StrikerZ/영웅 3D 프리팹 전부 만들기 (HeroModelBuilder.Build) 로 프리팹에 반영."""
import sys, os, json, struct, subprocess, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
HEROES = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Heroes/'
GLB2ANIM = 'C:/Users/MBC-501-22/AppData/Local/Temp/claude/C--Users-MBC-501-22-Desktop-Card/70516d1a-ee71-4f64-aca9-0e8d1addfdb4/scratchpad/glb2anim.py'
RAW = os.path.join(HERE, 'raw')
os.makedirs(RAW, exist_ok=True)


def get(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 1000: return
    if not url.startswith('http'): url = 'https://3d.varco.ai' + url
    for i in range(4):
        try:
            open(path, 'wb').write(urllib.request.urlopen(url, timeout=240).read()); return
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


W = json.load(open(os.path.join(HERE, 'weapons.json'), encoding='utf-8'))
U = json.load(open(sys.argv[1], encoding='utf-8'))
flip = W.get('_flip', {})
for hero, cfg in W.items():
    if hero.startswith('_'): continue
    dst = HEROES + hero + '/'
    out = []
    for w in cfg['weapons']:
        src = w.get('same') or w['slot']
        key = hero + src
        if key in U and not w.get('same'):
            get(U[key]['fbx'], dst + f'weapon_{src}.fbx')
            g = os.path.join(RAW, f'{key}.glb')
            get(U[key]['glb'], g)
            glb_texture(g, dst + f'weapon_{src}.png')
        out.append(dict(slot=w['slot'], src=src, hand=w['hand'], kind=w['kind'], length=w['length'], grip=w['grip'], flip=bool(flip.get(key, False))))
    json.dump({'weapons': out}, open(dst + 'weapons.json', 'w'), indent=1)
    ak = hero + '_attack'
    if ak in U:
        g = os.path.join(RAW, f'{ak}.glb')
        get(U[ak], g)
        subprocess.run([sys.executable, GLB2ANIM, g, dst + 'attack.json'], check=True, capture_output=True)
    print(hero, 'weapons', [o['slot'] for o in out], 'attack' if ak in U else '')
