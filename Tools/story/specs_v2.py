# -*- coding: utf-8 -*-
"""제2~10장 VARCO 이미지 명세 → specs_v2.json
key: 결과 파일 이름 (raw/{key}.png) · ar: 비율 · refs: 레퍼런스 캐릭터 시트 · prompt"""
import json

STYLE = ("Anime fantasy comic illustration, cinematic wide shot, dramatic lighting, rich saturated colors, "
         "detailed painted background, clean line art, cel-shaded with soft painterly lighting. "
         "Characters must match the reference character sheets exactly (same faces, hair, outfits, colors). "
         "No text, no speech bubbles, no letters, no panel borders, no watermark.")

KAI = "Kai (silver-haired academy boy in a navy blazer with cloth-wrapped fists)"
ARIA = "Aria (blue-haired water healer girl)"
LEON = "Leon (red-haired fire mage boy)"
BANE = "Professor Bane (old wind mage with a gray beard)"
PKAI = "Kai in his awakened prism form (silver hair with rainbow highlights, prismatic glowing eyes)"

S = {}
def sc(key, refs, desc):
    S[key] = dict(ar="16:9", refs=refs, prompt=desc + " " + STYLE)

# ── 제2장 회색의 전조 ──
sc("v2_1", ["kai", "aria", "leon", "bane"], f"Inside a magic academy map room at dusk, {BANE} points at a huge continent map stained with spreading gray blotches while {KAI}, {ARIA} and {LEON} look on with determination.")
sc("v2_2", ["kai", "aria", "leon"], f"Night forest road campfire, {KAI}, {ARIA} and {LEON} leap to their feet as gray mist rolls in and gray ash-colored wolves with empty eyes emerge from the dark trees.")
sc("v2_3", ["kai", "aria", "leon"], "A small medieval village where everything has turned colorless gray (roofs, flowers, laundry), hollow-eyed gray villagers holding spears surround " + f"{KAI}, {ARIA} and {LEON} in the village square, gray mist curling around their feet.")
sc("v2_4", ["kai", "aria", "leon"], f"A deep cavern under a village well, a gigantic golem made of solidified gray mist rises, trapped streaks of red yellow and green color swirling inside its body, {KAI}, {ARIA} and {LEON} ready to fight.")
sc("v2_5", ["varus", "kai"], f"A village festival bonfire frozen into gray stone at night, Varus (tall hooded gray-robed man with long gray hair and empty gray eyes) stands calmly in the mist facing {KAI} who raises his fist defiantly.")
sc("v2_6", ["bane", "kai", "aria"], f"Mountain path, a swarm of gray ash crows dives at fleeing villagers, {BANE} slices through the flock with a huge green wind gust while {KAI} and {ARIA} protect children.")
sc("v2_7", ["kai", "aria", "leon", "bane"], f"A mountain peak with a colossal gray stone gate open to the sky pouring waterfalls of gray mist, a stone gatekeeper giant clad in mist armor stands before it, {KAI}, {LEON}, {ARIA} and {BANE} face it.")

# ── 제3장 엇갈리는 원소들 ──
sc("v3_1", ["leon", "aria", "kai"], f"A fire kingdom border fortress of red rock above rivers of lava, fire-armored guards with flaming spears point at {ARIA}, {LEON} steps forward to argue, {KAI} beside them.")
sc("v3_2", ["ignis", "leon", "kai"], f"A burning throne hall of a volcano palace, Ignis (fire grand mage woman with blazing orange hair and red dress) rises from her flaming throne with fire swirling around her, {KAI} and {LEON} before her.")
sc("v3_3", ["leon", "aria", "kai"], f"A mirror-like lake with a white crystal castle, water-armored guards with tridents surround {LEON}, {ARIA} pleads with them, {KAI} stands ready.")
sc("v3_4", ["meril", "kai"], f"An elegant water palace hall over the lake, Meril (water grand mage queen with long silver-blue hair, crown and flowing teal robes) raises a towering wave behind her, facing {KAI}.")
sc("v3_5", ["kai", "aria", "leon"], f"A battlefield at a border river, fire soldiers and water soldiers clashing amid steam, gray mist creeping around their feet, {KAI}, {ARIA} and {LEON} rush in between.")
sc("v3_6", ["ignis", "meril", "kai"], f"The sky split in half, one side blazing hellfire from Ignis, the other a giant tidal wave from Meril, {KAI} stands in the exact middle with both palms outstretched pushing both apart with an invisible force.")
sc("v3_7", ["ignis", "meril", "kai"], f"A gray legion army advancing along a river, Ignis and Meril fight side by side for the first time, fire and water spiraling together, {KAI} leading the charge.")

