"""영웅 3D 에셋 받기: python fetch_hero.py <heroId> <rig.fbx url> <idle.glb url> <attack.glb url> <hit.glb url>
→ Assets/StrikerZ/Heroes/<id>/ model.fbx · albedo.png · idle.json · attack.json · hit.json"""
import sys, os, urllib.request, subprocess, shutil

hid, rig, idle, atk, hit = sys.argv[1:6]
S = os.path.dirname(os.path.abspath(__file__))
tmp = os.path.join(S, 'raw', hid)
os.makedirs(tmp, exist_ok=True)
dst = f'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Heroes/{hid}'
os.makedirs(dst, exist_ok=True)


def get(url, path):
    if not url.startswith('http'):
        url = 'https://3d.varco.ai' + url
    for _ in range(4):
        try:
            data = urllib.request.urlopen(url, timeout=240).read()
            open(path, 'wb').write(data)
            return len(data)
        except Exception as e:
            err = e
    raise err


print('rig', get(rig, os.path.join(tmp, 'model.fbx')) // 1024, 'KB')
shutil.copy(os.path.join(tmp, 'model.fbx'), os.path.join(dst, 'model.fbx'))
for name, url in (('idle', idle), ('attack', atk), ('hit', hit)):
    g = os.path.join(tmp, name + '.glb')
    print(name, get(url, g) // 1024, 'KB')
    args = [sys.executable, os.path.join(S, 'glb2anim.py'), g, os.path.join(dst, name + '.json')]
    if name == 'idle':
        args.append(os.path.join(dst, 'albedo.png'))
    subprocess.run(args, check=True)
print('done', dst)
