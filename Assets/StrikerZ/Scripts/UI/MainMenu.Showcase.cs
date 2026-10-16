using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 프로필 대표 카드 6칸. 평소 클릭 = 확대.
    /// [편집] → 칸 클릭 → 카드 고르기 창에서 보유 카드(영웅 포함) 선택. 이미 다른 칸에 있는 카드를 고르면 두 칸을 맞바꾼다.
    /// 한 번도 고르지 않았으면(또는 [자동 선택]) 사용 영웅 + 가공·등급 높은 카드로 자동 구성.
    /// </summary>
    public partial class MainMenu
    {
        const int ShowcaseSlots = 6;

        class ShowSlot
        {
            public RectTransform holder;
            public CardFace face;
            public Text cap;
            public GameObject ring, empty;
            public CardData card;
            public CardFinish finish;
        }

        readonly List<ShowSlot> showcase = new List<ShowSlot>();
        bool showcaseEdit;
        Text showcaseLabel;
        UIKit.Btn showcaseEditBtn, showcaseAutoBtn;

        // 카드 고르기 창
        RectTransform pickRoot, pickGrid, pickFilters;
        Text pickTitle;
        InputField pickSearch;
        int pickSlot;
        string pickFilter = "전체";
        readonly Dictionary<string, RectTransform> pickCells = new Dictionary<string, RectTransform>();
        Coroutine pickLayout;

        static bool ShowcaseOwned(CardData c) => c != null && (c.type == CardType.Hero ? StoryData.HeroAvailable(c) : Profile.Owned(c.id) > 0);

        void BuildShowcase(Transform right)
        {
            showcaseLabel = UIKit.Txt(right, "ShowcaseLabel", "대표 카드", 26, UIKit.Gold, TextAnchor.LowerLeft, FontStyle.Bold);
            showcaseLabel.rectTransform.Anchor(0, 0.46f, 1, 0.52f, 40, 0, 300, 0);
            showcaseLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            showcaseLabel.Section(UIKit.SecMe, 22);
            showcaseEditBtn = UIKit.Button(right, "편집", () => { showcaseEdit = !showcaseEdit; RefreshShowcase(); }, Dark, 20);
            showcaseEditBtn.rt.Place(new Vector2(1, 0.49f), new Vector2(-30, 0), new Vector2(110, 46), new Vector2(1, 0.5f));
            showcaseAutoBtn = UIKit.Button(right, "자동 선택", () =>
            {
                Profile.Data.showcase.Clear();
                Profile.Save();
                Toast("대표 카드를 자동 선택으로 되돌렸습니다");
                RefreshShowcase();
            }, Dark, 18, UIKit.Muted);
            showcaseAutoBtn.rt.Place(new Vector2(1, 0.49f), new Vector2(-150, 0), new Vector2(130, 46), new Vector2(1, 0.5f));

            var row = UIKit.Rect("Showcase", right).Anchor(0, 0.17f, 1, 0.45f, 40, 0, 30, 0);
            for (int i = 0; i < ShowcaseSlots; i++)
            {
                int idx = i;
                var s = new ShowSlot();
                s.holder = UIKit.Rect("Show" + i, row);
                s.holder.Place(new Vector2(0, 1), new Vector2(i * 124, 0), new Vector2(110, 154), new Vector2(0, 1));
                s.holder.HitArea();
                var empty = UIKit.Img(s.holder, "Empty", new Color(1, 1, 1, 0.06f), GameAssets.Rounded, true);
                empty.rectTransform.Fill();
                var plus = UIKit.Txt(empty.transform, "Plus", "+", 54, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
                plus.rectTransform.Fill();
                s.empty = empty.gameObject;
                s.face = CardFace.Build(s.holder, 110, 154, 0.15f);
                s.face.root.Fill();
                s.face.glow.gameObject.SetActive(false);
                var ring = UIKit.Img(s.holder, "Ring", UIKit.Gold, GameAssets.RoundedOutline, true);
                ring.rectTransform.Anchor(-0.04f, -0.03f, 1.04f, 1.03f);
                s.ring = ring.gameObject;
                s.cap = UIKit.Txt(row, "ShowCap" + i, "", 16, UIKit.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
                s.cap.rectTransform.Place(new Vector2(0, 1), new Vector2(i * 124 - 7, -158), new Vector2(124, 24), new Vector2(0, 1));
                s.cap.horizontalOverflow = HorizontalWrapMode.Overflow;
                var ev = s.holder.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () =>
                {
                    if (showcaseEdit) OpenPicker(idx);
                    else if (showcase[idx].card != null) CardZoom.Show(showcase[idx].card, showcase[idx].finish);
                };
                ev.onRightClick = () => { if (showcase[idx].card != null) CardZoom.Show(showcase[idx].card, showcase[idx].finish); };
                showcase.Add(s);
            }
        }

        /// <summary>자동 구성: 사용 덱 영웅 1장 + 보유 카드 중 가공(StrikerZ > Seven) · 등급 높은 순</summary>
        static List<CardData> AutoShowcase()
        {
            var list = new List<CardData>();
            var hero = CardDB.Get(Profile.ActiveDeck?.heroId);
            if (hero != null) list.Add(hero);
            list.AddRange(CardDB.All.Where(c => Profile.Owned(c.id) > 0)
                                    .OrderByDescending(c => Profile.BestFinish(c.id)).ThenByDescending(c => c.rarity).ThenBy(c => c.id)
                                    .Take(ShowcaseSlots - list.Count));
            return list;
        }

        bool ShowcaseCustom => Profile.Data.showcase != null && Profile.Data.showcase.Count > 0;

        /// <summary>저장 형식 "카드id:가공" (예전 형식 "카드id" 도 읽는다 — 가장 좋은 가공으로)</summary>
        static string ShowcaseEntry(CardData c, CardFinish f) => c == null ? "" : $"{c.id}:{f}";

        /// <summary>그 가공 사본을 지금 갖고 있는지 (영웅은 일반만)</summary>
        static bool OwnsFinish(CardData c, CardFinish f) => c.type == CardType.Hero ? f == CardFinish.Normal && StoryData.HeroAvailable(c) : Profile.FinishCount(c.id, f) > 0;

        /// <summary>보유한 가공 목록 (좋은 것부터)</summary>
        static IEnumerable<CardFinish> OwnedFinishes(CardData c)
        {
            foreach (var f in new[] { CardFinish.StrikerZ, CardFinish.Seven, CardFinish.Normal })
                if (OwnsFinish(c, f)) yield return f;
        }

        static (CardData card, CardFinish finish) ParseShowcase(string entry)
        {
            if (string.IsNullOrEmpty(entry)) return (null, CardFinish.Normal);
            var parts = entry.Split(':');
            var c = CardDB.Get(parts[0]);
            if (!ShowcaseOwned(c)) return (null, CardFinish.Normal);
            // 고른 가공을 더는 갖고 있지 않으면(분해 등) 가장 좋은 가공으로
            if (parts.Length > 1 && System.Enum.TryParse(parts[1], out CardFinish f) && OwnsFinish(c, f)) return (c, f);
            return (c, Profile.BestFinish(c.id));
        }

        /// <summary>6칸 (빈 칸 = card null). 직접 고른 카드가 더는 없으면(분해 등) 빈 칸</summary>
        List<(CardData card, CardFinish finish)> ShowcaseCards()
        {
            if (!ShowcaseCustom)
            {
                var a = AutoShowcase().Select(c => (c, Profile.BestFinish(c.id))).ToList();
                while (a.Count < ShowcaseSlots) a.Add((null, CardFinish.Normal));
                return a;
            }
            var s = Profile.Data.showcase;
            return Enumerable.Range(0, ShowcaseSlots).Select(i => ParseShowcase(i < s.Count ? s[i] : "")).ToList();
        }

        void RefreshShowcase()
        {
            if (Profile.Data.showcase == null) Profile.Data.showcase = new List<string>();
            var cards = ShowcaseCards();
            bool custom = ShowcaseCustom;
            for (int i = 0; i < showcase.Count; i++)
            {
                var s = showcase[i];
                var c = cards[i].card;
                s.card = c;
                s.finish = cards[i].finish;
                bool on = c != null || showcaseEdit;
                s.holder.gameObject.SetActive(on);
                s.cap.gameObject.SetActive(on);
                s.ring.SetActive(showcaseEdit);
                s.empty.SetActive(c == null);
                s.face.root.gameObject.SetActive(c != null);
                if (c == null) { s.cap.text = "<color=#888>빈 칸</color>"; continue; }
                s.face.Set(c);
                s.face.SetFinish(s.finish);
                string fin = s.finish == CardFinish.Normal ? "" : " " + FinishStyle.Rich(s.finish, "");
                s.cap.text = c.type == CardType.Hero
                    ? (!custom && i == 0 ? "<color=#ffd45a>사용 영웅</color>" : "<color=#ffd45a>영웅</color>") + fin
                    : CardData.RarityRich(c.rarity, fin);
            }
            showcaseLabel.text = showcaseEdit ? "대표 카드  <size=18><color=#ccc>바꿀 칸을 클릭하세요</color></size>"
                               : custom ? "대표 카드" : "대표 카드  <size=18><color=#999>(자동 선택)</color></size>";
            showcaseEditBtn.label.text = showcaseEdit ? "완료" : "편집";
            showcaseEditBtn.SetSelected(showcaseEdit);
            showcaseAutoBtn.rt.gameObject.SetActive(showcaseEdit && custom);
        }

        /// <summary>탭을 떠날 때: 편집·고르기 창 닫기</summary>
        void CloseShowcaseEdit()
        {
            showcaseEdit = false;
            if (pickRoot != null) pickRoot.gameObject.SetActive(false);
        }

        // ══════════════════════════ 카드 고르기 창 ══════════════════════════

        void OpenPicker(int slot)
        {
            pickSlot = slot;
            if (pickRoot == null) BuildPicker();
            pickTitle.text = $"대표 카드 {slot + 1}번 칸  <size=18><color=#bbb>가공별로 따로 고를 수 있습니다 · 클릭: 선택 · 우클릭: 확대</color></size>";
            pickRoot.SetAsLastSibling();
            pickRoot.gameObject.SetActive(true);
            RefreshPicker();
        }

        void BuildPicker()
        {
            var bg = UIKit.Img(root, "ShowcasePicker", new Color(0, 0, 0, 0.85f));
            bg.raycastTarget = true;
            pickRoot = bg.rectTransform;
            pickRoot.Fill();
            OverlayCanvas(pickRoot, 41);
            var panel = UIKit.Frame(pickRoot, "Panel", new Color(0.05f, 0.03f, 0.1f, 0.98f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440, 900));
            pickTitle = UIKit.Txt(panel.transform, "Title", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            pickTitle.rectTransform.Anchor(0, 1, 1, 1, 28, -64, 420, 20);
            pickTitle.Section(UIKit.SecMe, 26);
            pickTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            pickSearch = UIKit.Input(panel.transform, "", "카드 검색 (이름·효과)", 20, 20);
            pickSearch.GetComponent<RectTransform>().Anchor(1, 1, 1, 1, -400, -68, 28, 18);
            pickSearch.onValueChanged.AddListener(_ => RefreshPicker());

            pickFilters = UIKit.Rect("Filters", panel.transform).Anchor(0, 1, 0, 1, 28, -122, -660, 78);
            var h = pickFilters.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            foreach (var f in new[] { "전체", "영웅", "몬스터", "마법", "필드", "가공" })
            {
                string ff = f;
                UIKit.Button(pickFilters, f, () => { pickFilter = ff; RefreshPicker(); }, Dark, 18);
            }

            pickGrid = UIKit.ScrollGrid(panel.transform, "Grid", new Vector2(112, 157), new Vector2(12, 14), out _);
            ((RectTransform)pickGrid.parent).Anchor(0, 0, 1, 1, 16, 92, 16, 134);

            UIKit.Button(panel.transform, "빈 칸으로", () => SetShowcaseSlot(null), Dark, 22, UIKit.Muted).rt.Anchor(0, 0, 0, 0, 28, 20, -248, -74);
            UIKit.Button(panel.transform, "닫기", () => pickRoot.gameObject.SetActive(false), new Color(0.1f, 0.1f, 0.14f, 0.95f), 22, UIKit.Muted).rt.Anchor(1, 0, 1, 0, -248, 20, 28, -74);
        }

        void RefreshPicker()
        {
            UIKit.HighlightByLabel(pickFilters, pickFilter, UIKit.Accent, Dark);
            string q = pickSearch.text.Trim();
            IEnumerable<CardData> list = CardList.Heroes.Where(StoryData.HeroAvailable).Concat(CardDB.All.Where(c => Profile.Owned(c.id) > 0));
            switch (pickFilter)
            {
                case "영웅": list = list.Where(c => c.type == CardType.Hero); break;
                case "몬스터": list = list.Where(c => c.type == CardType.Monster); break;
                case "마법": list = list.Where(c => c.type == CardType.Magic); break;
                case "필드": list = list.Where(c => c.type == CardType.Field); break;
            }
            if (q.Length > 0) list = list.Where(c => (c.name != null && c.name.Contains(q)) || (c.text != null && c.text.Contains(q)));
            // 보유한 가공마다 한 칸씩 (일반 · ◆ Seven · ★ StrikerZ)
            var entries = list.SelectMany(c => OwnedFinishes(c).Select(f => (card: c, finish: f)));
            if (pickFilter == "가공") entries = entries.Where(e => e.finish != CardFinish.Normal);
            var sorted = entries.OrderBy(e => e.card.type == CardType.Hero ? 0 : 1)
                                .ThenByDescending(e => e.finish).ThenByDescending(e => e.card.rarity)
                                .ThenBy(e => CardDB.SortKey(e.card)).ThenBy(e => e.card.id).ToList();
            if (pickLayout != null) StopCoroutine(pickLayout);
            pickLayout = StartCoroutine(LayoutPicker(sorted));
        }

        /// <summary>고르기 창 셀 배치: (카드, 가공)별 셀을 재사용, 없는 셀만 만든다 (프레임당 약 10ms)</summary>
        System.Collections.IEnumerator LayoutPicker(List<(CardData card, CardFinish finish)> entries)
        {
            var keep = new HashSet<string>(entries.Select(e => ShowcaseEntry(e.card, e.finish)));
            foreach (var kv in pickCells) if (!keep.Contains(kv.Key)) kv.Value.gameObject.SetActive(false);
            var cur = ShowcaseCards().Select(x => ShowcaseEntry(x.card, x.finish)).ToList();
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < entries.Count; i++)
            {
                var (c, f) = entries[i];
                string key = ShowcaseEntry(c, f);
                if (!pickCells.TryGetValue(key, out var cell))
                {
                    cell = CardCell(pickGrid, c, 112, 157, 0.15f, () => SetShowcaseSlot(c, f), null, () => CardZoom.Show(c, f), null, out var face);
                    face.SetFinish(f);
                    if (f != CardFinish.Normal)
                        Badge(cell, FinishStyle.Rich(f), new Color(0.05f, 0.04f, 0.08f, 0.92f), new Vector4(0.08f, 0.01f, 0.92f, 0.13f), 14).transform.parent.name = "FinishTag";
                    pickCells[key] = cell;
                }
                cell.gameObject.SetActive(true);
                if (cell.GetSiblingIndex() != i) cell.SetSiblingIndex(i);
                // 이미 대표 카드인 칸 번호 배지
                for (int k = cell.childCount - 1; k >= 0; k--) if (cell.GetChild(k).name == "Badge") Destroy(cell.GetChild(k).gameObject);
                int at = cur.IndexOf(key);
                if (at >= 0) Badge(cell, $"{at + 1}번", at == pickSlot ? UIKit.Accent : new Color(0.2f, 0.2f, 0.28f, 0.95f), new Vector4(0.25f, 0.82f, 0.75f, 0.94f), 15);
                if (Time.realtimeSinceStartup - t0 > 0.010f) { yield return null; t0 = Time.realtimeSinceStartup; }
            }
            pickLayout = null;
        }

        /// <summary>고른 칸에 카드(가공 포함) 넣기 (null = 빈 칸). 같은 카드·가공이 다른 칸에 있으면 두 칸을 맞바꾼다</summary>
        void SetShowcaseSlot(CardData c, CardFinish f = CardFinish.Normal)
        {
            var s = Profile.Data.showcase;
            if (s.Count == 0) s.AddRange(ShowcaseCards().Select(x => ShowcaseEntry(x.card, x.finish)));   // 자동 → 직접 고르기로 전환
            while (s.Count < ShowcaseSlots) s.Add("");
            string entry = ShowcaseEntry(c, f);
            if (c != null)
            {
                // 예전 형식("id")으로 저장된 칸도 해석한 결과로 비교한다
                for (int i = 0; i < s.Count; i++)
                {
                    if (i == pickSlot) continue;
                    var (pc, pf) = ParseShowcase(s[i]);
                    if (pc != null && ShowcaseEntry(pc, pf) == entry) { s[i] = s[pickSlot]; break; }
                }
            }
            s[pickSlot] = entry;
            Profile.Save();
            pickRoot.gameObject.SetActive(false);
            RefreshShowcase();
        }

        // ── 테스트 ──
        public void TestShowcaseEdit(bool on) { showcaseEdit = on; RefreshShowcase(); }
        public void TestShowcasePick(int slot, string id, CardFinish f = CardFinish.Normal) { OpenPicker(slot); SetShowcaseSlot(id == null ? null : CardDB.Get(id), f); }
        public string TestShowcaseIds => string.Join(",", ShowcaseCards().Select(x => x.card == null ? "-" : ShowcaseEntry(x.card, x.finish)));
    }
}
