using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>보스 스킬 1장 (카드로 보여 주는 기술). 팀원 전장에 주는 피해(lane/hero)는 AI 팀원 시뮬레이션용</summary>
    public class BossSkill
    {
        public enum Kind { SummonA, SummonB, Attack, Ultimate }
        public Kind kind;
        public CardData card;
        /// <summary>팀원 전장: 몬스터 전체 피해 · 가장 강한 몬스터 1장 피해 · 영웅 피해</summary>
        public int laneAll, laneOne, hero;
    }

    /// <summary>
    /// 속성 레이드 보스 (7속성 · 무속성/무지개 제외).
    /// 보스는 일반 카드를 쓰지 않는다: 고유 스킬 카드 4장(하수인 소환 2 · 공격 · 궁극기)으로 싸우고, 하수인도 보스 전용 카드다.
    /// 특성(타임 시작)은 그 속성 몬스터의 성격을 키운 것. 약점 속성 몬스터는 보스에게 2배 피해, 핵심 카드는 소환만으로 최대 HP 10% 피해.
    /// </summary>
    public class RaidBoss
    {
        public string id, name, title;
        public Element element;
        /// <summary>1인 기준 최대 HP · 반격 ATK (파티 인원에 따라 배율 적용)</summary>
        public int baseHp, heroAtk;
        public string traitName, traitText;
        /// <summary>로비에 보여 줄 설정 (서식지 · 전승)</summary>
        public string habitat, lore;
        public CardData minionA, minionB;
        public readonly List<BossSkill> skills = new List<BossSkill>();

        public Element weakness => RaidData.WeaknessOf(element);
        public BossSkill Skill(BossSkill.Kind k) => skills.First(s => s.kind == k);

        CardData hero;
        /// <summary>보스 카드 (영웅 칸). 능력은 스킬 카드로 대신하므로 영웅 효과는 없다</summary>
        public CardData Hero => hero ?? (hero = new CardData
        {
            id = id, name = name, type = CardType.Hero, element = element, rarity = Rarity.Unique,
            heroHp = baseHp, heroAtk = heroAtk,
            text = $"[{traitName}] {traitText}\n스킬: {string.Join(" · ", skills.Select(s => s.card.name))}",
        });

        List<CardData> keys;
        /// <summary>핵심 카드: 약점 속성의 Rare 이상 몬스터 중 T5 · T7 · T9 구간 첫 카드 (최대 3장)</summary>
        public List<CardData> KeyCards
        {
            get
            {
                if (keys != null) return keys;
                var pool = CardList.Monsters.Where(m => m.element == weakness && m.rarity >= Rarity.Rare).OrderBy(m => m.minTime).ToList();
                keys = new List<CardData>();
                foreach (int t in new[] { 5, 7, 9 })
                {
                    var k = pool.FirstOrDefault(m => m.minTime >= t && !keys.Contains(m));
                    if (k != null) keys.Add(k);
                }
                return keys;
            }
        }
    }

    public static class RaidData
    {
        public const int ClearOpal = 7;
        public const int MaxParty = 3;
        /// <summary>보스 하수인은 필드에 최대 이 수까지</summary>
        public const int MaxMinions = 5;
        /// <summary>클리어 전리품: 오팔 + 등급별 랜덤 카드 1장씩 (임시 보상)</summary>
        public static readonly Rarity[] LootRarities = { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Unique };

        /// <summary>파티 인원별 보스 배율 (HP, ATK): 1인 1/1 · 2인 1.75/1.25 · 3인 2.5/1.5</summary>
        public static (float hp, float atk) Scale(int party) =>
            party >= 3 ? (2.5f, 1.5f) : party == 2 ? (1.75f, 1.25f) : (1f, 1f);

        static readonly Element[] Elems = { Element.Fire, Element.Water, Element.Wind, Element.Earth, Element.Thunder, Element.Light, Element.Dark };

        public static Element WeaknessOf(Element e)
        {
            foreach (var x in Elems) if (CardData.Beats(x) == e) return x;
            return Element.None;
        }

        const string ArtRoot = GameAssets.Root + "Art/Cards/";

        static CardData Minion(string id, string name, Element e, Rarity r, int atk, int hp, int minTime, DiceEffect[] dice, string text,
            bool taunt = false, bool regen = false, int attacks = 1, bool chain = false, bool noCounter = false)
        {
            var tags = new List<string>();
            if (taunt) tags.Add("[도발]");
            if (regen) tags.Add("[재생] 배틀 엔드에 HP를 모두 회복한다.");
            if (attacks > 1) tags.Add($"[연속 공격] 배틀페이즈에 {attacks}회 공격한다.");
            if (chain) tags.Add("[연쇄] 전투로 상대 몬스터를 파괴하면 다음 몬스터를 이어서 공격한다.");
            if (noCounter) tags.Add("[암살] 영웅 반격을 받지 않는다.");
            return new CardData
            {
                id = id, name = name, type = CardType.Monster, element = e, rarity = r, atk = atk, hp = hp, minTime = minTime, dice = dice,
                taunt = taunt, regen = regen, attackCount = attacks, attackAll = chain, noCounter = noCounter,
                text = string.Join(" ", tags.Append(text)).Trim(),
            };
        }

        static BossSkill Skill(BossSkill.Kind k, string id, string name, Element e, string art, string text,
            Func<GameManager, CardInstance, IEnumerator> effect, int laneAll = 0, int laneOne = 0, int hero = 0) => new BossSkill
        {
            kind = k, laneAll = laneAll, laneOne = laneOne, hero = hero,
            card = new CardData
            {
                id = id, name = name, type = CardType.Magic, element = e, rarity = Rarity.Unique, speed = MagicSpeed.Normal,
                art = ArtRoot + art, text = text, effect = effect,
            },
        };

        static RaidBoss Build(RaidBoss b, string summonA, string summonB, string attack, string attackText,
            Func<GameManager, CardInstance, IEnumerator> attackFx, int laneAll, int laneOne, int hero,
            string ult, string ultText, Func<GameManager, CardInstance, IEnumerator> ultFx)
        {
            var e = b.element;
            b.skills.Add(Skill(BossSkill.Kind.SummonA, b.id + "S1", summonA, e, b.minionA.id,
                $"하수인 「{b.minionA.name}」을 소환한다. (보스의 3번째 타임부터 2장)", (g, c) => g.BossSummon(b.minionA, g.BossTurn >= 3 ? 2 : 1, summonA)));
            b.skills.Add(Skill(BossSkill.Kind.SummonB, b.id + "S2", summonB, e, b.minionB.id,
                $"정예 하수인 「{b.minionB.name}」을 소환한다.", (g, c) => g.BossSummon(b.minionB, 1, summonB)));
            b.skills.Add(Skill(BossSkill.Kind.Attack, b.id + "S3", attack, e, b.id + "S3", attackText, attackFx, laneAll, laneOne, hero));
            b.skills.Add(Skill(BossSkill.Kind.Ultimate, b.id + "S4", ult, e, b.id,
                "[궁극기 · HP 50% 이하에서 1회] " + ultText, ultFx, 3, 0, 3));
            return b;
        }

        public static readonly List<RaidBoss> Bosses = new List<RaidBoss>
        {
            Build(new RaidBoss { id = "R001", name = "업화룡 이그니르", title = "화산을 삼킨 용", element = Element.Fire, baseHp = 72, heroAtk = 4,
                    traitName = "업화", traitText = "보스의 타임 시작 시 파티 전원의 영웅에게 2 데미지.",
                    habitat = "작열의 화산 아그니스",
                    lore = "천 년 전 화산 아그니스를 통째로 삼킨 용. 심장이 용암으로 뛰고, 날갯짓 한 번에 하늘이 잿빛으로 물든다. 그가 눈을 뜨는 해마다 산기슭 마을들은 '불의 계절'을 맞았다.",
                    minionA = Minion("R001M1", "용암 새끼용", Element.Fire, Rarity.Uncommon, 5, 2, 3, Dice.Of(Dice.Atk(2), Dice.Burn(2), Dice.Pierce(2)), "[보스의 하수인]"),
                    minionB = Minion("R001M2", "흑요석 용기사", Element.Fire, Rarity.Rare, 8, 4, 6, Dice.Of(Dice.Atk(3), Dice.Burn(3), Dice.Pierce(3)), "[보스의 하수인]", regen: true) },
                "새끼용 부화", "용기사 소집", "화염 숨결", "상대 몬스터 전체에 3 데미지, 상대에게 2 데미지.",
                Effects.Seq(Effects.DamageAllEnemies(3), Effects.Burn(2)), 3, 0, 2,
                "대분화", "상대 몬스터 전체에 3 데미지, 상대에게 4 데미지.", Effects.Seq(Effects.DamageAllEnemies(3), Effects.Burn(4))),

            Build(new RaidBoss { id = "R002", name = "심해왕 레비아스", title = "가라앉은 신전의 주인", element = Element.Water, baseHp = 80, heroAtk = 2,
                    traitName = "만조", traitText = "보스의 타임 시작 시 최대 HP의 4% 회복.",
                    habitat = "심해의 신전 엘 마레아",
                    lore = "바다에 잠긴 신전 엘 마레아의 마지막 왕. 신전이 가라앉던 날부터 산호 왕관을 쓴 채 잠들어 있었다. 그의 잠을 깨운 자는 바다 전체를 적으로 돌리게 된다.",
                    minionA = Minion("R002M1", "심해 창병", Element.Water, Rarity.Uncommon, 3, 5, 3, Dice.Of(Dice.Shield(2), Dice.Atk(2), Dice.Heal(2)), "[보스의 하수인]"),
                    minionB = Minion("R002M2", "산호 거북 수호병", Element.Water, Rarity.Rare, 4, 10, 6, Dice.Of(Dice.Shield(3), Dice.Atk(3), Dice.Heal(3)), "[보스의 하수인]", taunt: true) },
                "심연의 부름", "수호병 기상", "대해일", "상대 몬스터 전체에 2 데미지, 보스 HP 5 회복.",
                Effects.Seq(Effects.DamageAllEnemies(2), Effects.Heal(5)), 2, 0, 0,
                "바다의 분노", "HP 15 회복, 상대 몬스터 전체에 2 데미지.", Effects.Seq(Effects.Heal(15), Effects.DamageAllEnemies(2))),

            Build(new RaidBoss { id = "R003", name = "폭풍군주 실베른", title = "하늘을 찢는 날개", element = Element.Wind, baseHp = 72, heroAtk = 3,
                    traitName = "질풍", traitText = "보스의 타임 시작 시 하수인 1장이 이번 타임 추가 공격 +1.",
                    habitat = "천공의 부유섬",
                    lore = "부유섬 사이를 떠도는 여섯 날개의 군주. 고대 바람 종의 갑옷을 두르고 있어, 그가 날아오르면 하늘 전체가 종소리처럼 울린다. 그가 지나간 하늘은 사흘 밤낮 폭풍이 그치지 않는다.",
                    minionA = Minion("R003M1", "질풍 하피", Element.Wind, Rarity.Uncommon, 4, 3, 3, Dice.Of(Dice.Atk(2), Dice.Shield(2), Dice.Weaken(2)), "[보스의 하수인]"),
                    minionB = Minion("R003M2", "폭풍 그리폰 기수", Element.Wind, Rarity.Rare, 6, 6, 6, Dice.Of(Dice.Atk(3), Dice.Shield(3), Dice.Weaken(3)), "[보스의 하수인]", attacks: 2) },
                "하피 떼", "그리폰 강하", "천공 칼바람", "상대 몬스터 1장에게 4 데미지를 2번.",
                Effects.Seq(Effects.StrikeEnemy(4), Effects.StrikeEnemy(4)), 0, 4, 0,
                "대폭풍", "이번 타임 하수인 ATK +2, 상대 몬스터 1장에게 4 데미지.", Effects.Seq(Effects.BuffAllies(2), Effects.StrikeEnemy(4))),

            Build(new RaidBoss { id = "R004", name = "산맥거신 고르둔", title = "움직이는 대지", element = Element.Earth, baseHp = 76, heroAtk = 3,
                    traitName = "암반 갑옷", traitText = "보스가 받는 모든 데미지 -2 (최소 1). 약점 공격은 갑옷을 무시한다.",
                    habitat = "태고의 대협곡",
                    lore = "대협곡 그 자체가 일어선 거신. 등에는 천 년 묵은 숲이 자라고, 한 걸음마다 지도가 바뀐다. 고대인들은 그를 '걷는 산맥'이라 부르며 그의 잠자리에 도시를 세우지 않았다.",
                    minionA = Minion("R004M1", "바위 파수병", Element.Earth, Rarity.Uncommon, 2, 6, 3, Dice.Of(Dice.Shield(2), Dice.Guard(), Dice.Atk(2)), "[보스의 하수인]", taunt: true),
                    minionB = Minion("R004M2", "수정 갑각 골렘", Element.Earth, Rarity.Rare, 4, 11, 6, Dice.Of(Dice.Shield(3), Dice.Guard().Atk(1), Dice.Atk(3)), "[보스의 하수인]") },
                "파수병 각성", "골렘 굴기", "대지 진동", "상대 몬스터 전체에 2 데미지, 상대에게 2 데미지.",
                Effects.Seq(Effects.DamageAllEnemies(2), Effects.Burn(2)), 2, 0, 2,
                "지각 붕괴", "이번 타임 하수인 HP +4 · ATK +1, 상대 몬스터 전체에 2 데미지.", Effects.Seq(Effects.FortifyAllies(4), Effects.BuffAllies(1), Effects.DamageAllEnemies(2))),

            Build(new RaidBoss { id = "R005", name = "뇌제 볼테온", title = "천둥 구름의 폭군", element = Element.Thunder, baseHp = 68, heroAtk = 5,
                    traitName = "낙뢰", traitText = "보스의 타임 시작 시 파티의 모든 몬스터에게 2 데미지.",
                    habitat = "뇌운의 탑",
                    lore = "뇌운의 탑 꼭대기에 군림하는 네 팔의 폭군. 스스로를 '하늘의 황제'라 칭하며, 그에게 고개 숙이지 않는 땅에는 밤낮없이 벼락을 내린다. 탑의 첨탑은 그가 꽂아 둔 창이라고 한다.",
                    minionA = Minion("R005M1", "뇌전 정령", Element.Thunder, Rarity.Uncommon, 6, 1, 3, Dice.Of(Dice.Atk(2), Dice.Pierce(2), Dice.Burn(2)), "[보스의 하수인]"),
                    minionB = Minion("R005M2", "뇌운 기사", Element.Thunder, Rarity.Rare, 10, 3, 6, Dice.Of(Dice.Atk(3), Dice.Pierce(3), Dice.Burn(3)), "[보스의 하수인]", chain: true) },
                "정령 방전", "기사 강림", "천벌의 낙뢰", "상대 몬스터 1장에게 6 데미지, 상대에게 3 데미지.",
                Effects.Seq(Effects.StrikeEnemy(6), Effects.Burn(3)), 0, 6, 3,
                "뇌제의 심판", "상대 몬스터 전체에 4 데미지.", Effects.DamageAllEnemies(4)),

            Build(new RaidBoss { id = "R006", name = "광휘천사 세라피엘", title = "심판의 빛", element = Element.Light, baseHp = 72, heroAtk = 3,
                    traitName = "성광의 코인", traitText = "보스의 타임 시작 시 코인 토스. 앞면이면 이번 타임 하수인 ATK +2, 최대 HP의 5% 회복.",
                    habitat = "광휘의 대성당",
                    lore = "빛의 대성당을 지키는 여섯 날개의 심판자. 얼굴 없는 가면 뒤에서 금빛 동전을 굴려 죄의 무게를 잰다. 동전이 앞면으로 멈추면, 심판은 이미 끝난 것이다.",
                    minionA = Minion("R006M1", "빛의 수호천사", Element.Light, Rarity.Uncommon, 3, 3, 3, Dice.Of(Dice.Heal(2), Dice.Atk(2), Dice.Shield(2)), "[보스의 하수인]"),
                    minionB = Minion("R006M2", "성창 기사단장", Element.Light, Rarity.Rare, 6, 6, 6, Dice.Of(Dice.Heal(3), Dice.Atk(3), Dice.Shield(3)), "[보스의 하수인]", regen: true) },
                "천사 강림", "기사단 소집", "심판의 광휘", "이번 타임 하수인 ATK +2, 상대에게 3 데미지.",
                Effects.Seq(Effects.BuffAllies(2), Effects.Burn(3)), 0, 0, 3,
                "성역 선포", "HP 10 회복, 이번 타임 하수인 ATK +2.", Effects.Seq(Effects.Heal(10), Effects.BuffAllies(2))),

            Build(new RaidBoss { id = "R007", name = "심연군주 녹테라", title = "빛을 먹는 그림자", element = Element.Dark, baseHp = 76, heroAtk = 4,
                    traitName = "어둠의 코인", traitText = "보스의 타임 시작 시 코인 토스. 앞면이면 파티 전원에게서 HP 3 흡수, 뒷면이면 보스가 최대 HP의 5%를 잃는다.",
                    habitat = "칠흑의 심연",
                    lore = "빛을 먹고 자라는 심연의 여제. 그녀의 손바닥 위 검은 동전은 삼킨 영혼의 수만큼 무거워진다. 동전이 뒤집히는 순간에는 그녀조차 대가를 치러야 한다.",
                    minionA = Minion("R007M1", "그림자 망령", Element.Dark, Rarity.Uncommon, 5, 4, 3, Dice.Of(Dice.Atk(2), Dice.Wound().Atk(1), Dice.Weaken(2)), "[보스의 하수인]"),
                    minionB = Minion("R007M2", "공허의 처형자", Element.Dark, Rarity.Rare, 9, 8, 6, Dice.Of(Dice.Atk(3), Dice.Wound().Atk(2), Dice.Weaken(3)), "[보스의 하수인]", noCounter: true) },
                "망령 소환", "처형자 소환", "영혼 흡수", "상대에게 4 데미지, 보스 HP 4 회복.",
                Effects.Seq(Effects.Burn(4), Effects.Heal(4)), 0, 0, 4,
                "영겁의 밤", "상대 몬스터 1장을 파괴하고 상대에게 3 데미지.", Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Burn(3))),
        };

        public static RaidBoss Get(string id) => Bosses.FirstOrDefault(b => b.id == id);

        /// <summary>
        /// 추천(대여) 레이드 덱: 약점 속성 몬스터 위주 + 핵심 카드 3장 + 마법. 소지 여부와 관계없이 빌려 쓴다.
        /// </summary>
        public static List<CardData> RecommendedDeck(RaidBoss b, Random rng)
        {
            var w = b.weakness;
            var deck = new List<CardData>(b.KeyCards);
            var mons = CardList.Monsters.Where(m => m.element == w && !b.KeyCards.Contains(m)).ToList();
            void Take(IEnumerable<CardData> src, int n) => deck.AddRange(src.OrderBy(_ => rng.Next()).Take(n));
            Take(mons.Where(m => m.minTime <= 3), 11);
            Take(mons.Where(m => m.minTime >= 4 && m.minTime <= 6), 7);
            Take(mons.Where(m => m.minTime >= 7), 2);
            Take(CardList.Magics, 7);
            return deck;   // 레이드는 보스 필드 고정이라 필드 카드는 넣지 않는다
        }

        /// <summary>추천 덱의 영웅: 약점 속성 영웅</summary>
        public static CardData RecommendedHero(RaidBoss b) =>
            CardList.Heroes.FirstOrDefault(h => h.element == b.weakness) ?? CardList.Heroes[0];

        /// <summary>보스 속성의 결계 필드 (레이드 내내 고정 · 보스 속성 HP +2)</summary>
        public static CardData BossField(RaidBoss b) => CardList.Fields[Math.Max(0, Array.IndexOf(Elems, b.element)) * 3 + 2];

        /// <summary>전리품 카드: 등급별 수집 가능 카드(몬스터·마법) 중 랜덤 1장</summary>
        public static CardData RollLoot(Rarity r, Random rng)
        {
            var pool = CardDB.All.Where(c => c.rarity == r && (c.type == CardType.Monster || c.type == CardType.Magic)).ToList();
            if (pool.Count == 0) pool = CardDB.All.Where(c => c.rarity == r).ToList();
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }
    }
}
