# -*- coding: utf-8 -*-
"""스토리 1권 일러스트 설치.
raw/ 의 VARCO 원본(story_urls.json 으로 받은 것) →
  Resources/StrikerZ/Art/Story/{장면}.png  (16:9, 흰 테두리 자동 제거)
  Resources/StrikerZ/Art/Story/SH0x.png    (스토리 상대 영웅 카드 아트, 4:3)
  Resources/StrikerZ/Art/Story/face_{id}.png (대사 초상화 512)
  Resources/StrikerZ/Art/Cards/SM0x.png    (잿빛 짐승 카드)
  Resources/StrikerZ/Art/Cards/H017(_full).png (카일 영웅 카드)
사용: python install_story.py"""
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw')
RES = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Art/'
STORY = RES + 'Story/'
CARDS = RES + 'Cards/'
os.makedirs(STORY, exist_ok=True)


def load(n):
    return Image.open(os.path.join(RAW, n + '.png')).convert('RGB')


def trim_white(im, thr=232):
    """그림 둘레의 흰(밝은 단색) 테두리 제거"""
    a = np.asarray(im).astype(int)
    bright = a.min(axis=2) > thr
    rows = np.where(bright.mean(axis=1) < 0.9)[0]
    cols = np.where(bright.mean(axis=0) < 0.9)[0]
    if len(rows) == 0 or len(cols) == 0: return im
    pad = 4
    return im.crop((cols[0] + pad, rows[0] + pad, cols[-1] - pad, rows[-1] - pad))


def cover(im, w, h, fx=0.5, fy=0.5):
    """비율 맞춰 잘라내기 (fx, fy = 잘라낼 중심 위치 0~1)"""
    W, H = im.size
    if W / H > w / h:
        nw = int(H * w / h); x = int((W - nw) * fx)
        im = im.crop((x, 0, x + nw, H))
    else:
        nh = int(W * h / w); y = int((H - nh) * fy)
        im = im.crop((0, y, W, y + nh))
    return im.resize((w, h), Image.LANCZOS)


scenes = ['v1_p0', 'v1_1a', 'v1_1b', 'v1_2a', 'v1_2b', 'v1_3a', 'v1_3b', 'v1_4a', 'v1_4b',
          'v1_5a', 'v1_5b', 'v1_6a', 'v1_6b', 'v1_7a', 'v1_7b', 'v1_7c', 'v1_7d']
for n in scenes:
    if not os.path.exists(os.path.join(RAW, n + '.png')): continue
    cover(trim_white(load(n)), 1376, 774).save(STORY + n + '.png')

# 잿빛 짐승 카드 + 스토리 영웅 아트
for n in ['SM01', 'SM02', 'SM03']:
    im = cover(load(n), 1024, 765)
    im.save(CARDS + n + '.png')
    im.save(STORY + n + '.png')
cover(load('SH01'), 1024, 765).save(STORY + 'SH01.png')
cover(trim_white(load('v1_2a')), 1024, 765, fx=0.15).save(STORY + 'SH02.png')   # 베인 교수 (왼쪽)
cover(trim_white(load('v1_3a')), 1024, 765, fx=0.1).save(STORY + 'SH03.png')    # 그레고르 (왼쪽)
cover(trim_white(load('v1_7a')), 1024, 765, fx=0.5).save(STORY + 'SH06.png')    # 잿빛 대록

# 카일 영웅 카드
full = load('H017_full')
cover(full, 750, 1050).save(CARDS + 'H017_full.png')
W, H = full.size
lh = int(W * 765 / 1024); y = int(H * 0.08)
full.crop((0, y, W, y + lh)).resize((1024, 765), Image.LANCZOS).save(CARDS + 'H017.png')

# 대사 초상화
for c in ['kai', 'aria', 'leon', 'bane', 'gregor']:
    p = os.path.join(RAW, 'face_' + c + '.png')
    if os.path.exists(p):
        cover(Image.open(p).convert('RGB'), 512, 512, fy=0.35).save(STORY + 'face_' + c + '.png')
print('installed')
