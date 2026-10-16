"""VARCO(nano-banana-pro) 원본(마젠타 배경) → 투명 PNG 512px → Resources/StrikerZ/Textures/
    python Tools/attendance/process.py
고른 원본: PICK 참고 (urls.json 에 해시) — 출석체크 3장 + 세븐 패스 3장
"""
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Textures')
PICK = {'cp_common': 'common_a', 'cp_uncommon': 'unc_a', 'reward_rare_card': 'rare_b',
        'cp_rare': 'rcp_b', 'reward_seven_card': 'seven_a', 'reward_unique_card': 'uniq_a'}
M = np.array([255.0, 0.0, 255.0])


def key(path):
    a = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # 마젠타 정도: R·B 는 높고 G 는 낮을수록 배경. 흰색·금색은 G 가 높아 불투명
    mag = (np.minimum(r, b) - g) / 255.0
    alpha = np.clip(1.0 - (mag - 0.08) / 0.62, 0, 1)
    alpha[alpha < 0.04] = 0
    # 반투명(빛 번짐) 부분은 배경색을 빼서 원래 색을 되살린다
    am = np.maximum(alpha, 1e-3)[..., None]
    fg = (a - (1 - alpha)[..., None] * M) / am
    fg = np.clip(fg, 0, 255)
    rgba = np.dstack([fg, alpha * 255]).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')


if __name__ == '__main__':
    for out, src in PICK.items():
        im = key(os.path.join(HERE, 'raw', src + '.png'))
        im = im.crop(im.getbbox())
        side = max(im.size)
        canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        canvas.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
        canvas.resize((512, 512), Image.LANCZOS).save(os.path.join(OUT, out + '.png'))
        print(out, '<-', src)
