# -*- coding: utf-8 -*-
# 몬스터 270종 정의: (속성, 타임, k(0~2, 등급 오름차순), 기존ID or None, 이름, 영어 묘사(아트 프롬프트), 능력)
# 능력 키: taunt double chain assassin hidden burn heal draw strike quake rally weaken destroy haste revive guardall fortify regen max4  ("+" 로 조합, "" = 바닐라)

T = []
def add(elem, t, rows):
    for k, row in enumerate(rows):
        eid, ko, en, ab = row
        T.append(dict(elem=elem, t=t, k=k, id=eid, ko=ko, en=en, ab=ab))

# ───────── 무속성 (None) : 균형형 · 용병/짐승/기계 ─────────
E = 'None'
add(E, 1, [("M019", "떠돌이 기사", "", "max4"),
           (None, "견습 창병", "a young recruit spearman with a round wooden shield and leather helmet", ""),
           (None, "회색 늑대", "a fierce gray wolf baring fangs in a misty pine forest", "regen")])
add(E, 2, [(None, "철갑 멧돼지", "a huge boar wearing riveted iron armor plates, charging", ""),
           (None, "용병 궁수", "a scarred mercenary archer drawing a longbow, hooded cloak", "strike"),
           (None, "방랑 검객", "a wandering swordsman with a straw hat and a long katana, calm stance", "fortify")])
add(E, 3, [(None, "태엽 병정", "a brass clockwork toy soldier with a wind-up key on its back, holding a musket", "taunt"),
           (None, "대장장이 거한", "a giant muscular blacksmith swinging a glowing hot hammer over an anvil", "rally"),
           (None, "사막 전갈왕", "a giant armored desert scorpion with a golden stinger in sand dunes", "regen+strike")])
add(E, 4, [(None, "왕국 근위병", "a royal palace guard in shining plate armor with a halberd and blue cape", "taunt"),
           (None, "강철 골렘", "a massive steel golem with glowing blue rune joints, factory background", "fortify"),
           (None, "그리핀", "a majestic griffin with eagle head and lion body spreading wings on a cliff", "double")])
add(E, 5, [(None, "격투 투사 바르가", "a muscular gladiator with chains and a bronze helmet in an arena", "regen"),
           (None, "전쟁 맘모스", "a war mammoth with a fortified wooden tower on its back, snowy battlefield", "taunt+fortify"),
           (None, "백전의 기사단장", "a veteran knight commander with a scarred face, greatsword, and red banner", "rally+strike")])
add(E, 6, [(None, "마도 기갑병", "a magitech armored mech soldier with steam vents and a cannon arm", "strike"),
           (None, "키메라", "a chimera with lion, goat and serpent heads roaring", "double"),
           ("M020", "고대의 거신", "", "")])
add(E, 7, [(None, "천공 요새 골렘", "a colossal golem carrying a stone fortress on its shoulders above clouds", "taunt+regen"),
           (None, "히드라", "a five-headed green hydra rising from a swamp", "chain+regen"),
           (None, "전설의 용병왕", "a legendary mercenary king with a crown, scarred armor and twin axes", "rally+haste")])
add(E, 8, [(None, "거신병 타이탄", "a towering ancient titan war machine of bronze and stone", "taunt+fortify"),
           (None, "베히모스", "a behemoth beast with massive horns stomping through a canyon", "quake"),
           (None, "검성 무명", "a nameless sword saint with flowing white hair slicing the air, cherry blossoms", "double+strike")])
add(E, 9, [(None, "시간의 수호자", "a mysterious guardian of time holding a giant hourglass, clockwork wings", "revive+fortify"),
           (None, "영웅왕 레가투스", "a heroic king in golden armor raising a legendary sword to the sky", "rally+fortify"),
           (None, "고대의 회색룡", "an ancient elder dragon with gray stone-like scales on a mountain peak", "destroy")])
add(E, 10, [(None, "천하무적 검왕", "an invincible sword king surrounded by floating swords", "double+strike"),
            (None, "창세의 거신", "a genesis colossus made of stars and stone shaping the world", "quake+taunt"),
            (None, "절대자", "an absolute warrior deity in white armor, overwhelming aura", "destroy+rally")])

