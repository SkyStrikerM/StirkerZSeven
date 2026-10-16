# -*- coding: utf-8 -*-
"""선·칸·문장을 지운 필드 배경(EditImage) → Textures/Fields/playmat_<id>.png (1376x768)
룬·연성진 문양은 게임에서 playmat_overlay 레이어로 그 위에 겹쳐진다. 문양이 잘 보이게 배경은 살짝 어둡게.
사용: python install_clean.py <F001> <URL>"""
import sys, os, io, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Fields/'
DARKEN = 0.82
fid, url = sys.argv[1], sys.argv[2]
if not url.startswith('http'): url = 'https://3d.varco.ai' + url
cache = os.path.join(HERE, 'raw', f'clean_{fid}.png')
if not (os.path.exists(cache) and os.path.getsize(cache) > 10000):
    for i in range(4):
        try:
            open(cache, 'wb').write(urllib.request.urlopen(url, timeout=180).read()); break
        except Exception as e:
            if i == 3: raise
im = Image.open(cache).convert('RGB').resize((1376, 768), Image.LANCZOS)
a = np.asarray(im).astype(np.float32) * DARKEN
Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(OUT + f'playmat_{fid}.png')
print(fid, 'ok')
