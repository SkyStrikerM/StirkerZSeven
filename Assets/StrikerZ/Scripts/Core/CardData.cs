using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    public enum CardType { Monster, Magic, Hero, Field }
    public enum Rarity { Common, Uncommon, Rare, Unique }
    /// <summary>카드 가공: Seven = 은색 테두리·은빛 이름 (팩 10%), StrikerZ = 금색 테두리·금빛 이름 (팩 1%)</summary>
    public enum CardFinish { Normal, Seven, StrikerZ }
    public enum Element { None, Fire, Water, Wind, Earth, Thunder, Light, Dark, Rainbow }

    /// <summary>T0 = 지정된 타이밍(트리거)에 발동하는 마법, Normal = 일반 마법 — 메인(배치) 페이즈에 패에서 바로 발동 (2026-10-28, 예전 t/half 세트·배틀 발동 폐지)</summary>
    public enum MagicSpeed { Set, Normal }   // Set = 칸에 뒷면으로 놓아 두면 배틀 때 그 칸 차례에 공개·발동 (2026-10-29, 트리거 창 폐지) · Normal = 패에서 바로

    public enum Phase { None, TimeStart, Main, Battle, BattleEnd, End }
    /// <summary>Extra = 엑스트라 덱 (필드 카드, 뷰 없음 · 낼 때 만든다)</summary>
    public enum Zone { Deck, Hand, Slot, Grave, Center, Hero, Resolving, Extra }

    /// <summary>
    /// 카드 한 장의 정의. CardList.cs 의 리스트에 new CardData { ... } 로 추가한다.
    /// </summary>
    public class CardData
    {
        public string id;
        public string name;
        public CardType type;
        public Rarity rarity = Rarity.Common;
        public Element element = Element.None;
        public string text = "";
        /// <summary>Resources 경로 (예: "StrikerZ/Art/fire_dragon"). 비워두면 속성색 기본 아트.</summary>
        public string art;

        // ── 몬스터 ──
        public int atk;
        /// <summary>몬스터 HP. 피해가 쌓여 0 이 되면 파괴. 매 타임 시작 시 전부 회복 (상처 등 예외)</summary>
        public int hp;
        /// <summary>[재생] 배틀 엔드에 HP 를 모두 회복</summary>
        public bool regen;
        /// <summary>몬스터의 기본 비용 (예전 '소환 가능한 최소 타임' — 원소 제약으로 바뀐 뒤에는 같은 속성 원소 수)</summary>
        public int minTime = 1;
        /// <summary>소환 가능한 최대 타임 (0 = 제한 없음)</summary>
        public int maxTime = 0;
        /// <summary>낼 때 쓰는 원소 수. -1 = 자동 (몬스터 = minTime, 마법·필드 = 0). GameManager.Element.cs</summary>
        public int cost = -1;
        public int Cost => cost >= 0 ? cost : type == CardType.Monster ? minTime : 0;
        /// <summary>영웅 아래에 쌓는 원소 카드 (GameManager.ElementCardData)</summary>
        public bool isElement;

        /// <summary>비용 글: "화염 원소 3" · "원소 2 (아무 속성)" · "서로 다른 원소 4" · "없음"</summary>
        public string CostText()
        {
            int c = Cost;
            if (c <= 0) return "없음";
            if (element == Element.None) return $"원소 {c} (아무 속성)";
            if (element == Element.Rainbow) return $"서로 다른 원소 {c}";
            return $"{ElementName(element)} 원소 {c}";
        }
        /// <summary>자신의 효과로 뒷면 세로 표시 소환 가능</summary>
        public bool canSummonFaceDown;
        /// <summary>배틀페이즈 공격 횟수</summary>
        public int attackCount = 1;
        /// <summary>상대 몬스터 전원을 공격</summary>
        /// <summary>[연쇄] 전투로 상대 몬스터를 파괴하면 다음 강제 대상을 이어서 공격할 수 있다</summary>
        public bool attackAll;
        /// <summary>[암살] 직접 공격할 때 영웅의 반격을 받지 않는다</summary>
        public bool noCounter;
        /// <summary>[저격] 가장 뒤(오른쪽) 상대 소환수를 공격한다 ([도발]은 우선)</summary>
        public bool snipe;
        /// <summary>[선봉] 공격 턴에 칸 순서보다 먼저 공개·공격한다</summary>
        public bool vanguard;
        /// <summary>[불변] 파괴 외의 상대 카드 효과를 받지 않는다 (개기일식 중에도 행동)</summary>
        public bool resist;
        /// <summary>공격/발동 순서 우선도. 높을수록 먼저 (기본 0 = 왼쪽부터)</summary>
        public int priority;
        /// <summary>주사위 효과 [0]=1·2, [1]=3·4, [2]=5·6 (null 이면 Dice.Default)</summary>
        public DiceEffect[] dice;

        /// <summary>전투 판정 횟수: 주사위 1개 (바람 속성은 2개 중 선택) — 2026-10-28 3개(바람 4개)에서 변경</summary>
        public int DiceRolls => element == Element.Wind ? 2 : 1;

        /// <summary>빛 = 코인 앞면이면 이번 전투 ATK·HP +값, 어둠 = 코인 뒷면이면 ATK·HP -값 (타임 4까지 2, 8까지 3, 이후 4)</summary>
        public int CoinPower => type == CardType.Monster && (element == Element.Light || element == Element.Dark) ? (minTime <= 4 ? 2 : minTime <= 8 ? 3 : 4) : 0;

        /// <summary>속성 고유 규칙 한 줄 (없으면 null)</summary>
        public string ElementRule()
        {
            if (type != CardType.Monster) return null;
            switch (element)
            {
                case Element.Wind: return "<color=#9fe8b0>[질풍] 주사위 2개 중 선택</color>";
                case Element.Light: return $"<color=#fff2b0>[빛의 가호] 전투 시 코인 앞면: ATK·HP +{CoinPower}</color>";
                case Element.Dark: return $"<color=#d0a8ff>[어둠의 대가] 전투 시 코인 뒷면: ATK·HP -{CoinPower}</color>";
                default: return null;
            }
        }

        public DiceEffect DiceFor(int value)
        {
            var arr = dice ?? Dice.Default;
            return arr[Dice.Band(value)] ?? Dice.None();
        }

        public string DiceText()
        {
            var arr = dice ?? Dice.Default;
            string[] col = { "#8fb8ff", "#a8ffb0", "#ffd05c" };
            var lines = new string[3];
            for (int i = 0; i < 3; i++) lines[i] = $"<color={col[i]}><b>{Dice.BandNames[i]}</b></color> {(arr[i] ?? Dice.None()).Describe()}";
            var rule = ElementRule();
            return (rule != null ? rule + "\n" : "") + string.Join("\n", lines);
        }

        // ── 마법 ──
        public MagicSpeed speed = MagicSpeed.Normal;

        // ── 영웅 ──
        public int heroHp = 30;
        /// <summary>대전에서 실제로 쓰는 영웅 HP (동시 룰 배율 GameManager.HeroHpRate 적용) — 카드·덱 화면 표시용</summary>
        public int BattleHeroHp => Mathf.RoundToInt(heroHp * GameManager.HeroHpRate);
        /// <summary>영웅 공격력 (1~3): 직접 공격을 받으면 공격한 몬스터에게 이만큼 반격 데미지 (ATK 감소)</summary>
        public int heroAtk = 1;
        /// <summary>[도발] 상대 몬스터는 도발 몬스터부터 공격해야 한다 (앞면일 때만)</summary>
        public bool taunt;

        // ── 필드 ──
        /// <summary>필드 카드: 몬스터에게 주는 ATK 보정 (아군/적군 모두). 무속성 몬스터는 받지 않는다</summary>
        public Func<CardInstance, int> fieldAtkBonus;
        /// <summary>필드 카드: 몬스터 HP 보정</summary>
        public Func<CardInstance, int> fieldHpBonus;
        /// <summary>필드 카드: 전투 판정(주사위/코인) 횟수 보정</summary>
        public Func<CardInstance, int> fieldDiceBonus;

        /// <summary>
        /// 속성 상성: 이 속성이 이기는(약하게 만드는) 속성.
        /// 물 → 화염 → 바람 → 대지 → 번개 → 물 순환, 빛 ↔ 어둠 서로 상극.
        /// </summary>
        public static Element Beats(Element e)
        {
            switch (e)
            {
                case Element.Water: return Element.Fire;
                case Element.Fire: return Element.Wind;
                case Element.Wind: return Element.Earth;
                case Element.Earth: return Element.Thunder;
                case Element.Thunder: return Element.Water;
                case Element.Light: return Element.Dark;
                case Element.Dark: return Element.Light;
                default: return Element.None;
            }
        }

        /// <summary>필드 효과의 속성 조건: 같은 속성이거나 무지개(모든 속성 취급). 무속성 몬스터는 항상 제외</summary>
        public static bool FieldHits(CardInstance m, Element e)
        {
            var me = m.data.element;
            if (me == Element.None || e == Element.None) return false;
            if (e == Element.Rainbow) return me == Element.Rainbow;
            return me == e || me == Element.Rainbow;
        }

        /// <summary>필드 하나가 몬스터에게 주는 보정 (ATK, HP, 판정 횟수). 무속성 몬스터는 0</summary>
        public static (int atk, int hp, int dice) FieldDelta(CardData field, CardInstance m)
        {
            if (field == null || m == null || !m.IsMonster || m.data.element == Element.None) return (0, 0, 0);
            return (field.fieldAtkBonus?.Invoke(m) ?? 0, field.fieldHpBonus?.Invoke(m) ?? 0, field.fieldDiceBonus?.Invoke(m) ?? 0);
        }
        /// <summary>필드 카드: 이 필드 위에서는 누구나 뒷면 소환 가능</summary>
        public bool fieldAllowsFaceDown;

        // ── 효과 훅 ──
        /// <summary>발동 조건 (마법/영웅). null 이면 항상 가능</summary>
        public Func<GameManager, CardInstance, bool> canActivate;
        /// <summary>효과 처리. 마법=발동, 몬스터=앞면 소환/반전 시, 영웅=능력, 필드=등장 시</summary>
        public Func<GameManager, CardInstance, IEnumerator> effect;

        public bool IsHighRarity => rarity >= Rarity.Rare;

        public CardData Clone() => (CardData)MemberwiseClone();

        public string TypeLine()
        {
            switch (type)
            {
                case CardType.Monster:
                    return $"몬스터 · {ElementName(element)} · 비용 {Cost}" + (maxTime > 0 ? $" · T{maxTime}까지" : "");
                case CardType.Magic:
                    if (isElement) return $"원소 · {ElementName(element)}";
                    return speed == MagicSpeed.Set ? "마법 · 세트" : "마법 · 일반";
                case CardType.Hero: return $"영웅 · ATK {heroAtk} · HP {BattleHeroHp}";
                default: return $"필드 · {ElementName(element)}";
            }
        }

        public static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "화염";
                case Element.Water: return "물";
                case Element.Wind: return "바람";
                case Element.Earth: return "대지";
                case Element.Thunder: return "번개";
                case Element.Light: return "빛";
                case Element.Dark: return "어둠";
                case Element.Rainbow: return "무지개";
                default: return "무속성";
            }
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Fire: return new Color(1f, 0.32f, 0.22f);
                case Element.Water: return new Color(0.25f, 0.62f, 1f);
                case Element.Wind: return new Color(0.3f, 0.9f, 0.5f);
                case Element.Earth: return new Color(0.8f, 0.56f, 0.28f);
                case Element.Thunder: return new Color(1f, 0.86f, 0.25f);
                case Element.Light: return new Color(1f, 0.97f, 0.82f);
                case Element.Dark: return new Color(0.66f, 0.35f, 1f);
                case Element.Rainbow: return new Color(1f, 0.6f, 0.9f);
                default: return new Color(0.7f, 0.72f, 0.78f);
            }
        }

        public static string RarityLabel(Rarity r) => r.ToString();

        /// <summary>속성 색 (리치 텍스트용 RRGGBB)</summary>
        public static string ElementHex(Element e) => ColorUtility.ToHtmlStringRGB(ElementColor(e));

        /// <summary>등급 색 (리치 텍스트용 RRGGBB)</summary>
        public static string RarityHex(Rarity r) => ColorUtility.ToHtmlStringRGB(RarityColor(r));

        /// <summary>등급 색으로 칠한 등급 이름 (예: "&lt;color=#FFCC40&gt;Rare&lt;/color&gt;") + suffix</summary>
        public static string RarityRich(Rarity r, string suffix = "") => $"<color=#{RarityHex(r)}>{RarityLabel(r)}</color>{suffix}";

        /// <summary>팩 개봉 연출 강도 (0~5): Common 0 · Uncommon 1 · Rare 3 · Unique 5</summary>
        public static int RarityTier(Rarity r) => r == Rarity.Unique ? 5 : r == Rarity.Rare ? 3 : r == Rarity.Uncommon ? 1 : 0;

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.45f, 0.75f, 1f);
                case Rarity.Rare: return new Color(1f, 0.8f, 0.25f);
                case Rarity.Unique: return new Color(1f, 0.42f, 0.12f);
                default: return new Color(0.8f, 0.8f, 0.8f);
            }
        }
    }

    /// <summary>게임 중 실제로 존재하는 카드 한 장</summary>
    public class CardInstance
    {
        static int nextUid;
        public readonly int uid = ++nextUid;
        public readonly CardData data;
        public PlayerState owner;
        public Zone zone;
        public int slot = -1;
        public bool faceDown;
        /// <summary>타임 종료/배틀 종료 시 초기화되는 임시 ATK 보정</summary>
        public int tempAtk;
        /// <summary>약화: 다음 배틀 엔드까지 ATK 감소량 (주사위 "상대 ATK -n" · 약화 마법 등, 배틀 엔드에 0)</summary>
        public int weaken;
        public int priorityMod;
        /// <summary>이 타임 번호까지 공격 불능 (-1 = 없음)</summary>
        public int stunUntil = -1;
        /// <summary>이 타임 번호까지 [도발] 부여 (-1 = 없음)</summary>
        public int tauntUntil = -1;
        /// <summary>기간이 정해진 ATK 증감 (untilTime 타임까지 유효)</summary>
        public readonly List<AtkBuff> buffs = new List<AtkBuff>();
        public CardView view;
        /// <summary>표시용 가공 (게임 규칙에는 영향 없음)</summary>
        public CardFinish finish;
        /// <summary>받은 피해 (현재 HP = 최대 HP - damage). 타임 시작 시 0 으로 회복</summary>
        public int damage;
        /// <summary>이 타임 번호까지 타임 시작 회복 불가 ([상처])</summary>
        public int noRegenUntil = -1;
        /// <summary>기간이 정해진 HP 증감</summary>
        public readonly List<AtkBuff> hpBuffs = new List<AtkBuff>();
        /// <summary>동시 룰: 이번에 필드에 나온 뒤 소환(공개) 효과를 이미 냈다 — 개기일식으로 다시 뒷면이 됐다 공개돼도 다시 나오지 않는다</summary>
        public bool summonFxDone;

        /// <summary>필드를 떠날 때 초기화</summary>
        public void ClearFieldState()
        {
            summonFxDone = false;
            tempAtk = 0;
            weaken = 0;
            priorityMod = 0;
            stunUntil = -1;
            tauntUntil = -1;
            buffs.Clear();
            damage = 0;
            noRegenUntil = -1;
            hpBuffs.Clear();
        }

        public CardInstance(CardData data, PlayerState owner)
        {
            this.data = data;
            this.owner = owner;
        }

        public bool IsMonster => data.type == CardType.Monster;
        public bool IsMagic => data.type == CardType.Magic;
        public string Name => data.name;
        public int Priority => data.priority + priorityMod;
        public override string ToString() => data.name;
    }

    public struct AtkBuff
    {
        public int value;
        public int untilTime;
    }

    public class PlayerState
    {
        /// <summary>덱이 비었다는 안내를 이미 했다</summary>
        public bool deckOutWarned;
        public const int SlotCount = 7;

        public int index;
        public string name;
        public string avatar;
        public bool isHuman;
        public PlayerState opponent;

        public CardInstance hero;
        public int hp, maxHp;
        public bool heroUsed;
        public bool fieldPlayedThisTime;

        public readonly List<CardInstance> deck = new List<CardInstance>();
        public readonly List<CardInstance> hand = new List<CardInstance>();
        public readonly List<CardInstance> grave = new List<CardInstance>();
        /// <summary>엑스트라 덱: 필드 카드 (최대 10장, 같은 필드 1장씩) — T2 부터 필드 차례에 꺼내 전개</summary>
        public readonly List<CardInstance> extra = new List<CardInstance>();
        public readonly CardInstance[] slots = new CardInstance[SlotCount];

        // ── 원소 (GameManager.Element.cs): 영웅 아래 보유 · 이번 T 쓴 수 · 원소 덱 남은 장수 (속성 index) ──
        public readonly int[] elemPool = new int[9];
        public readonly int[] elemSpent = new int[9];
        public readonly int[] elemSupply = new int[9];
        /// <summary>비용 없이 낸다 (레이드 보스 · 퍼즐)</summary>
        public bool freeCost;
        public int Available(Element e) => Mathf.Max(0, elemPool[(int)e] - elemSpent[(int)e]);
        public int TotalAvailable { get { int n = 0; for (int i = 1; i <= 7; i++) n += Mathf.Max(0, elemPool[i] - elemSpent[i]); return n; } }

        public int FieldCount
        {
            get { int n = 0; foreach (var s in slots) if (s != null) n++; return n; }
        }

        public int FreeSlot()
        {
            for (int i = 0; i < SlotCount; i++) if (slots[i] == null) return i;
            return -1;
        }

        public List<CardInstance> Monsters()
        {
            var list = new List<CardInstance>();
            foreach (var s in slots) if (s != null && s.IsMonster) list.Add(s);
            return list;
        }
    }

    public enum StatusKind
    {
        /// <summary>대상 플레이어의 뒷면 카드를 앞면으로 공개 (봉화)</summary>
        RevealFaceDown,
        /// <summary>모든 몬스터는 뒷면 세로 표시로만 소환 (개기일식)</summary>
        FaceDownSummonOnly,
        /// <summary>대상 플레이어가 뒷면 소환 가능</summary>
        AllowFaceDown,
        /// <summary>대상 플레이어의 몬스터 ATK + value</summary>
        AtkBonus,
        /// <summary>대상 플레이어의 몬스터가 공격 대상을 자유롭게 선택 (도발 무시)</summary>
        FreeTarget,
    }

    public enum Expire
    {
        /// <summary>다음 배틀 엔드까지</summary>
        NextBattleEnd,
        /// <summary>현재 타임 종료까지</summary>
        EndOfTime,
        /// <summary>시전자의 다음 타임 시작까지 ("다음 턴까지")</summary>
        CasterNextTime,
        /// <summary>매치 종료까지</summary>
        Never,
    }

    public class Status
    {
        public StatusKind kind;
        /// <summary>대상 플레이어. null = 양쪽 모두</summary>
        public PlayerState target;
        public PlayerState caster;
        public Expire expire;
        public int value;
        public int createdTime;
        public string source;
    }
}
