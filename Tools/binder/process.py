"""도감 클리어파일 바인더 그림 (VARCO nano-banana-pro)
    python Tools/binder/process.py
  binder_sss.png  : 닫힌 바인더 표지 (마젠타 키잉, 세로 768px)   <- raw/cover_a.png
  binder_open.png : 펼친 바인더 배경 (그대로 복사)               <- raw/open_b.png
  펼친 바인더 속지 위치 (1376x768 기준): 왼쪽 x175~655, 오른쪽 x725~1210, y68~705 → MainMenu.Collection.cs 의 PageL/PageR
"""
import os, sys, shutil
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
import importlib.util
_spec = importlib.util.spec_from_file_location('navproc', os.path.join(HERE, '..', 'nav', 'process.py'))
_nav = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_nav)
key, OUT = _nav.key, _nav.OUT

if __name__ == '__main__':
    im = key(os.path.join(HERE, 'raw', 'cover_a.png'))
    im = im.crop(im.getbbox())
    h = 768
    im.resize((round(im.width * h / im.height), h), Image.LANCZOS).save(os.path.join(OUT, 'binder_sss.png'))
    shutil.copy(os.path.join(HERE, 'raw', 'open_b.png'), os.path.join(OUT, 'binder_open.png'))
    print('ok')