# ───────── 화염 (Fire) : 공격형 ─────────
E = 'Fire'
add(E, 1, [("M001", "불꽃 꼬마용", "", ""),
           (None, "불씨 도롱뇽", "a cute red salamander with ember sparks on its back, volcanic rocks", "burn"),
           (None, "화염 임프", "a mischievous fire imp juggling fireballs", "strike")])
add(E, 2, [(None, "화산 도마뱀", "a spiky lava lizard on a volcano slope", ""),
           (None, "불꽃 검투사", "a gladiator with a flaming sword and bronze shield", "burn"),
           (None, "숯불 늑대", "a wolf made of glowing cinders and smoke", "double")])
add(E, 3, [(None, "아기 불사조", "a baby phoenix chick wrapped in gentle flames", "regen"),
           ("M002", "화염 창술사", "", "burn"),
           (None, "홍염 마도사", "a crimson flame sorcerer casting a fire spiral", "strike+burn")])
add(E, 4, [(None, "작열 사자", "a blazing lion with a mane of fire roaring", "rally"),
           (None, "화염 기사", "a knight in red armor wielding a burning lance", "double"),
           (None, "마그마 골렘", "a hulking magma golem with cracks of molten lava", "quake")])
add(E, 5, [(None, "업화의 무희", "a fire dancer twirling flaming ribbons", "burn+haste"),
           (None, "화산 거인", "a volcano giant with an erupting crater on its back", "quake"),
           ("M003", "인페르노 드래곤", "", "double")])
add(E, 6, [(None, "지옥 사냥개", "a three-headed hellhound breathing fire", "double"),
           (None, "홍련 검성", "a sword master with a crimson lotus blade engulfed in flames", "strike+rally"),
           (None, "피닉스", "a majestic phoenix spreading huge burning wings", "regen+burn")])
add(E, 7, [(None, "지옥불 마룡", "a hellfire wyvern diving through black smoke", "strike"),
           (None, "이프리트", "a giant efreet djinn of fire with bracelets and a flaming scimitar", "burn+rally"),
           (None, "태양 사자왕", "a radiant sun lion king with a corona mane", "double+rally")])
add(E, 8, [(None, "화염 군주", "a fire overlord on a throne of molten rock", "quake+burn"),
           (None, "멸망의 불새", "a doom firebird raining fire over a burning city", "chain+burn"),
           (None, "홍염 용기사", "a dragon knight riding a red dragon with a flaming spear", "double+strike")])
add(E, 9, [(None, "태양 무녀", "a sun shrine maiden with a golden halo of fire", "rally+burn"),
           (None, "종말의 화염 거인", "an apocalyptic fire giant with a flaming greatsword", "quake+burn"),
           (None, "화룡왕 볼카누스", "a volcano dragon king with obsidian horns and lava wings", "destroy+burn")])
add(E, 10, [(None, "태양 폭군 솔라리스", "a sun tyrant with a blazing sun crown and burning cape", "double+burn"),
            (None, "원초의 화룡", "a primordial fire dragon emerging from the core of the world", "quake+strike"),
            (None, "염신 아그니", "the fire god with multiple arms and a halo of flames", "destroy+burn")])

# ───────── 물 (Water) : 수비형 · 회복 ─────────
E = 'Water'
add(E, 1, [("M004", "물방울 정령", "", "heal"),
           (None, "산호 게", "a little crab carrying pink coral on its shell", "taunt"),
           (None, "아기 해달", "a cute baby sea otter floating with a shell", "draw")])
add(E, 2, [(None, "강의 거북", "a mossy river turtle with a lotus on its shell", "taunt"),
           (None, "해파리 요정", "a glowing jellyfish fairy drifting in the deep", "heal+weaken"),
           (None, "인어 병사", "a merfolk soldier with a coral spear and shell armor", "")])
add(E, 3, [(None, "얼음 펭귄 기사", "a penguin knight with an icicle sword and ice armor", "taunt"),
           ("M005", "심해의 기사", "", ""),
           (None, "물의 무녀", "a water shrine maiden summoning a swirling water dragon", "heal+draw")])
