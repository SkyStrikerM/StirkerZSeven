using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    // ══════════════════════════ 스토리 데이터 구조 ══════════════════════════

    /// <summary>대사 1줄. who = 화자 id (비우면 내레이션, "#title" = 장 제목 카드). cond: 0 항상 · 1 승리 시 · 2 패배 시</summary>
    public class StoryLine
    {
        public string who, text, fx;
        public int cond;
    }

    /// <summary>장면 1개: 배경 일러스트 + 대사 묶음</summary>
    public class StoryScene
    {
        public string bg;
        public readonly List<StoryLine> lines = new List<StoryLine>();
    }

    /// <summary>튜토리얼 코치 팁: key = 언제 보여줄지 (start · main · battle · hero · magic · end)</summary>
    public class StoryTip
    {
        public string key, title, text;
    }

    /// <summary>스테이지 전투 설정</summary>
    public class StoryBattle
    {
        public string oppName = "???";
        public CardData oppHero;
        public int oppHp;                         // 0 이면 영웅 기본 HP
        public Func<System.Random, List<CardData>> oppDeck;
        public CardData field;
        public string goal = "상대 영웅의 HP를 0으로 만들어라";
        /// <summary>회색 안개: 속성 몬스터(무속성 제외) ATK - mist</summary>
        public int mist;
        /// <summary>동료 지원: 내 타임 시작 시 아리아 회복 · 레온 화염 (상대 영웅 데미지)</summary>
        public int ariaHeal, leonFire;
        /// <summary>그 밖의 동료 지원 (대마법사들) — 내 타임 시작 시 차례로</summary>
        public readonly List<StorySupport> supports = new List<StorySupport>();
        /// <summary>원소 흡수: 내 몬스터 ATK + absorb (카일이 타인의 마력을 머금는 연출)</summary>
        public int absorb;
        /// <summary>플레이어 영웅·덱·HP 교체 (각성한 프리즘 카일 등). null/0 이면 카일 기본</summary>
        public CardData playerHero;
        public Func<System.Random, List<CardData>> playerDeck;
        public int playerHp;
        /// <summary>져도 스토리가 이어지는 이벤트 전투</summary>
        public bool allowLose;
        /// <summary>상대는 타임마다 몬스터를 이 장수까지만 소환 (CPU 가 패를 한꺼번에 깔지 않게)</summary>
        public int summonCap = 2;
        public readonly List<StoryTip> tips = new List<StoryTip>();
    }

    /// <summary>동료 지원 1건: Heal 내 영웅 회복 · Burn 상대 영웅 피해 · Atk 이번 타임 내 몬스터 ATK · Strike 상대 몬스터 1장 피해</summary>
    public class StorySupport
    {
        public enum Kind { Heal, Burn, Atk, Strike }
        public string who, skill;
        public Kind kind;
        public int amount;
    }

    public class StoryStage
    {
        public string id, title, subtitle, synopsis, thumb;
        public int number, rewardOpal;
        public string rewardHero;   // 첫 클리어 시 해금되는 영웅 id
        public StoryVolume volume;
        public readonly List<StoryScene> pre = new List<StoryScene>();
        public readonly List<StoryScene> post = new List<StoryScene>();
        public StoryBattle battle;
    }

    public class StoryVolume
    {
        public int number;
        public string id, title, tagline, cover;
        public bool ready;
        public readonly List<StoryStage> stages = new List<StoryStage>();
    }

    /// <summary>화자 표시: 이름 · 이름표 색 · 초상화 (Art/Story/face_{id})</summary>
    public class StorySpeaker
    {
        public string name, face;
        public Color color;
    }

    // ══════════════════════════ 메인 시나리오 ══════════════════════════

    /// <summary>
    /// 메인 시나리오 (10장 구성). 제1장 = 스테이지 7개: 컷신(전/후) + 전투.
    /// 일러스트: Resources/StrikerZ/Art/Story/{bg}.png (Tools/story 에서 VARCO 로 생성)
    /// </summary>
    public static partial class StoryData
    {
        public const string Root = GameAssets.Root + "Art/Story/";

        public static readonly Dictionary<string, StorySpeaker> Speakers = new Dictionary<string, StorySpeaker>
        {
            ["kai"] = new StorySpeaker { name = "카일", face = "kai", color = new Color(0.85f, 0.88f, 0.95f) },
            ["aria"] = new StorySpeaker { name = "아리아", face = "aria", color = new Color(0.45f, 0.8f, 1f) },
            ["leon"] = new StorySpeaker { name = "레온", face = "leon", color = new Color(1f, 0.45f, 0.35f) },
            ["bane"] = new StorySpeaker { name = "베인 교수", face = "bane", color = new Color(0.55f, 0.95f, 0.65f) },
            ["gregor"] = new StorySpeaker { name = "그레고르", face = "gregor", color = new Color(0.95f, 0.85f, 0.35f) },
            ["stuA"] = new StorySpeaker { name = "학생 A", color = new Color(0.75f, 0.75f, 0.8f) },
            ["stuB"] = new StorySpeaker { name = "학생 B", color = new Color(0.75f, 0.75f, 0.8f) },
            ["inst"] = new StorySpeaker { name = "실기 교관", color = new Color(0.8f, 0.75f, 0.6f) },
            ["lumienQ"] = new StorySpeaker { name = "???", color = new Color(1f, 0.95f, 0.7f) },
            ["stag"] = new StorySpeaker { name = "숲의 주인", color = new Color(1f, 0.85f, 0.45f) },
            ["varusQ"] = new StorySpeaker { name = "???", color = new Color(0.6f, 0.6f, 0.62f) },
            ["ignis"] = new StorySpeaker { name = "이그니스", face = "ignis", color = new Color(1f, 0.55f, 0.25f) },
            ["meril"] = new StorySpeaker { name = "메릴", face = "meril", color = new Color(0.55f, 0.85f, 1f) },
            ["terra"] = new StorySpeaker { name = "테라", face = "terra", color = new Color(0.85f, 0.7f, 0.45f) },
            ["zephyr"] = new StorySpeaker { name = "제퍼", face = "zephyr", color = new Color(0.55f, 1f, 0.75f) },
            ["volt"] = new StorySpeaker { name = "볼트", face = "volt", color = new Color(0.6f, 0.75f, 1f) },
            ["lumien"] = new StorySpeaker { name = "루미엔", face = "lumien", color = new Color(1f, 0.95f, 0.65f) },
            ["erebos"] = new StorySpeaker { name = "에레보스", face = "erebos", color = new Color(0.8f, 0.6f, 1f) },
            ["varus"] = new StorySpeaker { name = "바루스", face = "varus", color = new Color(0.72f, 0.72f, 0.75f) },
            ["prisma"] = new StorySpeaker { name = "프리스마", face = "prisma", color = new Color(1f, 0.9f, 1f) },
            ["lord"] = new StorySpeaker { name = "망각의 군주", color = new Color(0.65f, 0.65f, 0.7f) },
            ["elder"] = new StorySpeaker { name = "마을 촌장", color = new Color(0.75f, 0.75f, 0.75f) },
            ["child"] = new StorySpeaker { name = "아이", color = new Color(0.8f, 0.8f, 0.85f) },
            ["guard"] = new StorySpeaker { name = "경비대장", color = new Color(0.9f, 0.7f, 0.55f) },
            ["knight"] = new StorySpeaker { name = "성기사", color = new Color(1f, 0.92f, 0.7f) },
            ["soldier"] = new StorySpeaker { name = "연합군 병사", color = new Color(0.8f, 0.8f, 0.75f) },
            ["mord"] = new StorySpeaker { name = "어둠의 장군 모르드", color = new Color(0.7f, 0.55f, 0.85f) },
            ["shadowkai"] = new StorySpeaker { name = "회색의 카일", color = new Color(0.62f, 0.62f, 0.66f) },
        };

        // ── 스토리 전용 카드 ──

        /// <summary>주인공 카일 (무속성 영웅). 처음 시작할 때 카일 스타터 덱과 함께 지급</summary>
        public static readonly CardData Kai = new CardData
        {
            id = "H017", name = "카일", type = CardType.Hero, element = Element.None, rarity = Rarity.Rare, heroHp = 50, heroAtk = 2,
            text = "[척력 반사] 상대 몬스터 1장을 주인의 패로 튕겨내고, 그 몬스터의 ATK만큼(최대 4) 상대에게 데미지.",
            effect = RepulseReflect,
        };

        static IEnumerator RepulseReflect(GameManager g, CardInstance c)
        {
            var targets = g.EffectTargets(c.owner.opponent);
            if (targets.Count == 0) yield break;
            CardInstance chosen = null;
            yield return g.ChooseCard(c.owner, targets, $"[{c.Name}] 튕겨낼 상대 몬스터를 선택", t => g.EffectiveAtk(t) * 2 + g.CurHp(t), r => chosen = r);
            if (chosen == null) yield break;
            int dmg = Mathf.Min(4, g.EffectiveAtk(chosen));
            yield return g.ReturnToHand(chosen, c.Name);
            if (dmg > 0 && !g.gameOver) yield return g.DamagePlayer(c.owner.opponent, dmg, c.Name);
        }

        static CardData StoryHero(string id, string name, Element e, int hp, int atk, string art, string text, Func<GameManager, CardInstance, IEnumerator> fx) => new CardData
        {
            id = id, name = name, type = CardType.Hero, element = e, rarity = Rarity.Rare, heroHp = hp, heroAtk = atk,
            art = art == null ? null : Root + art, text = text, effect = fx,
        };

        static CardData Beast(string id, string name, int atk, int hp, int minTime, DiceEffect[] dice, string text, bool taunt = false, int attacks = 1, bool noCounter = false) => new CardData
        {
            id = id, name = name, type = CardType.Monster, element = Element.None, rarity = Rarity.Uncommon, atk = atk, hp = hp, minTime = minTime,
            dice = dice, taunt = taunt, attackCount = attacks, noCounter = noCounter,
            text = ((taunt ? "[도발] " : "") + (attacks > 1 ? "[연속 공격] " : "") + (noCounter ? "[암살] " : "") + text).Trim(),
        };

        public static readonly CardData GreyWolf = Beast("SM01", "잿빛 늑대", 4, 3, 1, Dice.Of(Dice.Atk(2), Dice.Pierce(2), Dice.Shield(2)), "색을 잃고 회색 안개를 두른 늑대.");
        public static readonly CardData GreyBoar = Beast("SM02", "잿빛 멧돼지", 3, 7, 3, Dice.Of(Dice.Shield(2), Dice.Guard(), Dice.Atk(2)), "돌처럼 굳은 엄니로 길을 막는다.", taunt: true);
        public static readonly CardData GreyCrow = Beast("SM03", "잿빛 까마귀", 3, 2, 2, Dice.Of(Dice.Atk(2), Dice.Weaken(2), Dice.Pierce(1).Atk(1)), "안개 속에서 소리 없이 내려꽂힌다.", noCounter: true);

        static readonly CardData Doll = StoryHero("SH01", "마도 훈련 인형", Element.None, 18, 1, "SH01", "[자가 수리] HP 6 회복.", Effects.Heal(6));
        static readonly CardData Bane = StoryHero("SH02", "베인 교수", Element.Wind, 30, 2, "SH02", "[폭풍의 눈] 상대 몬스터 1장에게 3 데미지, 카드 1장 드로우.", Effects.Seq(Effects.StrikeEnemy(3), Effects.Draw(1)));
        static readonly CardData Gregor = StoryHero("SH03", "그레고르", Element.Thunder, 32, 3, "SH03", "[낙뢰 난무] 상대 몬스터 전체에 2 데미지.", Effects.DamageAllEnemies(2));
        static readonly CardData WolfPack = StoryHero("SH04", "잿빛 늑대 무리", Element.None, 30, 2, "SM01", "[무리 사냥] 이번 타임 아군 몬스터 ATK +2.", Effects.BuffAllies(2));
        static readonly CardData MistShade = StoryHero("SH05", "안개 속 그림자", Element.None, 32, 3, "SM03", "[색 먹기] 상대 몬스터 1장에게 4 데미지.", Effects.StrikeEnemy(4));
        static readonly CardData Stag = StoryHero("SH06", "잿빛 대록", Element.None, 44, 3, "SH06", "[잿빛 포효] 상대 몬스터 전체에 3 데미지.", Effects.DamageAllEnemies(3));

        // ── 덱 ──

        static List<CardData> Pick(IEnumerable<CardData> src, int n, System.Random rng) => src.OrderBy(_ => rng.Next()).Take(n).ToList();
        static IEnumerable<CardData> Mons(Element e, int minT, int maxT) => CardList.Monsters.Where(m => m.element == e && m.minTime >= minT && m.minTime <= maxT);

        /// <summary>카일 덱: 무속성 몬스터 20 + 마법 7 + 무속성 필드 3</summary>
        public static List<CardData> KaiDeck(System.Random rng)
        {
            var d = new List<CardData>();
            d.AddRange(Pick(Mons(Element.None, 1, 3), 11, rng));
            d.AddRange(Pick(Mons(Element.None, 4, 6), 7, rng));
            d.AddRange(Pick(Mons(Element.None, 7, 10), 2, rng));
            d.AddRange(Pick(CardList.Magics, 7, rng));
            d.AddRange(CardList.Fields.Where(f => f.element == Element.None).Take(2));
            d.Add(CardList.Fields.First(f => f.element == Element.None));
            return d;
        }

        static Func<System.Random, List<CardData>> ElementDeck(Element e, int low, int mid, int high, int magics) => rng =>
        {
            var d = new List<CardData>();
            d.AddRange(Pick(Mons(e, 1, 3), low, rng));
            d.AddRange(Pick(Mons(e, 4, 6), mid, rng));
            d.AddRange(Pick(Mons(e, 7, 10), high, rng));
            d.AddRange(Pick(CardList.Magics, magics, rng));
            while (d.Count < 30) d.AddRange(Pick(Mons(e, 1, 4), 1, rng));
            return d;
        };

        static Func<System.Random, List<CardData>> BeastDeck(int strong) => rng =>
        {
            var d = new List<CardData>();
            for (int i = 0; i < 4; i++) { d.Add(GreyWolf); d.Add(GreyCrow); }
            for (int i = 0; i < 3; i++) d.Add(GreyBoar);
            d.AddRange(Pick(Mons(Element.None, 1, 4), 9, rng));
            d.AddRange(Pick(Mons(Element.None, 5, 8), strong, rng));
            while (d.Count < 30) d.AddRange(Pick(Mons(Element.None, 1, 6), 1, rng));
            return d;
        };

        // ── 대본 작성 도우미 ──

        static StoryScene S(string bg, params StoryLine[] lines)
        {
            var s = new StoryScene { bg = bg };
            s.lines.AddRange(lines);
            return s;
        }
        static StoryLine N(string text, string fx = null, int cond = 0) => new StoryLine { who = "", text = text, fx = fx, cond = cond };
        static StoryLine L(string who, string text, string fx = null, int cond = 0) => new StoryLine { who = who, text = text, fx = fx, cond = cond };
        static StoryLine T(string title, string sub) => new StoryLine { who = "#title", text = title + "\n" + sub };
        static StoryTip Tip(string key, string title, string text) => new StoryTip { key = key, title = title, text = text };

        static CardData FieldNamed(string name) => CardList.FieldNamed(name) ?? throw new ArgumentException("필드 카드 이름 오타: " + name);

        static List<StoryVolume> volumes;
        /// <summary>CardList 와 서로 참조하므로 처음 쓸 때 만든다 (정적 초기화 순환 방지)</summary>
        public static List<StoryVolume> Volumes => volumes ?? (volumes = BuildVolumes());

        /// <summary>스토리 해금 영웅(카일)은 해금 전까지 영웅 목록·CPU 선택에서 빠진다</summary>
        public static bool HeroAvailable(CardData h)
        {
            if (h == Kai || Profile.HeroUnlocked(h.id)) return true;
            // 스토리 영웅: 해금 스테이지를 클리어했으면 사용 가능 (이미 클리어한 저장 데이터에도 소급)
            var st = Volumes.SelectMany(v => v.stages).FirstOrDefault(s => s.rewardHero == h.id);
            return st == null ? !StoryHeroIds.Contains(h.id) : Profile.StoryCleared(st.id);
        }

        public static StoryStage Stage(string id) => Volumes.SelectMany(v => v.stages).FirstOrDefault(s => s.id == id);

        static List<StoryVolume> BuildVolumes()
        {
            return new List<StoryVolume> { Volume1(), Volume2(), Volume3(), Volume4(), Volume5(), Volume6(), Volume7(), Volume8(), Volume9(), Volume10() };
        }

        static StoryVolume Volume1()
        {
            var v = new StoryVolume { number = 1, id = "V1", title = "색 잃은 소년", tagline = "투명한 척력이 회색을 밀어낸 날", cover = "v1_7b", ready = true };
            StoryStage Add(StoryStage s) { s.volume = v; s.number = v.stages.Count + 1; s.id = $"V1S{s.number}"; v.stages.Add(s); return s; }

            // ─────────────── 1. 버려진 돌 ───────────────
            var s1 = Add(new StoryStage
            {
                title = "버려진 돌", subtitle = "튜토리얼 · 소환과 공격", thumb = "v1_1b", rewardOpal = 50,
                synopsis = "색이 없다는 이유로 '버려진 돌'이라 불리는 소년 카일. 퇴학이 걸린 실기 평가가 시작된다.",
                battle = new StoryBattle
                {
                    oppName = "마도 훈련 인형", oppHero = Doll, oppHp = 18, field = FieldNamed("고요한 평원"), summonCap = 1,
                    oppDeck = rng => { var d = new List<CardData>(); while (d.Count < 26) d.AddRange(Pick(Mons(Element.None, 1, 3), 4, rng)); return d; },
                    goal = "실기 평가: 훈련 인형을 쓰러뜨려라",
                },
            });
            s1.battle.tips.Add(Tip("start", "실기 평가 시작", "시작 패는 7장. T2 부터 T 번호만큼(최대 7장) 드로우하고, 패는 9장까지 들 수 있어요. 상대 영웅의 HP를 0으로 만들면 승리!"));
            s1.battle.tips.Add(Tip("main", "몬스터 소환", "패의 몬스터 카드를 우클릭해 소환하세요 (왼쪽 클릭은 카드 확대). 카드 왼쪽 위의 T 숫자보다 지금 타임이 커야 소환할 수 있습니다. 준비가 끝나면 [배틀 페이즈]."));
            s1.battle.tips.Add(Tip("battle", "드래그해서 공격", "내 몬스터를 끌어 상대 몬스터나 영웅에게 놓으면 공격합니다. 화살표 색은 몬스터의 속성. [도발] 몬스터가 있으면 그쪽을 먼저 쳐야 해요."));
            s1.battle.tips.Add(Tip("end", "엔드 페이즈", "타임이 끝날 때 패를 전부 버리고 새로 7장을 받을지, 그대로 둘지 고를 수 있습니다."));
            s1.pre.Add(S("v1_p0",
                T("제1장", "색 잃은 소년"),
                N("세상은 일곱 빛깔로 이루어져 있다. 불, 물, 바람, 대지, 번개, 빛, 그리고 어둠."),
                N("프리스마 대륙의 모든 생명은 저마다의 색을 품고 태어난다. 그 색이 곧 마법이요, 존재의 증명이었다."),
                L("lumienQ", "……빛이…… 먹히고 있어……. 누군가…… 이 회색을……", "flash"),
                N("──그리고 아주 드물게, 아무 색도 품지 못한 아이가 태어난다.")));
            s1.pre.Add(S("v1_1a",
                N("프리스마 왕립 마법 학교. 대륙 각지에서 가장 빛나는 색들이 모이는 곳."),
                L("stuA", "봐, 레온이 또 화염구를 세 개나 동시에 띄웠어!"),
                L("stuB", "역시 불의 명가 출신이야. 우리랑은 급이 다르다니까.")));
            s1.pre.Add(S("v1_1b",
                L("kai", "……후우. 집중, 집중."),
                N("카일이 손바닥을 펴자, 보이지 않는 힘이 돌멩이를 툭 밀어낸다. 색도, 빛도, 소리도 없이."),
                L("stuA", "푸핫, 또 돌멩이 굴리기야? 무속성은 진짜 할 줄 아는 게 그것뿐이네."),
                L("leon", "그만둬. '버려진 돌'한테 말 걸어봤자 시간 낭비야."),
                L("kai", "……레온."),
                L("leon", "오후 실기 평가, 훈련 인형한테까지 지면 그땐 정말 퇴학이겠지. 짐 쌀 준비나 해 둬."),
                L("kai", "(색이 없다고 아무것도 못 하는 건 아니야. ……오늘, 증명해 보이겠어.)")));
            s1.post.Add(S("v1_1b",
                N("삐걱거리며 무너지는 훈련 인형. 교관의 기록판에 겨우 '합격'이 적힌다."),
                L("inst", "……합격. 마법 점수는 0점이지만, 전술 점수가 만점이라 간신히 넘겼다."),
                L("kai", "(간신히라도 좋아. 아직, 여기 있을 수 있어.)"),
                N("훈련장 담장 너머. 회색 수염의 노교수가 흥미롭다는 듯 턱을 쓰다듬고 있었다.")));

            // ─────────────── 2. 바람의 교수 ───────────────
            var s2 = Add(new StoryStage
            {
                title = "바람의 교수", subtitle = "튜토리얼 · 판정 주사위와 영웅 능력", thumb = "v1_2a", rewardOpal = 50,
                synopsis = "은퇴한 베테랑, 바람의 베인 교수가 카일의 '타이밍'에 주목한다. 내 바람을 한 번이라도 튕겨내 봐라.",
                battle = new StoryBattle
                {
                    oppName = "베인 교수", oppHero = Bane, oppHp = 28, field = FieldNamed("천공의 정원"),
                    oppDeck = ElementDeck(Element.Wind, 12, 8, 2, 8),
                    goal = "베인 교수의 바람을 튕겨내라",
                },
            });
            s2.battle.tips.Add(Tip("start", "판정 주사위", "전투가 붙으면 주사위를 굴려 1·2 / 3·4 / 5·6 중 하나의 효과를 고릅니다. 바람 속성은 4번 굴려 고를 폭이 넓어요."));
            s2.battle.tips.Add(Tip("hero", "영웅 능력: 척력 반사", "카일의 능력은 매치에 한 번! 상대 몬스터를 패로 튕겨내고 그 ATK만큼 피해를 돌려줍니다. 큰 몬스터가 나왔을 때 아껴 두세요."));
            s2.battle.tips.Add(Tip("battle", "영웅 반격", "영웅을 직접 치면 상대 영웅의 반격(ATK)을 받습니다. 몬스터 HP가 낮으면 반격에 쓰러질 수 있어요."));
            s2.pre.Add(S("v1_2a",
                L("bane", "거기 무속성 꼬마. 아까 그 인형, 마법이 아니라 '타이밍'으로 쓰러뜨렸지?"),
                L("kai", "베, 베인 교수님!? 저는 그냥…… 밀어낸 것뿐인데요."),
                L("bane", "밀어낸 것뿐, 이라. 다른 녀석들은 자기 색에 기대 마법을 '쏘지'. 하지만 넌 상대의 힘이 닿는 순간을 '읽고' 있더군."),
                L("bane", "내 현역 시절 별명이 '폭풍의 눈'이었다. 폭풍 한가운데가 가장 고요하다는 뜻이지. 너도 그 고요함을 가졌어."),
                L("kai", "……가르쳐 주시는 건가요? 저 같은 무속성을?"),
                L("bane", "가르치는 게 아니라 확인하는 거다. 덤벼 봐라. 내 바람을 한 번이라도 튕겨내면, 방과 후 특별 수업을 열어 주지.", "shake")));
            s2.post.Add(S("v1_2b",
                N("돌풍이 휘몰아친 순간── 카일의 손바닥 앞에서 바람이 거꾸로 꺾였다.", "shake"),
                L("bane", "……하. 내 돌풍을 정면에서 되받아치다니."),
                L("bane", "잘 들어라, 카일. 무속성은 '아무것도 없는' 게 아니다. '아무것에도 물들지 않은' 거지."),
                L("bane", "원소는 서로 간섭한다. 불은 물에 꺼지고, 물은 번개에 갈라지지. 하지만 네 척력은 그 무엇과도 섞이지 않아. 그러니 그 무엇이든 밀어낼 수 있다."),
                L("kai", "아무것에도…… 물들지 않는……."),
                L("bane", "매일 해 질 녘, 서쪽 훈련장. 늦으면 바람으로 날려 버릴 테니 각오해라."),
                L("kai", "네! ……네!!")));

            // ─────────────── 3. 물빛의 손 ───────────────
            var s3 = Add(new StoryStage
            {
                title = "물빛의 손", subtitle = "튜토리얼 · 마법 카드 (t/half · t0)", thumb = "v1_3a", rewardOpal = 50,
                synopsis = "상급생 그레고르의 괴롭힘. 그 앞을 막아선 것은 물의 치유사, 아리아였다.",
                battle = new StoryBattle
                {
                    oppName = "그레고르", oppHero = Gregor, oppHp = 30, field = FieldNamed("뇌운의 탑"), ariaHeal = 2,
                    oppDeck = ElementDeck(Element.Thunder, 11, 7, 2, 10),
                    goal = "아리아와 함께 그레고르를 막아라",
                },
            });
            s3.battle.tips.Add(Tip("start", "동료 지원: 아리아", "이번 전투에서는 아리아가 함께합니다. 내 타임이 시작될 때마다 아리아가 HP를 2 회복해 줘요."));
            s3.battle.tips.Add(Tip("magic", "마법 카드", "t/half 마법은 내 메인·배틀 페이즈에 바로 쓰거나 칸에 세트해 둘 수 있고, t0 마법은 상대 타임의 정해진 순간(타임 시작 · 배틀 개시 · 배틀 엔드)에 반응해 발동합니다."));
            s3.pre.Add(S("v1_3a",
                L("gregor", "어이, 돌멩이. 요즘 베인 영감한테 과외 받는다며? 무속성 주제에 꼴사납게."),
                L("kai", "……비켜 주세요, 그레고르 선배. 수업에 늦습니다."),
                L("gregor", "선배한테 말대꾸? 번개 맛 좀 봐야 정신 차리겠네."),
                N("──번쩍!", "flash"),
                L("aria", "그만하세요!"),
                N("맑은 물의 장막이 번개를 삼키며 흩어진다. 푸른 머리의 소녀가 카일 앞을 막아섰다."),
                L("aria", "색이 없다는 이유만으로 사람을 괴롭히는 게, 상급생이 할 일인가요?"),
                L("gregor", "아리아……. 치유 마법사가 끼어들어서 뭘 하겠다고? 좋아, 둘 다 덤벼. 결투로 정리하자고."),
                L("aria", "카일, 내가 뒤를 받칠게. 이번엔 혼자 싸우지 마.")));
            s3.post.Add(S("v1_3b",
                N("해 질 녘의 의무실. 아리아의 손끝에서 번진 물빛이 카일의 멍을 천천히 씻어 낸다."),
                L("aria", "또 무리했지. 베인 교수님 수업 끝나자마자 결투라니."),
                L("kai", "……고마워, 아리아. 근데 왜 매번 나를 도와줘? 너까지 이상한 눈으로 보잖아."),
                L("aria", "색이 없으면 뭐 어때. 넌 한 번도 남을 탓하거나 포기한 적이 없잖아. 난 그게…… 어떤 마법보다 멋지다고 생각해."),
                L("kai", "……."),
                L("aria", "아, 맞다. 다음 주 학년 대련 상대, 들었어? ……레온이래.")));

            // ─────────────── 4. 레온과의 대련 ───────────────
            var s4 = Add(new StoryStage
            {
                title = "화염의 천재", subtitle = "이벤트 전투 · 져도 이야기가 이어집니다", thumb = "v1_4a", rewardOpal = 50,
                synopsis = "학년 대련, 상대는 학교 최고의 천재 레온. 색이 없는 자가 넘을 수 없는 벽이 있다고 그는 말한다.",
                battle = new StoryBattle
                {
                    oppName = "레온", oppHero = CardList.Heroes.First(h => h.id == "H001"), oppHp = 34, field = FieldNamed("작열의 화산"), allowLose = true,
                    oppDeck = ElementDeck(Element.Fire, 10, 8, 4, 8),
                    goal = "학년 대련: 레온의 불꽃을 견뎌라 (패배해도 진행)",
                },
            });
            s4.battle.tips.Add(Tip("start", "이벤트 전투", "이 전투는 져도 이야기가 이어집니다. 하지만 이긴다면……? 필드 카드가 속성 몬스터를 강하게 만든다는 것도 기억해 두세요."));
            s4.pre.Add(S("v1_4a",
                N("학년 대련. 원형 투기장을 가득 메운 학생들의 시선이 한곳에 쏠린다."),
                L("stuA", "'버려진 돌' 대 '화염의 천재'? 결과는 뻔하잖아."),
                L("leon", "기권해라, 카일. 다치는 꼴 보기 싫으니까."),
                L("kai", "싫어. 베인 교수님한테 배운 걸 시험해 볼 거야."),
                L("leon", "……교수님이 너한테 뭘 가르쳤든 상관없어. 색이 없는 녀석이 넘을 수 없는 벽이 있다는 걸, 오늘 똑똑히 보여 주지."),
                L("leon", "작열하라, 홍염의 결계!", "flash")));
            s4.post.Add(S("v1_4b",
                N("불꽃이 걷혔을 때, 카일은 투기장 바닥에 무릎을 꿇고 있었다.", null, 2),
                L("leon", "……이게 현실이다, 카일. 넌 '버려진 돌'이야.", null, 2),
                N("마지막 불꽃이 척력에 튕겨 나가고, 레온의 무릎이 처음으로 땅에 닿았다. 투기장이 얼어붙은 듯 조용해진다.", "shake", 1),
                L("leon", "……운이 좋았을 뿐이다. 착각하지 마.", null, 1),
                L("leon", "(……뭐지, 저 움직임은. 기시감이 하나도 없어. 마법도 아닌데, 내 불꽃이 닿기 직전에 꼭 '밀려난다'.)"),
                L("kai", "다음엔…… 더 버틸 거야. 그다음엔, 너를 넘을 거고."),
                L("leon", "흥. 말은 잘하는군."),
                L("aria", "(레온…… 저렇게 신경 쓰는 얼굴은 처음 봐.)")));

            // ─────────────── 5. 숲으로 스며든 회색 ───────────────
            var s5 = Add(new StoryStage
            {
                title = "숲으로 스며든 회색", subtitle = "특수 규칙 · 회색 안개", thumb = "v1_5b", rewardOpal = 50,
                synopsis = "학교 외곽 '속삭임의 숲'의 나뭇잎이 색을 잃었다. 야간 순찰에 나선 세 사람 앞에 잿빛 짐승들이 나타난다.",
                battle = new StoryBattle
                {
                    oppName = "잿빛 늑대 무리", oppHero = WolfPack, oppHp = 30, field = FieldNamed("고요한 평원"), mist = 1, ariaHeal = 2,
                    oppDeck = BeastDeck(2),
                    goal = "잿빛 짐승들을 몰아내라",
                },
            });
            s5.battle.tips.Add(Tip("start", "특수 규칙: 회색 안개", "안개가 원소를 먹어 치웁니다. 속성 몬스터(무속성 제외)는 ATK -1. 색이 없는 카일의 무속성 몬스터는 영향을 받지 않아요!"));
            s5.pre.Add(S("v1_5a",
                N("그날 밤. 학교 외곽 '속삭임의 숲'에서 이상한 보고가 올라왔다."),
                L("bane", "숲의 나뭇잎이…… 색을 잃고 있다고? 순찰조를 꾸려라. 카일, 아리아, 레온. 너희 셋이다."),
                L("leon", "왜 내가 저 돌멩이랑 같은 조인데."),
                L("bane", "가장 빠르고, 가장 질기고, 가장 안 죽을 조합이니까. 불만 있으면 살아서 돌아와서 해라."),
                N("숲에 발을 들이자, 공기부터가 달랐다."),
                L("aria", "이 안개…… 차가워. 아니, 차가운 게 아니야. 아무 느낌이 없어."),
                L("leon", "잎사귀가 잿빛이야. 꽃도, 이끼도, 전부."),
                L("kai", "소리도 안 들려. 벌레 소리조차……")));
            s5.pre.Add(S("v1_5b",
                N("──그때, 덤불을 찢고 무언가가 튀어나왔다. 털빛도, 눈빛도 모두 지워진 잿빛 늑대 무리.", "shake"),
                L("leon", "짐승 주제에 감히. 타올라라!"),
                N("레온의 화염이 늑대에 닿는 순간── 불꽃이 빨려 들어가듯 꺼졌다."),
                L("leon", "뭐……? 내 불이, 먹혔어?"),
                L("aria", "레온, 물러나! 수가 너무 많아!"),
                L("kai", "내가 앞에 설게. 아리아는 뒤에서 버텨 줘!")));
            s5.post.Add(S("v1_5b",
                L("kai", "하아, 하아…… 내 척력은…… 먹히지 않았어."),
                L("aria", "카일의 힘만 안개에 흡수되지 않아. 마치 안개 쪽이 '밀려나는' 것처럼."),
                L("leon", "(……색이 없으니까, 먹을 것도 없다는 건가.)"),
                N("숲 깊은 곳에서, 거대한 무언가가 낮게 울부짖었다.", "shake")));

            // ─────────────── 6. 통하지 않는 원소 ───────────────
            var s6 = Add(new StoryStage
            {
                title = "통하지 않는 원소", subtitle = "특수 규칙 · 짙은 회색 안개", thumb = "v1_6b", rewardOpal = 50,
                synopsis = "숲 중심부의 안개는 벽처럼 짙었다. 아리아의 물도, 레온의 불도 회색에 삼켜진다. 이제 남은 건 카일뿐.",
                battle = new StoryBattle
                {
                    oppName = "안개 속 그림자", oppHero = MistShade, oppHp = 32, field = FieldNamed("황혼의 전장"), mist = 2,
                    oppDeck = BeastDeck(4),
                    goal = "홀로 안개를 밀어내라 (동료 지원 없음)",
                },
            });
            s6.battle.tips.Add(Tip("start", "짙은 회색 안개", "속성 몬스터 ATK -2. 아리아와 레온은 쓰러져 지원할 수 없습니다. 무속성의 힘만으로 버텨 내세요."));
            s6.pre.Add(S("v1_6a",
                N("울음소리를 따라 숲 중심부에 다다른 세 사람. 그곳의 안개는 벽처럼 짙었다."),
                L("aria", "수호의 물방울이여──! ……어? 장막이, 녹아내려?"),
                L("leon", "홍염 연격! ……젠장, 쏘는 족족 회색에 삼켜진다!"),
                N("화려한 원소 마법이 안개에 닿을 때마다 색을 잃고 흩어진다. 굶주린 짐승이 먹이를 삼키듯이."),
                L("leon", "크윽……! 마력이…… 빨려 나가……"),
                L("aria", "레온! ……나도, 다리에 힘이……")));
            s6.pre.Add(S("v1_6b",
                L("kai", "둘 다 뒤로 물러나 있어."),
                L("leon", "바보냐! 원소도 안 통하는 상대한테 무속성이 뭘──"),
                L("kai", "원소가 안 통하니까. ……그러니까 내가 가는 거야."),
                L("kai", "(베인 교수님이 말했어. 내 척력은 그 무엇과도 섞이지 않는다고. ……그렇다면, 이 회색과도!)", "flash")));
            s6.post.Add(S("v1_6b",
                N("카일이 손을 내밀 때마다 안개가 한 걸음씩 물러난다. 그 자리에 희미하게, 잎사귀의 초록이 돌아왔다."),
                L("aria", "색이…… 돌아오고 있어."),
                L("leon", "(무속성이…… 회색을 밀어내고 있다고……?)"),
                N("그리고 마침내, 걷힌 안개의 장막 너머에──"),
                L("stag", "그르르르르……", "shake")));

            // ─────────────── 7. 투명한 기적 ───────────────
            var s7 = Add(new StoryStage
            {
                title = "투명한 기적", subtitle = "보스 · 숲의 주인", thumb = "v1_7a", rewardOpal = 200,
                synopsis = "안개에 먹힌 숲의 주인, 잿빛 대록. 카일이 길을 열고, 아리아와 레온이 색을 쏟아붓는다.",
                battle = new StoryBattle
                {
                    oppName = "잿빛 대록", oppHero = Stag, oppHp = 44, field = FieldNamed("황혼의 전장"), mist = 2, ariaHeal = 2, leonFire = 2,
                    oppDeck = BeastDeck(6),
                    goal = "보스: 잿빛 대록을 해방하라",
                },
            });
            s7.battle.tips.Add(Tip("start", "합동 작전", "카일이 안개를 밀어내는 사이, 내 타임마다 아리아가 HP 2 회복, 레온이 보스에게 화염 2 데미지! 안개는 여전히 짙습니다(속성 ATK -2)."));
            s7.pre.Add(S("v1_7a",
                N("숲의 주인. 수백 년 동안 이 숲을 지켜 온 거대한 사슴왕이, 잿빛 껍데기가 되어 서 있었다."),
                L("aria", "숲의 수호령…… 저렇게 고결하던 존재가, 안개에……"),
                L("leon", "눈이…… 비어 있어. 아무것도 담겨 있지 않아."),
                N("사슴왕이 뿔을 치켜드는 순간, 숲 전체의 안개가 그 몸으로 빨려 들어간다.", "shake"),
                L("kai", "아리아, 레온. 부탁이 있어."),
                L("kai", "내가 안개를 밀어낼게. 틈이 생기면── 그때 너희의 색을 쏟아부어 줘."),
                L("leon", "……명령하지 마라, 버려진 돌. 하지만 이번만은 따라 주지."),
                L("aria", "응. 네 뒤는 내가 지킬게.")));
            s7.post.Add(S("v1_7b",
                L("kai", "흩어져라아아아──!!", "flash"),
                N("투명한 파동이 숲을 가로질렀다. 색도, 빛도 없는 힘. 그러나 그 힘이 지나간 자리마다 잿빛이 벗겨지고 본래의 색이 피어났다.", "shake"),
                N("초록의 잎. 붉은 열매. 그리고── 사슴왕의 눈동자에 깃드는 맑은 금빛."),
                L("stag", "……고맙다, 색 없는 아이여. 너는…… '그분'의 그릇을 닮았구나."),
                L("kai", "그릇……? 그게 무슨──"),
                N("대답 대신, 사슴왕은 빛의 입자가 되어 숲의 새벽 속으로 흩어졌다.")));
            s7.post.Add(S("v1_7c",
                L("leon", "……이봐, 카일."),
                L("kai", "응?"),
                L("leon", "오늘 일은…… 빚으로 해 두지. 언젠가 갚을 거다. 그러니 그 전에 멋대로 쓰러지지 마라."),
                L("aria", "(후훗. 레온도 참.)"),
                L("bane", "(……'간섭받지 않는 순수함'이라. 설마 했는데, 정말로 전설의 조각인가.)"),
                L("bane", "(회색 안개가 학교까지 손을 뻗었다면, 어딘가에 발원지가 있다. ……저 아이들에게 짐을 지우게 되겠군.)"),
                N("그날 아침, 카일을 '버려진 돌'이라 부르는 목소리는 조금 작아졌다.")));
            s7.post.Add(S("v1_7d",
                N("그리고 대륙의 반대편. 색이 모두 지워진 절벽 위에서──"),
                L("varusQ", "……색이 없는 자라. 흥미롭군."),
                N("회색의 사도가 처음으로, 낯선 '무색'의 기척에 고개를 들었다."),
                T("제1장 · 색 잃은 소년", "完 ── 다음 장 「회색의 전조」")));
            return v;
        }
    }
}
