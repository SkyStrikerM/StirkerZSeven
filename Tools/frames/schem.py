from PIL import Image, ImageDraw
import base64, io, json
L = {
 'monster': dict(art=(0.101,0.431,0.899,0.875), name=(0.115,0.886,0.884,0.964), type=(0.125,0.367,0.874,0.429), body=(0.14,0.095,0.858,0.341)),
 'magic':   dict(art=(0.077,0.442,0.935,0.899), name=(0.16,0.908,0.84,0.971), type=(0.101,0.368,0.909,0.44), body=(0.117,0.082,0.882,0.342)),
 'hero':    dict(art=(0.086,0.462,0.912,0.878), name=(0.12,0.889,0.88,0.953), type=(0.12,0.372,0.877,0.418), body=(0.127,0.087,0.871,0.344)),
 'field':   dict(art=(0.111,0.44,0.889,0.867), name=(0.125,0.879,0.874,0.962), type=(0.135,0.383,0.864,0.438), body=(0.151,0.121,0.848,0.357)),
}
json.dump(L, open('layouts.json','w'))
H = 400; CW = int(H * 858 / 1200); W = 300; ox = (W - CW) // 2
out = {}
for n, r in L.items():
    im = Image.new('RGB', (W, H), (255, 255, 255))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([ox, 0, ox + CW - 1, H - 1], radius=10, fill=(150, 150, 155))
    def R(k, col):
        x0, y0, x1, y1 = r[k]
        d.rectangle([ox + x0 * CW, (1 - y1) * H, ox + x1 * CW, (1 - y0) * H], fill=col)
    R('art', (0, 0, 0)); R('name', (225, 225, 225)); R('type', (225, 225, 225)); R('body', (60, 60, 64))
    b = io.BytesIO(); im.save(b, 'PNG', optimize=True)
    im.save(f'schem_{n}.png')
    out[n] = base64.b64encode(b.getvalue()).decode()
    print(n, len(out[n]))
json.dump(out, open('schem_b64.json', 'w'))
