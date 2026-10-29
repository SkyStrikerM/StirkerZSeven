# -*- coding: utf-8 -*-
"""컴퓨터에 설치된 한글 폰트 몇 개의 글리프를 섞어 새 TTF 를 만든다 → Resources/StrikerZ/Fonts/StrikerZMix.ttf
바탕 = 맑은 고딕(malgun.ttf, 라틴·숫자·기호·자모는 그대로). 한글 음절(AC00~D7A3)만 초성 번호에 따라 돌아가며 다른 폰트에서 가져온다:
  초성 index % 4 → 0 맑은 고딕 · 1 Noto Sans KR · 2 돋움 · 3 굴림  (같은 초성끼리는 같은 폰트라 글줄이 규칙적으로 섞인다)
글리프 외곽선은 UPM 비율로 맞추고 자간(advance)도 같이 맞춘다.
사용: python mix_font.py [preview]
"""
import os, sys, glob
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.recordingPen import DecomposingRecordingPen

WIN = r'C:\Windows\Fonts'
OUT = r'C:\Users\MBC-501-22\Desktop\Card\Assets\StrikerZ\Resources\StrikerZ\Fonts\StrikerZMix.ttf'
FAMILY = 'StrikerZ Mix'

def ttc_index(path, want):
    """TTC 안에서 이름이 want 인 폰트 번호"""
    for i in range(8):
        try:
            t = TTFont(path, fontNumber=i, lazy=True)
        except Exception:
            break
        n = t['name'].getDebugName(1) or ''
        if n.lower() == want.lower():
            return i
    raise SystemExit(f'{want} not in {path}')

def main():
    base = TTFont(os.path.join(WIN, 'malgun.ttf'))
    from fontTools.varLib.instancer import instantiateVariableFont
    noto = instantiateVariableFont(TTFont(os.path.join(WIN, 'NotoSansKR-VF.ttf')), {'wght': 400})   # 가변 폰트 기본값이 Thin(100)이라 Regular 로 고정
    dotum = TTFont(os.path.join(WIN, 'gulim.ttc'), fontNumber=ttc_index(os.path.join(WIN, 'gulim.ttc'), 'Dotum'))
    gulim = TTFont(os.path.join(WIN, 'gulim.ttc'), fontNumber=ttc_index(os.path.join(WIN, 'gulim.ttc'), 'Gulim'))
    sources = [None, noto, dotum, gulim]        # None = 바탕 폰트 그대로
    names = ['Malgun Gothic', 'Noto Sans KR', 'Dotum', 'Gulim']
    if 'fvar' in noto:
        print('Noto axes:', [(a.axisTag, a.defaultValue) for a in noto['fvar'].axes])

    bcmap = base.getBestCmap()
    glyf = base['glyf']; hmtx = base['hmtx']
    bupm = base['head'].unitsPerEm
    gsets = {}
    for f in sources:
        if f is not None:
            gsets[id(f)] = (f.getGlyphSet(), f.getBestCmap(), f['hmtx'], bupm / f['head'].unitsPerEm)

    replaced = [0, 0, 0, 0]
    for code in range(0xAC00, 0xD7A4):
        if code not in bcmap: continue
        lead = (code - 0xAC00) // 588
        k = lead % len(sources)
        src = sources[k]
        if src is None: replaced[k] += 1; continue
        gs, cmap, shm, sc = gsets[id(src)]
        sg = cmap.get(code)
        if sg is None or sg not in gs: continue
        rec = DecomposingRecordingPen(gs)          # 복합 글리프(컴포넌트)는 풀어서 윤곽선으로
        gs[sg].draw(rec)
        pen = TTGlyphPen(None)
        rec.replay(TransformPen(pen, (sc, 0, 0, sc, 0, 0)))
        g = pen.glyph()
        name = bcmap[code]
        glyf[name] = g
        adv, lsb = shm[sg]
        g.recalcBounds(glyf)
        hmtx[name] = (int(round(adv * sc)), g.xMin if hasattr(g, 'xMin') else int(round(lsb * sc)))
        replaced[k] += 1
    print('replaced per source:', dict(zip(names, replaced)))

    # 이름: 맑은 고딕과 충돌하지 않게
    name = base['name']
    for rec in name.names:
        if rec.nameID in (1, 3, 4, 6, 16):
            txt = rec.toUnicode()
            if rec.nameID == 6: rec.string = 'StrikerZMix-Regular'
            elif rec.nameID == 3: rec.string = 'StrikerZMix;mixed-from-installed-fonts'
            else: rec.string = FAMILY
    base['OS/2'].fsType = 0
    if 'DSIG' in base: del base['DSIG']
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    base.save(OUT)
    print('saved', OUT, os.path.getsize(OUT) // 1024, 'KB')

def preview():
    from PIL import Image, ImageDraw, ImageFont
    lines = ['스트라이커즈 세븐 — 믹스 폰트 미리보기', '가나다라마바사 아자차카타파하', '대장장이 거한 · 빛의 견습사제 · 개기일식',
             '배틀 시작 — 모든 카드 공개 · 공격 턴 · 수비 턴', '맑은 고딕 + Noto Sans KR + 돋움 + 굴림 (초성별 순환)',
             'ABC abc 0123456789 !?()[]…·']
    img = Image.new('RGB', (1500, 60 + 76 * len(lines)), (18, 18, 26))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(OUT, 44)
    for i, s in enumerate(lines):
        d.text((40, 30 + 76 * i), s, font=f, fill=(240, 236, 220))
    p = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'preview.png')
    img.save(p); print('preview', p)

if __name__ == '__main__':
    if 'preview' in sys.argv: preview()
    else: main(); preview()
