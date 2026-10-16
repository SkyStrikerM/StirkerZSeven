using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 도감 (마스터 듀얼식): 부스터팩마다 클리어파일 바인더 한 권. 책장에서 바인더를 고르면 펼쳐서
    /// 한 쪽 3×3 포켓(펼침면 18장)에 수록 카드를 번호순으로 담아 보여 준다. 보유 = 카드, 미보유 = 빈 실루엣.
    /// 그림: Tools/binder (VARCO nano-banana-pro)
    /// </summary>
    public partial class MainMenu
    {
        RectTransform colShelf, colBinder, shelfRow, pageL, pageR;
        Text binderTitle, binderPageText, binderCount;
        UIKit.Btn binderPrev, binderNext;
        readonly List<UIKit.Btn> binderFilterBtns = new List<UIKit.Btn>();
        BoosterPack openPack;
        /// <summary>(카드, 바인더 번호) — 필터를 걸어도 번호는 바인더 전체 기준</summary>
        List<(CardData card, int no)> binderCards = new List<(CardData, int)>();
        int binderSpread, binderFilter;
        const int PocketsPerPage = 9;
        static readonly string[] BinderFilters = { "전체", "몬스터", "마법", "필드" };

        // 펼친 바인더 그림(1376×768)에서 속지 위치
        static readonly Rect PageLRect = new Rect(175 / 1376f, 1 - 705 / 768f, 480 / 1376f, 637 / 768f);
        static readonly Rect PageRRect = new Rect(728 / 1376f, 1 - 705 / 768f, 480 / 1376f, 637 / 768f);

        static Sprite sheen;
        /// <summary>클리어 포켓의 비닐 광택 (대각선 빛줄기)</summary>
        static Sprite Sheen
        {
            get
            {
                if (sheen != null) return sheen;
                const int N = 128;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = (x + y) / (float)N;   // 0 (왼쪽 아래) ~ 2 (오른쪽 위)
                        float band = Mathf.Max(0, 1 - Mathf.Abs(d - 1.25f) / 0.16f) * 0.75f + Mathf.Max(0, 1 - Mathf.Abs(d - 1.55f) / 0.05f) * 0.5f;
                        float top = Mathf.Clamp01((y / (float)N - 0.55f) / 0.45f) * 0.25f;
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(band + top)));
                    }
                tex.Apply();
                return sheen = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>바인더 수록 순서: 몬스터 → 마법 → 필드, 그 안에서 카드 번호순</summary>
        static List<CardData> PackCards(BoosterPack p)
            => CardDB.All.Where(p.pool).OrderBy(c => c.type == CardType.Monster ? 0 : c.type == CardType.Magic ? 1 : 2).ThenBy(c => c.id).ToList();

        static Sprite BinderCover(BoosterPack p) => GameAssets.TexSprite("binder_" + p.id.ToLowerInvariant()) ?? GameAssets.TexSprite("binder_sss");

        RectTransform BuildCollection()
        {
            var page = UIKit.Rect("Collection", contentArea).Fill();

            // ── 책장 ──
            var shelf = HomeCard(page, "Shelf", "도감 · 카드 바인더", new Color(0.45f, 0.72f, 1f), "부스터팩마다 클리어파일 한 권 — 바인더를 눌러 펼치기");
            colShelf = shelf.rectTransform;
            colShelf.Fill();
            var plank = UIKit.Img(colShelf, "Plank", new Color(0.16f, 0.1f, 0.06f, 0.95f), GameAssets.Rounded, true);
            plank.rectTransform.Anchor(0, 0, 1, 0, 26, 258, 26, -282);
            var plankHi = UIKit.Img(plank.transform, "Hi", new Color(1f, 0.8f, 0.45f, 0.35f));
            plankHi.rectTransform.Anchor(0, 1, 1, 1, 6, -2, 6, 0);
            shelfRow = UIKit.Rect("Row", colShelf).Anchor(0, 0, 1, 1, 60, 30, 40, 76);
            var hl = shelfRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 56;
            hl.childAlignment = TextAnchor.LowerLeft;
            hl.childControlWidth = hl.childControlHeight = false;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;

            // ── 펼친 바인더 ──
            colBinder = UIKit.Rect("Binder", page).Fill();
            var bar = UIKit.Rect("Bar", colBinder).Anchor(0, 1, 1, 1, 0, -62, 0, 0);
            var back = UIKit.Button(bar, "◀  책장", () => { colBinder.gameObject.SetActive(false); colShelf.gameObject.SetActive(true); RefreshShelf(); }, Dark, 22);
            back.rt.Place(new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(150, 54), new Vector2(0, 0.5f));
            binderTitle = UIKit.Txt(bar, "Title", "", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            binderTitle.rectTransform.Anchor(0, 0, 1, 1, 170, 0, 560, 0);
            binderFilterBtns.Clear();
            for (int i = 0; i < BinderFilters.Length; i++)
            {
                int f = i;
                var b = UIKit.Button(bar, BinderFilters[i], () => { binderFilter = f; binderSpread = 0; RefreshBinder(); }, Dark, 20);
                b.rt.Place(new Vector2(1, 0.5f), new Vector2(-(BinderFilters.Length - 1 - i) * 118, 0), new Vector2(110, 50), new Vector2(1, 0.5f));
                binderFilterBtns.Add(b);
            }

            var area = UIKit.Rect("Area", colBinder).Anchor(0, 0, 1, 1, 0, 0, 0, 72);
            var open = UIKit.Img(area, "Open", Color.white, GameAssets.TexSprite("binder_open"));
            open.rectTransform.anchorMin = open.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var fit = open.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1376f / 768f;
            pageL = BinderPage(open.transform, "PageL", PageLRect);
            pageR = BinderPage(open.transform, "PageR", PageRRect);

            binderPrev = UIKit.Button(area, "◀", () => FlipBinder(-1), new Color(0.05f, 0.04f, 0.1f, 0.92f), 34);
            binderPrev.rt.Place(new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(64, 140), new Vector2(0, 0.5f));
            binderNext = UIKit.Button(area, "▶", () => FlipBinder(1), new Color(0.05f, 0.04f, 0.1f, 0.92f), 34);
            binderNext.rt.Place(new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(64, 140), new Vector2(1, 0.5f));
            var pg = UIKit.Img(area, "PageBox", new Color(0.02f, 0.01f, 0.05f, 0.85f), GameAssets.Rounded, true);
            pg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(380, 36), new Vector2(0.5f, 0));
            binderPageText = UIKit.Txt(pg.transform, "T", "", 18, UIKit.Ink, TextAnchor.MiddleCenter);
            binderPageText.rectTransform.Fill();
            binderCount = UIKit.Txt(bar, "Count", "", 18, UIKit.Muted, TextAnchor.MiddleRight);
            binderCount.rectTransform.Anchor(0, 0, 1, 1, 0, 0, 486, 0);
            binderCount.gameObject.SetActive(false);

            colBinder.gameObject.SetActive(false);
            return page;
        }

        static RectTransform BinderPage(Transform open, string name, Rect r)
        {
            var p = UIKit.Rect(name, open).Anchor(r.xMin, r.yMin, r.xMax, r.yMax);
            var g = p.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(158, 220);
            g.spacing = new Vector2(12, 10);
            g.childAlignment = TextAnchor.MiddleCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            return p;
        }

        void RefreshCollection()
        {
            colBinder.gameObject.SetActive(false);
            colShelf.gameObject.SetActive(true);
            RefreshShelf();
        }

        // ══════════════ 책장 ══════════════

        void RefreshShelf()
        {
            UIKit.Clear(shelfRow);
            foreach (var p in BoosterPack.Packs) ShelfBinder(p);
            // 다음 팩 자리 (아직 없음)
            for (int i = 0; i < 2; i++) ShelfBinder(null);
        }

        void ShelfBinder(BoosterPack p)
        {
            var slot = UIKit.Rect(p != null ? "Binder_" + p.id : "Soon", shelfRow);
            slot.sizeDelta = new Vector2(360, 760);
            var cover = UIKit.Img(slot, "Cover", p != null ? Color.white : new Color(0.16f, 0.14f, 0.22f, 0.85f), GameAssets.TexSprite("binder_sss"));
            if (p != null) cover.sprite = BinderCover(p);
            cover.preserveAspect = true;
            cover.rectTransform.Anchor(0, 0, 1, 0, 10, 250, 10, -740);
            if (p == null)
            {
                var soon = UIKit.Txt(cover.transform, "Soon", "COMING\nSOON", 34, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
                soon.rectTransform.Fill();
                var sl = UIKit.Txt(slot, "Label", "다음 부스터팩", 20, UIKit.Muted, TextAnchor.UpperCenter);
                sl.rectTransform.Anchor(0, 0, 1, 0, 0, 150, 0, -200);
                return;
            }

            // 표지 아래쪽 이름판 (팩 코드 · 이름)
            var plate = UIKit.Img(cover.transform, "Plate", new Color(0.02f, 0.01f, 0.05f, 0.78f), GameAssets.Rounded, true);
            plate.rectTransform.Anchor(0.14f, 0.07f, 0.9f, 0.2f);
            var pb = UIKit.Img(plate.transform, "Border", new Color(1f, 0.82f, 0.36f, 0.7f), GameAssets.RoundedOutline, true);
            pb.rectTransform.Fill();
            var code = UIKit.Txt(plate.transform, "Code", p.code, 30, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            code.rectTransform.Anchor(0, 0.42f, 1, 1, 4, 0, 4, 2);
            code.Outline(new Color(0.25f, 0.08f, 0, 0.9f), 2);
            var nm = UIKit.Txt(plate.transform, "Name", p.name, 13, UIKit.Ink, TextAnchor.MiddleCenter);
            nm.rectTransform.Anchor(0, 0, 1, 0.45f, 4, 4, 4, 0);

            // 수집률
            var cards = PackCards(p);
            int own = cards.Count(c => Profile.Owned(c.id) > 0);
            float rate = cards.Count == 0 ? 0 : own / (float)cards.Count;
            var t = UIKit.Txt(slot, "Rate", $"<b>{own}</b> / {cards.Count}   <color=#ffd77a>{Mathf.FloorToInt(rate * 100)}%</color>", 24, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.Anchor(0, 0, 1, 0, 0, 180, 0, -218);
            var barBg = UIKit.Img(slot, "BarBg", new Color(1, 1, 1, 0.1f), GameAssets.Rounded, true);
            barBg.rectTransform.Anchor(0, 0, 1, 0, 24, 160, 24, -172);
            var fill = UIKit.Img(barBg.transform, "Fill", new Color(0.45f, 0.72f, 1f, 0.95f), GameAssets.Rounded, true);
            fill.rectTransform.Anchor(0, 0, Mathf.Max(0.02f, rate), 1);
            // 등급별
            var lines = new List<string>();
            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
            {
                var rc = cards.Where(c => c.rarity == r).ToList();
                if (rc.Count == 0) continue;
                lines.Add($"<color=#a9a3b8>{CardData.RarityLabel(r)}</color>  {rc.Count(c => Profile.Owned(c.id) > 0)} / {rc.Count}");
            }
            var rr = UIKit.Txt(slot, "Rarity", string.Join("\n", lines), 17, UIKit.Ink, TextAnchor.UpperCenter);
            rr.rectTransform.Anchor(0, 0, 1, 0, 0, 0, 0, -150);
            rr.lineSpacing = 1.1f;

            // 누르면 펼치기
            cover.raycastTarget = true;
            var btn = cover.gameObject.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = new Color(1.15f, 1.12f, 1.05f);
            cs.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = cs;
            btn.onClick.AddListener(() => { Sfx.Play("click", 0.8f); OpenBinder(p); });
        }

        // ══════════════ 펼친 바인더 ══════════════

        public void OpenBinder(BoosterPack p)
        {
            openPack = p;
            binderSpread = 0;
            binderFilter = 0;
            colShelf.gameObject.SetActive(false);
            colBinder.gameObject.SetActive(true);
            RefreshBinder();
        }

        void FlipBinder(int dir)
        {
            int max = SpreadCount - 1;
            int next = Mathf.Clamp(binderSpread + dir, 0, max);
            if (next == binderSpread) return;
            binderSpread = next;
            Sfx.Play("flip", 0.6f);
            RefreshBinderPages();
        }

        int SpreadCount => Mathf.Max(1, Mathf.CeilToInt(binderCards.Count / (float)(PocketsPerPage * 2)));

        void RefreshBinder()
        {
            var all = PackCards(openPack);
            binderCards = all.Select((c, i) => (c, i + 1))
                .Where(x => binderFilter == 0 || x.c.type == (binderFilter == 1 ? CardType.Monster : binderFilter == 2 ? CardType.Magic : CardType.Field))
                .ToList();
            for (int i = 0; i < binderFilterBtns.Count; i++) binderFilterBtns[i].SetSelected(i == binderFilter);
            int own = all.Count(c => Profile.Owned(c.id) > 0);
            binderTitle.text = $"{openPack.code}  <size=20><color=#e8e6f0>{openPack.name}</color>   <color=#a9a3b8>수집</color> {own} / {all.Count}</size>";
            binderSpread = Mathf.Clamp(binderSpread, 0, SpreadCount - 1);
            RefreshBinderPages();
        }

        void RefreshBinderPages()
        {
            int start = binderSpread * PocketsPerPage * 2;
            FillPage(pageL, start);
            FillPage(pageR, start + PocketsPerPage);
            int shown = binderCards.Skip(start).Take(PocketsPerPage * 2).Count();
            int ownHere = binderCards.Skip(start).Take(PocketsPerPage * 2).Count(x => Profile.Owned(x.card.id) > 0);
            binderPageText.text = $"{binderSpread + 1} / {SpreadCount} 펼침   ·   이 면 {ownHere} / {shown}";
            binderPrev.Interactable = binderSpread > 0;
            binderNext.Interactable = binderSpread < SpreadCount - 1;
        }

        void FillPage(RectTransform page, int start)
        {
            UIKit.Clear(page);
            for (int i = 0; i < PocketsPerPage; i++)
            {
                int k = start + i;
                Pocket(page, k < binderCards.Count ? binderCards[k] : ((CardData)null, 0));
            }
        }

        /// <summary>클리어 포켓 한 칸: 보유 = 카드(누르면 확대) + 장수, 미보유 = 번호만 있는 빈 실루엣</summary>
        void Pocket(RectTransform page, (CardData card, int no) e)
        {
            var p = UIKit.Img(page, "Pocket", new Color(0.75f, 0.8f, 1f, 0.07f), GameAssets.Rounded, true).rectTransform;
            var edge = UIKit.Img(p, "Edge", new Color(1, 1, 1, 0.22f), GameAssets.RoundedOutline, true);
            edge.rectTransform.Fill();
            edge.raycastTarget = false;
            var c = e.card;
            if (c != null)
            {
                int n = Profile.Owned(c.id);
                if (n > 0)
                {
                    var cd = c;
                    var cell = CardCell(p, c, 146, 204, 0.23f, () => CardZoom.Show(cd, Profile.BestFinish(cd.id)), null);
                    cell.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(146, 204));
                    if (n > 1) Badge(cell, "×" + n, new Color(0.1f, 0.3f, 0.6f, 0.95f), new Vector4(0.7f, 0.89f, 1.04f, 1.01f), 16);
                }
                else
                {
                    var sil = UIKit.Img(p, "Empty", new Color(0.05f, 0.04f, 0.1f, 0.75f), GameAssets.Rounded, true);
                    sil.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(146, 204));
                    var q = UIKit.Txt(sil.transform, "Q", "?", 64, new Color(1, 1, 1, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    q.rectTransform.Fill();
                    var no = UIKit.Txt(sil.transform, "No", $"No.{e.no:000}", 16, new Color(1, 1, 1, 0.45f), TextAnchor.UpperCenter, FontStyle.Bold);
                    no.rectTransform.Anchor(0, 0.8f, 1, 0.97f);
                    var rl = UIKit.Txt(sil.transform, "R", CardData.RarityLabel(c.rarity), 14, RarityTint(c.rarity), TextAnchor.LowerCenter);
                    rl.rectTransform.Anchor(0, 0.03f, 1, 0.2f);
                }
            }
            // 비닐 광택 (맨 위, 클릭은 통과)
            var s = UIKit.Img(p, "Sheen", new Color(1, 1, 1, 0.1f), Sheen);
            s.rectTransform.Fill();
            s.raycastTarget = false;
        }

        static Color RarityTint(Rarity r) => r == Rarity.Unique ? new Color(1f, 0.5f, 0.35f, 0.8f) : r == Rarity.Rare ? new Color(1f, 0.82f, 0.4f, 0.8f) : r == Rarity.Uncommon ? new Color(0.55f, 0.8f, 1f, 0.75f) : new Color(1, 1, 1, 0.5f);

        /// <summary>테스트용</summary>
        public void TestOpenBinder(int spread) { OpenBinder(BoosterPack.Packs[0]); binderSpread = spread; RefreshBinderPages(); }
    }
}
