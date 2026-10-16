# -*- coding: utf-8 -*-
import json, os, sys
sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
M = [m for m in json.load(open(os.path.join(HERE, 'monsters.json'), encoding='utf-8')) if m['new']]
THEME = {
    'None': 'neutral earthy steel-gray tones',
    'Fire': 'fiery red and orange flames, embers',
    'Water': 'deep blue water, ice and sea foam',
    'Wind': 'emerald green winds, swirling leaves and clouds',
    'Earth': 'brown rock, moss and amber crystals',
    'Thunder': 'electric yellow lightning and storm clouds',
    'Light': 'radiant white and gold holy light',
    'Dark': 'shadowy purple and black, eerie glow',
    'Rainbow': 'prismatic rainbow colors, stars and aurora',
}
groups = []
by = {}
for m in M: by.setdefault(m['elem'], []).append(m)
for el, lst in by.items():
    for i in range(0, len(lst), 4):
        g = lst[i:i + 4]
        if len(g) < 4: g = g + lst[:4 - len(g)]   # 남는 칸은 같은 속성 앞쪽 몬스터로 채움 (중복은 무시)
        groups.append(dict(elem=el, ids=[x['id'] for x in g], fill=len(lst[i:i + 4])))
for gi, g in enumerate(groups):
    ms = [next(x for x in M if x['id'] == i) for i in g['ids']]
    g['prompt'] = ("A 2x2 grid of four separate fantasy trading card illustrations, separated by thin straight black gutters. "
                   "Each panel is a complete standalone painting with its own background, one subject centered, landscape framing. "
                   "Painterly high-detail anime fantasy card game art, dramatic lighting, vivid colors, " + THEME[g['elem']] + ". "
                   + ' '.join(f"Panel {k + 1} ({['top-left', 'top-right', 'bottom-left', 'bottom-right'][k]}): {m['en']}." for k, m in enumerate(ms))
                   + " No text, no letters, no frames, no watermark.")
json.dump(groups, open(os.path.join(HERE, 'art_groups.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(len(groups), 'groups')
