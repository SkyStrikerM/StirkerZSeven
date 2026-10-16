"""코인 면 그림 (VARCO nano-banana-pro, 정면·양각) → 원을 찾아 정중앙으로 잘라 512px 원형 PNG
    python Tools/coins/process.py           # PICK 에 적힌 원본으로 전부
  원본: raw/<이름>_<h|t>_<a|b>.png  (prompts.py 의 프롬프트)
  결과: 기본 = Resources/StrikerZ/Models/coin_heads|tails.png, 나머지 = Resources/StrikerZ/Textures/Cosmetics/coin_<이름>_<h|t>.png
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
RES = os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ')
SIZE = 512


def coin_mask(a):
    """마젠타 배경이 아닌 곳 (R·B 높고 G 낮은 = 배경)"""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.maximum(np.maximum(r, b), 1)
    bg = (np.minimum(r, b) / m > 0.72) & (g / m < 0.45) & (m > 60)
    return ~bg


def circle_of(mask):
    """원 중심·반지름: 행/열마다 코인 폭을 보고 가장 넓은 행·열의 가운데 (가장자리 잡티에 강하다)"""
    ys, xs = np.where(mask)
    rows = mask.sum(1); cols = mask.sum(0)
    # 폭이 최대폭의 98% 이상인 행들의 가운데 = 세로 중심, 열도 같게
    cy = np.mean(np.where(rows >= rows.max() * 0.98)[0])
    cx = np.mean(np.where(cols >= cols.max() * 0.98)[0])
    r = (rows.max() + cols.max()) / 4.0
    return cx, cy, r


def process(src, dst):
    im = Image.open(src).convert('RGB')
    a = np.asarray(im).astype(np.float32)
    mask = coin_mask(a)
    cx, cy, r = circle_of(mask)
    r *= 0.985                                     # 가장자리 마젠타 번짐을 살짝 잘라 낸다
    box = (cx - r, cy - r, cx + r, cy + r)
    face = im.crop(tuple(int(round(v)) for v in box)).resize((SIZE, SIZE), Image.LANCZOS)
    # 원형 알파 (안티에일리어스)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    d = np.sqrt((xx - (SIZE - 1) / 2) ** 2 + (yy - (SIZE - 1) / 2) ** 2) / (SIZE / 2)
    alpha = np.clip((1.0 - d) * SIZE / 2, 0, 1)
    rgba = np.dstack([np.asarray(face), (alpha * 255).astype(np.uint8)])
    Image.fromarray(rgba.astype(np.uint8), 'RGBA').save(dst)
    ring = (d > 0.86) & (d < 0.97)
    rim = np.asarray(face).astype(np.float32)[ring].mean(0) / 255
    return cx, cy, r, im.size, rim


def dest(name, side):
    if name == 'default':
        return os.path.join(RES, 'Models', 'coin_heads.png' if side == 'h' else 'coin_tails.png')
    return os.path.join(RES, 'Textures', 'Cosmetics', f'coin_{name}_{side}.png')


# 고른 원본 (a/b)
PICK = {'default_h': 'b', 'default_t': 'a', 'phoenix_h': 'b', 'phoenix_t': 'b', 'sapphire_h': 'b', 'sapphire_t': 'b',
        'gale_h': 'b', 'gale_t': 'a', 'bronze_h': 'a', 'bronze_t': 'a', 'topaz_h': 'a', 'topaz_t': 'a',
        'moon_h': 'b', 'moon_t': 'b', 'obsidian_h': 'b', 'obsidian_t': 'b', 'opal_h': 'a', 'opal_t': 'a'}

if __name__ == '__main__':
    only = sys.argv[1:]
    for key, v in PICK.items():
        name, side = key.split('_')
        if only and key not in only:
            continue
        src = os.path.join(HERE, 'raw', f'{key}_{v}.png')
        cx, cy, r, sz, rim = process(src, dest(name, side))
        print(key, v, f'center ({cx:.0f},{cy:.0f}) r {r:.0f} of {sz}  rim new Color({rim[0]:.2f}f, {rim[1]:.2f}f, {rim[2]:.2f}f)')
