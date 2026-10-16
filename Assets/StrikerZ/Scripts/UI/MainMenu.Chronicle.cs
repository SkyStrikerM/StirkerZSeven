using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>프로필 › 듀얼 연대기: 최근 대전 · 많이 쓴 카드</summary>
    public partial class MainMenu
    {
        RectTransform chronRoot, chronList, chronCards;
        Text chronEmpty;

        public void TestOpenChronicle() => OpenChronicle();

        void OpenChronicle()
        {
            if (chronRoot == null) BuildChronicle();
            RefreshChronicle();
            chronRoot.SetAsLastSibling();
            chronRoot.gameObject.SetActive(true);
        }

        void BuildChronicle()
        {
            var bg = UIKit.Img(root, "Chronicle", new Color(0, 0, 0, 0.9f));
            bg.raycastTarget = true;
            chronRoot = bg.rectTransform;
            chronRoot.Fill();
            OverlayCanvas(chronRoot, 41);
            var panel = UIKit.Frame(chronRoot, "Panel", new Color(0.04f, 0.03f, 0.08f, 0.98f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 960));
            var p = panel.transform;
            var title = UIKit.Txt(p, "Title", "듀얼 연대기", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 30, -64, 30, 20);
            title.Section(UIKit.SecMe, 26);
            var sub = UIKit.Txt(p, "Sub", $"최근 {Chronicle.Keep}판까지 기록 · 훈련장 · 마법진 퍼즐은 기록하지 않습니다", 18, UIKit.Muted, TextAnchor.MiddleLeft);
            sub.rectTransform.Anchor(0, 1, 1, 1, 32, -102, 30, 70);
            var close = UIKit.Button(p, "닫기", () => chronRoot.gameObject.SetActive(false), Dark, 20);
            close.rt.Place(new Vector2(1, 1), new Vector2(-20, -16), new Vector2(120, 46), new Vector2(1, 1));

            // 왼쪽: 최근 대전
            var left = UIKit.Img(p, "Recent", new Color(0.02f, 0.02f, 0.05f, 0.85f), GameAssets.Rounded, true).rectTransform;
            left.Anchor(0, 0, 0.58f, 1, 20, 20, 10, 116);
            UIKit.Txt(left, "H", "최근 대전", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Anchor(0, 1, 1, 1, 16, -44, 10, 8);
            var sv = UIKit.Rect("Scroll", left).Anchor(0, 0, 1, 1, 8, 8, 8, 50);
            sv.HitArea();
            sv.gameObject.AddComponent<RectMask2D>();
            chronList = UIKit.Rect("Content", sv);
            chronList.anchorMin = new Vector2(0, 1); chronList.anchorMax = new Vector2(1, 1); chronList.pivot = new Vector2(0.5f, 1);
            chronList.offsetMin = chronList.offsetMax = Vector2.zero;
            var vl = chronList.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 4; vl.childControlWidth = true; vl.childControlHeight = false; vl.childForceExpandHeight = false;
            chronList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.content = chronList; sr.viewport = sv; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            chronEmpty = UIKit.Txt(left, "Empty", "아직 기록이 없습니다 — 대전을 해 보세요!", 22, UIKit.Muted, TextAnchor.MiddleCenter);
            chronEmpty.rectTransform.Fill();

            // 오른쪽: 많이 쓴 카드 (승패 집계·승률은 없음 — 전적은 온라인 대전 전용)
            var right = UIKit.Img(p, "Stats", new Color(0.02f, 0.02f, 0.05f, 0.85f), GameAssets.Rounded, true).rectTransform;
            right.Anchor(0.58f, 0, 1, 1, 10, 20, 20, 116);
            UIKit.Txt(right, "H3", "많이 쓴 카드", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Anchor(0, 1, 1, 1, 16, -44, 10, 8);
            chronCards = UIKit.Rect("Cards", right).Anchor(0, 0, 1, 1, 14, 14, 14, 56);
            var gl = chronCards.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(108, 178); gl.spacing = new Vector2(8, 6);
            chronRoot.gameObject.SetActive(false);
        }

        void RefreshChronicle()
        {
            UIKit.Clear(chronList);
            var recs = Chronicle.Records;
            chronEmpty.gameObject.SetActive(recs.Count == 0);
            foreach (var r in recs)
            {
                var row = UIKit.Img(chronList, "R", r.won ? new Color(0.08f, 0.16f, 0.1f, 0.9f) : new Color(0.17f, 0.07f, 0.07f, 0.9f), GameAssets.Rounded, true).rectTransform;
                row.sizeDelta = new Vector2(0, 58);
                var hero = CardDB.Get(r.hero);
                var opp = CardDB.Get(r.oppHero);
                var res = UIKit.Txt(row, "Res", r.won ? "승리" : "패배", 22, r.won ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
                res.rectTransform.Anchor(0, 0, 0, 1, 6, 0, -80, 0);
                var t = UIKit.Txt(row, "T",
                    $"<color=#ffd77a>{Chronicle.ModeName(r.mode, r.level)}</color>   {hero?.name ?? "?"} <color=#999>vs</color> {r.oppName} ({opp?.name ?? "?"})\n" +
                    $"<size=15><color=#aaa>{r.date} · T{r.time} · HP {r.hp} : {r.oppHp}{(string.IsNullOrEmpty(r.deck) ? "" : " · " + r.deck)}</color></size>",
                    18, UIKit.Ink, TextAnchor.MiddleLeft);
                t.rectTransform.Anchor(0, 0, 1, 1, 92, 2, 10, 2);
            }
            UIKit.Clear(chronCards);
            foreach (var (card, n) in Chronicle.TopCards(15))
            {
                var c = card;
                var cell = UIKit.Rect("C", chronCards);
                var cc = CardCell(cell, c, 108, 151, 0.17f, null, null);
                cc.anchorMin = cc.anchorMax = new Vector2(0.5f, 1); cc.pivot = new Vector2(0.5f, 1); cc.anchoredPosition = Vector2.zero;
                CardZoom.Bind(cc, () => c);
                var lab = UIKit.Txt(cell, "N", $"{n}회", 17, UIKit.Gold, TextAnchor.LowerCenter, FontStyle.Bold);
                lab.rectTransform.Fill();
            }
        }
    }
}