# ── 제4장 대지와 바람의 시련 ──
sc("v4_1", ["zephyr", "kai", "leon"], f"A cozy highland tavern, Zephyr (playful green-haired bard with goggles, scarf and lute) grins and strums his lute on a table, {KAI} and {LEON} watch skeptically.")
sc("v4_2", ["zephyr", "kai", "aria"], f"A wind path climbing to floating sky islands, a huge swirling storm of gray clouds raining ash, Zephyr summons a green tailwind, {KAI} and {ARIA} brace against the gale.")
sc("v4_3", ["volt", "kai"], f"Top of a thunder tower among dark storm clouds, Volt (black-haired armored martial artist with a glowing electric gauntlet) throws a lightning punch, {KAI} meets it with his wrapped fist.")
sc("v4_4", ["zephyr", "volt", "kai"], f"A beautiful valley with flute-like rock pillars, now gray and silent, gray soldiers marching through, Zephyr looks heartbroken, Volt and {KAI} step forward.")
sc("v4_5", ["terra", "kai"], f"An ancient canyon of layered amber rock, Terra (huge bearded earth grand mage in mossy stone armor with crystals and a rune shield) stands immovable like a mountain, facing {KAI}.")
sc("v4_6", ["terra", "volt", "kai"], f"A giant made of hardened gray mist bursts out of the canyon floor, as tall as a mountain, Terra braces the ground, Volt charges with lightning, {KAI} in front.")
sc("v4_7", ["kai", "terra", "zephyr", "volt"], f"On the canyon rim a gray-cloaked expressionless woman lieutenant with faint sea-blue hair under the hood leads a gray army, {KAI} with Terra, Zephyr and Volt confront her.")

# ── 제5장 빛과 어둠의 성전 ──
sc("v5_1", ["kai", "leon"], f"A dazzling holy city of white marble and golden spires, armored paladins in gold-white armor raise glowing swords at {KAI}, {LEON} beside him.")
sc("v5_2", ["lumien", "kai"], f"Inside a grand cathedral flooded with golden light through stained glass, Lumien (serene blonde light grand mage saint in white veil and gold-trimmed dress holding a sun staff) faces {KAI}.")
sc("v5_3", ["kai", "aria", "leon"], f"A border where black shadow army soldiers surge like a tide toward the holy city, the ground stained black, {KAI}, {ARIA} and {LEON} stand on a ridge watching.")
sc("v5_4", ["erebos", "kai"], f"A pitch-black abyss throne room lit by violet flames, Erebos (tall pale dark grand mage with black horned crown, violet eyes and dark purple armor) leans from his throne, facing {KAI}.")
sc("v5_5", ["lumien", "erebos", "kai"], f"A vast plain where a gray legion stretches to the horizon, a golden light barrier and a black shadow spear strike side by side, Lumien and Erebos fight back to back, {KAI} at the center.")
sc("v5_6", ["varus", "bane", "kai"], f"Twilight plain parted by the gray legion, Varus walks forward alone with faint silver light leaking from his gray robe, {BANE} and {KAI} face him tensely.")
sc("v5_7", ["ignis", "meril", "zephyr", "terra"], "Seven grand mages standing in a great circle on a twilight plain, seven pillars of colored light (red, blue, green, amber, yellow lightning, gold, violet) rising into the sky, Ignis, Meril, Zephyr and Terra visible in front, and above them the sky starting to crack like glass.")

