using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 상점: 마스코트 '로지'의 카드 상점.
    /// 진열장의 부스터 박스(팩마다 1개) · 오늘의 특가(낱장, Common/Uncommon/Rare).
    /// 가공(마력부여)은 가운데 방의 로지 부모님(로한·벨라)이 맡는다 — 화면은 여기(enchantView)에 있다.
    /// </summary>
    public partial class MainMenu
    {
        RectTransform shopView, enchantView;

        // ── 로지 ──
        RectTransform rosie;
        Image heldPack;
        Text rosieLine;
        Coroutine rosieCo;
        float rosieHop = -10f;

        static readonly string[] RosieGreet =
        {
            "어서 와! 로지의 카드 상점에 온 걸 환영해~",
            "오늘의 특가 카드 봤어? 하루에 한 번씩 바뀌니까 놓치지 마!",
            "새 부스터팩이 들어왔어! 좋은 카드가 나오길 같이 빌어 줄게~",
            "가공 카드를 원하면 가운데 방 엄마 아빠한테 가 봐! 마력부여사거든~",
            "슬리브랑 주사위는 뒤편 할아버지 공방에서 팔아. 루콘 할아버지 안경은… 놀라지 마!",
            "왔구나! 오늘은 어떤 카드를 찾고 있어?",
        };
        static readonly string[] RosiePoke =
        {
            "간지러~",
            "이 마법사 모자 어때?",
            "카드는 언제나 정성껏 관리하고 있어. 걱정 마!",
            "특가 카드는 CP 로 살 수 있어. 생성 비용의 딱 절반이야!",
            "남는 카드 77장을 모아 가면 아빠가 더 좋은 카드로 바꿔 줘!",
            "할아버지 눈? 나도 딱 한 번 봤어. 그건 비밀이야~",
        };

        // ── 진열장 / 특가 ──
        Text shopOwned, specialTimer, specialCp;
        readonly List<(BoosterPack pack, UIKit.Btn b1, UIKit.Btn b10, RectTransform box)> packSlots = new List<(BoosterPack, UIKit.Btn, UIKit.Btn, RectTransform)>();
        RectTransform specialRow;

        // ── 마력부여기 ──
        RectTransform enchantGrid, enchantCardHolder;
        CardFace enchantFace;
        Text enchantInfo, enchantCp;
        UIKit.Btn enchantSeven, enchantStrikerZ;
        Image enchantFlash, enchantGlow;
        CardData enchantSel;
        Rarity? enchantFilter;
        readonly Dictionary<string, UIKit.Btn> enchantFilterBtns = new Dictionary<string, UIKit.Btn>();

        static readonly Color Glass = new Color(0.04f, 0.025f, 0.06f, 0.82f);

        // ══════════════════════════ 로지의 상점 ══════════════════════════

        void BuildShopView(RectTransform page)
        {
            shopView = UIKit.Rect("ShopView", page).Fill();
            var frame = UIKit.Frame(shopView, "Scene", new Color(0.02f, 0.01f, 0.03f, 1f));
            frame.rectTransform.Fill();
            var mask = UIKit.Img(frame.transform, "Mask", Color.white, GameAssets.Rounded, true);
            mask.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var bg = UIKit.Img(mask.transform, "Bg", new Color(0.92f, 0.9f, 0.9f), GameAssets.TexSprite("shop_bg"));
            bg.rectTransform.Fill();
            var vignette = UIKit.Img(mask.transform, "Shade", new Color(0.02f, 0.01f, 0.05f, 0.18f));
            vignette.rectTransform.Fill();
            frame.transform.GetChild(0).SetAsLastSibling();          // 금테를 맨 위로

            // 로지 (카운터 뒤 왼쪽). 클릭하면 대사 + 깡총
            rosie = UIKit.Rect("RosieArea", shopView).Anchor(-0.02f, 0.0f, 0.34f, 1.0f, 0, 3, 0, 0);
            rosie.pivot = new Vector2(0.5f, 0);
            // 이미지 비율 그대로 맞춘 상자: 손에 든 팩을 이미지 좌표(0~1)로 배치하기 위함
            var fitRt = UIKit.Rect("Fit", rosie);
            fitRt.pivot = new Vector2(0.5f, 0);
            var fit = fitRt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var rosieSprite = GameAssets.TexSprite("shop_rosie");
            fit.aspectRatio = rosieSprite != null ? rosieSprite.rect.width / rosieSprite.rect.height : 0.63f;
            // 손에 든 팩 = 최신 팩. 로지 이미지에서 원래 팩 자리를 비워 두었고(손가락은 남김) 그 뒤에 깔아 손가락이 팩을 쥔 모양이 된다
            heldPack = UIKit.Img(fitRt, "HeldPack", Color.white, GameAssets.TexSprite(BoosterPack.Latest.art));
            heldPack.rectTransform.Anchor(0.1055f, 0.6297f, 0.2171f, 0.7595f);
            heldPack.rectTransform.localRotation = Quaternion.Euler(0, 0, 27.9f);
            var rosieImg = UIKit.Img(fitRt, "Rosie", Color.white, rosieSprite);
            rosieImg.rectTransform.Fill();
            rosieImg.raycastTarget = true;
            var rb = rosieImg.gameObject.AddComponent<Button>();
            rb.transition = Selectable.Transition.None;
            rb.onClick.AddListener(() => { rosieHop = Time.unscaledTime; RosieSay(RosiePoke[rng.Next(RosiePoke.Length)]); });

            // 대화창 (로지 아래)
            var box = UIKit.Frame(shopView, "Dialog", new Color(0.06f, 0.04f, 0.1f, 0.92f));
            box.rectTransform.Anchor(0.012f, 0.018f, 0.33f, 0.2f);
            var tag = UIKit.Img(box.transform, "NameTag", new Color(0.55f, 0.3f, 0.75f, 1f), GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(18, 18), new Vector2(120, 38), new Vector2(0, 1));
            var tn = UIKit.Txt(tag.transform, "N", "로지", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            tn.rectTransform.Fill();
            rosieLine = UIKit.Txt(box.transform, "Line", "", 23, UIKit.Ink, TextAnchor.UpperLeft);
            rosieLine.rectTransform.Anchor(0, 0, 1, 1, 22, 12, 18, 28);
            rosieLine.lineSpacing = 1.1f;
            // 꾸미기 상점 입구 (대화창 오른쪽 위 꼭지)
            // 루콘(할아버지)의 꾸미기 공방으로 (커튼 전환)
            var cos = UIKit.Button(box.transform, "✦ 악세서리 상점 ▶", () => GoSection(SecLukon), new Color(0.5f, 0.32f, 0.12f, 0.97f), 19, Color.white);
            cos.rt.Place(new Vector2(1, 1), new Vector2(-14, 18), new Vector2(196, 40), new Vector2(1, 1));

            // 진열장: 부스터 박스 (팩마다 한 칸)
            var shelf = UIKit.Frame(shopView, "Shelf", Glass);
            shelf.rectTransform.Anchor(0.345f, 0.515f, 0.968f, 0.985f);
            HelpIcon(shelf.rectTransform, RosieHelp).Place(new Vector2(1, 1), new Vector2(-12, -8), new Vector2(40, 40), new Vector2(1, 1));
            var st = UIKit.Txt(shelf.transform, "T", "부스터팩 진열장", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            st.rectTransform.Anchor(0, 1, 1, 1, 22, -50, 20, 12);
            st.Section(UIKit.SecCollect, 22);
            shopOwned = UIKit.Txt(shelf.transform, "Owned", "", 19, UIKit.Muted, TextAnchor.MiddleRight);
            shopOwned.rectTransform.Anchor(0, 1, 1, 1, 22, -52, 66, 8);
            var view = UIKit.Rect("Packs", shelf.transform).Anchor(0, 0, 1, 1, 10, 10, 10, 56);
            view.HitArea();
            view.gameObject.AddComponent<RectMask2D>();
            var content = UIKit.Rect("Content", view);
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14; hl.childForceExpandHeight = true; hl.childForceExpandWidth = false; hl.childControlWidth = false; hl.childControlHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.viewport = view; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            packSlots.Clear();
            float slotW = BoosterPack.Packs.Count == 1 ? 1030 : 460;
            foreach (var p in BoosterPack.Packs) BuildPackSlot(content, p, slotW);

            // 오늘의 특가 (낱장)
            var sp = UIKit.Frame(shopView, "Specials", Glass);
            sp.rectTransform.Anchor(0.345f, 0.015f, 0.968f, 0.5f);
            var spt = UIKit.Txt(sp.transform, "T", "오늘의 특가 <size=18><color=#bbb>낱장 · CP 생성 비용의 절반 · 가공비도 절반</color></size>", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            spt.rectTransform.Anchor(0, 1, 1, 1, 22, -48, 20, 12);
            spt.Section(UIKit.SecCollect, 22);
            specialTimer = UIKit.Txt(sp.transform, "Timer", "", 19, new Color(1f, 0.75f, 0.5f), TextAnchor.MiddleRight);
            specialTimer.rectTransform.Anchor(0, 1, 1, 1, 22, -50, 22, 8);
            specialCp = UIKit.Txt(sp.transform, "Cp", "", 18, UIKit.Ink, TextAnchor.MiddleLeft);
            specialCp.rectTransform.Anchor(0, 1, 1, 1, 22, -82, 22, 50);
            specialRow = UIKit.Rect("Row", sp.transform).Anchor(0, 0, 1, 1, 14, 10, 14, 86);
            var row = specialRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12; row.childForceExpandWidth = true; row.childForceExpandHeight = true; row.childControlWidth = true; row.childControlHeight = true;
        }

        void BuildPackSlot(RectTransform parent, BoosterPack p, float w)
        {
            var slot = UIKit.Img(parent, "Pack_" + p.id, new Color(0.08f, 0.05f, 0.1f, 0.6f), GameAssets.Rounded, true).rectTransform;
            slot.sizeDelta = new Vector2(w, 0);
            bool wide = w > 600;
            var glow = UIKit.Img(slot, "Glow", new Color(1f, 0.78f, 0.35f, 0.3f), GameAssets.Glow);
            glow.rectTransform.Anchor(wide ? 0.0f : 0.1f, wide ? 0.05f : 0.32f, wide ? 0.46f : 0.9f, 0.98f);
            var boxImg = UIKit.Img(slot, "Box", Color.white, GameAssets.TexSprite(p.boxArt) ?? GameAssets.TexSprite(p.art) ?? GameAssets.TexSprite("card_back"));
            boxImg.preserveAspect = true;
            var box = boxImg.rectTransform;
            box.Anchor(wide ? 0.02f : 0.12f, wide ? 0.04f : 0.3f, wide ? 0.44f : 0.88f, 0.98f);
            box.pivot = new Vector2(0.5f, 0.05f);
            if (wide)
            {
                var code = UIKit.Txt(slot, "Code", p.code, 64, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
                code.rectTransform.Anchor(0.46f, 0.76f, 1, 0.98f, 0, 0, 14, 0);
                code.Outline(new Color(0.25f, 0.08f, 0, 0.9f), 2);
                var nm = UIKit.Txt(slot, "Name", p.name, 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                nm.rectTransform.Anchor(0.46f, 0.67f, 1, 0.77f, 0, 0, 14, 0);
                var desc = UIKit.Txt(slot, "Desc", $"1팩 {p.cardsPerPack}장 · 카드마다 등급 확률 적용\n가공 확률  {BoosterPack.FinishRateText}", 17, UIKit.Ink, TextAnchor.UpperLeft);
                desc.rectTransform.Anchor(0.46f, 0.47f, 1, 0.67f, 0, 0, 14, 0);
                var rate = UIKit.Txt(slot, "Rate", p.RateText(), 15, new Color(1f, 0.8f, 0.55f), TextAnchor.UpperLeft);
                rate.rectTransform.Anchor(0.46f, 0.33f, 1, 0.47f, 0, 0, 14, 0);
                rate.verticalOverflow = VerticalWrapMode.Overflow;     // 확률 표기는 잘리면 안 된다
                var odds = UIKit.Button(slot, "확률 정보", () => ShowOdds(p), Dark, 16);
                odds.rt.Anchor(0.80f, 0.86f, 0.985f, 0.95f);
                Compact(odds);
            }
            else
            {
                var nm = UIKit.Txt(slot, "Name", $"{p.code}  <size=16>{p.name}</size>", 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                nm.rectTransform.Anchor(0, 0.19f, 1, 0.3f);
            }
            var b1 = UIKit.Button(slot, $"1팩\n{p.price} 오팔", () => BuyPack(p, 1), Red, 22, Color.white);
            var b10 = UIKit.Button(slot, $"10팩\n{p.price * 10:N0} 오팔", () => BuyPack(p, 10), UIKit.Accent, 22, Color.white);
            if (wide) { b1.rt.Anchor(0.46f, 0.04f, 0.72f, 0.3f); b10.rt.Anchor(0.73f, 0.04f, 0.985f, 0.3f); }
            else { b1.rt.Anchor(0.05f, 0.03f, 0.49f, 0.18f); b10.rt.Anchor(0.51f, 0.03f, 0.95f, 0.18f); }
            packSlots.Add((p, b1, b10, box));
        }

        // ══════════════════════════ 확률 정보 ══════════════════════════
        // 표의 숫자는 모두 BoosterPack(실제 뽑기와 같은 데이터)에서 계산한다

        RectTransform oddsRoot;
        Text oddsTitle, oddsNote;
        RectTransform oddsTable;

        void ShowOdds(BoosterPack p)
        {
            if (oddsRoot == null)
            {
                var bg = UIKit.Img(root, "Odds", new Color(0, 0, 0, 0.85f));
                bg.raycastTarget = true;
                oddsRoot = bg.rectTransform;
                oddsRoot.Fill();
                OverlayCanvas(oddsRoot, 41);
                var panel = UIKit.Frame(oddsRoot, "Panel", new Color(0.05f, 0.03f, 0.1f, 0.98f));
                panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, 640));
                oddsTitle = UIKit.Txt(panel.transform, "Title", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                oddsTitle.rectTransform.Anchor(0, 1, 1, 1, 34, -72, 34, 22);
                oddsTitle.Section(UIKit.SecCollect, 26);
                oddsTable = UIKit.Rect("Table", panel.transform).Anchor(0, 1, 1, 1, 34, -380, 34, 92);
                oddsNote = UIKit.Txt(panel.transform, "Note", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
                oddsNote.rectTransform.Anchor(0, 0, 1, 1, 34, 100, 34, 396);
                oddsNote.lineSpacing = 1.2f;
                UIKit.Button(panel.transform, "닫기", () => oddsRoot.gameObject.SetActive(false), Dark, 22).rt.Anchor(0.5f, 0, 0.5f, 0, -120, 24, -120, -80);
            }
            oddsTitle.text = $"확률 정보  ·  {p.code} {p.name}";
            UIKit.Clear(oddsTable);
            var cols = new[] { ("등급", 0f, 0.16f), ("등급 확률", 0.16f, 0.33f), ("수록 카드", 0.33f, 0.48f), ("카드 1종 (1장 기준)", 0.48f, 0.74f), ($"카드 1종 (1팩 {p.cardsPerPack}장 중 1장 이상)", 0.74f, 1f) };
            var rows = p.Breakdown();
            const float rowH = 46f;
            void Cell(int row, int col, string text, Color c, FontStyle st = FontStyle.Normal)
            {
                var t = UIKit.Txt(oddsTable, $"C{row}_{col}", text, row == 0 ? 17 : 20, c, TextAnchor.MiddleCenter, st);
                t.rectTransform.Anchor(cols[col].Item2, 1, cols[col].Item3, 1, 0, -(row + 1) * rowH, 0, row * rowH);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var head = UIKit.Img(oddsTable, "Head", new Color(0.18f, 0.13f, 0.05f, 0.95f), GameAssets.Rounded, true);
            head.rectTransform.Anchor(0, 1, 1, 1, 0, -rowH, 0, 0);
            for (int c = 0; c < cols.Length; c++) Cell(0, c, cols[c].Item1, UIKit.Gold, FontStyle.Bold);
            for (int i = 0; i < rows.Count; i++)
            {
                var (r, chance, kinds, perCard, perPack) = rows[i];
                if (i % 2 == 0)
                {
                    var stripe = UIKit.Img(oddsTable, "Stripe", new Color(1, 1, 1, 0.04f));
                    stripe.rectTransform.Anchor(0, 1, 1, 1, 0, -(i + 2) * rowH, 0, (i + 1) * rowH);
                }
                Cell(i + 1, 0, CardData.RarityLabel(r), CardData.RarityColor(r), FontStyle.Bold);
                Cell(i + 1, 1, BoosterPack.Pct(chance), Color.white);
                Cell(i + 1, 2, $"{kinds}종", UIKit.Ink);
                Cell(i + 1, 3, BoosterPack.Pct(perCard), UIKit.Ink);
                Cell(i + 1, 4, BoosterPack.Pct(perPack), UIKit.Ink);
            }
            double normal = 1 - BoosterPack.SevenChance - BoosterPack.StrikerZChance;
            oddsNote.text =
                $"<b>등급</b>  {p.RateText()}  (1팩 {p.cardsPerPack}장, 장마다 따로 판정)\n" +
                $"<b>가공</b>  {BoosterPack.FinishRateText} · 일반 {BoosterPack.Pct(normal)}  (등급과 별개로 모든 카드에 따로 판정)\n" +
                "<color=#a9a3b8>· 같은 등급 안의 카드는 모두 같은 확률로 나옵니다.\n" +
                "· 확률은 팩을 열 때마다 독립적으로 적용되며, 천장(일정 횟수 보장)은 없습니다.\n" +
                "· 특정 카드·가공이 함께 나올 확률 = 카드 확률 × 가공 확률 (예: Unique 카드 1종의 StrikerZ = " +
                $"{BoosterPack.Pct((rows.FirstOrDefault(x => x.rarity == Rarity.Unique).perCard) * BoosterPack.StrikerZChance)})</color>";
            oddsRoot.SetAsLastSibling();
            oddsRoot.gameObject.SetActive(true);
        }

        void BuyPack(BoosterPack p, int n)
        {
            if (Profile.Data.opal < p.price * n) { RosieSay("앗… 오팔이 조금 모자란 것 같아. 퀘스트를 깨고 다시 와 줘!"); return; }
            RosieSay(n >= 10 ? "10팩이나?! 좋아, 레어 카드가 쏟아지길 기도할게!" : "두근두근… 좋은 카드가 나오길!");
            Buy(p, n);
        }

        void RefreshShop()
        {
            shopCam.gameObject.SetActive(true);
            shopView.gameObject.SetActive(true);
            enchantView.gameObject.SetActive(false);
            upgradeView.gameObject.SetActive(false);
            if (!shopMoving && !doorPlaying) RefreshNav();
            heldPack.sprite = GameAssets.TexSprite(BoosterPack.Latest.art) ?? heldPack.sprite;
            shopOwned.text = $"보유 오팔 {Profile.Data.opal:N0}   ·   도감 {Profile.OwnedKinds}/{CardDB.All.Count}";
            foreach (var (p, b1, b10, _) in packSlots)
            {
                b1.Interactable = Profile.Data.opal >= p.price;
                b10.Interactable = Profile.Data.opal >= p.price * 10;
            }
            RefreshSpecials();
            if (shopSection == SecRosie && (rosieLine.text.Length == 0 || !rosieGreeted)) { rosieGreeted = true; RosieSay(RosieGreet[rng.Next(RosieGreet.Length)]); }
        }

        bool rosieGreeted;

        void RefreshSpecials()
        {
            UIKit.Clear(specialRow);
            specialCp.text = "보유 CP   " + Crafting.CpText();
            foreach (var o in ShopDaily.Offers)
            {
                var c = CardDB.Get(o.id);
                if (c == null) continue;
                var offer = o;
                var slot = UIKit.Img(specialRow, "Offer_" + o.rarity, new Color(0.08f, 0.05f, 0.1f, 0.7f), GameAssets.Rounded, true).rectTransform;
                var rc = CardData.RarityColor(o.rarity);
                var head = UIKit.Txt(slot, "Rarity", CardData.RarityLabel(o.rarity), 22, rc, TextAnchor.MiddleLeft, FontStyle.Bold);
                head.rectTransform.Anchor(0, 1, 1, 1, 12, -34, 8, 4);
                if (o.finish != CardFinish.Normal)
                {
                    var ft = UIKit.Txt(slot, "Finish", o.finish == CardFinish.StrikerZ ? "★ STRIKERZ" : "◆ SEVEN", 17, FinishStyle.Base(o.finish), TextAnchor.MiddleRight, FontStyle.Bold);
                    ft.rectTransform.Anchor(0, 1, 1, 1, 8, -34, 12, 4);
                }
                // 카드
                var holder = UIKit.Rect("Card", slot);
                holder.Place(new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(140, 196), new Vector2(0.5f, 1));
                var glow = UIKit.Img(holder, "Glow", new Color(rc.r, rc.g, rc.b, 0.4f), GameAssets.Glow);
                glow.rectTransform.Anchor(-0.25f, -0.18f, 1.25f, 1.18f);
                var face = CardFace.Build(holder, 140, 196, 0.19f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(c);
                face.SetFinish(o.finish);
                holder.HitArea();
                var ev = holder.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () =>
                {
                    RosieSay($"{c.name}! {(o.finish != CardFinish.Normal ? "게다가 반짝이는 가공 카드야. " : "")}지금 {CardData.RarityLabel(o.rarity)} CP {ShopDaily.Price(o):N0} 이면 가져갈 수 있어~");
                    CardZoom.Show(c, o.finish);
                };
                ev.onRightClick = () => CardZoom.Show(c, o.finish);
                var nm = UIKit.Txt(slot, "Name", c.name, 17, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                nm.rectTransform.Anchor(0, 0, 1, 0, 6, 88, 6, -116);
                // 가격
                int price = ShopDaily.Price(o);
                string priceTxt = $"<color=#{CardData.RarityHex(o.rarity)}>{CardData.RarityLabel(o.rarity)} CP</color> {price:N0}  <size=13>" +
                    (o.finish == CardFinish.Normal ? $"<color=#888>(생성 {Crafting.CraftCost})</color>" : $"<color=#999>({ShopDaily.CardPrice}+가공 {ShopDaily.FinishPrice(o.finish):N0})</color>") + "</size>";
                var pt = UIKit.Txt(slot, "Price", priceTxt, 18, Color.white, TextAnchor.MiddleCenter);
                pt.horizontalOverflow = HorizontalWrapMode.Overflow;
                pt.rectTransform.Anchor(0, 0, 1, 0, 6, 58, 6, -86);
                bool can = ShopDaily.CanBuy(o, out _);
                var btn = UIKit.Button(slot, o.sold ? "품절" : "구매", () => BuySpecial(offer), o.sold ? Dark : new Color(0.2f, 0.4f, 0.15f, 0.95f), 20, Color.white);
                btn.rt.Anchor(0.12f, 0, 0.88f, 0, 0, 8, 0, -52);
                btn.Interactable = can;
                if (o.sold)
                {
                    var dim = UIKit.Img(holder, "Sold", new Color(0, 0, 0, 0.55f), GameAssets.Rounded, true);
                    dim.rectTransform.Fill();
                    var stamp = UIKit.Txt(holder, "Stamp", "SOLD OUT", 30, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    stamp.rectTransform.Anchor(-0.2f, 0.35f, 1.2f, 0.65f);
                    stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
                    stamp.Outline(new Color(0.2f, 0, 0, 0.9f), 2);
                }
            }
        }

        void BuySpecial(SpecialOffer o)
        {
            var c = CardDB.Get(o.id);
            if (!ShopDaily.Buy(o, out string why))
            {
                RosieSay(o.sold ? "그건 오늘 이미 팔렸어. 내일 새 카드를 들여올게!" : "앗… " + why + ". 카드를 분해하면 CP 를 모을 수 있어!");
                return;
            }
            Sfx.Play(o.finish == CardFinish.StrikerZ ? "strikerz" : o.finish == CardFinish.Seven ? "seven" : "reveal1");
            RosieSay(o.finish != CardFinish.Normal ? $"와, 반짝반짝한 {(o.finish == CardFinish.StrikerZ ? "StrikerZ" : "Seven")} 가공 {c.name}! 오늘 운이 좋은걸?" : $"{c.name}, 탁월한 선택이야! 소중히 다뤄 줘~");
            Toast($"{c.name}{(o.finish != CardFinish.Normal ? $" ({FinishStyle.Label(o.finish)})" : "")} 을(를) 구매했습니다");
            RefreshTop();
            RefreshShop();
        }

        void RosieSay(string line)
        {
            if (rosieLine == null) return;
            if (rosieCo != null) StopCoroutine(rosieCo);
            Voice.Line("rosie", line);
            rosieCo = StartCoroutine(Typewriter(line));
        }

        IEnumerator Typewriter(string line)
        {
            for (int i = 1; i <= line.Length; i++)
            {
                rosieLine.text = line.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.022f);
            }
        }

        void Update()
        {
            if (shopView != null && shopView.gameObject.activeInHierarchy)
            {
                float t = Time.unscaledTime;
                foreach (var s in packSlots) s.box.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * 1.2f);
                // 로지: 숨쉬기 + 클릭하면 깡총
                float hop = t - rosieHop;
                float jump = hop < 0.45f ? Mathf.Sin(hop / 0.45f * Mathf.PI) * 26f : 0f;
                rosie.anchoredPosition = new Vector2(0, jump);
                rosie.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2.1f) * 0.008f, 1f);
                if (specialTimer != null && Time.frameCount % 20 == 0)
                {
                    var left = ShopDaily.UntilReset;
                    specialTimer.text = "새 특가까지 " + DailyClock.Countdown(left);
                    if (left.TotalSeconds < 1.5) RefreshSpecials();
                }
            }
            UpdateLukon();
            UpdateShopHall();
            if (enchantView != null && enchantView.gameObject.activeInHierarchy && enchantGlow != null)
            {
                float t = Time.unscaledTime;
                enchantGlow.color = new Color(0.75f, 0.5f, 1f, 0.35f + Mathf.Sin(t * 2.4f) * 0.12f);
                if (enchantCardHolder != null) enchantCardHolder.anchoredPosition = new Vector2(0, Mathf.Sin(t * 1.6f) * 8f);
            }
        }

        // ══════════════════════════ 마력부여기 ══════════════════════════

        void BuildEnchantView(RectTransform page)
        {
            enchantView = UIKit.Rect("EnchantView", page).Fill();
            var left = UIKit.Frame(enchantView, "Machine", new Color(0.05f, 0.025f, 0.09f, 0.94f));
            left.rectTransform.Anchor(0, 0, 0.4f, 1, 0, 0, 8, 0);
            var back = UIKit.Button(left.transform, "◀ 마력부여실", () => { RefreshShop(); ParentsSay((true, "마음에 드는 가공이 됐길 바란다!")); }, Dark, 20);
            back.rt.Place(new Vector2(0, 1), new Vector2(16, -16), new Vector2(200, 52), new Vector2(0, 1));
            var title = UIKit.Txt(left.transform, "T", "마력부여실", 30, new Color(0.85f, 0.75f, 1f), TextAnchor.UpperRight, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 20, -64, 22, 16);
            enchantGlow = UIKit.Img(left.transform, "Glow", new Color(0.75f, 0.5f, 1f, 0.35f), GameAssets.Glow);
            enchantGlow.rectTransform.Anchor(0.0f, 0.3f, 1f, 0.95f);
            var machine = UIKit.Img(left.transform, "Img", Color.white, GameAssets.TexSprite("enchanter"));
            machine.preserveAspect = true;
            machine.rectTransform.Anchor(0.04f, 0.3f, 0.96f, 0.9f);
            // 고리 안에 떠 있는 선택 카드
            var anchor = UIKit.Rect("CardAnchor", left.transform);
            anchor.Place(new Vector2(0.5f, 0.775f), Vector2.zero, new Vector2(150, 210));
            enchantCardHolder = UIKit.Rect("Card", anchor);
            enchantCardHolder.sizeDelta = new Vector2(150, 210);
            enchantFace = CardFace.Build(enchantCardHolder, 150, 210, 0.2f);
            enchantFace.root.Fill();
            enchantFace.glow.gameObject.SetActive(false);
            // 고리 위 카드 클릭 = 확대
            CardZoom.Bind(enchantCardHolder, () => enchantSel, () => EnchantShownFinish(enchantSel));
            enchantInfo = UIKit.Txt(left.transform, "Info", "", 19, UIKit.Ink, TextAnchor.UpperCenter);
            enchantInfo.rectTransform.Anchor(0, 0.15f, 1, 0.31f, 18, 0, 18, 0);
            enchantInfo.lineSpacing = 1.15f;
            enchantSeven = UIKit.Button(left.transform, "", () => DoEnchant(CardFinish.Seven), new Color(0.32f, 0.36f, 0.45f, 0.97f), 20, Color.white);
            enchantSeven.rt.Anchor(0.04f, 0.025f, 0.49f, 0.14f);
            enchantStrikerZ = UIKit.Button(left.transform, "", () => DoEnchant(CardFinish.StrikerZ), new Color(0.6f, 0.42f, 0.08f, 0.97f), 20, Color.white);
            enchantStrikerZ.rt.Anchor(0.51f, 0.025f, 0.96f, 0.14f);
            enchantFlash = UIKit.Img(left.transform, "Flash", new Color(1, 1, 1, 0), GameAssets.Rounded, true);
            enchantFlash.rectTransform.Fill();
            enchantFlash.raycastTarget = false;

            var right = UIKit.Frame(enchantView, "Collection", Dark);
            right.rectTransform.Anchor(0.4f, 0, 1, 1, 8, 0, 0, 0);
            var rt = UIKit.Txt(right.transform, "T", "보유 카드에서 가공할 카드를 고르세요", 24, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            rt.rectTransform.Anchor(0, 1, 1, 1, 22, -54, 20, 10);
            enchantCp = UIKit.Txt(right.transform, "Cp", "", 18, UIKit.Ink, TextAnchor.MiddleLeft);
            enchantCp.rectTransform.Anchor(0, 1, 1, 1, 22, -88, 20, 54);
            var filters = new (string key, string label, Rarity? r)[] { ("all", "전체", null), ("Common", "Common", Rarity.Common), ("Uncommon", "Uncommon", Rarity.Uncommon), ("Rare", "Rare", Rarity.Rare), ("Unique", "Unique", Rarity.Unique) };
            for (int i = 0; i < filters.Length; i++)
            {
                var f = filters[i];
                var b = UIKit.Button(right.transform, f.label, () => { enchantFilter = f.r; RefreshEnchantGrid(); }, Dark, 18);
                b.rt.Place(new Vector2(0, 1), new Vector2(22 + i * 128, -96), new Vector2(120, 40), new Vector2(0, 1));
                enchantFilterBtns[f.key] = b;
            }
            enchantGrid = UIKit.ScrollGrid(right.transform, "Grid", new Vector2(112, 157), new Vector2(12, 14), out _);
            enchantGrid.parent.GetComponent<RectTransform>().Anchor(0, 0, 1, 1, 12, 12, 12, 146);
            enchantView.gameObject.SetActive(false);
        }

        void OpenEnchanter(CardData sel)
        {
            shopCam.gameObject.SetActive(false);
            navL.gameObject.SetActive(false);
            navR.gameObject.SetActive(false);
            upgradeView.gameObject.SetActive(false);
            enchantView.gameObject.SetActive(true);
            enchantSel = sel ?? (enchantSel != null && Profile.Owned(enchantSel.id) > 0 ? enchantSel : null);
            RefreshEnchantGrid();
            RefreshEnchantPanel();
        }

        IEnumerable<CardData> EnchantCandidates()
            => CardDB.All.Where(c => c.type != CardType.Hero && Profile.Owned(c.id) > 0 && (enchantFilter == null || c.rarity == enchantFilter))
                         .OrderByDescending(c => c.rarity).ThenBy(c => c.id);

        void RefreshEnchantGrid()
        {
            foreach (var kv in enchantFilterBtns)
            {
                bool on = kv.Key == (enchantFilter == null ? "all" : enchantFilter.ToString());
                kv.Value.SetSelected(on, new Color(0.45f, 0.25f, 0.65f, 0.97f));
            }
            enchantCp.text = "보유 CP   " + Crafting.CpText();
            UIKit.Clear(enchantGrid);
            foreach (var c in EnchantCandidates())
            {
                var cd = c;
                // 클릭 = 가공할 카드로 선택 + 확대 (우클릭 = 확대만)
                var cell = CardCell(enchantGrid, c, 112, 157, 0.15f, () =>
                {
                    enchantSel = cd;
                    RefreshEnchantPanel();
                    CardZoom.Show(cd, Profile.BestFinish(cd.id));
                }, null, () => CardZoom.Show(cd, Profile.BestFinish(cd.id)));
                int s = Profile.FinishCount(c.id, CardFinish.Seven), z = Profile.FinishCount(c.id, CardFinish.StrikerZ), n = Profile.FinishCount(c.id, CardFinish.Normal);
                Badge(cell, $"{n}{(s > 0 ? " " + FinishStyle.Rich(CardFinish.Seven, s.ToString()) : "")}{(z > 0 ? " " + FinishStyle.Rich(CardFinish.StrikerZ, z.ToString()) : "")}", new Color(0, 0, 0, 0.75f), new Vector4(0.08f, 0.0f, 0.92f, 0.13f), 16);
                if (c == enchantSel)
                {
                    var hl = UIKit.Img(cell, "Sel", new Color(0.8f, 0.6f, 1f, 1f), GameAssets.RoundedOutline, true);
                    hl.rectTransform.Anchor(-0.04f, -0.03f, 1.04f, 1.03f);
                }
            }
        }

        void RefreshEnchantPanel()
        {
            var c = enchantSel;
            enchantCardHolder.gameObject.SetActive(c != null);
            if (c == null)
            {
                enchantInfo.text = "오른쪽에서 카드를 고르면\n고리 위에 카드가 올라갑니다";
                enchantSeven.label.text = $"◆ Seven 가공\n<size=16>CP {ShopDaily.SevenEnchantCost:N0}</size>";
                enchantStrikerZ.label.text = $"★ StrikerZ 가공\n<size=16>CP {ShopDaily.StrikerZEnchantCost:N0}</size>";
                enchantSeven.Interactable = enchantStrikerZ.Interactable = false;
                return;
            }
            enchantFace.Set(c);
            var src = ShopDaily.EnchantSource(c, CardFinish.StrikerZ);
            enchantFace.SetFinish(EnchantShownFinish(c));
            string rc = CardData.RarityHex(c.rarity);
            enchantInfo.text = $"<b>{c.name}</b>  {CardData.RarityRich(c.rarity)}\n" +
                               $"일반 {Profile.FinishCount(c.id, CardFinish.Normal)} · {FinishStyle.Rich(CardFinish.Seven, $" Seven {Profile.FinishCount(c.id, CardFinish.Seven)}")} · {FinishStyle.Rich(CardFinish.StrikerZ, $" StrikerZ {Profile.FinishCount(c.id, CardFinish.StrikerZ)}")}\n" +
                               $"<color=#{rc}>{CardData.RarityLabel(c.rarity)} CP</color> {Crafting.CP(c.rarity):N0}" +
                               (src == CardFinish.Seven ? "\n<size=15><color=#bbb>일반 사본이 없어 Seven 사본을 StrikerZ 로 올립니다</color></size>" : "");
            enchantSeven.label.text = $"◆ Seven 가공\n<size=16>{CardData.RarityLabel(c.rarity)} CP {ShopDaily.SevenEnchantCost:N0}</size>";
            enchantStrikerZ.label.text = $"★ StrikerZ 가공\n<size=16>{CardData.RarityLabel(c.rarity)} CP {ShopDaily.StrikerZEnchantCost:N0}</size>";
            enchantSeven.Interactable = ShopDaily.CanEnchant(c, CardFinish.Seven, out _);
            enchantStrikerZ.Interactable = ShopDaily.CanEnchant(c, CardFinish.StrikerZ, out _);
        }

        /// <summary>고리 위에 보여 줄 사본: 일반 사본이 있으면 일반 (가공 대상), 없으면 가장 좋은 가공</summary>
        static CardFinish EnchantShownFinish(CardData c) => Profile.FinishCount(c.id, CardFinish.Normal) > 0 ? CardFinish.Normal : Profile.BestFinish(c.id);

        void DoEnchant(CardFinish target)
        {
            var c = enchantSel;
            if (c == null) return;
            if (!ShopDaily.Enchant(c, target, out string why)) { Toast(why); return; }
            StartCoroutine(EnchantFx(c, target));
        }

        IEnumerator EnchantFx(CardData c, CardFinish target)
        {
            enchantSeven.Interactable = enchantStrikerZ.Interactable = false;
            bool z = target == CardFinish.StrikerZ;
            Sfx.Play(z ? "charge5" : "charge3");
            var col = FinishStyle.Base(target);
            // 기 모으기: 카드가 떨리며 커지고 빛이 차오른다
            float dur = z ? 1.3f : 0.9f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur;
                enchantCardHolder.localScale = Vector3.one * (1f + k * 0.12f);
                enchantCardHolder.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 60f) * 2.5f * k);
                enchantGlow.color = new Color(col.r, col.g, col.b, 0.3f + k * 0.6f);
                yield return null;
            }
            Sfx.Play(z ? "strikerz" : "seven");
            enchantFace.SetFinish(target);
            enchantCardHolder.localRotation = Quaternion.identity;
            for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.9f;
                enchantFlash.color = new Color(col.r, col.g, col.b, (1 - k) * 0.85f);
                enchantCardHolder.localScale = Vector3.one * (1.12f + Mathf.Sin(k * Mathf.PI) * 0.15f - k * 0.12f);
                yield return null;
            }
            enchantFlash.color = new Color(1, 1, 1, 0);
            enchantCardHolder.localScale = Vector3.one;
            Toast($"{c.name} 에 {(z ? "★ StrikerZ" : "◆ Seven")} 가공을 부여했습니다!");
            ParentsSay(z ? (true, $"{c.name}, StrikerZ 가공 완성이다! 눈부시지?") : (false, $"{c.name} 에 Seven 가공을 입혔단다. 예쁘게 빛나지?"));
            RefreshTop();
            RefreshEnchantGrid();
            RefreshEnchantPanel();
            enchantFace.SetFinish(target);
        }

        // ── 테스트 ──
        public void TestOpenEnchanter(string id) => OpenEnchanter(CardDB.Get(id));
        public void TestEnchant(CardFinish f) => DoEnchant(f);
        public void TestRefreshShop() => RefreshShop();
    }
}