add(E, 4, [(None, "거대 소라게", "a giant hermit crab living in a ruined sea temple shell", "taunt+regen"),
           (None, "해마 기병", "a merman cavalry riding a giant seahorse", "weaken"),
           (None, "빙결 마도사", "a frost mage freezing the air with blue runes", "strike+weaken")])
add(E, 5, [(None, "빙하 늑대", "a glacier wolf with ice crystal fur", "regen"),
           (None, "세이렌", "a siren singing on a rock in a stormy sea", "weaken+draw"),
           (None, "심해 거북왕", "a deep sea turtle king with an island on its back", "taunt+fortify")])
add(E, 6, [(None, "트리톤 전사", "a triton warrior with a trident and conch horn", "taunt+heal"),
           (None, "얼음 여왕의 시녀", "an ice queen's handmaiden in a crystal gown", "weaken+fortify"),
           (None, "해룡", "a long blue sea serpent coiling out of the waves", "chain+heal")])
add(E, 7, [(None, "크라켄", "a giant kraken grabbing a ship with tentacles", "taunt+weaken"),
           (None, "빙결 여제", "a frost empress on an ice throne", "strike+fortify"),
           ("M006", "레비아탄", "", "destroy")])
add(E, 8, [(None, "심해 고래왕", "a colossal whale king with glowing runes in the abyss", "taunt+heal+fortify"),
           (None, "빙룡", "an ice dragon breathing freezing blizzards", "quake+weaken"),
           (None, "해신의 창 기사", "a champion of the sea god with a golden trident", "rally+heal")])
add(E, 9, [(None, "절대영도 거인", "an absolute zero frost giant", "taunt+weaken"),
           (None, "심연의 여신", "an abyss goddess with flowing dark-blue hair and pearls", "revive+heal"),
           (None, "해왕 넵투누스", "the sea king with a coral crown and trident on a wave chariot", "destroy+heal")])
add(E, 10, [(None, "영겁의 빙하신", "an eternal glacier god carved from ancient ice", "taunt+fortify+weaken"),
            (None, "원초의 해룡", "a primordial sea dragon larger than the ocean", "quake+heal"),
            (None, "바다의 어머니 티아마", "Tiamat, mother of the seas, a serene sea goddess-dragon", "destroy+draw")])

# ───────── 바람 (Wind) : 주사위 4회 · 연속 공격 ─────────
E = 'Wind'
add(E, 1, [("M007", "질풍 매", "", "double"),
           (None, "바람 요정", "a tiny wind sylph riding a swirl of leaves", "draw"),
           (None, "회오리 다람쥐", "a flying squirrel gliding on a whirlwind", "haste")])
add(E, 2, [(None, "깃털 여우", "a fox with feathered tails floating in the breeze", ""),
           (None, "숲의 궁수", "an elf archer shooting arrows of wind in a forest", "strike"),
           (None, "바람 닌자", "a ninja leaping through the air with green scarves", "double")])
add(E, 3, [(None, "폭풍 까마귀", "a storm crow cawing in swirling winds", "draw"),
           (None, "하늘 해적", "a sky pirate on a flying ship's deck with a cutlass", "double"),
           (None, "회오리 무투가", "a martial artist monk spinning a tornado kick", "haste+strike")])
add(E, 4, [(None, "바람 정령사", "a wind shaman with feathered staff calling the wind", "draw+fortify"),
           ("M008", "폭풍 검사", "", "double"),
           (None, "그린 와이번", "a green wyvern swooping over a valley", "double+strike")])
add(E, 5, [(None, "질풍 검객", "a gale swordsman dashing with afterimages", "double"),
           (None, "천공의 기마궁수", "a pegasus rider archer in the sky", "strike+draw"),
           (None, "회오리 거인", "a giant made of a living tornado", "quake")])
add(E, 6, [(None, "하늘 고래", "a gentle sky whale swimming through clouds", "draw+heal"),
           (None, "바람의 마검사", "a spellblade with a sword wrapped in wind", "double+strike"),
           (None, "로크", "a gigantic roc bird carrying off a boulder", "chain")])
