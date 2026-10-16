# -*- coding: utf-8 -*-
"""VARCO 공격 화살표(검은 배경, 오른쪽 방향) → Resources/StrikerZ/Textures/Fx/arrow_<속성>.png
사용: python process_arrow.py <속성> [이미지 URL]   (URL 생략 시 raw/arrow_<속성>.png 사용)
  1) 밝기 → 알파 (검은 배경 투명, 색은 알파로 나눠 복원 = 빛나는 이펙트)
  2) 화살표 외곽 박스로 자르고 4:1 비율(1024x256)에 맞춰 세로 여백을 붙인다
게임에서는 9-슬라이스로 쓴다: 왼쪽 8%(꼬리) · 오른쪽 30%(화살촉)는 고정, 가운데 몸통만 늘어남 (GameAssets.AttackArrow)"""
import sys, os, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Fx/'
W, H = 1024, 256
elem = sys.argv[1]
raw = os.path.join(HERE, 'raw', f'arrow_{elem}.png')
if len(sys.argv) > 2:
    url = sys.argv[2] if sys.argv[2].startswith('http') else 'https://3d.varco.ai' + sys.argv[2]
    open(raw, 'wb').write(urllib.request.urlopen(url, timeout=180).read())
im = np.asarray(Image.open(raw).convert('RGB')).astype(np.float32) / 255.0

corners = np.concatenate([im[:20, :20], im[:20, -20:], im[-20:, :20], im[-20:, -20:]]).reshape(-1, 3).mean()
white_bg = corners > 0.75                           # 모델이 흰 배경으로 그린 경우 (대지 등)
if white_bg:
    inv = 1.0 - im                                  # c = a·f + (1-a)·1  →  1-c = a·(1-f)
    a = np.clip((inv.max(-1) - 0.035) / 0.965, 0, 1)
else:
    a = im.max(-1)
    a = np.clip((a - 0.045) / 0.955, 0, 1)        # 검은 배경 잡음 제거
ys, xs = np.where(a > 0.06)
x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
pad = 6
x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
x1, y1 = min(im.shape[1], x1 + pad), min(im.shape[0], y1 + pad)
if white_bg:
    rgb = 1.0 - inv[y0:y1, x0:x1] / np.maximum(a[y0:y1, x0:x1, None], 1e-3)
    rgb = rgb * 1.35 + 0.04                         # 흰 배경 원본은 어두운 보드 위에서 탁해 보여 밝게
else:
    rgb = im[y0:y1, x0:x1] / np.maximum(a[y0:y1, x0:x1, None], 1e-3)
rgba = np.dstack([np.clip(rgb, 0, 1), a[y0:y1, x0:x1]])
img = Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA')

# 4:1 로: 세로가 모자라면 위아래 투명 여백, 넘치면 가로 기준으로 맞춤
w, h = img.size
target_h = int(round(w / (W / H)))
if h < target_h:
    canvas = Image.new('RGBA', (w, target_h), (0, 0, 0, 0))
    canvas.paste(img, (0, (target_h - h) // 2))
    img = canvas
img = img.resize((W, H), Image.LANCZOS)
os.makedirs(OUT, exist_ok=True)
img.save(OUT + f'arrow_{elem}.png')
print(elem, 'bbox', (x0, y0, x1, y1), '->', OUT + f'arrow_{elem}.png')
