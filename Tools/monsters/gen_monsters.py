# -*- coding: utf-8 -*-
"""몬스터 270종 → Assets/StrikerZ/Scripts/Data/MonsterSet.cs + monsters.json (아트 프롬프트용)"""
import json, math, os, sys
sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from monsters_table import T

EX = json.load(open(os.path.join(HERE, 'existing.json'), encoding='utf-8'))
OUT_CS = 'C:/Users/MBC-501-22/Desktop/Card/Assets/StrikerZ/Scripts/Data/MonsterSet.cs'

RT = {1: 'N N R', 2: 'N R R', 3: 'N R SR', 4: 'R SR SR', 5: 'R SR SSR', 6: 'SR SR SSR',
      7: 'SR SSR UR', 8: 'SR SSR UR', 9: 'SSR SSR UR', 10: 'SSR UR UR'}
S_T = {1: 6, 2: 7, 3: 9, 4: 10, 5: 12, 6: 13, 7: 15, 8: 16, 9: 18, 10: 20}
BONUS = {'N': 0, 'R': 1, 'SR': 2, 'SSR': 2, 'UR': 3}
# 카드 등급 표기 (2026-10-01 개편): 내부 밸런스 단계 N·R·SR·SSR·UR → Common·Uncommon·Rare
OUTR = {'N': 'Common', 'R': 'Common', 'SR': 'Uncommon', 'SSR': 'Rare', 'UR': 'Rare'}
VAL = {'N': 2.0, 'R': 2.5, 'SR': 3.0, 'SSR': 3.5, 'UR': 4.0}


# 2026-10-12 동시 룰 개편: 소환 가능 타임을 T1~10 → T1~7 로 압축 (가장 강한 소환수 = T7). 스탯·등급은 원래 단계 그대로.
MAPT = {1: 1, 2: 2, 3: 2, 4: 3, 5: 4, 6: 4, 7: 5, 8: 6, 9: 6, 10: 7}
# 폐지·의미 없어진 능력 → 새 능력 (연속·연쇄 공격 폐지, 추가 공격 폐지, 뒷면 소환은 모두 기본)
REPLACE = {'double': 'snipe', 'chain': 'vanguard', 'haste': 'rally', 'hidden': 'resist'}


def rhalf(x):
    return int(math.floor(x + 0.5))


