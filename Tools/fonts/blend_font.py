# -*- coding: utf-8 -*-
"""설치된 폰트 여러 개의 '모양 자체'를 평균 내어 하나의 새 폰트를 만든다 → Resources/StrikerZ/Fonts/StrikerZBlend.ttf
방법: 글자마다 각 폰트로 같은 크기·기준선으로 래스터(그림)를 그리고, 그 그림들을 평균 낸 뒤 50% 로 잘라(= 여러 폰트가 겹치는 '중간 모양')
      potrace 로 다시 벡터 윤곽선을 뽑아 TrueType 글리프로 넣는다. 그래서 글자 하나하나가 네 폰트의 특색이 섞인 새 모양이 된다.
  한글 음절(AC00~D7A3): 맑은 고딕 + Noto Sans KR(400) + 돋움 + 굴림
  숫자(0~9):            위 넷 + Segoe UI + Calibri + Verdana (있는 것만)
  그 밖(라틴·기호·자모)은 맑은 고딕 그대로.
사용: python blend_font.py            전체 빌드 (몇 분)
      python blend_font.py sample     표본 글자만 만들어 preview_blend.png 로 확인
"""
import os, sys, time
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import potrace
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.cu2quPen import Cu2QuPen
from fontTools.pens.transformPen import TransformPen
from fontTools.varLib.instancer import instantiateVariableFont

HERE = os.path.dirname(os.path.abspath(__file__))
WIN = r'C:\Windows\Fonts'
BUILD = os.path.join(HERE, 'build')
OUT = r'C:\Users\MBC-501-22\Desktop\Card\Assets\StrikerZ\Resources\StrikerZ\Fonts\StrikerZBlend.ttf'
FAMILY = 'StrikerZ Blend'
S = 320                       # 1em 픽셀 (래스터 해상도)
W, H = int(S * 1.4), int(S * 1.5)
X0, Y0 = int(S * 0.2), int(S * 1.15)   # 글자 원점(왼쪽 기준선) 픽셀 좌표

def noto_static():
    os.makedirs(BUILD, exist_ok=True)
    p = os.path.join(BUILD, 'noto400.ttf')
    if not os.path.exists(p):
        instantiateVariableFont(TTFont(os.path.join(WIN, 'NotoSansKR-VF.ttf')), {'wght': 400}).save(p)
    return p

def ttc_index(path, want):
    for i in range(8):
        try: t = TTFont(path, fontNumber=i, lazy=True)
        except Exception: break
        if (t['name'].getDebugName(1) or '').lower() == want.lower(): return i
    raise SystemExit(f'{want} not in {path}')

class Src:
    """한 소스 폰트: 경로·인덱스·세로 보정 배율 (한글 상자 높이를 바탕 폰트에 맞춘다)"""
    def __init__(self, path, index=0):
        self.path, self.index = path, index
        self.cache = {}
        self.scale = 1.0
    def font(self, px):
        px = int(round(px))
        if px not in self.cache: self.cache[px] = ImageFont.truetype(self.path, px, index=self.index)
        return self.cache[px]
    def hangul_height(self):
        f = self.font(S); tops = []; bots = []
        for i in range(0, 11172, 131):
            b = f.getbbox(chr(0xAC00 + i), anchor='ls')
            if b: tops.append(b[1]); bots.append(b[3])
        return (sum(bots) - sum(tops)) / len(tops)
    def render(self, ch):
        img = Image.new('L', (W, H), 0)
        f = self.font(S * self.scale)
        ImageDraw.Draw(img).text((X0, Y0), ch, font=f, fill=255, anchor='ls')
        return np.asarray(img, dtype=np.float32) / 255.0
    def advance(self, ch):
        return self.font(S * self.scale).getlength(ch) / S   # em 단위

def trace_to_glyph(mask, upm):
    """0/1 마스크 → potrace → TrueType 글리프 (픽셀 → 폰트 단위, y 뒤집기)"""
    bm = potrace.Bitmap(~mask)      # potracer 는 0 을 검정(글자)으로 본다
    path = bm.trace(turdsize=14, alphamax=1.0, opticurve=True, opttolerance=0.45)
    k = upm / S
    tt = TTGlyphPen(None)
    pen = TransformPen(Cu2QuPen(tt, max_err=upm / 500.0), (k, 0, 0, -k, -X0 * k, Y0 * k))
    for curve in path:
        sp = curve.start_point
        pen.moveTo((sp.x, sp.y))
        for seg in curve.segments:
            if seg.is_corner:
                pen.lineTo((seg.c.x, seg.c.y)); pen.lineTo((seg.end_point.x, seg.end_point.y))
            else:
                pen.curveTo((seg.c1.x, seg.c1.y), (seg.c2.x, seg.c2.y), (seg.end_point.x, seg.end_point.y))
        pen.closePath()
    return tt.glyph()

