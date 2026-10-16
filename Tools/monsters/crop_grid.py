# -*- coding: utf-8 -*-
"""2x2 그리드 이미지 → 카드 아트 4장 (1024x765). 사용: python crop_grid.py <groupIndex> <url> [preview.jpg]"""
import sys, io, json, os, urllib.request
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
DST = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Art/Cards/'
groups = json.load(open(os.path.join(HERE, 'art_groups.json'), encoding='utf-8'))


def gutter(profile, n):
    """중앙 30~70% 구간에서 가장 어두운 줄(검은 간격) 찾기"""
    a, b = int(n * 0.35), int(n * 0.65)
    seg = profile[a:b]
    i = int(np.argmin(seg)) + a
    if profile[i] > 60:   # 간격이 없으면 정확히 절반
        return n // 2, n // 2
    lo = i
    while lo > a and profile[lo - 1] < profile[i] + 25: lo -= 1
    hi = i
    while hi < b and profile[hi + 1] < profile[i] + 25: hi += 1
    return lo, hi + 1


def trim_dark(img):
    """가장자리의 검은 테두리·단색 여백(회색 마진, 패널 테두리 선) 제거"""
    g = np.asarray(img.convert('L')).astype(float)
    def flat(line): return line.mean() < 40 or line.mean() > 222 or line.std() < 13
    H, W = g.shape
    t, b, l, r = 0, H, 0, W
    for _ in range(3):   # 마진 → 테두리 선 → 안쪽 마진 순으로 여러 겹 제거
        while t < H // 7 and flat(g[t, l:r]): t += 1
        while b > H * 6 // 7 and flat(g[b - 1, l:r]): b -= 1
        while l < W // 7 and flat(g[t:b, l]): l += 1
        while r > W * 6 // 7 and flat(g[t:b, r - 1]): r -= 1
    return img.crop((l, t, r, b))


def fit(img, W=1024, H=765):
    w, h = img.size
    if w / h > W / H:
        nw = int(h * W / H); img = img.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
    else:
        nh = int(w * H / W); img = img.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
    return img.resize((W, H), Image.LANCZOS)


def main():
    gi, url = int(sys.argv[1]), sys.argv[2]
    if not url.startswith('http'): url = 'https://3d.varco.ai' + url
    os.makedirs(os.path.join(HERE, 'grids'), exist_ok=True)
    cache = os.path.join(HERE, 'grids', f'g{gi:02d}.png')
    if os.path.exists(cache) and os.path.getsize(cache) > 10000:
        raw = open(cache, 'rb').read()
    else:
        raw = urllib.request.urlopen(url, timeout=180).read()
        open(cache, 'wb').write(raw)
    im = Image.open(io.BytesIO(raw)).convert('RGB')
    g = np.asarray(im.convert('L')).astype(float)
    x0, x1 = gutter(g.mean(0), im.width)
    y0, y1 = gutter(g.mean(1), im.height)
    boxes = [(0, 0, x0, y0), (x1, 0, im.width, y0), (0, y1, x0, im.height), (x1, y1, im.width, im.height)]
    grp = groups[gi]
    outs = []
    for k, (box, mid) in enumerate(zip(boxes, grp['ids'])):
        if k >= grp['fill']: break
        p = trim_dark(im.crop(box))
        w, h = p.size
        p = p.crop((int(w * 0.03), int(h * 0.03), w - int(w * 0.03), h - int(h * 0.03)))
        p = fit(p)
        p.save(DST + mid + '.png')
        outs.append(p)
    print(gi, im.size, 'gutters', (x0, x1), (y0, y1), 'saved', grp['ids'][:grp['fill']])
    if len(sys.argv) > 3:
        sheet = Image.new('RGB', (1024, 765))
        for k, p in enumerate(outs): sheet.paste(p.resize((512, 382)), ((k % 2) * 512, (k // 2) * 383))
        sheet.save(sys.argv[3], quality=85)


main()
