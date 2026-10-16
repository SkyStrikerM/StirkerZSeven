"""VARCO(nano-banana-pro) 메뉴 아이콘 원본(마젠타 배경) → 투명 PNG 256px → Resources/StrikerZ/Textures/nav_*.png (+ cp_unique 512px)
    python Tools/nav/process.py
"""
import os, sys
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'attendance'))
from process import OUT
import numpy as np


def key(path):
    # 마젠타 배경 판정: R·B 가 비슷하게 높고 G 가 낮을 때 (밝기 무관 → 어두운 그라데이션 배경도). 보라 에나멜(R≪B)·금색(G 높음)은 남는다
    a = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = np.maximum(np.maximum(r, b), 1)
    bal = np.minimum(r, b) / m
    gr = g / m
    w = np.clip((bal - 0.68) / 0.17, 0, 1) * np.clip((0.42 - gr) / 0.17, 0, 1) * np.clip((m - 50) / 40, 0, 1)
    alpha = 1 - w
    alpha[alpha < 0.05] = 0
    spill = np.clip(np.minimum(r, b) - g, 0, None) * w
    out = a.copy()
    out[..., 0] -= spill
    out[..., 2] -= spill
    rgba = np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')

PICK = {'nav_home': 'home_b', 'nav_story': 'story_b', 'nav_raid': 'raid_a', 'nav_challenge': 'challenge_b',
        'nav_deck': 'deck_a', 'nav_shop': 'shop_b', 'nav_quest': 'quest_b', 'nav_profile': 'profile_b',
        'nav_settings': 'settings_a', 'nav_title': 'title_a', 'nav_quit': 'quit_a', 'cp_unique': 'cpu_a', 'nav_collection': 'collection_a', 'tab_quest': 'trophy_a', 'tab_pass': 'pass_b', 'tab_attend': 'attend_a', 'btl_hero': 'hero_a', 'btl_field': 'field_a', 'btl_ready': 'ready_b', 'btl_speed': 'speed_a', 'btl_stamp': 'stamp_b', 'btl_scout': 'scout_b'}

if __name__ == '__main__':
    only = sys.argv[1:]
    for out, src in PICK.items():
        p = os.path.join(HERE, 'raw', src + '.png')
        if (only and out not in only) or not os.path.exists(p):
            continue
        im = key(p)
        im = im.crop(im.getbbox())
        side = max(im.size)
        canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        canvas.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
        size = 512 if out.startswith('cp_') else 256
        canvas.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, out + '.png'))
        print(out, '<-', src)