add(E, 7, [(None, "질풍의 여왕", "a harpy queen with emerald wings", "haste+draw"),
           (None, "천풍 검성", "a heavenly wind sword saint slicing clouds", "double+strike"),
           (None, "폭풍 용", "a green storm dragon inside a hurricane", "chain+quake")])
add(E, 8, [(None, "천마 페가수스 왕", "a regal white pegasus king with golden armor", "haste+draw"),
           (None, "바람의 대정령", "a great wind spirit of swirling emerald light", "quake+draw"),
           (None, "태풍의 눈", "a colossus standing in the eye of a typhoon", "double+chain")])
add(E, 9, [(None, "폭풍의 지배자", "a storm ruler commanding tornadoes", "rally+haste"),
           (None, "하늘의 용왕", "a sky dragon king with feathered wings above the clouds", "double+draw"),
           (None, "풍신", "the wind god carrying a giant bag of winds", "destroy+haste")])
add(E, 10, [(None, "영겁의 태풍", "an eternal typhoon elemental with lightning inside", "quake+chain"),
            (None, "원초의 풍룡", "a primordial wind dragon spanning the sky", "double+strike"),
            (None, "창공신 아에로스", "the sky god Aeros with vast wings of wind", "destroy+haste")])

# ───────── 대지 (Earth) : 방어형 · 도발 ─────────
E = 'Earth'
add(E, 1, [(None, "이끼 버섯돌이", "a cute mossy mushroom creature with a stone club", "taunt"),
           (None, "진흙 골렘", "a small mud golem with pebble eyes", "regen"),
           (None, "두더지 광부", "a mole miner with a pickaxe and helmet lamp", "draw")])
add(E, 2, [("M009", "바위 골렘", "", "taunt"),
           (None, "드워프 전사", "a stout dwarf warrior with an axe and braided beard", "taunt"),
           (None, "갑옷 아르마딜로", "an armadillo rolled in rock-hard armor plates", "fortify")])
add(E, 3, [(None, "바위 거북", "a rock tortoise with a mountain shell", "taunt"),
           (None, "사암 수호병", "a sandstone statue guardian with a spear", "taunt+guardall"),
           (None, "어린 트렌트", "a young treant sapling with glowing leaves", "regen+heal")])
add(E, 4, [(None, "곰 전사", "a bear warrior wearing bark armor", "rally"),
           ("M010", "대지의 수호자", "", "taunt+rally"),
           (None, "수정 골렘", "a crystal golem with amethyst spikes", "taunt+fortify")])
add(E, 5, [(None, "대지 코뿔소", "an earth rhino with stone horn charging", "strike"),
           (None, "드워프 왕", "a dwarf king with a runic hammer and golden beard", "rally+fortify"),
           (None, "고대 트렌트", "an ancient treant with a face in its trunk", "taunt+regen")])
add(E, 6, [(None, "석상 수문장", "a giant stone statue gatekeeper at a temple gate", "taunt"),
           (None, "산악 거인", "a mountain giant with a cliff for a shoulder", "quake"),
           (None, "대지룡", "an earth dragon with rocky scales burrowing out of the ground", "taunt+strike")])
add(E, 7, [(None, "대지모신의 사제", "a priestess of the earth mother with vines and flowers", "heal+fortify"),
           (None, "용암석 타이탄", "a basalt titan with glowing cracks", "taunt+quake"),
           (None, "세계수의 수호자", "a guardian of the world tree, an ancient elk spirit", "taunt+fortify+heal")])
add(E, 8, [(None, "황금 골렘", "a golden golem encrusted with jewels", "taunt+fortify"),
           (None, "대산맥의 왕", "the king of mountains wearing a crown of peaks", "rally+fortify"),
           (None, "대지의 고룡", "an ancient earth dragon covered in forests", "taunt+quake")])
add(E, 9, [(None, "대륙 거북", "a continent-sized turtle carrying a kingdom", "taunt+heal"),
           (None, "금강 거신", "an adamantine colossus of diamond", "taunt+fortify"),
           (None, "가이아의 화신", "an avatar of Gaia, a goddess made of earth and blossoms", "destroy+heal")])
