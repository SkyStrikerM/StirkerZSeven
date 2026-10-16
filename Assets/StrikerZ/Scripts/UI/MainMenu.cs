using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 메인 허브 (홈 · 덱 · 상점 · 프로필). 좌측 세로 내비게이션 + 상단 프로필/재화 바.
    /// </summary>
    public partial class MainMenu : MonoBehaviour
    {
        public enum Tab { Home, Deck, Shop, Quest, Profile, Settings, Raid, Story, Challenge, Collection }

        public Action<DeckData> onDuel;
        /// <summary>시간 봉인된 대전 이어하기</summary>
        public Action onResumeSeal;
        /// <summary>훈련장: 편집 중인 덱으로 수련 골렘과 연습</summary>
        public Action<DeckData> onPractice;
        public Action onTitle;

        RectTransform root, contentArea;
        readonly Dictionary<Tab, RectTransform> pages = new Dictionary<Tab, RectTransform>();
        readonly Dictionary<Tab, NavItem> nav = new Dictionary<Tab, NavItem>();
        Tab current;
        readonly System.Random rng = new System.Random();

        static readonly Color Red = new Color(0.55f, 0.09f, 0.06f, 0.97f);
        static readonly Color Dark = UIKit.Dark;

        SettingsPage settings;
        QuestPage quests;
        Image questBadge;
        Text questBadgeText;

        void RefreshQuestBadge()
        {
            if (questBadge == null) return;
            int n = Quests.ClaimableCount + SevenPass.ClaimableCount + (Attendance.CanClaim ? 1 : 0);
            questBadge.gameObject.SetActive(n > 0);
            questBadgeText.text = n > 9 ? "9+" : n.ToString();
        }

        public bool IsOpen => root != null && root.gameObject.activeSelf;

        // ══════════════════════════ 구성 ══════════════════════════

        public void Build()
        {
            var go = new GameObject("MenuUI", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            UIKit.ScreenCanvas(go, 30);
            root = (RectTransform)go.transform;

            var bg = UIKit.Img(root, "Bg", new Color(0.42f, 0.42f, 0.52f), GameAssets.TexSprite("title_bg"));
            bg.rectTransform.Fill();
            var shade = UIKit.Img(root, "Shade", new Color(0.02f, 0.01f, 0.06f, 0.55f));
            shade.rectTransform.Fill();

            BuildSidebar();
            BuildTopBar();
            contentArea = UIKit.Rect("Content", root).Anchor(0, 0, 1, 1, 254, 24, 24, 118);
            pages[Tab.Home] = BuildHome();
            pages[Tab.Story] = BuildStory();
            pages[Tab.Raid] = BuildRaid();
            pages[Tab.Challenge] = BuildChallenge();
            pages[Tab.Deck] = BuildDeck();
            pages[Tab.Shop] = BuildShop();
            pages[Tab.Collection] = BuildCollection();
            quests = gameObject.AddComponent<QuestPage>();
            quests.toast = Toast;
            pages[Tab.Quest] = quests.Build(contentArea);
            BuildQuestTabs(pages[Tab.Quest]);
            pages[Tab.Profile] = BuildProfile();
            settings = gameObject.AddComponent<SettingsPage>();
            pages[Tab.Settings] = settings.Build(contentArea);
            BuildOpening();
            BuildToast();

            Profile.Changed += RefreshTop;
            root.gameObject.SetActive(false);
        }

        /// <summary>왼쪽 메뉴 한 칸: 아이콘 + 이름, 선택 시 금빛 바탕 + 왼쪽 막대</summary>
        class NavItem
        {
            public Image bg, border, bar, icon;
            public Text label;

            public void SetSelected(bool on)
            {
                bg.color = on ? new Color(0.46f, 0.32f, 0.08f, 0.96f) : new Color(1, 1, 1, 0.035f);
                border.color = on ? UIKit.Gold : new Color(1, 1, 1, 0.09f);
                bar.enabled = on;
                label.color = on ? Color.white : UIKit.Ink;
                icon.color = Color.white;
            }
        }

        static readonly Color NavPlay = new Color(0.95f, 0.45f, 0.35f), NavCollect = new Color(0.45f, 0.72f, 1f), NavMe = new Color(1f, 0.82f, 0.36f);

        /// <summary>메뉴 구역 머리말 (홈 카드와 같은 색 막대 + 제목 + 구분선)</summary>
        static void NavHeader(Transform side, string label, Color accent, float y)
        {
            var bar = UIKit.Img(side, "HeadBar_" + label, accent);
            bar.rectTransform.Place(new Vector2(0, 1), new Vector2(18, y - 6), new Vector2(5, 18), new Vector2(0, 1));
            var t = UIKit.Txt(side, "Head_" + label, label, 16, Color.Lerp(accent, Color.white, 0.35f), TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(30, y), new Vector2(180, 30), new Vector2(0, 1));
            var line = UIKit.Img(side, "HeadLine_" + label, new Color(accent.r, accent.g, accent.b, 0.25f));
            line.rectTransform.Place(new Vector2(0, 1), new Vector2(16, y - 30), new Vector2(198, 1), new Vector2(0, 1));
        }

        NavItem NavButton(Transform side, Tab tab, string label, string icon, float y)
        {
            var n = new NavItem();
            n.bg = UIKit.Img(side, "Nav_" + label, Color.white, GameAssets.Rounded, true);
            n.bg.raycastTarget = true;
            n.bg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(200, 60), new Vector2(0.5f, 1));
            n.border = UIKit.Img(n.bg.transform, "Border", Color.white, GameAssets.RoundedOutline, true);
            n.border.rectTransform.Fill();
            n.bar = UIKit.Img(n.bg.transform, "Bar", Color.white);
            n.bar.rectTransform.Anchor(0, 0.18f, 0, 0.82f, 3, 0, -8, 0);
            n.icon = UIKit.Img(n.bg.transform, "Icon", Color.white, GameAssets.TexSprite("nav_" + icon));
            n.icon.preserveAspect = true;
            n.icon.raycastTarget = false;
            n.icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(52, 52), new Vector2(0.5f, 0.5f));
            n.label = UIKit.Txt(n.bg.transform, "Label", label, 25, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            n.label.rectTransform.Anchor(0, 0, 1, 1, 76, 0, 8, 0);
            n.label.Shadow();
            var btn = n.bg.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.6f, 1.5f, 1.3f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = n.bg;
            btn.onClick.AddListener(() => { Sfx.Play("click", 0.8f); Show(tab); });
            n.SetSelected(false);
            nav[tab] = n;
            return n;
        }

        /// <summary>아래쪽 작은 버튼 (타이틀 · 게임 종료)</summary>
        static void NavSmall(Transform side, string label, string icon, Action onClick, float x)
        {
            var b = UIKit.Button(side, "", onClick, new Color(0.06f, 0.05f, 0.1f, 0.95f), 16, UIKit.Muted);
            b.bg.transform.Find("Border").GetComponent<Image>().color = new Color(1, 1, 1, 0.14f);
            b.rt.Place(new Vector2(0.5f, 0), new Vector2(x, 18), new Vector2(97, 72), new Vector2(0.5f, 0));
            var ic = UIKit.Img(b.bg.transform, "Icon", Color.white, GameAssets.TexSprite("nav_" + icon));
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            ic.rectTransform.Anchor(0, 0.34f, 1, 1, 0, 0, 0, 5);
            b.label.text = label;
            b.label.fontSize = 15;
            b.label.rectTransform.Anchor(0, 0, 1, 0.36f, 2, 4, 2, 0);
        }

        void BuildSidebar()
        {
            var side = UIKit.Img(root, "Sidebar", new Color(0.03f, 0.02f, 0.07f, 0.95f));
            side.rectTransform.Anchor(0, 0, 0, 1, 0, 0, -230, 0);
            var line = UIKit.Img(side.transform, "Line", new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.6f));
            line.rectTransform.Anchor(1, 0, 1, 1, -2, 0, 0, 0);

            var logo = UIKit.Img(side.transform, "Logo", Color.white, GameAssets.TexSprite("logo"));
            logo.preserveAspect = true;
            logo.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(206, 116), new Vector2(0.5f, 1));

            // 구역: 플레이 / 컬렉션 / 내 정보 — 홈 카드(대전 설정 · 내 덱)와 같은 색 머리말
            var groups = new[]
            {
                ("플레이", NavPlay, new[] { (Tab.Home, "홈", "home"), (Tab.Story, "스토리", "story"), (Tab.Raid, "레이드", "raid"), (Tab.Challenge, "도전", "challenge") }),
                ("컬렉션", NavCollect, new[] { (Tab.Deck, "덱", "deck"), (Tab.Collection, "도감", "collection"), (Tab.Shop, "상점", "shop") }),
                ("내 정보", NavMe, new[] { (Tab.Quest, "퀘스트", "quest"), (Tab.Profile, "프로필", "profile"), (Tab.Settings, "설정", "settings") }),
            };
            float y = -142;
            foreach (var (head, accent, items) in groups)
            {
                NavHeader(side.transform, head, accent, y);
                y -= 34;
                foreach (var (tab, label, icon) in items)
                {
                    var n = NavButton(side.transform, tab, label, icon, y);
                    n.bar.color = accent;
                    if (tab == Tab.Quest)
                    {
                        questBadge = UIKit.Img(n.bg.transform, "Badge", new Color(0.9f, 0.18f, 0.2f, 1f), GameAssets.Circle);
                        questBadge.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(32, 32), new Vector2(1, 0.5f));
                        questBadgeText = UIKit.Txt(questBadge.transform, "N", "", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                        questBadgeText.rectTransform.Fill();
                        questBadge.gameObject.SetActive(false);
                    }
                    y -= 66;
                }
                y -= 8;
            }

            var sl = UIKit.Img(side.transform, "BottomLine", new Color(1, 1, 1, 0.1f));
            sl.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(198, 1), new Vector2(0.5f, 0));
            NavSmall(side.transform, "타이틀", "title", () => onTitle?.Invoke(), -50);
            NavSmall(side.transform, "게임 종료", "quit", Quit, 50);
        }

        Image topAvatar;
        Text topName, gemText;
        readonly Text[] topCp = new Text[4];

        void BuildTopBar()
        {
            var top = UIKit.Frame(root, "TopBar", new Color(0.03f, 0.02f, 0.07f, 0.86f));
            top.rectTransform.Anchor(0, 1, 1, 1, 254, -100, 24, 16);

            topAvatar = AvatarCircle(top.transform, 68);
            ((RectTransform)topAvatar.transform.parent.parent).Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(68, 68), new Vector2(0, 0.5f));
            topName = UIKit.Txt(top.transform, "Name", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            topName.rectTransform.Anchor(0, 0, 0.4f, 1, 96, 0, 0, 0);

            // 재화: 오팔 + 등급별 CP (Common · Uncommon · Rare · Unique)
            var box = UIKit.Img(top.transform, "Wallet", new Color(0, 0, 0, 0.5f), GameAssets.Rounded, true);
            box.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(812, 58), new Vector2(1, 0.5f));
            var gem = UIKit.Img(box.transform, "Opal", Color.white, GameAssets.TexSprite("opal"));
            gem.preserveAspect = true;
            gem.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(32, 0), new Vector2(46, 46), new Vector2(0.5f, 0.5f));
            var gl = UIKit.Txt(box.transform, "Label", "오팔", 16, UIKit.Muted, TextAnchor.MiddleLeft);
            gl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(50, 40), new Vector2(0, 0.5f));
            gemText = UIKit.Txt(box.transform, "Gems", "", 27, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            gemText.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(222, 0), new Vector2(120, 44), new Vector2(1, 0.5f));
            var sep = UIKit.Img(box.transform, "Sep", new Color(1, 1, 1, 0.15f));
            sep.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(238, 0), new Vector2(1, 38));
            var cpl = UIKit.Txt(box.transform, "CpLabel", "CP", 16, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            cpl.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(252, 0), new Vector2(30, 40), new Vector2(0, 0.5f));
            string[] icons = { "cp_common", "cp_uncommon", "cp_rare", "cp_unique" };
            Color[] tint = { new Color(0.9f, 0.9f, 0.95f), new Color(0.6f, 0.82f, 1f), new Color(1f, 0.82f, 0.45f), new Color(1f, 0.55f, 0.4f) };
            for (int i = 0; i < 4; i++)
            {
                float x = 286 + i * 130;
                var ic = UIKit.Img(box.transform, "Cp" + i, Color.white, GameAssets.TexSprite(icons[i]) ?? GameAssets.TexSprite("cp_rare"));
                ic.preserveAspect = true;
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(x + 22, 0), new Vector2(44, 44), new Vector2(0.5f, 0.5f));
                topCp[i] = UIKit.Txt(box.transform, "CpN" + i, "", 22, tint[i], TextAnchor.MiddleLeft, FontStyle.Bold);
                topCp[i].rectTransform.Place(new Vector2(0, 0.5f), new Vector2(x + 48, 0), new Vector2(80, 40), new Vector2(0, 0.5f));
            }
        }

        static Image AvatarCircle(Transform parent, float size) => UIKit.AvatarCircle(parent, size);

        static void SetAvatar(Image img, string id)
        {
            var s = GameAssets.Avatar(id);
            img.sprite = s;
            img.color = s != null ? Color.white : new Color(0.35f, 0.3f, 0.5f);
        }

        void RefreshTop()
        {
            if (topName == null) return;
            var d = Profile.Data;
            SetAvatar(topAvatar, d.avatar);
            topName.text = d.nickname;
            gemText.text = d.opal.ToString("N0");
            for (int i = 0; i < topCp.Length; i++) topCp[i].text = (d.cp != null && i < d.cp.Count ? d.cp[i] : 0).ToString("N0");
            RefreshQuestBadge();
        }

        // ══════════════════════════ 흐름 ══════════════════════════

        public void Show(Tab tab)
        {
            root.gameObject.SetActive(true);
            CardZoom.Close();
            CloseShowcaseEdit();
            if (oddsRoot != null) oddsRoot.gameObject.SetActive(false);
            Voice.Stop();   // 다른 화면으로 가면 하던 대사는 끊는다
            // 상점은 전용 BGM (VARCO Sound), 상점을 떠나면 메뉴 BGM 으로
            Sfx.MenuBgm = tab == Tab.Shop ? "bgm_shop" : "bgm_menu";
            if (tab == Tab.Shop || current == Tab.Shop) Sfx.PlayBgm(Sfx.MenuBgm, 1f);
            current = tab;
            foreach (var kv in pages)
            {
                if (kv.Key == Tab.Deck) SetDeckPageVisible(tab == Tab.Deck);
                else kv.Value.gameObject.SetActive(kv.Key == tab);
            }
            foreach (var kv in nav) kv.Value.SetSelected(kv.Key == tab);
            RefreshTop();
            switch (tab)
            {
                case Tab.Home: RefreshHome(); break;
                case Tab.Raid: RefreshRaid(); break;
                case Tab.Challenge: RefreshChallenge(); break;
                case Tab.Story: RefreshStory(); break;
                case Tab.Deck: OpenDeckPage(); break;
                case Tab.Shop: OnEnterShop(); RefreshShop(); break;
                case Tab.Profile: RefreshProfile(); break;
                case Tab.Collection: RefreshCollection(); break;
                case Tab.Settings: settings.Refresh(); break;
                case Tab.Quest: ShowQuestTab(questTab); break;
            }
            if (tab != Tab.Deck) WarmDeckPage();
        }

        public void Hide()
        {
            CardZoom.Close();
            Voice.Stop();
            if (Sfx.MenuBgm != "bgm_menu") { Sfx.MenuBgm = "bgm_menu"; Sfx.PlayBgm("bgm_menu"); }   // 상점에서 타이틀·전투로 나갈 때
            root.gameObject.SetActive(false);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ══════════════════════════ 공용 ══════════════════════════

        Text toastText;
        CanvasGroup toastGroup;
        Coroutine toastCo;

        void BuildToast()
        {
            var bg = UIKit.Frame(root, "Toast", new Color(0.05f, 0.03f, 0.08f, 0.95f));
            bg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(115, 60), new Vector2(760, 70), new Vector2(0.5f, 0));
            OverlayCanvas(bg.rectTransform, 42);
            toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastText = UIKit.Txt(bg.transform, "T", "", 24, Color.white);
            toastText.rectTransform.Fill();
            toastGroup.alpha = 0;
        }

        public void Toast(string msg)
        {
            toastText.text = msg;
            toastGroup.transform.SetAsLastSibling();
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastRoutine());
        }

        IEnumerator ToastRoutine()
        {
            toastGroup.alpha = 1;
            yield return new WaitForSecondsRealtime(1.8f);
            for (float t = 1; t > 0; t -= Time.unscaledDeltaTime * 3) { toastGroup.alpha = t; yield return null; }
            toastGroup.alpha = 0;
        }

        /// <summary>카드 한 칸 (CardFace + 클릭/호버). onDoubleClick 이 있으면 클릭 = onClick(선택), 더블클릭 = onDoubleClick (덱 넣기/빼기)</summary>
        static RectTransform CardCell(Transform parent, CardData d, float w, float h, float fs, Action onClick, Action onHover, Action onRightClick = null, Action onDoubleClick = null)
            => CardCell(parent, d, w, h, fs, onClick, onHover, onRightClick, onDoubleClick, out _);

        static RectTransform CardCell(Transform parent, CardData d, float w, float h, float fs, Action onClick, Action onHover, Action onRightClick, Action onDoubleClick, out CardFace face)
        {
            var cell = UIKit.Rect("Cell_" + d.id, parent);
            cell.sizeDelta = new Vector2(w, h);
            cell.HitArea();
            face = CardFace.Build(cell, w, h, fs);
            face.root.Fill();
            face.glow.gameObject.SetActive(false);
            face.Set(d);
            face.SetFinish(Profile.BestFinish(d.id));
            if (onClick != null && onDoubleClick == null)
            {
                var btn = cell.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => onClick());
            }
            if (onHover != null || onRightClick != null || onDoubleClick != null)
            {
                // EventTrigger 는 스크롤 이벤트까지 먹어 버려서 전용 컴포넌트 사용
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onEnter = onHover;
                ev.onRightClick = onRightClick;
                if (onDoubleClick != null) { ev.onLeftClick = onClick; ev.onDoubleClick = onDoubleClick; }
            }
            return cell;
        }

        static Text Badge(Transform cell, string text, Color bg, Vector4 anchor, int size = 18)
        {
            var b = UIKit.Img(cell, "Badge", bg, GameAssets.Rounded, true);
            b.rectTransform.Anchor(anchor.x, anchor.y, anchor.z, anchor.w);
            var t = UIKit.Txt(b.transform, "T", text, size, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill();
            return t;
        }

        // ══════════════════════════ 홈 ══════════════════════════

        Image homeHero;
        Text homeHeroName, homeDeckName, homeDeckState, homeDeckCount;
        Image homeCase;
        UIKit.Btn duelBtn;

        static readonly Color CardFill = new Color(0.035f, 0.028f, 0.07f, 0.94f);
        static Sprite hGrad;
        /// <summary>왼쪽 불투명 → 오른쪽 투명 가로 그라데이션</summary>
        static Sprite HGrad
        {
            get
            {
                if (hGrad != null) return hGrad;
                var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < 256; x++) tex.SetPixel(x, 0, new Color(1, 1, 1, Mathf.SmoothStep(1f, 0f, x / 255f)));
                tex.Apply();
                return hGrad = Sprite.Create(tex, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>홈 구역 카드: 어두운 판 + 색 테두리 + 머리말(색 막대 · 제목 · 구분선). 내용은 반환된 판의 머리말 아래(위 54px)에</summary>
        static Image HomeCard(Transform parent, string name, string label, Color accent, string sub = null)
        {
            var card = UIKit.Frame(parent, name, CardFill);
            card.transform.Find("Border").GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.42f);
            var bar = UIKit.Img(card.transform, "Accent", accent);
            bar.rectTransform.Anchor(0, 1, 0, 1, 22, -40, -28, 16);
            var t = UIKit.Txt(card.transform, "Head", label, 21, Color.Lerp(accent, Color.white, 0.35f), TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Anchor(0, 1, 1, 1, 38, -44, 20, 12);
            if (sub != null)
            {
                var s = UIKit.Txt(card.transform, "HeadSub", sub, 15, UIKit.Muted, TextAnchor.MiddleRight);
                s.rectTransform.Anchor(0, 1, 1, 1, 38, -44, 22, 12);
            }
            var line = UIKit.Img(card.transform, "Line", new Color(accent.r, accent.g, accent.b, 0.25f));
            line.rectTransform.Anchor(0, 1, 1, 1, 18, -54, 18, 53);
            return card;
        }

        RectTransform BuildHome()
        {
            var page = UIKit.Rect("Home", contentArea).Fill();
            const float split = 0.635f;
            var gold = UIKit.Gold;

            // ── 히어로 쇼케이스 (좌상) ── 배경 아트 + 영웅 초상 + DUEL 타이틀 + 출전 덱
            var show = UIKit.Frame(page, "Showcase", new Color(0.02f, 0.015f, 0.04f, 1f));
            show.rectTransform.Anchor(0, 0.3f, split, 1, 0, 9, 10, 0);
            var clip = UIKit.Rect("Clip", show.transform).Anchor(0, 0, 1, 1, 4, 4, 4, 4);
            clip.gameObject.AddComponent<RectMask2D>();
            var art = UIKit.Img(clip, "Art", Color.white, GameAssets.TexSprite("home_duel_bg") ?? GameAssets.TexSprite("title_bg"));
            art.rectTransform.Fill();
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.sprite != null ? art.sprite.rect.width / art.sprite.rect.height : 16f / 9f;
            var shade = UIKit.Img(clip, "Shade", new Color(0.015f, 0.01f, 0.04f, 0.92f), HGrad);
            shade.rectTransform.Anchor(0, 0, 0.78f, 1);
            var floor = UIKit.Img(clip, "Floor", new Color(0, 0, 0, 0.35f));
            floor.rectTransform.Anchor(0, 0, 1, 0.26f);

            // 영웅 초상 (오른쪽, 금테 + 이름판)
            var port = UIKit.Img(show.transform, "Portrait", gold, GameAssets.Rounded, true);
            var prt = port.rectTransform;
            prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(1, 0.5f);
            prt.sizeDelta = new Vector2(400, -52);
            prt.anchoredPosition = new Vector2(-30, 0);
            var pf = port.gameObject.AddComponent<AspectRatioFitter>();
            pf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            pf.aspectRatio = 750f / 1050f;
            var pclip = UIKit.Rect("Clip", port.transform).Anchor(0, 0, 1, 1, 4, 4, 4, 4);
            pclip.gameObject.AddComponent<RectMask2D>();
            homeHero = UIKit.Img(pclip, "Hero", Color.white);
            homeHero.rectTransform.Fill();
            var plate = UIKit.Img(pclip, "Plate", new Color(0.02f, 0.01f, 0.04f, 0.82f));
            plate.rectTransform.Anchor(0, 0, 1, 0, 0, 0, 0, -58);
            homeHeroName = UIKit.Txt(plate.transform, "Name", "", 22, gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            homeHeroName.rectTransform.Fill();
            var tag = UIKit.Img(port.transform, "Tag", new Color(0.55f, 0.09f, 0.06f, 0.97f), GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, 14), new Vector2(128, 32), new Vector2(0.5f, 1));
            UIKit.Txt(tag.transform, "T", "LEADER", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();

            // 타이틀 · 부제
            var title = UIKit.Txt(show.transform, "Title", "DUEL", 132, gold, TextAnchor.LowerLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 0.5f, 0.6f, 0.86f, 52, 0, 0, 0);
            title.Outline(new Color(0.25f, 0.08f, 0, 0.9f), 3).Shadow(new Color(0, 0, 0, 0.9f), 5);
            var sub = UIKit.Txt(show.transform, "Sub", "CPU 대전  ·  선택한 덱으로 싸웁니다", 26, Color.white, TextAnchor.UpperLeft);
            sub.rectTransform.Anchor(0, 0.38f, 0.6f, 0.5f, 58, 0, 0, 6);
            sub.Shadow(new Color(0, 0, 0, 0.9f), 2);

            // 출전 덱
            var dlabel = UIKit.Txt(show.transform, "DeckLabel", "출전 덱", 17, new Color(0.75f, 0.7f, 0.85f), TextAnchor.LowerLeft, FontStyle.Bold);
            dlabel.rectTransform.Anchor(0, 0, 0.6f, 0, 58, 104, 0, -128);
            var dbar = UIKit.Img(show.transform, "DeckBar", gold);
            dbar.rectTransform.Anchor(0, 0, 0, 0, 44, 30, -48, -126);
            homeDeckName = UIKit.Txt(show.transform, "DeckName", "", 36, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            homeDeckName.rectTransform.Anchor(0, 0, 0.6f, 0, 58, 58, 0, -104);
            homeDeckName.Shadow(new Color(0, 0, 0, 0.9f), 2);
            homeDeckState = UIKit.Txt(show.transform, "DeckState", "", 20, UIKit.Ink, TextAnchor.UpperLeft);
            homeDeckState.rectTransform.Anchor(0, 0, 0.6f, 0, 58, 24, 0, -58);
            homeDeckState.Shadow(new Color(0, 0, 0, 0.9f), 2);

            // 시간 봉인: 끝나지 않은 대전이 있으면 이어하기 / 버리기 (쇼케이스 위쪽 띠)
            var seal = UIKit.Img(show.transform, "Seal", new Color(0.04f, 0.1f, 0.18f, 0.95f), GameAssets.Rounded, true);
            sealBar = seal.rectTransform;
            sealBar.Anchor(0, 1, 1, 1, 22, -96, 470, 18);
            var sb = UIKit.Img(sealBar, "Border", new Color(0.45f, 0.75f, 1f, 0.7f), GameAssets.RoundedOutline, true);
            sb.rectTransform.Fill();
            sealText = UIKit.Txt(sealBar, "T", "", 16, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleLeft);
            sealText.rectTransform.Anchor(0, 0, 1, 1, 16, 0, 214, 0);
            sealText.lineSpacing = 1.1f;
            var resume = UIKit.Button(sealBar, "⌛ 이어하기", () => onResumeSeal?.Invoke(), new Color(0.1f, 0.3f, 0.5f, 0.97f), 18, Color.white);
            resume.rt.Place(new Vector2(1, 0.5f), new Vector2(-86, 0), new Vector2(120, 52), new Vector2(1, 0.5f));
            var drop = UIKit.Button(sealBar, "버리기", () => { TimeSeal.Clear(); RefreshHome(); Toast("봉인된 대전을 버렸습니다"); }, Dark, 16, UIKit.Muted);
            drop.rt.Place(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(70, 52), new Vector2(1, 0.5f));

            // ── 대전 설정 (좌하) ── 난이도 + 듀얼 시작
            var setup = HomeCard(page, "Setup", "대전 설정", new Color(0.95f, 0.45f, 0.35f), "CPU 난이도");
            setup.rectTransform.Anchor(0, 0, split, 0.3f, 0, 0, 10, 9);
            levelBtns.Clear();
            for (int i = 0; i < 3; i++)
            {
                int lv = i;
                var b = UIKit.Button(setup.transform, GameManager.LevelName((CpuLevel)i), () => { Profile.Data.cpuLevel = lv; Profile.Save(); RefreshLevel(); }, Dark, 25);
                b.rt.Place(new Vector2(0, 0), new Vector2(30 + i * 176, 100), new Vector2(166, 62), new Vector2(0, 0));
                levelBtns.Add(b);
            }
            levelDesc = UIKit.Txt(setup.transform, "LevelDesc", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            levelDesc.rectTransform.Anchor(0, 0, 1, 0, 32, 22, 470, -88);
            duelBtn = UIKit.Button(setup.transform, "듀얼 시작  ▶", StartDuel, Red, 42, Color.white);
            duelBtn.rt.Anchor(1, 0, 1, 1, -440, 22, 26, 70);
            duelBtn.label.Outline(new Color(0.2f, 0.02f, 0, 0.8f), 2);

            // ── 오른쪽 열: 신규 팩 (쇼케이스와 같은 높이) / 내 덱 (대전 설정과 같은 높이) ──
            var col = UIKit.Rect("Right", page).Anchor(split, 0, 1, 1, 10, 0, 0, 0);

            var pack = BoosterPack.Packs[0];
            var promo = HomeCard(col, "Promo", "신규 부스터팩", gold, "NEW");
            promo.rectTransform.Anchor(0, 0.3f, 1, 1, 0, 9, 0, 0);
            var packArt = UIKit.Img(promo.transform, "Pack", Color.white, GameAssets.TexSprite(pack.art) ?? GameAssets.TexSprite("card_back"));
            packArt.preserveAspect = true;
            packArt.rectTransform.Anchor(0.03f, 0.17f, 0.5f, 1, 0, 0, 0, 66);
            var pt = UIKit.Txt(promo.transform, "T", $"{pack.code} 부스터팩", 34, gold, TextAnchor.UpperLeft, FontStyle.Bold);
            pt.rectTransform.Anchor(0.53f, 0.6f, 1, 1, 0, 0, 18, 80);
            // 고등급 확률만 요약 (숫자는 팩 데이터에서)
            string top = string.Join("\n", pack.Breakdown().Where(r => r.rarity >= Rarity.Rare).Select(r => $"<color=#a9a3b8>{CardData.RarityLabel(r.rarity)}</color>  {BoosterPack.Pct(r.chance)}"));
            var pd = UIKit.Txt(promo.transform, "D", $"{pack.name}\n<size=17>1팩 {pack.cardsPerPack}장\n{top}</size>", 21, UIKit.Ink, TextAnchor.UpperLeft);
            pd.rectTransform.Anchor(0.53f, 0.2f, 1, 0.62f, 0, 0, 18, 0);
            pd.lineSpacing = 1.1f;
            var go = UIKit.Button(promo.transform, "상점으로", () => Show(Tab.Shop), Red, 25, Color.white);
            go.rt.Anchor(0, 0, 1, 0, 22, 22, 22, -96);

            var deck = HomeCard(col, "DeckBox", "내 덱", new Color(0.45f, 0.72f, 1f));
            deck.rectTransform.Anchor(0, 0, 1, 0.3f, 0, 0, 0, 9);
            // 장착한 덱케이스 (클릭 = 꾸미기 상점 덱케이스 탭)
            homeCase = UIKit.Img(deck.transform, "Case", Color.white, null);
            homeCase.preserveAspect = true;
            homeCase.rectTransform.Anchor(0, 0, 0.26f, 1, 14, 16, 0, 62);
            homeCase.raycastTarget = true;
            var cb = homeCase.gameObject.AddComponent<Button>();
            cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(() => { Show(Tab.Shop); TestOpenCosmetics(CosmeticKind.Case); });
            homeDeckCount = UIKit.Txt(deck.transform, "C", "", 20, UIKit.Ink, TextAnchor.MiddleLeft);
            homeDeckCount.rectTransform.Anchor(0.26f, 0, 0.62f, 1, 14, 16, 0, 62);
            homeDeckCount.lineSpacing = 1.15f;
            var db = UIKit.Button(deck.transform, "덱 편집", () => Show(Tab.Deck), new Color(0.08f, 0.16f, 0.3f, 0.97f), 24, Color.white);
            db.rt.Anchor(0.63f, 0, 1, 1, 0, 22, 22, 70);

            return page;
        }

        readonly List<UIKit.Btn> levelBtns = new List<UIKit.Btn>();
        RectTransform sealBar;
        Text sealText;
        Text levelDesc;

        void RefreshLevel()
        {
            int cur = Mathf.Clamp(Profile.Data.cpuLevel, 0, 2);
            for (int i = 0; i < levelBtns.Count; i++) levelBtns[i].SetSelected(i == cur);
            if (levelDesc != null) levelDesc.text = $"<color=#ffd77a>{GameManager.LevelName((CpuLevel)cur)}</color>  —  {GameManager.LevelDesc((CpuLevel)cur)}";
        }

        void RefreshHome()
        {
            RefreshLevel();
            bool hasSeal = TimeSeal.Exists;
            sealBar.gameObject.SetActive(hasSeal);
            if (hasSeal) sealText.text = "<b>⌛ 시간 봉인된 대전</b>\n<size=15>" + TimeSeal.Describe(TimeSeal.Load()) + "</size>";
            var d = Profile.Data;
            var deck = Profile.ActiveDeck;
            var hero = CardDB.Get(deck.heroId);
            homeHero.sprite = hero != null ? (GameAssets.FullArtFor(hero) ?? GameAssets.ArtFor(hero)) : null;
            homeHero.color = homeHero.sprite != null ? Color.white : new Color(0.2f, 0.17f, 0.3f);
            homeHeroName.text = hero?.name ?? "영웅 없음";
            homeDeckName.text = deck.name;
            string err = DeckRules.Validate(deck);
            homeDeckState.text = err == null
                ? $"메인 {deck.cards.Count}장 · 엑스트라 {deck.extra?.Count ?? 0}장  ·  <color=#8f8>사용 가능</color>"
                : $"<color=#f98>{err}</color>";
            duelBtn.Interactable = err == null;
            homeDeckCount.text = $"<b><color=#ffd77a>{deck.name}</color></b>\n<color=#a9a3b8>보유 덱</color>  {d.decks.Count}개\n<color=#a9a3b8>영웅</color>  {hero?.name ?? "-"}";
            homeCase.sprite = Cosmetics.Icon(Cosmetics.Equipped(CosmeticKind.Case));
            homeCase.enabled = homeCase.sprite != null;
        }

        void StartDuel()
        {
            var deck = Profile.ActiveDeck;
            string err = DeckRules.Validate(deck);
            if (err != null) { Toast(err); Show(Tab.Deck); return; }
            onDuel?.Invoke(deck);
        }

        // 덱 페이지: MainMenu.Deck.cs

        // ══════════════════════════ 상점 ══════════════════════════

        // 상점 화면: MainMenu.Shop.cs

        public void Buy(BoosterPack pack, int count)
        {
            if (!Profile.Spend(pack.price * count)) { Toast("오팔이 부족합니다"); return; }
            OpenPacks(pack, count);
        }

        /// <summary>팩 개봉 (구매 · 세븐 패스 · 출석 보상 공용, 오팔은 호출 측에서)</summary>
        public void OpenPacks(BoosterPack pack, int count)
        {
            var packs = new List<List<(CardData card, bool isNew, CardFinish finish)>>();
            for (int i = 0; i < count; i++)
            {
                var cards = pack.Open(rng);
                if (i == 0 && TestForceRarity.HasValue)
                {
                    var pool = CardDB.All.Where(c => c.rarity == TestForceRarity.Value).ToList();
                    if (pool.Count > 0) cards[cards.Count - 1] = pool[rng.Next(pool.Count)];
                    TestForceRarity = null;
                }
                var list = new List<(CardData, bool, CardFinish)>();
                for (int k = 0; k < cards.Count; k++)
                {
                    var c = cards[k];
                    var fin = BoosterPack.RollFinish(rng);
                    if (i == 0 && TestForceFinish != null && k < TestForceFinish.Length) fin = TestForceFinish[k];
                    bool isNew = Profile.Owned(c.id) == 0;
                    Profile.AddCard(c.id, 1, true, fin);
                    if (fin == CardFinish.Seven) Quests.Add(Stat.SevenGot);
                    if (fin == CardFinish.StrikerZ) Quests.Add(Stat.StrikerZGot);
                    if (c.rarity == Rarity.Unique) Quests.Add(Stat.MythicGot);
                    list.Add((c, isNew, fin));
                }
                if (i == 0) TestForceFinish = null;
                packs.Add(list);
            }
            Profile.Data.packsOpened += count;
            Quests.Add(Stat.PackOpened, count);
            Profile.Save();
            RefreshShop();
            StartCoroutine(PlayOpening(pack, packs));
        }

        // ── 팩 개봉 연출 (PackOpening) ──
        PackOpening opening;

        void BuildOpening()
        {
            opening = gameObject.AddComponent<PackOpening>();
            opening.auto = AutoTest.Enabled;
            opening.Build(root);
        }

        public PackOpening Opening => opening;
        public void TestContinueOpening() => opening.Continue();

        /// <summary>테스트용: 다음 구매 첫 팩의 마지막 장을 이 등급으로 고정</summary>
        public static Rarity? TestForceRarity;
        /// <summary>테스트용: 다음 구매 첫 팩의 카드별 가공 고정</summary>
        public static CardFinish[] TestForceFinish;

        IEnumerator PlayOpening(BoosterPack pack, List<List<(CardData card, bool isNew, CardFinish finish)>> packs)
        {
            yield return opening.Play(pack, packs);
            RefreshTop();
            RefreshShop();
        }

        // ══════════════════════════ 프로필 ══════════════════════════

        Image profileAvatar;
        InputField nickInput;
        Text profileStats;
        RectTransform avatarGrid;
        UIKit.Btn resetBtn;
        bool resetArmed;

        RectTransform BuildProfile()
        {
            var page = UIKit.Rect("Profile", contentArea).Fill();

            var left = UIKit.Frame(page, "Me", Dark);
            left.rectTransform.Anchor(0, 0, 0.5f, 1, 0, 0, 12, 0);
            profileAvatar = AvatarCircle(left.transform, 180);
            ((RectTransform)profileAvatar.transform.parent.parent).Place(new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(180, 180), new Vector2(0.5f, 1));
            var nl = UIKit.Txt(left.transform, "NickLabel", "닉네임", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            nl.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-190, -212), new Vector2(200, 34), new Vector2(0, 1));
            nickInput = UIKit.Input(left.transform, "", "닉네임 (최대 12자)", 28, 12);
            nickInput.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), new Vector2(-70, -250), new Vector2(420, 60), new Vector2(0.5f, 1));
            var set = UIKit.Button(left.transform, "변경", () =>
            {
                string v = nickInput.text.Trim();
                if (v.Length < 2) { Toast("닉네임은 2자 이상이어야 합니다"); return; }
                Profile.Data.nickname = v;
                Profile.Save();
                Toast("닉네임을 변경했습니다");
                RefreshProfile();
            }, Red, 24, Color.white);
            set.rt.Place(new Vector2(0.5f, 1), new Vector2(210, -250), new Vector2(140, 60), new Vector2(0.5f, 1));
            var al = UIKit.Txt(left.transform, "AvatarLabel", "아바타", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            al.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-235, -326), new Vector2(200, 34), new Vector2(0, 1));
            avatarGrid = UIKit.Rect("Avatars", left.transform);
            avatarGrid.Place(new Vector2(0.5f, 1), new Vector2(0, -364), new Vector2(600, 226), new Vector2(0.5f, 1));
            var gg = avatarGrid.gameObject.AddComponent<GridLayoutGroup>();
            gg.cellSize = new Vector2(104, 104);
            gg.spacing = new Vector2(18, 18);
            gg.childAlignment = TextAnchor.UpperCenter;

            // 듀얼 액세서리 (루콘의 공방에서 산 슬리브·덱케이스·코인·주사위)
            BuildAccessories(left.transform, -606);

            var right = UIKit.Frame(page, "Stats", Dark);
            right.rectTransform.Anchor(0.5f, 0, 1, 1, 12, 0, 0, 0);
            var st = UIKit.Txt(right.transform, "T", "기록", 40, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            st.rectTransform.Anchor(0, 1, 1, 1, 40, -74, 250, 28);
            st.Section(UIKit.SecMe);
            profileStats = UIKit.Txt(right.transform, "S", "", 28, UIKit.Ink, TextAnchor.UpperLeft);
            profileStats.rectTransform.Anchor(0, 0.53f, 1, 0.85f, 44, 0, 30, 0);
            profileStats.lineSpacing = 1.3f;

            var chron = UIKit.Button(right.transform, "듀얼 연대기", OpenChronicle, new Color(0.32f, 0.2f, 0.05f, 0.96f), 22, Color.white);
            chron.rt.Place(new Vector2(1, 1), new Vector2(-30, -26), new Vector2(200, 54), new Vector2(1, 1));
            BuildShowcase(right.transform);
            resetBtn = UIKit.Button(right.transform, "데이터 초기화", ResetData, new Color(0.12f, 0.05f, 0.05f, 0.95f), 22, new Color(1f, 0.6f, 0.55f));
            resetBtn.rt.Anchor(0.6f, 0.04f, 0.95f, 0.13f);
            var path = UIKit.Txt(right.transform, "Path", "저장 위치: 로컬 (스팀 클라우드/모바일 백업 연동 예정)", 16, UIKit.Muted, TextAnchor.LowerLeft);
            path.rectTransform.Anchor(0, 0.04f, 0.58f, 0.13f, 40, 0, 0, 0);
            return page;
        }

        void RefreshProfile()
        {
            resetArmed = false;
            resetBtn.label.text = "데이터 초기화";
            var d = Profile.Data;
            SetAvatar(profileAvatar, d.avatar);
            nickInput.SetTextWithoutNotify(d.nickname);
            UIKit.Clear(avatarGrid);
            foreach (var id in Profile.Avatars)
            {
                string aid = id;
                var cell = UIKit.Rect("A_" + id, avatarGrid);
                var img = AvatarCircle(cell, 104);
                var ring = (RectTransform)img.transform.parent.parent;
                ring.Fill();
                ring.GetComponent<Image>().color = d.avatar == id ? UIKit.Gold : new Color(0.3f, 0.27f, 0.36f);
                SetAvatar(img, id);
                ring.GetComponent<Image>().raycastTarget = true;
                var btn = ring.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { Profile.Data.avatar = aid; Profile.Save(); RefreshProfile(); });
            }
            int cards = d.collection.Sum(c => c.n);
            // 전적은 온라인 대전만 (CPU 대전 기록은 듀얼 연대기)
            profileStats.text =
                "온라인 전적   " + (d.wins + d.losses > 0 ? $"{d.wins}승  {d.losses}패" : "<color=#a9a3b8>온라인 대전 준비 중</color>") + "\n" +
                $"도감          {Profile.OwnedKinds} / {CardDB.All.Count}종\n" +
                $"보유 카드     {cards}장\n" +
                $"보유 덱       {d.decks.Count}개\n" +
                $"오팔          {d.opal:N0}";
            RefreshShowcase();
            RefreshAccessories();
        }

        void ResetData()
        {
            if (!resetArmed) { resetArmed = true; resetBtn.label.text = "정말 초기화?"; Toast("한 번 더 누르면 모든 데이터가 초기화됩니다"); return; }
            Profile.Reset();
            Toast("데이터를 초기화했습니다");
            RefreshProfile();
            RefreshTop();
        }
    }
}
