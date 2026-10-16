# -*- coding: utf-8 -*-
"""VARCO 3D 프레임 → Frames/<out>.png + <out>_over.png
사용: python process_frame.py <layoutKey monster|magic|hero|field> <url> <outName>"""
import sys, io, os, json, urllib.request
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__))
from collections import deque
def grow(mask, seeds):
    """mask(bool) 안에서 seeds 와 이어진 영역 (4-연결 BFS)"""
    H, W = mask.shape
    out = np.zeros_like(mask)
    q = deque()
    for (y, x) in seeds:
        if mask[y, x] and not out[y, x]:
            out[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        if y > 0 and mask[y - 1, x] and not out[y - 1, x]: out[y - 1, x] = True; q.append((y - 1, x))
        if y < H - 1 and mask[y + 1, x] and not out[y + 1, x]: out[y + 1, x] = True; q.append((y + 1, x))
        if x > 0 and mask[y, x - 1] and not out[y, x - 1]: out[y, x - 1] = True; q.append((y, x - 1))
        if x < W - 1 and mask[y, x + 1] and not out[y, x + 1]: out[y, x + 1] = True; q.append((y, x + 1))
    return out

D = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Frames/'
L = json.load(open(os.path.join(HERE, 'layouts.json')))
key, url, out = sys.argv[1], sys.argv[2], sys.argv[3]
if not url.startswith('http'): url = 'https://3d.varco.ai' + url
os.makedirs(os.path.join(HERE, 'raw'), exist_ok=True)
cache = os.path.join(HERE, 'raw', out + '.png')
if os.path.exists(cache) and os.path.getsize(cache) > 10000: raw = open(cache, 'rb').read()
else:
    raw = urllib.request.urlopen(url, timeout=180).read(); open(cache, 'wb').write(raw)
im = Image.open(io.BytesIO(raw)).convert('RGB')
W0, H0 = im.size
# 스케매틱: 300x400 캔버스, 카드 폭 286 (x 7~293)
x0, x1 = W0 * 7 / 300, W0 * 293 / 300
im = im.crop((int(round(x0)), 0, int(round(x1)), H0)).resize((858, 1200), Image.LANCZOS)
W, H = im.size
rgba = im.convert('RGBA')
arr = np.array(rgba)
lum = arr[..., :3].mean(-1)
r = L[key]

def box(rect, pad=(0, 0)):
    a, b, c, d = rect
    return [(a - pad[0]) * W, (1 - d - pad[1]) * H, (c + pad[0]) * W, (1 - b + pad[1]) * H]

# 정렬 측정: 아트 창 근처의 검은 영역 경계
ax0, ay0, ax1, ay1 = [int(v) for v in box(r['art'], (0.03, 0.03))]
blk = lum[max(0, ay0):ay1, max(0, ax0):ax1] < 30
ys, xs = np.where(blk)
if len(xs):
    mx0, mx1 = (xs.min() + ax0) / W, (xs.max() + ax0) / W
    my0, my1 = 1 - (ys.max() + ay0) / H, 1 - (ys.min() + ay0) / H
    print(f'{out}: art expected {r["art"]}  measured ({mx0:.3f},{my0:.3f},{mx1:.3f},{my1:.3f})')

alpha = Image.new('L', (W, H), 255)
dr = ImageDraw.Draw(alpha)
# 아트 창 (정확한 레이아웃 기준) + 그 주변의 검은 픽셀
dr.rectangle(box(r['art']), fill=0)
# 실제로 그려진 검은 창 = 아트 창 중심에서 이어진 어두운 영역 (모델이 창을 조금 크게 그리는 경우 대응)
near = np.zeros((H, W), bool)
ex = [int(v) for v in box(r['art'], (0.035, 0.03))]
near[max(0, ex[1]):ex[3], max(0, ex[0]):ex[2]] = True
cxp, cyp = int((r['art'][0] + r['art'][2]) / 2 * W), int((1 - (r['art'][1] + r['art'][3]) / 2) * H)
comp = grow(near & (arr[..., :3].max(-1) < 22), [(cyp, cxp)])
a = np.array(alpha)
a[comp] = 0
ys, xs = np.where(a == 0)
inner = (xs > W * 0.03) & (xs < W * 0.97)
meas = (xs[inner].min() / W, 1 - ys[inner].max() / H, xs[inner].max() / W, 1 - ys[inner].min() / H)
mp = os.path.join(HERE, 'frame_meas.json')
mj = json.load(open(mp)) if os.path.exists(mp) else {}
mj[out] = [round(v, 4) for v in meas]
json.dump(mj, open(mp, 'w'), indent=1)
print(out, 'hole', mj[out])
# 바깥 흰 여백 (둥근 모서리 밖) → 투명
# 바깥 흰 여백: 가장자리에서 이어진 밝은 무채색 영역 전부 (모델이 프레임을 안쪽에 작게 그린 경우 포함)
rgb = arr[..., :3].astype(int)
whiteish = (lum > 222) & ((rgb.max(-1) - rgb.min(-1)) < 22)
seeds = [(0, x) for x in range(W)] + [(H - 1, x) for x in range(W)] + [(y, 0) for y in range(H)] + [(y, W - 1) for y in range(H)]
a[grow(whiteish, seeds)] = 0
a[:, :2] = np.minimum(a[:, :2], 255);
alpha = Image.fromarray(a).filter(ImageFilter.GaussianBlur(0.8))
rgba.putalpha(alpha)
rgba.save(D + out + '.png')

# 오버프레임: 이름·종류·텍스트 영역 뚫기 (make_over_frames.py 와 같은 여백)
pads = dict(name=(0.012, 0.008), type=(0.012, 0.008), body=(0.036, 0.021))
hole = Image.new('L', (W, H), 0)
dh = ImageDraw.Draw(hole)
for k in ('name', 'type', 'body'):
    dh.rounded_rectangle(box(r[k], pads[k]), radius=int(W * 0.012), fill=255)
hole = hole.filter(ImageFilter.GaussianBlur(1.5))
ov = rgba.copy()
ov.putalpha(Image.fromarray(np.minimum(np.array(rgba.getchannel('A')), 255 - np.array(hole)).astype('uint8')))
ov.save(D + out + '_over.png')
print('saved', out)