add(E, 10, [(None, "세계의 기둥", "a titan holding up the sky like a pillar", "taunt+fortify"),
            (None, "원초의 지룡", "a primordial earth dragon whose body is a mountain range", "quake+taunt"),
            (None, "대지신 테라", "the earth god Terra with a crown of stone and roots", "destroy+fortify")])

# ───────── 번개 (Thunder) : 유리 대포 · 연쇄 ─────────
E = 'Thunder'
add(E, 1, [("M011", "스파크 여우", "", "hidden"),
           (None, "번개 쥐", "a cute yellow mouse crackling with electricity", "burn"),
           (None, "전격 박쥐", "a bat with lightning wings", "strike")])
add(E, 2, [(None, "번개 늑대", "a wolf with electric blue fur and sparks", ""),
           (None, "뇌전 궁수", "an archer firing lightning arrows", "strike"),
           (None, "뇌운 소녀", "a girl riding a small thundercloud", "burn+hidden")])
add(E, 3, [(None, "볼트 매", "a hawk diving with a lightning trail", "chain"),
           (None, "전격 창병", "a lancer with a crackling electric spear", "strike"),
           (None, "번개 도깨비", "a goblin oni beating thunder drums", "quake")])
add(E, 4, [(None, "전광 표범", "a lightning panther dashing in a blur", "chain"),
           (None, "뇌전 마도사", "a storm mage summoning lightning bolts", "strike+burn"),
           (None, "천둥새", "a thunderbird with storm clouds under its wings", "quake+chain")])
add(E, 5, [(None, "번개 기사", "a knight in electrified armor with a lightning sword", "chain"),
           (None, "어린 뇌룡", "a young thunder dragon with crackling horns", "strike+hidden"),
           ("M012", "뇌신 라이자", "", "chain")])
add(E, 6, [(None, "전격의 무녀", "a shrine maiden with lightning talismans", "burn+strike"),
           (None, "뇌광 기린", "a thunder qilin galloping on clouds", "chain+burn"),
           (None, "천둥 거인", "a thunder giant hurling lightning bolts", "quake")])
add(E, 7, [(None, "번개 군주", "a lightning lord on a throne of storm clouds", "chain+strike"),
           (None, "폭풍 뇌룡", "a storm thunder dragon roaring lightning", "quake+burn"),
           (None, "뇌정 검성", "a thunder sword saint drawing a blade of lightning", "chain+double")])
add(E, 8, [(None, "뇌전 그리폰", "a thunder griffin with electric feathers", "chain"),
           (None, "망치의 뇌전사", "a warrior with a giant thunder hammer", "quake+strike"),
           (None, "천벌의 사도", "an apostle of divine punishment calling down lightning", "chain+burn")])
add(E, 9, [(None, "번개 폭군", "a lightning tyrant in black and gold armor", "chain+strike"),
           (None, "천뢰의 용왕", "a heavenly thunder dragon king in a storm", "quake+chain"),
           (None, "뇌신 인드라", "a thunder god with a vajra weapon on a white elephant", "destroy+burn")])
add(E, 10, [(None, "심판의 번개신", "a god of judgment lightning with glowing eyes", "chain+quake"),
            (None, "원초의 뇌룡", "a primordial thunder dragon made of pure lightning", "strike+chain"),
            (None, "뇌제", "the thunder emperor wearing a crown of lightning", "destroy+chain")])

# ───────── 빛 (Light) : 코인 앞면 강화 · 회복/드로우 ─────────
E = 'Light'
add(E, 1, [("M013", "빛의 견습사제", "", "draw"),
           (None, "반딧불 요정", "a firefly fairy glowing in the night", "heal"),
           (None, "아기 유니콘", "a baby unicorn with a sparkling horn", "regen")])
add(E, 2, [(None, "빛의 정령", "a floating wisp spirit of warm light", ""),
           (None, "수련 성기사", "a novice paladin with a small shield and holy sword", "taunt"),
           (None, "흰 비둘기 수호령", "a white dove guardian spirit with a halo", "heal+draw")])
