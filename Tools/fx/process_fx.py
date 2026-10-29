# -*- coding: utf-8 -*-
"""VARCO 3D 로 만든 이펙트 이미지(검은 배경) → Resources/StrikerZ/Textures/Fx/<이름>.png
사용: python process_fx.py [이름 ...]   (이름 생략 시 nodes.json 의 전부)
  1) 밝기 → 알파 (검은 배경 투명, 색은 알파로 나눠 복원 = 빛나는 이펙트, process_arrow.py 와 같은 규칙)
  2) 정사각형 그대로 (burst/ring/glyph 등 512, mote/star/shard 256); bolt 는 90° 돌려 가로(LineRenderer 용)
프롬프트는 prompts.json, 노드 id 는 nodes.json (VARCO 3D 플로우 ee5e795a…)."""
import sys, os, json
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/Fx/'
SMALL = ('mote_', 'star', 'shard', 'grain')

def process(name):
    raw = os.path.join(HERE, 'raw', f'{name}.png')
    im = np.asarray(Image.open(raw).convert('RGB')).astype(np.float32) / 255.0
    corners = np.concatenate([im[:20, :20], im[:20, -20:], im[-20:, :20], im[-20:, -20:]]).reshape(-1, 3).mean()
    mx0 = im.max(-1)
    if corners > 0.75 and np.percentile(mx0, 50) < 0.2:
        # 검은 그림 둘레에 흰 액자를 그린 경우 (mote_fire) → 안쪽 검은 네모만 잘라 쓴다
        rows = np.where(np.median(mx0, axis=1) < 0.2)[0]
        cols = np.where(np.median(mx0, axis=0) < 0.2)[0]
        im = im[rows.min() + 4:rows.max() - 3, cols.min() + 4:cols.max() - 3]
        corners = 0.0
    if corners > 0.75:                                   # 흰 배경으로 그린 경우
        inv = 1.0 - im
        a = np.clip((inv.max(-1) - 0.035) / 0.965, 0, 1)
        rgb = 1.0 - inv / np.maximum(a[..., None], 1e-3)
        rgb = rgb * 1.35 + 0.04
    else:
        # 배경이 완전한 검정이 아닐 수 있다 (어두운 회색 네모) → 테두리 밝기의 중앙값을 바닥으로 빼고 알파를 만든다
        mx = im.max(-1)
        border = np.concatenate([mx[:12].ravel(), mx[-12:].ravel(), mx[:, :12].ravel(), mx[:, -12:].ravel()])
        floor = float(np.median(border))
        p50 = float(np.percentile(mx, 50))
        if p50 < 0.3: floor = max(floor, p50)          # 그림이 작아 배경이 대부분이면 (입자) 전체 중앙값도 바닥으로 (검은 테두리 + 회색 네모 대비)
        floor += 0.03
        a = np.clip((mx - floor) / max(1e-3, 1.0 - floor), 0, 1)
        rgb = np.clip(im - floor, 0, 1) / np.maximum(a[..., None], 1e-3) / max(1e-3, 1.0 - floor)
    # 가장자리로 갈수록 알파를 죽여 네모 테두리가 보이지 않게 (바깥 6%)
    h, w = a.shape
    yy, xx = np.mgrid[0:h, 0:w]
    ex = np.minimum(np.minimum(xx, w - 1 - xx) / (w * 0.06), 1.0)
    ey = np.minimum(np.minimum(yy, h - 1 - yy) / (h * 0.06), 1.0)
    a = a * np.clip(ex, 0, 1) * np.clip(ey, 0, 1)
    rgba = np.dstack([np.clip(rgb, 0, 1), a])
    img = Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA')
    if name == 'bolt':
        img = img.rotate(90, expand=True)                 # 위→아래 번개를 왼쪽→오른쪽으로
    size = 256 if name.startswith(SMALL) else 512
    img = img.resize((size, size), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    img.save(OUT + f'{name}.png')
    return img

if __name__ == '__main__':
    names = sys.argv[1:] or list(json.load(open(os.path.join(HERE, 'nodes.json'), encoding='utf-8')).keys())
    done = []
    for n in names:
        if not os.path.exists(os.path.join(HERE, 'raw', f'{n}.png')):
            print('skip (no raw)', n); continue
        process(n); done.append(n)
    print('processed', len(done))
    # 검토용 모아보기 (어두운 바탕)
    if done:
        cell = 160
        cols = 8
        rows = (len(done) + cols - 1) // cols
        sheet = Image.new('RGBA', (cols * cell, rows * cell), (20, 20, 28, 255))
        for i, n in enumerate(done):
            im = Image.open(OUT + f'{n}.png').convert('RGBA').resize((cell - 8, cell - 8), Image.LANCZOS)
            sheet.alpha_composite(im, ((i % cols) * cell + 4, (i // cols) * cell + 4))
        sheet.save(os.path.join(HERE, 'sheet.png'))
        print('sheet', os.path.join(HERE, 'sheet.png'))
