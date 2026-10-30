using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>게임 화면 UI: 영웅 HP, 타임/페이즈, 버튼, 로그, 카드 확대, 선택창, 배너, 전투 결과</summary>
    public partial class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        GameManager g;
        Canvas canvas;
        RectTransform root;

        // 상단
        Text timeText, phaseText, turnText;
        /// <summary>동시 룰 턴 아이콘: 내 공격 턴 = 쌍검, 수비 턴 = 방패 (VARCO Textures/Fx/turn_attack · turn_defense)</summary>
        Image turnIcon;
        Image timePanel;
        // 영웅 패널
        class HeroPanel
        {
            public Image portrait, hpFill, hpFlash;
            public Text name, hp, info, skill;
            public RectTransform rt;
        }
        HeroPanel humanPanel, cpuPanel;
        // 필드 정보
        Text fieldName, fieldText, statusText;
        // 버튼
        UIKit.Btn battleBtn, endBtn, heroBtn;
        // 로그
        Text logText;
        Image logBg;
        readonly List<string> logLines = new List<string>();
        // 카드 확대
        RectTransform detailRoot;
        CardFace detailFace;
        Text detailExtra;
        CardInstance hoverCard;
        // 행동 메뉴
        RectTransform actionMenu;
        // 선택창
        RectTransform choicePanel;
        Text choicePrompt;
        RectTransform choiceList;
        // 배너
        RectTransform bannerRoot;
        Text bannerMain, bannerSub;
        CanvasGroup bannerGroup;
        // 전투
        RectTransform clashRoot;
        Text clashLeft, clashRight, clashMid;
        CanvasGroup clashGroup;
        // 게임 오버
        RectTransform gameOverRoot;
        Text gameOverTitle;
        public Action onRematch, onTitle;

        void Awake() { I = this; }

        public void Build()
        {
            canvas = UIKit.ScreenCanvas(gameObject, 10);
            root = (RectTransform)transform;

            BuildTop();
            humanPanel = BuildHeroPanel(false);
            cpuPanel = BuildHeroPanel(true);
            BuildField();
            BuildButtons();
            BuildExtras();
            BuildFieldButton();
            BuildLog();
            BuildRaid();
            BuildStory();
            BuildDetail();
            BuildChoice();
            BuildTargetMode();
            BuildBattleMode();
            BuildClash();
            BuildBanner();
            BuildGameOver();
            BuildPileTip();
            BuildPause();
            Show(false);
        }

        public void Show(bool on) => gameObject.SetActive(on);

        // ══════════════════════════ 구성 ══════════════════════════

        void BuildTop()
        {
            timePanel = UIKit.Frame(root, "TimePanel", PanelFill);
            timePanel.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(430, 118));
            timeText = UIKit.Txt(timePanel.transform, "Time", "TIME 1", 54, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            timeText.rectTransform.Anchor(0, 0.42f, 1, 1, 0, 0, 0, 4);
            timeText.Shadow();
            phaseText = UIKit.Txt(timePanel.transform, "Phase", "", 24, UIKit.Ink);
            phaseText.rectTransform.Anchor(0, 0.06f, 1, 0.45f);
            turnIcon = UIKit.Img(timePanel.transform, "TurnIcon", Color.white);
            turnIcon.preserveAspect = true;
            turnIcon.raycastTarget = false;
            turnIcon.rectTransform.Anchor(0.04f, 0.03f, 0.2f, 0.5f);
            turnIcon.enabled = false;
            turnText = UIKit.Txt(root, "Turn", "", 22, UIKit.Muted);
            turnText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(600, 30));
            turnText.Shadow();
        }

        HeroPanel BuildHeroPanel(bool top)
        {
            var hp = new HeroPanel();
            var bg = UIKit.Frame(root, top ? "CpuPanel" : "HumanPanel", PanelFill);
            // 나 = 파랑, 상대 = 빨강 (왼쪽 메뉴 구역 색)
            var accent = top ? UIKit.SecPlay : UIKit.SecCollect;
            bg.transform.Find("Border").GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.45f);
            hp.rt = bg.rectTransform;
            hp.rt.Place(top ? new Vector2(0, 1) : new Vector2(0, 0), top ? new Vector2(18, -18) : new Vector2(18, 18), new Vector2(470, 150));

            var ring = UIKit.Img(bg.transform, "Ring", Color.Lerp(accent, UIKit.Gold, 0.35f), GameAssets.Circle);
            ring.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(126, 126), new Vector2(0, 0.5f));
            var mask = UIKit.Img(ring.transform, "Mask", Color.white, GameAssets.Circle);
            mask.rectTransform.Anchor(0.04f, 0.04f, 0.96f, 0.96f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            hp.portrait = UIKit.Img(mask.transform, "Portrait", Color.white);
            hp.portrait.rectTransform.Anchor(-0.2f, -0.02f, 1.2f, 1.02f);
            hp.portrait.preserveAspect = true;

            hp.name = UIKit.Txt(bg.transform, "Name", "", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            hp.name.rectTransform.Anchor(0, 0.7f, 1, 0.96f, 152, 2, 84, 0);
            hp.name.Section(accent, 26);

            var barBg = UIKit.Img(bg.transform, "HpBg", new Color(0.1f, 0.02f, 0.03f, 1), GameAssets.Rounded, true);
            barBg.rectTransform.Anchor(0, 0.43f, 1, 0.66f, 152, 0, 16, 0);
            hp.hpFlash = UIKit.Img(barBg.transform, "Flash", new Color(1, 1, 1, 0), GameAssets.Rounded, true);
            hp.hpFlash.rectTransform.Fill();
            hp.hpFill = UIKit.Img(barBg.transform, "Fill", HpColor(1), GameAssets.Rounded, true);
            hp.hpFill.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            hp.hp = UIKit.Txt(barBg.transform, "HpText", "", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            hp.hp.rectTransform.Fill();
            hp.hp.Outline(new Color(0, 0, 0, 0.9f));

            hp.info = UIKit.Txt(bg.transform, "Info", "", 20, UIKit.Ink, TextAnchor.MiddleLeft);
            hp.info.rectTransform.Anchor(0, 0.2f, 1, 0.42f, 152, 0, 12, 0);
            hp.skill = UIKit.Txt(bg.transform, "Skill", "", 18, UIKit.Muted, TextAnchor.MiddleLeft);
            hp.skill.rectTransform.Anchor(0, 0.02f, 1, 0.22f, 152, 0, 12, 0);
            // 묘지 보기 (나 / 상대)
            var gb = UIKit.Button(bg.transform, "묘지", () => OpenGrave(!top), null, 15);
            gb.rt.Place(new Vector2(1, 1), new Vector2(-8, -8), new Vector2(66, 30), new Vector2(1, 1));
            return hp;
        }

        void BuildField()
        {
            var bg = UIKit.Frame(root, "FieldPanel", PanelFill);
            bg.transform.Find("Border").GetComponent<Image>().color = new Color(FieldAccent.r, FieldAccent.g, FieldAccent.b, 0.45f);
            bg.rectTransform.Place(new Vector2(1, 1), new Vector2(-18, -18), new Vector2(400, 190));
            fieldName = UIKit.Txt(bg.transform, "FieldName", "", 26, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            fieldName.rectTransform.Anchor(0, 1, 1, 1, 16, -48, 16, 12);
            fieldName.Section(FieldAccent, 22);
            fieldText = UIKit.Txt(bg.transform, "FieldText", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            fieldText.rectTransform.Anchor(0, 0.36f, 1, 1, 16, 0, 16, 56);
            statusText = UIKit.Txt(bg.transform, "Status", "", 17, new Color(1f, 0.7f, 0.5f), TextAnchor.UpperLeft);
            statusText.rectTransform.Anchor(0, 0.02f, 1, 0.37f, 16, 4, 16, 0);
        }

        void BuildButtons()
        {
            battleBtn = UIKit.Button(root, "배틀 페이즈 ▶", () => { CloseMenu(); g.RequestBattle(); }, new Color(0.45f, 0.08f, 0.05f, 0.96f), 30, Color.white);
            battleBtn.rt.Place(new Vector2(1, 0.5f), new Vector2(-24, 40), new Vector2(270, 84));
            endBtn = UIKit.Button(root, "타임 종료", () => { CloseMenu(); g.RequestEndTime(); }, null, 24);
            endBtn.rt.Place(new Vector2(1, 0.5f), new Vector2(-24, -46), new Vector2(270, 60));
            heroBtn = UIKit.Button(root, "영웅 능력", () => { CloseMenu(); if (g.CanUseHero(g.Human)) g.HumanAct(g.UseHero(g.Human)); }, new Color(0.35f, 0.25f, 0.02f, 0.96f), 24, Color.white);
            heroBtn.rt.Place(new Vector2(0, 0), new Vector2(500, 30), new Vector2(210, 60));
            // 2배속 (CPU · 스토리 대전). 누를 때마다 켜기/끄기, 설정에 기억
            speedBtn = UIKit.Button(root, "▶▶ x2", ToggleSpeed, null, 22);
            speedBtn.rt.Place(new Vector2(1, 0.5f), new Vector2(-24, 128), new Vector2(150, 56));
            // 오토 모드 (모든 대전): 누를 때마다 자동/수동. 1분 동안 입력이 없으면 저절로 켜지고, 움직이면 다시 수동
            autoBtn = UIKit.Button(root, "AUTO", ToggleAuto, null, 22);
            autoBtn.rt.Place(new Vector2(1, 0.5f), new Vector2(-24, 190), new Vector2(150, 56));
            // 메인 메뉴처럼 아이콘 + 이름 (VARCO 아이콘, Tools/nav)
            battleBtn.Icon(GameAssets.TexSprite("btl_ready"), 58);
            heroBtn.Icon(GameAssets.TexSprite("btl_hero"), 46);
            speedBtn.Icon(GameAssets.TexSprite("btl_speed"), 42);
            autoBtn.Icon(GameAssets.TexSprite("btl_auto"), 42);
        }

        UIKit.Btn speedBtn;
        bool speedShown;

        // ── 오토 모드 ──
        /// <summary>이만큼 입력(마우스 움직임·클릭·키)이 없으면 오토 모드로 (실제 시간)</summary>
        public static float AfkSeconds = 60f;
        UIKit.Btn autoBtn;
        bool autoShown;
        float lastInputTime;
        int afkRevertFrame = -1;

        void ToggleAuto()
        {
            if (g == null || g.players[0] == null) return;
            if (Time.frameCount == afkRevertFrame) return;    // 이 클릭이 AFK 오토를 이미 수동으로 되돌렸다
            g.SetAuto(!g.autoHuman);
            RefreshAutoBtn();
        }

        void RefreshAutoBtn()
        {
            if (autoBtn == null || g == null) return;
            bool on = g.autoHuman;
            autoBtn.label.text = on ? "AUTO  ON" : "AUTO";
            autoBtn.label.color = on ? new Color(0.18f, 0.08f, 0.02f) : UIKit.Gold;
            autoBtn.bg.color = on ? new Color(0.45f, 0.95f, 0.75f, 0.97f) : new Color(0.06f, 0.04f, 0.1f, 0.9f);
        }

        /// <summary>새 입력이 있었는가 (마우스 움직임·버튼·휠, 키보드)</summary>
        static bool AnyInput()
        {
            var m = Mouse.current;
            if (m != null && (m.delta.ReadValue().sqrMagnitude > 0.5f || m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed || m.scroll.ReadValue().sqrMagnitude > 0.5f)) return true;
            var k = Keyboard.current;
            return k != null && k.anyKey.isPressed;
        }

        /// <summary>
        /// 오토 버튼 표시(대전 중 · 결과 화면 전까지)와 AFK 전환: 1분 동안 입력이 없으면 오토 모드,
        /// AFK 로 켜진 오토는 입력이 들어오는 순간 수동으로 돌아간다 (버튼으로 켠 오토는 그대로).
        /// </summary>
        void UpdateAutoMode()
        {
            bool inMatch = g != null && g.players[0] != null && !g.gameOver
                           && !(TutorialGuide.Active != null && TutorialGuide.Active.Visible);   // 튜토리얼 안내 중엔 오토 버튼 숨김
            if (autoBtn != null && autoShown != inMatch)
            {
                autoShown = inMatch;
                autoBtn.bg.gameObject.SetActive(inMatch);
                RefreshAutoBtn();
            }
            if (!inMatch || Paused || AutoTest.Enabled || g.mode == MatchMode.Tutorial) { lastInputTime = Time.unscaledTime; return; }   // 자동 테스트는 autoHuman 을 직접 다룬다 · 튜토리얼은 읽는 동안 AFK 로 넘어가지 않게
            bool input = AnyInput();
            if (input) lastInputTime = Time.unscaledTime;
            if (input && g.autoHuman && g.autoByAfk) { g.SetAuto(false); afkRevertFrame = Time.frameCount; RefreshAutoBtn(); }
            else if (!g.autoHuman && Time.unscaledTime - lastInputTime >= AfkSeconds) { g.SetAuto(true, true); RefreshAutoBtn(); }
            else if (autoBtn != null && (autoBtn.label.text == "AUTO  ON") != g.autoHuman) RefreshAutoBtn();   // 코드로 바뀐 경우
        }

        void ToggleSpeed()
        {
            GameSettings.Data.battleX2 = !GameSettings.Data.battleX2;
            GameSettings.Save();
            if (!Paused) GameSettings.Apply(false);
            RefreshSpeedBtn();
        }

        void RefreshSpeedBtn()
        {
            if (speedBtn == null) return;
            bool on = GameSettings.Data.battleX2;
            speedBtn.label.text = on ? "▶▶ x2  ON" : "▶▶ x2";
            speedBtn.label.color = on ? new Color(0.18f, 0.08f, 0.02f) : UIKit.Gold;
            speedBtn.bg.color = on ? new Color(1f, 0.78f, 0.3f, 0.97f) : new Color(0.06f, 0.04f, 0.1f, 0.9f);
        }

        /// <summary>2배속은 CPU · 스토리 대전 진행 중에만 (레이드 · 결과 화면 · 메뉴는 보통 속도)</summary>
        void UpdateSpeedBoost()
        {
            bool allowed = g != null && g.players[0] != null && g.raid == null && !g.gameOver;
            if (speedBtn != null && speedShown != allowed)
            {
                speedShown = allowed;
                speedBtn.bg.gameObject.SetActive(allowed);
                RefreshSpeedBtn();
            }
            if (GameSettings.BoostActive != allowed)
            {
                GameSettings.BoostActive = allowed;
                if (!Paused && !AutoTest.Enabled) GameSettings.Apply(false);
            }
        }

        /// <summary>전투 화면 패널 바탕 (메뉴 카드와 같은 어두운 판)</summary>
        static readonly Color PanelFill = new Color(0.035f, 0.028f, 0.07f, 0.9f);
        static readonly Color FieldAccent = new Color(0.5f, 0.9f, 0.6f);

        void BuildLog()
        {
            var bg = UIKit.Img(root, "Log", new Color(0.035f, 0.028f, 0.07f, 0.7f), GameAssets.Rounded, true);
            logBg = bg;
            bg.rectTransform.Place(new Vector2(1, 0), new Vector2(-18, 18), new Vector2(430, 250));
            var edge = UIKit.Img(bg.transform, "Border", new Color(1, 1, 1, 0.12f), GameAssets.RoundedOutline, true);
            edge.rectTransform.Fill();
            edge.raycastTarget = false;
            var head = UIKit.Txt(bg.transform, "Head", "전투 기록", 16, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.rectTransform.Anchor(0, 1, 1, 1, 14, -32, 12, 6);
            head.Section(UIKit.SecMe, 16);
            var clip = UIKit.Rect("Clip", bg.transform).Anchor(0, 0, 1, 1, 0, 0, 0, 38);
            clip.gameObject.AddComponent<RectMask2D>();
            logText = UIKit.Txt(clip, "LogText", "", 17, UIKit.Ink, TextAnchor.LowerLeft);
            logText.rectTransform.Anchor(0, 0, 1, 1, 14, 10, 12, 4);
            logText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void BuildDetail()
        {
            detailRoot = UIKit.Rect("Detail", root);
            detailRoot.Place(new Vector2(0, 0.5f), new Vector2(22, 30), new Vector2(330, 462), new Vector2(0, 0.5f));
            detailFace = CardFace.Build(detailRoot, 330, 462, 0.46f);
            detailFace.root.Fill();
            detailFace.glow.color = new Color(1, 0.85f, 0.4f, 0.35f);
            var extraBg = UIKit.Img(detailRoot, "ExtraBg", new Color(0, 0, 0, 0.75f), GameAssets.Rounded, true);
            extraBg.rectTransform.Anchor(0, -0.2f, 1, -0.015f);
            detailExtra = UIKit.Txt(extraBg.transform, "Extra", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            detailExtra.rectTransform.Anchor(0, 0, 1, 1, 12, 8, 12, 8);
            // 룬 해설: 카드 정보 오른쪽
            var rb = UIKit.Img(detailRoot, "Runes", new Color(0.04f, 0.02f, 0.08f, 0.88f), GameAssets.Rounded, true);
            detailRunes = rb.rectTransform;
            detailRunes.Place(new Vector2(1, 1), new Vector2(12, 0), new Vector2(360, 200), new Vector2(0, 1));
            rb.raycastTarget = false;
            detailRuneText = UIKit.Txt(rb.transform, "T", "", 17, UIKit.Ink, TextAnchor.UpperLeft);
            detailRuneText.rectTransform.Anchor(0, 0, 1, 1, 14, 10, 12, 10);
            detailRuneText.lineSpacing = 1.1f;
            detailRoot.gameObject.SetActive(false);
        }

        void BuildChoice()
        {
            var bg = UIKit.Frame(root, "Choice", new Color(0.05f, 0.03f, 0.1f, 0.94f));
            choicePanel = bg.rectTransform;
            choicePanel.Place(new Vector2(0.5f, 1), new Vector2(0, -134), new Vector2(760, 150));
            choicePrompt = UIKit.Txt(bg.transform, "Prompt", "", 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            choicePrompt.rectTransform.Anchor(0, 1, 1, 1, 16, -56, 16, 8);
            choicePrompt.rectTransform.offsetMin = new Vector2(16, -56);
            choicePrompt.rectTransform.offsetMax = new Vector2(-16, -8);
            choiceList = UIKit.Rect("List", bg.transform);
            choiceList.Anchor(0, 0, 1, 1, 16, 14, 16, 62);
            var grid = choiceList.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(232, 56);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            choicePanel.gameObject.SetActive(false);

            actionMenu = UIKit.Rect("ActionMenu", root);
            actionMenu.Place(new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(360, 10), new Vector2(0.5f, 0));
            var v = actionMenu.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8;
            v.childForceExpandHeight = false;
            v.childControlHeight = true;
            v.childControlWidth = true;
            actionMenu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            actionMenu.gameObject.SetActive(false);
        }

        void BuildClash()
        {
            clashRoot = UIKit.Rect("Clash", root);
            clashRoot.Place(new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1100, 190));
            clashGroup = clashRoot.gameObject.AddComponent<CanvasGroup>();
            var bg = UIKit.Img(clashRoot, "Bg", new Color(0, 0, 0, 0.72f), GameAssets.Rounded, true);
            bg.rectTransform.Fill();
            clashLeft = UIKit.Txt(clashRoot, "L", "", 30, new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            clashLeft.rectTransform.Anchor(0, 0, 0.42f, 1, 10, 10, 0, 10);
            clashRight = UIKit.Txt(clashRoot, "R", "", 30, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            clashRight.rectTransform.Anchor(0.58f, 0, 1, 1, 0, 10, 10, 10);
            clashMid = UIKit.Txt(clashRoot, "M", "VS", 44, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            clashMid.rectTransform.Anchor(0.42f, 0, 0.58f, 1);
            clashMid.Shadow();
            clashRoot.gameObject.SetActive(false);
        }

        void BuildBanner()
        {
            bannerRoot = UIKit.Rect("Banner", root);
            bannerRoot.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1920, 220));
            bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            var strip = UIKit.Img(bannerRoot, "Strip", new Color(0, 0, 0, 0.65f));
            strip.rectTransform.Anchor(0, 0.1f, 1, 0.9f);
            var line1 = UIKit.Img(bannerRoot, "Line1", UIKit.Gold);
            line1.rectTransform.Anchor(0, 0.9f, 1, 0.9f, 0, -2, 0, -2);
            var line2 = UIKit.Img(bannerRoot, "Line2", UIKit.Gold);
            line2.rectTransform.Anchor(0, 0.1f, 1, 0.1f, 0, -2, 0, -2);
            bannerMain = UIKit.Txt(bannerRoot, "Main", "", 92, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerMain.rectTransform.Anchor(0, 0.35f, 1, 0.92f);
            bannerMain.horizontalOverflow = HorizontalWrapMode.Overflow;
            bannerMain.Outline(new Color(0.3f, 0.1f, 0, 0.9f), 3f).Shadow(new Color(0, 0, 0, 0.8f), 5f);
            bannerSub = UIKit.Txt(bannerRoot, "Sub", "", 32, UIKit.Ink, TextAnchor.MiddleCenter);
            bannerSub.rectTransform.Anchor(0, 0.1f, 1, 0.38f);
            bannerRoot.gameObject.SetActive(false);
        }

        void BuildGameOver()
        {
            var bg = UIKit.Img(root, "GameOver", new Color(0, 0, 0, 0.72f));
            gameOverRoot = bg.rectTransform;
            gameOverRoot.Fill();
            bg.raycastTarget = true;
            gameOverTitle = UIKit.Txt(gameOverRoot, "Title", "", 150, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            gameOverTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1600, 220));
            gameOverTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            gameOverTitle.Outline(new Color(0.3f, 0.1f, 0, 0.9f), 4f).Shadow(new Color(0, 0, 0, 0.8f), 6f);
            var sub = gameOverSub = UIKit.Txt(gameOverRoot, "Sub", "패배한 쪽이 다음 매치의 필드를 결정합니다", 26, UIKit.Ink);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1200, 44));
            var re = gameOverRematch = UIKit.Button(gameOverRoot, "재대결", () => onRematch?.Invoke(), new Color(0.45f, 0.08f, 0.05f, 0.96f), 32, Color.white);
            re.rt.Place(new Vector2(0.5f, 0.5f), new Vector2(-160, -110), new Vector2(280, 80));
            gameOverReward = UIKit.Txt(gameOverRoot, "Reward", "", 30, new Color(0.5f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            gameOverReward.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1200, 50));
            var tt = UIKit.Button(gameOverRoot, "홈으로", () => onTitle?.Invoke(), null, 28);
            tt.rt.Place(new Vector2(0.5f, 0.5f), new Vector2(160, -110), new Vector2(280, 80));
            gameOverRoot.gameObject.SetActive(false);
        }

        // ══════════════════════════ 갱신 ══════════════════════════

        public void BeginMatch(GameManager gm)
        {
            g = gm;
            ResetExtras();
            logLines.Clear();
            logText.text = "";
            gameOverRoot.gameObject.SetActive(false);
            choicePanel.gameObject.SetActive(false);
            clashRoot.gameObject.SetActive(false);
            CloseMenu();
            HideBattleMode();
            HideTargetMode();
            HidePileTip();
            CloseGrave();
            ClosePause();
            CancelPick();        // 이전 대전을 고르기 화면(운명 재배치·원소·패 정리)이 열린 채 나갔으면 그 창이 남아 다음 대전이 막힌다
            ChoiceTimer(-1);
            EndRaidHud();
            EndStoryHud();
            Show(true);
        }

        public void Refresh()
        {
            if (g == null || g.players[0] == null) return;
            timeText.text = g.time > 0 ? $"TIME {g.time}" : "READY";
            string phaseName = PhaseName(g.phase);
            string lane = laneSlot >= 0 ? $" · <color=#ffd77a>▶ {laneSlot + 1}번 칸</color>" : "";
            if (SimulTurnShown)
            {
                // 동시 룰: "동시 진행 · 타임 시작" 같은 문구 대신 아이콘(쌍검 = 내 공격 턴 · 방패 = 수비 턴) + 짧은 라벨
                bool atk = g.LaneFirst == g.Human;
                var sp = GameAssets.TexSprite(atk ? "Fx/turn_attack" : "Fx/turn_defense");
                turnIcon.sprite = sp;
                turnIcon.enabled = sp != null;
                string ph = g.phase == Phase.Main ? " · 배치" : g.phase == Phase.Battle ? " · 배틀" : g.phase == Phase.BattleEnd ? " · 배틀 엔드" : "";
                phaseText.text = TurnLabel() + ph + lane;
                phaseText.alignment = TextAnchor.MiddleLeft;
                phaseText.rectTransform.Anchor(0.22f, 0.06f, 0.98f, 0.45f);
            }
            else
            {
                turnIcon.enabled = false;
                phaseText.alignment = TextAnchor.MiddleCenter;
                phaseText.rectTransform.Anchor(0, 0.06f, 1, 0.45f);
                string who = g.Simul ? "" : g.active == null ? "" : (g.active == g.Human ? (g.raid != null ? "파티 타임" : "나의 타임") : (g.raid != null ? "보스 타임" : "상대 타임"));
                phaseText.text = g.time > 0 ? (who.Length > 0 ? $"{who} · " : "") + phaseName + lane : "매치 준비";
            }
            timeText.color = g.Simul || g.active == g.Human ? UIKit.Gold : new Color(1f, 0.55f, 0.5f);
            turnText.text = "";

            SetHero(humanPanel, g.Human);
            SetHero(cpuPanel, g.Cpu);
            RefreshFieldButton();
            if (g.raid != null) RefreshRaid();

            if (g.center != null)
            {
                fieldName.text = $"필드 · {g.center.Name}" + (g.raid != null ? "  <size=18><color=#f96>[보스 필드 · 고정]</color></size>" : "");
                fieldName.color = Color.Lerp(UIKit.Gold, CardData.ElementColor(g.center.data.element), 0.5f);
                fieldText.text = g.center.data.text;
            }
            var st = g.statuses.Select(s => $"◆ {s.source} ({ExpireName(s.expire)})").ToList();
            if (g.MustSummonFaceDown) st.Insert(0, "※ 뒷면 소환만 가능");
            statusText.text = string.Join("\n", st.Take(3));

            bool main = g.IsHumanMain;
            battleBtn.Interactable = main;
            endBtn.Interactable = main;
            heroBtn.Interactable = main && g.CanUseHero(g.Human);
            battleBtn.label.text = (g.Simul ? "배치 완료 ▶" : "배틀 페이즈 ▶") + KeyHint(GameAction.BattlePhase);
            endBtn.label.text = "타임 종료" + KeyHint(GameAction.EndTime);
            endBtn.bg.gameObject.SetActive(!g.Simul);       // 동시 룰: 배치가 끝나면 배틀은 자동
            heroBtn.label.text = g.Human.heroUsed ? "능력 사용됨" : "영웅 능력" + KeyHint(GameAction.HeroSkill);
            logBg.gameObject.SetActive(GameSettings.Data.showLog);
            if (!main) CloseMenu();

            if (hoverCard != null) ShowDetail(hoverCard);
        }

        void SetHero(HeroPanel hp, PlayerState p)
        {
            // 플레이어 프로필 (아바타 + 닉네임). 영웅은 보드 위 영웅 카드로 표시
            var av = GameAssets.Avatar(p.avatar);
            hp.portrait.sprite = av;
            hp.portrait.color = av != null ? Color.white : new Color(0.35f, 0.3f, 0.5f);
            hp.portrait.rectTransform.Anchor(0, 0, 1, 1);
            string order = g.first == null ? "" : g.Simul
                ? (g.LaneFirst == p ? "  <color=#ff9a7a>공격 턴</color>" : "  <color=#9cf>수비 턴</color>")
                : (g.first == p ? "  <color=#ffd05c>선공·홀수</color>" : "  <color=#9cf>후공·짝수</color>");
            hp.name.text = $"{p.name}  <size=18><color=#bbb>{(p.isHuman ? "YOU" : "")}</color>{order}</size>";
            float r = p.maxHp > 0 ? (float)p.hp / p.maxHp : 0;
            hp.hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(r), 1);
            hp.hpFill.color = HpColor(r);
            hp.hp.text = $"HP {p.hp} / {p.maxHp}";
            hp.info.text = $"패 {p.hand.Count}   덱 {p.deck.Count}   묘지 {p.grave.Count}   EX {p.extra.Count}   칸 {p.FieldCount}/7";
            string pool = g.PoolText(p);     // 원소 제약: 영웅 능력 상태는 아래 '능력' 버튼에 보이므로 이 줄은 원소에 쓴다
            hp.skill.text = pool.Length > 0 ? pool : $"영웅 {p.hero.Name} · ATK {p.hero.data.heroAtk} · " + (p.heroUsed ? "능력 사용 완료" : "능력 사용 가능");
            if (g.raid != null && p == g.Cpu)
            {
                hp.info.text = $"하수인 {p.FieldCount}/{RaidData.MaxMinions}   묘지 {p.grave.Count}";
                hp.skill.text = $"레이드 보스 · 반격 ATK {g.HeroAtk(p)} · 스킬 카드 {g.raid.boss.skills.Count}종";
            }
            hp.rt.GetComponent<Image>().color = g.active == p ? new Color(0.14f, 0.09f, 0.02f, 0.92f) : UIKit.Panel;
        }

        // ── 튜토리얼(TutorialMatch)이 깜빡이게 짚는 UI 참조 ──
        public RectTransform Root => root;
        public RectTransform BattleBtnRt => battleBtn?.rt;
        public RectTransform HeroBtnRt => heroBtn?.rt;
        public RectTransform AutoBtnRt => autoBtn?.rt;
        public RectTransform SpeedBtnRt => speedBtn?.rt;
        public bool ChoiceOpen => choicePanel != null && choicePanel.gameObject.activeSelf;
        /// <summary>공격 대상 지정 모드(3D 카드 클릭/드래그로 고르는 중)인가</summary>
        public bool TargetModeOpen => targetPick != null;
        /// <summary>열려 있는 선택창의 첫 번째 누를 수 있는 버튼 (없으면 null)</summary>
        public RectTransform FirstChoiceButton
        {
            get
            {
                if (!ChoiceOpen) return null;
                for (int i = 0; i < choiceList.childCount; i++)
                {
                    var b = choiceList.GetChild(i).GetComponent<Button>();
                    if (b != null && b.interactable) return (RectTransform)b.transform;
                }
                return null;
            }
        }

        /// <summary>동시 룰 대전(레이드 제외)에서 턴 아이콘·라벨을 쓰는가</summary>
        bool SimulTurnShown => g != null && g.Simul && g.raid == null && g.time > 0 && g.first != null;

        /// <summary>"내 공격 턴" / "수비 턴" 색 라벨</summary>
        string TurnLabel() => g.LaneFirst == g.Human ? "<color=#ff9a7a><b>내 공격 턴</b></color>" : "<color=#9cf><b>수비 턴</b></color>";

        string PhaseName(Phase p)
        {
            switch (p)
            {
                case Phase.TimeStart: return "타임 시작";
                case Phase.Main: return g.Simul ? "배치 (뒷면)" : "메인 페이즈";
                case Phase.Battle: return g.Simul ? (g.LaneFirst == g.Human ? "배틀 · 내 공격 턴" : "배틀 · 수비 턴 (반격만)") : "배틀 페이즈";
                case Phase.BattleEnd: return "배틀 엔드 (t0만 가능)";
                case Phase.End: return "타임 종료";
                default: return "";
            }
        }

        static string ExpireName(Expire e)
        {
            switch (e)
            {
                case Expire.NextBattleEnd: return "다음 배틀 엔드까지";
                case Expire.EndOfTime: return "이번 타임";
                case Expire.CasterNextTime: return "다음 턴까지";
                default: return "매치 동안";
            }
        }

        public void Log(string msg)
        {
            logLines.Add(msg);
            while (logLines.Count > 9) logLines.RemoveAt(0);
            if (logText != null) logText.text = string.Join("\n", logLines);
        }

        // ══════════════════════════ 카드 상호작용 ══════════════════════════

        public void OnCardHover(CardInstance c, bool on)
        {
            // 덱 · 묘지: 남은 장수 툴팁 (덱은 내용 비공개)
            if (c != null && (c.zone == Zone.Deck || c.zone == Zone.Grave) && c.owner != null)
            {
                if (on) ShowPileTip(c.owner, c.zone);
                else if (pileOwner == c.owner && pileZone == c.zone) HidePileTip();
                if (c.zone == Zone.Deck) { if (hoverCard == c) { hoverCard = null; detailRoot.gameObject.SetActive(false); } return; }
            }
            if (on) { hoverCard = c; ShowDetail(c); }
            else if (hoverCard == c) { hoverCard = null; detailRoot.gameObject.SetActive(false); }
        }

        // ══════════════════════════ 덱 · 묘지 장수 툴팁 ══════════════════════════

        RectTransform pileTip;
        Text pileText;
        PlayerState pileOwner;
        Zone pileZone;

        void BuildPileTip()
        {
            var bg = UIKit.Frame(root, "PileTip", new Color(0.04f, 0.03f, 0.08f, 0.94f));
            pileTip = bg.rectTransform;
            pileTip.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 86), new Vector2(0, 0));
            pileText = UIKit.Txt(bg.transform, "Text", "", 24, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            pileText.rectTransform.Fill();
            pileText.supportRichText = true;
            bg.raycastTarget = false;
            pileTip.gameObject.SetActive(false);
        }

        void ShowPileTip(PlayerState p, Zone z)
        {
            pileOwner = p; pileZone = z;
            pileTip.gameObject.SetActive(true);
            pileTip.SetAsLastSibling();
            UpdatePileTip();
        }

        void HidePileTip() { pileOwner = null; if (pileTip != null) pileTip.gameObject.SetActive(false); }

        void UpdatePileTip()
        {
            if (pileOwner == null || g == null) return;
            bool mine = pileOwner == g.Human;
            int n = pileZone == Zone.Deck ? pileOwner.deck.Count : pileOwner.grave.Count;
            pileText.text = $"{(mine ? "내" : "상대")} {(pileZone == Zone.Deck ? "덱" : "묘지")}\n<size=34>{n}장</size>";
            var mouse = Mouse.current;
            if (mouse != null) pileTip.anchoredPosition = ToLocal(mouse.position.ReadValue()) + new Vector2(-250, 20);
        }

        // ══════════════════════════ 일시정지 (Esc): 계속 · 설정 · 항복 ══════════════════════════

        RectTransform pauseRoot, pausePanel, settingsPanel, confirmPanel;
        SettingsPage pauseSettings;
        float pausedScale = 1f;
        public bool Paused => pauseRoot != null && pauseRoot.gameObject.activeSelf;

        void BuildPause()
        {
            var dim = UIKit.Img(root, "Pause", new Color(0, 0, 0, 0.62f));
            dim.raycastTarget = true;
            pauseRoot = dim.rectTransform;
            pauseRoot.Fill();

            var panel = UIKit.Frame(pauseRoot, "PausePanel", new Color(0.05f, 0.03f, 0.09f, 0.96f));
            pausePanel = panel.rectTransform;
            pausePanel.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 420));
            var title = UIKit.Txt(panel.transform, "Title", "일시정지", 44, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 0, -96, 0, -20);
            title.Shadow();
            var resume = UIKit.Button(panel.transform, "계속하기", ClosePause, new Color(0.25f, 0.18f, 0.05f, 0.96f), 28);
            resume.rt.Place(new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(340, 68), new Vector2(0.5f, 1));
            var set = UIKit.Button(panel.transform, "설정", OpenPauseSettings, null, 28);
            set.rt.Place(new Vector2(0.5f, 1), new Vector2(0, -212), new Vector2(340, 68), new Vector2(0.5f, 1));
            var sur = UIKit.Button(panel.transform, "항복", () => { confirmPanel.gameObject.SetActive(true); pausePanel.gameObject.SetActive(false); },
                new Color(0.35f, 0.06f, 0.04f, 0.96f), 28, new Color(1f, 0.75f, 0.7f));
            sur.rt.Place(new Vector2(0.5f, 1), new Vector2(0, -294), new Vector2(340, 68), new Vector2(0.5f, 1));
            var hint = UIKit.Txt(panel.transform, "Hint", "[Esc] 닫기", 18, UIKit.Muted, TextAnchor.MiddleCenter);
            hint.rectTransform.Anchor(0, 0, 1, 0, 0, 10, 0, 44);

            // 설정: 메인 메뉴와 같은 설정 페이지
            var sp = UIKit.Frame(pauseRoot, "PauseSettings", new Color(0.03f, 0.02f, 0.06f, 0.97f));
            settingsPanel = sp.rectTransform;
            settingsPanel.Anchor(0.08f, 0.06f, 0.92f, 0.94f);
            var content = UIKit.Rect("Content", settingsPanel).Anchor(0, 0, 1, 1, 20, 20, 20, 84);
            pauseSettings = gameObject.AddComponent<SettingsPage>();
            pauseSettings.Build(content);
            var st = UIKit.Txt(settingsPanel, "Title", "설정", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            st.rectTransform.Anchor(0, 1, 1, 1, 30, -64, 220, 20);
            st.Section(UIKit.SecMe, 26);
            var back = UIKit.Button(settingsPanel, "닫기", ClosePauseSettings, null, 24);
            back.rt.Place(new Vector2(1, 1), new Vector2(-20, -14), new Vector2(180, 54), new Vector2(1, 1));
            settingsPanel.gameObject.SetActive(false);

            // 항복 확인
            var cp = UIKit.Frame(pauseRoot, "Surrender", new Color(0.08f, 0.02f, 0.03f, 0.97f));
            confirmPanel = cp.rectTransform;
            confirmPanel.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 280));
            var q = UIKit.Txt(cp.transform, "Q", "정말 항복할까요?\n<size=22><color=#d9c9c9>이번 매치는 패배로 처리됩니다.</color></size>", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            q.supportRichText = true;
            q.rectTransform.Anchor(0, 0.42f, 1, 1, 20, 0, 20, 10);
            var yes = UIKit.Button(cp.transform, "항복", () => { ClosePause(); g.Surrender(); }, new Color(0.45f, 0.07f, 0.05f, 0.97f), 26, Color.white);
            yes.rt.Place(new Vector2(0.5f, 0), new Vector2(-120, 34), new Vector2(210, 66), new Vector2(0.5f, 0));
            var no = UIKit.Button(cp.transform, "취소", () => { confirmPanel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(true); }, null, 26);
            no.rt.Place(new Vector2(0.5f, 0), new Vector2(120, 34), new Vector2(210, 66), new Vector2(0.5f, 0));
            confirmPanel.gameObject.SetActive(false);

            pauseRoot.gameObject.SetActive(false);
        }

        void OpenPause()
        {
            if (Paused) return;
            CloseMenu();
            pausedScale = Time.timeScale;
            Time.timeScale = 0f;
            pauseRoot.gameObject.SetActive(true);
            pauseRoot.SetAsLastSibling();
            pausePanel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
            confirmPanel.gameObject.SetActive(false);
        }

        void ClosePause()
        {
            if (!Paused) return;
            pauseRoot.gameObject.SetActive(false);
            if (AutoTest.Enabled) Time.timeScale = pausedScale;
            else GameSettings.Apply(false);     // 설정에서 바꾼 연출 속도 반영 (화면 모드는 설정 페이지에서 이미 반영)
        }

        void OpenPauseSettings()
        {
            pausePanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
            pauseSettings.Refresh();
        }

        void ClosePauseSettings()
        {
            settingsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        /// <summary>시나리오 테스트용</summary>
        public void TestPause(bool on) { if (on) OpenPause(); else ClosePause(); }
        public void TestPileTip(PlayerState p, Zone z) { if (p == null) HidePileTip(); else ShowPileTip(p, z); }

        /// <summary>항복·매치 종료 시 진행 중이던 선택창 · 공격 모드 등 입력 UI 정리</summary>
        public void ResetInteraction()
        {
            HideChoice();
            HideTargetMode();
            HideBattleMode();
            HidePileTip();
            CloseGrave();
            CloseMenu();
            if (clashRoot != null) clashRoot.gameObject.SetActive(false);
            if (fateRoot != null) { fateDone = null; fateRoot.gameObject.SetActive(false); }
            HideCoinToss();
        }

        /// <summary>Esc: 일시정지 메뉴 열기/닫기 (설정 → 메뉴 → 닫기 순서로 한 단계씩)</summary>
        bool HandleEscape()
        {
            if (g == null || g.players[0] == null) return false;
            if (TutorialGuide.Active != null && TutorialGuide.Active.Visible) return false;   // 튜토리얼 중 Esc 는 가이드의 나가기 확인창
            if (Paused)
            {
                Time.timeScale = 0f;     // 설정 변경(GameSettings.Apply)이 속도를 되돌리지 않게
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame && !pauseSettings.IsListening)
                {
                    if (settingsPanel.gameObject.activeSelf) ClosePauseSettings();
                    else if (confirmPanel.gameObject.activeSelf) { confirmPanel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(true); }
                    else ClosePause();
                }
                return true;
            }
            var k = Keyboard.current;
            if (GraveOpen && k != null && k.escapeKey.wasPressedThisFrame) { CloseGrave(); return true; }
            if (k != null && k.escapeKey.wasPressedThisFrame && !g.gameOver && !gameOverRoot.gameObject.activeSelf) { OpenPause(); return true; }
            return false;
        }

        /// <summary>HP 바 색: 절반 초과 초록 · 1/4 초과 주황 · 그 아래 빨강 (KO 는 회색)</summary>
        static Color HpColor(float ratio, bool ko = false) =>
            ko ? new Color(0.35f, 0.35f, 0.35f) : ratio > 0.5f ? new Color(0.2f, 0.9f, 0.45f) : ratio > 0.25f ? new Color(1f, 0.75f, 0.2f) : new Color(1f, 0.3f, 0.25f);

        void ShowDetail(CardInstance c)
        {
            if (g == null) return;
            bool see = g.HumanCanSee(c);
            if (!see)
            {
                detailRoot.gameObject.SetActive(false);
                return;
            }
            detailRoot.gameObject.SetActive(true);
            detailFace.Set(c.data, c.IsMonster ? g.EffectiveAtk(c) : -1, c.IsMonster && c.zone == Zone.Slot ? g.CurHp(c) : -1);
            detailFace.SetFinish(g.HumanCanSee(c) ? c.finish : CardFinish.Normal);
            var extra = new List<string>();
            if (c.IsMonster)
            {
                extra.Add($"ATK {g.EffectiveAtk(c)} (기본 {c.data.atk}) · HP {(c.zone == Zone.Slot ? g.CurHp(c) : c.data.hp)}/{(c.zone == Zone.Slot ? g.MaxHp(c) : c.data.hp)}");
                if (c.zone == Zone.Slot && c.noRegenUntil >= g.time) extra.Add("<color=#f98>[상처] 다음 타임 HP 회복 불가</color>");
                var tags = new List<string>();
                if (!g.Simul && c.data.attackAll) tags.Add("연쇄 공격");
                if (!g.Simul && c.data.attackCount > 1) tags.Add($"{c.data.attackCount}회 공격");
                if (c.data.noCounter) tags.Add("암살");
                if (c.data.regen) tags.Add("재생");
                if (g.IsTaunt(c)) tags.Add("도발");
                if (g.Snipe(c)) tags.Add("저격");
                if (c.data.vanguard) tags.Add("선봉");
                if (g.Resist(c)) tags.Add("불변");
                if (!g.Simul && c.data.canSummonFaceDown) tags.Add("뒷면 소환 가능");
                extra.Add(c.data.DiceText());
                if (tags.Count > 0) extra.Add(string.Join(" · ", tags));
            }
            if (c.zone == Zone.Slot && c.faceDown) extra.Add(g.BeaconRevealed(c) ? "뒷면 (봉화로 공개 중)" : g.IsRevealed(c) ? "뒷면 (배틀 공개)" : "뒷면 세로 표시");
            if (c.owner == g.Human && g.IsHumanMain && (c.zone == Zone.Hand || c.zone == Zone.Hero || (c.zone == Zone.Slot && (c.faceDown || g.Simul))))
                extra.Add("<color=#9f9>우클릭: 행동 선택 · 클릭: 확대</color>");
            detailExtra.text = string.Join("\n", extra);
            string runes = Runes.Text(c.data);
            detailRunes.gameObject.SetActive(runes != "");
            detailRuneText.text = runes;
            if (runes != "") detailRunes.sizeDelta = new Vector2(360, 24 + detailRuneText.preferredHeight);
        }

        RectTransform detailRunes;
        Text detailRuneText;

        /// <summary>
        /// 카드 왼쪽 클릭: 게임 동작(효과 대상 선택 · 배틀 중 마법 발동)이 우선이고, 그 외에는 카드 확대.
        /// 메인 페이즈 행동 메뉴(소환·세트·반전·영웅 능력)는 우클릭 (OnCardRightClicked).
        /// </summary>
        public void OnCardClicked(CardInstance c)
        {
            if (g == null) return;
            if (planDrag != null || Time.frameCount <= suppressClickFrame) return;   // 드래그 배치를 놓은 클릭은 확대하지 않는다
            if (g.ChoicePending) { g.TryPickChoice(c); return; }
            if (c.zone == Zone.Grave) { OpenGrave(c.owner == g.Human); return; }   // 묘지 더미 = 묘지 보기
            if (c.data.isElement && c.zone == Zone.Center) { OpenElements(c.owner == g.Human); return; }   // 영웅 아래 원소 더미 = 원소 보기
            if (g.HumanBattleOpen)
            {
                // 배틀: 세트한 t/half 마법은 클릭해서 발동 (몬스터 공격은 드래그)
                if (c.owner == g.Human && g.CanActivateInBattle(c)) g.TryHumanMagic(c);
                else ZoomCard(c);
                return;
            }
            if (targetPick != null) return;      // 공격 대상 지정 중 (대상은 드래그·키로 고른다)
            // 그 외 왼쪽 클릭은 모두 확대 (패 · 필드 · 영웅 공통, 행동 메뉴는 우클릭)
            ZoomCard(c);
        }

        /// <summary>카드 우클릭: 낼 수 있는 패(초록 테두리)는 바로 낸다(동시 룰), 그 외 내 메인 페이즈의 내 카드는 행동 메뉴(할 행동이 없으면 확대), 나머지는 확대</summary>
        public void OnCardRightClicked(CardInstance c)
        {
            if (g == null) return;
            if (c.owner == g.Human && g.IsHumanMain && !g.ChoicePending && targetPick == null)
            {
                if (c.zone == Zone.Hand && QuickPlay(c)) return;
                OpenMenu(c);
            }
            else ZoomCard(c);
        }

        /// <summary>우클릭 바로 내기: 소환수 = 뒷면 소환(왼쪽 빈 칸), 마법 = 칸에 세트, 필드 = 전개. 낼 수 없으면 false (메뉴로)</summary>
        bool QuickPlay(CardInstance c)
        {
            if (!g.Simul) return false;     // 레이드(교대 룰)는 앞면/뒷면을 골라야 해서 메뉴로
            CloseMenu();
            if (c.IsMonster && g.CanSummon(c, out _) && g.CanSummonFaceDown(c)) { g.HumanAct(g.Summon(c, true)); return true; }
            if (c.IsMagic && g.CanSet(c)) { g.HumanAct(g.SetMagic(c)); return true; }
            if (c.data.type == CardType.Field && g.CanPlayField(c)) { g.HumanAct(g.PlayField(c)); return true; }
            return false;
        }

        /// <summary>카드 확대 (보이지 않는 뒷면 카드는 확대하지 않는다)</summary>
        public void ZoomCard(CardInstance c)
        {
            if (g == null || c == null || !g.HumanCanSee(c)) return;
            CloseMenu();
            CardZoom.Show(c.data, c.finish, c.IsMonster ? g.EffectiveAtk(c) : -1, c.IsMonster && c.zone == Zone.Slot ? g.CurHp(c) : -1);
        }

        void OpenMenu(CardInstance c)
        {
            CloseMenu();
            var items = new List<(string, Action, bool)>();
            string note = null;
            switch (c.zone)
            {
                case Zone.Hand:
                    if (c.IsMonster)
                    {
                        bool ok = g.CanSummon(c, out string why);
                        if (!ok) note = why;
                        if (!g.Simul) items.Add(("소환 (앞면 세로)", () => g.HumanAct(g.Summon(c, false)), ok && g.CanSummonFaceUp(c)));
                        if (g.CanSummonFaceDown(c))
                            items.Add(("소환 (뒷면 세로)", () => g.HumanAct(g.Summon(c, true)), ok));
                        if (g.MustSummonFaceDown) note = (note != null ? note + "\n" : "") + "개기일식: 뒷면으로만 소환 가능";
                    }
                    else if (c.IsMagic)
                    {
                        items.Add(("칸에 세트", () => g.HumanAct(g.SetMagic(c)), g.CanSet(c)));
                        note = "세트 마법: 칸에 뒷면으로 놓아 두면 배틀 때 그 칸 차례에 공개·발동 (패에서는 발동 불가)";
                        if (c.owner.FreeSlot() < 0) note += "\n빈 칸이 없습니다";
                        if (c.data.Cost > 0) note += "\n" + (g.PayProblem(c) ?? $"비용 {c.data.CostText()}");
                    }
                    else if (c.data.type == CardType.Field)
                    {
                        items.Add(("필드 전개 (센터)", () => g.HumanAct(g.PlayField(c)), g.CanPlayField(c)));
                        if (g.raid != null) note = "레이드에서는 보스의 필드를 바꿀 수 없다";
                        else if (c.owner.fieldPlayedThisTime) note = "필드는 타임마다 1장";
                    }
                    break;
                case Zone.Slot:
                    if (g.Simul)
                    {
                        // 동시 룰 배치: 옆 칸과 자리 바꾸기 (앞면으로 남은 잔존형 마법은 이동 불가)
                        items.Add(("◀ 왼쪽 칸으로", () => g.HumanAct(g.MoveSlot(c, -1)), g.CanMoveSlot(c, -1)));
                        items.Add(("오른쪽 칸으로 ▶", () => g.HumanAct(g.MoveSlot(c, 1)), g.CanMoveSlot(c, 1)));
                        note = $"{c.slot + 1}번 칸 · 끌어다 다른 칸에 놓아도 이동 · 배틀은 왼쪽 칸부터";
                        if (c.IsMagic && !c.faceDown) note = "잔존형 마법은 자리를 옮길 수 없습니다";
                    }
                    else if (c.IsMonster && c.faceDown)
                    {
                        items.Add(("반전 소환 (앞면으로)", () => g.HumanAct(g.Flip(c)), g.CanFlip(c)));
                        if (g.MustSummonFaceDown) note = "개기일식 중에는 반전할 수 없습니다";
                    }
                    break;
                case Zone.Hero:
                    items.Add(("영웅 능력 발동", () => g.HumanAct(g.UseHero(c.owner)), g.CanUseHero(c.owner)));
                    if (c.owner.heroUsed) note = "이미 사용했습니다 (매치 1회)";
                    break;
            }
            if (items.Count == 0 && note == null) { ZoomCard(c); return; }

            var title = UIKit.Txt(actionMenu, "Title", c.Name, 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            title.Shadow();
            foreach (var (label, act, enabled) in items)
            {
                var b = UIKit.Button(actionMenu, label, () => { CloseMenu(); act(); }, null, 24);
                b.bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                b.Interactable = enabled;
            }
            if (note != null)
            {
                var n = UIKit.Txt(actionMenu, "Note", note, 18, new Color(1f, 0.8f, 0.6f));
                n.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
                n.Shadow();
            }
            var zoom = UIKit.Button(actionMenu, "카드 확대", () => ZoomCard(c), new Color(0.1f, 0.08f, 0.16f, 0.95f), 20);
            zoom.bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var close = UIKit.Button(actionMenu, "닫기", CloseMenu, new Color(0.1f, 0.1f, 0.12f, 0.9f), 20, UIKit.Muted);
            close.bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            actionMenu.gameObject.SetActive(true);
        }

        /// <summary>동시 룰 배틀: 지금 차례인 칸 표시 (-1 이면 지움)</summary>
        int laneSlot = -1;
        public void SetLaneMarker(int slot)
        {
            laneSlot = slot;
            Refresh();
        }

        public void CloseMenu()
        {
            if (actionMenu == null) return;
            for (int i = actionMenu.childCount - 1; i >= 0; i--) Destroy(actionMenu.GetChild(i).gameObject);
            actionMenu.gameObject.SetActive(false);
        }

        // ══════════════════════════ 선택 ══════════════════════════

        public void ShowChoice(string prompt, List<CardInstance> options, string cancelLabel, Action<CardInstance> onPick)
        {
            ClearChoice();
            choiceList.GetComponent<GridLayoutGroup>().cellSize = new Vector2(232, 56);
            choicePrompt.text = prompt;
            foreach (var o in options)
            {
                var c = o;
                string label = g.HumanCanSee(c) ? c.Name : "뒷면 카드";
                if (c.IsMonster && g.HumanCanSee(c)) label += $"  <color=#fc6>ATK {g.EffectiveAtk(c)}</color> <color=#8f8>HP {g.CurHp(c)}</color>";
                var b = UIKit.Button(choiceList, label, () => onPick(c), null, 20);
                var trig = b.bg.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                AddHover(trig, c);
            }
            if (cancelLabel != null)
                UIKit.Button(choiceList, cancelLabel, () => onPick(null), new Color(0.12f, 0.12f, 0.14f, 0.95f), 20, UIKit.Muted);
            FitChoice(options.Count + (cancelLabel != null ? 1 : 0));
        }

        void AddHover(UnityEngine.EventSystems.EventTrigger trig, CardInstance c)
        {
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => OnCardHover(c, true));
            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => OnCardHover(c, false));
            trig.triggers.Add(enter);
            trig.triggers.Add(exit);
        }

        public void ShowOptions(string prompt, string[] labels, Action<int> onPick)
        {
            ClearChoice();
            choicePrompt.text = prompt;
            bool wide = false;
            foreach (var l in labels) if (l.Length > 16) wide = true;
            choiceList.GetComponent<GridLayoutGroup>().cellSize = wide ? new Vector2(728, 52) : new Vector2(232, 56);
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                UIKit.Button(choiceList, labels[i], () => onPick(idx), i == 0 ? new Color(0.4f, 0.1f, 0.05f, 0.96f) : null, 20, Color.white);
            }
            FitChoice(labels.Length, wide);
        }

        void FitChoice(int count, bool wide = false, int rowHeight = 0)
        {
            int rows = wide ? count : Mathf.CeilToInt(count / 3f);
            var grid = choiceList.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(10, rowHeight > 0 ? 6 : 10);
            choicePanel.sizeDelta = new Vector2(760, rowHeight > 0 ? 70 + rows * (rowHeight + 6) : 76 + rows * (wide ? 62 : 66));
            choicePanel.gameObject.SetActive(true);
            CloseMenu();
        }

        void ClearChoice()
        {
            for (int i = choiceList.childCount - 1; i >= 0; i--) Destroy(choiceList.GetChild(i).gameObject);
        }

        /// <summary>주사위 구간 선택: 1·2 / 3·4 / 5·6 세 버튼 고정, 나온 눈이 있는 구간만 빨간색 활성화</summary>
        public void ShowBandChoice(string prompt, string[] bandText, bool[] active, string[] rolled, bool coin, Action<int> onPick)
        {
            ClearChoice();
            choicePrompt.text = prompt;
            choiceList.GetComponent<GridLayoutGroup>().cellSize = new Vector2(728, 42);
            for (int i = 0; i < 3; i++)
            {
                int band = i;
                string face = active[i] ? $"   <size=17><color=#ffe0a0>(나온 {(coin ? "코인" : "눈")}: {rolled[i]})</color></size>" : "";
                var b = UIKit.Button(choiceList, $"{Dice.BandNames[i]}   {bandText[i]}{face}", () => onPick(band),
                    active[i] ? new Color(0.72f, 0.08f, 0.06f, 0.97f) : new Color(0.12f, 0.12f, 0.14f, 0.9f), 19, Color.white);
                b.label.supportRichText = true;
                b.Interactable = active[i];
                if (!active[i]) b.label.text = $"{Dice.BandNames[i]}   {bandText[i]}";
            }
            FitChoice(3, true, 42);
        }

        static string KeyHint(GameAction a) => $" <size=17><color=#ffdca0>[{GameSettings.KeyLabel(a)}]</color></size>";

        // ══════════════════════════ 공격 대상 모드 (선택창 없음) ══════════════════════════
        // 공격 몬스터를 대상에게 드래그 · 대상 카드 클릭 · 단축키(칸 번호) · 공격 안 함 키

        RectTransform targetBar, dragLine, dragTip;
        Text targetPrompt, targetHint;
        UIKit.Btn targetSkip;
        readonly List<(CardInstance card, RectTransform tag)> targetTags = new List<(CardInstance, RectTransform)>();
        CardInstance targetAttacker;
        Action<CardInstance> targetPick;
        bool dragging, singleTarget;
        float targetMsgUntil;
        string targetHintText;

        void BuildTargetMode()
        {
            var bg = UIKit.Frame(root, "TargetBar", new Color(0.05f, 0.03f, 0.1f, 0.84f));
            targetBar = bg.rectTransform;
            targetBar.Place(new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(820, 78));
            targetPrompt = UIKit.Txt(bg.transform, "Prompt", "", 25, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            targetPrompt.rectTransform.Anchor(0, 0.46f, 1, 1, 22, 0, 220, 4);
            targetHint = UIKit.Txt(bg.transform, "Hint", "", 17, UIKit.Ink, TextAnchor.MiddleLeft);
            targetHint.rectTransform.Anchor(0, 0, 1, 0.5f, 22, 6, 220, 0);
            targetSkip = UIKit.Button(bg.transform, "공격 안 함", () => PickTarget(null), new Color(0.12f, 0.12f, 0.14f, 0.95f), 20, UIKit.Muted);
            targetSkip.rt.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(196, 56), new Vector2(1, 0.5f));
            targetBar.gameObject.SetActive(false);

            dragLine = UIKit.Img(root, "DragLine", new Color(1f, 0.8f, 0.3f, 0.9f), GameAssets.Rounded, true).rectTransform;
            dragLine.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 10), new Vector2(0, 0.5f));
            dragTip = UIKit.Img(root, "DragTip", new Color(1f, 0.8f, 0.3f, 0.95f), GameAssets.Glow).rectTransform;
            dragTip.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            dragLine.gameObject.SetActive(false);
            dragTip.gameObject.SetActive(false);
        }

        public void ShowTargetMode(CardInstance attacker, List<CardInstance> options, string prompt, Action<CardInstance> onPick)
        {
            HideTargetMode();
            CloseMenu();
            targetAttacker = attacker;
            targetPick = onPick;
            targetPrompt.text = prompt;
            singleTarget = options.Count == 1;
            targetHintText = singleTarget
                ? $"공격 가능한 대상 1장 · [{GameSettings.KeyLabel(GameAction.Attack)}] 공격 · 드래그/클릭"
                : $"대상으로 드래그 · 카드 클릭 · 칸 번호 키 [{GameSettings.KeyLabel(GameAction.Target1)}]~[{GameSettings.KeyLabel(GameAction.Target7)}]";
            targetHint.text = targetHintText;
            targetHint.color = UIKit.Ink;
            targetSkip.label.text = $"공격 안 함 [{GameSettings.KeyLabel(GameAction.NoAttack)}]";
            targetBar.gameObject.SetActive(true);
            targetBar.SetAsLastSibling();
            foreach (var c in options)
            {
                if (c.slot < 0 || singleTarget) continue;     // 대상 고정이면 키 태그 없음 (자동 공격)
                var tag = UIKit.Img(root, "KeyTag", new Color(0.06f, 0.03f, 0.1f, 0.92f), GameAssets.Rounded, true);
                tag.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54, 46));
                var border = UIKit.Img(tag.transform, "Border", UIKit.Gold, GameAssets.RoundedOutline, true);
                border.rectTransform.Fill();
                string keyText = singleTarget ? GameSettings.KeyLabel(GameAction.Attack) : GameSettings.KeyLabel(GameAction.Target1 + c.slot);
                if (singleTarget) tag.rectTransform.sizeDelta = new Vector2(Mathf.Max(54, 20 + keyText.Length * 15), 46);
                var t = UIKit.Txt(tag.transform, "Key", keyText, 24, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Fill();
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                tag.gameObject.SetActive(false);
                targetTags.Add((c, tag.rectTransform));
            }
        }

        public void HideTargetMode()
        {
            targetPick = null;
            targetAttacker = null;
            dragging = false;
            if (targetBar != null) targetBar.gameObject.SetActive(false);
            if (dragLine != null) { dragLine.gameObject.SetActive(false); dragTip.gameObject.SetActive(false); }
            foreach (var t in targetTags) if (t.tag != null) Destroy(t.tag.gameObject);
            targetTags.Clear();
        }

        void PickTarget(CardInstance c)
        {
            var pick = targetPick;
            if (pick == null) return;
            pick(c);
        }

        Vector2 ToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var lp);
            return lp;
        }

        CardInstance RaycastCard(Vector2 screen)
        {
            var cam = g.board.cam;
            if (cam == null) return null;
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(screen), 200f);
            CardInstance best = null;
            float bestD = float.MaxValue;
            foreach (var h in hits)
            {
                var v = h.collider.GetComponentInParent<CardView>();
                if (v != null && v.card != null && h.distance < bestD) { best = v.card; bestD = h.distance; }
            }
            return best;
        }

        void Update()
        {
            UpdateSpeedBoost();
            UpdateAutoMode();
            if (g == null || g.players[0] == null) return;
            if (CardZoom.IsOpen)
            {
                // 확대 중: Esc 는 확대만 닫고, 단축키·드래그 공격 입력은 받지 않는다
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) CardZoom.Close();
                UpdateRaid();
                UpdateStory();
                return;
            }
            if (HandleEscape()) return;
            UpdateRaid();
            UpdateStory();
            if (pileOwner != null) UpdatePileTip();
            if (targetPick != null) { UpdateTargetMode(); return; }
            if (battleMode) { UpdateBattleMode(); return; }
            UpdatePlanDrag();
            float left = g.PlanSecondsLeft;
            if (left >= 0) phaseText.text = (SimulTurnShown ? TurnLabel() + " · 배치" : "배치 (뒷면)") + $" · <color={(left <= 5 ? "#ff6655" : "#ffd77a")}>남은 시간 {Mathf.CeilToInt(left)}초</color>";
            if (!g.IsHumanMain) return;
            if (TutorialGuide.Active != null && TutorialGuide.Active.Blocking) return;   // 튜토리얼 강제 단계: 단축키로 건너뛰지 않게
            if (GameSettings.Pressed(GameAction.BattlePhase)) { CloseMenu(); g.RequestBattle(); }
            else if (GameSettings.Pressed(GameAction.EndTime)) { CloseMenu(); g.RequestEndTime(); }
            else if (GameSettings.Pressed(GameAction.HeroSkill) && g.CanUseHero(g.Human)) { CloseMenu(); g.HumanAct(g.UseHero(g.Human)); }
        }

        // ── 동시 룰 배치: 드래그 앤 드롭 (패 → 빈 칸 · 칸 → 다른 칸) ──
        CardInstance planPress, planDrag;
        Vector2 planPressPos;
        int planHoverSlot = -1, suppressClickFrame = -1;
        Image planMark;

        void UpdatePlanDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!g.Simul || !g.IsHumanMain || g.busy || g.ChoicePending) { CancelPlanDrag(); return; }
            Vector2 mp = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                var c = RaycastCard(mp);
                planPress = c != null && c.owner == g.Human && (c.zone == Zone.Hand || c.zone == Zone.Slot) ? c : null;
                if (planPress != null && TutorialGuide.Active != null && !TutorialGuide.Active.AllowsCard(planPress)) planPress = null;   // 튜토리얼: 짚어 준 카드만
                planPressPos = mp;
            }
            if (planPress != null && planDrag == null && mouse.leftButton.isPressed && (mp - planPressPos).magnitude > 14f)
            {
                planDrag = planPress;
                CloseMenu();
            }
            if (planDrag == null)
            {
                if (mouse.leftButton.wasReleasedThisFrame) planPress = null;
                return;
            }

            var cam = g.board.cam;
            if (cam == null || planDrag.view == null || (planDrag.zone != Zone.Hand && planDrag.zone != Zone.Slot)) { CancelPlanDrag(); return; }
            var ray = cam.ScreenPointToRay(mp);
            bool hitPlane = new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float dist);
            Vector3 wp = hitPlane ? ray.GetPoint(dist) : Vector3.zero;
            planHoverSlot = hitPlane ? NearestHumanSlot(wp) : -1;
            bool fromHand = planDrag.zone == Zone.Hand;
            bool valid = planHoverSlot >= 0 && (fromHand ? g.CanPlaceAt(planDrag, planHoverSlot) : g.CanMoveSlotTo(planDrag, planHoverSlot))
                         && (TutorialGuide.Active == null || TutorialGuide.Active.AllowsSlot(planHoverSlot));   // 튜토리얼: 짚어 준 칸만

            // 카드는 포인터를 따라 보드 위에 떠 있다
            var v = planDrag.view;
            v.hoverLift = false;
            if (hitPlane) v.SetTarget(wp + new Vector3(0, 0.5f, 0), Quaternion.identity, fromHand || !planDrag.faceDown, 1.05f);

            if (planMark == null)
            {
                planMark = UIKit.Img(root, "PlanMark", Color.white, GameAssets.Glow);
                planMark.raycastTarget = false;
            }
            planMark.gameObject.SetActive(planHoverSlot >= 0);
            if (planHoverSlot >= 0)
            {
                planMark.rectTransform.anchoredPosition = ToLocal(cam.WorldToScreenPoint(g.board.SlotPos(g.Human, planHoverSlot)));
                float s = 190f + Mathf.Sin(Time.unscaledTime * 8f) * 12f;
                planMark.rectTransform.sizeDelta = new Vector2(s * 0.8f, s);
                planMark.color = valid ? new Color(0.3f, 1f, 0.5f, 0.75f) : new Color(1f, 0.3f, 0.25f, 0.6f);
                planMark.transform.SetAsLastSibling();
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                var c = planDrag;
                int slot = planHoverSlot;
                CancelPlanDrag();
                suppressClickFrame = Time.frameCount + 1;
                if (valid)
                {
                    if (fromHand) g.HumanAct(c.IsMonster ? g.Summon(c, true, slot) : g.SetMagic(c, slot));
                    else g.HumanAct(g.MoveSlotTo(c, slot));
                }
                else if (slot >= 0) g.Log(fromHand ? "그 칸에는 놓을 수 없습니다" : "그 칸으로는 옮길 수 없습니다");
            }
        }

        /// <summary>오토 전환: 끌고 있던 배치 카드를 놓는다</summary>
        public void CancelPlanInput() => CancelPlanDrag();

        void CancelPlanDrag()
        {
            bool was = planDrag != null;
            planPress = planDrag = null;
            planHoverSlot = -1;
            if (planMark != null) planMark.gameObject.SetActive(false);
            if (was) g.RefreshAll();       // 제자리로
        }

        int NearestHumanSlot(Vector3 wp)
        {
            for (int i = 0; i < PlayerState.SlotCount; i++)
            {
                var sp = g.board.SlotPos(g.Human, i);
                if (Mathf.Abs(wp.x - sp.x) < BoardView.SlotSpacing * 0.5f && Mathf.Abs(wp.z - sp.z) < 0.85f) return i;
            }
            return -1;
        }

        void UpdateTargetMode()
        {
            var cam = g.board.cam;
            foreach (var (c, tag) in targetTags)
            {
                bool vis = c.view != null && (c.zone == Zone.Slot || c.zone == Zone.Hero);
                tag.gameObject.SetActive(vis);
                if (vis) tag.anchoredPosition = ToLocal(cam.WorldToScreenPoint(c.view.transform.position + new Vector3(0, 0.25f, -0.95f)));
            }
            if (Time.unscaledTime > targetMsgUntil && targetHint.text != targetHintText) { targetHint.text = targetHintText; targetHint.color = UIKit.Ink; }

            if (singleTarget && GameSettings.Pressed(GameAction.Attack) && targetTags.Count > 0) { PickTarget(targetTags[0].card); return; }
            // 단축키: 상대 칸 번호 (왼쪽부터 1~7) — 자유 대상일 때만
            var opp = targetAttacker.owner.opponent;
            for (int i = 0; i < PlayerState.SlotCount; i++)
            {
                if (!GameSettings.Pressed(GameAction.Target1 + i)) continue;
                var t = opp.slots[i];
                if (t != null && g.IsChoiceOption(t)) { PickTarget(t); return; }
                targetHint.text = singleTarget ? $"대상 지정 불가 — [{GameSettings.KeyLabel(GameAction.Attack)}] 로 공격하거나 공격하지 않습니다"
                                               : $"{i + 1}번 칸에는 공격할 수 있는 몬스터가 없습니다";
                targetHint.color = new Color(1f, 0.5f, 0.45f);
                targetMsgUntil = Time.unscaledTime + 1.6f;
            }
            if (GameSettings.Pressed(GameAction.NoAttack)) { PickTarget(null); return; }

            // 드래그 앤 드롭: 공격 몬스터를 눌러 대상에게 끌어다 놓기
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && RaycastCard(mp) == targetAttacker) dragging = true;
            if (!dragging) return;
            if (targetAttacker.view == null) { dragging = false; return; }
            var hover = RaycastCard(mp);
            bool valid = hover != null && g.IsChoiceOption(hover) && hover.view != null;
            DrawArrow(targetAttacker, valid ? hover : null, mp);
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                HideArrow();
                if (valid) PickTarget(hover);
            }
        }

        // ══════════════════════════ 공격 화살표 (속성별 VARCO 이펙트) ══════════════════════════

        /// <summary>공격 몬스터 → 대상(없으면 포인터) 화살표. 대상이 유효하면 굵고 선명하게, 끝에 맥동 글로우</summary>
        void DrawArrow(CardInstance from, CardInstance target, Vector2 pointer, float grow = 1f, float alpha = 1f)
        {
            var cam = g.board.cam;
            if (cam == null || from == null || from.view == null) { HideArrow(); return; }
            bool valid = target != null && target.view != null;
            Vector2 a = ToLocal(cam.WorldToScreenPoint(from.view.transform.position));
            Vector2 b = valid ? ToLocal(cam.WorldToScreenPoint(target.view.transform.position)) : ToLocal(pointer);
            b = Vector2.Lerp(a, b, grow);
            var d = b - a;
            var img = dragLine.GetComponent<Image>();
            var sp = GameAssets.AttackArrow(from.data.element);
            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 12f);
            float thick;
            if (sp != null)
            {
                thick = (valid ? 130f : 104f) * pulse;
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = sp.rect.height / thick;    // 화살촉·꼬리(9-슬라이스 가장자리)가 두께에 비례
                img.color = new Color(1f, 1f, 1f, (valid ? 1f : 0.62f) * alpha);
                float min = (sp.border.x + sp.border.z) * thick / sp.rect.height;
                dragLine.sizeDelta = new Vector2(Mathf.Max(d.magnitude, min), thick);
            }
            else
            {
                thick = valid ? 12 : 8;
                img.sprite = GameAssets.Rounded;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
                var ec = CardData.ElementColor(from.data.element);
                img.color = new Color(ec.r, ec.g, ec.b, (valid ? 0.95f : 0.7f) * alpha);
                dragLine.sizeDelta = new Vector2(d.magnitude, thick);
            }
            dragLine.anchoredPosition = a;
            dragLine.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            dragLine.gameObject.SetActive(true);
            dragLine.SetAsLastSibling();
            dragTip.gameObject.SetActive(valid);
            if (valid)
            {
                var tc = Color.Lerp(CardData.ElementColor(from.data.element), Color.white, 0.25f);
                tc.a = 0.75f * alpha;
                var tip = dragTip.GetComponent<Image>();
                tip.color = tc;
                dragTip.anchoredPosition = b;
                dragTip.sizeDelta = Vector2.one * (150 + Mathf.Sin(Time.unscaledTime * 10) * 14);
            }
        }

        void HideArrow()
        {
            if (dragLine != null) dragLine.gameObject.SetActive(false);
            if (dragTip != null) dragTip.gameObject.SetActive(false);
        }

        /// <summary>공격 연출: 공격 몬스터에서 대상까지 화살표가 뻗어 나갔다가 사라진다 (사람·AI 공통)</summary>
        public IEnumerator AttackArrow(CardInstance a, CardInstance t)
        {
            if (a == null || t == null || a.view == null || t.view == null || g == null) yield break;
            float dur = a.owner != null && a.owner.isHuman ? 0.16f : 0.32f, hold = 0.22f, fade = 0.16f;
            for (float e = 0; e < dur; e += Time.deltaTime) { DrawArrow(a, t, Vector2.zero, Mathf.SmoothStep(0, 1, e / dur)); yield return null; }
            for (float e = 0; e < hold; e += Time.deltaTime) { DrawArrow(a, t, Vector2.zero); yield return null; }
            for (float e = 0; e < fade; e += Time.deltaTime) { DrawArrow(a, t, Vector2.zero, 1f, 1f - e / fade); yield return null; }
            HideArrow();
        }

        // ══════════════════════════ 배틀 모드 (드래그앤드롭 공격) ══════════════════════════
        // 빛나는(주황) 아군 몬스터를 상대 몬스터 / 상대 영웅에게 끌어다 놓으면 공격. 하늘색 = t/half 마법 (클릭 발동).

        RectTransform battleBar;
        Text battlePrompt, battleHint;
        UIKit.Btn battleEndBtn;
        bool battleMode;
        CardInstance dragAttacker;
        float battleMsgUntil;
        readonly List<(CardInstance card, RectTransform tag, Text text)> atkTags = new List<(CardInstance, RectTransform, Text)>();
        readonly List<(CardInstance card, RectTransform mark)> targetMarks = new List<(CardInstance, RectTransform)>();

        void BuildBattleMode()
        {
            var bg = UIKit.Frame(root, "BattleBar", new Color(0.1f, 0.02f, 0.02f, 0.86f));
            battleBar = bg.rectTransform;
            battleBar.Place(new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(900, 78));
            battlePrompt = UIKit.Txt(bg.transform, "Prompt", "", 25, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            battlePrompt.rectTransform.Anchor(0, 0.46f, 1, 1, 22, 0, 240, 4);
            battleHint = UIKit.Txt(bg.transform, "Hint", "", 17, UIKit.Ink, TextAnchor.MiddleLeft);
            battleHint.rectTransform.Anchor(0, 0, 1, 0.5f, 22, 6, 240, 0);
            battleEndBtn = UIKit.Button(bg.transform, "배틀 종료", () => { if (g != null) g.RequestEndBattle(); }, new Color(0.35f, 0.06f, 0.04f, 0.96f), 21, Color.white);
            battleEndBtn.rt.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(216, 56), new Vector2(1, 0.5f));
            battleBar.gameObject.SetActive(false);
        }

        string BattleHintText => "주황빛 몬스터를 상대에게 드래그해 공격 · 하늘빛 마법은 클릭 발동";

        public void ShowBattleMode()
        {
            HideBattleMode();
            CloseMenu();
            battleMode = true;
            battlePrompt.text = "배틀 페이즈 — 공격할 몬스터를 상대에게 드래그";
            battleHint.text = BattleHintText;
            battleHint.color = UIKit.Ink;
            battleEndBtn.label.text = "배틀 종료" + KeyHint(GameAction.EndTime);
            battleBar.gameObject.SetActive(true);
            battleBar.SetAsLastSibling();
            foreach (var c in g.Human.slots)
            {
                if (c == null || !g.CanAttackNow(c)) continue;
                var tag = UIKit.Img(root, "AtkTag", new Color(0.12f, 0.03f, 0.02f, 0.92f), GameAssets.Rounded, true);
                tag.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 40));
                var border = UIKit.Img(tag.transform, "Border", new Color(1f, 0.5f, 0.25f), GameAssets.RoundedOutline, true);
                border.rectTransform.Fill();
                var t = UIKit.Txt(tag.transform, "N", "", 22, new Color(1f, 0.85f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Fill();
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                tag.gameObject.SetActive(false);
                atkTags.Add((c, tag.rectTransform, t));
            }
        }

        public void HideBattleMode()
        {
            battleMode = false;
            dragAttacker = null;
            if (battleBar != null) battleBar.gameObject.SetActive(false);
            HideArrow();
            foreach (var t in atkTags) if (t.tag != null) Destroy(t.tag.gameObject);
            atkTags.Clear();
            ClearTargetMarks();
        }

        void ClearTargetMarks()
        {
            foreach (var m in targetMarks) if (m.mark != null) Destroy(m.mark.gameObject);
            targetMarks.Clear();
        }

        void BattleMessage(string msg)
        {
            battleHint.text = msg;
            battleHint.color = new Color(1f, 0.5f, 0.45f);
            battleMsgUntil = Time.unscaledTime + 1.8f;
        }

        void UpdateBattleMode()
        {
            var cam = g.board.cam;
            if (cam == null) return;
            foreach (var (c, tag, text) in atkTags)
            {
                bool vis = c.view != null && c.zone == Zone.Slot && g.CanAttackNow(c) && dragAttacker != c;
                tag.gameObject.SetActive(vis);
                if (!vis) continue;
                tag.anchoredPosition = ToLocal(cam.WorldToScreenPoint(c.view.transform.position + new Vector3(0, 0.25f, -0.95f)));
                text.text = $"⚔ {g.AttacksLeft(c)}";
            }
            if (Time.unscaledTime > battleMsgUntil && battleHint.text != BattleHintText) { battleHint.text = BattleHintText; battleHint.color = UIKit.Ink; }
            if (GameSettings.Pressed(GameAction.EndTime) || GameSettings.Pressed(GameAction.NoAttack)) { g.RequestEndBattle(); return; }

            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                var c = RaycastCard(mp);
                if (c != null && c.owner == g.Human && c.zone == Zone.Slot && c.IsMonster)
                {
                    if (g.CanAttackNow(c))
                    {
                        dragAttacker = c;
                        ClearTargetMarks();
                        foreach (var t in g.AttackTargets(c))
                        {
                            var mark = UIKit.Img(root, "TargetMark", new Color(1f, 0.3f, 0.2f, 0.5f), GameAssets.Glow);
                            mark.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
                            targetMarks.Add((t, mark.rectTransform));
                        }
                    }
                    else if (c.faceDown) BattleMessage($"뒷면 몬스터는 공격할 수 없습니다");
                    else if (c.stunUntil >= g.time) BattleMessage($"{c.Name}: 행동 불능 — 이번 타임 공격할 수 없습니다");
                    else BattleMessage($"{c.Name}: 이번 배틀페이즈 공격을 모두 사용했습니다");
                }
            }
            if (dragAttacker == null) return;
            if (dragAttacker.view == null || !g.CanAttackNow(dragAttacker)) { dragAttacker = null; HideArrow(); ClearTargetMarks(); return; }

            var hover = RaycastCard(mp);
            bool valid = hover != null && hover.view != null && g.AttackTargets(dragAttacker).Contains(hover);
            foreach (var (t, mark) in targetMarks)
            {
                bool vis = t.view != null;
                mark.gameObject.SetActive(vis);
                if (!vis) continue;
                mark.anchoredPosition = ToLocal(cam.WorldToScreenPoint(t.view.transform.position));
                float s = (t == hover ? 190f : 150f) + Mathf.Sin(Time.unscaledTime * 6f) * 10f;
                mark.sizeDelta = new Vector2(s, s);
            }
            DrawArrow(dragAttacker, valid ? hover : null, mp);
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                var a = dragAttacker;
                dragAttacker = null;
                HideArrow();
                ClearTargetMarks();
                if (valid) g.TryHumanAttack(a, hover);
                else if (hover != null && hover.owner == a.owner.opponent && (hover.IsMonster || hover.zone == Zone.Hero))
                    BattleMessage(hover.zone == Zone.Hero ? "상대 몬스터가 있으면 영웅을 직접 공격할 수 없습니다" : "[도발] 몬스터를 먼저 공격해야 합니다");
            }
        }

        public void HideChoice()
        {
            ClearChoice();
            choicePanel.gameObject.SetActive(false);
        }

        // ══════════════════════════ 연출 ══════════════════════════

        public IEnumerator Banner(string main, string sub, float hold = 0.9f)
        {
            bannerMain.text = main;
            bannerSub.text = sub;
            bannerRoot.gameObject.SetActive(true);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 5f;
                bannerGroup.alpha = t;
                bannerRoot.localScale = new Vector3(1, Mathf.Lerp(0.3f, 1f, t), 1);
                yield return null;
            }
            yield return new WaitForSeconds(hold);
            while (t > 0f)
            {
                t -= Time.deltaTime * 4f;
                bannerGroup.alpha = t;
                yield return null;
            }
            bannerRoot.gameObject.SetActive(false);
        }

        public struct ClashSide
        {
            public CardInstance card;
            public int baseAtk, hp, die, power;
            public bool dies;
            public bool coin;
            public DiceEffect eff;
        }

        static string ClashText(ClashSide s)
        {
            string face = s.coin ? (s.die >= 5 ? "앞면" : "뒷면") : $"주사위 {s.die}";
            return $"<size=24>{s.card.owner.name} · {s.card.Name}</size>\n" +
                   $"<size=22>ATK {s.baseAtk} · HP {s.hp} · {face} [{Dice.BandNames[Dice.Band(s.die)]}]\n{s.eff.Describe()}</size>\n<size=48>{s.power}</size>" +
                   (s.dies ? "<size=22><color=#ff7060> 격파</color></size>" : "");
        }

        /// <summary>d == null 이면 직접 공격</summary>
        public IEnumerator ShowClash(ClashSide a, ClashSide? d)
        {
            clashLeft.text = ClashText(a);
            if (d.HasValue)
            {
                clashRight.text = ClashText(d.Value);
                bool da = a.dies, dd = d.Value.dies;
                clashMid.text = da && dd ? "상호 격파" : dd ? "◀ 격파" : da ? "격파 ▶" : "생존";
            }
            else
            {
                clashRight.text = $"<size=24>{a.card.owner.opponent.name} 영웅</size>\n<size=48>-{a.power}</size>";
                clashMid.text = "DIRECT";
            }
            clashRoot.gameObject.SetActive(true);
            clashGroup.alpha = 0;
            float t = 0;
            while (t < 1f) { t += Time.deltaTime * 5f; clashGroup.alpha = t; yield return null; }
            yield return new WaitForSeconds(1.6f);
            while (t > 0f) { t -= Time.deltaTime * 4f; clashGroup.alpha = t; yield return null; }
            clashRoot.gameObject.SetActive(false);
        }

        public void FlashHp(PlayerState p, bool heal)
        {
            var hp = p.isHuman ? humanPanel : cpuPanel;
            StartCoroutine(FlashRoutine(hp.hpFlash, heal ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.2f, 0.15f)));
        }

        IEnumerator FlashRoutine(Image img, Color c)
        {
            float t = 1f;
            while (t > 0)
            {
                t -= Time.deltaTime * 2f;
                img.color = new Color(c.r, c.g, c.b, t);
                yield return null;
            }
            img.color = new Color(c.r, c.g, c.b, 0);
        }

        public void FloatText(Vector3 world, string text, Color c)
        {
            StartCoroutine(FloatRoutine(world, text, c));
        }

        IEnumerator FloatRoutine(Vector3 world, string text, Color c)
        {
            var t = UIKit.Txt(root, "Float", text, 72, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.sizeDelta = new Vector2(400, 120);
            t.Outline(new Color(0, 0, 0, 0.9f), 3f);
            float el = 0;
            var cam = Camera.main;
            while (el < 1.2f)
            {
                el += Time.deltaTime;
                Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * (0.5f + el * 0.8f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var lp);
                t.rectTransform.anchoredPosition = lp;
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                t.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(1.6f - el * 1.3f));
                yield return null;
            }
            Destroy(t.gameObject);
        }

        public void ShowGameOver(bool won)
        {
            gameOverTitle.text = won ? "VICTORY" : "DEFEAT";
            gameOverTitle.color = won ? UIKit.Gold : new Color(0.8f, 0.35f, 0.35f);
            bool raid = g != null && g.raid != null;
            if (raid) gameOverTitle.text = won ? "RAID CLEAR" : "RAID FAILED";
            gameOverSub.text = raid ? (won ? $"{g.raid.boss.name} 토벌 성공!" : $"{g.raid.boss.name}에게 패배했습니다") : "패배한 쪽이 다음 매치의 필드를 결정합니다";
            gameOverRematch.label.text = raid ? "다시 도전" : "재대결";
            if (g != null && g.story != null)
            {
                gameOverTitle.text = won ? "STAGE CLEAR" : "DEFEAT";
                gameOverSub.text = $"STAGE {g.story.number} · {g.story.title}" + (!won && g.story.battle.allowLose ? "  —  이 전투는 져도 이야기가 이어집니다" : "");
                gameOverRematch.label.text = g.StoryProceeds ? "이야기 계속 ▶" : "다시 도전";
            }
            if (g != null && g.mode == MatchMode.Puzzle && g.puzzle != null)
            {
                gameOverTitle.text = won ? "PUZZLE CLEAR" : "PUZZLE FAILED";
                gameOverSub.text = $"마법진 퍼즐 · {g.puzzle.title}";
                int idx = Puzzles.All.IndexOf(g.puzzle);
                gameOverRematch.label.text = won ? (idx + 1 < Puzzles.All.Count ? "다음 퍼즐 ▶" : "다시 풀기") : "다시 도전";
            }
            if (g != null && g.mode == MatchMode.Arena)
            {
                gameOverSub.text = $"원소 투기장 · {Arena.S.wins}승 {Arena.S.losses}패";
                gameOverRematch.label.text = Arena.Ready ? "다음 대전 ▶" : "도전으로";
            }
            if (g != null && g.mode == MatchMode.Event)
                gameOverSub.text = $"원소 이변전 · {ElementEvent.Name(g.eventRule)}";
            if (g != null && g.mode == MatchMode.Practice)
            {
                gameOverTitle.text = "훈련 종료";
                gameOverSub.text = "훈련장 · 수련 골렘";
                gameOverRematch.label.text = "다시 훈련";
            }
            if (g != null && g.mode == MatchMode.Tutorial)
            {
                gameOverTitle.text = won ? "튜토리얼 완료" : "튜토리얼 종료";
                gameOverSub.text = "튜토리얼 · 수련 CPU";
                gameOverRematch.label.text = "다시 배우기";
            }
            gameOverRoot.gameObject.SetActive(true);
            gameOverRoot.SetAsLastSibling();
        }

        Text gameOverReward, gameOverSub;
        UIKit.Btn gameOverRematch;

        public void SetReward(string text)
        {
            if (gameOverReward != null) gameOverReward.text = text;
        }

        public void HideGameOver() => gameOverRoot.gameObject.SetActive(false);
    }
}
