using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 원소 제약 (2026-10-28, ver2 규칙을 ver1 에 이식 — 카드를 내는 조건이 T 번호에서 원소 비용으로 바뀜):
    ///   매 T 시작에 양쪽이 엑스트라 덱(원소 덱)에서 원소 카드 1장을 골라 영웅 카드 아래에 둔다.
    ///   쌓인 원소는 매 T 다시 쓴다 (MTG 대지식) → T7 이면 원소 7개.
    ///   카드의 비용(CardData.Cost = 몬스터의 minTime) 만큼 같은 속성 원소를 이번 T 에 쓴다 (예: 화염 2 = 화염 원소 2개).
    ///   무속성 카드는 아무 원소, 무지개 카드는 서로 다른 원소로 낸다. 마법은 기본 비용 0 (cost 로 지정 가능).
    ///   원소 덱 = 덱에 있는 속성마다 ElementDeckSize 장 (자동 · 덱 편집 없음). 레이드 보스·퍼즐은 비용 없음.
    ///   원소 상한 (영웅 속성 상성, ver2 2026-10-21 표): 자기 속성 10 · 이기는 속성 5 · 상관없는 속성 3 · 약점 1,
    ///   무지개 영웅은 전부 5, 무속성 영웅은 "백지 각인" — 덱에 가장 많은 속성 8 · 나머지 4 (약점 없음). 원소 덱 = 상한까지만.
    /// </summary>
    public partial class GameManager
    {
        public const int ElementDeckSize = 10;
        static readonly Element[] AllElements = { Element.Fire, Element.Water, Element.Wind, Element.Earth, Element.Thunder, Element.Light, Element.Dark };
        static bool IsBasic(Element e) => e >= Element.Fire && e <= Element.Dark;

        // ── 영웅 아래 원소 카드 (보드 표시용) ──
        readonly List<CardInstance>[] elemCards = { new List<CardInstance>(), new List<CardInstance>() };
        static readonly CardData[] elemCardData = new CardData[9];

        /// <summary>원소 카드 정의 (속성마다 하나, 보드·선택 창 공용)</summary>
        public static CardData ElementCardData(Element e)
            => elemCardData[(int)e] ?? (elemCardData[(int)e] = new CardData
            {
                id = "ELEM_" + e, name = CardData.ElementName(e) + " 원소", type = CardType.Magic, element = e, isElement = true, cost = 0,
                text = "영웅 아래에 놓는다. 쌓인 원소는 매 T 다시 쓴다 — 같은 속성 카드의 비용.",
                art = "StrikerZ/Art/Elements/" + e.ToString().ToLower(),
            });

        void ResetElementState()
        {
            foreach (var l in elemCards) l.Clear();     // 뷰는 board.ClearCards 가 지운다
        }

        // ── 원소 상한 (영웅 속성 상성) ──
        public static int CapOwn = 10, CapStrong = 5, CapNeutral = 3, CapWeak = 1, CapRainbow = 5;
        /// <summary>무속성 영웅 "백지 각인": 덱에 가장 많은 속성 CapImprint, 나머지 CapColorless, 약점 없음</summary>
        public static int CapImprint = 8, CapColorless = 4;

        /// <summary>내가 이기는 속성: 순환 상성은 Beats, 빛·어둠은 서로 약점이라 따로 (빛 → 번개, 어둠 → 바람)</summary>
        public static Element StrongPartner(Element hero)
            => hero == Element.Light ? Element.Thunder : hero == Element.Dark ? Element.Wind : CardData.Beats(hero);

        /// <summary>영웅 속성 hero 일 때 원소 e 를 영웅 아래에 몇 개까지 놓을 수 있는가 (imprint = 무속성 영웅의 각인 속성)</summary>
        public static int ElementCap(Element hero, Element e, Element imprint = Element.None)
        {
            if (hero == Element.Rainbow) return CapRainbow;
            if (!IsBasic(hero)) return e == imprint ? CapImprint : CapColorless;
            if (e == hero) return CapOwn;
            if (CardData.Beats(e) == hero) return CapWeak;        // 나를 이기는 속성 (빛 ↔ 어둠은 서로 약점)
            if (StrongPartner(hero) == e) return CapStrong;       // 내가 이기는 속성
            return CapNeutral;
        }

        /// <summary>무속성 영웅 각인 속성: 덱에 가장 많은 속성 (영웅·무속성·무지개 제외, 없으면 None)</summary>
        public static Element Imprint(IEnumerable<CardData> deck)
        {
            var g = deck.Where(c => c.type != CardType.Hero && IsBasic(c.element)).GroupBy(c => c.element).OrderByDescending(x => x.Count()).ThenBy(x => x.Key).FirstOrDefault();
            return g != null ? g.Key : Element.None;
        }

        /// <summary>원소 상한 표 한 줄 (덱 편집 영웅 상세용)</summary>
        public static string CapText(Element hero)
        {
            if (hero == Element.Rainbow) return $"모든 속성 {CapRainbow}";
            if (!IsBasic(hero)) return $"백지 각인 — 덱에 가장 많은 속성 {CapImprint} · 나머지 {CapColorless} (약점 없음)";
            return string.Join(" · ", AllElements.Select(e => $"<color=#{CardData.ElementHex(e)}>{CardData.ElementName(e)}</color> {ElementCap(hero, e)}"));
        }

        /// <summary>SetupPlayer: 덱에 있는 속성마다 원소 카드 (속성 카드가 없으면 영웅 속성, 그것도 없으면 7속성 전부) — 장수 = 영웅 상성 상한 (최대 ElementDeckSize)</summary>
        void SetupElementDeck(PlayerState p, List<CardData> cards)
        {
            Array.Clear(p.elemPool, 0, p.elemPool.Length);
            Array.Clear(p.elemSpent, 0, p.elemSpent.Length);
            Array.Clear(p.elemSupply, 0, p.elemSupply.Length);
            var heroEl = p.hero != null ? p.hero.data.element : Element.None;
            var imprint = IsBasic(heroEl) || heroEl == Element.Rainbow ? Element.None : Imprint(cards);
            var els = cards.Select(c => c.element).Where(IsBasic).Distinct().ToList();
            if (els.Count == 0 && IsBasic(heroEl)) els.Add(heroEl);
            if (els.Count == 0) els.AddRange(AllElements);
            foreach (var e in els) p.elemSupply[(int)e] = p.freeCost ? ElementDeckSize : Mathf.Min(ElementDeckSize, ElementCap(heroEl, e, imprint));
            if (!p.freeCost)
                Log($"{p.name}: 원소 덱 — " + string.Join(" · ", els.Select(e => $"{CardData.ElementName(e)} {p.elemSupply[(int)e]}")) + (imprint != Element.None ? $" (각인 {CardData.ElementName(imprint)})" : ""));
        }

        /// <summary>원소 덱에 남은 속성 (고를 수 있는 것)</summary>
        public List<Element> ElementOptions(PlayerState p)
            => p == null || p.freeCost ? new List<Element>() : AllElements.Where(e => p.elemSupply[(int)e] > 0).ToList();

        public int ElementDeckCount(PlayerState p) => p == null ? 0 : AllElements.Sum(e => p.elemSupply[(int)e]);

        /// <summary>원소 1장을 영웅 아래에 (원소 덱에서 빼서 풀에 더한다)</summary>
        public void TakeElement(PlayerState p, Element e)
        {
            if (p == null || !IsBasic(e) || p.elemSupply[(int)e] <= 0) return;
            p.elemSupply[(int)e]--;
            p.elemPool[(int)e]++;
            Log($"{p.name}: {CardData.ElementName(e)} 원소를 영웅 아래에 놓았다 ({p.elemPool[(int)e]}개)");
            SyncElementCards(p);
        }

        /// <summary>T 시작: 이번 T 쓴 원소를 되돌린다 (쌓인 원소는 매 T 다시 쓴다)</summary>
        void ResetElementSpent()
        {
            foreach (var p in players) if (p != null) Array.Clear(p.elemSpent, 0, p.elemSpent.Length);
        }

        /// <summary>동시 룰: 양쪽이 동시에 원소 1장 (CPU 는 내 선택을 보지 않고 먼저 정한다)</summary>
        IEnumerator ElementStep()
        {
            if (mode == MatchMode.Puzzle) yield break;
            var cpuPick = ai.ChooseElement(Cpu);
            Element mine = Element.None;
            yield return ChooseElement(Human, e => mine = e);
            if (mine != Element.None) TakeElement(Human, mine);
            if (cpuPick != Element.None) TakeElement(Cpu, cpuPick);
            RefreshAll();
            if (mine != Element.None || cpuPick != Element.None) { Sfx.Play("draw", 0.5f); yield return Wait(0.35f); }
        }

        /// <summary>한 쪽이 원소를 고른다 (교대 룰 = 자기 T 시작에). 고를 게 하나면 바로, 사람이 아니면 AI</summary>
        public IEnumerator ChooseElement(PlayerState p, Action<Element> result)
        {
            var opts = ElementOptions(p);
            if (opts.Count == 0) { result(Element.None); yield break; }
            if (!HumanDecides(p)) { result(ai.ChooseElement(p)); yield break; }
            if (opts.Count == 1) { result(opts[0]); yield break; }
            var aiPick = ai.ChooseElement(p);
            var map = new Dictionary<CardInstance, Element>();
            var cis = new List<CardInstance>();
            foreach (var e in opts)
            {
                var d = ElementCardData(e).Clone();
                d.name = $"{CardData.ElementName(e)} 원소  (덱 {p.elemSupply[(int)e]} · 보유 {p.elemPool[(int)e]})";
                var ci = new CardInstance(d, p) { zone = Zone.Extra };
                map[ci] = e; cis.Add(ci);
            }
            CardInstance chosen = null;
            yield return ChooseFromList(p, cis, $"T{time} 엑스트라 덱 — 영웅 아래에 놓을 원소를 고르세요", c => map[c] == aiPick ? 1f : 0f, c => chosen = c);
            result(chosen != null ? map[chosen] : aiPick);
        }

        // ── 비용 ──

        /// <summary>낼 수 있으면 속성별 지불 계획 (원소 index → 개수), 못 내면 null</summary>
        int[] PlanPay(PlayerState p, CardData d)
        {
            var plan = new int[9];
            int cost = d.Cost;
            if (cost <= 0 || p.freeCost) return plan;
            if (IsBasic(d.element))
            {
                if (p.Available(d.element) < cost) return null;
                plan[(int)d.element] = cost;
                return plan;
            }
            // 무지개: 서로 다른 원소 1개씩 · 무속성: 아무 원소 (많이 남은 속성부터)
            var avail = AllElements.Select(e => (e, n: p.Available(e))).Where(x => x.n > 0).OrderByDescending(x => x.n).ToList();
            if (d.element == Element.Rainbow)
            {
                if (avail.Count < cost) return null;
                for (int i = 0; i < cost; i++) plan[(int)avail[i].e] = 1;
                return plan;
            }
            if (avail.Sum(x => x.n) < cost) return null;
            var left = avail.ToDictionary(x => x.e, x => x.n);
            for (int i = 0; i < cost; i++)
            {
                var best = left.OrderByDescending(x => x.Value).First().Key;
                plan[(int)best]++;
                left[best]--;
            }
            return plan;
        }

        public bool CanPay(CardInstance c) => c != null && c.owner != null && PlanPay(c.owner, c.data) != null;

        /// <summary>왜 못 내는지 (원소 부족) — 낼 수 있으면 null</summary>
        public string PayProblem(CardInstance c)
        {
            if (CanPay(c)) return null;
            var d = c.data;
            string have = IsBasic(d.element) ? $"{CardData.ElementName(d.element)} 원소 {c.owner.Available(d.element)}개"
                : d.element == Element.Rainbow ? $"서로 다른 원소 {AllElements.Count(e => c.owner.Available(e) > 0)}종" : $"원소 {c.owner.TotalAvailable}개";
            return $"원소가 부족합니다 — 비용 {d.CostText()} (이번 T 남은 {have})";
        }

        /// <summary>비용 지불 (이번 T 쓴 원소로 표시). 낼 수 없으면 false</summary>
        bool Pay(CardInstance c)
        {
            var plan = PlanPay(c.owner, c.data);
            if (plan == null) return false;
            for (int i = 0; i < plan.Length; i++) c.owner.elemSpent[i] += plan[i];
            return true;
        }

        // ── 보드 표시 ──

        /// <summary>보드에 놓인 원소 카드 (속성 순서, 같은 속성끼리 붙어서)</summary>
        public List<CardInstance> ElementCards(PlayerState p) => p != null ? elemCards[p.index] : null;

        /// <summary>이번 T 이미 쓴 원소 카드인가 (어둡게 표시)</summary>
        public bool ElementSpent(CardInstance c)
        {
            if (c == null || c.owner == null) return false;
            var list = elemCards[c.owner.index];
            int i = list.IndexOf(c);
            if (i < 0) return false;
            var e = c.data.element;
            int before = 0; for (int k = 0; k < i; k++) if (list[k].data.element == e) before++;
            return before >= c.owner.Available(e);
        }

        void SyncElementCards(PlayerState p)
        {
            var list = elemCards[p.index];
            var old = new List<CardInstance>(list);
            list.Clear();
            foreach (var e in AllElements)
                for (int k = 0; k < p.elemPool[(int)e]; k++)
                {
                    var ci = old.FirstOrDefault(c => c.data.element == e);
                    if (ci != null) old.Remove(ci);
                    else
                    {
                        ci = new CardInstance(ElementCardData(e), p) { zone = Zone.Center };
                        board.CreateView(ci, board.HeroPos(p) + Vector3.up * 0.6f);   // 영웅 쪽에서 날아와 쌓인다
                    }
                    list.Add(ci);
                }
            foreach (var c in old) board.DestroyView(c);
        }

        /// <summary>HUD 한 줄: 영웅 아래 원소 (속성별 이번 T 남은 수/보유). 패널이 좁아 짧게 쓴다</summary>
        public string PoolText(PlayerState p)
        {
            if (p == null || p.freeCost) return "";
            var parts = new List<string>();
            foreach (var e in AllElements)
                if (p.elemPool[(int)e] > 0)
                    parts.Add($"<color=#{CardData.ElementHex(e)}>{CardData.ElementName(e)}{p.Available(e)}/{p.elemPool[(int)e]}</color>");
            return "원소 " + (parts.Count > 0 ? string.Join(" ", parts) : "없음");
        }
    }
}
