"""타이틀 화면 그림 후처리 (VARCO 3D · nano-banana-pro 결과 → Textures)
raw/bg_a.png  → title_bg_clean.png  (운석·무지개 꼬리를 뺀 배경)
raw/met_b.png → title_meteor_0..6.png (마젠타 배경 키잉 → 7개로 잘라 머리가 아래로 오게 세운다)
               순서: 화염 · 물 · 바람 · 대지 · 번개 · 빛 · 어둠 (TitleScreen.MeteorGlow 와 같음)
raw/txt_b.png → title_start.png (검은 배경 → 밝기로 알파, "Click Anywhere To Start")
"""
import math
import os
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw')
TEX = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Textures'))

# ── 배경 ──
Image.open(os.path.join(RAW, 'bg_a.png')).convert('RGB').save(os.path.join(TEX, 'title_bg_clean.png'), optimize=True)
print('title_bg_clean')

# ── 운석: 마젠타 키잉 ──
a = np.asarray(Image.open(os.path.join(RAW, 'met_b.png')).convert('RGB')).astype(np.float32)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
mag = np.clip((np.minimum(r, b) - g) / 255, 0, 1)            # 마젠타에 가까울수록 1
alpha = np.clip((0.62 - mag) / 0.42, 0, 1)
# 반투명 부분은 마젠타가 섞여 있으니 빼 준다 (C = aF + (1-a)M → F)
M = np.array([255, 0, 255], np.float32)
safe = np.maximum(alpha, 0.05)[..., None]
fg = np.clip((a - (1 - alpha)[..., None] * M) / safe, 0, 255)
fg = np.where(alpha[..., None] > 0.97, a, fg)
# 남은 분홍 번짐 제거: 반투명 가장자리에서 r·b 가 g 보다 튀는 만큼 깎는다
spill = np.clip(np.minimum(fg[..., 0], fg[..., 2]) - fg[..., 1], 0, None) * (1 - alpha) * 1.6
fg[..., 0] -= spill
fg[..., 2] -= spill
fg = np.clip(fg, 0, 255)
rgba = np.dstack([fg, alpha * 255]).astype(np.uint8)
H, W = alpha.shape

