using System;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>런타임 UGUI 생성 헬퍼</summary>
    public static class UIKit
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.36f);
        public static readonly Color GoldDark = new Color(0.55f, 0.4f, 0.14f);
        public static readonly Color Panel = new Color(0.04f, 0.035f, 0.07f, 0.86f);
        public static readonly Color Ink = new Color(0.93f, 0.92f, 0.96f);
        public static readonly Color Muted = new Color(0.65f, 0.63f, 0.72f);
        /// <summary>선택된 탭·필터 버튼 바탕 (금색)</summary>
        public static readonly Color Accent = new Color(0.62f, 0.46f, 0.12f, 0.97f);
        /// <summary>기본 버튼·패널 바탕</summary>
        public static readonly Color Dark = new Color(0.04f, 0.03f, 0.08f, 0.9f);

        /// <summary>
        /// 화면 캔버스 (Overlay · 1920×1080 기준 · 가로세로 절반씩 맞춤). raycast=false 면 입력을 받지 않는다.
        /// </summary>
        public static Canvas ScreenCanvas(GameObject go, int order, bool raycast = true)
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (raycast) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>클릭을 받는 투명 영역 (이미 Image 가 있으면 그것을 쓴다)</summary>
        public static Image HitArea(this RectTransform rt)
        {
            var img = rt.GetComponent<Image>();
            if (img == null)
            {
                img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.001f);
            }
            img.raycastTarget = true;
            return img;
        }

        /// <summary>한 줄의 버튼들 중 라벨이 selected 인 것만 강조 (필터·탭 줄)</summary>
        public static void HighlightByLabel(Transform row, string selected, Color on, Color off, Color? offText = null)
        {
            foreach (Transform t in row)
            {
                var lbl = t.GetComponentInChildren<Text>();
                bool sel = lbl != null && lbl.text == selected;
                var img = t.GetComponent<Image>();
                if (img != null) img.color = sel ? on : IsNeutral(off) ? BtnFill : off;
                if (lbl != null) lbl.color = sel ? Color.white : offText ?? Ink;
                var edge = t.Find("Border")?.GetComponent<Image>();
                if (edge != null) edge.color = sel ? Gold : EdgeFor(off);
                var bar = t.Find("Bar")?.GetComponent<Image>();
                if (bar != null) bar.enabled = sel;
            }
        }

        /// <summary>원형 아바타 (링 → 마스크 → 이미지). 반환값은 이미지, 링은 이미지의 parent.parent</summary>
        public static Image AvatarCircle(Transform parent, float size)
        {
            var ring = Img(parent, "AvatarRing", Gold, GameAssets.Circle);
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            var mask = Img(ring.transform, "Mask", Color.white, GameAssets.Circle);
            mask.rectTransform.Anchor(0.05f, 0.05f, 0.95f, 0.95f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var img = Img(mask.transform, "Avatar", Color.white);
            img.rectTransform.Fill();
            img.preserveAspect = true;
            return img;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, float minX, float minY, float maxX, float maxY, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>고정 크기 + 앵커 지점</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt) => rt.Anchor(0, 0, 1, 1);

        public static Image Img(Transform parent, string name, Color c, Sprite s = null, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.sprite = s;
            if (sliced && s != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GameAssets.Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static T Shadow<T>(this T g, Color? c = null, float dist = 2f) where T : Graphic
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = c ?? new Color(0, 0, 0, 0.8f);
            s.effectDistance = new Vector2(dist, -dist);
            return g;
        }

        public static T Outline<T>(this T g, Color c, float dist = 1.5f) where T : Graphic
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(dist, -dist);
            return g;
        }

        /// <summary>금테 패널</summary>
        public static Image Frame(Transform parent, string name, Color? fill = null)
        {
            var bg = Img(parent, name, fill ?? Panel, GameAssets.Rounded, true);
            var border = Img(bg.transform, "Border", new Color(Gold.r, Gold.g, Gold.b, 0.3f), GameAssets.RoundedOutline, true);
            border.rectTransform.Fill();
            return bg;
        }

        // ── 버튼 (왼쪽 메뉴와 같은 스타일): 어두운 유리 바탕 + 옅은 테두리 + 흰 글씨, 선택 = 금빛 바탕 + 금 테두리 + 왼쪽 색 막대 ──

        // ── 구역 제목 (왼쪽 메뉴 머리말과 같은 모양): 색 막대 + 제목 + 구분선 ──
        public static readonly Color SecPlay = new Color(0.95f, 0.45f, 0.35f), SecCollect = new Color(0.45f, 0.72f, 1f), SecMe = new Color(1f, 0.82f, 0.36f);

        /// <summary>패널 제목 Text 를 구역 제목 모양으로: 왼쪽 색 막대 + 밝힌 색 글자 + 아래 구분선 (글자는 막대만큼 오른쪽으로)</summary>
        public static Text Section(this Text t, Color accent, int size = 24)
        {
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.Lerp(accent, Color.white, 0.35f);
            foreach (var fx in t.GetComponents<UnityEngine.UI.Shadow>()) UnityEngine.Object.Destroy(fx);
            var rt = t.rectTransform;
            rt.offsetMin += new Vector2(16, 0);
            var bar = Img(t.transform, "Accent", accent);
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0, 0.5f);
            bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.anchoredPosition = new Vector2(-16, 0);
            bar.rectTransform.sizeDelta = new Vector2(5, size + 2);
            var line = Img(t.transform, "Line", new Color(accent.r, accent.g, accent.b, 0.28f));
            line.raycastTarget = false;
            line.rectTransform.anchorMin = new Vector2(0, 0);
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.offsetMin = new Vector2(-18, -3);
            line.rectTransform.offsetMax = new Vector2(0, -2);
            return t;
        }

        /// <summary>기본 버튼 바탕 (어두운 유리)</summary>
        public static readonly Color BtnFill = new Color(0.07f, 0.06f, 0.12f, 0.94f);
        /// <summary>기본 버튼 테두리 (옅은 흰색)</summary>
        public static readonly Color BtnEdge = new Color(1, 1, 1, 0.14f);
        /// <summary>선택된 탭·토글 바탕 (왼쪽 메뉴 선택과 같은 금빛)</summary>
        public static readonly Color SelFill = new Color(0.46f, 0.32f, 0.08f, 0.96f);

        static bool IsNeutral(Color c) => c.r < 0.12f && c.g < 0.12f && c.b < 0.16f;

        /// <summary>바탕색에 맞는 테두리: 어두운 버튼 = 옅은 흰색, 색 버튼 = 같은 색을 밝힌 테두리</summary>
        static Color EdgeFor(Color fill) => IsNeutral(fill) ? BtnEdge : new Color(Mathf.Lerp(fill.r, 1, 0.5f), Mathf.Lerp(fill.g, 1, 0.5f), Mathf.Lerp(fill.b, 1, 0.5f), 0.6f);

        public class Btn
        {
            public Button button;
            public Image bg, border, bar, icon;
            public Text label;
            public RectTransform rt => (RectTransform)button.transform;
            public bool Interactable { get => button.interactable; set { button.interactable = value; bg.color = value ? baseColor : new Color(0.16f, 0.16f, 0.2f, 0.8f); label.color = value ? textColor : new Color(0.5f, 0.5f, 0.55f); if (icon != null) icon.color = value ? Color.white : new Color(1, 1, 1, 0.4f); } }
            public Color baseColor, textColor;

            /// <summary>탭·토글 버튼 선택 상태 (왼쪽 메뉴와 같은 모양): 켜짐 = onColor(기본 금빛) 바탕 + 금 테두리 + 왼쪽 막대 + 흰 글자, 꺼짐 = 어두운 유리 바탕</summary>
            public void SetSelected(bool on, Color? onColor = null, Color? offColor = null)
            {
                baseColor = on ? onColor ?? SelFill : offColor ?? BtnFill;
                textColor = on ? Color.white : Ink;
                border.color = on ? Gold : EdgeFor(baseColor);
                bar.enabled = on;
                Interactable = true;    // 세터가 색을 다시 칠한다
            }

            /// <summary>왼쪽에 아이콘 (왼쪽 메뉴처럼 아이콘 + 이름)</summary>
            public Btn Icon(Sprite s, float size = 0)
            {
                if (s == null) return this;
                if (icon == null)
                {
                    icon = Img(bg.transform, "Icon", Color.white, s);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }
                icon.sprite = s;
                float h = rt.rect.height > 1 ? rt.rect.height : rt.sizeDelta.y;
                if (size <= 0) size = Mathf.Clamp(h - 10, 26, 56);
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(10 + size / 2, 0), new Vector2(size, size), new Vector2(0.5f, 0.5f));
                label.rectTransform.Anchor(0, 0, 1, 1, size + 14, 2, 10, 2);
                // 좁은 버튼에서도 한 줄로 (넘치면 글자를 줄인다)
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(10, label.fontSize - 8);
                label.resizeTextMaxSize = label.fontSize;
                return this;
            }
        }

        public static Btn Button(Transform parent, string label, Action onClick, Color? color = null, int fontSize = 26, Color? textColor = null)
        {
            var b = new Btn();
            var fill = color ?? BtnFill;
            if (IsNeutral(fill)) fill = BtnFill;     // Dark 등 어두운 버튼은 모두 같은 유리 바탕
            b.baseColor = fill;
            b.textColor = textColor ?? Ink;
            b.bg = Img(parent, "Btn_" + label, b.baseColor, GameAssets.Rounded, true);
            b.bg.raycastTarget = true;
            b.border = Img(b.bg.transform, "Border", EdgeFor(fill), GameAssets.RoundedOutline, true);
            b.border.rectTransform.Fill();
            b.bar = Img(b.bg.transform, "Bar", Gold);
            b.bar.rectTransform.Anchor(0, 0.2f, 0, 0.8f, 3, 0, -8, 0);
            b.bar.raycastTarget = false;
            b.bar.enabled = false;
            b.button = b.bg.gameObject.AddComponent<Button>();
            var colors = b.button.colors;
            colors.highlightedColor = new Color(1.6f, 1.5f, 1.3f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            b.button.colors = colors;
            b.button.targetGraphic = b.bg;
            b.button.onClick.AddListener(() => { Sfx.Play("click", 0.8f); onClick?.Invoke(); });
            b.label = Txt(b.bg.transform, "Label", label, fontSize, b.textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            b.label.rectTransform.Anchor(0, 0, 1, 1, 8, 2, 8, 2);
            b.label.Shadow();
            return b;
        }

        /// <summary>한 줄 입력칸</summary>
        public static InputField Input(Transform parent, string text, string placeholder, int fontSize = 26, int maxLength = 12)
        {
            var bg = Img(parent, "Input", new Color(0.02f, 0.02f, 0.05f, 0.9f), GameAssets.Rounded, true);
            bg.raycastTarget = true;
            var border = Img(bg.transform, "Border", new Color(1, 1, 1, 0.22f), GameAssets.RoundedOutline, true);
            border.rectTransform.Fill();
            var ph = Txt(bg.transform, "Placeholder", placeholder, fontSize, Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
            ph.rectTransform.Anchor(0, 0, 1, 1, 16, 4, 16, 4);
            var t = Txt(bg.transform, "Text", "", fontSize, Ink, TextAnchor.MiddleLeft);
            t.rectTransform.Anchor(0, 0, 1, 1, 16, 4, 16, 4);
            t.supportRichText = false;
            var input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = t;
            input.placeholder = ph;
            input.characterLimit = maxLength;
            input.lineType = InputField.LineType.SingleLine;
            input.text = text;
            return input;
        }

        /// <summary>세로 스크롤 그리드. content 에 셀을 추가하면 된다.</summary>
        public static RectTransform ScrollGrid(Transform parent, string name, Vector2 cell, Vector2 spacing, out ScrollRect scroll)
        {
            var view = Rect(name, parent);
            view.HitArea();
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = spacing;
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 카드 앞면: 프레임 이미지(타입별) + 실시간 텍스트/수치. 배치는 CardFrameLayout.cs.
    /// 3D 카드(월드 캔버스)와 확대 패널이 공유.
    /// </summary>
    public class CardFace
    {
        public RectTransform root;
        public Image frame, art, artTint, glow;
        Outline nameOutline, typeOutline, bodyOutline;
        // 오버프레임 흐림 유리 패널 (이름 / 종류 / 텍스트)
        readonly RectTransform[] frostPanels = new RectTransform[3];
        readonly RawImage[] frostImages = new RawImage[3];
        readonly Image[] frostTints = new Image[3];
        public Text nameText, typeText, bodyText, atkText, rarityText, badgeText, hpText, atkCap, hpCap;
        public Image atkBadge, badge, hpBadge;
        /// <summary>ATK·HP 수치만큼 작은 검·하트 아이콘 (배지 위 세로 줄)</summary>
        public RectTransform atkPips, hpPips;
        readonly System.Collections.Generic.List<Image> atkPipImgs = new System.Collections.Generic.List<Image>(), hpPipImgs = new System.Collections.Generic.List<Image>();

        /// <summary>
        /// n 개의 작은 아이콘을 카드 옆 테두리를 따라 아래(배지 위)에서 위로 쌓는다.
        /// x0~x1 = 아이콘 칸 가로 범위, aspect = 아이콘 가로/세로 (카드 픽셀 기준)
        /// </summary>
        void LayoutPips(System.Collections.Generic.List<Image> pool, RectTransform parent, string icon, int n, float x0, float x1, float aspect, float yStart)
        {
            n = Mathf.Clamp(n, 0, 20);
            while (pool.Count < n)
            {
                var img = UIKit.Img(parent, "Pip", Color.white, GameAssets.TexSprite(icon));
                img.preserveAspect = true;
                img.raycastTarget = false;
                pool.Add(img);
            }
            const float yEnd = 0.875f;
            float cw = x1 - x0;
            float ch = cw * faceW / faceH / Mathf.Max(0.2f, aspect);       // 아이콘 칸 높이 (카드 비율)
            float step = n <= 1 ? ch : Mathf.Min(ch * 1.15f, (yEnd - yStart - ch) / (n - 1));
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < n;
                pool[i].gameObject.SetActive(on);
                if (!on) continue;
                float y = yStart + i * step;
                pool[i].rectTransform.Anchor(x0, y, x1, y + ch);
            }
        }
        // 가공 (Seven / StrikerZ)
        public CardFinish finish { get; private set; }
        Image finishGlow, finishBorder, finishSheen;
        RawImage finishSecret;
        RectTransform finishSheenMask;
        Text finishTag;
        FinishFx finishFx;
        ShimmerText nameShimmer;
        float faceW, faceH, fontK;

        public static CardFace Build(Transform parent, float w, float h, float fontScale = 1f)
        {
            var f = new CardFace();
            f.root = UIKit.Rect("Face", parent);
            f.root.sizeDelta = new Vector2(w, h);

            f.glow = UIKit.Img(f.root, "Glow", new Color(1, 0.85f, 0.3f, 0), GameAssets.Glow);
            f.glow.rectTransform.Anchor(-0.25f, -0.2f, 1.25f, 1.2f);

            var bgBack = UIKit.Img(f.root, "Back", new Color(0.05f, 0.05f, 0.07f), GameAssets.Rounded, true);
            bgBack.rectTransform.Anchor(0.02f, 0.01f, 0.98f, 0.99f);

            f.art = UIKit.Img(f.root, "Art", Color.white);
            f.artTint = UIKit.Img(f.art.transform, "Tint", new Color(0, 0, 0, 0));
            f.artTint.rectTransform.Fill();

            for (int i = 0; i < 3; i++)
            {
                var panel = UIKit.Rect("Frost" + i, f.root);
                panel.gameObject.AddComponent<RectMask2D>();
                var raw = UIKit.Rect("Blur", panel).gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                var tint = UIKit.Img(panel, "Tint", Color.clear);
                tint.rectTransform.Fill();
                panel.gameObject.SetActive(false);
                f.frostPanels[i] = panel;
                f.frostImages[i] = raw;
                f.frostTints[i] = tint;
            }

            f.frame = UIKit.Img(f.root, "Frame", Color.white);
            f.frame.rectTransform.Fill();

            int fs(float v) => Mathf.RoundToInt(v * fontScale);

            f.nameText = UIKit.Txt(f.root, "Name", "", fs(40), Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            f.nameText.resizeTextForBestFit = true;
            f.nameText.resizeTextMinSize = fs(18);
            f.nameText.resizeTextMaxSize = fs(42);
            f.nameOutline = f.nameText.gameObject.AddComponent<Outline>();
            f.nameOutline.effectDistance = new Vector2(2.5f * fontScale + 0.5f, -2.5f * fontScale - 0.5f);

            f.rarityText = UIKit.Txt(f.art.transform, "Rarity", "", fs(26), Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            f.rarityText.rectTransform.Anchor(0, 0.8f, 1, 1, 0, 0, 12, 6);
            f.rarityText.Outline(new Color(0, 0, 0, 0.9f), 2f);
            f.rarityText.resizeTextForBestFit = true;          // "Uncommon" 처럼 긴 등급명은 줄여서 한 줄로
            f.rarityText.resizeTextMinSize = 6;
            f.rarityText.resizeTextMaxSize = fs(26);

            f.typeText = UIKit.Txt(f.root, "Type", "", fs(26), Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            f.typeText.resizeTextForBestFit = true;
            f.typeText.resizeTextMinSize = fs(14);
            f.typeText.resizeTextMaxSize = fs(28);
            f.typeOutline = f.typeText.gameObject.AddComponent<Outline>();
            f.typeOutline.effectDistance = new Vector2(2f * fontScale + 0.5f, -2f * fontScale - 0.5f);

            f.bodyText = UIKit.Txt(f.root, "Body", "", fs(26), UIKit.Ink, TextAnchor.UpperLeft);
            f.bodyText.resizeTextForBestFit = true;
            f.bodyText.resizeTextMinSize = fs(12);
            f.bodyText.resizeTextMaxSize = fs(28);
            f.bodyText.lineSpacing = 1.05f;
            f.bodyOutline = f.bodyText.gameObject.AddComponent<Outline>();
            f.bodyOutline.effectDistance = new Vector2(1.5f * fontScale + 0.5f, -1.5f * fontScale - 0.5f);

            // ATK = 검 아이콘(왼쪽 아래, 손잡이 메달리온 안에 숫자) · HP = 하트 보석(오른쪽 아래, 가운데 숫자) — VARCO 아이콘
            f.atkPips = UIKit.Rect("AtkPips", f.root);
            f.atkPips.Fill();
            f.hpPips = UIKit.Rect("HpPips", f.root);
            f.hpPips.Fill();
            f.atkBadge = UIKit.Img(f.root, "AtkBadge", Color.white, GameAssets.TexSprite("icon_sword") ?? GameAssets.Circle);
            f.atkBadge.preserveAspect = true;
            f.atkText = UIKit.Txt(f.atkBadge.transform, "Atk", "", fs(46), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            f.atkText.rectTransform.Anchor(0.08f, 0.36f, 0.92f, 0.56f);
            f.atkText.resizeTextForBestFit = true;
            f.atkText.resizeTextMinSize = fs(14);
            f.atkText.resizeTextMaxSize = fs(46);
            f.atkText.Outline(new Color(0, 0, 0, 0.95f), 2.5f);
            f.atkCap = UIKit.Txt(f.atkBadge.transform, "Cap", "", fs(10), Color.clear, TextAnchor.MiddleCenter);
            f.atkCap.gameObject.SetActive(false);

            f.hpBadge = UIKit.Img(f.root, "HpBadge", Color.white, GameAssets.TexSprite("icon_heart") ?? GameAssets.Circle);
            f.hpBadge.preserveAspect = true;
            f.hpText = UIKit.Txt(f.hpBadge.transform, "Hp", "", fs(52), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            f.hpText.rectTransform.Anchor(0.12f, 0.24f, 0.88f, 0.72f);
            f.hpText.resizeTextForBestFit = true;
            f.hpText.resizeTextMinSize = fs(14);
            f.hpText.resizeTextMaxSize = fs(52);
            f.hpText.Outline(new Color(0.25f, 0, 0, 0.95f), 2.5f);
            f.hpCap = UIKit.Txt(f.hpBadge.transform, "Cap", "", fs(10), Color.clear, TextAnchor.MiddleCenter);
            f.hpCap.gameObject.SetActive(false);

            f.badge = UIKit.Img(f.root, "Badge", new Color(0.1f, 0.25f, 0.6f, 0.95f), GameAssets.Rounded, true);
            f.badgeText = UIKit.Txt(f.badge.transform, "BadgeText", "", fs(26), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            f.badgeText.rectTransform.Fill();

            // ── 가공 레이어 ──
            f.faceW = w; f.faceH = h; f.fontK = fontScale;
            f.finishGlow = UIKit.Img(f.root, "FinishGlow", Color.clear, GameAssets.Glow);
            f.finishGlow.rectTransform.Anchor(-0.32f, -0.24f, 1.32f, 1.24f);
            f.finishGlow.transform.SetAsFirstSibling();
            f.finishBorder = UIKit.Img(f.root, "FinishBorder", Color.clear, GameAssets.FinishBorder, true);
            f.finishBorder.rectTransform.Fill();
            f.finishBorder.pixelsPerUnitMultiplier = 8f / Mathf.Max(2f, w * 0.048f);
            f.finishBorder.transform.SetSiblingIndex(f.frame.transform.GetSiblingIndex() + 1);
            // 시크릿 레어 사선 포일 (일러스트·프레임 위, 글자 아래)
            f.finishSecret = UIKit.Rect("FinishSecret", f.root).gameObject.AddComponent<RawImage>();
            f.finishSecret.raycastTarget = false;
            f.finishSecret.texture = GameAssets.SecretRare;
            f.finishSecret.rectTransform.Anchor(0.02f, 0.01f, 0.98f, 0.99f);
            float tiles = 2.3f;                    // 카드 가로에 약 74줄
            f.finishSecret.uvRect = new Rect(0, 0, tiles, tiles * h / w);
            f.finishSecret.transform.SetSiblingIndex(f.frame.transform.GetSiblingIndex() + 1);
            f.finishSheenMask = UIKit.Rect("FinishSheen", f.root).Anchor(0.02f, 0.01f, 0.98f, 0.99f);
            f.finishSheenMask.gameObject.AddComponent<RectMask2D>();
            f.finishSheen = UIKit.Img(f.finishSheenMask, "Bar", Color.clear, GameAssets.Glow);
            f.finishSheen.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * 0.42f, h * 2.4f));
            f.finishSheen.rectTransform.localRotation = Quaternion.Euler(0, 0, -24);
            f.finishFx = f.root.gameObject.AddComponent<FinishFx>();
            f.finishFx.width = w; f.finishFx.height = h;
            for (int i = 0; i < 6; i++)
            {
                var sp = UIKit.Img(f.finishSheenMask, "Spark", Color.clear, GameAssets.Sparkle);
                sp.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f) * w, UnityEngine.Random.Range(-0.4f, 0.4f) * h), Vector2.one * (w * 0.11f));
                f.finishFx.sparkles.Add(sp);
            }
            f.finishFx.border = f.finishBorder;
            f.finishFx.glow = f.finishGlow;
            f.finishFx.sheen = f.finishSheen;
            f.finishFx.secret = f.finishSecret;
            f.finishTag = UIKit.Txt(f.root, "FinishTag", "", fs(22), Color.white, TextAnchor.LowerCenter, FontStyle.Bold);
            f.finishTag.rectTransform.Anchor(0.3f, 0.004f, 0.7f, 0.055f);
            f.finishTag.horizontalOverflow = HorizontalWrapMode.Overflow;
            f.finishTag.Outline(new Color(0, 0, 0, 0.85f), 1.5f * fontScale + 0.5f);
            f.nameShimmer = f.nameText.gameObject.AddComponent<ShimmerText>();
            f.finishFx.shimmer = f.nameShimmer;
            f.SetFinish(CardFinish.Normal);
            return f;
        }

        /// <summary>가공 표시 (Set 이후 호출해도 되고, Set 을 다시 불러도 유지된다)</summary>
        public void SetFinish(CardFinish fin)
        {
            finish = fin;
            bool on = fin != CardFinish.Normal;
            finishGlow.gameObject.SetActive(on);
            finishBorder.gameObject.SetActive(on);
            // FinishKeepsFrame: 3D 속성 프레임 위에 얇은 VARCO 림 (없으면 얇은 절차적 테두리) / 아니면 VARCO 가공 전용 테두리 (없으면 절차적 외곽선)
            bool keepFrame = CardFrameLayout.FinishKeepsFrame;
            var art = !on ? null : keepFrame ? GameAssets.FinishRim(fin) : GameAssets.FinishFrame(fin);
            if (art != null)
            {
                finishBorder.sprite = art;
                finishBorder.type = Image.Type.Simple;
                if (keepFrame) finishBorder.rectTransform.Fill();
                else finishBorder.rectTransform.Anchor(-0.012f, -0.009f, 1.012f, 1.009f);
            }
            else
            {
                finishBorder.sprite = GameAssets.FinishBorder;
                finishBorder.type = Image.Type.Sliced;
                finishBorder.rectTransform.Fill();
                finishBorder.pixelsPerUnitMultiplier = 8f / Mathf.Max(2f, faceW * (keepFrame ? CardFrameLayout.FinishRim : 0.048f));
            }
            finishFx.texturedBorder = art != null;
            finishSecret.gameObject.SetActive(on);
            finishSheenMask.gameObject.SetActive(on);
            finishTag.gameObject.SetActive(on);
            nameShimmer.enabled = on;
            finishFx.finish = fin;
            finishFx.enabled = on;
            if (!on) return;
            finishSheenMask.SetAsLastSibling();
            finishTag.transform.SetAsLastSibling();
            finishSecret.color = new Color(1f, 1f, 1f, 0.14f);   // 첫 프레임(FinishFx 갱신 전)도 옅게
            var bc = FinishStyle.Base(fin);
            finishBorder.color = art != null ? Color.white : bc;
            nameShimmer.baseColor = bc;
            nameShimmer.highColor = FinishStyle.High(fin);
            nameShimmer.deepColor = FinishStyle.Deep(fin);
            nameOutline.enabled = true;
            nameOutline.effectColor = new Color(0.04f, 0.02f, 0.06f, 0.95f);
            finishTag.text = fin == CardFinish.StrikerZ ? "★ STRIKERZ" : "◆ SEVEN";
            finishTag.color = Color.Lerp(bc, Color.white, 0.3f);
            nameShimmer.Refresh();
        }

        static void Place(Graphic g, Vector4 r) => g.rectTransform.Anchor(r.x, r.y, r.z, r.w);

        void ApplyLayout(CardData d, Sprite fullArt)
        {
            // 원소 카드(마법 종류)는 다른 카드처럼 속성 색 프레임이 보이도록 몬스터 레이아웃(속성 전용 프레임)을 쓴다
            var L = d.isElement ? CardFrameLayout.Monster : CardFrameLayout.For(d.type);
            bool over = fullArt != null;
            // 모든 종류: 속성 전용 3D 프레임 (Frames/{frame}_{속성}, 예: frame_monster_fire · frame_field_water) 이 있으면 색 틴트 없이 그대로.
            // 새 카드·새 종류도 파일만 넣으면 자동 적용, 없으면 기본 프레임(+속성 틴트)으로 대체
            string frameName = L.frame;
            bool elementFrame = false;
            string ef = L.frame + "_" + d.element.ToString().ToLower();
            if (GameAssets.FrameSprite(ef) != null) { frameName = ef; elementFrame = true; }
            var frameSprite = over ? (GameAssets.FrameSprite(frameName + "_over") ?? GameAssets.FrameSprite(frameName)) : GameAssets.FrameSprite(frameName);
            frame.sprite = frameSprite;
            // 몬스터 대지 프레임은 이름판이 어두운 청동이라 밝은 글자 + 외곽선 (필드 대지는 밝은 사암판)
            bool darkPlate = elementFrame && (d.type == CardType.Monster || d.isElement) && d.element == Element.Earth && !over;
            Color fc = L.tintByElement && !elementFrame ? Color.Lerp(Color.white, CardData.ElementColor(d.element), L.tint) : L.frameTint;
            if (frameSprite == null) fc = new Color(0.8f, 0.8f, 0.85f, 0.25f);
            else if (over) fc.a = L.overFrameAlpha;
            frame.color = fc;

            if (over) art.rectTransform.Fill();
            else Place(art, L.art);
            Place(nameText, L.name);
            Place(typeText, L.type);
            Place(bodyText, L.body);
            Place(atkBadge, L.atk);
            Place(hpBadge, L.hp);
            atkPips.gameObject.SetActive(d.type == CardType.Monster || d.type == CardType.Hero);
            hpPips.gameObject.SetActive(d.type == CardType.Monster);
            Place(badge, L.badge);

            nameText.color = over ? L.overNameColor : darkPlate ? new Color(1f, 0.95f, 0.84f) : L.nameColor;
            var no = over ? L.overTextOutline : darkPlate ? new Color(0.12f, 0.06f, 0.02f, 1f) : L.nameOutline;
            nameOutline.effectColor = no;
            nameOutline.enabled = no.a > 0;
            typeText.color = over ? L.overNameColor : darkPlate ? new Color(1f, 0.95f, 0.84f) : L.typeColor;
            typeOutline.effectColor = darkPlate ? new Color(0.12f, 0.06f, 0.02f, 1f) : L.overTextOutline;
            typeOutline.enabled = over || darkPlate;
            bodyText.color = L.bodyColor;
            bodyOutline.effectColor = L.overTextOutline;
            bodyOutline.enabled = over;

            // 흐림 유리 패널: 같은 일러스트의 블러 버전을 카드 전체 기준으로 맞춰 영역만 보이게
            var regions = new[] { L.name, L.type, L.body };
            var blur = over ? GameAssets.Blurred(fullArt.texture) : null;
            for (int i = 0; i < 3; i++)
            {
                var panel = frostPanels[i];
                panel.gameObject.SetActive(over);
                if (!over) continue;
                var r = regions[i];
                float p = L.frostPad;
                var rr = new Vector4(Mathf.Max(0, r.x - p), Mathf.Max(0, r.y - p), Mathf.Min(1, r.z + p), Mathf.Min(1, r.w + p));
                panel.Anchor(rr.x, rr.y, rr.z, rr.w);
                float sw = rr.z - rr.x, sh = rr.w - rr.y;
                frostImages[i].texture = blur;
                frostImages[i].rectTransform.Anchor(-rr.x / sw, -rr.y / sh, (1 - rr.x) / sw, (1 - rr.y) / sh);
                frostTints[i].color = L.frost;
            }
        }

        /// <summary>atk / hp = 현재 수치 (-1 이면 기본값)</summary>
        public void Set(CardData d, int atk = -1, int hp = -1)
        {
            var fullArt = GameAssets.FullArtFor(d);
            ApplyLayout(d, fullArt);
            var ec = CardData.ElementColor(d.element);
            var sprite = fullArt ?? GameAssets.ArtFor(d);
            art.sprite = sprite;
            art.color = sprite != null ? Color.white : ec * 0.8f;
            nameText.text = d.name;
            typeText.text = d.TypeLine();
            bodyText.text = d.type == CardType.Monster ? (string.IsNullOrEmpty(d.text) ? "" : d.text + "\n") + d.DiceText() : d.text;
            rarityText.text = CardData.RarityLabel(d.rarity);
            rarityText.color = CardData.RarityColor(d.rarity);

            bool showAtk = d.type == CardType.Monster || d.type == CardType.Hero;
            atkBadge.gameObject.SetActive(showAtk);
            hpBadge.gameObject.SetActive(showAtk);
            int pipAtk = 0, pipHp = 0;
            if (d.type == CardType.Monster)
            {
                int hs = hp >= 0 ? hp : d.hp;
                hpText.text = hs.ToString();
                hpText.color = hs > d.hp ? new Color(0.5f, 1f, 0.5f) : hs < d.hp ? new Color(1f, 0.5f, 0.45f) : Color.white;
                int shown = atk >= 0 ? atk : d.atk;
                atkText.text = shown.ToString();
                atkText.color = shown > d.atk ? new Color(0.5f, 1f, 0.5f) : shown < d.atk ? new Color(1f, 0.5f, 0.45f) : Color.white;
                pipAtk = shown; pipHp = hs;
            }
            else if (d.type == CardType.Hero)
            {
                // 영웅: 검 = 반격 ATK, 하트 = HP (작은 아이콘은 ATK 만 — HP 는 수치가 커서 생략)
                atkText.text = d.heroAtk.ToString();
                atkText.color = Color.white;
                hpText.text = d.BattleHeroHp.ToString();
                hpText.color = Color.white;
                pipAtk = d.heroAtk;
            }
            // 작은 검: 왼쪽 테두리 / 작은 하트: 오른쪽 테두리
            LayoutPips(atkPipImgs, atkPips, "icon_sword_pip", pipAtk, 0.004f, 0.104f, 256f / 111f, 0.262f);
            LayoutPips(hpPipImgs, hpPips, "icon_heart", pipHp, 0.935f, 0.992f, 512f / 478f, 0.182f);

            badge.gameObject.SetActive(d.type != CardType.Monster && !d.isElement);   // 원소 카드는 배지 없음
            switch (d.type)
            {
                case CardType.Magic:
                    badgeText.text = d.speed == MagicSpeed.Set ? "세트" : "일반";
                    badge.color = d.speed == MagicSpeed.Set ? new Color(0.6f, 0.15f, 0.5f, 0.95f) : new Color(0.1f, 0.35f, 0.7f, 0.95f);
                    break;
                case CardType.Hero:
                    badgeText.text = "HERO"; badge.color = new Color(0.6f, 0.42f, 0.08f, 0.95f); break;
                case CardType.Field:
                    badgeText.text = "FIELD"; badge.color = new Color(0.15f, 0.45f, 0.25f, 0.95f); break;
            }
            if (finish != CardFinish.Normal) SetFinish(finish);
        }
    }
}
