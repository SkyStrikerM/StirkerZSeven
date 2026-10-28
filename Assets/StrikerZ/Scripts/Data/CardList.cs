using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// ★ 카드 추가는 여기서 ★
    /// Monsters / Magics / Heroes 리스트에 new CardData { ... } 를 추가하면 덱 생성·영웅 선택에 자동 반영된다.
    /// 필드 카드(25종)는 BuildFields() 에서 생성된다.
    /// </summary>
    public static partial class CardList
    {
        // ─────────────────────────── 몬스터 ───────────────────────────
        // 일반 몬스터 270종은 MonsterSet.cs (자동 생성). dice = Dice.Of(1·2, 3·4, 5·6) — 세 구간은 밸류가 비슷한 서로 다른 효과.
        // 효과: Atk 자신 ATK / Weaken 상대 ATK 감소 / Shield 이번 전투 HP+ / Guard 파괴 방지(HP 1) / Pierce 파괴 시 추가 데미지
        //       Heal 영웅 회복 / Draw 드로우 / Burn 상대 영웅 데미지 / Wound 상처(다음 타임 회복 불가)

        // ─────────────────────────── 마법 ───────────────────────────
        public static readonly List<CardData> Magics = new List<CardData>
        {
            new CardData { id = "S001", name = "봉화", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.T0, trigger = Trigger.OpponentTimeStart,
                text = "타임 시작 시 발동 가능. 카드 1장 드로우. 이번 배틀 엔드까지 상대의 가장 앞 소환수 ATK -2.",
                effect = Effects.Seq(Effects.Draw(1), Effects.WeakenEnemies(1, 2, front: true)) },

            new CardData { id = "S002", name = "포기란없다", type = CardType.Magic, rarity = Rarity.Uncommon, speed = MagicSpeed.T0, trigger = Trigger.BattleEnd,
                text = "배틀 엔드에, 상대보다 필드 카드 수와 HP가 모두 적을 경우 발동 가능. 묘지에서 몬스터 1장과 마법 1장을 패로 가져온다.",
                canActivate = (g, c) => c.owner.FieldCount < c.owner.opponent.FieldCount && c.owner.hp < c.owner.opponent.hp,
                effect = Effects.ReturnFromGrave(1, 1) },

            new CardData { id = "S003", name = "개기일식", type = CardType.Magic, rarity = Rarity.Uncommon, speed = MagicSpeed.Normal,
                text = "공개되면 양쪽 필드의 소환수를 모두 뒷면으로 되돌린다 ([불변] 제외). 이번 배틀이 끝날 때까지 뒷면 카드는 행동하지 못하고 공격받지도 않는다 — [불변] 소환수만 상대 [불변] 소환수를, 없으면 영웅을 공격한다. 다시 공개돼도 소환 효과는 나오지 않는다.",
                effect = Effects.Eclipse() },

            new CardData { id = "S004", name = "번개 일격", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "상대의 가장 앞 소환수에게 5 데미지.", effect = Effects.StrikeFront(5) },

            new CardData { id = "S005", name = "전투의 함성", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "이번 타임 동안 아군 몬스터 ATK +2.", effect = Effects.BuffAllies(2) },

            new CardData { id = "S006", name = "화염구", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "상대에게 3 데미지.", effect = Effects.Burn(3) },

            new CardData { id = "S007", name = "지혜의 샘", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "카드 2장 드로우.", effect = Effects.Draw(2) },

            new CardData { id = "S008", name = "질풍 가속", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "아군 가장 앞 소환수의 ATK +3 (이번 타임).", effect = Effects.BuffFront(3) },

            new CardData { id = "S009", name = "치유의 빛", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.T0, trigger = Trigger.BattleEnd,
                text = "배틀 엔드에 발동 가능. HP 5 회복.",
                canActivate = (g, c) => c.owner.hp < c.owner.maxHp, effect = Effects.Heal(5) },

            new CardData { id = "S010", name = "반격의 결의", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.T0, trigger = Trigger.OpponentTimeStart,
                text = "타임 시작 시 발동 가능. 다음 배틀 엔드까지 아군 몬스터 ATK +2.",
                effect = Effects.BuffAllies(2, Expire.NextBattleEnd) },

            new CardData { id = "S011", name = "저격 조준", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.Normal,
                text = "이번 타임 동안 아군 소환수는 가장 뒤(오른쪽) 상대 소환수를 공격한다. ([도발] 무시)",
                effect = Effects.AddStatus(StatusKind.FreeTarget, Expire.EndOfTime) },
            new CardData { id = "S012", name = "도발의 깃발", type = CardType.Magic, rarity = Rarity.Common, speed = MagicSpeed.T0, trigger = Trigger.OpponentTimeStart,
                text = "타임 시작 시 발동 가능. 아군 몬스터 1장은 이번 타임 동안 [도발]을 얻는다.",
                effect = Effects.GrantTaunt() },
        };

        // ─────────────────────────── 신화 몬스터 ───────────────────────────
        // 판타지 마스터즈 '이카젠' 등급 유닛을 변형: 주사위(코인)·방어력 제거, 원작 스킬 → 주사위 구간/소환 효과.
        // 덱에는 신화 몬스터 1장만 (MaxMythicPerDeck). 아트는 오버프레임 (Art/Cards/{id}_full.png).
        public static readonly List<CardData> MythicMonsters = new List<CardData>
        {
            new CardData { id = "M101", name = "엔시아나", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Water, atk = 3, hp = 7, minTime = 2,
                dice = Dice.Of(Dice.Draw(2).Heal(2), Dice.Weaken(4), Dice.Guard().Weaken(2)), effect = MythicEffects.ControlCard, text = "[컨트롤] 소환 시 상대 필드 마법 1장을 상대 패로 되돌린다." },
            new CardData { id = "M102", name = "천요화", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Fire, atk = 6, hp = 5, minTime = 3,
                dice = Dice.Of(Dice.Atk(2).Burn(2), Dice.Weaken(4), Dice.Guard().Burn(2)), effect = Effects.Burn(3), text = "[생명 폭발] 소환 시 상대 영웅에게 3 데미지." },
            new CardData { id = "M103", name = "태초의 창조신", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Light, atk = 6, hp = 7, minTime = 4,
                dice = Dice.Of(Dice.Heal(4).Draw(1), Dice.Guard().Atk(2), Dice.Weaken(3).Pierce(1)), effect = Effects.Draw(2), text = "[창세] 소환 시 카드 2장 드로우." },
            new CardData { id = "M104", name = "아스트라가론", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Dark, atk = 6, hp = 9, minTime = 3,
                dice = Dice.Of(Dice.Guard().Atk(2), Dice.Guard().Weaken(2), Dice.Atk(2).Pierce(3)), text = "[수호 일격 / 수호 희생] 전투에서 쉽게 쓰러지지 않는 수호 기사." },
            new CardData { id = "M105", name = "무한의 성수", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Earth, atk = 5, hp = 10, minTime = 2,
                dice = Dice.Of(Dice.Atk(3).Heal(1), Dice.Guard().Draw(1), Dice.Weaken(3).Pierce(1)), effect = MythicEffects.SacredBeast, text = "[위업 달성] 소환 시 이번 타임 아군 필드 몬스터 수만큼 ATK 증가." },
            new CardData { id = "M106", name = "카로넬리아", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Thunder, atk = 4, hp = 2, minTime = 2,
                dice = Dice.Of(Dice.Guard().Heal(3), Dice.Weaken(4), Dice.Atk(2).Draw(1)), effect = MythicEffects.MagicBreak, text = "[매직 체인지] 소환 시 상대 필드 마법 1장을 파괴한다." },
            new CardData { id = "M107", name = "테레지아", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Dark, atk = 7, hp = 10, minTime = 4,
                dice = Dice.Of(Dice.Atk(2).Pierce(3), Dice.Weaken(3).Burn(1), Dice.Atk(4)), text = "[본질의 힘 / 학살의 춤] 이기면 적 영웅에게 큰 피해를 준다." },
            new CardData { id = "M108", name = "듀드티엘", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Water, atk = 3, hp = 7, minTime = 2,
                dice = Dice.Of(Dice.Guard().Weaken(2), Dice.Weaken(4), Dice.Draw(2).Heal(1)), resist = true, text = "[미러 이미지] [불변] 상대 카드 효과(개기일식·데미지·약화·행동 불능·되돌리기 등)를 받지 않는다. 파괴 효과와 전투로만 쓰러진다. 개기일식 중에도 싸운다. 환영으로 적의 공격을 흘려낸다." },
            new CardData { id = "M109", name = "판타즘 마스터", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Rainbow, atk = 7, hp = 8, minTime = 4,
                dice = Dice.Of(Dice.Atk(4), Dice.Weaken(2).Draw(1), Dice.Pierce(3).Burn(2)), effect = MythicEffects.SkillBreak, text = "[기술 파괴 / 한계 구현] 소환 시 이번 배틀 엔드까지 상대 몬스터 전체 ATK -2." },
            new CardData { id = "M110", name = "이세리브", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Light, atk = 2, hp = 4, minTime = 1,
                dice = Dice.Of(Dice.Weaken(4), Dice.Draw(2).Heal(1), Dice.Guard().Burn(2)), effect = MythicEffects.HistoryRevenge, text = "[역사의 보복] 소환 시 상대 필드에서 ATK가 가장 높은 몬스터를 이번 타임 행동 불능으로." },
            new CardData { id = "M111", name = "라리시엘", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Light, atk = 10, hp = 9, minTime = 6,
                dice = Dice.Of(Dice.Guard().Heal(3), Dice.Atk(2).Pierce(3), Dice.Weaken(4)), effect = Effects.Heal(5), text = "[전쟁의 주인 / 균형의 수호자] 소환 시 HP 5 회복." },
            new CardData { id = "M112", name = "오르브레크", type = CardType.Monster, rarity = Rarity.Unique, element = Element.Dark, atk = 8, hp = 12, minTime = 5,
                dice = Dice.Of(Dice.Weaken(3).Burn(1), Dice.Guard().Weaken(2), Dice.Atk(2).Burn(2)), effect = MythicEffects.ChaosLord, text = "[혼돈의 주관자] 소환 시 양쪽 필드의 마법을 모두 파괴한다." },
        };

        // ─────────────────────────── 신화 마법 ───────────────────────────
        // 효과 구현: MythicEffects.cs. 덱에는 신화 마법 1장만 (MaxMythicPerDeck). 아트는 오버프레임 (Art/Cards/{id}_full.png).
        // "순속성" = 아군 필드 몬스터가 모두 같은 속성 (무지개는 어느 속성과도 일치).
        public static readonly List<CardData> Mythics = new List<CardData>
        {
            new CardData { id = "S101", name = "저스티아", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Light, speed = MagicSpeed.T0, trigger = Trigger.OpponentBattle,
                text = "배틀 개시 시 발동. 아군 패·필드의 빛 속성 카드 수+1장까지 상대 필드 마법을 파괴하고, 파괴한 수만큼 ATK가 높은 상대 몬스터를 이번 타임 행동 불능으로 만든다.",
                canActivate = MythicEffects.OpponentHasField, effect = MythicEffects.Judgment },
            new CardData { id = "S102", name = "블래스트 플레어", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Fire, speed = MagicSpeed.Normal,
                text = "아군 필드에 화염 몬스터가 있을 때 발동. ATK 4 이하인 상대 몬스터를 모두 파괴한다. 그 후 아군 필드 가장 오른쪽 화염 몬스터는 이번 타임 행동 불능.",
                canActivate = (g, c) => MythicEffects.HasElement(c.owner, Element.Fire), effect = MythicEffects.BurstFire },
            new CardData { id = "S103", name = "게헨나 플레임", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Fire, speed = MagicSpeed.Normal,
                text = "상대 필드 왼쪽 몬스터 2장에게 지옥불을 붙인다: 3타임 동안 ATK -3. 상대 영웅에게 3 데미지.",
                effect = MythicEffects.Hellfire },
            new CardData { id = "S104", name = "데스 사이즈", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Dark, speed = MagicSpeed.T0, trigger = Trigger.OpponentBattle,
                text = "배틀 개시 시 발동. 상대 필드 왼쪽 몬스터 2장 중 기본 ATK 5 이하인 몬스터를 파괴한다.",
                canActivate = (g, c) => c.owner.opponent.Monsters().Count > 0, effect = MythicEffects.DeathBlade },
            new CardData { id = "S105", name = "세인트 리벤저", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Light, speed = MagicSpeed.Normal,
                text = "상대 필드에서 ATK가 가장 높은 몬스터의 ATK를 이번 배틀 엔드까지 절반(올림)으로 만들고, 줄어든 수치만큼 아군 필드 몬스터 전체 ATK 증가. 순속성: 줄어든 수치만큼 HP 회복.",
                canActivate = (g, c) => g.EffectTargets(c.owner.opponent).Count > 0, effect = MythicEffects.HolyAvenger },
            new CardData { id = "S106", name = "영광의 최후", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Earth, speed = MagicSpeed.Normal,
                text = "상대 몬스터를 1장 골라 파괴하고, 아군 필드에서 ATK가 가장 높은 몬스터 1장도 함께 파괴한다. 순속성: 아군 몬스터는 파괴되지 않는다.",
                canActivate = (g, c) => c.owner.opponent.Monsters().Count > 0, effect = MythicEffects.GloryWarrior },
            new CardData { id = "S107", name = "천둥 포화", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Thunder, speed = MagicSpeed.Normal,
                text = "아군 왼쪽 1·2번째 몬스터가 각각 상대 왼쪽 1·2번째 몬스터를 조준한다. 아군 ATK가 같거나 높으면 대상을 파괴. 순속성: 파괴한 수×3 만큼 상대 영웅에게 데미지.",
                canActivate = (g, c) => c.owner.Monsters().Count > 0 && g.EffectTargets(c.owner.opponent).Count > 0, effect = MythicEffects.Volley },
            new CardData { id = "S108", name = "세이렌의 눈물", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Water, speed = MagicSpeed.T0, trigger = Trigger.OpponentBattle,
                text = "배틀 개시 시 발동. 상대 필드 왼쪽 몬스터 3장의 ATK를 이번 배틀 엔드까지 아군 패의 물 속성 카드 수(최소 2)로 나눈다(내림). 상대 필드 마법 1장을 상대 패로 되돌린다.",
                canActivate = MythicEffects.OpponentHasField, effect = MythicEffects.MermaidTear },
            new CardData { id = "S109", name = "루미너스 보이드", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Light, speed = MagicSpeed.T0, trigger = Trigger.OpponentBattle,
                text = "배틀 개시 시 발동. 아군 필드 빛 속성 몬스터를 오른쪽부터 모두 패로 되돌리고, 되돌린 수만큼 상대 필드 카드(ATK 높은 몬스터 우선)를 상대 덱으로 보낸다.",
                canActivate = (g, c) => MythicEffects.HasElement(c.owner, Element.Light) && c.owner.opponent.FieldCount > 0, effect = MythicEffects.WhiteHole },
            new CardData { id = "S110", name = "테라 사이클론", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Wind, speed = MagicSpeed.Normal,
                text = "상대 필드 왼쪽 몬스터부터 차례로 파괴한다. 파괴한 몬스터의 기본 ATK 합계가 9를 넘을 수 없다. 아군 필드 몬스터가 모두 바람 속성이 아니면 한도 7.",
                canActivate = (g, c) => c.owner.opponent.Monsters().Count > 0, effect = MythicEffects.GigaWind },
            new CardData { id = "S111", name = "종말의 마차", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Dark, speed = MagicSpeed.Normal,
                text = "상대 필드 몬스터 중 가장 높은 ATK만큼 상대 영웅에게 데미지를 주고, 그 절반(올림)만큼 자신도 데미지를 받는다.",
                canActivate = (g, c) => g.EffectTargets(c.owner.opponent).Count > 0, effect = MythicEffects.DoomChariot },
            new CardData { id = "S112", name = "소울 레스큐", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Light, speed = MagicSpeed.Normal,
                text = "아군 필드 빛 속성 몬스터 1장을 패로 되돌린다. 그 후 양쪽 묘지의 몬스터 1장을 골라 아군 필드에 ATK +1 (영구) 상태로 특수 소환한다.",
                canActivate = MythicEffects.AnyGraveMonster, effect = MythicEffects.LifeRescue },

            // ── 이카젠 등급 마법 변형 ──
            new CardData { id = "S201", name = "신비의 심해", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Water, speed = MagicSpeed.Normal,
                text = "순속성(물): 아군 묘지의 물 속성 몬스터를 최대 2장 특수 소환한다. 2장 소환하면 필드를 [심해의 신전]으로 바꾼다.",
                canActivate = MythicEffects.AbyssCan, effect = MythicEffects.MysticAbyss },
            new CardData { id = "S202", name = "깊은 암야", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Dark, speed = MagicSpeed.Normal,
                text = "순속성(어둠): 아군 어둠 몬스터 수만큼 ATK가 높은 상대 몬스터의 ATK를 이번 배틀 엔드까지 1로 만든다. 조건을 못 채우면 상대 왼쪽 몬스터 2장을 행동 불능으로.",
                canActivate = (g, c) => g.EffectTargets(c.owner.opponent).Count > 0, effect = MythicEffects.DeepNight },
            new CardData { id = "S203", name = "들풀의 군세", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Wind, speed = MagicSpeed.Normal,
                text = "순속성(바람): 패의 바람 속성 몬스터를 최대 2장 특수 소환한다. 2장 소환하면 필드를 [천공의 정원]으로 바꾼다.",
                canActivate = MythicEffects.WildgrassCan, effect = MythicEffects.WildgrassLegion },
            new CardData { id = "S204", name = "대지의 분노", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Earth, speed = MagicSpeed.Normal,
                text = "순속성(대지): 아군 대지 몬스터 최고 HP (최대 14) 한도로 상대 몬스터를 왼쪽부터 파괴. 아군 몬스터가 없으면 패의 대지 몬스터 1장을 ATK +5 (영구)로 특수 소환.",
                canActivate = MythicEffects.EarthCan, effect = MythicEffects.WrathOfEarth },
            new CardData { id = "S205", name = "인간의 긍지", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Thunder, speed = MagicSpeed.Normal,
                text = "순속성(번개): 아군 필드 최고 ATK보다 기본 ATK가 높은 아군 묘지 몬스터를 최근 순으로 빈칸만큼 특수 소환. 1장만 소환되면 그 몬스터는 이번 타임 행동 불능.",
                canActivate = MythicEffects.PrideCan, effect = MythicEffects.PrideOfMankind },
            new CardData { id = "S206", name = "화염 군단", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Fire, speed = MagicSpeed.Normal,
                text = "순속성(화염): (아군 화염 몬스터 최고 ATK+2) 만큼의 화력으로 상대 몬스터를 왼쪽부터 ATK 합계만큼 파괴하고, 남은 화력의 1/3 만큼 상대 영웅에게 데미지.",
                canActivate = (g, c) => MythicEffects.HasElement(c.owner, Element.Fire) && g.IsMonoElement(c.owner), effect = MythicEffects.FlameLegion },
            new CardData { id = "S207", name = "균형의 저울", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Light, speed = MagicSpeed.Normal,
                text = "순속성(빛): 양쪽 필드의 카드 수가 같아질 때까지, 그 다음 양쪽 패의 수가 같아질 때까지 많은 쪽의 카드를 뒤에서부터 묘지로 보낸다.",
                canActivate = MythicEffects.BalanceCan, effect = MythicEffects.ScalesOfBalance },
            new CardData { id = "S208", name = "앱솔루트 스펠", type = CardType.Magic, rarity = Rarity.Unique, element = Element.Rainbow, speed = MagicSpeed.Normal,
                text = "아군 필드 몬스터가 없거나 속성이 2종류 이상일 때: 양쪽 필드의 마법을 모두 파괴하고, 파괴한 상대 마법 수만큼 상대 몬스터를 오른쪽부터 패로 되돌린다.",
                canActivate = MythicEffects.AbsoluteCan, effect = MythicEffects.AbsoluteSpell },
        };

        // ─────────────────────────── 영웅 (리더) ───────────────────────────
        // 능력은 매치에 1회. 능력이 강할수록 HP 가 낮다. ATK(1~3)는 직접 공격을 받을 때 반격 데미지.
        public static readonly List<CardData> Heroes = new List<CardData>
        {
            new CardData { id = "H001", name = "레온", type = CardType.Hero, element = Element.Fire, rarity = Rarity.Rare, heroHp = 42, heroAtk = 3,
                text = "[홍염의 스트라이커] 상대 몬스터 1장을 파괴하고 상대에게 3 데미지.",
                effect = Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Burn(3)) },
            new CardData { id = "H002", name = "마리나", type = CardType.Hero, element = Element.Water, rarity = Rarity.Rare, heroHp = 51, heroAtk = 1,
                text = "[심해의 무녀] HP 10 회복, 카드 1장 드로우.",
                effect = Effects.Seq(Effects.Heal(10), Effects.Draw(1)) },
            new CardData { id = "H003", name = "가론", type = CardType.Hero, element = Element.Earth, rarity = Rarity.Rare, heroHp = 57, heroAtk = 2,
                text = "[대지의 방패] 이번 타임 아군 몬스터 ATK +2.",
                effect = Effects.BuffAllies(2) },
            new CardData { id = "H004", name = "녹스", type = CardType.Hero, element = Element.Dark, rarity = Rarity.Rare, heroHp = 45, heroAtk = 3,
                text = "[그림자 군주] 상대 소환수 2장(ATK 높은 순)의 ATK 를 이번 배틀 엔드까지 -3, 카드 1장 드로우.",
                effect = Effects.Seq(Effects.WeakenEnemies(2, 3), Effects.Draw(1)) },
            new CardData { id = "H005", name = "루미나", type = CardType.Hero, element = Element.Light, rarity = Rarity.Rare, heroHp = 45, heroAtk = 1,
                text = "[빛의 성녀] 묘지에서 몬스터 2장을 패로 가져오고 HP 5 회복.",
                effect = Effects.Seq(Effects.ReturnFromGrave(2, 0), Effects.Heal(5)) },
            new CardData { id = "H006", name = "제로", type = CardType.Hero, element = Element.Rainbow, rarity = Rarity.Rare, heroHp = 36, heroAtk = 2,
                text = "[세븐 스트라이커] 카드 3장 드로우, 이번 타임 아군 몬스터 ATK +3.",
                effect = Effects.Seq(Effects.Draw(3), Effects.BuffAllies(3)) },
            new CardData { id = "H007", name = "이그니스", type = CardType.Hero, element = Element.Fire, rarity = Rarity.Rare, heroHp = 45, heroAtk = 2,
                text = "[업화의 마녀] 상대에게 5 데미지, 이번 타임 아군 몬스터 ATK +1.",
                effect = Effects.Seq(Effects.Burn(5), Effects.BuffAllies(1)) },
            new CardData { id = "H008", name = "카이", type = CardType.Hero, element = Element.Water, rarity = Rarity.Rare, heroHp = 54, heroAtk = 1,
                text = "[해류 방벽] 다음 배틀 엔드까지 상대 몬스터 ATK -2, HP 4 회복.",
                effect = Effects.Seq(Effects.AddStatus(StatusKind.AtkBonus, Expire.NextBattleEnd, onOpponent: true, value: -2), Effects.Heal(4)) },
            new CardData { id = "H009", name = "실피아", type = CardType.Hero, element = Element.Wind, rarity = Rarity.Rare, heroHp = 42, heroAtk = 2,
                text = "[질풍의 사수] 아군 가장 앞 소환수 ATK +3 (이번 타임), 카드 1장 드로우.",
                effect = Effects.Seq(Effects.BuffFront(3), Effects.Draw(1)) },
            new CardData { id = "H010", name = "가일", type = CardType.Hero, element = Element.Wind, rarity = Rarity.Rare, heroHp = 48, heroAtk = 3,
                text = "[폭풍의 검객] 이번 타임 아군 소환수는 가장 뒤 상대 소환수를 공격 (도발 무시), 아군 소환수 ATK +1.",
                effect = Effects.Seq(Effects.AddStatus(StatusKind.FreeTarget, Expire.EndOfTime), Effects.BuffAllies(1)) },
            new CardData { id = "H011", name = "볼트", type = CardType.Hero, element = Element.Thunder, rarity = Rarity.Rare, heroHp = 48, heroAtk = 2,
                text = "[뇌신의 주먹] 상대 몬스터 1장을 파괴한다.",
                effect = Effects.DestroyEnemyMonster(1) },
            new CardData { id = "H012", name = "라이카", type = CardType.Hero, element = Element.Thunder, rarity = Rarity.Rare, heroHp = 42, heroAtk = 2,
                text = "[연쇄 전격] 상대에게 3 데미지, 카드 2장 드로우.",
                effect = Effects.Seq(Effects.Burn(3), Effects.Draw(2)) },
            new CardData { id = "H013", name = "브론", type = CardType.Hero, element = Element.Earth, rarity = Rarity.Rare, heroHp = 60, heroAtk = 1,
                text = "[암반의 수호자] HP 8 회복, 아군 몬스터 1장에게 이번 타임 [도발] 부여.",
                effect = Effects.Seq(Effects.Heal(8), Effects.GrantTaunt()) },
            new CardData { id = "H014", name = "세라핀", type = CardType.Hero, element = Element.Light, rarity = Rarity.Rare, heroHp = 48, heroAtk = 2,
                text = "[성광의 기사] 이번 타임 아군 몬스터 ATK +2, HP 4 회복.",
                effect = Effects.Seq(Effects.BuffAllies(2), Effects.Heal(4)) },
            new CardData { id = "H015", name = "모르가나", type = CardType.Hero, element = Element.Dark, rarity = Rarity.Rare, heroHp = 42, heroAtk = 2,
                text = "[저주의 마녀] 이번 매치 동안 상대 몬스터 ATK -1.",
                effect = Effects.AddStatus(StatusKind.AtkBonus, Expire.Never, onOpponent: true, value: -1) },
            new CardData { id = "H016", name = "오메가", type = CardType.Hero, element = Element.Rainbow, rarity = Rarity.Rare, heroHp = 39, heroAtk = 2,
                text = "[종언의 칠색] 상대 몬스터 2장을 파괴한다.",
                effect = Effects.DestroyEnemyMonster(2) },
            // 스토리 주인공 (1권 클리어 시 해금 — StoryData.HeroAvailable)
            StoryData.Kai,
            // 스토리 등장인물 (해금 스테이지 클리어 시 사용 가능 — StoryData.HeroAvailable)
            StoryData.Aria, StoryData.BaneHero, StoryData.Meril, StoryData.Terra, StoryData.Zephyr,
            StoryData.Lumien, StoryData.Erebos, StoryData.Varus, StoryData.Prisma, StoryData.PrismKai,
        };

        // ─────────────────────────── 필드 (25종) ───────────────────────────
        public static readonly List<CardData> Fields = BuildFields();

        /// <summary>이름으로 필드 카드 찾기 (없으면 null)</summary>
        public static CardData FieldNamed(string name) => Fields.FirstOrDefault(f => f.name == name);

        static List<CardData> BuildFields()
        {
            var list = new List<CardData>();
            var elements = new[] { Element.Fire, Element.Water, Element.Wind, Element.Earth, Element.Thunder, Element.Light, Element.Dark };
            var names = new[,]
            {
                { "작열의 화산", "업화의 폭주", "불꽃의 결계" },
                { "심해의 신전", "해일의 폭주", "물안개 결계" },
                { "천공의 정원", "폭풍의 폭주", "산들바람 결계" },
                { "태고의 협곡", "지각의 폭주", "암석의 결계" },
                { "뇌운의 탑", "번개의 폭주", "전류의 결계" },
                { "광휘의 성당", "섬광의 폭주", "성광의 결계" },
                { "칠흑의 심연", "암흑의 폭주", "그림자 결계" },
            };
            int n = 1;
            // 상성 기준: 그 속성 강화 + 그 속성이 이기는 속성(약점) 약화. 무지개 몬스터는 모든 조건을 받고, 무속성 몬스터는 필드 영향 없음
            for (int i = 0; i < elements.Length; i++)
            {
                var e = elements[i];
                var w = CardData.Beats(e);
                string en = CardData.ElementName(e), wn = CardData.ElementName(w);
                // 기본: 공격형
                list.Add(new CardData { id = $"F{n++:000}", name = names[i, 0], type = CardType.Field, element = e, rarity = Rarity.Common,
                    text = $"{en} 속성 몬스터 ATK +2, {wn} 속성 몬스터 ATK -1.",
                    fieldAtkBonus = m => (CardData.FieldHits(m, e) ? 2 : 0) + (CardData.FieldHits(m, w) ? -1 : 0) });
                // 폭주: ATK·HP 모두
                list.Add(new CardData { id = $"F{n++:000}", name = names[i, 1], type = CardType.Field, element = e, rarity = Rarity.Uncommon,
                    text = $"{en} 속성 몬스터 ATK·HP +1, {wn} 속성 몬스터 ATK·HP -1.",
                    fieldAtkBonus = m => (CardData.FieldHits(m, e) ? 1 : 0) + (CardData.FieldHits(m, w) ? -1 : 0),
                    fieldHpBonus = m => (CardData.FieldHits(m, e) ? 1 : 0) + (CardData.FieldHits(m, w) ? -1 : 0) });
                // 결계: 수비형 (동시 룰은 모든 소환이 뒷면이라 예전 "뒷면 소환 허용" 문구는 뺐다)
                list.Add(new CardData { id = $"F{n++:000}", name = names[i, 2], type = CardType.Field, element = e, rarity = Rarity.Common,
                    text = $"{en} 속성 몬스터 HP +2, {wn} 속성 몬스터 HP -1.",
                    fieldHpBonus = m => (CardData.FieldHits(m, e) ? 2 : 0) + (CardData.FieldHits(m, w) ? -1 : 0) });
            }
            list.Add(new CardData { id = $"F{n++:000}", name = "무지개 파라다이스", type = CardType.Field, element = Element.Rainbow, rarity = Rarity.Rare,
                text = "무지개 몬스터 ATK·HP +1, 그 외 속성 몬스터 ATK +1. (무속성 제외)",
                fieldAtkBonus = m => 1, fieldHpBonus = m => m.data.element == Element.Rainbow ? 1 : 0 });
            list.Add(new CardData { id = $"F{n++:000}", name = "고요한 평원", type = CardType.Field, element = Element.None, rarity = Rarity.Common,
                text = "아무 효과도 없는 평온한 전장." });
            list.Add(new CardData { id = $"F{n++:000}", name = "황혼의 전장", type = CardType.Field, element = Element.None, rarity = Rarity.Uncommon,
                text = "속성 몬스터 ATK +1, HP -1. (무속성 제외) 짧고 치열한 싸움.",
                fieldAtkBonus = m => 1, fieldHpBonus = m => -1 });
            list.Add(new CardData { id = $"F{n++:000}", name = "혼돈의 균열", type = CardType.Field, element = Element.Rainbow, rarity = Rarity.Rare,
                text = "무지개 몬스터 ATK·HP -1. 그 외 속성 몬스터는 전투 판정(주사위·코인) 횟수 -1.",
                fieldAtkBonus = m => m.data.element == Element.Rainbow ? -1 : 0,
                fieldHpBonus = m => m.data.element == Element.Rainbow ? -1 : 0,
                fieldDiceBonus = m => m.data.element == Element.Rainbow ? 0 : -1 });
            return list;
        }

        // ─────────────────────────── 덱 생성 ───────────────────────────
        public const int DeckSize = 30;

        static int CopiesFor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 3;
                case Rarity.Uncommon: return 2;
                default: return 1;   // Rare / Unique
            }
        }

        /// <summary>몬스터 20 + 마법 7 + 필드 3 장의 랜덤 덱</summary>
        public static List<CardData> BuildRandomDeck(System.Random rng)
        {
            var deck = new List<CardData>();
            // 몬스터는 타임 곡선대로: T1~2 9장 · T3~4 7장 · T5~7 4장 (신화는 중반 구간에서 최대 1장)
            if (TestAllMythics) deck.AddRange(Pick(Monsters.Where(m => m.minTime <= 3).ToList(), 14, rng).Concat(MythicMonsters));
            else
            {
                deck.AddRange(Pick(Monsters.Where(m => m.minTime <= 2).ToList(), 13, rng));
                deck.AddRange(Pick(Monsters.Where(m => m.minTime >= 3 && m.minTime <= 4).Concat(MythicMonsters).ToList(), 10, rng));
                deck.AddRange(Pick(Monsters.Where(m => m.minTime >= 5).ToList(), 5, rng));
            }
            if (TestAllMythics) deck.AddRange(Mythics);   // 자동 테스트용: 신화 전부 투입
            else deck.AddRange(Pick(Magics.Concat(Mythics).ToList(), 12, rng));
            deck.AddRange(Fields.OrderBy(_ => rng.Next()).Take(DeckRules.MaxExtra));   // 엑스트라 덱 (SetupPlayer 가 나눈다)
            return deck.OrderBy(_ => rng.Next()).ToList();
        }

        /// <summary>
        /// 전설 난이도 CPU 덱: 타임 구간마다 (등급 → ATK+HP) 상위 카드만 후보로 두고 뽑는다.
        /// 곡선은 무작위 덱과 같다 (T1~2 9 · T3~4 7 · T5~7 4, 마법 7, 필드 3)
        /// </summary>
        public static List<CardData> BuildStrongDeck(System.Random rng)
        {
            List<CardData> Top(IEnumerable<CardData> src, float share)
            {
                var l = src.OrderByDescending(c => (int)c.rarity * 4 + c.atk + c.hp).ToList();
                return l.Take(Mathf.Max(4, Mathf.CeilToInt(l.Count * share))).ToList();
            }
            var deck = new List<CardData>();
            deck.AddRange(Pick(Top(Monsters.Where(m => m.minTime <= 2), 0.35f), 13, rng));
            deck.AddRange(Pick(Top(Monsters.Where(m => m.minTime >= 3 && m.minTime <= 4).Concat(MythicMonsters), 0.35f), 10, rng));
            deck.AddRange(Pick(Top(Monsters.Where(m => m.minTime >= 5), 0.4f), 5, rng));
            deck.AddRange(Pick(Magics.Concat(Mythics).OrderByDescending(c => (int)c.rarity).Take(Mathf.Max(16, (Magics.Count + Mythics.Count) / 2)).ToList(), 12, rng));
            // 상위 후보가 적어 메인 40장이 안 차면 같은 곡선의 일반 후보로 채운다
            if (deck.Count < DeckRules.MinSize)
                deck.AddRange(Pick(Monsters.Where(m => m.minTime <= 4).ToList(), DeckRules.MinSize - deck.Count, rng));
            deck.AddRange(Fields.OrderBy(_ => rng.Next()).Take(DeckRules.MaxExtra));
            return deck.OrderBy(_ => rng.Next()).ToList();
        }

        /// <summary>자동 테스트 전용: 모든 신화 카드를 덱에 넣는다</summary>
        public static bool TestAllMythics;

        /// <summary>덱 한 벌에 들어갈 수 있는 신화 카드 최대 수 — 몬스터·마법 각각 (Pick 호출마다 적용)</summary>
        public const int MaxMythicPerDeck = 1;

        static IEnumerable<CardData> Pick(List<CardData> source, int count, System.Random rng)
        {
            var pool = new List<CardData>();
            foreach (var c in source) for (int i = 0; i < CopiesFor(c.rarity); i++) pool.Add(c);
            var picked = new List<CardData>();
            int mythic = 0;
            foreach (var c in pool.OrderBy(_ => rng.Next()))
            {
                if (picked.Count >= count) break;
                if (c.rarity == Rarity.Unique) { if (mythic >= MaxMythicPerDeck) continue; mythic++; }
                picked.Add(c);
            }
            return picked;
        }
    }
}
