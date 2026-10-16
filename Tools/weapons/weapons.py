# -*- coding: utf-8 -*-
"""영웅 무기 정의 (VARCO). python weapons.py → weapons.json
key: 파일 이름(Heroes/<hero>/weapon_<slot>.fbx), hand R/L, kind: blade(손에서 앞으로) | pole(팔과 나란히 세움) | bow | shield
length = 캐릭터 키(1) 대비 무기 길이, grip = 무기 아래에서 손잡이까지 비율"""
import json
W = {
 'H001': [dict(slot='R', hand='R', kind='blade', length=0.62, grip=0.12, anim='sword_slash', desc='crimson-and-gold flame longsword with a glowing red blade edged with stylized flame engravings and a gold crossguard')],
 'H002': [dict(slot='R', hand='R', kind='pole', length=0.95, grip=0.42, anim='staff_spin_attack', desc='elegant coral-and-silver priestess staff topped with a glowing sea-blue pearl orb held inside curling coral branches')],
 'H003': [dict(slot='R', hand='R', kind='blade', length=0.55, grip=0.15, anim='landing_smash', desc='heavy bronze dwarven battle axe with one wide crescent blade, runic engravings and a leather-wrapped wooden handle'),
          dict(slot='L', hand='L', kind='shield', length=0.5, grip=0.5, anim=None, desc='large rectangular carved stone tower shield with a thick bronze rim and a glowing amber rune in the center')],
 'H004': [dict(slot='R', hand='R', kind='pole', length=1.0, grip=0.45, anim='spinning_slash', desc='dark shadow reaper scythe with a long black curved blade glowing purple along the edge and a twisted black shaft')],
 'H005': [dict(slot='R', hand='R', kind='pole', length=0.95, grip=0.42, anim='gather_hands_magic', desc='holy white-and-gold saint staff topped with a radiant clear crystal framed by small golden angel wings')],
 'H006': [dict(slot='R', hand='R', kind='blade', length=0.68, grip=0.12, anim='combo_slash', desc='heroic rainbow longsword with a prismatic crystal blade shifting through rainbow colors and a white-gold hilt')],
 'H007': [dict(slot='R', hand='R', kind='pole', length=0.9, grip=0.42, anim='magic_ground_attack_1', desc='fire witch staff of twisted dark wood ending in a burning ember crystal surrounded by curling flame shapes')],
 'H008': [dict(slot='R', hand='R', kind='pole', length=1.05, grip=0.4, anim='jump_stab_land', desc='silver-blue sea knight trident with three sharp prongs and an aquamarine gem at the base of the prongs')],
 'H009': [dict(slot='L', hand='L', kind='bow', length=0.75, grip=0.5, anim='arm_stretch_magic', desc='elegant emerald elven longbow with leaf-shaped carved limbs and a glowing green bowstring')],
 'H010': [dict(slot='R', hand='R', kind='blade', length=0.55, grip=0.14, anim='double_slash', desc='curved jade-green wind saber with a thin single-edged blade and a teal wrapped hilt'),
          dict(slot='L', hand='L', kind='blade', length=0.55, grip=0.14, anim=None, desc=None, same='R')],
 'H012': [dict(slot='R', hand='R', kind='pole', length=0.9, grip=0.42, anim='magic_left_hand_attack', desc='lightning mage staff of purple and gold metal topped with a crackling yellow lightning orb inside a golden ring')],
 'H013': [dict(slot='R', hand='R', kind='blade', length=0.62, grip=0.12, anim='magic_ground_slam', desc='massive stone war hammer with a rough boulder head embedded with glowing amber crystals and a thick stone handle')],
 'H014': [dict(slot='R', hand='R', kind='blade', length=0.64, grip=0.12, anim='spinning_slash', desc='radiant holy knight longsword with a white-gold blade, a winged golden crossguard and a glowing sun gem in the hilt')],
 'H015': [dict(slot='R', hand='R', kind='pole', length=0.92, grip=0.42, anim='magic_pull', desc='cursed witch staff of black twisted metal topped with a glowing violet amethyst crystal wrapped in thin dark chains')],
 'H016': [dict(slot='R', hand='R', kind='blade', length=0.75, grip=0.1, anim='sword_slash', desc='prismatic crystal greatsword with a long translucent blade refracting rainbow light and a dark silver hilt')],
}
def prompt(w):
    if w['kind'] == 'shield':
        pose = 'standing upright with its front face toward the camera'
    elif w['kind'] == 'bow':
        pose = 'standing perfectly upright and vertical, the bow limbs pointing up and down'
    else:
        pose = 'standing perfectly upright and vertical with the tip or head pointing up and the handle at the bottom'
    return (f"3D game asset concept of a single {w['desc']}, stylized anime fantasy game style matching high-quality hand-painted RPG weapons, "
            f"isolated object {pose}, centered, the whole object fully visible with margin, front view, clean even studio lighting, "
            "plain light gray background, no hands, no character, no text, no shadow, no extra objects.")
for h, ws in W.items():
    for w in ws:
        if w.get('desc'): w['prompt'] = prompt(w)
json.dump(W, open('weapons.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(sum(1 for ws in W.values() for w in ws if w.get('desc')), 'weapon meshes')
