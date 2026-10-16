# 코인 면 그림 (VARCO nano-banana-pro) — 정면 · 정중앙 · 양각
BASE = ("Perfectly straight top-down orthographic view of ONE fantasy game coin face, exactly centered in the image, a perfect circle "
        "whose diameter fills about 92% of the image width, absolutely no perspective, no tilt, no shadow. "
        "Highly detailed raised embossed relief (bas-relief) metal coin sculpting with crisp bevels, soft studio light from the top-left "
        "so the raised parts catch highlights and cast tiny shadows, a raised beaded rim ring around the edge. "
        "Design: {design}. Material: {mat}. The main emblem is precisely in the center of the coin. "
        "Background outside the coin is a perfectly flat solid magenta (#FF00FF). No text, no letters, no numbers, no watermark.")
COINS = {
    'default':  ("polished bright gold",
                 "a large bold seven-pointed star emblem in the center with fine radiating sun rays between the points",
                 "an ornate laurel wreath circle with a small radiant sun in the center"),
    'phoenix':  ("rose gold with small ruby gem accents",
                 "a phoenix with wings spread symmetrically, centered, a small ruby on its chest",
                 "a symmetric swirling flame mandala with a ruby in the exact center"),
    'sapphire': ("deep blue faceted sapphire crystal set in a silver rim",
                 "a seven-pointed star cut into the sapphire with crystal facets radiating from the center",
                 "concentric ocean wave rings carved around a small central droplet"),
    'gale':     ("pale green polished jade with silver inlay",
                 "a swirling wind spiral around a small seven-pointed star in the center",
                 "a symmetric wreath of feathers around a small spiral in the center"),
    'bronze':   ("ancient aged bronze with green verdigris patina",
                 "a seven-pointed star with a mountain range relief and a round amber gem in the exact center",
                 "an ancient runic circle with geometric earth glyphs around a central amber dot"),
    'topaz':    ("dark brushed steel with gold lightning inlay",
                 "a seven-pointed star made of lightning bolts with a glowing faceted yellow topaz gem in the exact center",
                 "a symmetric crackling lightning sigil radiating from the center"),
    'moon':     ("polished sterling silver",
                 "a crescent moon embracing a round glowing moonstone in the exact center, tiny stars around",
                 "a star constellation pattern in a ring with a small eight-pointed star in the center"),
    'obsidian': ("glossy black obsidian with violet amethyst",
                 "a seven-pointed faceted amethyst crystal star in the center",
                 "a dark symmetric spiral sigil with small amethyst points around a central gem"),
    'opal':     ("iridescent white opal with rainbow play-of-color and a silver rim",
                 "a seven-pointed star carved in the opal at the center",
                 "concentric rainbow rings radiating from a small central star"),
}
def prompt(name, side):
    mat, h, t = COINS[name]
    return BASE.format(design=h if side == 'h' else t, mat=mat)
if __name__ == '__main__':
    import json, sys
    jobs = [(n, s) for n in COINS for s in 'ht']
    print(json.dumps([[n, s, prompt(n, s)] for n, s in jobs], ensure_ascii=False))
