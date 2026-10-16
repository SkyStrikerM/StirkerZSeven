# -*- coding: utf-8 -*-
"""기본 플레이매트(Textures/playmat.png)에서 룬 문자 · 금색 연성진(기하학 선·칸 선) · 중앙 문장만 뽑아 투명 레이어로
→ Textures/playmat_overlay.png. 게임에서는 필드 배경 위에 항상 겹쳐 그린다 (BoardView)."""
import numpy as np
from PIL import Image, ImageFilter
T = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Textures/'
im = np.asarray(Image.open(T + 'playmat.png').convert('RGB')).astype(np.float32) / 255
H, W, _ = im.shape
r, g, b = im[..., 0], im[..., 1], im[..., 2]
mx, mn = im.max(-1), im.min(-1)
sat, lum = mx - mn, im.mean(-1)
a_gold = np.clip(((r - b) - 0.025) / 0.09, 0, 1) * np.clip((mx - 0.1) / 0.2, 0, 1)          # 금색 선 (대리석은 무채색)
a_rune = np.clip((sat - 0.12) / 0.18, 0, 1) * np.clip((mx - 0.15) / 0.25, 0, 1)             # 색 룬 + 글로우
yy, xx = np.mgrid[0:H, 0:W]
disc = np.clip((150 - np.hypot(xx - W / 2, yy - H / 2)) / 10, 0, 1)
a_emb = np.clip((lum - 0.05) / 0.16, 0, 1) * disc                                           # 중앙 문장
a = np.maximum(np.maximum(a_gold, a_rune), a_emb)
a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32) / 255
a = np.clip(a * 1.15, 0, 1)
rgb = np.clip(im / np.maximum(a[..., None], 0.45) * 1.3, 0, 1)   # 어두운 필드 배경 위에서도 보이게 조금 밝게
Image.fromarray((np.dstack([rgb, a]) * 255).astype(np.uint8), 'RGBA').save(T + 'playmat_overlay.png')
print('overlay', W, H, 'coverage', round(float((a > 0.1).mean()), 3))
