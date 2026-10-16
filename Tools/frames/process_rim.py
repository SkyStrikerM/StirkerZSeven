# -*- coding: utf-8 -*-
"""VARCO 가공 테두리(얇은 림) → Frames/<out>.png
스케매틱: schem_rim.png (300x400, 회색 띠 + 안쪽 검정). 모델은 띠를 두껍게·카드를 작게 그리는 경향이 있어서
  1) 바깥 흰 여백·안쪽 검정을 투명 처리하고 림 외곽 박스로 자른 뒤
  2) 9-슬라이스처럼 모서리는 비율 유지 축소, 변은 비율 유지 축소 후 길이 방향으로 이어 붙여(교차 페이드)
     띠 두께를 BAND(카드 폭 비율)로 맞춘다 — 빗금 각도·보석 모양이 그대로 유지된다.
사용: python process_rim.py <이미지 URL> <finish_rim_seven | finish_rim_strikerz>"""
import sys, io, os, urllib.request
import numpy as np
from PIL import Image, ImageFilter
from collections import deque

HERE = os.path.dirname(os.path.abspath(__file__))
D = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Frames/'
OW, OH = 858, 1200     # 프레임 이미지와 같은 크기
BAND = 0.037           # 결과 띠 두께 (카드 폭 비율)


def grow(mask, seeds):
    """mask(bool) 안에서 seeds 와 이어진 영역 (4-연결 BFS) — process_frame.py 와 같음"""
    H, W = mask.shape
    out = np.zeros_like(mask)
    q = deque()
    for (y, x) in seeds:
        if mask[y, x] and not out[y, x]:
            out[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not out[ny, nx]:
                out[ny, nx] = True; q.append((ny, nx))
    return out


def tile(strip, length, axis, ov=24):
    """strip 을 axis 방향으로 교차 페이드하며 이어 붙여 length 로"""
    a = np.asarray(strip).astype(np.float32)
    if axis == 1: a = a.transpose(1, 0, 2)
    seg = a.shape[0]
    ov = min(ov, seg // 3)
    out = a.copy()
    w = np.linspace(0, 1, ov)[:, None, None]
    while out.shape[0] < length:
        blend = out[-ov:] * (1 - w) + a[:ov] * w
        out = np.concatenate([out[:-ov], blend, a[ov:]], 0)
    out = out[:length]
    if axis == 1: out = out.transpose(1, 0, 2)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), 'RGBA')


url, out = sys.argv[1], sys.argv[2]
if not url.startswith('http'): url = 'https://3d.varco.ai' + url
os.makedirs(os.path.join(HERE, 'raw'), exist_ok=True)
cache = os.path.join(HERE, 'raw', out + '.png')
if os.path.exists(cache) and os.path.getsize(cache) > 10000: raw = open(cache, 'rb').read()
else:
    raw = urllib.request.urlopen(url, timeout=180).read(); open(cache, 'wb').write(raw)
im = Image.open(io.BytesIO(raw)).convert('RGB')
W0, H0 = im.size
im = im.crop((int(round(W0 * 7 / 300)), 0, int(round(W0 * 293 / 300)), H0)).resize((OW, OH), Image.LANCZOS)
rgb = np.array(im).astype(int)
H, W = rgb.shape[:2]
lum = rgb.mean(-1)

# 1) 투명 처리: 가운데에서 이어진 검정, 가장자리에서 이어진 흰 여백
a = np.full((H, W), 255, np.uint8)
inner = grow(rgb.max(-1) < 26, [(H // 2, W // 2)])
a[inner] = 0
whiteish = (lum > 222) & ((rgb.max(-1) - rgb.min(-1)) < 22)
seeds = [(0, x) for x in range(W)] + [(H - 1, x) for x in range(W)] + [(y, 0) for y in range(H)] + [(y, W - 1) for y in range(H)]
a[grow(whiteish, seeds)] = 0
src = im.convert('RGBA')
src.putalpha(Image.fromarray(a).filter(ImageFilter.GaussianBlur(0.7)))

# 림 외곽 박스로 자르기 + 변마다 띠 두께 측정 (가운데 줄 기준)
ys, xs = np.where(a > 0)
x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
src = src.crop((x0, y0, x1, y1))
inn = inner[y0:y1, x0:x1]
Ws, Hs = src.size
mr, mc = inn[Hs // 2], inn[:, Ws // 2]
bL = int(np.argmax(mr)); bR = int(np.argmax(mr[::-1])); bT = int(np.argmax(mc)); bB = int(np.argmax(mc[::-1]))
bs = max(bL, bR, bT, bB) + 4          # 안쪽 베벨까지 포함
bd = int(round(OW * BAND))
k = bd / bs
print(out, 'rim box', (x0, y0, x1, y1), 'band src', (bL, bR, bT, bB), '->', bd, 'scale', round(k, 3))

def sc(img): return img.resize((max(1, round(img.width * k)), max(1, round(img.height * k))), Image.LANCZOS)

res = Image.new('RGBA', (OW, OH), (0, 0, 0, 0))
c = bs
corners = {
    (0, 0): src.crop((0, 0, c, c)), (OW - bd, 0): src.crop((Ws - c, 0, Ws, c)),
    (0, OH - bd): src.crop((0, Hs - c, c, Hs)), (OW - bd, OH - bd): src.crop((Ws - c, Hs - c, Ws, Hs)),
}
# 변: 모서리 바로 옆 구간은 빼고 가운데 구간을 사용
m = int(c * 0.6)
top = tile(sc(src.crop((c + m, 0, Ws - c - m, c))), OW - 2 * bd, 1)
bot = tile(sc(src.crop((c + m, Hs - c, Ws - c - m, Hs))), OW - 2 * bd, 1)
lef = tile(sc(src.crop((0, c + m, c, Hs - c - m))), OH - 2 * bd, 0)
rig = tile(sc(src.crop((Ws - c, c + m, Ws, Hs - c - m))), OH - 2 * bd, 0)
res.alpha_composite(top.resize((OW - 2 * bd, bd)), (bd, 0))
res.alpha_composite(bot.resize((OW - 2 * bd, bd)), (bd, OH - bd))
res.alpha_composite(lef.resize((bd, OH - 2 * bd)), (0, bd))
res.alpha_composite(rig.resize((bd, OH - 2 * bd)), (OW - bd, bd))
for pos, img in corners.items():
    res.alpha_composite(img.resize((bd, bd), Image.LANCZOS), pos)
res.save(D + out + '.png')
print('saved', out, 'opaque', round(float((np.array(res.getchannel('A')) > 20).mean()), 3))