add(E, 3, [(None, "치유의 수녀", "a healing nun with a glowing staff", "heal"),
           (None, "황금 사자", "a golden lion radiating light", "rally"),
           ("M014", "성기사 아르테", "", "taunt+heal")])
add(E, 4, [(None, "빛의 궁수", "an archer firing arrows of light", "strike"),
           (None, "유니콘 기사", "a knight riding a white unicorn", "double"),
           (None, "견습 대천사", "an apprentice archangel with small white wings", "revive+heal")])
add(E, 5, [(None, "성스러운 그리폰", "a holy griffin with white and gold feathers", "fortify"),
           (None, "발키리", "a valkyrie with winged helmet and spear", "strike+heal"),
           (None, "성녀", "a saint praying with light pouring from the sky", "heal+draw")])
add(E, 6, [(None, "태양의 성기사", "a sun paladin with a sunburst shield", "taunt+fortify"),
           (None, "대천사", "an archangel with six wings and a flaming sword", "strike+rally"),
           (None, "빛의 성룡", "a holy white dragon in rays of light", "heal+regen")])
add(E, 7, [(None, "빛의 거인", "a colossus of light standing over a city", "taunt+heal"),
           (None, "천상의 기사단장", "a celestial knight commander on a pegasus", "rally+fortify"),
           (None, "치천사 세라핌", "a seraph angel with burning wings", "quake+heal")])
add(E, 8, [(None, "성검의 영웅", "a hero lifting a holy sword surrounded by light", "strike+draw"),
           (None, "백금룡", "a platinum dragon with gleaming scales", "taunt+regen"),
           (None, "천상의 여왕", "a celestial queen with a halo crown", "revive+heal+draw")])
add(E, 9, [(None, "구원의 여신", "a goddess of salvation reaching out her hand", "heal+fortify"),
           (None, "성광룡왕", "a holy light dragon king", "quake+heal"),
           (None, "대천사장 루미엘", "archangel lord Lumiel with a spear of light", "destroy+heal")])
add(E, 10, [(None, "영원의 태양신", "an eternal sun deity", "rally+heal"),
            (None, "원초의 성룡", "a primordial holy dragon of pure light", "revive+fortify"),
            (None, "광명신", "the god of radiance", "destroy+draw")])

# ───────── 어둠 (Dark) : 코인 뒷면 약화 · 고스탯 · 암살 ─────────
E = 'Dark'
add(E, 1, [(None, "그림자 박쥐", "a shadow bat with red eyes", ""),
           (None, "꼬마 유령", "a cute little ghost with a lantern", "hidden"),
           (None, "해골 병사", "a skeleton soldier with a rusty sword", "regen")])
add(E, 2, [(None, "구울", "a hunched ghoul in a graveyard", ""),
           ("M015", "그림자 암살자", "", "hidden+assassin"),
           (None, "견습 마녀", "an apprentice witch with a cat and a crooked staff", "weaken")])
add(E, 3, [(None, "저주 인형", "a creepy cursed doll with stitches", "weaken"),
           (None, "밤의 늑대인간", "a werewolf howling at the moon", "double"),
           (None, "해골 마법사", "a skeleton mage casting purple necrotic magic", "revive+strike")])
add(E, 4, [(None, "그림자 표범", "a shadow panther emerging from darkness", "assassin+hidden"),
           (None, "어둠의 기사", "a dark knight in black armor with a cursed sword", "strike"),
           (None, "흡혈귀 백작", "a vampire count in a red-lined cape", "heal+weaken")])
add(E, 5, [(None, "몽마", "a nightmare demon (succubus) in a dreamy purple mist", "weaken+hidden"),
           (None, "데스나이트", "a death knight on an undead horse", "strike+regen"),
           (None, "사신", "the grim reaper with a giant scythe", "assassin+strike")])
add(E, 6, [(None, "지옥의 기수", "a hell rider with a flaming skull on a nightmare horse", "double+assassin"),
           (None, "리치", "a lich with a phylactery and a crown of bones", "revive+weaken"),
           (None, "어둠의 용", "a dark dragon with purple flames", "quake")])
