using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 상점 › 루콘의 꾸미기 공방: 카드 슬리브 · 덱케이스 · 코인 · 주사위.
    /// 로지의 할아버지 '루콘'(골뱅이 안경 때문에 눈이 안 보인다)이 파는 별도 화면.
    /// 상점 띠의 오른쪽 칸 (MainMenu.ShopHall). 처음 걸어 들어오면 루콘이 깡총 등장(안경 반짝) → 인사.
    /// 화면: 왼쪽 루콘·대화창, 가운데 고른 아이템 크게 보기 + 구매/장착, 오른쪽 종류 탭 + 목록.
    /// </summary>
    public partial class MainMenu
    {
        RectTransform cosmeticView, cosmeticGrid, cosPreview, shopPage;
        Text cosName, cosDesc, cosState, cosOpal;
        Image cosGlow;
        UIKit.Btn cosBuy, cosEquip;
        CosmeticKind cosKind = CosmeticKind.Sleeve;
        CosmeticItem cosSel;
        readonly Dictionary<CosmeticKind, UIKit.Btn> cosTabs = new Dictionary<CosmeticKind, UIKit.Btn>();

        // ── 루콘 ──
        RectTransform lukon, lukonFit, lukonGlint;
        Text lukonLine;
        Coroutine lukonCo;
        float lukonHop = -10f;
        CanvasGroup cosPreviewGroup, cosListGroup, lukonGroup;
        bool workshopBusy;

        static readonly Color CosAccent = new Color(0.2f, 0.45f, 0.55f, 0.97f);
        static readonly Color WorkshopGlass = new Color(0.03f, 0.03f, 0.05f, 0.84f);
        /// <summary>루콘 그림(0~1 좌표)에서 안경 위치 — 반짝임을 띄울 자리</summary>
        static readonly Vector2 LukonGlasses = new Vector2(0.59f, 0.675f);

        static readonly string[] LukonGreet =
        {
            "어서 오게! 오늘은 무얼 꾸며 볼 텐가?",
            "또 왔구먼. 새로 깎은 주사위 좀 보고 가게나.",
            "속성마다 하나씩, 이 늙은이가 정성껏 만들었다네.",
            "로지 녀석은 장사 잘 하고 있던가?",
            "카드에도 옷이 필요하지. 슬리브 구경하고 가게!",
        };
        static readonly string[] LukonPoke =
        {
            "어이쿠, 안경 건드리지 말게! 이게 없으면 아무것도 안 보여!",
            "늙은이 수염은 왜 당기누.",
            "이 주사위는 한 면 한 면 손으로 다듬었다네.",
            "슬리브를 바꾸면 카드가 기분이 좋아진다네. 진짜야.",
            "눈? 이 안경 뒤에 있지. 본 사람은… 로지뿐일세.",
            "덱케이스엔 레이드 보스들 문양을 새겼지. 진짜 보스는 못 봤지만!",
        };

        void BuildCosmeticView(RectTransform page)
        {
            cosmeticView = UIKit.Rect("CosmeticView", page).Fill();

            // ── 공방 배경 (로지 상점과 같은 액자) ──
            var frame = UIKit.Frame(cosmeticView, "Scene", new Color(0.02f, 0.01f, 0.03f, 1f));
            frame.rectTransform.Fill();
            var mask = UIKit.Img(frame.transform, "Mask", Color.white, GameAssets.Rounded, true);
            mask.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var bg = UIKit.Img(mask.transform, "Bg", new Color(0.92f, 0.9f, 0.9f), GameAssets.TexSprite("shop_lukon_bg") ?? GameAssets.TexSprite("shop_bg"));
            bg.rectTransform.Fill();
            var shade = UIKit.Img(mask.transform, "Shade", new Color(0.02f, 0.01f, 0.05f, 0.2f));
            shade.rectTransform.Fill();
            frame.transform.GetChild(0).SetAsLastSibling();

            // ── 루콘 (왼쪽). 클릭하면 대사 + 깡총 + 안경 반짝 ──
            lukon = UIKit.Rect("LukonArea", cosmeticView).Anchor(-0.02f, 0.0f, 0.3f, 0.97f, 0, 3, 0, 0);
            lukon.pivot = new Vector2(0.5f, 0);
            lukonGroup = lukon.gameObject.AddComponent<CanvasGroup>();
            lukonFit = UIKit.Rect("Fit", lukon);
            lukonFit.pivot = new Vector2(0.5f, 0);
            var fit = lukonFit.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var lukonSprite = GameAssets.TexSprite("shop_lukon");
            fit.aspectRatio = lukonSprite != null ? lukonSprite.rect.width / lukonSprite.rect.height : 0.7f;
            var lukonImg = UIKit.Img(lukonFit, "Lukon", Color.white, lukonSprite);
            lukonImg.rectTransform.Fill();
            lukonImg.raycastTarget = true;
            var lb = lukonImg.gameObject.AddComponent<Button>();
            lb.transition = Selectable.Transition.None;
            lb.onClick.AddListener(() => { if (workshopBusy) return; lukonHop = Time.unscaledTime; StartCoroutine(Glint()); LukonSay(LukonPoke[rng.Next(LukonPoke.Length)]); });
            // 안경 반짝임: 십자 빛줄기 두 개 + 가운데 빛
            lukonGlint = UIKit.Rect("Glint", lukonFit);
            lukonGlint.Place(LukonGlasses, Vector2.zero, new Vector2(10, 10));
            var gc = UIKit.Img(lukonGlint, "Core", new Color(1f, 1f, 0.9f, 0.95f), GameAssets.Glow);
            gc.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            var gh = UIKit.Img(lukonGlint, "H", new Color(1f, 1f, 1f, 0.9f), GameAssets.Glow);
            gh.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 16));
            var gv = UIKit.Img(lukonGlint, "V", new Color(1f, 1f, 1f, 0.9f), GameAssets.Glow);
            gv.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 170));
            foreach (var g in lukonGlint.GetComponentsInChildren<Image>()) g.raycastTarget = false;
            lukonGlint.gameObject.SetActive(false);

            // 대화창 (루콘 아래)
            var box = UIKit.Frame(cosmeticView, "Dialog", new Color(0.05f, 0.04f, 0.08f, 0.93f));
            box.rectTransform.Anchor(0.012f, 0.018f, 0.3f, 0.2f);
            var tag = UIKit.Img(box.transform, "NameTag", new Color(0.55f, 0.36f, 0.16f, 1f), GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(18, 18), new Vector2(120, 38), new Vector2(0, 1));
            var tn = UIKit.Txt(tag.transform, "N", "루콘", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            tn.rectTransform.Fill();
            lukonLine = UIKit.Txt(box.transform, "Line", "", 22, UIKit.Ink, TextAnchor.UpperLeft);
            lukonLine.rectTransform.Anchor(0, 0, 1, 1, 22, 12, 18, 28);
            lukonLine.lineSpacing = 1.1f;
            var back = UIKit.Button(box.transform, "◀ 카드 상점", () => GoSection(SecRosie), Dark, 19);
            back.rt.Place(new Vector2(1, 1), new Vector2(-14, 18), new Vector2(170, 40), new Vector2(1, 1));

            // ── 가운데: 미리보기 ──
            var mid = UIKit.Frame(cosmeticView, "Preview", WorkshopGlass);
            mid.rectTransform.Anchor(0.31f, 0.015f, 0.565f, 0.985f);
            cosPreviewGroup = mid.gameObject.AddComponent<CanvasGroup>();
            var title = UIKit.Txt(mid.transform, "T", "루콘의 꾸미기 공방", 30, new Color(1f, 0.82f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 10, -58, 10, 12);
            title.Shadow();
            HelpIcon(mid.rectTransform, LukonHelp).Place(new Vector2(1, 1), new Vector2(-10, -10), new Vector2(40, 40), new Vector2(1, 1));
            cosGlow = UIKit.Img(mid.transform, "Glow", new Color(0.45f, 0.8f, 1f, 0.25f), GameAssets.Glow);
            cosGlow.rectTransform.Anchor(0.0f, 0.36f, 1f, 0.92f);
            // 제목 아래 ~ 이름 위 빈 공간의 정중앙에 (위아래 여백 같게)
            cosPreview = UIKit.Rect("Art", mid.transform).Anchor(0.06f, 0.4f, 0.94f, 1f, 0, 0, 0, 66);
            cosName = UIKit.Txt(mid.transform, "Name", "", 28, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            cosName.rectTransform.Anchor(0, 0.32f, 1, 0.39f, 12, 0, 12, 0);
            cosName.resizeTextForBestFit = true; cosName.resizeTextMinSize = 18; cosName.resizeTextMaxSize = 28;
            cosName.Shadow();
            cosDesc = UIKit.Txt(mid.transform, "Desc", "", 18, UIKit.Ink, TextAnchor.UpperCenter);
            cosDesc.rectTransform.Anchor(0, 0.2f, 1, 0.32f, 22, 0, 22, 0);
            cosDesc.lineSpacing = 1.15f;
            cosState = UIKit.Txt(mid.transform, "State", "", 18, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleCenter);
            cosState.rectTransform.Anchor(0, 0.145f, 1, 0.2f, 12, 0, 12, 0);
            cosBuy = UIKit.Button(mid.transform, "", () => BuyCosmetic(cosSel), new Color(0.2f, 0.4f, 0.15f, 0.97f), 21, Color.white);
            cosBuy.rt.Anchor(0.05f, 0.025f, 0.49f, 0.13f);
            cosEquip = UIKit.Button(mid.transform, "장착", () => EquipCosmetic(cosSel), CosAccent, 21, Color.white);
            cosEquip.rt.Anchor(0.51f, 0.025f, 0.95f, 0.13f);

            // ── 오른쪽: 탭 + 목록 ──
            var right = UIKit.Frame(cosmeticView, "List", WorkshopGlass);
            right.rectTransform.Anchor(0.575f, 0.015f, 0.993f, 0.985f);
            cosListGroup = right.gameObject.AddComponent<CanvasGroup>();
            var rt = UIKit.Txt(right.transform, "T", "장착한 꾸미기는 듀얼에서 내 쪽에 적용됩니다", 19, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            rt.rectTransform.Anchor(0, 1, 1, 1, 20, -50, 20, 10);
            cosOpal = UIKit.Txt(right.transform, "Opal", "", 18, UIKit.Ink, TextAnchor.MiddleRight);
            cosOpal.rectTransform.Anchor(0, 1, 1, 1, 20, -50, 20, 10);
            for (int i = 0; i < Cosmetics.Kinds.Length; i++)
            {
                var k = Cosmetics.Kinds[i];
                var b = UIKit.Button(right.transform, Cosmetics.KindLabel(k), () => { cosKind = k; cosSel = Cosmetics.Equipped(k); RefreshCosmetics(); }, Dark, 19);
                b.rt.Place(new Vector2(0, 1), new Vector2(20 + i * 152, -58), new Vector2(144, 44), new Vector2(0, 1));
                cosTabs[k] = b;
            }
            cosmeticGrid = UIKit.ScrollGrid(right.transform, "Grid", new Vector2(172, 232), new Vector2(10, 12), out _);
            ((RectTransform)cosmeticGrid.parent).Anchor(0, 0, 1, 1, 8, 8, 8, 112);
            // 꾸미기 화면이 꺼지면(상점 복귀·다른 탭) 3D 무대 카메라도 끈다
            cosmeticView.gameObject.AddComponent<ActiveHook>().off = DiceStage.Stop;
        }

        /// <summary>루콘 등장: 아래에서 깡총 뛰어올라 착지(찌그러짐) → 안경 반짝 → 인사. 패널은 오른쪽에서 미끄러져 들어온다</summary>
        IEnumerator LukonEnter()
        {
            workshopBusy = true;
            var p = lukon;
            StartCoroutine(SlideIn(cosPreviewGroup, 0.1f));
            StartCoroutine(SlideIn(cosListGroup, 0.22f));
            const float dur = 0.55f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur;
                lukonGroup.alpha = Mathf.Clamp01(k * 3);
                // 포물선: 아래(-420)에서 위로 솟았다가 제자리
                float y = k < 0.7f ? Mathf.Lerp(-420, 60, EaseOut(k / 0.7f)) : Mathf.Lerp(60, 0, (k - 0.7f) / 0.3f);
                p.anchoredPosition = new Vector2(0, y);
                p.localScale = new Vector3(1f, 1f + (k < 0.7f ? 0.06f : 0f), 1f);
                yield return null;
            }
            Sfx.Play("dice", 0.5f);
            // 착지 찌그러짐 → 복원
            for (float t = 0; t < 0.28f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.28f;
                float sq = Mathf.Sin(k * Mathf.PI) * (1 - k * 0.4f);
                p.localScale = new Vector3(1f + sq * 0.07f, 1f - sq * 0.09f, 1f);
                p.anchoredPosition = Vector2.zero;
                yield return null;
            }
            p.localScale = Vector3.one;
            lukonGroup.alpha = 1;
            yield return Glint();

            var d = Profile.Data;
            if (!d.lukonMet)
            {
                d.lukonMet = true;
                Profile.Save();
                LukonSay("처음 보는 얼굴이로군! 나는 로지 할애비 루콘일세. 이 공방에선 슬리브·덱케이스·코인·주사위를 직접 만든다네.",
                         "눈이 안 보인다고? 걱정 말게, 이 안경 너머로 다 보인다네. …아마도.");
            }
            else LukonSay(LukonGreet[rng.Next(LukonGreet.Length)]);
            workshopBusy = false;
        }

        IEnumerator SlideIn(CanvasGroup g, float delay)
        {
            var r = (RectTransform)g.transform;
            var basePos = r.anchoredPosition;
            g.alpha = 0;
            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / 0.35f);
                g.alpha = k;
                r.anchoredPosition = basePos + new Vector2((1 - k) * 90, 0);
                yield return null;
            }
            g.alpha = 1;
            r.anchoredPosition = basePos;
        }

        /// <summary>골뱅이 안경 반짝: 십자 빛이 커지며 돌다가 사라진다</summary>
        IEnumerator Glint()
        {
            lukonGlint.gameObject.SetActive(true);
            Sfx.Play("seven", 0.35f);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                float s = Mathf.Sin(k * Mathf.PI);
                lukonGlint.localScale = Vector3.one * (0.2f + s * 0.9f);
                lukonGlint.localRotation = Quaternion.Euler(0, 0, k * 90f);
                yield return null;
            }
            lukonGlint.gameObject.SetActive(false);
        }

        void LukonSay(params string[] lines)
        {
            if (lukonLine == null) return;
            if (lukonCo != null) StopCoroutine(lukonCo);
            lukonCo = StartCoroutine(LukonTalk(lines));
        }

        IEnumerator LukonTalk(string[] lines)
        {
            for (int n = 0; n < lines.Length; n++)
            {
                var line = lines[n];
                Voice.Line("lukon", line);
                for (int i = 1; i <= line.Length; i++)
                {
                    lukonLine.text = line.Substring(0, i);
                    yield return new WaitForSecondsRealtime(0.024f);
                }
                if (n < lines.Length - 1) yield return new WaitForSecondsRealtime(1.8f);
            }
        }

        /// <summary>루콘 숨쉬기 · 깡총 (Update 에서)</summary>
        void UpdateLukon()
        {
            if (cosmeticView == null || !cosmeticView.gameObject.activeInHierarchy || workshopBusy) return;
            float t = Time.unscaledTime;
            float hop = t - lukonHop;
            float jump = hop < 0.45f ? Mathf.Sin(hop / 0.45f * Mathf.PI) * 22f : 0f;
            lukon.anchoredPosition = new Vector2(0, jump);
            lukon.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.8f) * 0.008f, 1f);
        }

        // ══════════════════════════ 목록 · 미리보기 ══════════════════════════

        void OpenCosmetics()
        {
            if (shopSection == SecLukon && !shopMoving && !doorPlaying) { RefreshCosmetics(); return; }
            GoSection(SecLukon);
        }

        void RefreshCosmetics()
        {
            foreach (var kv in cosTabs) kv.Value.SetSelected(kv.Key == cosKind, CosAccent);
            cosOpal.text = $"보유 오팔 {Profile.Data.opal:N0}";
            UIKit.Clear(cosmeticGrid);
            var items = Cosmetics.OfKind(cosKind).ToList();
            // 주사위 목록은 3D 로 한 장에 찍어 칸마다 잘라 쓴다
            Rect[] diceUv = null;
            Texture diceIcons = cosKind == CosmeticKind.Dice ? DiceStage.Icons(items, out diceUv) : null;
            for (int idx = 0; idx < items.Count; idx++)
            {
                var it = items[idx];
                bool owned = Cosmetics.Owned(it), equipped = Cosmetics.IsEquipped(it);
                var cell = UIKit.Img(cosmeticGrid, "Item_" + it.id, new Color(0.08f, 0.07f, 0.12f, 0.9f), GameAssets.Rounded, true).rectTransform;
                var tint = it.Tint;
                var g = UIKit.Img(cell, "Glow", new Color(tint.r, tint.g, tint.b, it == cosSel ? 0.4f : 0.14f), GameAssets.Glow);
                g.rectTransform.Anchor(0, 0.2f, 1, 1);
                if (diceIcons != null)
                {
                    var raw = UIKit.Rect("Icon3D", cell).Anchor(0.02f, 0.22f, 0.98f, 0.98f).gameObject.AddComponent<RawImage>();
                    raw.texture = diceIcons;
                    raw.uvRect = diceUv[idx];
                    raw.raycastTarget = false;
                    var af = raw.gameObject.AddComponent<AspectRatioFitter>();
                    af.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    af.aspectRatio = diceUv[idx].width * diceIcons.width / (diceUv[idx].height * diceIcons.height);
                }
                else IconArt(cell, it, new Vector4(0.08f, 0.24f, 0.92f, 0.96f));
                // 속성 표시 (왼쪽 위)
                var pill = UIKit.Img(cell, "ElementTag", new Color(0.02f, 0.02f, 0.05f, 0.85f), GameAssets.Rounded, true);
                pill.rectTransform.Place(new Vector2(0, 1), new Vector2(6, -6), new Vector2(it.element == Element.Rainbow ? 64 : 50, 24), new Vector2(0, 1));
                var el = UIKit.Txt(pill.transform, "T", it.element == Element.None ? "기본" : CardData.ElementName(it.element), 14, it.element == Element.None ? UIKit.Muted : tint, TextAnchor.MiddleCenter, FontStyle.Bold);
                el.rectTransform.Fill();
                var nm = UIKit.Txt(cell, "Name", it.name, 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                nm.rectTransform.Anchor(0, 0.11f, 1, 0.23f, 6, 0, 6, 0);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = 17;
                string st = equipped ? "<color=#8ef>● 장착 중</color>" : owned ? "<color=#9f9>보유</color>" : $"<color=#ffd77a>{it.price:N0} 오팔</color>";
                var pt = UIKit.Txt(cell, "State", st, 16, UIKit.Ink, TextAnchor.MiddleCenter);
                pt.rectTransform.Anchor(0, 0.01f, 1, 0.11f);
                if (it == cosSel)
                {
                    var hl = UIKit.Img(cell, "Sel", new Color(0.55f, 0.9f, 1f, 1f), GameAssets.RoundedOutline, true);
                    hl.rectTransform.Anchor(-0.02f, -0.015f, 1.02f, 1.015f);
                }
                cell.HitArea();
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () => { cosSel = it; Sfx.Play("click"); RefreshCosmetics(); };
            }
            RefreshCosmeticPreview();
        }

        void RefreshCosmeticPreview()
        {
            var c = cosSel ?? Cosmetics.Equipped(cosKind);
            UIKit.Clear(cosPreview);
            if (c.kind == CosmeticKind.Coin || c.kind == CosmeticKind.Dice)
            {
                // 3D: 전투와 같은 주사위/코인이 천천히 돌며 여러 면(코인은 앞·뒷면)을 보여 준다
                var raw = UIKit.Rect("Live3D", cosPreview).Anchor(0, 0, 1, 1).gameObject.AddComponent<RawImage>();
                raw.texture = DiceStage.Live(c);
                raw.raycastTarget = false;
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio = 1;
            }
            else
            {
                DiceStage.Stop();
                IconArt(cosPreview, c, new Vector4(0, 0, 1, 1));
            }
            var tc = c.Tint;
            cosGlow.color = new Color(tc.r, tc.g, tc.b, 0.3f);

            cosName.text = c.name;
            cosDesc.text = c.desc;
            bool owned = Cosmetics.Owned(c), equipped = Cosmetics.IsEquipped(c);
            cosState.text = $"{Cosmetics.KindLabel(c.kind)}  ·  " + (equipped ? "장착 중" : owned ? (c.IsDefault ? "기본 아이템" : "보유 중") : $"가격 {c.price:N0} 오팔");
            cosBuy.label.text = owned ? "보유 중" : $"구매\n<size=16>{c.price:N0} 오팔</size>";
            cosBuy.Interactable = !owned && Profile.Data.opal >= c.price;
            cosEquip.label.text = equipped ? "장착 중" : "장착";
            cosEquip.Interactable = owned && !equipped;
        }

        /// <summary>아이템 대표 그림 (주사위는 6 면, 코인은 앞면)</summary>
        static void IconArt(RectTransform parent, CosmeticItem c, Vector4 a)
        {
            var img = UIKit.Img(parent, "Icon", Color.white, Cosmetics.Icon(c));
            img.preserveAspect = true;
            img.rectTransform.Anchor(a.x, a.y, a.z, a.w);
            if (c.kind == CosmeticKind.Dice) img.rectTransform.Anchor(a.x + (a.z - a.x) * 0.12f, a.y + (a.w - a.y) * 0.1f, a.z - (a.z - a.x) * 0.12f, a.w - (a.w - a.y) * 0.1f);
        }

        void BuyCosmetic(CosmeticItem c)
        {
            if (c == null) return;
            if (!Cosmetics.Buy(c, out string why))
            {
                Toast(why);
                if (!Cosmetics.Owned(c)) LukonSay("오팔이 조금 모자라구먼. 퀘스트를 깨고 다시 오게나.");
                return;
            }
            Sfx.Play("reveal1");
            Cosmetics.Equip(c);         // 사면 바로 장착
            Toast($"「{c.name}」 구매 완료! 바로 장착했습니다");
            lukonHop = Time.unscaledTime;
            StartCoroutine(Glint());
            LukonSay($"고맙네! 「{c.name}」, 자네한테 잘 어울릴 걸세. 바로 장착해 두었네.");
            RefreshTop();
            RefreshCosmetics();
        }

        void EquipCosmetic(CosmeticItem c)
        {
            if (c == null || !Cosmetics.Equip(c)) return;
            Sfx.Play("click");
            Toast($"「{c.name}」 장착!");
            LukonSay(c.IsDefault ? "기본도 좋지. 처음 마음을 잊지 않는 게 중요하다네." : $"「{c.name}」로 바꿔 두었네. 듀얼에서 뽐내 보게!");
            RefreshCosmetics();
            RefreshHome();
        }

        // ── 테스트 ──
        public void TestOpenCosmetics(CosmeticKind k, string id = null)
        {
            cosKind = k;
            cosSel = id != null ? Cosmetics.Get(id) : Cosmetics.Equipped(k);
            OpenCosmetics();
        }
        public void TestBuyCosmetic(string id) => BuyCosmetic(Cosmetics.Get(id));
        public bool TestWorkshopBusy => workshopBusy;
    }

    /// <summary>켜지고 꺼질 때 알려 주는 작은 훅 (화면 전환 시 3D 무대 정리용)</summary>
    public class ActiveHook : MonoBehaviour
    {
        public System.Action on, off;
        void OnEnable() => on?.Invoke();
        void OnDisable() => off?.Invoke();
    }
}
