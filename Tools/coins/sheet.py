import sys, os
from PIL import Image, ImageDraw
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from process import coin_mask, circle_of
names = sys.argv[2:]
W = 220
sheet = Image.new('RGB', (W * 4, W * ((len(names) + 3) // 4)), (30, 30, 30))
for k, n in enumerate(names):
    im = Image.open(f'raw/{n}.png').convert('RGB')
    a = np.asarray(im).astype(np.float32)
    cx, cy, r = circle_of(coin_mask(a))
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(0, 255, 255), width=4)
    d.line([(cx, cy - r), (cx, cy + r)], fill=(255, 0, 0), width=2); d.line([(cx - r, cy), (cx + r, cy)], fill=(255, 0, 0), width=2)
    t = im.resize((W - 6, W - 6)); ImageDraw.Draw(t).text((4, 4), n, fill=(255, 255, 255))
    sheet.paste(t, ((k % 4) * W, (k // 4) * W))
sheet.save(sys.argv[1], quality=88)
