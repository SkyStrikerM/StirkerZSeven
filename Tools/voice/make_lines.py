"""VARCO Voice 로 녹음할 대사 목록 만들기 → lines.json

- 영웅 27명 × (atk 반격 · hit 피격 · skill 영웅 능력)  → Voice/hero_{id}_{kind}
- 상점 NPC 고정 대사 (코드에서 자동 추출: 이름 등이 끼워지는 $"…" 대사는 제외) → Voice/npc_{화자}_{md5앞8}
  화자: rosie(로지) · lukon(루콘) · rohan(로한) · bella(벨라)
파일 키 규칙은 Scripts/View/Voice.cs 의 Key() 와 같다 (UTF-8 MD5 앞 8자리).
"""
import hashlib
import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
UI = os.path.abspath(os.path.join(HERE, '..', '..', 'Assets', 'StrikerZ', 'Scripts', 'UI'))

# 영웅: (이름, 성별/분위기 → 목소리 고를 때 참고, 반격, 피격, 능력)
HEROES = {
    'H001': ('레온', 'M 열혈 소년 주인공', '불꽃처럼 타올라라!', '크윽… 아직이야!', '홍염의 스트라이커, 간다!'),
    'H002': ('마리나', 'F 차분한 무녀', '파도여, 휩쓸어라.', '아앗… 괜찮아요.', '심해의 무녀여, 치유의 물결을!'),
    'H003': ('가론', 'M 굵은 수호자', '흐읍! 받아라!', '끄떡없다!', '대지의 방패, 모두를 지켜라!'),
    'H004': ('녹스', 'M 냉소적인 그림자 군주', '그림자에 삼켜져라.', '…제법이군.', '그림자 군주의 이름으로.'),
    'H005': ('루미나', 'F 상냥한 성녀', '빛이여, 길을 밝혀 주세요!', '꺄앗…!', '빛의 성녀여, 잠든 이들을 깨워라.'),
    'H006': ('제로', 'M 자신만만한 청년', '일곱 빛으로, 끝낸다!', '윽… 이 정도로는!', '세븐 스트라이커, 해방!'),
    'H007': ('이그니스', 'F 거침없는 불의 마녀', '재가 되어라!', '흥, 뜨겁지도 않아.', '업화여, 모든 걸 불살라라!'),
    'H008': ('카이', 'M 차분한 청년 기사', '물살을 가른다!', '흐읏… 막아냈어.', '해류 방벽, 전개!'),
    'H009': ('실피아', 'F 발랄한 엘프 궁수', '바람을 타고, 명중!', '아얏! 빗나가라니까!', '질풍의 화살, 한 발 더!'),
    'H010': ('가일', 'M 날카로운 검객', '폭풍처럼 베어 주마!', '칫… 얕았어.', '폭풍의 검, 길을 열어라!'),
    'H011': ('볼트', 'M 호쾌한 격투가', '뇌신의 주먹이다!', '크억! 짜릿한데!', '번개야, 부숴 버려라!'),
    'H012': ('라이카', 'F 재빠른 번개 소녀', '번쩍! 맞았지?', '꺅! 따끔하잖아!', '연쇄 전격, 이어진다!'),
    'H013': ('브론', 'M 느릿한 거인', '으랴아!', '…간지럽군.', '암반이여, 나를 따르라.'),
    'H014': ('세라핀', 'F 고결한 여기사', '성광의 검을 받아라!', '흐읏… 무릎 꿇지 않아!', '성광의 기사여, 모두에게 축복을!'),
    'H015': ('모르가나', 'F 요염한 저주 마녀', '후후, 고통을 맛봐.', '감히… 나를?', '저주여, 영원히 스며들어라.'),
    'H016': ('오메가', 'M 냉혹한 종언자', '종언이다.', '…무의미한 발버둥.', '칠색의 종언, 사라져라.'),
    'H017': ('카일', 'M 소년 주인공', '간다, 스트라이커즈!', '크윽… 아직 안 끝났어!', '척력 반사! 튕겨 내라!'),
    'H018': ('아리아', 'F 다정한 치유사', '물방울아, 힘을 빌려 줘!', '아… 아파라.', '치유의 물방울, 모두에게!'),
    'H019': ('베인', 'M 노련한 중년 전사', '폭풍의 눈을 보아라!', '흐음… 아직 멀었다.', '폭풍의 눈, 휘몰아쳐라!'),
    'H020': ('메릴', 'F 기품 있는 호수의 여왕', '얼어붙어라.', '…무례하군요.', '호수의 여왕이 명한다.'),
    'H021': ('테라', 'M 우직한 드워프', '도끼맛 좀 봐라!', '하! 그게 다냐?', '대지의 맹세, 꺾이지 않는다!'),
    'H022': ('제퍼', 'M 경쾌한 음유시인', '이 노래를 받아라!', '아이쿠, 음정이 나갔네.', '자유의 노래, 울려 퍼져라!'),
    'H023': ('루미엔', 'F 성스러운 사제', '성광이여, 내려라!', '흐윽… 빛은 꺼지지 않아요.', '성광의 축복을 그대들에게.'),
    'H024': ('에레보스', 'M 위압적인 마왕', '심연으로 떨어져라.', '크크… 그 정도인가.', '심연의 손아귀, 붙잡아라!'),
    'H025': ('바루스', 'M 음산한 회색 망령', '잿빛으로 물들어라…', '…아프지 않다.', '회색 잠식, 퍼져라.'),
    'H026': ('프리스마', 'F 신비로운 빛의 여신', '빛이 너를 가르리라.', '아… 별이 흔들려.', '창세의 빛이여, 피어나라!'),
    'H027': ('프리즘 카일', 'M 각성한 소년 주인공', '일곱 빛깔로 간다!', '크윽… 이 빛은 꺼지지 않아!', '일곱 빛깔의 검, 해방!'),
}


