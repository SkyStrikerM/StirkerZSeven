# -*- coding: utf-8 -*-
"""꾸미기 아이템 원본 → VARCO 업로드용 작은 JPEG (src/). 배경은 어두운 색으로 채운다."""
import os
from PIL import Image
R = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/'
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'src')
jobs = {'card_back': ('Textures/card_back.png', 560), 'pack_box': ('Textures/pack_sss_box.png', 460),
        'coin_heads': ('Models/coin_heads.png', 256), 'coin_tails': ('Models/coin_tails.png', 256),
        'dice_faces': ('Models/dice_faces.png', 600)}
for name, (path, w) in jobs.items():
    im = Image.open(R + path).convert('RGBA')
    bg = Image.new('RGBA', im.size, (12, 10, 18, 255)); bg.alpha_composite(im)
    h = round(im.height * w / im.width)
    bg.convert('RGB').resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name + '.jpg'), quality=82)
    print(name, w, h, os.path.getsize(os.path.join(OUT, name + '.jpg')) // 1024, 'KB')
