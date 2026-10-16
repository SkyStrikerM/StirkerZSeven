"""로지의 부모님(로한·벨라)과 방문 그림 후처리.
raw/rohan_b.png, raw/bella_a.png (마젠타 배경) → Textures/shop_rohan.png, shop_bella.png (투명, 높이 1200)
raw/room_door.png → Textures/shop_room_door.png (양옆 어두운 벽 띠를 잘라 문짝만)
"""
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Textures'))


def key(src, dst):
    a = np.asarray(Image.open(os.path.join(HERE, 'raw', src)).convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((150 - (np.minimum(r, b) - g)) / 70, 0, 1)
    m = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    edge = 1 - np.asarray(m.filter(ImageFilter.MinFilter(7))).astype(np.float32) / 255
    spill = np.clip(np.minimum(r, b) - g, 0, None) * edge
    out = a.copy()
    out[..., 0] -= spill
    out[..., 2] -= spill
    im = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).convert('RGBA')
    im.putalpha(m)
    bb = m.point(lambda v: 255 if v > 40 else 0).getbbox()
    im = im.crop((max(0, bb[0] - 12), max(0, bb[1] - 12), min(im.width, bb[2] + 12), min(im.height, bb[3] + 2)))
    s = 1200 / im.height
    im = im.resize((int(im.width * s), 1200), Image.LANCZOS)
    im.save(os.path.join(TEX, dst), optimize=True)
    print(dst, im.size)


key('rohan_b.png', 'shop_rohan.png')
key('bella_a.png', 'shop_bella.png')

# 방문: 밝은 나무 문짝 열만 남긴다 (양옆 어두운 벽 기둥 제거)
d = Image.open(os.path.join(HERE, 'raw', 'room_door.png')).convert('RGB')
lum = np.asarray(d.convert('L')).astype(np.float32).mean(axis=0)
bright = np.nonzero(lum > lum.max() * 0.55)[0]
x0, x1 = int(bright.min()), int(bright.max())
d = d.crop((x0 + 6, 0, x1 - 6, d.height))
d.save(os.path.join(TEX, 'shop_room_door.png'), optimize=True)
print('shop_room_door', d.size, 'cut', x0, x1)