def key(text):
    return hashlib.md5(text.encode('utf-8')).hexdigest()[:8]


lines = []
for hid, (name, tone, atk, hit, skill) in HEROES.items():
    for kind, text in (('atk', atk), ('hit', hit), ('skill', skill)):
        lines.append({'file': f'hero_{hid}_{kind}', 'speaker': hid, 'name': name, 'tone': tone, 'kind': kind, 'text': text})

# ── NPC 고정 대사 자동 추출 ──
src = {n: open(os.path.join(UI, n), encoding='utf-8').read() for n in ['MainMenu.Shop.cs', 'MainMenu.Cosmetics.cs', 'MainMenu.ShopHall.cs', 'MainMenu.Upgrade.cs']}
lit = r'(?<![$@])"((?:[^"\\]|\\.)*)"'
npc = []


def add(speaker, text):
    if '{' in text or not text.strip():
        return
    if (speaker, text) not in npc:
        npc.append((speaker, text))


def array(file, name):
    m = re.search(r'static readonly string\[\] ' + name + r'\s*=\s*\{(.*?)\};', src[file], re.S)
    return re.findall(lit, m.group(1))


for t in array('MainMenu.Shop.cs', 'RosieGreet') + array('MainMenu.Shop.cs', 'RosiePoke'):
    add('rosie', t)
for t in array('MainMenu.Cosmetics.cs', 'LukonGreet') + array('MainMenu.Cosmetics.cs', 'LukonPoke'):
    add('lukon', t)
for t in array('MainMenu.ShopHall.cs', 'RohanPoke'):
    add('rohan', t)
for t in array('MainMenu.ShopHall.cs', 'BellaPoke'):
    add('bella', t)
# (bool rohan, "대사") 튜플 — ParentGreet 배열과 ParentsSay 호출
for f in src.values():
    for b, t in re.findall(r'\((true|false),\s*(?<![$@])"((?:[^"\\]|\\.)*)"\)', f):
        add('rohan' if b == 'true' else 'bella', t)
# 직접 호출: RosieSay("…") · LukonSay("…", "…")
for f in src.values():
    for args in re.findall(r'RosieSay\(([^;]*?)\);', f):
        if '$"' in args or '+' in args:
            continue
        for t in re.findall(lit, args):
            add('rosie', t)
    for args in re.findall(r'LukonSay\(([^;]*?)\);', f):
        if 'Greet[' in args or 'Poke[' in args or '$"' in args or '+' in args:
            continue
        for t in re.findall(lit, args):
            add('lukon', t)

NPC_INFO = {
    'rosie': ('로지', 'F 밝고 귀여운 소녀 상점 마스코트'),
    'lukon': ('루콘', 'M 장난스러운 할아버지 장인'),
    'rohan': ('로한', 'M 호탕한 40대 아빠 마력부여사'),
    'bella': ('벨라', 'F 다정한 30대 엄마 마력부여사 (후후)'),
}
for sp, t in npc:
    t = t.replace('\\n', '\n').replace('\\"', '"')     # C# 이스케이프 풀기: 게임 속 문자열과 같아야 키가 맞는다
    lines.append({'file': f'npc_{sp}_{key(t)}', 'speaker': sp, 'name': NPC_INFO[sp][0], 'tone': NPC_INFO[sp][1], 'kind': 'npc', 'text': t})

# 읽는 소리용 표기 (화면 글자·파일 키는 그대로, TTS 에 넣는 문장만 바꾼다)
SAY = [('StrikerZ', '스트라이커즈'), ('Unique', '유니크'), ('Rare', '레어'), ('CP', '씨피'), ('77장', '칠십칠 장'), ('10팩', '열 팩'),
       ('7이 두 개', '칠이 두 개'), ('77장!', '칠십칠 장!'), ('1장', '한 장'), ('…', '...'), ('·', ', ')]
for l in lines:
    s = l['text']
    for a, b in SAY:
        s = s.replace(a, b)
    l['say'] = s.replace('\n', ' ')

json.dump(lines, open(os.path.join(HERE, 'lines.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
from collections import Counter
print(len(lines), Counter(l['speaker'] if l['kind'] == 'npc' else 'hero' for l in lines))
for l in lines:
    if l['kind'] == 'npc':
        print(l['file'], l['text'])