# ── 제6장 붕괴하는 하늘 ──
sc("v6_1", ["erebos", "kai", "aria"], f"The sky shattered like broken glass revealing white nothingness, jagged gray void shards raining down on the land, Erebos raises shadow spears beside {KAI} and {ARIA}.")
sc("v6_2", ["kai", "volt", "terra"], f"A snowy northern plain, a colossal gray arm with mountain-sized fingers reaches out of a sky rift and grips the earth, Volt, Terra and {KAI} tiny in comparison.")
sc("v6_3", ["lord", "kai", "ignis", "meril"], f"The Lord of Oblivion (formless gray giant with a single huge white eye in its chest and thousands of tentacles) descends onto a snowfield, Ignis's flames vanish into nothing before reaching it, {KAI} watches in horror.")
sc("v6_4", ["kai", "aria", "leon"], f"An allied army of fire, water, light and dark banners lined up together on a twilight plain, {KAI}, {ARIA} and {LEON} at the front line, a gray army and a giant shadow on the horizon.")
sc("v6_5", ["leon", "ignis"], f"{LEON} awakening, his red flames turning into brilliant WHITE fire, standing protectively in front of fallen soldiers, a gray-cloaked woman with a gray blade recoils, Ignis watching proudly behind.")
sc("v6_6", ["aria", "kai"], f"{ARIA} awakening in a field hospital camp, a giant spiraling tide of glowing blue water rising from her feet and healing fallen soldiers, gray tentacles retreating, {KAI} looking at her in awe.")
sc("v6_7", ["varus", "kai", "lord"], f"Varus kneeling with silver light breaking through his gray robe, reaching out to throw a silver shard to {KAI}, while a giant gray tentacle from the sky pierces down toward him.")

# ── 제7장 참패와 절망 ──
sc("v7_1", ["lord", "ignis", "meril", "volt"], "The Lord of Oblivion grown twice as large blotting out the sky, seven colors of grand mage magic hitting it and dissolving into nothing, grand mages blown back and falling, despair.")
sc("v7_2", ["kai", "aria", "leon"], f"A forest made entirely of gray tentacles growing like trees, {KAI}, {ARIA} and {LEON} escaping exhausted and wounded, everything gray.")
sc("v7_3", ["kai", "aria", "leon"], f"A ruined village at night, a huge void devourer creature with a gaping maw made of gray shards rises, exhausted {ARIA} and {LEON} lean on each other while {KAI} stands in front.")
sc("v7_4", ["kai"], f"Dawn at a crumbling temple, {KAI} alone on the steps facing a line of expressionless gray soldiers, his friends asleep inside behind him, determined but tired.")
sc("v7_5", ["leon", "kai"], f"On a hill a phantom copy of {LEON} with gray hollow eyes and ash-gray flames, the real {LEON} and {KAI} stand below facing it.")
sc("v7_6", ["kai", "aria", "leon"], f"Gray-robed apostle mages descending in a line from a sky rift, each imitating a grand mage's magic in dull gray, {KAI}, {ARIA} and {LEON} with renewed hope in their eyes.")
sc("v7_7", ["kai", "leon", "aria"], f"A giant gray hand lifting a chunk of earth into the sky toward a white rift, {LEON} and {ARIA} leaping off to safety, a gray finger wrapped around {KAI}'s ankle dragging him into the rift, {LEON} screaming his name.")

# ── 제8장 순백의 도화지 ──
sc("v8_1", ["kai", "ignis"], f"An infinite pure white void like a blank canvas, a spirit of Ignis made of flames hands a small flame to {KAI}, the flame resting peacefully on his palm without burning him.")
sc("v8_2", ["kai", "meril"], f"The white void with glowing blue water ribbons flowing around {KAI}, a translucent spirit of Meril smiling gently, fire and water swirling together calmly around him.")
sc("v8_3", ["kai"], f"The white void stained by spreading gray ink blots that form gray wolves and crows, {KAI} standing alone looking pained, whispering shadows around him.")
sc("v8_4", ["kai"], f"Mirror duel in a white void: {KAI} faces a gray shadow version of himself with hollow gray eyes and gray bandages, both in the same fighting stance, symmetrical composition.")
sc("v8_5", ["prisma", "kai"], f"The white void blooming with rainbow light, the goddess Prisma (ethereal woman with long iridescent rainbow hair, translucent prismatic gown and a halo) floats before {KAI} smiling warmly.")
sc("v8_6", ["volt", "zephyr", "lumien", "kai"], f"In the white void, spirits of the grand mages appear as colored lights, Volt's spirit throws a lightning punch which {KAI} meets with a fist glowing in seven colors, Zephyr and Lumien watching.")
sc("v8_7", ["prismkai", "aria", "leon"], f"A ruined battlefield before a sky rift, white light pours out of the rift as {PKAI} steps out, {ARIA} in tears of joy, {LEON} grinning, gray tentacles recoiling.")