def mags(t):
    return dict(burn=1 + t // 2, heal=2 + t // 2, draw=2 if t >= 8 else 1, strike=1 + t // 2,
                quake=1 + t // 4, rally=1 + t // 4, weaken=1 + t // 4, fortify=1 + t // 3)


def ability_cost(elem, t, keys):
    m = mags(t)
    c = 0.0
    for k in keys:
        c += {'taunt': 0 if elem in ('Water', 'Earth') else 1, 'chain': 1, 'assassin': 0.5, 'hidden': 0, 'regen': 1,
              'max4': -1, 'double': 0, 'snipe': 1, 'vanguard': 1.5, 'resist': 0.5, 'burn': 0.5 * m['burn'], 'heal': 0.35 * m['heal'], 'draw': 1.5 * m['draw'],
              'strike': 0.6 * m['strike'], 'quake': 1.3 * m['quake'], 'rally': m['rally'], 'weaken': m['weaken'],
              'fortify': 0.8 * m['fortify'], 'destroy': 3.5, 'haste': 2, 'revive': 2, 'guardall': 1}[k]
    return c


def stats(elem, t, k, rarity, keys):
    cost = ability_cost(elem, t, keys)
    cost = min(cost, 0.3 * S_T[t]) if cost > 0 else cost
    tot = max(2, rhalf(S_T[t] + BONUS[rarity] - cost))
    if elem in ('None', 'Rainbow'):
        atk = (tot + 1) // 2 if k % 2 == 0 else tot // 2
        hp = tot - atk
    elif elem == 'Fire':
        atk = rhalf(tot * 2 / 3); hp = tot - atk
    elif elem == 'Water':
        hp = rhalf(tot * 2 / 3); atk = tot - hp
    elif elem == 'Wind':
        tw = tot - 1
        atk = (tw + 1) // 2 if k % 2 == 0 else tw // 2
        hp = tw - atk
    elif elem == 'Thunder':
        hp = 1 + (t - 1) // 3; atk = tot - hp
    elif elem == 'Earth':
        atk = 1 + (t - 1) // 3; hp = tot - atk
    elif elem == 'Light':
        tw = tot - 2; atk = (tw + 1) // 2; hp = tw - atk
    elif elem == 'Dark':
        tw = tot + 4; atk = (tw + 1) // 2; hp = tw - atk
    return max(1, atk), max(1, hp)


W = dict(atk=1, weaken=1, hp=1, guard=2, pierce=0.75, heal=0.6, draw=1.5, burn=0.8, wound=1)
CS = dict(atk='Atk', weaken='Weaken', hp='Shield', guard='Guard', pierce='Pierce', heal='Heal', draw='Draw', burn='Burn', wound='Wound')
# 2026-10-28 주사위 1개(바람 2개): 고를 수 없으니 세 구간 모두 "그 전투에 바로 쓰이는 것"(atk·hp·weaken) 또는
# "언제나 이득인 것"(burn·draw)을 주 효과로. 회복(heal)만 있는 구간은 영웅 HP 가 가득이면 허탕이라 부효과로만 둔다.
BANDS = {
    'Fire': [('atk', 'pierce'), ('burn', 'atk'), ('wound', 'atk')],
    'Water': [('hp', 'heal'), ('draw', 'hp'), ('weaken', 'hp')],
    'Wind': [('atk', 'draw'), ('pierce', 'atk'), ('draw', 'hp')],
    'Earth': [('guard', 'hp'), ('hp', 'heal'), ('weaken', 'hp')],
    'Thunder': [('atk', 'burn'), ('burn', 'pierce'), ('weaken', 'atk')],
    'Light': [('hp', 'heal'), ('draw', 'hp'), ('atk', 'heal')],
    'Dark': [('wound', 'pierce'), ('weaken', 'burn'), ('pierce', 'atk')],
    'None': [('atk', 'hp'), ('hp', 'atk'), ('guard', 'atk')],
    'Rainbow': [('draw', 'atk'), ('atk', 'hp'), ('hp', 'heal')],
}


def term(kind, n, first):
    name = CS[kind]
    arg = '' if kind in ('guard', 'wound') else str(n)
    return f"Dice.{name}({arg})" if first else f".{name}({arg})"


def band(prim, sec, v):
    w = W[prim]
    n = 1 if prim in ('guard', 'wound') else max(1, int(v // w))
    left = v - (w if prim in ('guard', 'wound') else n * w)
    s = term(prim, n, True)
    score = v - left
    if left >= 0.5 and sec != prim:
        w2 = W[sec]
        if sec in ('guard', 'wound'):
            if left >= w2 * 0.75: s += term(sec, 1, False); score += w2
        else:
            m = max(1, rhalf(left / w2)); s += term(sec, m, False); score += m * w2
    return s, score


def dice_for(elem, t, k, rarity):
    v = VAL[rarity] + 0.15 * (t - 1)
    sets = BANDS[elem]
    r = (t + k) % 3
    sets = sets[r:] + sets[:r]
    parts, scores = [], []
    for p, s2 in sets:
        b, sc = band(p, s2, v)
        parts.append(b); scores.append(sc)
    return 'Dice.Of(' + ', '.join(parts) + ')', scores


def ability_cs(elem, t, keys):
    m = mags(t)
    flags, texts, effs = [], [], []
    for k in keys:
        if k == 'taunt': flags.append('taunt = true'); texts.insert(0, '[도발]')
        elif k == 'double': flags.append('attackCount = 2'); texts.append('[연속 공격] 배틀페이즈에 2회 공격한다.')
        elif k == 'chain': flags.append('attackAll = true'); texts.append('[연쇄] 전투로 상대 몬스터를 파괴하면 다음 몬스터를 이어서 공격한다.')
        elif k == 'assassin': flags.append('noCounter = true'); texts.append('[암살] 직접 공격할 때 영웅의 반격을 받지 않는다.')
        elif k == 'hidden': flags.append('canSummonFaceDown = true'); texts.append('뒷면 세로 표시로 소환할 수 있다.')
        elif k == 'regen': flags.append('regen = true'); texts.append('[재생] 배틀 엔드에 HP를 모두 회복한다.')
        elif k == 'max4': flags.append(f'maxTime = {MAPT[t] + 2}'); texts.append(f'T{MAPT[t] + 2}까지만 소환할 수 있다.')
        elif k == 'snipe': flags.append('snipe = true'); texts.append('[저격] 가장 뒤(오른쪽) 상대 소환수를 공격한다.')
        elif k == 'vanguard': flags.append('vanguard = true'); texts.append('[선봉] 공격 턴에 칸 순서보다 먼저 공개·공격한다.')
        elif k == 'resist': flags.append('resist = true'); texts.append('[불변] 상대 카드 효과(개기일식·데미지·약화·행동 불능·되돌리기 등)를 받지 않는다. 파괴 효과와 전투로만 쓰러진다. 개기일식 중에도 싸운다.')
        elif k == 'burn': effs.append(f"Effects.Burn({m['burn']})"); texts.append(f"소환 시 상대에게 {m['burn']} 데미지.")
        elif k == 'heal': effs.append(f"Effects.Heal({m['heal']})"); texts.append(f"소환 시 HP {m['heal']} 회복.")
        elif k == 'draw': effs.append(f"Effects.Draw({m['draw']})"); texts.append(f"소환 시 카드 {m['draw']}장 드로우.")
        elif k == 'strike': effs.append(f"Effects.StrikeEnemy({m['strike']})"); texts.append(f"소환 시 상대 몬스터 1장에게 {m['strike']} 데미지.")
        elif k == 'quake': effs.append(f"Effects.DamageAllEnemies({m['quake']})"); texts.append(f"소환 시 상대 몬스터 전체에 {m['quake']} 데미지.")
        elif k == 'rally': effs.append(f"Effects.BuffAllies({m['rally']})"); texts.append(f"소환 시 이번 타임 아군 몬스터 ATK +{m['rally']}.")
        elif k == 'weaken': effs.append(f"Effects.AddStatus(StatusKind.AtkBonus, Expire.NextBattleEnd, onOpponent: true, value: -{m['weaken']})"); texts.append(f"소환 시 다음 배틀 엔드까지 상대 몬스터 ATK -{m['weaken']}.")
        elif k == 'fortify': effs.append(f"Effects.FortifyAllies({m['fortify']})"); texts.append(f"소환 시 이번 타임 아군 몬스터 HP +{m['fortify']}.")
        elif k == 'destroy': effs.append('Effects.DestroyEnemyMonster(1)'); texts.append('소환 시 상대 몬스터 1장을 파괴한다.')
        elif k == 'haste': effs.append('Effects.Haste(0, 1)'); texts.append('소환 시 아군 몬스터 1장이 이번 타임 추가 공격 +1.')
        elif k == 'revive': effs.append('Effects.ReturnFromGrave(1, 0)'); texts.append('소환 시 묘지의 몬스터 1장을 패로 가져온다.')
        elif k == 'guardall': effs.append('Effects.GrantTaunt()'); texts.append('소환 시 아군 몬스터 1장에게 이번 타임 [도발] 부여.')
        else: raise ValueError(k)
    eff = None
    if len(effs) == 1: eff = effs[0]
    elif effs: eff = 'Effects.Seq(' + ', '.join(effs) + ')'
    return flags, ' '.join(texts), eff


def main():
    assert len(T) == 270, len(T)
    lines, js = [], []
    nid = 200
    stats_by_el = {}
    for e in T:
        t, k, el = e['t'], e['k'], e['elem']
        rar = RT[t].split()[k]
        raw = [x for x in e['ab'].split('+') if x]
        keys = [REPLACE.get(x, x) for x in raw]
        changed = raw != keys
        atk, hp = stats(el, t, k, rar, keys)
        flags, text, eff = ability_cs(el, t, keys)
        if e['id']:
            mid = e['id']; ex = EX[mid]
            assert ex['name'] == e['ko'], (mid, ex['name'], e['ko'])
            dice = ex['dice']
            rest = ex['rest'].lstrip(', ').strip()
            if not changed:
                text = ex['text']
                eff = rest[len('effect = '):] if rest.startswith('effect = ') else None
        else:
            nid += 1; mid = f'M{nid}'
            dice, _ = dice_for(el, t, k, rar)
        f = ', '.join(flags)
        parts = [f'id = "{mid}"', f'name = "{e["ko"]}"', 'type = CardType.Monster', f'element = Element.{el}', f'rarity = Rarity.{OUTR[rar]}',
                 f'atk = {atk}', f'hp = {hp}', f'minTime = {MAPT[t]}']
        if f: parts.append(f)
        parts.append(f'dice = {dice}')
        if eff: parts.append(f'effect = {eff}')
        parts.append(f'text = "{text}"')
        lines.append('            new CardData { ' + ', '.join(parts) + ' },')
        js.append(dict(id=mid, ko=e['ko'], en=e['en'], elem=el, t=t, rarity=rar, atk=atk, hp=hp, ab=e['ab'], new=not e['id']))
        stats_by_el.setdefault(el, []).append((t, rar, atk, hp, e['ab']))
    cs = ('using System.Collections.Generic;\n\nnamespace StrikerZ\n{\n'
          '    // ★ 자동 생성: Tools/monsters/gen_monsters.py (monsters_table.py 편집 후 python gen_monsters.py) — 몬스터 270종 (9속성 × T1~10 × 3)\n'
          '    // 스탯 기준 (T1): 무속성·무지개 3/3 · 화염 4/2 · 물 2/4 · 바람 주사위 2개 중 선택(합 -1) · 번개 5/1 · 대지 1/5\n'
          '    //                 빛 2/2 (코인 앞면 ATK·HP +2) · 어둠 5/5 (코인 뒷면 ATK·HP -2). 타임·등급이 오르면 총합 증가, 능력은 스탯을 소모.\n'
          '    public static partial class CardList\n    {\n'
          '        public static readonly List<CardData> Monsters = new List<CardData>\n        {\n'
          + '\n'.join(lines) + '\n        };\n    }\n}\n')
    open(OUT_CS, 'w', encoding='utf-8').write(cs)
    json.dump(js, open(os.path.join(HERE, 'monsters.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('wrote', len(lines), 'new ids up to', f'M{nid}')
    for el, rows in stats_by_el.items():
        print(el, ' '.join(f"T{t}{r}:{a}/{h}" for t, r, a, h, ab in rows[:9]), '...', ' '.join(f"T{t}{r}:{a}/{h}" for t, r, a, h, ab in rows[-3:]))


main()