add(E, 7, [(None, "마왕의 장군", "a demon general with horns and a massive cleaver", "rally+strike"),
           (None, "흡혈귀 여왕", "a vampire queen on a blood-red throne", "heal+weaken+assassin"),
           ("M016", "공허의 군주", "", "destroy")])
add(E, 8, [(None, "해골룡", "a bone dragon rising from a graveyard", "regen+quake"),
           (None, "심연의 마왕", "an abyss demon lord with huge wings", "weaken+strike"),
           (None, "죽음의 여신", "a goddess of death with a veil and black roses", "destroy+weaken")])
add(E, 9, [(None, "영혼 수확자", "a soul harvester collecting glowing souls", "assassin+double"),
           (None, "혼돈의 마룡", "a chaos dragon with mismatched wings", "quake+weaken"),
           (None, "명계의 왕", "the king of the underworld on a throne of skulls", "destroy+revive")])
add(E, 10, [(None, "종말의 그림자", "a giant shadow of the end of the world", "assassin+quake"),
            (None, "원초의 흑룡", "a primordial black dragon swallowing the stars", "destroy+weaken"),
            (None, "마신", "the demon god with countless eyes and wings", "destroy+quake")])

# ───────── 무지개 (Rainbow) : 균형형 · 만능 ─────────
E = 'Rainbow'
add(E, 1, [(None, "프리즘 슬라임", "a cute rainbow prism slime", ""),
           (None, "무지개 나비", "a butterfly with rainbow wings", "draw"),
           (None, "별사탕 요정", "a tiny fairy holding a star candy", "heal")])
add(E, 2, [(None, "유리구슬 골렘", "a golem made of rainbow glass marbles", "taunt"),
           (None, "칠색 앵무", "a seven-colored parrot", "draw"),
           (None, "오로라 여우", "a fox with aurora-colored tails", "strike")])
add(E, 3, [(None, "칠보 거북", "a turtle with a jeweled shell of seven gems", "taunt+heal"),
           (None, "프리즘 마도사", "a prism mage splitting light into rainbow beams", "strike+draw"),
           ("M017", "무지개 요정", "", "draw")])
add(E, 4, [(None, "크리스탈 기사", "a knight in crystal armor reflecting rainbows", "taunt"),
           (None, "오로라 정령", "an aurora spirit dancing in the polar sky", "fortify+draw"),
           (None, "무지개 유니콘", "a winged unicorn with a rainbow mane", "double")])
add(E, 5, [(None, "별의 무희", "a star dancer with constellations on her dress", "rally"),
           (None, "칠색 공작왕", "a seven-colored peacock king", "weaken+draw"),
           (None, "프리즘 드래곤", "a young prism dragon with crystal scales", "strike+fortify")])
add(E, 6, [(None, "칠성 검사", "a swordsman with a blade bearing seven stars", "double"),
           (None, "은하 고래", "a whale swimming through a galaxy", "heal+draw"),
           (None, "오로라 여제", "an aurora empress with a gown of northern lights", "rally+fortify")])
add(E, 7, [(None, "별빛 마법사", "a starlight archmage casting a constellation", "quake+draw"),
           (None, "무지개 다리의 수호자", "the guardian of the rainbow bridge with a horn", "taunt+fortify"),
           (None, "성운룡", "a nebula dragon of swirling cosmic colors", "strike+chain")])
add(E, 8, [(None, "은하 기사", "a galaxy knight with armor full of stars", "rally+strike"),
           (None, "칠색 봉황", "a seven-colored phoenix", "regen+heal"),
           ("M018", "세븐 스트라이커", "", "double")])
add(E, 9, [(None, "차원 방랑자", "a dimension wanderer stepping through portals", "haste+draw"),
           (None, "별의 여신", "a star goddess holding a galaxy", "revive+fortify"),
           (None, "무지개 용왕", "a rainbow dragon king", "destroy+rally")])
add(E, 10, [(None, "세븐 오리진", "the seven origin, seven elemental crystals forming a being", "rally+fortify+draw"),
            (None, "우주의 의지", "the will of the cosmos, an entity of stars", "quake+draw"),
            (None, "창세의 칠색룡", "a genesis rainbow dragon creating the universe", "destroy+double")])