# 덩어리 나누기 (1/4 축소 + 팽창 후 flood fill)
s = 4
small = Image.fromarray((alpha > 0.08).astype(np.uint8) * 255).resize((W // s, H // s), Image.BILINEAR)
small = np.asarray(small.filter(ImageFilter.MaxFilter(5))) > 20
lab = np.zeros(small.shape, np.int32)
blobs = []
for y in range(small.shape[0]):
    for x in range(small.shape[1]):
        if small[y, x] and not lab[y, x]:
            n = len(blobs) + 1
            q = deque([(y, x)]); lab[y, x] = n; pts = []
            while q:
                cy, cx = q.popleft(); pts.append((cy, cx))
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy + dy, cx + dx
                    if 0 <= ny < small.shape[0] and 0 <= nx < small.shape[1] and small[ny, nx] and not lab[ny, nx]:
                        lab[ny, nx] = n; q.append((ny, nx))
            ys, xs = zip(*pts)
            blobs.append((len(pts), min(xs) * s, min(ys) * s, (max(xs) + 1) * s, (max(ys) + 1) * s, n))
blobs = sorted(blobs, reverse=True)[:7]
# 그리드 순서: 위 줄(왼→오) 다음 아래 줄
blobs.sort(key=lambda t: ((t[2] + t[4]) / 2 > H / 2, (t[1] + t[3]) / 2))
for i, (_, x0, y0, x1, y1, n) in enumerate(blobs):
    pad = 6
    x0, y0, x1, y1 = max(0, x0 - pad), max(0, y0 - pad), min(W, x1 + pad), min(H, y1 + pad)
    m = np.kron(lab == n, np.ones((s, s), bool))[:H, :W]
    m = np.pad(m, ((0, H - m.shape[0]), (0, W - m.shape[1])))
    piece = rgba[y0:y1, x0:x1].copy()
    piece[..., 3] = (piece[..., 3] * m[y0:y1, x0:x1]).astype(np.uint8)   # 이웃 운석 조각 제거
    # 빛무리에 남은 분홍기: 분홍·자홍 색조를 그 속성 색조로 돌린다 (어둠=보라는 그대로)
    if i != 6:
        HUE = [18, 150, 100, 38, 45, 42][i]                   # PIL HSV 색조 0~255 (화염 주황 · 물 파랑 · 바람 초록 · 대지/번개/빛 금색)
        hsv = np.asarray(Image.fromarray(piece[..., :3], 'RGB').convert('HSV')).copy()
        hh = hsv[..., 0].astype(np.int32)
        pink = (hh > 185) | (hh < 12 if i != 0 else hh < 0)
        pink &= piece[..., 3] < 254
        hsv[..., 0][pink] = HUE
        piece[..., :3] = np.asarray(Image.fromarray(hsv, 'HSV').convert('RGB'))
    al = piece[..., 3].astype(np.float32) / 255
    ys, xs = np.nonzero(al > 0.3)
    w = al[ys, xs]
    cx, cy = np.average(xs, weights=w), np.average(ys, weights=w)
    cov = np.cov(np.vstack([xs - cx, ys - cy]), aweights=w)
    ev, evec = np.linalg.eigh(cov)
    ax = evec[:, 1]                                                     # 긴 축 (꼬리 방향)
    # 머리 = 꽉 찬(불투명) 픽셀이 많은 쪽
    proj = (xs - cx) * ax[0] + (ys - cy) * ax[1]
    solid = al[ys, xs] > 0.95
    if (proj[solid] > 0).sum() > (proj[solid] < 0).sum():
        ax = -ax                                                        # ax 는 머리 → 꼬리
    # 머리가 아래(+y), 꼬리가 위로 가게 회전: 꼬리 방향을 (0,-1) 로
    ang = math.degrees(math.atan2(ax[1], ax[0]))                       # 이미지 좌표 (y 아래)
    rot = -(ang + 90)                                                   # PIL 회전은 반시계
    im = Image.fromarray(piece, 'RGBA').rotate(-rot, resample=Image.BICUBIC, expand=True)
    bb = im.split()[3].point(lambda v: 255 if v > 10 else 0).getbbox()
    im = im.crop(bb)
    if im.height > 560:
        im = im.resize((int(im.width * 560 / im.height), 560), Image.LANCZOS)
    im.save(os.path.join(TEX, f'title_meteor_{i}.png'), optimize=True)
    # 박힌 뒤 남길 보석 머리: 같은 크기, 위쪽(꼬리)으로 갈수록 투명하게
    hd = np.asarray(im).copy()
    up = 1 - (np.arange(hd.shape[0], dtype=np.float32) + 0.5) / hd.shape[0]   # 아래 0 → 위 1
    ramp = np.clip((0.52 - up) / 0.18, 0, 1)[:, None]
    hd[..., 3] = (hd[..., 3] * ramp).astype(np.uint8)
    Image.fromarray(hd, 'RGBA').save(os.path.join(TEX, f'title_meteor_{i}_head.png'), optimize=True)
    print(f'title_meteor_{i}', im.size, f'tail angle {ang:.0f}')

# ── 안내 문구: 검은 배경 → 밝기 알파 ──
t = np.asarray(Image.open(os.path.join(RAW, 'txt_b.png')).convert('RGB')).astype(np.float32)
mx = t.max(axis=2)
al = np.clip((mx - 12) / 200, 0, 1)
col = np.clip(t / np.maximum(al, 0.05)[..., None], 0, 255)
out = Image.fromarray(np.dstack([col, al * 255]).astype(np.uint8), 'RGBA')
bb = out.split()[3].point(lambda v: 255 if v > 18 else 0).getbbox()
out = out.crop((max(0, bb[0] - 10), max(0, bb[1] - 10), min(out.width, bb[2] + 10), min(out.height, bb[3] + 10)))
out.save(os.path.join(TEX, 'title_start.png'), optimize=True)
print('title_start', out.size)