# ── 제9장 일곱 빛깔의 검 ──
sc("v9_1", ["prismkai", "ignis", "leon"], f"A gray petrified volcano summit, Ignis trapped inside a gray crystal, {PKAI} touches it with red light spreading from his hand, {LEON} beside him, gray apostles turning toward them.")
sc("v9_2", ["prismkai", "meril", "ignis"], f"A frozen gray lake with a void devourer crouching on it, Meril trapped in crystal, Ignis and {PKAI} charging across the ice.")
sc("v9_3", ["prismkai", "zephyr", "volt"], f"Inside a gray storm above sky islands, {PKAI} punches through the storm with a rainbow fist, freed Zephyr playing his lute and Volt surging with lightning.")
sc("v9_4", ["terra", "erebos", "prismkai"], f"Terra slams the ground raising enormous stone walls in a canyon, Erebos with shadow spears, {PKAI} standing on the wall facing gray giants.")
sc("v9_5", ["lumien", "erebos", "prismkai"], f"The holy city wrapped in gray tentacles, Erebos reaching toward Lumien inside a crystal, light and shadow joining hands, {PKAI} between them glowing.")
sc("v9_6", ["prismkai", "aria", "leon"], f"Seven beams of colored light converging into {PKAI} in a holy city plaza, his hair turning rainbow, {ARIA} and {LEON} amazed, a gray avatar of the Lord looming.")
sc("v9_7", ["prismkai", "lord"], f"Epic climax: {PKAI} wielding a radiant crystal sword of seven rainbow colors leaps and pierces the giant white eye of the Lord of Oblivion in the sky above the holy city, colors bursting outward.")

# ── 제10장 태초의 기적, 그리고 ──
sc("v10_1", ["prismkai", "aria", "leon"], f"Inside the rift: an endless white nothingness, a path of colored light trailing behind {PKAI}, {ARIA} and {LEON} walking forward, thousands of gray tentacles rising ahead.")
sc("v10_2", ["prismkai", "aria", "leon"], f"In the white void a vast gray army fills the horizon, beams of colored light from the grand mages pour through the rift from outside to support {PKAI}, {ARIA} and {LEON}.")
sc("v10_3", ["prismkai"], f"A pulsing white core like a giant heart floating in the void, gray tendrils rebuilding a body around it, {PKAI} raising his rainbow sword.")
sc("v10_4", ["prismkai", "lord"], f"The Lord of Oblivion shrinking and cracking, its single white eye weeping, attacking desperately, {PKAI} standing firm with sad compassion.")
sc("v10_5", ["prismkai", "lord"], f"{PKAI} and the Lord of Oblivion face off in the void, a silver shard glowing beside Kai with the faint ghost of Varus, the Lord gathering all remaining nothingness into a final storm.")
sc("v10_e", ["kai", "prisma"], f"Emotional climax: {KAI} has dropped his rainbow sword and hugs a small child-sized gray shadow figure, the shadow turning into soft white light, the goddess Prisma's translucent figure smiling behind them, rainbows filling the sky beyond the rift.")
sc("v10_6", ["kai", "leon", "aria"], f"The magic academy circular arena packed with cheering students, {KAI} and {LEON} lying side by side exhausted on the arena floor laughing after a duel, {ARIA} waving from the stands.")
sc("v10_7", ["kai", "aria", "leon", "bane"], f"The whispering forest in bright morning sunlight full of vivid green leaves, red berries and blue sky, {KAI}, {ARIA}, {LEON} and {BANE} walking together on a forest path, a faint golden spirit stag watching from the trees.")

# ── 대사 초상화 (1:1) ──
FACE = ("Anime visual-novel dialogue portrait of the exact character in the reference image: head and shoulders bust, facing three-quarters "
        "toward the viewer, natural expressive face, same hair, eyes, outfit and colors as the reference. Plain soft dark-blue gradient background, "
        "centered, clean line art, cel-shaded with soft painterly lighting, high detail, absolutely no text, no labels, no letters, no frame.")
for c in ["ignis", "meril", "terra", "zephyr", "volt", "lumien", "erebos", "varus", "prisma"]:
    S["face_" + c] = dict(ar="1:1", refs=[c], prompt=FACE)

