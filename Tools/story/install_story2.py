# -*- coding: utf-8 -*-
"""스토리 제2~10장 일러스트 설치 (install_story.py 의 후속).
1) fetch: nodes_job_*.json 의 GenerateImage 결과를 raw/{key}.png 로 받는다 (VARCO 그래프 저장본에서 URL 조회)
     python install_story2.py fetch <graph.txt>
2) install: raw/ → Resources/StrikerZ/Art/Story · Art/Cards
     python install_story2.py install"""
import os, sys, json, glob, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
RES = "C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Resources/StrikerZ/Art/"
STORY = RES + "Story/"
CARDS = RES + "Cards/"


def trim_white(im, thr=232):
    """그림 둘레의 흰(밝은 단색) 테두리 제거 (install_story.py 와 같음)"""
    a = np.asarray(im).astype(int)
    bright = a.min(axis=2) > thr
    rows = np.where(bright.mean(axis=1) < 0.9)[0]
    cols = np.where(bright.mean(axis=0) < 0.9)[0]
    if len(rows) == 0 or len(cols) == 0: return im
    pad = 4
    return im.crop((cols[0] + pad, rows[0] + pad, cols[-1] - pad, rows[-1] - pad))


def cover(im, w, h, fx=0.5, fy=0.5):
    W, H = im.size
    if W / H > w / h:
        nw = int(H * w / h); x = int((W - nw) * fx)
        im = im.crop((x, 0, x + nw, H))
    else:
        nh = int(W * h / w); y = int((H - nh) * fy)
        im = im.crop((0, y, W, y + nh))
    return im.resize((w, h), Image.LANCZOS)

SCENES = [f"v{c}_{s}" for c in range(2, 11) for s in range(1, 8)] + ["v10_e"]
FACES = ["ignis", "meril", "terra", "zephyr", "volt", "lumien", "erebos", "varus", "prisma"]
HEROES = [f"H0{n}" for n in range(18, 28)]
MINIONS = ["SM04", "SM05", "SM06"]
# 하얀 배경(순백의 세계) 장면은 흰 테두리 자르기를 하면 인물까지 잘려 나간다
NO_TRIM = {"v8_1", "v8_2", "v8_3", "v8_4", "v8_5", "v8_6", "v10_1", "v10_2", "v10_e"}
# 스토리 전용 상대 영웅 아트: (원본, 가로 중심 fx)
# 스토리 전용 상대 영웅 아트: (원본, 중심 x, 중심 y, 확대) — 장면 CG 에서 상대가 있는 곳을 잘라냄
SH = {
    "SH07": ("SM04", 0.5, 0.5, 1.0), "SH08": ("v2_4", 0.5, 0.4, 1.3), "SH09": ("v2_7", 0.85, 0.35, 1.6),
    "SH10": ("v3_1", 0.42, 0.55, 1.9), "SH11": ("v3_3", 0.08, 0.5, 1.4), "SH12": ("v4_2", 0.75, 0.2, 1.7),
    "SH13": ("v4_6", 0.45, 0.4, 1.4), "SH14": ("v4_7", 0.15, 0.5, 1.6), "SH15": ("v5_1", 0.12, 0.55, 1.6),
    "SH16": ("v5_3", 0.2, 0.65, 1.6), "SH17": ("v7_6", 0.35, 0.3, 1.4), "SH18": ("v6_3", 0.85, 0.4, 1.5),
    "SH20": ("v10_4", 0.2, 0.45, 1.5), "SH22": ("v8_4", 0.75, 0.5, 1.5), "SH24": ("v7_5", 0.5, 0.35, 1.8),
    "SH25": ("v10_3", 0.25, 0.45, 1.5), "SH26": ("v2_2", 1.0, 0.7, 2.0),
}


def zoom_crop(im, cx, cy, z):
    """(cx, cy) 중심으로 1/z 크기 영역을 잘라낸다 (가장자리에 맞춰 밀어 넣음)"""
    W, H = im.size
    cw, ch = int(W / z), int(H / z)
    x = min(max(0, int(cx * W - cw / 2)), W - cw)
    y = min(max(0, int(cy * H - ch / 2)), H - ch)
    return im.crop((x, y, x + cw, y + ch))


def fetch(graph_path):
    g = json.load(open(graph_path, encoding="utf-8"))["graph"]
    out = {n["id"]: [o["value"]["url"] for o in (n.get("output") or []) if (o.get("value") or {}).get("url")] for n in g["nodes"]}
    jobs = {}
    for p in glob.glob(os.path.join(HERE, "nodes_job_*.json")):
        if p.endswith("_3d.json"): continue
        jobs.update(json.load(open(p, encoding="utf-8")))
    urls = json.load(open(os.path.join(HERE, "story_urls.json"), encoding="utf-8"))
    missing = []
    for key, nid in jobs.items():
        u = out.get(nid)
        if not u: missing.append(key); continue
        url = "https://3d.varco.ai" + u[0]
        urls[key] = url
        dst = os.path.join(RAW, key + ".png")
        if not os.path.exists(dst):
            urllib.request.urlretrieve(url, dst)
    json.dump(urls, open(os.path.join(HERE, "story_urls.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    print("fetched", len(jobs) - len(missing), "missing", missing)


def load(n):
    return Image.open(os.path.join(RAW, n + ".png")).convert("RGB")


def has(n):
    return os.path.exists(os.path.join(RAW, n + ".png"))


def install():
    done = []
    for n in SCENES:
        if has(n):
            im = load(n) if n in NO_TRIM else trim_white(load(n))
            cover(im, 1376, 774).save(STORY + n + ".png"); done.append(n)
    for c in FACES:
        if has("face_" + c):
            cover(load("face_" + c), 512, 512, fy=0.35).save(STORY + "face_" + c + ".png"); done.append("face_" + c)
    for h in HEROES:
        if not has(h + "_full"): continue
        full = trim_white(load(h + "_full"))
        cover(full, 750, 1050).save(CARDS + h + "_full.png")
        W, H = full.size
        lh = int(W * 765 / 1024); y = int(H * 0.06)
        full.crop((0, y, W, min(H, y + lh))).resize((1024, 765), Image.LANCZOS).save(CARDS + h + ".png")
        done.append(h)
    for m in MINIONS:
        if has(m):
            im = cover(trim_white(load(m)), 1024, 765)
            im.save(CARDS + m + ".png"); im.save(STORY + m + ".png"); done.append(m)
    for sh, (src, cx, cy, z) in SH.items():
        if has(src):
            im = load(src) if src in NO_TRIM else trim_white(load(src))
            cover(zoom_crop(im, cx, cy, z), 1024, 765).save(STORY + sh + ".png"); done.append(sh)
    print("installed", len(done))


if __name__ == "__main__":
    if sys.argv[1] == "fetch": fetch(sys.argv[2])
    else: install()
