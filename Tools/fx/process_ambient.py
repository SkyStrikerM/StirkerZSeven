# -*- coding: utf-8 -*-
"""VARCO(nano-banana-pro) 로 만든 필드 분위기 스프라이트(마젠타 배경) → 투명 PNG 512px → Resources/StrikerZ/Textures/Fx/amb_*.png
사용: python Tools/fx/process_ambient.py [이름 ...]   (이름 생략 시 ambient_urls.json 전부)
  1) ambient_urls.json (이름 → objects 해시) 에서 raw/amb_*.png 로 내려받고
  2) 마젠타 키 (Tools/nav/process.py 와 같은 규칙) → 알파, 스필 제거
  3) 내용이 있는 부분만 잘라 정사각형 가운데 두고 512px 로"""
import sys, os, json, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Fx/'
RAW = os.path.join(HERE, 'raw')


def key(path):
    a = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.maximum(np.maximum(r, b), 1)
    bal = np.minimum(r, b) / m
    gr = g / m
    w = np.clip((bal - 0.68) / 0.17, 0, 1) * np.clip((0.42 - gr) / 0.17, 0, 1) * np.clip((m - 50) / 40, 0, 1)
    alpha = 1 - w
    alpha[alpha < 0.05] = 0
    spill = np.clip(np.minimum(r, b) - g, 0, None) * w
    out = a.copy()
    out[..., 0] -= spill
    out[..., 2] -= spill
    rgba = np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')


def square(img, size=512, pad=0.04):
    a = np.asarray(img)[..., 3]
    ys, xs = np.where(a > 20)
    if len(ys) == 0:
        return img.resize((size, size), Image.LANCZOS)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    side = int(max(y1 - y0, x1 - x0) * (1 + pad * 2))
    cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
    canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    canvas.paste(img.crop((x0, y0, x1, y1)), (side // 2 - (x1 - x0) // 2, side // 2 - (y1 - y0) // 2))
    return canvas.resize((size, size), Image.LANCZOS)


if __name__ == '__main__':
    urls = json.load(open(os.path.join(HERE, 'ambient_urls.json'), encoding='utf-8'))
    names = sys.argv[1:] or list(urls.keys())
    os.makedirs(RAW, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    for n in names:
        raw = os.path.join(RAW, n + '.png')
        if not os.path.exists(raw):
            urllib.request.urlretrieve('https://3d.varco.ai/api/objects/' + urls[n] + '.png', raw)
        img = square(key(raw))
        img.save(OUT + n + '.png')
        print('ok', n, img.size)
    # 검토용 시트
    sheet = Image.new('RGBA', (256 * 6, 256 * ((len(names) + 5) // 6)), (60, 60, 60, 255))
    for i, n in enumerate(names):
        im = Image.open(OUT + n + '.png').resize((256, 256))
        sheet.paste(im, ((i % 6) * 256, (i // 6) * 256), im)
    sheet.save(os.path.join(HERE, 'sheet_ambient.png'))
