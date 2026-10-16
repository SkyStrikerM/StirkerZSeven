# -*- coding: utf-8 -*-
"""VARCO 보스 하수인·스킬 일러스트(4:3) → Resources/StrikerZ/Art/Cards/<id>.png (1024x765). 사용: python install_art.py"""
import json, os, urllib.request
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
D = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Art/Cards/'
RAW = os.path.join(HERE, 'raw'); os.makedirs(RAW, exist_ok=True)
U = json.load(open(os.path.join(HERE, 'art_urls.json')))
sheet = Image.new('RGB', (7 * 256, 3 * 192), (20, 20, 20))
for k, (cid, h) in enumerate(sorted(U.items())):
    p = os.path.join(RAW, cid + '.png')
    if not os.path.exists(p):
        open(p, 'wb').write(urllib.request.urlopen(f'https://3d.varco.ai/api/objects/{h}.png', timeout=240).read())
    im = Image.open(p).convert('RGB'); W, H = im.size
    th = int(W * 765 / 1024)
    if th <= H: im = im.crop((0, (H - th) // 2, W, (H - th) // 2 + th))
    else:
        tw = int(H * 1024 / 765); im = im.crop(((W - tw) // 2, 0, (W - tw) // 2 + tw, H))
    im = im.resize((1024, 765), Image.LANCZOS); im.save(D + cid + '.png')
    b, slot = divmod(k, 3)
    sheet.paste(im.resize((256, 192)), (b * 256, slot * 192))
sheet.save(os.path.join(HERE, 'minion_skill_preview.jpg'), quality=85)
print('installed', len(U))
