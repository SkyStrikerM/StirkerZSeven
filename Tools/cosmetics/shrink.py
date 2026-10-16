import os
from PIL import Image
D = 'C:/Users/MBC-501-22/Desktop/Card/Tools/cosmetics/src/'
for name, w, q in [('card_back', 420, 72), ('pack_box', 380, 72), ('coin_heads', 256, 75), ('coin_tails', 256, 75), ('dice_faces', 540, 75)]:
    im = Image.open(D + name + '.jpg'); h = round(im.height * w / im.width)
    im.resize((w, h), Image.LANCZOS).save(D + name + '_s.jpg', quality=q)
    print(name, os.path.getsize(D + name + '_s.jpg') // 1024, 'KB')
