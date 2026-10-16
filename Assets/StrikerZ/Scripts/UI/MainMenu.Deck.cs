using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 덱 편집 페이지 (마스터듀얼 방식): 덱 목록 · 검색/정렬/필터 · 카드 분해/생성 · 가공 잠금 · 초과분 일괄 분해 · 덱 코드.
    /// 카드 클릭 = 선택, 더블클릭 = 덱에 넣기(보유 카드) / 빼기(덱 카드). 선택한 카드는 오른쪽 패널에서 분해·생성·잠금.
    /// </summary>
    public partial class MainMenu
    {
        int editIndex;
        DeckData editing;
        string filter = "전체";
        RectTransform deckTabs, deckGrid, collectionGrid, filterRow, heroSlot, elemBar, extraRow;
        Text extraHead;
        InputField deckName, searchInput;
        Text deckCount, deckValid, deckStats, elemLegend, listCount, cpText;
        Text detailName, detailRarity, detailStats, detailText;
        CardFace detailFace;
        RectTransform detailRoot, finishRows;
        CardData pinned;          // 선택(고정)한 카드
        UIKit.Btn sortBtn, orderBtn, rarityBtn, elementBtn, finishBtn, craftBtn;

        // ── 셀 풀: 카드 셀(CardFace ≈ 40개 오브젝트)을 갱신 때마다 지우고 새로 만들지 않고 카드별로 재사용한다 ──
        class PooledCell { public RectTransform rt; public CardFace face; public CardFinish finish; public string badges; }
        readonly Dictionary<string, PooledCell> collCells = new Dictionary<string, PooledCell>();
        readonly Dictionary<string, List<PooledCell>> deckCells = new Dictionary<string, List<PooledCell>>();
        PooledCell deckHeroCell;
        string deckHeroId;
        Coroutine collLayout, collWarm;
        // 덱 페이지는 숨길 때 SetActive(false) 대신 자체 Canvas 만 끈다 (다시 켤 때 수백 장의 텍스트·메시 재생성 방지)
        Canvas deckCanvas;
        CanvasGroup deckGroup;

        static readonly string[] SortNames = { "기본", "레어도", "이름", "ATK", "보유 수", "속성", "HP", "타임" };
        int sortMode;
        bool sortDesc = true;
        int rarityFilter = -1;              // -1 = 전체
        int elementFilter = -1;
        bool finishOnly;
        string search = "";

        // ══════════════════════════ 구성 ══════════════════════════

        RectTransform BuildDeck()
        {
            var page = UIKit.Rect("Deck", contentArea).Fill();
            deckCanvas = page.gameObject.AddComponent<Canvas>();
            page.gameObject.AddComponent<GraphicRaycaster>();
            deckGroup = page.gameObject.AddComponent<CanvasGroup>();

            // 상단: 덱 목록 / 이름 / 관리 버튼
            var top = UIKit.Rect("Top", page).Anchor(0, 1, 1, 1, 0, -66, 0, 0);
            deckTabs = UIKit.Rect("Tabs", top).Anchor(0, 0, 0.44f, 1);
            var h = deckTabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            deckName = UIKit.Input(top, "", "덱 이름", 24, 16);
            deckName.GetComponent<RectTransform>().Anchor(0.45f, 0.08f, 0.62f, 0.92f);
            deckName.onEndEdit.AddListener(v => { if (editing == null) return; editing.name = string.IsNullOrWhiteSpace(v) ? "새 덱" : v.Trim(); SaveDeck(); RefreshDeckPage(); });
            var act = UIKit.Button(top, "사용 덱으로", () => { Profile.Data.activeDeck = editIndex; Profile.Save(); Toast($"[{editing.name}] 을(를) 사용 덱으로 설정했습니다"); RefreshDeckPage(); }, Red, 21, Color.white);
            act.rt.Anchor(0.63f, 0.08f, 0.74f, 0.92f);
            UIKit.Button(top, "+ 새 덱", NewDeck, Dark, 21).rt.Anchor(0.75f, 0.08f, 0.83f, 0.92f);
            UIKit.Button(top, "복제", CopyDeck, Dark, 21).rt.Anchor(0.84f, 0.08f, 0.91f, 0.92f);
            UIKit.Button(top, "삭제", DeleteDeck, Dark, 21, UIKit.Muted).rt.Anchor(0.92f, 0.08f, 1f, 0.92f);

            // ── 좌: 카드 상세 (이름 띠 · 카드 · 수치 · 효과 · 가공별 분해/잠금 · 생성 · CP) ──
            var left = UIKit.Frame(page, "Detail", Dark);
            left.rectTransform.Anchor(0, 0, 0.235f, 1, 0, 0, 6, 78);
            var nameBar = SectionHeader(left.transform, "NameBar", 10);
            detailName = UIKit.Txt(nameBar, "Name", "카드 상세", 22, HeadInk, TextAnchor.MiddleLeft, FontStyle.Bold);
            detailName.rectTransform.Anchor(0, 0, 1, 1, 14, 0, 104, 0);
            detailName.resizeTextForBestFit = true;
            detailName.resizeTextMinSize = 14;
            detailName.resizeTextMaxSize = 22;
            detailName.Shadow();
            detailRarity = UIKit.Txt(nameBar, "Rarity", "", 17, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            detailRarity.rectTransform.Anchor(1, 0, 1, 1, -100, 0, 12, 0);
            detailRarity.horizontalOverflow = HorizontalWrapMode.Overflow;
            detailRarity.Shadow();
            detailRoot = UIKit.Rect("Face", left.transform);
            detailRoot.Place(new Vector2(0, 1), new Vector2(12, -64), new Vector2(172, 241), new Vector2(0, 1));
            detailFace = CardFace.Build(detailRoot, 172, 241, 0.24f);
            detailFace.root.Fill();
            detailFace.glow.gameObject.SetActive(false);
            detailStats = UIKit.Txt(left.transform, "Stats", "", 17, UIKit.Ink, TextAnchor.UpperLeft);
            detailStats.rectTransform.Anchor(0, 1, 1, 1, 196, -305, 10, 66);
            detailStats.lineSpacing = 1.15f;
            var textBox = UIKit.Img(left.transform, "TextBox", new Color(0.02f, 0.015f, 0.05f, 0.85f), GameAssets.Rounded, true);
            textBox.rectTransform.Anchor(0, 0, 1, 1, 10, 334, 10, 316);
            detailText = UIKit.Txt(textBox.transform, "Text", "카드에 마우스를 올리면 상세 정보가 표시됩니다.\n\n클릭: 카드 선택\n더블클릭: 덱에 넣기 / 빼기\n\n선택한 카드는 아래에서 분해·생성·잠금할 수 있습니다.", 17, UIKit.Ink, TextAnchor.UpperLeft);
            detailText.rectTransform.Anchor(0, 0, 1, 1, 12, 10, 12, 10);
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 12;
            detailText.resizeTextMaxSize = 17;
            finishRows = UIKit.Rect("Rows", left.transform).Anchor(0, 0, 1, 0, 10, 96, 10, -326);
            var cpBox = UIKit.Img(left.transform, "CpBox", new Color(0.02f, 0.015f, 0.05f, 0.85f), GameAssets.Rounded, true);
            cpBox.rectTransform.Anchor(0, 0, 1, 0, 10, 10, 10, -88);
            cpText = UIKit.Txt(cpBox.transform, "CP", "", 14, UIKit.Ink, TextAnchor.MiddleLeft);
            cpText.rectTransform.Anchor(0, 0, 0.64f, 1, 10, 4, 0, 4);
            cpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Button(cpBox.transform, "초과분\n일괄 분해", DismantleSurplusDialog, new Color(0.3f, 0.08f, 0.06f, 0.95f), 16, Color.white).rt.Anchor(0.66f, 0.12f, 0.98f, 0.88f);
            detailRoot.gameObject.SetActive(false);

            // ── 중: 현재 덱 (메인 덱 그리드 10열 · 영웅 · 덱 분석) ──
            var mid = UIKit.Frame(page, "DeckPanel", Dark);
            mid.rectTransform.Anchor(0.235f, 0, 0.765f, 1, 6, 0, 6, 78);
            var mainHead = SectionHeader(mid.transform, "MainHead", 8);
            deckCount = UIKit.Txt(mainHead, "Count", "", 22, HeadInk, TextAnchor.MiddleLeft, FontStyle.Bold);
            deckCount.rectTransform.Anchor(0, 0, 0.6f, 1, 14, 0, 0, 0);
            deckCount.horizontalOverflow = HorizontalWrapMode.Overflow;
            deckValid = UIKit.Txt(mainHead, "Valid", "", 17, UIKit.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            deckValid.rectTransform.Anchor(0.35f, 0, 1, 1, 0, 0, 14, 0);
            // 빈 칸 자리표시 (최대 장수만큼, 덱 그리드와 같은 배치) — 몇 장 더 넣을 수 있는지 한눈에
            var slots = UIKit.Rect("Slots", mid.transform).Anchor(0, 1, 1, 1, 6, -520, 6, 52);
            var sg = slots.gameObject.AddComponent<GridLayoutGroup>();
            sg.cellSize = new Vector2(76, 106);
            sg.spacing = new Vector2(5, 5);
            sg.padding = new RectOffset(8, 8, 8, 8);
            sg.childAlignment = TextAnchor.UpperCenter;
            for (int i = 0; i < DeckRules.MaxSize; i++)
                UIKit.Img(slots, "Slot", new Color(1, 1, 1, i < DeckRules.MinSize ? 0.06f : 0.03f), GameAssets.Rounded, true);
            deckGrid = UIKit.ScrollGrid(mid.transform, "DeckGrid", new Vector2(76, 106), new Vector2(5, 5), out _);
            ((RectTransform)deckGrid.parent).Anchor(0, 1, 1, 1, 6, -520, 6, 52);
            AddExit((RectTransform)deckGrid.parent);

            var heroHead = SectionHeader(mid.transform, "HeroHead", 528);
            UIKit.Txt(heroHead, "T", "영웅 · 덱 분석", 20, HeadInk, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Anchor(0, 0, 1, 1, 14, 0, 0, 0);
            heroSlot = UIKit.Rect("HeroSlot", mid.transform);
            heroSlot.Place(new Vector2(0, 1), new Vector2(18, -580), new Vector2(124, 174), new Vector2(0, 1));
            AddExit(heroSlot);
            deckStats = UIKit.Txt(mid.transform, "Stats", "", 16, UIKit.Ink, TextAnchor.UpperLeft);
            deckStats.rectTransform.Anchor(0, 1, 1, 1, 160, -628, 16, 572);
            deckStats.lineSpacing = 1.1f;
            // 엑스트라 덱 (필드 최대 10장) — 더블클릭으로 빼기
            extraHead = UIKit.Txt(mid.transform, "ExtraHead", "", 16, new Color(0.62f, 0.92f, 0.7f), TextAnchor.MiddleLeft, FontStyle.Bold);
            extraHead.rectTransform.Anchor(0, 1, 1, 1, 160, -652, 16, 630);
            var exBg = UIKit.Rect("ExtraSlots", mid.transform).Anchor(0, 1, 1, 1, 160, -730, 16, 654);
            var exg = exBg.gameObject.AddComponent<HorizontalLayoutGroup>();
            exg.spacing = 4; exg.childControlWidth = exg.childControlHeight = false; exg.childForceExpandWidth = false; exg.childAlignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < DeckRules.MaxExtra; i++)
            {
                var ph = UIKit.Img(exBg, "Slot", new Color(1f, 1f, 1f, 0.06f), GameAssets.Rounded, true);
                ph.rectTransform.sizeDelta = new Vector2(52, 73);
            }
            extraRow = UIKit.Rect("ExtraRow", mid.transform).Anchor(0, 1, 1, 1, 160, -730, 16, 654);
            var exr = extraRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            exr.spacing = 4; exr.childControlWidth = exr.childControlHeight = false; exr.childForceExpandWidth = false; exr.childAlignment = TextAnchor.MiddleLeft;
            elemBar = UIKit.Rect("ElemBar", mid.transform).Anchor(0, 1, 1, 1, 160, -748, 16, 736);
            var eb = elemBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            eb.spacing = 2;
            eb.childControlWidth = eb.childControlHeight = true;
            eb.childForceExpandWidth = false;
            elemLegend = UIKit.Txt(mid.transform, "ElemLegend", "", 15, UIKit.Muted, TextAnchor.UpperLeft);
            elemLegend.rectTransform.Anchor(0, 1, 1, 1, 160, -790, 16, 752);
            elemLegend.lineSpacing = 1.25f;

            var tools = UIKit.Rect("Tools", mid.transform).Anchor(0, 0, 1, 0, 12, 12, 12, -60);
            var th = tools.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 8;
            th.childControlWidth = th.childControlHeight = true;
            UIKit.Button(tools, "자동 구성", () =>
            {
                var built = DeckRules.AutoBuild(editing.name, editing.heroId);
                editing.cards = built.cards;
                SaveDeck();
                RefreshDeckPage();
            }, Dark, 19);
            UIKit.Button(tools, "비우기", () => { editing.cards.Clear(); SaveDeck(); RefreshDeckPage(); }, Dark, 19, UIKit.Muted);
            UIKit.Button(tools, "덱 코드 복사", () =>
            {
                GUIUtility.systemCopyBuffer = Crafting.ExportDeck(editing);
                Toast("덱 코드를 클립보드에 복사했습니다");
            }, Dark, 19);
            UIKit.Button(tools, "덱 코드 입력", ImportDeck, Dark, 19);
            UIKit.Button(tools, "스타터 덱", OpenStarters, new Color(0.32f, 0.2f, 0.05f, 0.96f), 19, Color.white);
            // 훈련장: 지금 덱으로 수련 골렘과 연습 (기록·보상 없음)
            UIKit.Button(tools, "훈련장", () =>
            {
                string err = DeckRules.Validate(editing);
                if (err != null) { Toast(err); return; }
                SaveDeck();
                onPractice?.Invoke(editing);
            }, new Color(0.18f, 0.32f, 0.12f, 0.96f), 19, Color.white);

            // ── 우: 카드 리스트 (검색 · 종류 · 정렬/필터 · 3열 그리드) ──
            var right = UIKit.Frame(page, "Collection", Dark);
            right.rectTransform.Anchor(0.765f, 0, 1, 1, 6, 0, 0, 78);
            var listHead = SectionHeader(right.transform, "ListHead", 8);
            UIKit.Txt(listHead, "T", "카드 리스트", 20, HeadInk, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Anchor(0, 0, 0.6f, 1, 14, 0, 0, 0);
            listCount = UIKit.Txt(listHead, "N", "", 16, UIKit.Muted, TextAnchor.MiddleRight);
            listCount.rectTransform.Anchor(0.4f, 0, 1, 1, 0, 0, 14, 0);
            searchInput = UIKit.Input(right.transform, "", "텍스트 검색 (이름·효과)", 17, 20);
            searchInput.GetComponent<RectTransform>().Anchor(0, 1, 1, 1, 10, -94, 10, 54);
            searchInput.onValueChanged.AddListener(v => { search = v.Trim(); RefreshCollection(); });
            filterRow = UIKit.Rect("Filters", right.transform).Anchor(0, 1, 1, 1, 10, -136, 10, 100);
            var fr = filterRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            fr.spacing = 3;
            fr.childControlWidth = fr.childControlHeight = true;

            var row2 = UIKit.Rect("Row2", right.transform).Anchor(0, 1, 1, 1, 10, -178, 10, 142);
            var r2 = row2.gameObject.AddComponent<HorizontalLayoutGroup>();
            r2.spacing = 4;
            r2.childControlWidth = r2.childControlHeight = true;
            sortBtn = UIKit.Button(row2, "", () => { sortMode = (sortMode + 1) % SortNames.Length; RefreshCollection(); }, Dark, 15);
            orderBtn = UIKit.Button(row2, "", () => { sortDesc = !sortDesc; RefreshCollection(); }, Dark, 15);
            rarityBtn = UIKit.Button(row2, "", () => { rarityFilter = rarityFilter >= (int)Rarity.Unique ? -1 : rarityFilter + 1; RefreshCollection(); }, Dark, 15);
            elementBtn = UIKit.Button(row2, "", () => { elementFilter = elementFilter >= (int)Element.Rainbow ? -1 : elementFilter + 1; RefreshCollection(); }, Dark, 15);
            finishBtn = UIKit.Button(row2, "가공", () => { finishOnly = !finishOnly; RefreshCollection(); }, Dark, 15);
            foreach (var (b, w) in new[] { (sortBtn, 1.5f), (orderBtn, 0.55f), (rarityBtn, 1.3f), (elementBtn, 1.3f), (finishBtn, 0.8f) })
            {
                b.bg.gameObject.AddComponent<LayoutElement>().flexibleWidth = w;
                Compact(b);
            }

            collectionGrid = UIKit.ScrollGrid(right.transform, "CollGrid", new Vector2(112, 157), new Vector2(6, 8), out _);
            ((RectTransform)collectionGrid.parent).Anchor(0, 0, 1, 1, 4, 6, 4, 186);
            AddExit((RectTransform)collectionGrid.parent);
            return page;
        }

        /// <summary>패널 상단 제목 띠 (top = 패널 위에서부터의 거리)</summary>
        /// <summary>덱 화면 머리말 글자색 (컬렉션 구역 파랑을 밝힌 색)</summary>
        static readonly Color HeadInk = Color.Lerp(UIKit.SecCollect, Color.white, 0.35f);

        static RectTransform SectionHeader(Transform parent, string name, float top)
        {
            // 왼쪽 메뉴 머리말과 같은 모양: 색 막대 + (호출 측의) 제목 + 아래 구분선, 바탕 없음
            var bar = UIKit.Img(parent, name, new Color(0, 0, 0, 0));
            bar.rectTransform.Anchor(0, 1, 1, 1, 8, -(top + 40), 8, top);
            var accent = UIKit.Img(bar.transform, "Accent", UIKit.SecCollect);
            accent.rectTransform.Anchor(0, 0.22f, 0, 0.78f, 4, 0, -9, 0);
            var line = UIKit.Img(bar.transform, "Line", new Color(UIKit.SecCollect.r, UIKit.SecCollect.g, UIKit.SecCollect.b, 0.28f));
            line.rectTransform.Anchor(0, 0, 1, 0, 2, 0, 2, -1);
            return bar.rectTransform;
        }

        /// <summary>좁은 버튼: 글자가 줄바꿈되지 않게 여백을 줄인다</summary>
        static void Compact(UIKit.Btn b)
        {
            b.label.rectTransform.Anchor(0, 0, 1, 1, 2, 2, 2, 2);
            b.label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        /// <summary>그리드 밖으로 나가면 상세를 선택한 카드로 되돌린다</summary>
        void AddExit(RectTransform view)
        {
            var ev = view.gameObject.AddComponent<CellEvents>();
            ev.onExit = () => { if (pinned != null) ShowDetail(pinned); };
        }

        // ══════════════════════════ 덱 관리 ══════════════════════════

        void OpenDeckPage()
        {
            editIndex = Mathf.Clamp(Profile.Data.activeDeck, 0, Profile.Data.decks.Count - 1);
            editing = Profile.Data.decks[editIndex];
            RefreshDeckPage();
        }

        void SaveDeck()
        {
            DeckRules.SortCards(editing);
            Profile.Save();
        }

        const int MaxDecks = 8;

        /// <summary>덱 개수 상한이면 안내하고 true</summary>
        bool DeckLimitReached()
        {
            if (Profile.Data.decks.Count < MaxDecks) return false;
            Toast($"덱은 최대 {MaxDecks}개까지 만들 수 있습니다");
            return true;
        }

        void NewDeck()
        {
            if (DeckLimitReached()) return;
            Profile.Data.decks.Add(new DeckData { name = $"새 덱 {Profile.Data.decks.Count + 1}", heroId = editing?.heroId ?? "H001" });
            SelectLastDeck();
        }

        void CopyDeck()
        {
            if (DeckLimitReached()) return;
            var c = editing.Clone();
            c.name = (editing.name + " 복사").Substring(0, Math.Min(16, editing.name.Length + 3));
            Profile.Data.decks.Add(c);
            SelectLastDeck();
            Toast("덱을 복제했습니다");
        }

        void SelectLastDeck()
        {
            editIndex = Profile.Data.decks.Count - 1;
            editing = Profile.Data.decks[editIndex];
            Profile.Save();
            RefreshDeckPage();
        }

        bool deleteArmed;

        void DeleteDeck()
        {
            if (Profile.Data.decks.Count <= 1) { Toast("마지막 덱은 삭제할 수 없습니다"); return; }
            if (!deleteArmed) { deleteArmed = true; Toast("한 번 더 누르면 덱이 삭제됩니다"); return; }
            deleteArmed = false;
            Profile.Data.decks.RemoveAt(editIndex);
            if (Profile.Data.activeDeck >= Profile.Data.decks.Count) Profile.Data.activeDeck = 0;
            editIndex = Mathf.Clamp(editIndex - 1, 0, Profile.Data.decks.Count - 1);
            editing = Profile.Data.decks[editIndex];
            Profile.Save();
            RefreshDeckPage();
        }

        void ImportDeck()
        {
            Modal("덱 코드 입력", "다른 곳에서 복사한 덱 코드(SZ1-…)를 붙여 넣으세요.", true, (code, _) =>
            {
                var d = Crafting.ParseDeck(code, "가져온 덱");
                if (d == null) { Toast("올바른 덱 코드가 아닙니다"); return; }
                if (DeckLimitReached()) return;
                var missing = Crafting.Missing(d);
                if (missing.Count == 0) { AddImported(d); return; }
                int n = missing.Sum(m => m.n);
                bool can = Crafting.CanCraftAll(missing, out string need);
                Modal("부족한 카드", $"덱을 완성하려면 {n}장이 더 필요합니다.\n필요 CP: {need}\n{(can ? "CP 로 모두 생성할 수 있습니다." : "<color=#f98>CP 가 부족합니다.</color>")}", false,
                    (_, __) => { if (can) { Crafting.CraftAll(missing); AddImported(d); Toast($"{n}장을 생성하고 덱을 추가했습니다"); } },
                    can ? "생성 후 추가" : null,
                    () =>
                    {
                        foreach (var (c, k) in missing) for (int i = 0; i < k; i++) { if (!d.cards.Remove(c.id)) d.extra?.Remove(c.id); }
                        AddImported(d);
                        Toast("보유한 카드만으로 덱을 추가했습니다");
                    }, "보유분만 추가");
            }, "가져오기");
        }

        void AddImported(DeckData d)
        {
            Profile.Data.decks.Add(d);
            editIndex = Profile.Data.decks.Count - 1;
            editing = d;
            SaveDeck();
            RefreshDeckPage();
        }

        // ══════════════════════════ 분해 · 생성 ══════════════════════════

        void DismantleSurplusDialog()
        {
            var s = Crafting.Surplus();
            if (s.Count == 0) { Toast("분해할 초과분이 없습니다 (잠긴 카드·덱 사용분 제외)"); return; }
            int cards = s.Sum(x => x.n);
            var byR = s.GroupBy(x => x.card.rarity).Select(g => $"{CardData.RarityLabel(g.Key)} +{g.Sum(x => Crafting.DismantleValue(x.card, x.finish) * x.n)}");
            Modal("초과분 일괄 분해", $"덱 최대 장수를 넘는 카드 {cards}장을 분해합니다.\n획득 CP: {string.Join(" · ", byR)}\n잠긴 카드와 덱에서 쓰는 카드는 제외됩니다.", false, (_, __) =>
            {
                int got = Crafting.DismantleSurplus();
                Sfx.Play("destroy", 0.8f);
                Toast($"{cards}장 분해 · CP +{got}");
                RefreshDeckPage();
            }, "분해");
        }

        void Dismantle(CardData c, CardFinish f)
        {
            if (Crafting.Dismantle(c, f, 1, out int got, out string why))
            {
                Sfx.Play("destroy", 0.7f);
                Toast($"{c.name} 분해 · {CardData.RarityLabel(c.rarity)} CP +{got}");
                RefreshDeckPage();
                ShowDetail(c, true);
            }
            else Toast(why);
        }

        void Craft(CardData c)
        {
            Modal("카드 생성", $"[{c.name}] 1장을 생성합니다.\n{CardData.RarityLabel(c.rarity)} CP {Crafting.CraftCost} 사용 (보유 {Crafting.CP(c.rarity)})", false, (_, __) =>
            {
                if (Crafting.Craft(c, out string why))
                {
                    Sfx.Play("reveal2", 0.8f);
                    Toast($"{c.name} 생성!");
                    RefreshDeckPage();
                    ShowDetail(c, true);
                }
                else Toast(why);
            }, "생성");
        }

        // ══════════════════════════ 갱신 ══════════════════════════

        void RefreshDeckPage()
        {
            deleteArmed = false;
            var d = Profile.Data;

            UIKit.Clear(deckTabs);
            for (int i = 0; i < d.decks.Count; i++)
            {
                int idx = i;
                bool sel = i == editIndex;
                string label = (i == d.activeDeck ? "★ " : "") + d.decks[i].name;
                var b = UIKit.Button(deckTabs, label, () => { editIndex = idx; editing = d.decks[idx]; RefreshDeckPage(); }, Dark, 20);
                b.SetSelected(sel);
                b.Icon(GameAssets.TexSprite("nav_deck"), 34);
                b.bg.gameObject.AddComponent<LayoutElement>().preferredWidth = Mathf.Clamp(label.Length * 20 + 80, 150, 260);
            }
            deckName.SetTextWithoutNotify(editing.name);

            RefreshDeckGrid();
            RefreshExtraRow();
            string err = DeckRules.Validate(editing);
            int n = editing.cards.Count;
            bool sizeOk = n >= DeckRules.MinSize && n <= DeckRules.MaxSize;
            deckCount.text = $"메인 덱   <color=#{(sizeOk ? "9f9" : "fb8")}>{n}</color><size=16><color=#a9a3b8>  / {(DeckRules.MinSize == DeckRules.MaxSize ? $"{DeckRules.MinSize}" : $"{DeckRules.MinSize}~{DeckRules.MaxSize}")}장</color></size>";
            deckValid.text = err == null ? "<color=#8f8>● 사용 가능</color>" : $"<color=#f98>● {err}</color>";
            RefreshDeckAnalysis();

            UIKit.Clear(filterRow);
            foreach (var f in new[] { "전체", "영웅", "몬스터", "마법", "필드", "유니크", "미보유" })
            {
                string ff = f;
                bool sel = filter == f;
                var fb = UIKit.Button(filterRow, f, () => { filter = ff; RefreshCollection(); }, sel ? UIKit.Accent : Dark, 14, sel ? Color.white : UIKit.Gold);
                fb.bg.gameObject.AddComponent<LayoutElement>().flexibleWidth = f.Length;
                Compact(fb);
            }
            RefreshCollection(false);
            if (pinned != null) ShowDetail(pinned, true);
        }

        /// <summary>덱 분석: 종류별 장수 · 평균 ATK · 유니크/필드 제한 · 가공 · 몬스터 속성 분포 막대</summary>
        void RefreshDeckAnalysis()
        {
            var cards = editing.cards.Select(CardDB.Get).Where(c => c != null).ToList();
            var mons = cards.Where(c => c.type == CardType.Monster).ToList();
            string avg = mons.Count > 0 ? $"{mons.Average(c => c.atk):0.0}" : "-";
            int mm = DeckRules.MythicCount(editing, CardType.Monster), ms = DeckRules.MythicCount(editing, CardType.Magic);
            int fin = Profile.AssignFinishes(cards).Count(f => f != CardFinish.Normal);
            string Lim(int v, int max) => v > max ? $"<color=#f98>{v}/{max}</color>" : $"{v}/{max}";
            deckStats.text =
                $"<color=#ffb27a>몬스터 {mons.Count}</color>   <color=#7fd8ff>마법 {cards.Count(c => c.type == CardType.Magic)}</color>     평균 ATK <b>{avg}</b>" + (fin > 0 ? $"     <color=#ffd45a>가공 {fin}장</color>" : "") + "\n" +
                $"<color=#a9a3b8>유니크 몬스터</color> {Lim(mm, 1)}   <color=#a9a3b8>유니크 마법</color> {Lim(ms, 1)}";

            UIKit.Clear(elemBar);
            var groups = mons.GroupBy(c => c.element).OrderByDescending(g => g.Count()).ToList();
            foreach (var g in groups)
            {
                var seg = UIKit.Img(elemBar, "E_" + g.Key, CardData.ElementColor(g.Key), GameAssets.Rounded, true);
                seg.gameObject.AddComponent<LayoutElement>().flexibleWidth = g.Count();
            }
            if (groups.Count == 0) UIKit.Img(elemBar, "Empty", new Color(1, 1, 1, 0.08f), GameAssets.Rounded, true).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            elemLegend.text = groups.Count == 0 ? "몬스터 속성 분포" :
                string.Join("   ", groups.Select(g => $"<color=#{CardData.ElementHex(g.Key)}>■</color> {CardData.ElementName(g.Key)} {g.Count()}"));
        }

        void RefreshCollection(bool rebuildFilters = true)
        {
            if (rebuildFilters) UIKit.HighlightByLabel(filterRow, filter, UIKit.Accent, Dark);   // 상단 종류 필터 버튼 색 갱신
            sortBtn.label.text = $"정렬 {SortNames[sortMode]}";
            orderBtn.label.text = sortDesc ? "▼" : "▲";
            rarityBtn.label.text = rarityFilter < 0 ? "모든 등급" : CardData.RarityLabel((Rarity)rarityFilter);
            elementBtn.label.text = elementFilter < 0 ? "모든 속성" : CardData.ElementName((Element)elementFilter);
            finishBtn.SetSelected(finishOnly);
            cpText.text = "<color=#a9a3b8>보유 CP</color>\n" + string.Join("\n", new[] { (Rarity.Common, Rarity.Uncommon), (Rarity.Rare, Rarity.Unique) }
                .Select(p => $"{CpLabel(p.Item1)} · {CpLabel(p.Item2)}"));

            var entries = new List<CardData>();
            // 영웅 카드: 모두 기본 보유, [전체]에서는 맨 앞
            if ((filter == "전체" || filter == "영웅") && !finishOnly && rarityFilter < 0)
                entries.AddRange(CardList.Heroes.Where(Matches).Where(StoryData.HeroAvailable));
            IEnumerable<CardData> list = CardDB.All;
            switch (filter)
            {
                case "영웅": list = Enumerable.Empty<CardData>(); break;
                case "몬스터": list = list.Where(c => c.type == CardType.Monster && Profile.Owned(c.id) > 0); break;
                case "마법": list = list.Where(c => c.type == CardType.Magic && Profile.Owned(c.id) > 0); break;
                case "필드": list = list.Where(c => c.type == CardType.Field && Profile.Owned(c.id) > 0); break;
                case "유니크": list = list.Where(c => c.rarity == Rarity.Unique); break;
                case "미보유": list = list.Where(c => Profile.Owned(c.id) == 0); break;
                default: list = list.Where(c => Profile.Owned(c.id) > 0); break;
            }
            list = list.Where(Matches);
            if (rarityFilter >= 0) list = list.Where(c => (int)c.rarity == rarityFilter);
            if (finishOnly) list = list.Where(c => Profile.BestFinish(c.id) != CardFinish.Normal);
            entries.AddRange(Sorted(list));
            listCount.text = $"{entries.Count}종";

            if (collLayout != null) StopCoroutine(collLayout);
            collLayout = null;
            var job = LayoutCollection(entries);
            if (isActiveAndEnabled) collLayout = StartCoroutine(job);
            else while (job.MoveNext()) { }
        }

        static string CpLabel(Rarity r) => Crafting.CpLabel(r);

        /// <summary>보유 카드 셀 배치: 있는 셀은 순서·배지만 고치고, 없는 셀만 만든다 (프레임당 약 10ms 까지 — 화면이 멈추지 않게)</summary>
        System.Collections.IEnumerator LayoutCollection(List<CardData> entries)
        {
            var keep = new HashSet<string>(entries.Select(c => c.id));
            foreach (var kv in collCells)
                if (!keep.Contains(kv.Key) && kv.Value.rt.gameObject.activeSelf) kv.Value.rt.gameObject.SetActive(false);
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < entries.Count; i++)
            {
                var c = entries[i];
                var cell = CollCell(c);
                if (!cell.rt.gameObject.activeSelf) cell.rt.gameObject.SetActive(true);
                if (cell.rt.GetSiblingIndex() != i) cell.rt.SetSiblingIndex(i);
                SyncFinish(cell, c.id);
                CollBadges(cell, c);
                if (Time.realtimeSinceStartup - t0 > 0.010f) { yield return null; t0 = Time.realtimeSinceStartup; }
            }
            collLayout = null;
        }

        /// <summary>카드별 보유 카드 셀 (없으면 만든다). 더블클릭 동작은 누르는 순간의 덱·보유 수로 판단</summary>
        PooledCell CollCell(CardData c)
        {
            if (collCells.TryGetValue(c.id, out var pc)) return pc;
            var cd = c;
            Action dbl;
            if (c.type == CardType.Hero)
                dbl = () =>
                {
                    Pin(cd);
                    if (editing.heroId == cd.id) { Toast($"이미 [{cd.name}] 영웅을 사용 중입니다"); return; }
                    editing.heroId = cd.id;
                    SaveDeck();
                    Toast($"영웅을 [{cd.name}] (으)로 교체했습니다");
                    RefreshDeckPage();
                };
            else
                dbl = () =>
                {
                    Pin(cd);
                    if (Profile.Owned(cd.id) == 0) return;
                    if (cd.type == CardType.Field)
                    {
                        // 필드 → 엑스트라 덱
                        if (DeckRules.CanAddExtra(editing, cd, out string whyEx))
                        {
                            editing.extra.Add(cd.id);
                            Profile.MarkSeen(cd.id);
                            SaveDeck();
                            RefreshDeckPage();
                        }
                        else Toast(whyEx);
                        return;
                    }
                    if (DeckRules.CanAdd(editing, cd, out string why))
                    {
                        editing.cards.Add(cd.id);
                        Profile.MarkSeen(cd.id);
                        SaveDeck();
                        RefreshDeckPage();
                    }
                    else Toast(why);
                };
            var rt = CardCell(collectionGrid, c, 112, 157, 0.15f, () => Pin(cd), () => ShowDetail(cd), () => Pin(cd), dbl, out var face);
            Zoomable(rt, cd);
            pc = new PooledCell { rt = rt, face = face, finish = Profile.BestFinish(c.id) };
            collCells[c.id] = pc;
            return pc;
        }

        void SyncFinish(PooledCell cell, string id)
        {
            var f = Profile.BestFinish(id);
            if (cell.finish == f) return;
            cell.finish = f;
            cell.face.SetFinish(f);
        }

        /// <summary>보유 카드 셀의 배지 (사용 중 · 덱/보유 수 · 가공 · NEW · 미보유). 내용이 같으면 건드리지 않는다</summary>
        void CollBadges(PooledCell cell, CardData c)
        {
            string sig;
            int own = 0, inDeck = 0, fz = 0, fsv = 0;
            bool locked = false;
            if (c.type == CardType.Hero) sig = editing.heroId == c.id ? "H1" : "H0";
            else
            {
                own = Profile.Owned(c.id);
                if (own == 0) sig = "U" + (Crafting.CP(c.rarity) >= Crafting.CraftCost ? 1 : 0);
                else
                {
                    inDeck = editing.Count(c.id);
                    fz = Profile.FinishCount(c.id, CardFinish.StrikerZ);
                    fsv = Profile.FinishCount(c.id, CardFinish.Seven);
                    locked = (fz > 0 && Crafting.IsLocked(c.id, CardFinish.StrikerZ)) || (fsv > 0 && Crafting.IsLocked(c.id, CardFinish.Seven));
                    sig = $"O{inDeck}/{own}/{fz}/{fsv}/{locked}/{Profile.IsNew(c.id)}";
                }
            }
            if (cell.badges == sig) return;
            cell.badges = sig;
            var rt = cell.rt;
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                var ch = rt.GetChild(i);
                if (ch.name == "Badge" || ch.name == "Dim") Destroy(ch.gameObject);
            }
            if (c.type == CardType.Hero)
            {
                bool using_ = editing.heroId == c.id;
                Badge(rt, using_ ? "사용 중" : "영웅", using_ ? new Color(0.55f, 0.1f, 0.06f, 0.95f) : UIKit.Accent, new Vector4(0.2f, 0.74f, 0.8f, 0.86f), 15);
                return;
            }
            if (own == 0)
            {
                var dim = UIKit.Img(rt, "Dim", new Color(0, 0, 0, 0.62f));
                dim.rectTransform.Fill();
                Badge(rt, Crafting.CP(c.rarity) >= Crafting.CraftCost ? "생성 가능" : "미보유", new Color(0.2f, 0.2f, 0.24f, 0.95f), new Vector4(0.1f, 0.42f, 0.9f, 0.58f), 15);
                return;
            }
            Badge(rt, $"{inDeck}/{own}", inDeck > 0 ? new Color(0.55f, 0.1f, 0.06f, 0.95f) : new Color(0.08f, 0.08f, 0.12f, 0.92f), new Vector4(0.24f, 0.05f, 0.76f, 0.175f), 16);
            if (fz + fsv > 0)
            {
                string ft = (locked ? "<color=#9cf>■</color>" : "") + (fz > 0 ? FinishStyle.Rich(CardFinish.StrikerZ, fz.ToString()) : "") + (fz > 0 && fsv > 0 ? " " : "") + (fsv > 0 ? FinishStyle.Rich(CardFinish.Seven, fsv.ToString()) : "");
                Badge(rt, ft, new Color(0.05f, 0.04f, 0.08f, 0.92f), new Vector4(0.02f, 0.87f, 0.54f, 0.99f), 15);
            }
            if (Profile.IsNew(c.id)) Badge(rt, "NEW", new Color(0.9f, 0.2f, 0.25f, 0.95f), new Vector4(0.55f, 0.88f, 0.98f, 0.99f), 15);
        }

        /// <summary>현재 덱 그리드: 영웅 + 카드 사본마다 셀을 재사용 (같은 카드 2~3장은 사본 순서대로)</summary>
        void RefreshDeckGrid()
        {
            var heroCard = CardDB.Get(editing.heroId) ?? CardList.Heroes[0];
            if (deckHeroCell == null || deckHeroId != heroCard.id)
            {
                if (deckHeroCell != null) Destroy(deckHeroCell.rt.gameObject);
                var hc = heroCard;
                var hrt = CardCell(heroSlot, hc, 124, 174, 0.17f, () => Pin(hc), () => ShowDetail(hc), () => Pin(hc),
                    () => Toast("영웅은 카드 리스트의 [영웅] 카테고리에서 다른 영웅을 더블클릭해 교체합니다"), out var hface);
                hrt.Fill();
                Zoomable(hrt, hc);
                Badge(hrt, "영웅", UIKit.Accent, new Vector4(0.2f, 0.76f, 0.8f, 0.87f), 15);
                deckHeroCell = new PooledCell { rt = hrt, face = hface, finish = Profile.BestFinish(hc.id) };
                deckHeroId = hc.id;
            }
            var used = new Dictionary<string, int>();
            int sib = 0;
            foreach (var id in editing.cards)
            {
                var c = CardDB.Get(id);
                if (c == null) continue;
                used.TryGetValue(id, out int k);
                used[id] = k + 1;
                var cell = DeckCopy(c, k);
                if (!cell.rt.gameObject.activeSelf) cell.rt.gameObject.SetActive(true);
                if (cell.rt.GetSiblingIndex() != sib) cell.rt.SetSiblingIndex(sib);
                sib++;
                SyncFinish(cell, id);
            }
            foreach (var kv in deckCells)
            {
                used.TryGetValue(kv.Key, out int n);
                for (int i = n; i < kv.Value.Count; i++)
                    if (kv.Value[i].rt.gameObject.activeSelf) kv.Value[i].rt.gameObject.SetActive(false);
            }
        }

        /// <summary>엑스트라 덱 줄 (필드 최대 10장). 더블클릭 = 빼기</summary>
        void RefreshExtraRow()
        {
            if (editing.extra == null) editing.extra = new List<string>();
            extraHead.text = $"엑스트라 덱 (필드)   {editing.extra.Count} / {DeckRules.MaxExtra}   <size=13><color=#a9a3b8>T2 부터 필드 차례에 꺼내 전개 · 같은 필드 1장씩</color></size>";
            UIKit.Clear(extraRow);
            foreach (var id in editing.extra)
            {
                var cd = CardDB.Get(id);
                if (cd == null) continue;
                var rt = CardCell(extraRow, cd, 52, 73, 0.07f, () => Pin(cd), () => ShowDetail(cd), () => Pin(cd), () =>
                {
                    editing.extra.Remove(cd.id);
                    SaveDeck();
                    RefreshDeckPage();
                });
                rt.sizeDelta = new Vector2(52, 73);
                Zoomable(rt, cd);
            }
        }

        /// <summary>덱 그리드의 k번째 사본 셀 (없으면 만든다)</summary>
        PooledCell DeckCopy(CardData c, int k)
        {
            if (!deckCells.TryGetValue(c.id, out var copies)) deckCells[c.id] = copies = new List<PooledCell>();
            while (copies.Count <= k)
            {
                var cd = c;
                var rt = CardCell(deckGrid, cd, 76, 106, 0.1f, () => Pin(cd), () => ShowDetail(cd), () => Pin(cd), () =>
                {
                    editing.cards.Remove(cd.id);
                    SaveDeck();
                    RefreshDeckPage();
                }, out var face);
                Zoomable(rt, cd);
                rt.gameObject.SetActive(false);
                copies.Add(new PooledCell { rt = rt, face = face, finish = Profile.BestFinish(cd.id) });
            }
            return copies[k];
        }

        /// <summary>
        /// 덱 페이지 미리 만들기: 다른 탭에 있는 동안 보유 카드·영웅 셀을 프레임당 몇 장씩(약 4ms) 만들어 둔다.
        /// 덱 탭을 처음 열 때 수백 장을 한꺼번에 만드는 대기를 없앤다.
        /// </summary>
        public void WarmDeckPage()
        {
            if (collWarm != null || !isActiveAndEnabled || collectionGrid == null) return;
            collWarm = StartCoroutine(WarmCells());
        }

        System.Collections.IEnumerator WarmCells()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var cards = CardList.Heroes.Where(StoryData.HeroAvailable).Concat(CardDB.All.Where(c => Profile.Owned(c.id) > 0)).ToList();
            float t0 = Time.realtimeSinceStartup;
            // 사용 덱의 카드 사본부터 (덱 탭을 열면 바로 보이는 곳)
            var deck = Profile.Data.decks.Count > 0 ? Profile.Data.decks[Mathf.Clamp(Profile.Data.activeDeck, 0, Profile.Data.decks.Count - 1)] : null;
            if (deck != null)
            {
                var seen = new Dictionary<string, int>();
                foreach (var id in deck.cards.ToList())
                {
                    var c = CardDB.Get(id);
                    if (c == null) continue;
                    seen.TryGetValue(id, out int k);
                    seen[id] = k + 1;
                    if (deckCells.TryGetValue(id, out var have) && have.Count > k) continue;
                    DeckCopy(c, k);
                    if (Time.realtimeSinceStartup - t0 > 0.004f) { yield return null; t0 = Time.realtimeSinceStartup; }
                }
            }
            foreach (var c in cards)
            {
                if (collCells.ContainsKey(c.id)) continue;
                var pc = CollCell(c);
                pc.rt.gameObject.SetActive(false);
                if (Time.realtimeSinceStartup - t0 > 0.004f) { yield return null; t0 = Time.realtimeSinceStartup; }
            }
            collWarm = null;
        }

        /// <summary>덱 페이지 표시/숨김: GameObject 는 켜 둔 채 자체 Canvas·입력만 끈다</summary>
        void SetDeckPageVisible(bool on)
        {
            if (!deckCanvas.gameObject.activeSelf) deckCanvas.gameObject.SetActive(true);
            deckCanvas.enabled = on;
            deckGroup.blocksRaycasts = on;
            deckGroup.interactable = on;
            if (!on) CardZoom.Close();
        }

        bool Matches(CardData c)
        {
            if (elementFilter >= 0 && (int)c.element != elementFilter) return false;
            if (string.IsNullOrEmpty(search)) return true;
            return (c.name != null && c.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                || (c.text != null && c.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        IEnumerable<CardData> Sorted(IEnumerable<CardData> list)
        {
            IOrderedEnumerable<CardData> o;
            switch (sortMode)
            {
                case 1: o = sortDesc ? list.OrderByDescending(c => c.rarity) : list.OrderBy(c => c.rarity); break;
                case 2: o = sortDesc ? list.OrderByDescending(c => c.name) : list.OrderBy(c => c.name); break;
                case 3: o = sortDesc ? list.OrderByDescending(c => c.type == CardType.Monster ? c.atk : -1) : list.OrderBy(c => c.type == CardType.Monster ? c.atk : 99); break;
                case 4: o = sortDesc ? list.OrderByDescending(c => Profile.Owned(c.id)) : list.OrderBy(c => Profile.Owned(c.id)); break;
                case 5: o = sortDesc ? list.OrderByDescending(c => c.element) : list.OrderBy(c => c.element); break;
                case 6: o = sortDesc ? list.OrderByDescending(c => c.type == CardType.Monster ? c.hp : -1) : list.OrderBy(c => c.type == CardType.Monster ? c.hp : 99); break;
                case 7: o = sortDesc ? list.OrderByDescending(c => c.type == CardType.Monster ? c.minTime : -1) : list.OrderBy(c => c.type == CardType.Monster ? c.minTime : 99); break;
                default: o = sortDesc ? list.OrderBy(CardDB.SortKey) : list.OrderByDescending(CardDB.SortKey); break;
            }
            return o.ThenBy(c => c.id);
        }

        // ══════════════════════════ 상세 ══════════════════════════

        /// <summary>테스트용: 카드 선택</summary>
        public void TestPin(string id) { var c = CardDB.Get(id); if (c != null) Pin(c); }
        /// <summary>테스트용: 덱 화면 갱신 (카드 추가·제거 시와 같은 경로)</summary>
        public void TestRefreshDeck() => RefreshDeckPage();
        /// <summary>테스트용: 보유 카드 그리드를 아직 배치 중인지</summary>
        public bool TestDeckBusy => collLayout != null;

        void Pin(CardData c)
        {
            pinned = c;
            ShowDetail(c, true);
        }

        void ShowDetail(CardData c) => ShowDetail(c, false);

        void ShowDetail(CardData c, bool isPinned)
        {
            detailRoot.gameObject.SetActive(true);
            detailFace.Set(c);
            detailFace.SetFinish(Profile.BestFinish(c.id));
            UIKit.Clear(finishRows);
            detailName.text = c.name;
            string el = $"<color=#{CardData.ElementHex(c.element)}>◆ {CardData.ElementName(c.element)}</color>";
            const string dim = "<color=#a9a3b8>";
            if (c.type == CardType.Hero)
            {
                detailRarity.text = "<color=#ffd45a>HERO</color>";
                detailStats.text = $"{dim}종류</color>  영웅\n{dim}ATK</color>  <b>{c.heroAtk}</b>\n{dim}HP</color>  <b>{c.BattleHeroHp}</b>" + (c.taunt ? "\n<color=#fb8>[도발]</color>" : "") +
                                   (editing?.heroId == c.id ? "\n\n<color=#ffd45a>사용 중</color>" : "");
                detailText.text = $"{c.text}\n\n<color=#a9a3b8>직접 공격을 받으면 공격한 몬스터에게 ATK 만큼 반격합니다.</color>";
                return;
            }
            detailRarity.text = CardData.RarityRich(c.rarity);
            string own = $"\n\n{dim}보유</color>  <b>{Profile.Owned(c.id)}</b>\n{dim}덱</color>  <b>{editing?.Count(c.id) ?? 0}</b> / {DeckRules.MaxCopies(c)}";
            switch (c.type)
            {
                case CardType.Monster:
                    detailStats.text = $"{dim}몬스터</color>  {el}\n{dim}ATK</color>  <b>{c.atk}</b>   {dim}HP</color>  <b>{c.hp}</b>\n{dim}타임</color>  {(c.maxTime > 0 ? $"T{c.minTime}~T{c.maxTime}" : $"T{c.minTime}~")}\n{dim}판정</color>  {c.DiceRolls}회" + own;
                    detailText.text = (string.IsNullOrWhiteSpace(c.text) ? "" : c.text + "\n\n") + c.DiceText();
                    break;
                case CardType.Magic:
                    detailStats.text = $"{dim}종류</color>  마법\n{dim}속도</color>  {(c.speed == MagicSpeed.T0 ? "t0" : "t/half")}\n{dim}발동</color>  {(c.speed == MagicSpeed.T0 ? CardData.TriggerName(c.trigger) : "배틀")}" + own;
                    detailText.text = c.text;
                    break;
                default:
                    detailStats.text = $"{dim}종류</color>  필드\n{dim}속성</color>  {el}" + own;
                    detailText.text = c.text;
                    break;
            }
            if (!(isPinned || c == pinned))
            {
                var hint = UIKit.Txt(finishRows, "Hint", "클릭: 이 카드 선택 (분해·생성·잠금)\n더블클릭: 덱에 넣기 / 빼기", 16, UIKit.Muted, TextAnchor.LowerCenter);
                hint.rectTransform.Fill();
                return;
            }

            // 가공별: 보유 · 잠금 · 분해 (아래에서 위로)
            float y = 58;
            foreach (var f in new[] { CardFinish.StrikerZ, CardFinish.Seven, CardFinish.Normal })
            {
                int n = Profile.FinishCount(c.id, f);
                if (n <= 0 && f != CardFinish.Normal) continue;
                var ff = f;
                var row = UIKit.Img(finishRows, "Row_" + f, new Color(0.08f, 0.06f, 0.13f, 0.9f), GameAssets.Rounded, true).rectTransform;
                row.Anchor(0, 0, 1, 0, 0, y, 0, 0);
                row.sizeDelta = new Vector2(row.sizeDelta.x, 42);
                row.anchoredPosition = new Vector2(0, y + 21);
                string name = FinishStyle.Rich(f);
                var t = UIKit.Txt(row, "N", $"{name} ×{n}", 17, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                t.rectTransform.Anchor(0, 0, 0.44f, 1, 10, 0, 0, 0);
                bool locked = Crafting.IsLocked(c.id, f);
                var lb = UIKit.Button(row, locked ? "잠금" : "해제", () => { Crafting.SetLock(c.id, ff, !Crafting.IsLocked(c.id, ff)); RefreshCollection(); ShowDetail(c, true); },
                    locked ? new Color(0.12f, 0.3f, 0.5f, 0.95f) : new Color(0.12f, 0.12f, 0.16f, 0.95f), 15, locked ? Color.white : UIKit.Muted);
                lb.rt.Anchor(0.45f, 0.1f, 0.64f, 0.9f);
                int can = Crafting.Dismantlable(c, f);
                var db = UIKit.Button(row, $"분해 +{Crafting.DismantleValue(c, f)}", () => Dismantle(c, ff), new Color(0.3f, 0.08f, 0.06f, 0.95f), 15, Color.white);
                db.rt.Anchor(0.66f, 0.1f, 0.99f, 0.9f);
                db.Interactable = can > 0;
                y += 48;
            }
            craftBtn = UIKit.Button(finishRows, $"생성  ({CardData.RarityLabel(c.rarity)} CP {Crafting.CP(c.rarity)}/{Crafting.CraftCost})", () => Craft(c), new Color(0.2f, 0.4f, 0.15f, 0.95f), 17, Color.white);
            craftBtn.rt.Anchor(0, 0, 1, 0, 0, 4, 0, 0);
            craftBtn.rt.sizeDelta = new Vector2(craftBtn.rt.sizeDelta.x, 48);
            craftBtn.rt.anchoredPosition = new Vector2(0, 28);
            craftBtn.Interactable = Crafting.CanCraft(c, out _);
        }

        // ══════════════════════════ 카드 확대 ══════════════════════════
        // 카드 한 번 클릭 → 확대 (1단계) → 클릭 → 더 크게 (2단계) → 클릭 → 닫기. 확대 창은 CardZoom (전투와 공용).

        /// <summary>
        /// 덱 페이지는 자체 Canvas 라서 루트에 직접 그린 그래픽은 그 아래에 깔린다.
        /// 덱 페이지 위에 띄우는 창은 별도 Canvas(정렬 순서 위)로 그리고 입력도 받는다.
        /// </summary>
        static void OverlayCanvas(RectTransform rt, int order)
        {
            var cv = rt.gameObject.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = order;
            rt.gameObject.AddComponent<GraphicRaycaster>();
        }

        void Zoomable(RectTransform cell, CardData c)
        {
            var ev = cell.GetComponent<CellEvents>();
            if (ev != null) ev.onSingleClick = () => ZoomCard(c);
        }

        void ZoomCard(CardData c)
        {
            if (!deckCanvas.enabled) return;   // 그 사이 다른 탭으로 넘어갔으면 무시
            CardZoom.Show(c, Profile.BestFinish(c.id));
        }

        /// <summary>테스트용: 카드 확대 열기 / 확대 창 클릭 / 현재 단계 (0 = 닫힘)</summary>
        public void TestZoom(string id) { var c = CardDB.Get(id); if (c != null) ZoomCard(c); }
        public void TestZoomClick() => CardZoom.Advance();
        public int TestZoomLevel => CardZoom.Level;

        // ══════════════════════════ 확인 창 ══════════════════════════

        RectTransform modalRoot;
        Text modalTitle, modalBody;
        InputField modalInput;
        UIKit.Btn modalYes, modalAlt;

        /// <summary>확인 창. input=true 면 입력칸 표시. alt 가 있으면 두 번째 선택지 버튼.</summary>
        void Modal(string title, string body, bool input, Action<string, bool> onYes, string yesLabel = "확인", Action onAlt = null, string altLabel = null)
        {
            if (modalRoot == null)
            {
                var bg = UIKit.Img(root, "Modal", new Color(0, 0, 0, 0.72f));
                bg.raycastTarget = true;
                modalRoot = bg.rectTransform;
                modalRoot.Fill();
                OverlayCanvas(modalRoot, 41);
                var panel = UIKit.Frame(modalRoot, "Panel", new Color(0.05f, 0.03f, 0.1f, 0.98f));
                panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 380));
                modalTitle = UIKit.Txt(panel.transform, "Title", "", 32, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                modalTitle.rectTransform.Anchor(0, 1, 1, 1, 20, -70, 20, 16);
                modalBody = UIKit.Txt(panel.transform, "Body", "", 22, UIKit.Ink, TextAnchor.UpperCenter);
                modalBody.rectTransform.Anchor(0, 0, 1, 1, 34, 150, 34, 86);
                modalInput = UIKit.Input(panel.transform, "", "SZ1-...", 20, 4000);
                modalInput.GetComponent<RectTransform>().Anchor(0, 0, 1, 0, 34, 104, 34, -160);
                modalYes = UIKit.Button(panel.transform, "확인", null, Red, 24, Color.white);
                modalYes.rt.Anchor(0.52f, 0, 0.74f, 0, 0, 24, 0, -90);
                modalAlt = UIKit.Button(panel.transform, "", null, Dark, 22);
                modalAlt.rt.Anchor(0.28f, 0, 0.5f, 0, 0, 24, 0, -90);
                UIKit.Button(panel.transform, "취소", () => modalRoot.gameObject.SetActive(false), new Color(0.1f, 0.1f, 0.14f, 0.95f), 22, UIKit.Muted).rt.Anchor(0.76f, 0, 0.96f, 0, 0, 24, 0, -90);
            }
            modalRoot.SetAsLastSibling();
            modalRoot.gameObject.SetActive(true);
            modalTitle.text = title;
            modalBody.text = body;
            modalInput.gameObject.SetActive(input);
            modalInput.text = input ? (GUIUtility.systemCopyBuffer?.StartsWith("SZ1-") == true ? GUIUtility.systemCopyBuffer : "") : "";
            modalYes.bg.gameObject.SetActive(yesLabel != null);
            modalYes.label.text = yesLabel ?? "";
            modalYes.button.onClick.RemoveAllListeners();
            modalYes.button.onClick.AddListener(() => { Sfx.Play("click", 0.8f); string v = modalInput.text; modalRoot.gameObject.SetActive(false); onYes?.Invoke(v, true); });
            modalAlt.bg.gameObject.SetActive(onAlt != null);
            modalAlt.label.text = altLabel ?? "";
            modalAlt.button.onClick.RemoveAllListeners();
            modalAlt.button.onClick.AddListener(() => { Sfx.Play("click", 0.8f); modalRoot.gameObject.SetActive(false); onAlt?.Invoke(); });
        }
    }
}