# ── 영웅 카드 풀아트 (3:4) ──
HERO = ("Trading card game hero full-art illustration of the exact character in the reference sheet, {pose}. Dynamic heroic composition, "
        "full body or three-quarter body filling the frame, {bg}. Same face, hair, outfit and colors as the reference. Anime fantasy style, "
        "cel-shaded with rich painterly lighting, high detail, no text, no letters, no card frame, no border.")
HEROES = {
    "H018": ("aria", "gracefully casting healing magic, glowing blue water droplets and ribbons of water swirling around her raised hands", "background of a shimmering blue lake temple"),
    "H019": ("bane", "an old wind master in a calm fighting stance, his robe and beard whipped by a spiraling green tornado around him", "background of a stormy sky over a magic academy"),
    "H020": ("meril", "the water queen raising one hand as a towering tidal wave curls behind her", "background of a crystal palace over a moonlit lake"),
    "H021": ("terra", "the mighty earth guardian planting his rune shield, the ground cracking with glowing amber crystals", "background of an ancient canyon"),
    "H022": ("zephyr", "the playful bard leaping through the air with his lute, green wind currents and feathers around him", "background of floating sky islands"),
    "H023": ("lumien", "the light saint holding her sun staff high, radiant golden light and feathers raining down", "background of a cathedral with stained glass"),
    "H024": ("erebos", "the dark king spreading his shadow cape like wings, violet flames and shadow spears around him", "background of a black abyss with violet light"),
    "H025": ("varus", "the gray apostle standing in swirling gray mist, faint silver light leaking from under his hood, melancholic", "background of a colorless wasteland under a pale sky"),
    "H026": ("prisma", "the goddess floating with arms open, rainbow light refracting all around her like a prism", "background of a pure white void blooming with rainbows"),
    "H027": ("prismkai", "the awakened hero holding a radiant crystal sword of seven rainbow colors, prismatic light bursting from his eyes", "background of a shattered gray sky with rainbow light breaking through"),
}
for hid, (c, pose, bg) in HEROES.items():
    S[hid + "_full"] = dict(ar="3:4", refs=[c], prompt=HERO.format(pose=pose, bg=bg))

# ── 3D 용 A-포즈 레퍼런스 (1:1) → Generate3D → Rig(humanoid-fingers) → Animate ──
APOSE = ("Full body 3D game character model reference of the exact same character as the reference sheet: single character only, standing in a relaxed "
         "A-pose with arms slightly away from the body, legs slightly apart, exactly two arms and two legs, front view, entire body visible from the top "
         "of the head to the feet. Same design, hair, outfit and colors as the reference. Hands empty, no weapon, no staff, no shield, no lute, no props, "
         "no effects, no particles. {extra}Plain light-gray studio background, even soft lighting, clean stylized anime game-asset render, no text.")
APX = {
    "H018": "", "H019": "", "H020": "", "H021": "Keep the stone armor but no shield. ", "H022": "Goggles on head, scarf. ",
    "H023": "", "H024": "Cape hanging straight down. ", "H025": "Hood down revealing his face. ", "H026": "Feet wearing simple sandals. ",
    "H027": "No sword. ",
}
for hid, (c, _, _) in HEROES.items():
    S[hid + "_apose"] = dict(ar="1:1", refs=[c], prompt=APOSE.format(extra=APX[hid]))

# ── 하수인 카드 (4:3) ──
MON = "Trading card monster illustration, {d}, dark fantasy anime style, dramatic lighting, detailed, centered creature filling the frame, no text, no letters, no border."
S["SM04"] = dict(ar="4:3", refs=[], prompt=MON.format(d="a colorless gray soldier in gray plate armor with a tower shield and spear, hollow empty gray eyes, gray mist curling around him, on a desaturated battlefield"))
S["SM05"] = dict(ar="4:3", refs=["lord"], prompt=MON.format(d="a huge gray tentacle bursting out of a white cracked rift in the ground, its surface absorbing colors from flowers around it which turn gray"))
S["SM06"] = dict(ar="4:3", refs=[], prompt=MON.format(d="a jagged floating shard of white-gray void crystal with a small glowing white eye, splitting into smaller shards as it charges forward, in a shattered sky"))

json.dump(S, open("specs_v2.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(len(S), "specs")
