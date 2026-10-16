using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 프로필 › 듀얼 액세서리 (마스터듀얼의 액세서리 설정처럼):
    /// 카드 슬리브 · 덱케이스 · 코인 · 주사위 4칸에 지금 장착한 것을 보여 주고,
    /// 칸을 누르면 선택 창 — 큰 미리보기(주사위·코인은 3D) + 보유 아이템 장착 / 미보유는 루콘 공방으로.
    /// </summary>
    public partial class MainMenu
    {
        RectTransform accRow, accRoot, accGrid, accPreview;
        Text accTitle, accName, accDesc, accState;
        Image accGlow;
        UIKit.Btn accEquip, accShop;
        CosmeticKind accKind;
        CosmeticItem accSel;

        /// <summary>프로필 왼쪽 아래: 제목 + 4칸</summary>
        void BuildAccessories(Transform parent, float top)
        {
            var t = UIKit.Txt(parent, "AccLabel", "듀얼 액세서리", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-380, top), new Vector2(300, 34), new Vector2(0, 1));
            var hint = UIKit.Txt(parent, "AccHint", "칸을 눌러 바꾸기 · 듀얼에서 내 쪽에 적용", 15, UIKit.Muted, TextAnchor.MiddleRight);
            hint.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(380, top), new Vector2(420, 34), new Vector2(1, 1));
            accRow = UIKit.Rect("Accessories", parent);
            accRow.Place(new Vector2(0.5f, 1), new Vector2(0, top - 42), new Vector2(760, 232), new Vector2(0.5f, 1));
            var h = accRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            // 화면을 떠나면 3D 무대 정리
            accRow.gameObject.AddComponent<ActiveHook>().off = DiceStage.Stop;
        }

        void RefreshAccessories()
        {
            if (accRow == null) return;
            UIKit.Clear(accRow);
            Rect[] diceUv = null;
            var dice = Cosmetics.OfKind(CosmeticKind.Dice).ToList();
            Texture diceTex = DiceStage.Icons(dice, out diceUv);
            foreach (var k in Cosmetics.Kinds)
            {
                var kind = k;
                var it = Cosmetics.Equipped(k);
                var tint = it.Tint;
                var cell = UIKit.Img(accRow, "Acc_" + k, new Color(0.07f, 0.06f, 0.11f, 0.95f), GameAssets.Rounded, true).rectTransform;
                var g = UIKit.Img(cell, "Glow", new Color(tint.r, tint.g, tint.b, 0.22f), GameAssets.Glow);
                g.rectTransform.Anchor(0, 0.2f, 1, 1);
                var kl = UIKit.Txt(cell, "Kind", Cosmetics.KindLabel(k), 16, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                kl.rectTransform.Anchor(0, 1, 1, 1, 4, -28, 4, 4);
                var art = UIKit.Rect("Art", cell).Anchor(0.08f, 0.22f, 0.92f, 0.86f);
                if (k == CosmeticKind.Dice) ThumbDice(art, diceTex, diceUv[dice.IndexOf(it)]);
                else IconArt(art, it, new Vector4(0, 0, 1, 1));
                var nm = UIKit.Txt(cell, "Name", it.name, 15, Color.white, TextAnchor.MiddleCenter);
                nm.rectTransform.Anchor(0, 0.03f, 1, 0.2f, 6, 0, 6, 0);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 11; nm.resizeTextMaxSize = 15;
                var edge = UIKit.Img(cell, "Edge", new Color(1f, 0.78f, 0.35f, 0.5f), GameAssets.RoundedOutline, true);
                edge.rectTransform.Fill();
                edge.raycastTarget = false;
                cell.HitArea();
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () => { Sfx.Play("click"); OpenAccessoryPicker(kind); };
            }
        }

        static void ThumbDice(RectTransform parent, Texture tex, Rect uv)
        {
            var raw = UIKit.Rect("Icon3D", parent).Anchor(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.uvRect = uv;
            raw.raycastTarget = false;
            var af = raw.gameObject.AddComponent<AspectRatioFitter>();
            af.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            af.aspectRatio = uv.width * tex.width / (uv.height * tex.height);
        }

        // ══════════════════════════ 선택 창 ══════════════════════════

        void BuildAccessoryPicker()
        {
            var bg = UIKit.Img(root, "AccessoryPicker", new Color(0, 0, 0, 0.9f));
            bg.raycastTarget = true;
            accRoot = bg.rectTransform;
            accRoot.Fill();
            OverlayCanvas(accRoot, 41);
            var panel = UIKit.Frame(accRoot, "Panel", new Color(0.04f, 0.03f, 0.08f, 0.98f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 800));
            var p = panel.transform;
            accTitle = UIKit.Txt(p, "Title", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            accTitle.rectTransform.Anchor(0, 1, 1, 1, 30, -62, 30, 18);
            accTitle.Section(UIKit.SecMe, 26);
            var close = UIKit.Button(p, "닫기", CloseAccessoryPicker, Dark, 20);
            close.rt.Place(new Vector2(1, 1), new Vector2(-20, -16), new Vector2(120, 46), new Vector2(1, 1));

            // 왼쪽: 미리보기
            var left = UIKit.Img(p, "Preview", new Color(0.02f, 0.02f, 0.05f, 0.85f), GameAssets.Rounded, true).rectTransform;
            left.Anchor(0.02f, 0.03f, 0.39f, 0.9f);
            accGlow = UIKit.Img(left, "Glow", new Color(1, 1, 1, 0.25f), GameAssets.Glow);
            accGlow.rectTransform.Anchor(0, 0.35f, 1, 0.98f);
            accPreview = UIKit.Rect("Art", left).Anchor(0.08f, 0.4f, 0.92f, 0.96f);
            accName = UIKit.Txt(left, "Name", "", 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            accName.rectTransform.Anchor(0, 0.31f, 1, 0.39f, 10, 0, 10, 0);
            accName.resizeTextForBestFit = true; accName.resizeTextMinSize = 16; accName.resizeTextMaxSize = 26;
            accDesc = UIKit.Txt(left, "Desc", "", 17, UIKit.Ink, TextAnchor.UpperCenter);
            accDesc.rectTransform.Anchor(0, 0.19f, 1, 0.31f, 20, 0, 20, 0);
            accState = UIKit.Txt(left, "State", "", 17, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleCenter);
            accState.rectTransform.Anchor(0, 0.135f, 1, 0.19f);
            accEquip = UIKit.Button(left, "장착", () => EquipFromPicker(accSel), new Color(0.2f, 0.45f, 0.55f, 0.97f), 21, Color.white);
            accEquip.rt.Anchor(0.05f, 0.025f, 0.95f, 0.125f);
            accShop = UIKit.Button(left, "", () => BuyFromPicker(accSel), new Color(0.55f, 0.34f, 0.12f, 0.97f), 19, Color.white);
            accShop.rt.Anchor(0.05f, 0.025f, 0.95f, 0.125f);

            // 오른쪽: 목록
            var right = UIKit.Img(p, "List", new Color(0.02f, 0.02f, 0.05f, 0.6f), GameAssets.Rounded, true).rectTransform;
            right.Anchor(0.405f, 0.03f, 0.98f, 0.9f);
            accGrid = UIKit.ScrollGrid(right, "Grid", new Vector2(160, 214), new Vector2(12, 14), out _);
            ((RectTransform)accGrid.parent).Anchor(0, 0, 1, 1, 6, 6, 6, 6);
            accRoot.gameObject.SetActive(false);
        }

        void OpenAccessoryPicker(CosmeticKind k)
        {
            if (accRoot == null) BuildAccessoryPicker();
            accKind = k;
            accSel = Cosmetics.Equipped(k);
            accRoot.SetAsLastSibling();
            accRoot.gameObject.SetActive(true);
            RefreshAccessoryPicker();
        }

        void CloseAccessoryPicker()
        {
            accRoot.gameObject.SetActive(false);
            RefreshAccessories();           // 다이스 아이콘 무대를 다시 쓰고, 미리보기 3D 는 꺼진다
        }

        void RefreshAccessoryPicker()
        {
            var items = Cosmetics.OfKind(accKind).ToList();
            int owned = items.Count(Cosmetics.Owned);
            accTitle.text = $"{Cosmetics.KindLabel(accKind)} 선택   <size=18><color=#bbb>보유 {owned} / {items.Count}</color></size>";
            UIKit.Clear(accGrid);
            Rect[] uv = null;
            Texture diceTex = accKind == CosmeticKind.Dice ? DiceStage.Icons(items, out uv) : null;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                bool has = Cosmetics.Owned(it), on = Cosmetics.IsEquipped(it);
                var cell = UIKit.Img(accGrid, "Item_" + it.id, new Color(0.08f, 0.07f, 0.12f, 0.95f), GameAssets.Rounded, true).rectTransform;
                var tint = it.Tint;
                var g = UIKit.Img(cell, "Glow", new Color(tint.r, tint.g, tint.b, it == accSel ? 0.4f : 0.12f), GameAssets.Glow);
                g.rectTransform.Anchor(0, 0.2f, 1, 1);
                var art = UIKit.Rect("Art", cell).Anchor(0.06f, 0.2f, 0.94f, 0.96f);
                if (diceTex != null) ThumbDice(art, diceTex, uv[i]); else IconArt(art, it, new Vector4(0, 0, 1, 1));
                var nm = UIKit.Txt(cell, "Name", it.name, 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                nm.rectTransform.Anchor(0, 0.08f, 1, 0.2f, 4, 0, 4, 0);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 11; nm.resizeTextMaxSize = 15;
                var st = UIKit.Txt(cell, "State", on ? "<color=#8ef>● 장착 중</color>" : has ? "<color=#9f9>보유</color>" : $"<color=#ffd77a>{it.price:N0} 오팔</color>", 14, UIKit.Ink, TextAnchor.MiddleCenter);
                st.rectTransform.Anchor(0, 0, 1, 0.09f);
                if (!has)
                {
                    var dim = UIKit.Img(cell, "Dim", new Color(0.02f, 0.02f, 0.04f, 0.86f), GameAssets.Rounded, true);   // 리니어 색공간이라 진하게
                    dim.rectTransform.Anchor(0, 0.2f, 1, 1);
                    dim.raycastTarget = false;
                }
                if (it == accSel)
                {
                    var hl = UIKit.Img(cell, "Sel", new Color(0.55f, 0.9f, 1f, 1f), GameAssets.RoundedOutline, true);
                    hl.rectTransform.Anchor(-0.02f, -0.015f, 1.02f, 1.015f);
                    hl.raycastTarget = false;
                }
                cell.HitArea();
                var ev = cell.gameObject.AddComponent<CellEvents>();
                var cur = it;
                ev.onLeftClick = () => { accSel = cur; Sfx.Play("click", 0.5f); RefreshAccessoryPicker(); };
                ev.onDoubleClick = () => { accSel = cur; if (Cosmetics.Owned(cur)) EquipFromPicker(cur); };
            }

            // 미리보기
            var c = accSel ?? Cosmetics.Equipped(accKind);
            UIKit.Clear(accPreview);
            if (c.kind == CosmeticKind.Coin || c.kind == CosmeticKind.Dice)
            {
                var raw = UIKit.Rect("Live3D", accPreview).Anchor(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
                raw.texture = DiceStage.Live(c);
                raw.raycastTarget = false;
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = 1;
            }
            else IconArt(accPreview, c, new Vector4(0, 0, 1, 1));
            var tc = c.Tint;
            accGlow.color = new Color(tc.r, tc.g, tc.b, 0.3f);
            accName.text = c.name;
            accDesc.text = c.desc;
            bool owns = Cosmetics.Owned(c), equipped = Cosmetics.IsEquipped(c);
            accState.text = (c.element == Element.None ? "기본" : CardData.ElementName(c.element) + " 테마") + "  ·  " + (equipped ? "장착 중" : owns ? "보유 중" : $"미보유 · {c.price:N0} 오팔");
            accEquip.bg.gameObject.SetActive(owns);
            accShop.bg.gameObject.SetActive(!owns);
            accEquip.label.text = equipped ? "장착 중" : "장착";
            accEquip.Interactable = owns && !equipped;
            accShop.label.text = $"루콘의 공방에서 구매 ▶  <size=15>{c.price:N0} 오팔</size>";
        }

        void EquipFromPicker(CosmeticItem c)
        {
            if (c == null || !Cosmetics.Equip(c)) return;
            Sfx.Play("reveal1", 0.6f);
            Toast($"「{c.name}」 장착!");
            RefreshAccessoryPicker();
            RefreshHome();
        }

        /// <summary>미보유: 상점 › 루콘의 공방으로 데려가 그 아이템을 골라 둔다</summary>
        void BuyFromPicker(CosmeticItem c)
        {
            if (c == null) return;
            accRoot.gameObject.SetActive(false);
            Show(Tab.Shop);
            cosKind = c.kind;
            cosSel = c;
            GoSection(SecLukon);
        }

        // ── 테스트 ──
        public void TestOpenAccessory(CosmeticKind k, string id = null)
        {
            OpenAccessoryPicker(k);
            if (id != null) { accSel = Cosmetics.Get(id); RefreshAccessoryPicker(); }
        }
    }
}
