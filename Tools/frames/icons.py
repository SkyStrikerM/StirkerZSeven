import numpy as np, json
from PIL import Image, ImageFilter
from collections import deque
D = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/'
info = {}
for n in ('sword', 'heart'):
    im = Image.open(f'raw_{n}.png').convert('RGB')
    a = np.asarray(im).astype(int)
    H, W, _ = a.shape
    bgmask = (a.min(-1) > 225) & (np.ptp(a, -1) < 18)
    seen = np.zeros((H, W), bool)
    q = deque([(0, 0), (0, W - 1), (H - 1, 0), (H - 1, W - 1)])
    for p in q: seen[p] = True
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and not seen[ny, nx] and bgmask[ny, nx]:
                seen[ny, nx] = True; q.append((ny, nx))
    alpha = Image.fromarray(np.where(seen, 0, 255).astype('uint8')).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
    rgba = im.convert('RGBA'); rgba.putalpha(alpha)
    bb = rgba.getbbox(); rgba = rgba.crop(bb)
    # 숫자 자리: 칼 = 남색 메달리온 중심, 하트 = 중앙
    arr = np.asarray(rgba).astype(int)
    if n == 'sword':
        m = (arr[..., 2] > arr[..., 0] + 25) & (arr[..., 0] < 90) & (arr[..., 3] > 200)
    else:
        m = None
    if m is not None and m.sum() > 50:
        ys, xs = np.where(m); cx, cy = xs.mean() / rgba.width, 1 - ys.mean() / rgba.height
        rad = (xs.max() - xs.min()) / rgba.width / 2
    else:
        cx, cy, rad = 0.5, 0.47, 0.3
    info[n] = dict(w=rgba.width, h=rgba.height, cx=round(cx, 3), cy=round(cy, 3), r=round(rad, 3))
    out = rgba.resize((int(rgba.width * 512 / max(rgba.size)), int(rgba.height * 512 / max(rgba.size))), Image.LANCZOS)
    out.save(D + f'icon_{n}.png')
    print(n, info[n], out.size)
json.dump(info, open('icons.json', 'w'))
