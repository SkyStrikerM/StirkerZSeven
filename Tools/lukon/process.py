"""루콘(로지 할아버지) 상점 그림 후처리.
raw/lukon_a.png (마젠타 배경) → Textures/shop_lukon.png (투명, 높이 1200)
raw/workshop.png → Textures/shop_lukon_bg.png
안경(골뱅이 렌즈) 위치를 0~1 좌표로 출력 → MainMenu.Cosmetics.LukonGlasses
"""
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Textures'))

src = Image.open(os.path.join(HERE, 'raw', 'lukon_a.png')).convert('RGB')
a = np.asarray(src).astype(np.float32)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
mag = np.minimum(r, b) - g                      # 마젠타 정도
alpha = np.clip((150 - mag) / 70, 0, 1)
m = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
edge = 1 - np.asarray(m.filter(ImageFilter.MinFilter(7))).astype(np.float32) / 255
spill = np.clip(np.minimum(r, b) - g, 0, None) * edge          # 가장자리 마젠타 번짐 제거
out = a.copy()
out[..., 0] -= spill
out[..., 2] -= spill
im = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).convert('RGBA')
im.putalpha(m)
bbox = m.point(lambda v: 255 if v > 40 else 0).getbbox()
pad = 12
box = (max(0, bbox[0] - pad), max(0, bbox[1] - pad), min(im.width, bbox[2] + pad), min(im.height, bbox[3] + 2))
im = im.crop(box)
s = 1200 / im.height
im = im.resize((int(im.width * s), 1200), Image.LANCZOS)
im.save(os.path.join(TEX, 'shop_lukon.png'), optimize=True)
print('shop_lukon', im.size)

# 안경: 위쪽 20~45% 높이에서 밝고 채도 낮은(흰 소용돌이 렌즈) 픽셀의 중심
c = np.asarray(im).astype(np.float32)
H, W = c.shape[:2]
y0, y1 = int(H * 0.15), int(H * 0.40)
reg = c[y0:y1]
mx, mn = reg[..., :3].max(axis=2), reg[..., :3].min(axis=2)
lens = (mn > 205) & (mx - mn < 30) & (reg[..., 3] > 200)
ys, xs = np.nonzero(lens)
cy, cx = ys.mean() + y0, xs.mean()
print(f'glasses uv = ({cx / W:.3f}, {1 - cy / H:.3f})  px=({cx:.0f},{cy:.0f}) n={len(xs)}')

bg = Image.open(os.path.join(HERE, 'raw', 'workshop.png')).convert('RGB')
bg.save(os.path.join(TEX, 'shop_lukon_bg.png'), optimize=True)
print('shop_lukon_bg', bg.size)
