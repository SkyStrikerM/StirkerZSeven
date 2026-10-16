# -*- coding: utf-8 -*-
"""필드 테마 프롬프트 (VARCO nano-banana-pro). python field_prompts.py → field_prompts.json
playmat: schem_playmat.png 를 sourceImage 로 EditImage(16:9) · env: GenerateImage(16:9)"""
import json, os

# id: (이름, 플레이매트 테마, 선 색, 문장, 환경(위에서 본 장면), 환경 가운데)
F = {
 'F001': ('작열의 화산', 'an active volcano landscape with rivers of glowing lava, black obsidian rock and drifting ash', 'molten orange-gold', 'flaming volcano', 'an active volcanic land with glowing lava rivers, black obsidian rocks, craters, smoke and embers', 'a dark cooled lava field'),
 'F002': ('업화의 폭주', 'a raging inferno, a sea of roaring hellfire flames and burning embers over scorched ground', 'blazing crimson-orange', 'roaring flame', 'a raging sea of hellfire with roaring flames, burning embers and scorched cracked ground', 'a scorched dark crater'),
 'F003': ('불꽃의 결계', 'a protective barrier of runic fire, a glowing golden-orange flame dome over cracked magma stone', 'warm amber fire-rune', 'fire rune seal', 'a circular ring of protective fire runes and flame pillars on cracked glowing magma stone', 'a large stone rune circle'),
 'F004': ('심해의 신전', 'a sunken underwater temple with ancient pillars, coral and glowing deep-sea light rays', 'aquamarine', 'trident and seashell', 'the deep sea floor with a sunken ancient temple, broken pillars, coral reefs, kelp and glowing light rays from above', 'a calm dark temple courtyard under water'),
 'F005': ('해일의 폭주', 'a stormy raging ocean with towering tidal waves and white foam', 'bright cyan', 'crashing wave', 'a violent stormy ocean with huge swirling tidal waves, white foam and spray', 'a dark deep whirlpool of water'),
 'F006': ('물안개 결계', 'a misty sacred lake with a red torii gate, soft blue fog and a calm water barrier', 'soft pale-blue', 'water droplet', 'a mystical misty lake with lotus flowers, a red torii gate on the water, soft blue fog and calm reflections', 'calm misty water'),
 'F007': ('천공의 정원', 'floating sky garden islands with waterfalls, lush greenery above the clouds', 'emerald', 'feathered wing', 'floating sky islands with lush gardens and waterfalls pouring into a sea of white clouds', 'a wide open cloud sea'),
 'F008': ('폭풍의 폭주', 'a violent tornado storm over green plains with swirling winds and flying leaves', 'vivid jade-green', 'spiral whirlwind', 'green plains torn by a giant swirling tornado, wind streaks, flying leaves and storm clouds', 'the dark eye of the storm'),
 'F009': ('산들바람 결계', 'a gentle meadow protected by a translucent green wind dome with swaying flowers', 'mint-green', 'leaf', 'a peaceful flower meadow with soft swirling green wind ribbons and a translucent protective wind dome', 'a calm grassy clearing'),
 'F010': ('태고의 협곡', 'an ancient red-rock canyon with natural stone arches and primordial cliffs', 'warm amber-bronze', 'ancient stone', 'a vast ancient red-rock canyon with natural stone arches, deep ravines and primordial cliffs', 'a flat dusty canyon floor'),
 'F011': ('지각의 폭주', 'a shattered earthquake terrain with tectonic cracks, rising rock pillars and glowing magma seams', 'glowing amber-orange', 'cracked boulder', 'a shattered earthquake landscape with deep tectonic cracks, rising jagged rock pillars and glowing magma seams', 'a dark cracked rock plate'),
 'F012': ('암석의 결계', 'a fortress ring of massive standing stones and amber crystals guarding a rocky plateau', 'golden amber', 'amber crystal', 'a rocky plateau surrounded by a ring of massive standing stones and glowing amber crystal clusters', 'a smooth stone plaza'),
 'F013': ('뇌운의 탑', 'a towering lightning spire on a mountain peak beneath dark thunderclouds', 'electric yellow', 'lightning bolt', 'jagged dark mountain peaks under thick thunderclouds lit by lightning, with a tall lightning tower', 'a dark rocky summit plateau'),
 'F014': ('번개의 폭주', 'a wild electric storm with countless lightning bolts striking a dark plain', 'crackling electric-yellow', 'forked lightning', 'a dark plain struck by countless forked lightning bolts, glowing scorch marks and storm clouds', 'a dark scorched plain'),
 'F015': ('전류의 결계', 'a circular arena enclosed by crackling blue-white electric barrier pylons', 'blue-white electric', 'charged orb', 'a circular metal arena surrounded by tall tesla pylons linked by crackling electric arcs', 'a dark metal arena floor'),
 'F016': ('광휘의 성당', 'a radiant golden cathedral interior with stained glass light and holy rays', 'radiant gold', 'holy sun', 'the inside of a vast radiant cathedral seen from above, golden floor mosaics, stained glass light beams and pillars', 'a polished marble floor'),
 'F017': ('섬광의 폭주', 'an explosion of blinding white-gold light rays bursting over a celestial landscape', 'brilliant white-gold', 'starburst', 'a celestial cloud landscape with a burst of blinding white-gold light rays and sparkles', 'a soft glowing cloud field'),
 'F018': ('성광의 결계', 'a golden holy dome protecting a shining white city', 'warm gold', 'angel wing', 'a shining white marble city protected by a translucent golden holy dome of light', 'a white marble plaza'),
 'F019': ('칠흑의 심연', 'a pitch-black abyss void with swirling purple darkness and floating debris', 'deep violet', 'void eye', 'a pitch-black abyss with swirling purple dark energy, floating rock debris and faint violet glows', 'a deep black void'),
 'F020': ('암흑의 폭주', 'a ruined dark city under a blood-red moon with violet shadow flames', 'violet-magenta', 'blood moon', 'a ruined dark gothic city under a blood-red moon with violet shadow flames rising from the streets', 'a dark ruined square'),
 'F021': ('그림자 결계', 'a purple shadow barrier dome over a gothic castle at night with a full moon', 'lavender-purple', 'crescent moon', 'a gothic castle and dark forest at night covered by a translucent purple shadow barrier dome', 'a dark castle courtyard'),
 'F023': ('고요한 평원', 'a calm peaceful green plain with a winding dirt road and a lone tree', 'soft warm-silver', 'simple compass', 'a calm peaceful green plain with gentle hills, a winding dirt road, a lone tree and small wildflowers', 'an open grassy field'),
 'F024': ('황혼의 전장', 'a dusk battlefield with broken banners and weapons under a purple-orange sunset', 'dusky orange-purple', 'crossed swords', 'an old battlefield at dusk with broken banners, scattered swords and shields and long sunset shadows', 'a trampled dark field'),
 'F025': ('혼돈의 균열', 'a chaotic dimensional rift tearing reality with a prismatic storm and shattered floating rocks', 'chaotic prismatic', 'shattered crystal', 'a chaotic landscape torn by glowing dimensional rifts, prismatic storms and shattered floating rocks', 'a dark rift crater'),
}

PM = ("Turn this flat layout sketch into a premium fantasy trading card game playmat illustration themed as {scene}. "
      "Keep EXACTLY the same layout: the outer border, the three outlined zone boxes in the left column and in the right column, "
      "the big central rectangle and the round emblem in the middle must stay at identical positions and sizes, redrawn as fine glowing {line} lines, "
      "with a small {emblem} emblem inside the circle. The artwork behind the lines is rich but slightly darkened and low-contrast, "
      "with a calmer, darker center so that game cards placed on top stay readable. Flat top-down view, no perspective, no text, no letters, no numbers, no cards, no characters.")
ENV = ("Fantasy game environment art seen straight from high above (top-down aerial view) of {env}. "
       "The scene fills the entire wide frame edge to edge with rich detail and atmospheric lighting; the central area is {center}. "
       "Painterly high-quality fantasy style, vivid colors, no text, no UI, no characters, no cards, no table, no border.")

out = {k: {'name': v[0], 'playmat': PM.format(scene=v[1], line=v[2], emblem=v[3]), 'env': ENV.format(env=v[4], center=v[5])} for k, v in F.items()}
json.dump(out, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'field_prompts.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(len(out))
