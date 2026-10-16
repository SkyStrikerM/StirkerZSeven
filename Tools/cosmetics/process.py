"""꾸미기 아이템 후처리: raw/ (VARCO 결과) → Resources/StrikerZ/Textures/Cosmetics/

- 슬리브: 원본 card_back 의 알파(둥근 모서리)를 그대로 씌운다
- 덱케이스: 마젠타 배경 키잉 → 여백 잘라 512 높이
- 코인: 원 검출 → 정사각 크롭 → 원형 알파 (256)
- 주사위: 재질 텍스처 위에 원본 dice_faces 의 눈 위치·장식선을 다시 그려 같은 3×2 아틀라스(768×512)로 합성
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ'))
OUT = os.path.join(ROOT, 'Textures', 'Cosmetics')
RAW = os.path.join(HERE, 'raw')
os.makedirs(OUT, exist_ok=True)


def raw(name):
    return Image.open(os.path.join(RAW, name + '.png')).convert('RGB')


def save(im, name):
    im.save(os.path.join(OUT, name + '.png'), optimize=True)
    print('saved', name, im.size)


# ── 슬리브 ──
back_alpha = Image.open(os.path.join(ROOT, 'Textures', 'card_back.png')).getchannel('A')
for k in ['void', 'lava', 'sea', 'sky', 'rainbow', 'earth', 'thunder', 'light']:
    im = raw('sleeve_' + k).resize(back_alpha.size, Image.LANCZOS).convert('RGBA')
    # 생성 이미지 바깥 테두리(배경)가 살짝 보이는 경우가 있어 2% 확대 후 중앙 크롭
    w, h = im.size
    im = im.resize((int(w * 1.02), int(h * 1.02)), Image.LANCZOS)
    dx, dy = (im.width - w) // 2, (im.height - h) // 2
    im = im.crop((dx, dy, dx + w, dy + h))
    im.putalpha(back_alpha)
    save(im, 'sleeve_' + k)

# ── 덱케이스 ──
for k in ['default', 'ignir', 'levias', 'volteon', 'seraphiel', 'noctera', 'silvern', 'gordun']:
    a = np.asarray(raw('case_' + k)).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # 마젠타 정도: R·B 가 높고 G 가 낮을수록 배경
    mag = np.minimum(r, b) - g
    alpha = np.clip((150 - mag) / 70, 0, 1)
    # 경계의 마젠타 번짐 제거 (despill): 반투명 가장자리에서만 G 보다 높은 R·B 를 눌러 준다
    # (녹테라의 보라 달처럼 안쪽의 정상 보라색은 건드리지 않음)
    m = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    edge = 1 - np.asarray(m.filter(ImageFilter.MinFilter(7))).astype(np.float32) / 255
    spill = np.clip(np.minimum(r, b) - g, 0, None) * edge
    out = a.copy()
    out[..., 0] -= spill
    out[..., 2] -= spill
    im = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), 'RGB').convert('RGBA')
    im.putalpha(m)
    bbox = m.point(lambda v: 255 if v > 40 else 0).getbbox()
    pad = 16
    im = im.crop((max(0, bbox[0] - pad), max(0, bbox[1] - pad), min(im.width, bbox[2] + pad), min(im.height, bbox[3] + pad)))
    s = 512 / im.height
    im = im.resize((int(im.width * s), 512), Image.LANCZOS)
    save(im, 'case_' + k)

# ── 코인 ──
for k in ['moon', 'obsidian', 'sapphire', 'phoenix', 'opal', 'gale', 'bronze', 'topaz']:
    for side in 'ht':
        im = raw(f'coin_{k}_{side}')
        a = np.asarray(im).astype(np.float32)
        lum = a.max(axis=2)
        mask = lum > 28
        ys, xs = np.where(mask)
        # 테두리 잡티 영향을 줄이려고 분위수로 원 경계를 잡는다
        x0, x1 = np.percentile(xs, [0.3, 99.7])
        y0, y1 = np.percentile(ys, [0.3, 99.7])
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        rad = max(x1 - x0, y1 - y0) / 2
        box = (int(cx - rad), int(cy - rad), int(cx + rad), int(cy + rad))
        im = im.crop(box).resize((512, 512), Image.LANCZOS).convert('RGBA')
        circ = Image.new('L', (512 * 4, 512 * 4), 0)
        ImageDraw.Draw(circ).ellipse((10, 10, 512 * 4 - 10, 512 * 4 - 10), fill=255)
        circ = circ.resize((512, 512), Image.LANCZOS)
        im.putalpha(circ)
        save(im.resize((256, 256), Image.LANCZOS), f'coin_{k}_{side}')

# ── 주사위 ──
orig = np.asarray(Image.open(os.path.join(ROOT, 'Models', 'dice_faces.png')).convert('RGB')).astype(np.float32)
CELL = 256


def label(mask):
    """작은 4-연결 라벨링 (scipy 없이)"""
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    n = 0
    for y in range(h):
        for x in range(w):
            if mask[y, x] and lab[y, x] == 0:
                n += 1
                stack = [(y, x)]
                lab[y, x] = n
                while stack:
                    cy, cx = stack.pop()
                    for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                        if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and lab[ny, nx] == 0:
                            lab[ny, nx] = n
                            stack.append((ny, nx))
    return lab, n


def pips_of(cell):
    """원본 칸에서 눈(밝은 원) 중심·반지름. 1/4 축소해서 라벨링"""
    lum = cell.mean(axis=2)
    small = lum[::4, ::4] > 175
    lab, n = label(small)
    out = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if len(ys) < 12:
            continue
        cy, cx = ys.mean() * 4 + 2, xs.mean() * 4 + 2
        r = np.sqrt(len(ys) * 16 / np.pi)
        out.append((cx, cy, r))
    return out


def ornament_mask(cell, pips):
    """금색 장식선(테두리 곡선·마름모): 채도 높은 주황 계열에서 눈 영역을 뺀 것"""
    r, g, b = cell[..., 0], cell[..., 1], cell[..., 2]
    m = np.clip(((r - b) - 55) / 50, 0, 1) * np.clip((r - 90) / 60, 0, 1)
    yy, xx = np.mgrid[0:CELL, 0:CELL]
    for cx, cy, pr in pips:
        m[(xx - cx) ** 2 + (yy - cy) ** 2 < (pr * 1.9) ** 2] = 0
    m[:18, -40:] = 0                     # 오른쪽 위 노란 삼각형 번짐
    return m, (r + g + b) / 3


def rounded_shade(size, inset, rad):
    """주사위 모서리 베벨: 안쪽 둥근 사각형 바깥을 어둡게"""
    big = 4
    m = Image.new('L', (size * big, size * big), 0)
    ImageDraw.Draw(m).rounded_rectangle((inset * big, inset * big, (size - inset) * big, (size - inset) * big), rad * big, fill=255)
    m = m.resize((size, size), Image.LANCZOS).filter(ImageFilter.GaussianBlur(7))
    return np.asarray(m).astype(np.float32) / 255


DICE = {
    # 재질, 금속 색(눈·장식), 눈 바탕 홈 색
    'marble': ((1.0, 0.78, 0.32), (0.25, 0.2, 0.15)),
    'lava':   ((1.0, 0.72, 0.28), (0.05, 0.02, 0.02)),
    'ice':    ((0.86, 0.92, 1.0), (0.12, 0.2, 0.35)),
    'jade':   ((1.0, 0.8, 0.36), (0.04, 0.16, 0.08)),
    'nebula': ((0.9, 0.9, 1.0), (0.06, 0.02, 0.1)),
    'amber':  ((1.0, 0.82, 0.4), (0.2, 0.09, 0.02)),
    'storm':  ((0.9, 0.93, 1.0), (0.04, 0.04, 0.1)),
}
shade = rounded_shade(CELL, 10, 46)
cells = [(c, r) for r in range(2) for c in range(3)]
cell_info = []
for c, r in cells:
    cell = orig[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL]
    p = pips_of(cell)
    om, ol = ornament_mask(cell, p)
    cell_info.append((p, om, ol))
    print('cell', c, r, 'pips', len(p))

for k, (metal, groove) in DICE.items():
    mat = raw('dice_' + k)
    if k == 'nebula':                       # 가장자리 액자처럼 나온 부분 잘라내기
        w = mat.width
        mat = mat.crop((int(w * 0.06), int(w * 0.06), int(w * 0.94), int(w * 0.94)))
    mat = np.asarray(mat.resize((CELL * 3, CELL * 3), Image.LANCZOS)).astype(np.float32)
    atlas = np.zeros((CELL * 2, CELL * 3, 3), np.float32)
    metal = np.array(metal) * 255
    groove = np.array(groove) * 255
    yy, xx = np.mgrid[0:CELL, 0:CELL]
    for i, (c, r) in enumerate(cells):
        # 칸마다 재질 다른 부분을 써서 면이 똑같아 보이지 않게
        ox, oy = (i * 97) % (CELL * 2), (i * 151) % (CELL * 2)
        base = mat[oy:oy + CELL, ox:ox + CELL].copy()
        base *= (0.55 + 0.45 * shade)[..., None]
        pips, om, ol = cell_info[i]
        # 장식선: 원본 밝기로 음영을 살린 금속색
        lit = np.clip(ol / 200, 0.35, 1.25)[..., None]
        base = base * (1 - om[..., None] * 0.9) + (metal * lit) * om[..., None] * 0.9
        for cx, cy, pr in pips:
            d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
            R = pr * 1.05
            # 홈 (바깥 그림자)
            g = np.clip((R * 1.32 - d) / 4, 0, 1)
            base = base * (1 - g[..., None] * 0.85) + groove * g[..., None] * 0.85
            # 금속 눈: 위쪽이 밝은 구면 음영 + 하이라이트
            disc = np.clip((R - d) / 2, 0, 1)
            nyv = (yy - cy) / R
            nxv = (xx - cx) / R
            sph = np.clip(1.15 - 0.55 * (nyv + 0.6) ** 2 - 0.3 * nxv ** 2, 0.35, 1.2)
            col = metal[None, None, :] * sph[..., None]
            hl = np.clip(1 - np.sqrt((nxv + 0.3) ** 2 + (nyv + 0.38) ** 2) / 0.32, 0, 1) ** 1.5
            col = col + (255 - col) * hl[..., None] * 0.85
            base = base * (1 - disc[..., None]) + col * disc[..., None]
        atlas[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL] = base
    save(Image.fromarray(np.clip(atlas, 0, 255).astype(np.uint8), 'RGB'), 'dice_' + k)
