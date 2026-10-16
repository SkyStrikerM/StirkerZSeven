# -*- coding: utf-8 -*-
"""VARCO 필드 테마 → Resources/StrikerZ/Textures/Fields/playmat_<id>.png · env_<id>.png
사용: python install_field.py <F001> <playmat URL> <env URL>
  playmat: 스케매틱(schem_playmat.png) 기반 EditImage 결과 → 1376x768 (기본 플레이매트와 같은 크기)
  env    : 위에서 내려다본 주변 환경 → 1920x1080
"""
import sys, os, urllib.request, io
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Fields/'
os.makedirs(OUT, exist_ok=True)
os.makedirs(os.path.join(HERE, 'raw'), exist_ok=True)


def get(url, cache):
    if os.path.exists(cache) and os.path.getsize(cache) > 10000:
        return Image.open(cache).convert('RGB')
    if not url.startswith('http'): url = 'https://3d.varco.ai' + url
    for i in range(4):
        try:
            data = urllib.request.urlopen(url, timeout=180).read()
            open(cache, 'wb').write(data)
            return Image.open(io.BytesIO(data)).convert('RGB')
        except Exception as e:
            err = e
    raise err


fid, pm_url, env_url = sys.argv[1:4]
pm = get(pm_url, os.path.join(HERE, 'raw', f'playmat_{fid}.png')).resize((1376, 768), Image.LANCZOS)
pm.save(OUT + f'playmat_{fid}.png')
env = get(env_url, os.path.join(HERE, 'raw', f'env_{fid}.png')).resize((1920, 1080), Image.LANCZOS)
env.save(OUT + f'env_{fid}.png', optimize=True)
print(fid, 'ok')
