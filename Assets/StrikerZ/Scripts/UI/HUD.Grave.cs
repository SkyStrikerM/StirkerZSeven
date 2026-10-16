using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>묘지 보기: 묘지 더미(보드)나 영웅 패널의 [묘지] 버튼을 누르면 나 / 상대 묘지 카드를 모두 펼쳐 본다 (최근에 간 카드부터, 카드 클릭 = 확대)</summary>
    public partial class HUD
    {
        RectTransform graveRoot, graveGrid;
        Text graveTitle;
        UIKit.Btn graveMine, graveOpp;

        public bool GraveOpen => graveRoot != null && graveRoot.gameObject.activeSelf;

        void BuildGrave()
        {
            var dim = UIKit.Img(root, "Grave", new Color(0, 0, 0, 0.6f));
            graveRoot = dim.rectTransform;
            graveRoot.Fill();
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseGrave);

            var panel = UIKit.Frame(graveRoot, "Panel", new Color(0.035f, 0.028f, 0.07f, 0.97f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 820));
            panel.raycastTarget = true;     // 판 안을 눌러도 닫히지 않게
            graveTitle = UIKit.Txt(panel.transform, "Title", "", 26, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            graveTitle.rectTransform.Anchor(0, 1, 1, 1, 28, -62, 560, 18);
            graveTitle.Section(UIKit.SecMe, 26);

            graveMine = UIKit.Button(panel.transform, "내 묘지", () => ShowGrave(true), null, 20);
            graveMine.rt.Place(new Vector2(1, 1), new Vector2(-418, -16), new Vector2(150, 46), new Vector2(1, 1));
            graveOpp = UIKit.Button(panel.transform, "상대 묘지", () => ShowGrave(false), null, 20);
            graveOpp.rt.Place(new Vector2(1, 1), new Vector2(-260, -16), new Vector2(150, 46), new Vector2(1, 1));
            UIKit.Button(panel.transform, "닫기", CloseGrave, null, 20).rt
                .Place(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(120, 46), new Vector2(1, 1));

            var view = UIKit.Rect("View", panel.transform).Anchor(0, 0, 1, 1, 16, 16, 16, 80);
            graveGrid = UIKit.ScrollGrid(view, "Scroll", new Vector2(150, 210), new Vector2(10, 12), out _);
            ((RectTransform)graveGrid.parent).Fill();
            graveRoot.gameObject.SetActive(false);
        }

        /// <summary>묘지 보기 열기 (mine = 내 묘지부터)</summary>
        public void OpenGrave(bool mine)
        {
            if (g == null || g.players[0] == null) return;
            if (graveRoot == null) BuildGrave();
            CloseMenu();
            graveRoot.gameObject.SetActive(true);
            graveRoot.SetAsLastSibling();
            ShowGrave(mine);
        }

        public void CloseGrave() { if (graveRoot != null) graveRoot.gameObject.SetActive(false); }

        void ShowGrave(bool mine)
        {
            graveMine.SetSelected(mine);
            graveOpp.SetSelected(!mine);
            var p = mine ? g.Human : g.Cpu;
            graveTitle.text = $"{(mine ? "내" : "상대")} 묘지  <size=18><color=#a9a3b8>{p.grave.Count}장 · 최근에 간 카드부터 · 클릭 = 확대</color></size>";
            UIKit.Clear(graveGrid);
            if (p.grave.Count == 0)
            {
                var none = UIKit.Txt(graveGrid, "Empty", "묘지가 비어 있습니다", 22, UIKit.Muted);
                none.rectTransform.sizeDelta = new Vector2(400, 210);
                return;
            }
            foreach (var c in p.grave.AsEnumerable().Reverse())
            {
                var card = c;
                var cell = UIKit.Rect("G_" + c.uid, graveGrid);
                cell.HitArea();
                var face = CardFace.Build(cell, 150, 210, 0.22f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(c.data);
                face.SetFinish(c.finish);
                var btn = cell.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => CardZoom.Show(card.data, card.finish));
            }
        }
    }
}
