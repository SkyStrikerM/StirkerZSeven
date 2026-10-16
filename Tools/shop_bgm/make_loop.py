"""VARCO Sound 상점 BGM → 루프 ogg (앞 무음 제거, 끝 아웃트로 잘라내고 4초 교차 페이드로 이음새 없이)"""
import subprocess, numpy as np, sys
SR = 44100
OUT = '../../Assets/StrikerZ/Resources/StrikerZ/Audio/'
def load(p):
    raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', p, '-f', 'f32le', '-ac', '2', '-ar', str(SR), '-'], capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).reshape(-1, 2)
for src, name in [('shop_1.mp3', 'bgm_shop'), ('shop_2.mp3', 'bgm_shop2')]:
    x = load(src)
    amp = np.abs(x).max(1)
    start = int(np.argmax(amp > 0.01))
    x = x[start:]
    dur = len(x) / SR
    L = int(min(dur - 12, 160) * SR)       # 마지막 아웃트로(페이드아웃)는 버린다
    F = 4 * SR
    out = x[:L].copy()
    t = np.linspace(0, 1, F, dtype=np.float32)[:, None]
    out[:F] = x[:F] * np.sqrt(t) + x[L:L + F] * np.sqrt(1 - t)   # 끝 → 처음으로 넘어갈 때 x[L] 로 자연스럽게 이어진다
    tmp = name + '.f32'
    out.astype(np.float32).tofile(tmp)
    # 음량: 측정한 만큼만 볼륨을 맞춘다 (loudnorm 은 재샘플링 때문에 ogg 길이가 어긋나 Unity 가 truncated 경고)
    m = subprocess.run(['ffmpeg', '-hide_banner', '-f', 'f32le', '-ar', str(SR), '-ac', '2', '-i', tmp, '-af', 'ebur128', '-f', 'null', '-'], capture_output=True, text=True).stderr
    I = float(m.split('Integrated loudness:')[1].split('I:')[1].split('LUFS')[0])
    gain = -17 - I
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'f32le', '-ar', str(SR), '-ac', '2', '-i', tmp, '-af', f'volume={gain:.2f}dB,alimiter=limit=0.85:level=false', '-c:a', 'libvorbis', '-q:a', '4', OUT + name + '.ogg'], check=True)
    print(name, f'loudness {I:.1f} → gain {gain:+.1f} dB')
    import os; os.remove(tmp)
    print(name, f'{dur:.1f}s → {L / SR:.1f}s loop, start trim {start / SR:.2f}s')
