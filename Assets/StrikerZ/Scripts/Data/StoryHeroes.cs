using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 스토리 등장인물 영웅 카드 (H018~H027) · 2장 이후의 스토리 전용 하수인/상대 영웅.
    /// 스토리 영웅은 해금 스테이지(rewardHero)를 클리어해야 영웅 목록·CPU 선택에 나온다 (HeroAvailable).
    /// </summary>
    public static partial class StoryData
    {
        /// <summary>그림 경로 없는 스토리 영웅 (그림은 카드 id 로 찾는다)</summary>
        static CardData Hero(string id, string name, Element e, int hp, int atk, string text, Func<GameManager, CardInstance, IEnumerator> fx)
            => StoryHero(id, name, e, hp, atk, null, text, fx);

        public static readonly CardData Aria = Hero("H018", "아리아", Element.Water, 50, 1,
            "[치유의 물방울] HP 10 회복, 이번 타임 아군 몬스터 HP +2.", Effects.Seq(Effects.Heal(10), Effects.FortifyAllies(2)));
        public static readonly CardData BaneHero = Hero("H019", "베인", Element.Wind, 44, 2,
            "[폭풍의 눈] 상대 몬스터 1장에게 4 데미지, 아군 가장 앞 소환수 ATK +3 (이번 타임).", Effects.Seq(Effects.StrikeEnemy(4), Effects.BuffFront(3)));
        public static readonly CardData Meril = Hero("H020", "메릴", Element.Water, 52, 2,
            "[호수의 여왕] 상대 몬스터 전체에 2 데미지, HP 5 회복.", Effects.Seq(Effects.DamageAllEnemies(2), Effects.Heal(5)));
        public static readonly CardData Terra = Hero("H021", "테라", Element.Earth, 60, 1,
            "[대지의 맹세] 이번 타임 아군 몬스터 HP +3, HP 6 회복.", Effects.Seq(Effects.FortifyAllies(3), Effects.Heal(6)));
        public static readonly CardData Zephyr = Hero("H022", "제퍼", Element.Wind, 42, 2,
            "[자유의 노래] 카드 2장 드로우, 아군 가장 앞 소환수 ATK +3 (이번 타임).", Effects.Seq(Effects.Draw(2), Effects.BuffFront(3)));
        public static readonly CardData Lumien = Hero("H023", "루미엔", Element.Light, 46, 2,
            "[성광의 축복] HP 12 회복, 이번 타임 아군 몬스터 ATK +1.", Effects.Seq(Effects.Heal(12), Effects.BuffAllies(1)));
        public static readonly CardData Erebos = Hero("H024", "에레보스", Element.Dark, 45, 3,
            "[심연의 손아귀] 상대 몬스터 1장을 파괴하고 상대에게 2 데미지.", Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Burn(2)));
        public static readonly CardData Varus = Hero("H025", "바루스", Element.None, 48, 3,
            "[회색 잠식] 다음 배틀 엔드까지 상대 몬스터 ATK -2, 상대 몬스터 1장에게 3 데미지.",
            Effects.Seq(Effects.AddStatus(StatusKind.AtkBonus, Expire.NextBattleEnd, onOpponent: true, value: -2), Effects.StrikeEnemy(3)));
        public static readonly CardData Prisma = Hero("H026", "프리스마", Element.Rainbow, 40, 2,
            "[창세의 빛] 상대 몬스터 전체에 2 데미지, HP 6 회복, 카드 1장 드로우.", Effects.Seq(Effects.DamageAllEnemies(2), Effects.Heal(6), Effects.Draw(1)));
        public static readonly CardData PrismKai = new CardData
        {
            id = "H027", name = "프리즘 카일", type = CardType.Hero, element = Element.Rainbow, rarity = Rarity.Rare, heroHp = 46, heroAtk = 3,
            text = "[일곱 빛깔의 검] 상대 몬스터 1장을 주인의 패로 튕겨내고 그 ATK만큼(최대 6) 상대에게 데미지, 이번 타임 아군 몬스터 ATK +1.",
            effect = PrismReflect,
        };

        static IEnumerator PrismReflect(GameManager g, CardInstance c)
        {
            var targets = g.EffectTargets(c.owner.opponent);
            if (targets.Count > 0)
            {
                CardInstance chosen = null;
                yield return g.ChooseCard(c.owner, targets, $"[{c.Name}] 튕겨낼 상대 몬스터를 선택", t => g.EffectiveAtk(t) * 2 + g.CurHp(t), r => chosen = r);
                if (chosen != null)
                {
                    int dmg = Mathf.Min(6, g.EffectiveAtk(chosen));
                    yield return g.ReturnToHand(chosen, c.Name);
                    if (dmg > 0 && !g.gameOver) yield return g.DamagePlayer(c.owner.opponent, dmg, c.Name);
                }
            }
            if (!g.gameOver) yield return Effects.BuffAllies(1)(g, c);
        }

        /// <summary>스토리에서 해금되는 영웅들 (해금 전에는 영웅 목록·CPU 선택에서 빠짐)</summary>
        /// <summary>partial 파일 간 정적 초기화 순서가 정해져 있지 않으므로 처음 쓸 때 만든다</summary>
        public static CardData[] StoryHeroes => storyHeroes ?? (storyHeroes = new[] { Kai, Aria, BaneHero, Meril, Terra, Zephyr, Lumien, Erebos, Varus, Prisma, PrismKai });
        static CardData[] storyHeroes;
        static HashSet<string> StoryHeroIds => storyHeroIds ?? (storyHeroIds = new HashSet<string>(StoryHeroes.Select(h => h.id)));
        static HashSet<string> storyHeroIds;
        public static bool IsStoryHero(CardData h) => StoryHeroIds.Contains(h.id);

        // ── 2장 이후 하수인: 회색 군단 ──

        public static readonly CardData GreySoldier = Beast("SM04", "회색 병사", 3, 5, 2, Dice.Of(Dice.Shield(2), Dice.Guard(), Dice.Atk(2)),
            "색을 빼앗기고 바루스를 따르는 병사. 눈동자가 비어 있다.", taunt: true);
        public static readonly CardData Tentacle = Beast("SM05", "망각의 촉수", 5, 4, 4, Dice.Of(Dice.Atk(2), Dice.Weaken(2), Dice.Pierce(2)),
            "차원의 틈에서 뻗어 나온 촉수. 닿는 것의 색을 빨아들인다.", noCounter: true);
        public static readonly CardData VoidShard = Beast("SM06", "공허의 파편", 2, 3, 1, Dice.Of(Dice.Atk(2), Dice.Pierce(1).Atk(1), Dice.Shield(2)),
            "망각의 군주에게서 떨어져 나온 조각. 쉬지 않고 들이받는다.", attacks: 2);

        // ── 2장 이후 스토리 전용 상대 영웅 (아트: Art/Story/SHxx — 장면 CG에서 잘라냄) ──

        static CardData oppSoldierCaptain, oppGolem, oppGatekeeper, oppFireCaptain, oppLakeCaptain, oppStormSpirit, oppGiant, oppLieutenant,
            oppPaladin, oppDarkGeneral, oppGreyLegion, oppLord, oppLord2, oppLordFinal, oppTentacleLord, oppShadowKai, oppDevourer, oppPhantomLeon, oppCore, oppRemnant;

        static void BuildStoryOpponents()
        {
            if (oppSoldierCaptain != null) return;
            oppSoldierCaptain = StoryHero("SH07", "회색 병사 대장", Element.None, 34, 2, "SH07", "[회색 방진] 이번 타임 아군 몬스터 HP +2, HP 4 회복.", Effects.Seq(Effects.FortifyAllies(2), Effects.Heal(4)));
            oppGolem = StoryHero("SH08", "안개 골렘", Element.None, 40, 3, "SH08", "[짓누르기] 상대 몬스터 1장에게 5 데미지.", Effects.StrikeEnemy(5));
            oppGatekeeper = StoryHero("SH09", "안개의 수문장", Element.None, 44, 3, "SH09", "[잿빛 문] 상대 몬스터 전체에 2 데미지, HP 5 회복.", Effects.Seq(Effects.DamageAllEnemies(2), Effects.Heal(5)));
            oppFireCaptain = StoryHero("SH10", "화염 경비대장", Element.Fire, 36, 3, "SH10", "[불꽃 창진] 상대에게 4 데미지.", Effects.Burn(4));
            oppLakeCaptain = StoryHero("SH11", "호수 근위대장", Element.Water, 38, 2, "SH11", "[물의 방벽] HP 8 회복, 이번 타임 아군 몬스터 HP +2.", Effects.Seq(Effects.Heal(8), Effects.FortifyAllies(2)));
            oppStormSpirit = StoryHero("SH12", "잿빛 폭풍", Element.Wind, 40, 3, "SH12", "[회색 돌풍] 상대 몬스터 전체에 2 데미지, 카드 1장 드로우.", Effects.Seq(Effects.DamageAllEnemies(2), Effects.Draw(1)));
            oppGiant = StoryHero("SH13", "안개 거인", Element.None, 52, 4, "SH13", "[대지 강타] 상대 몬스터 전체에 3 데미지.", Effects.DamageAllEnemies(3));
            oppLieutenant = StoryHero("SH14", "회색의 부관 세레스", Element.None, 46, 3, "SH14", "[색 지우기] 상대 몬스터 1장을 파괴한다.", Effects.DestroyEnemyMonster(1));
            oppPaladin = StoryHero("SH15", "성기사단장 아우렐", Element.Light, 46, 3, "SH15", "[심판의 빛] 상대 몬스터 1장에게 4 데미지, HP 4 회복.", Effects.Seq(Effects.StrikeEnemy(4), Effects.Heal(4)));
            oppDarkGeneral = StoryHero("SH16", "어둠의 장군 모르드", Element.Dark, 46, 3, "SH16", "[그림자 진군] 이번 타임 아군 몬스터 ATK +2, 카드 1장 드로우.", Effects.Seq(Effects.BuffAllies(2), Effects.Draw(1)));
            oppGreyLegion = StoryHero("SH17", "회색 군단", Element.None, 50, 3, "SH17", "[무색의 물결] 이번 타임 아군 몬스터 ATK +2.", Effects.BuffAllies(2));
            oppLord = StoryHero("SH18", "망각의 군주", Element.None, 70, 5, "SH18", "[망각] 상대 몬스터 2장을 파괴한다.", Effects.DestroyEnemyMonster(2));
            oppLord2 = StoryHero("SH19", "망각의 군주 · 해방", Element.None, 60, 4, "SH18", "[색의 포식] 상대 몬스터 전체에 3 데미지, HP 6 회복.", Effects.Seq(Effects.DamageAllEnemies(3), Effects.Heal(6)));
            oppLordFinal = StoryHero("SH20", "망각의 군주 · 최후", Element.None, 64, 4, "SH20", "[무(無)의 저편] 상대 몬스터 1장을 파괴하고 상대에게 4 데미지.", Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Burn(4)));
            oppTentacleLord = StoryHero("SH21", "망각의 팔", Element.None, 48, 3, "SM05", "[휘감기] 상대 몬스터 1장에게 4 데미지, 상대에게 2 데미지.", Effects.Seq(Effects.StrikeEnemy(4), Effects.Burn(2)));
            oppShadowKai = StoryHero("SH22", "회색의 카일", Element.None, 44, 2, "SH22", "[척력 왜곡] 상대 몬스터 1장에게 4 데미지, 카드 1장 드로우.", Effects.Seq(Effects.StrikeEnemy(4), Effects.Draw(1)));
            oppDevourer = StoryHero("SH23", "공허의 포식자", Element.None, 46, 3, "SM06", "[삼키기] 상대 몬스터 1장을 파괴하고 HP 4 회복.", Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Heal(4)));
            oppPhantomLeon = StoryHero("SH24", "환영의 레온", Element.Fire, 42, 3, "SH24", "[잿빛 홍염] 상대 몬스터 1장을 파괴하고 상대에게 2 데미지.", Effects.Seq(Effects.DestroyEnemyMonster(1), Effects.Burn(2)));
            oppCore = StoryHero("SH25", "망각의 핵", Element.None, 56, 2, "SH25", "[무한 증식] HP 10 회복, 카드 2장 드로우.", Effects.Seq(Effects.Heal(10), Effects.Draw(2)));
            oppRemnant = StoryHero("SH26", "안개의 잔재", Element.None, 30, 2, "SH26", "[마지막 그림자] 상대 몬스터 1장에게 3 데미지.", Effects.StrikeEnemy(3));
        }

        static Func<System.Random, List<CardData>> GreyDeck(int soldiers, int tentacles, int shards, int strong) => rng =>
        {
            var d = new List<CardData>();
            for (int i = 0; i < soldiers; i++) d.Add(GreySoldier);
            for (int i = 0; i < tentacles; i++) d.Add(Tentacle);
            for (int i = 0; i < shards; i++) d.Add(VoidShard);
            d.AddRange(Pick(Mons(Element.None, 5, 9), strong, rng));
            d.AddRange(Pick(CardList.Magics, 4, rng));
            while (d.Count < 30) d.AddRange(Pick(Mons(Element.None, 1, 5), 1, rng));
            return d;
        };

        /// <summary>두 속성을 섞은 덱 (협공·합동 상대)</summary>
        static Func<System.Random, List<CardData>> DualDeck(Element a, Element b, int strong) => rng =>
        {
            var d = new List<CardData>();
            d.AddRange(Pick(Mons(a, 1, 4), 7, rng));
            d.AddRange(Pick(Mons(b, 1, 4), 7, rng));
            d.AddRange(Pick(Mons(a, 5, 10).Concat(Mons(b, 5, 10)), 4 + strong, rng));
            d.AddRange(Pick(CardList.Magics, 7, rng));
            while (d.Count < 30) d.AddRange(Pick(Mons(a, 1, 5), 1, rng));
            return d;
        };

        /// <summary>프리즘 카일 덱: 무지개 몬스터 + 무속성 몬스터 + 마법 + 무지개 필드</summary>
        static List<CardData> PrismDeck(System.Random rng)
        {
            var d = new List<CardData>();
            d.AddRange(Pick(Mons(Element.Rainbow, 1, 3), 7, rng));
            d.AddRange(Pick(Mons(Element.Rainbow, 4, 10), 6, rng));
            d.AddRange(Pick(Mons(Element.None, 1, 3), 4, rng));
            d.AddRange(Pick(Mons(Element.None, 5, 10), 3, rng));
            d.AddRange(Pick(CardList.Magics, 7, rng));
            d.AddRange(CardList.Fields.Where(f => f.element == Element.Rainbow).Take(3));
            while (d.Count < 30) d.AddRange(Pick(Mons(Element.Rainbow, 1, 5), 1, rng));
            return d;
        }

        static StorySupport Sup(string who, string skill, StorySupport.Kind kind, int amount) => new StorySupport { who = who, skill = skill, kind = kind, amount = amount };
    }
}
