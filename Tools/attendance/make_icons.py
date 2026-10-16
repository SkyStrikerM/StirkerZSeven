"""출석체크 보상 아이콘 (cp_common · cp_uncommon · reward_rare_card) 을 코드로 그린다.
VARCO 연결이 없을 때 쓰는 자체 제작본. 결과: Assets/StrikerZ/Resources/StrikerZ/Textures/
    python Tools/attendance/make_icons.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Textures')
S = 512


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def glow(size, color, radius, alpha=200):
    g = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    c = size // 2
    d.ellipse([c - radius, c - radius, c + radius, c + radius], fill=color + (alpha,))
    return g.filter(ImageFilter.GaussianBlur(radius * 0.45))


def sparkle(d, x, y, r, col):
    d.polygon([(x, y - r), (x + r * 0.22, y - r * 0.22), (x + r, y), (x + r * 0.22, y + r * 0.22),
               (x, y + r), (x - r * 0.22, y + r * 0.22), (x - r, y), (x - r * 0.22, y - r * 0.22)], fill=col)


def crystal(name, light, mid, dark, glow_col):
    """마력 결정: 길쭉한 육각 결정 1개 + 작은 결정 2개, 면마다 명암"""
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    img.alpha_composite(glow(S, glow_col, 190, 150))
    layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    def gem(cx, cy, w, h, tip):
        # 꼭짓점: 위 끝 · 어깨 좌/우 · 허리 좌/우 · 아래 끝
        top = (cx, cy - h / 2 - tip)
        bot = (cx, cy + h / 2 + tip * 0.6)
        lt, rt = (cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2)
        lb, rb = (cx - w / 2, cy + h / 2), (cx + w / 2, cy + h / 2)
        cm_t, cm_b = (cx + w * 0.08, cy - h / 2 + 6), (cx + w * 0.08, cy + h / 2 - 6)
        d.polygon([top, lt, cm_t], fill=light)              # 위 왼면
        d.polygon([top, cm_t, rt], fill=mid)                # 위 오른면
        d.polygon([lt, cm_t, cm_b, lb], fill=lerp(light, mid, 0.35))   # 몸 왼면 (밝음)
        d.polygon([cm_t, rt, rb, cm_b], fill=dark)          # 몸 오른면 (그늘)
        d.polygon([lb, cm_b, bot], fill=lerp(mid, dark, 0.4))
        d.polygon([cm_b, rb, bot], fill=lerp(dark, (0, 0, 0), 0.25))
        edge = lerp(light, (255, 255, 255), 0.6) + (230,)
        d.line([top, lt, lb, bot, rb, rt, top], fill=edge, width=4)
        d.line([top, cm_t, cm_b, bot], fill=edge, width=3)
        # 하이라이트 줄
        d.line([(cx - w * 0.3, cy - h * 0.35), (cx - w * 0.3, cy + h * 0.2)], fill=(255, 255, 255, 170), width=7)

    gem(170, 300, 70, 120, 55)
    gem(352, 316, 64, 104, 48)
    gem(258, 250, 112, 210, 85)
    img.alpha_composite(layer)
    d2 = ImageDraw.Draw(img)
    for (x, y, r) in [(148, 140, 26), (390, 175, 20), (330, 430, 16), (110, 395, 12)]:
        sparkle(d2, x, y, r, (255, 255, 255, 235))
    img.save(os.path.join(OUT, name + '.png'))


def rare_card():
    """랜덤 Rare 카드: 금빛으로 물든 카드 뒷면 + 큰 물음표 + 금빛 후광"""
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    img.alpha_composite(glow(S, (255, 200, 70), 215, 170))
    back = Image.open(os.path.join(OUT, 'card_back.png')).convert('RGBA')
    h = 400
    w = int(back.width * h / back.height)
    back = back.resize((w, h), Image.LANCZOS)
    # 금빛 물들이기 (명도 유지)
    px = back.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            l = (r * 0.3 + g * 0.59 + b * 0.11) / 255
            gold = lerp((165, 110, 25), (255, 242, 180), min(1.0, l * 1.25))
            px[x, y] = lerp((r, g, b), gold, 0.82) + (a,)
    card = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    card.paste(back, ((S - w) // 2, (S - h) // 2), back)
    card = card.rotate(-8, resample=Image.BICUBIC, center=(S // 2, S // 2))
    img.alpha_composite(card)
    d = ImageDraw.Draw(img)
    font = None
    for f in ['C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/arialbd.ttf']:
        if os.path.exists(f):
            font = ImageFont.truetype(f, 230)
            break
    q = '?'
    bbox = d.textbbox((0, 0), q, font=font)
    tx = (S - (bbox[2] - bbox[0])) // 2 - bbox[0]
    ty = (S - (bbox[3] - bbox[1])) // 2 - bbox[1] - 6
    d.text((tx, ty), q, font=font, fill=(255, 250, 225, 255), stroke_width=12, stroke_fill=(120, 60, 0, 255))
    for (x, y, r) in [(120, 120, 30), (400, 150, 22), (395, 405, 26), (130, 400, 16), (256, 70, 14)]:
        sparkle(d, x, y, r, (255, 255, 235, 240))
    # 아래 'R' 띠
    d.rounded_rectangle([196, 436, 316, 486], radius=22, fill=(255, 205, 64, 255), outline=(120, 60, 0, 255), width=5)
    if font:
        f2 = ImageFont.truetype(font.path, 40)
        bb = d.textbbox((0, 0), 'Rare', font=f2)
        d.text((256 - (bb[2] - bb[0]) // 2 - bb[0], 461 - (bb[3] - bb[1]) // 2 - bb[1]), 'Rare', font=f2, fill=(80, 35, 0, 255))
    img.save(os.path.join(OUT, 'reward_rare_card.png'))


if __name__ == '__main__':
    crystal('cp_common', (245, 248, 255), (190, 198, 215), (110, 118, 140), (220, 225, 240))
    crystal('cp_uncommon', (200, 235, 255), (95, 170, 245), (30, 80, 170), (110, 180, 255))
    rare_card()
    print('ok')
