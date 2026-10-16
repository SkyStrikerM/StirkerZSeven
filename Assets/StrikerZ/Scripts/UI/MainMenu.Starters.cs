using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>덱 편집 › 원소 스타터 덱: 7속성 완성 덱을 오팔로 받기 (카드 40장 + 덱)</summary>
    public partial class MainMenu
    {
        RectTransform starterRoot, starterList;

        public void TestOpenStarters() => OpenStarters();

        void OpenStarters()
        {
            if (starterRoot == null) BuildStarters();
            RefreshStarters();
            starterRoot.SetAsLastSibling();
            starterRoot.gameObject.SetActive(true);
        }

        void BuildStarters()
        {
            var bg = UIKit.Img(root, "Starters", new Color(0, 0, 0, 0.9f));
            bg.raycastTarget = true;
            starterRoot = bg.rectTransform;
            starterRoot.Fill();
            OverlayCanvas(starterRoot, 41);
            var panel = UIKit.Frame(starterRoot, "Panel", new Color(0.04f, 0.03f, 0.08f, 0.98f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1380, 960));
            var p = panel.transform;
            var title = UIKit.Txt(p, "Title", "원소 스타터 덱", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 30, -64, 30, 20);
            title.Section(UIKit.SecCollect, 26);
            var sub = UIKit.Txt(p, "Sub", $"속성마다 바로 쓸 수 있는 40장 덱 + 엑스트라 덱 필드 {StarterDecks.ExtraFields}장 · 오팔 {StarterDecks.Price} · 카드를 받고 덱 목록에 추가됩니다 (속성마다 1번) · 카드를 누르면 확대", 19, UIKit.Muted, TextAnchor.MiddleLeft);
            sub.rectTransform.Anchor(0, 1, 1, 1, 32, -104, 30, 70);
            var close = UIKit.Button(p, "닫기", () => starterRoot.gameObject.SetActive(false), Dark, 20);
            close.rt.Place(new Vector2(1, 1), new Vector2(-20, -16), new Vector2(120, 46), new Vector2(1, 1));
            starterList = UIKit.Rect("List", p).Anchor(0, 0, 1, 1, 24, 20, 24, 116);
            var vl = starterList.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8;
            vl.childControlWidth = true; vl.childControlHeight = false; vl.childForceExpandHeight = false;
        }

        void RefreshStarters()
        {
            UIKit.Clear(starterList);
            foreach (var e in StarterDecks.Elements)
            {
                var el = e;
                var col = CardData.ElementColor(e);
                var row = UIKit.Img(starterList, "Row_" + e, new Color(0.07f, 0.05f, 0.11f, 0.95f), GameAssets.Rounded, true).rectTransform;
                row.sizeDelta = new Vector2(0, 110);
                var chip = UIKit.Img(row, "Chip", col, GameAssets.Rounded, true);
                chip.rectTransform.Anchor(0, 0, 0, 1, 8, 8, -16, 8);
                var hero = StarterDecks.Hero(e);
                var cards = StarterDecks.Cards(e);
                var name = UIKit.Txt(row, "Name", $"{StarterDecks.Name(e)}", 26, col, TextAnchor.LowerLeft, FontStyle.Bold);
                name.rectTransform.Anchor(0, 0.5f, 0.32f, 1, 30, 0, 0, 6);
                int mons = cards.Count(c => c.type == CardType.Monster), mags = cards.Count(c => c.type == CardType.Magic), flds = cards.Count(c => c.type == CardType.Field);
                var info = UIKit.Txt(row, "Info", $"영웅 {hero.name} · 소환수 {mons} · 마법 {mags} · 필드 {flds}", 17, UIKit.Ink, TextAnchor.UpperLeft);
                info.rectTransform.Anchor(0, 0, 0.32f, 0.5f, 30, 4, 0, 4);
                // 대표 카드 7장 (등급 → ATK+HP 순)
                var strip = UIKit.Rect("Strip", row).Anchor(0.32f, 0, 0.82f, 1, 0, 6, 0, 6);
                var hl = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 6; hl.childControlWidth = hl.childControlHeight = false; hl.childAlignment = TextAnchor.MiddleLeft;
                foreach (var c in cards.Distinct().OrderByDescending(c => (int)c.rarity).ThenByDescending(c => c.atk + c.hp).Take(7))
                {
                    var card = c;
                    var cell = CardCell(strip, c, 70, 98, 0.12f, null, null);
                    CardZoom.Bind(cell, () => card);
                }
                bool bought = StarterDecks.Bought(e);
                var buy = UIKit.Button(row, bought ? "받음" : $"오팔 {StarterDecks.Price}", () =>
                {
                    if (StarterDecks.Buy(el, out string msg)) { RefreshStarters(); RefreshDeckPage(); RefreshTop(); }
                    Toast(msg);
                }, bought ? Dark : Red, 22, bought ? UIKit.Muted : Color.white);
                buy.rt.Anchor(0.84f, 0.2f, 0.985f, 0.8f);
                buy.Interactable = !bought;
            }
        }
    }
}