def align_to(base, m, max_shift):
    """m 을 base 에 가장 잘 겹치도록 평행이동 (FFT 상호상관, ±max_shift 픽셀)"""
    from scipy.signal import fftconvolve
    b = base[::2, ::2].astype(np.float32); mm = m[::2, ::2].astype(np.float32)
    corr = fftconvolve(b, mm[::-1, ::-1], mode='same')
    cy, cx = np.array(corr.shape) // 2
    r = max(1, max_shift // 2)
    win = corr[cy - r:cy + r + 1, cx - r:cx + r + 1]
    dy, dx = np.unravel_index(np.argmax(win), win.shape)
    dy = (dy - r) * 2; dx = (dx - r) * 2
    out = np.zeros_like(m)
    H_, W_ = m.shape
    ys = slice(max(0, dy), min(H_, H_ + dy)); yd = slice(max(0, -dy), min(H_, H_ - dy))
    xs = slice(max(0, dx), min(W_, W_ + dx)); xd = slice(max(0, -dx), min(W_, W_ - dx))
    out[ys, xs] = m[yd, xd]
    return out

def blend_mask(srcs, ch, weights=None, thresh=0.35, blur=2.2):
    """첫 폰트(바탕)에 나머지 폰트의 글자를 평행이동으로 맞춘 뒤 가중 평균 → 흐림 → thresh 로 자른다.
    바탕 가중치(0.4) 가 thresh 보다 크므로 바탕의 획은 절대 사라지지 않고, 다른 폰트는 획 굵기·곡선·끝모양을 바꾸거나
    둘 이상이 동의하는 곳에 새 획 모양을 더한다 (단순 평균은 위치가 다른 가는 획을 통째로 지워 글자가 깨졌다)."""
    from PIL import ImageFilter
    ws = weights if weights is not None else [1.0] * len(srcs)
    imgs = []
    for s in srcs:
        img = s.render(ch)
        imgs.append(np.asarray(Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.0)), dtype=np.float32) / 255.0)
    base = imgs[0] >= 0.5
    acc = imgs[0] * ws[0]
    for i in range(1, len(imgs)):
        m = imgs[i] >= 0.5
        if base.any() and m.any():
            aligned = align_to(base, imgs[i], int(S * 0.08))
        else:
            aligned = imgs[i]
        acc = acc + aligned * ws[i]
    acc /= sum(ws)
    if blur > 0:
        acc = np.asarray(Image.fromarray((np.clip(acc, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur)), dtype=np.float32) / 255.0
    m = acc >= thresh
    # 획 가장자리의 잔 톱니·패임을 정리: 작은 원판으로 닫기(틈 메움) → 열기(돌기 제거). 작은 글씨에서 '깨져' 보이던 원인
    from scipy import ndimage
    r = max(2, int(round(S * 0.012)))
    yy, xx = np.ogrid[-r:r + 1, -r:r + 1]
    disk = (xx * xx + yy * yy) <= r * r
    m = ndimage.binary_closing(m, structure=disk)
    m = ndimage.binary_opening(m, structure=disk)
    return m
def main(sample_only=False):
    t0 = time.time()
    malgun = Src(os.path.join(WIN, 'malgun.ttf'))
    noto = Src(noto_static())
    dotum = Src(os.path.join(WIN, 'gulim.ttc'), ttc_index(os.path.join(WIN, 'gulim.ttc'), 'Dotum'))
    gulim = Src(os.path.join(WIN, 'gulim.ttc'), ttc_index(os.path.join(WIN, 'gulim.ttc'), 'Gulim'))
    hangul = [malgun, noto, dotum, gulim]
    HANGUL_W = [0.4, 0.3, 0.15, 0.15]     # 바탕(맑은 고딕) 0.4 > thresh 0.35: 바탕 획은 늘 남고, 다른 폰트는 굵기·곡선을 바꾼다
    h0 = malgun.hangul_height()
    for s in hangul[1:]:
        s.scale = h0 / s.hangul_height()
    print('hangul scale:', [round(s.scale, 3) for s in hangul])
    digits = list(hangul)
    for fn in ['segoeui.ttf', 'calibri.ttf', 'verdana.ttf']:
        p = os.path.join(WIN, fn)
        if os.path.exists(p): digits.append(Src(p))
    # 숫자는 높이(0 의 상자)를 맑은 고딕에 맞춘다
    def digit_h(s):
        b = s.font(S).getbbox('0', anchor='ls'); return b[3] - b[1]
    d0 = digit_h(malgun)
    dscale = [1.0] + [d0 / digit_h(s) for s in digits[1:]]
    print('digit fonts:', len(digits), 'scales', [round(x, 3) for x in dscale])

    base = TTFont(os.path.join(WIN, 'malgun.ttf'))
    cmap = base.getBestCmap(); glyf = base['glyf']; hmtx = base['hmtx']; upm = base['head'].unitsPerEm

    codes = list(range(0xAC00, 0xD7A4))
    if sample_only:
        codes = [ord(c) for c in '스트라이커즈세븐가나다라마바사아자차카타파하대장장이거한빛의견습사제개기일식배틀시작공격턴수비양쪽모두생존엔시아나마도기갑병용암석타이탄약화회복드로우']
    n = 0
    for code in codes:
        if code not in cmap: continue
        ch = chr(code)
        mask = blend_mask(hangul, ch, HANGUL_W)
        g = trace_to_glyph(mask, upm)
        name = cmap[code]
        glyf[name] = g
        g.recalcBounds(glyf)
        adv = int(round(sum(s.advance(ch) for s in hangul) / len(hangul) * upm))
        hmtx[name] = (adv, getattr(g, 'xMin', 0))
        n += 1
        if n % 500 == 0: print(f'  {n} glyphs, {time.time() - t0:.0f}s')
    # 숫자
    for ch in '0123456789':
        code = ord(ch)
        if code not in cmap: continue
        saved = [s.scale for s in digits]
        for s, sc in zip(digits, dscale): s.scale = sc
        mask = blend_mask(digits, ch)
        g = trace_to_glyph(mask, upm)
        name = cmap[code]
        glyf[name] = g; g.recalcBounds(glyf)
        adv = int(round(sum(s.advance(ch) for s in digits) / len(digits) * upm))
        hmtx[name] = (adv, getattr(g, 'xMin', 0))
        for s, sc in zip(digits, saved): s.scale = sc
    print(f'blended {n} hangul + 10 digits in {time.time() - t0:.0f}s')

    nm = base['name']
    for rec in nm.names:
        if rec.nameID == 6: rec.string = 'StrikerZBlend-Regular'
        elif rec.nameID == 3: rec.string = 'StrikerZBlend;blend-of-installed-fonts'
        elif rec.nameID in (1, 4, 16): rec.string = FAMILY
    # Unity 는 TTF 에셋의 줄 높이를 hhea ascent-descent 로 잡는다 (맑은 고딕 원본은 2229+495 = 1.33em 이라 54px 'TIME' 이 64px 칸에서 잘림).
    # OS 폰트 경로(1em)와 같게 typo 메트릭으로 맞춘다
    base['hhea'].ascent, base['hhea'].descent, base['hhea'].lineGap = 1638, -410, 0
    base['OS/2'].usWinAscent, base['OS/2'].usWinDescent = 1638, 410
    base['OS/2'].fsType = 0
    if 'DSIG' in base: del base['DSIG']
    out = os.path.join(BUILD, 'sample.ttf') if sample_only else OUT
    os.makedirs(os.path.dirname(out), exist_ok=True)
    base.save(out)
    print('saved', out, os.path.getsize(out) // 1024, 'KB')
    preview(out)

def preview(path):
    lines = ['스트라이커즈 세븐 — 합성 폰트 미리보기 0123456789', '가나다라마바사 아자차카타파하', '양쪽 모두 생존 — 엔시아나 · 마도 기갑병 · 용암석 타이탄', '대장장이 거한 · 빛의 견습사제 · 개기일식',
             '배틀 시작 — 공격 턴 · 수비 턴  HP 36 / 36  ATK 7', 'ABC abc !?()[]…·']
    img = Image.new('RGB', (1500, 60 + 76 * len(lines)), (18, 18, 26))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(path, 44)
    for i, s in enumerate(lines): d.text((40, 30 + 76 * i), s, font=f, fill=(240, 236, 220))
    p = os.path.join(HERE, 'preview_blend.png'); img.save(p); print('preview', p)

if __name__ == '__main__':
    main(sample_only='sample' in sys.argv)
