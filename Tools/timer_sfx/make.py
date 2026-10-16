"""전투 선택지 5초 이하 경고음 (VARCO Sound 효과음 모드에서 생성한 원본에서 '째깍' 하나만 잘라 냄)
    python Tools/timer_sfx/make.py
  timer_tick      <- raw/tick_1 (1.01s 의 째깍 · 시계 초침 + 마법 차임)  : 5·4·3초
  timer_tick_last <- raw/last_3 (0.56s 의 날카로운 경고 핑)               : 2·1초
"""
import os, wave
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Resources', 'StrikerZ', 'Audio')
CUTS = {'timer_tick': ('tick_1', 1.01, 0.45), 'timer_tick_last': ('last_3', 0.56, 0.48)}

def load(name):
    w = wave.open(os.path.join(HERE, 'raw', name + '.wav'))
    a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768
    return a, w.getframerate()

for out, (src, t, dur) in CUTS.items():
    a, sr = load(src)
    s = max(0, int((t - 0.008) * sr)); e = s + int(dur * sr)
    seg = a[s:e].copy()
    fi = int(0.004 * sr); fo = int(0.08 * sr)
    seg[:fi] *= np.linspace(0, 1, fi); seg[-fo:] *= np.linspace(1, 0, fo)
    seg *= 0.7 / max(1e-6, np.abs(seg).max())          # 최고점 -3 dBFS
    with wave.open(os.path.join(OUT, out + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr)
        w.writeframes((seg * 32767).astype(np.int16).tobytes())
    print(out, '<-', src, t, dur)
