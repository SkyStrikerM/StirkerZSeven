using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 퀘스트 탭 위쪽 탭: [퀘스트 · 업적] [세븐 패스] [출석체크].
    /// 팩 보상은 상점 팩 개봉 연출(무료 개봉)로 바로 연다.
    /// </summary>
    public partial class MainMenu
    {
        RectTransform passPanel, attendPanel, passTrack;
        readonly List<UIKit.Btn> questTabs = new List<UIKit.Btn>();
        int questTab;
        Text passTitle, passLevel, passInfo;
        Image passFill;
        UIKit.Btn passPremiumBtn, passClaimAllBtn, attendBtn;
        readonly List<(Image bg, Text t, Image glow, Image icon, Text mark)> attendCells = new List<(Image, Text, Image, Image, Text)>();

        /// <summary>보상 그림 (출석체크 · 세븐 패스 공용): 오팔 · CP 결정(등급별) · 부스터팩 · 랜덤 카드(Rare · Seven 가공 · 유니크)</summary>
        static Sprite AttendIcon(PassReward k)
        {
            switch (k)
            {
                case PassReward.Opal: return GameAssets.TexSprite("opal");
                case PassReward.CpCommon: return GameAssets.TexSprite("cp_common") ?? GameAssets.TexSprite("opal");
                case PassReward.CpUncommon: return GameAssets.TexSprite("cp_uncommon") ?? GameAssets.TexSprite("opal");
                case PassReward.CpRare: return GameAssets.TexSprite("cp_rare") ?? GameAssets.TexSprite("cp_uncommon");
                case PassReward.Pack: return GameAssets.TexSprite(BoosterPack.Latest.art) ?? GameAssets.TexSprite("card_back");
                case PassReward.RareCard: return GameAssets.TexSprite("reward_rare_card") ?? GameAssets.TexSprite("card_back");
                case PassReward.SevenCard: return GameAssets.TexSprite("reward_seven_card") ?? GameAssets.TexSprite("reward_rare_card");
                case PassReward.UniqueCard: return GameAssets.TexSprite("reward_unique_card") ?? GameAssets.TexSprite("reward_rare_card");
                default: return GameAssets.TexSprite("opal");
            }
        }
        Text attendInfo;

        void BuildQuestTabs(RectTransform page)
        {
            var strip = UIKit.Rect("Tabs", page).Anchor(0, 1, 1, 1, 0, -60, 0, 0);
            var hl = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childControlWidth = false; hl.childControlHeight = true; hl.childForceExpandWidth = false;
            string[] names = { "퀘스트 · 업적", "세븐 패스", "출석체크" };
            string[] icons = { "tab_quest", "tab_pass", "tab_attend" };
            questTabs.Clear();
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var b = UIKit.Button(strip, names[i], () => ShowQuestTab(idx), Dark, 24);
                b.rt.sizeDelta = new Vector2(250, 56);
                b.Icon(GameAssets.TexSprite(icons[i]), 46);
                questTabs.Add(b);
            }
            passPanel = BuildPassPanel(page);
            attendPanel = BuildAttendPanel(page);
            ShowQuestTab(0);
        }

        void ShowQuestTab(int i)
        {
            questTab = i;
            for (int k = 0; k < questTabs.Count; k++) questTabs[k].SetSelected(k == i);
            quests.SetBodyVisible(i == 0);
            passPanel.gameObject.SetActive(i == 1);
            attendPanel.gameObject.SetActive(i == 2);
            if (i == 0) quests.Refresh();
            if (i == 1) RefreshPass();
            if (i == 2) RefreshAttend();
        }

        public void TestQuestTab(int i) => ShowQuestTab(i);

        // ══════════════════════════ 세븐 패스 ══════════════════════════

        RectTransform BuildPassPanel(RectTransform page)
        {
            var f = UIKit.Frame(page, "SevenPass", Dark);
            f.rectTransform.Anchor(0, 0, 1, 1, 0, 0, 0, 72);
            var p = f.transform;
            passTitle = UIKit.Txt(p, "Title", "", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            passTitle.rectTransform.Anchor(0, 1, 0.6f, 1, 28, -64, 0, 20);
            passTitle.Section(UIKit.SecMe, 26);
            passInfo = UIKit.Txt(p, "Info", "", 18, UIKit.Muted, TextAnchor.MiddleLeft);
            passInfo.rectTransform.Anchor(0, 1, 0.7f, 1, 30, -104, 0, 70);
            passLevel = UIKit.Txt(p, "Level", "", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            passLevel.rectTransform.Anchor(0, 1, 0.3f, 1, 30, -152, 0, 110);
            var barBg = UIKit.Img(p, "Bar", new Color(0.02f, 0.02f, 0.05f, 1f), GameAssets.Rounded, true);
            barBg.rectTransform.Anchor(0.22f, 1, 0.62f, 1, 0, -144, 0, 118);
            passFill = UIKit.Img(barBg.transform, "Fill", new Color(0.55f, 0.85f, 1f), GameAssets.Rounded, true);
            passPremiumBtn = UIKit.Button(p, "", () =>
            {
                if (SevenPass.BuyPremium(out string msg)) { Sfx.Play("reveal2"); RefreshPass(); RefreshTop(); }
                Toast(msg);
            }, new Color(0.45f, 0.3f, 0.05f, 0.97f), 22, Color.white);
            passPremiumBtn.rt.Place(new Vector2(1, 1), new Vector2(-250, -24), new Vector2(300, 64), new Vector2(1, 1));
            passClaimAllBtn = UIKit.Button(p, "모두 받기", ClaimAllPass, Red, 22, Color.white);
            passClaimAllBtn.rt.Place(new Vector2(1, 1), new Vector2(-24, -24), new Vector2(210, 64), new Vector2(1, 1));
            var rowLab1 = UIKit.Txt(p, "Free", "무료", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            rowLab1.rectTransform.Anchor(0, 0, 0, 1, 10, 0, -100, 0);
            rowLab1.rectTransform.anchorMin = new Vector2(0, 0.5f); rowLab1.rectTransform.anchorMax = new Vector2(0, 0.5f);
            rowLab1.rectTransform.sizeDelta = new Vector2(90, 40); rowLab1.rectTransform.anchoredPosition = new Vector2(55, 30);
            var rowLab2 = UIKit.Txt(p, "Prem", "프리미엄", 20, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            rowLab2.rectTransform.anchorMin = rowLab2.rectTransform.anchorMax = new Vector2(0, 0.5f);
            rowLab2.rectTransform.sizeDelta = new Vector2(90, 40); rowLab2.rectTransform.anchoredPosition = new Vector2(55, -200);
            var view = UIKit.Rect("Track", p).Anchor(0, 0, 1, 1, 104, 24, 20, 176);
            view.HitArea();
            view.gameObject.AddComponent<RectMask2D>();
            passTrack = UIKit.Rect("Content", view);
            passTrack.anchorMin = new Vector2(0, 0); passTrack.anchorMax = new Vector2(0, 1); passTrack.pivot = new Vector2(0, 0.5f);
            passTrack.offsetMin = passTrack.offsetMax = Vector2.zero;
            var hl = passTrack.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childControlWidth = false; hl.childControlHeight = true; hl.childForceExpandHeight = true;
            passTrack.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = passTrack; sr.viewport = view; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 50;
            return f.rectTransform;
        }

        void RefreshPass()
        {
            var s = SevenPass.S;
            int lv = SevenPass.Level;
            var left = SevenPass.UntilSeasonEnd;
            passTitle.text = $"세븐 패스 · 시즌 {SevenPass.SeasonNumber}";
            passInfo.text = $"시즌 종료까지 {left.Days}일 {left.Hours}시간 · XP: 대전 +{SevenPass.XpMatch} (승리 +{SevenPass.XpWin}) · 일일 퀘스트 +{SevenPass.XpDaily} · 퍼즐 첫 클리어 +{SevenPass.XpPuzzle} · 출석 +{SevenPass.XpAttend}";
            passLevel.text = $"Lv {lv} / {SevenPass.Levels}";
            passFill.rectTransform.Anchor(0, 0, Mathf.Clamp01((float)SevenPass.XpInLevel / SevenPass.XpPerLevel), 1, 2, 2, 2, 2);
            passPremiumBtn.label.text = s.premium ? "프리미엄 ✔" : $"프리미엄 열기 · 오팔 {SevenPass.PremiumPrice:N0}";
            passPremiumBtn.Interactable = !s.premium;
            passClaimAllBtn.Interactable = SevenPass.ClaimableCount > 0;
            UIKit.Clear(passTrack);
            for (int i = 1; i <= SevenPass.Levels; i++)
            {
                int level = i;
                var col = UIKit.Rect("Lv" + i, passTrack);
                col.sizeDelta = new Vector2(150, 0);
                bool reached = i <= lv;
                var head = UIKit.Txt(col, "Lv", $"Lv {i}", 20, reached ? UIKit.Gold : UIKit.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                head.rectTransform.Anchor(0, 1, 1, 1, 0, -34, 0, 0);
                PassCell(col, level, false, new Vector4(0, 0.5f, 1, 1), 40);
                PassCell(col, level, true, new Vector4(0, 0, 1, 0.5f), 6);
            }
        }

        void PassCell(RectTransform col, int lv, bool premium, Vector4 a, float topPad)
        {
            var r = premium ? SevenPass.Premium(lv) : SevenPass.Free(lv);
            bool claimed = SevenPass.Claimed(lv, premium);
            bool can = SevenPass.CanClaim(lv, premium);
            bool locked = premium && !SevenPass.S.premium;
            var bg = UIKit.Img(col, premium ? "P" : "F", premium ? new Color(0.2f, 0.14f, 0.04f, 0.95f) : new Color(0.08f, 0.07f, 0.14f, 0.95f), GameAssets.Rounded, true);
            bg.rectTransform.Anchor(a.x, a.y, a.z, a.w, 2, 6, 2, topPad);
            if (can)
            {
                var border = UIKit.Img(bg.transform, "Ready", UIKit.Gold, GameAssets.RoundedOutline, true);
                border.rectTransform.Fill();
            }
            bool big = r.kind == PassReward.UniqueCard || r.kind == PassReward.SevenCard || r.kind == PassReward.Pack;
            // 보상 그림 (받은 칸은 흐리게)
            var icon = UIKit.Img(bg.transform, "Icon", claimed ? new Color(1, 1, 1, 0.4f) : Color.white, AttendIcon(r.kind));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.Anchor(0.14f, 0.5f, 0.86f, 0.97f, 0, 0, 0, 4);
            var t = UIKit.Txt(bg.transform, "T", SevenPass.Label(r), big ? 16 : 15, big ? new Color(1f, 0.85f, 0.5f) : UIKit.Ink, TextAnchor.MiddleCenter, big ? FontStyle.Bold : FontStyle.Normal);
            t.rectTransform.Anchor(0, 0.33f, 1, 0.5f, 4, 0, 4, 0);
            t.Outline(new Color(0, 0, 0, 0.8f), 1);
            var b = UIKit.Button(bg.transform, claimed ? "✔ 받음" : can ? "받기" : locked ? "잠김" : "-", () => ClaimPass(lv, premium),
                can ? Red : new Color(0.1f, 0.1f, 0.14f, 0.9f), 18, can ? Color.white : UIKit.Muted);
            b.rt.Anchor(0.08f, 0.05f, 0.92f, 0.31f);
            b.Interactable = can;
        }

        void ClaimPass(int lv, bool premium)
        {
            string got = SevenPass.Claim(lv, premium, out int packs);
            if (got == null) return;
            Sfx.Play("reveal1");
            Toast($"세븐 패스 Lv {lv} — {got}");
            RefreshPass();
            RefreshTop();
            if (packs > 0) OpenPacks(BoosterPack.Latest, packs);
            RevealCards($"세븐 패스 Lv {lv} 보상", SevenPass.TakeGranted());   // 카드 보상은 뒤집어 공개
        }

        void ClaimAllPass()
        {
            int packs = 0, n = 0;
            for (int lv = 1; lv <= SevenPass.Levels; lv++)
                foreach (bool prem in new[] { false, true })
                    if (SevenPass.CanClaim(lv, prem)) { SevenPass.Claim(lv, prem, out int p); packs += p; n++; }
            if (n == 0) return;
            Sfx.Play("reveal2");
            Toast($"세븐 패스 보상 {n}개를 받았습니다" + (packs > 0 ? $" · 부스터팩 {packs}팩 개봉" : ""));
            RefreshPass();
            RefreshTop();
            if (packs > 0) OpenPacks(BoosterPack.Latest, packs);
            RevealCards("세븐 패스 카드 보상", SevenPass.TakeGranted());   // 팩이 있으면 팩 개봉 뒤에 이어서
        }

        // ══════════════════════════ 출석체크 ══════════════════════════

        RectTransform BuildAttendPanel(RectTransform page)
        {
            var f = UIKit.Frame(page, "Attendance", Dark);
            f.rectTransform.Anchor(0, 0, 1, 1, 0, 0, 0, 72);
            var p = f.transform;
            var title = UIKit.Txt(p, "Title", "출석체크", 40, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 30, -64, 30, 20);
            title.Section(UIKit.SecMe, 26);
            attendInfo = UIKit.Txt(p, "Info", "", 20, UIKit.Ink, TextAnchor.MiddleLeft);
            attendInfo.rectTransform.Anchor(0, 1, 1, 1, 32, -104, 30, 70);
            var row = UIKit.Rect("Row", p).Anchor(0.03f, 0.3f, 0.97f, 0.78f);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true;
            attendCells.Clear();
            for (int i = 0; i < 7; i++)
            {
                var bg = UIKit.Img(row, "D" + (i + 1), new Color(0.07f, 0.06f, 0.12f, 0.95f), GameAssets.Rounded, true);
                var glow = UIKit.Img(bg.transform, "Glow", Color.white, GameAssets.Glow);
                glow.rectTransform.Anchor(0.05f, 0.36f, 0.95f, 0.98f);
                glow.raycastTarget = false;
                var r = Attendance.Rewards[i];
                var icon = UIKit.Img(bg.transform, "Icon", Color.white, AttendIcon(r.kind));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.Anchor(0.14f, 0.42f, 0.86f, 0.92f);
                var t = UIKit.Txt(bg.transform, "T", $"<b>{i + 1}일째</b>\n{SevenPass.Label(r)}", 20, UIKit.Ink, TextAnchor.LowerCenter);
                t.rectTransform.Anchor(0, 0, 1, 0.4f, 6, 14, 6, 0);
                var mark = UIKit.Txt(bg.transform, "Mark", "✔", 64, new Color(0.55f, 1f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
                mark.rectTransform.Anchor(0, 0.42f, 1, 0.92f);
                mark.Outline(new Color(0, 0, 0, 0.9f), 3);
                attendCells.Add((bg, t, glow, icon, mark));
            }
            attendBtn = UIKit.Button(p, "출석 보상 받기", ClaimAttend, Red, 30, Color.white);
            attendBtn.rt.Place(new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(420, 90));
            return f.rectTransform;
        }

        void RefreshAttend()
        {
            int lit = Attendance.ClaimedInCycle;
            int next = Attendance.NextDay;
            bool can = Attendance.CanClaim;
            for (int i = 0; i < attendCells.Count; i++)
            {
                var (bg, t, glow, icon, mark) = attendCells[i];
                bool done = i < lit;
                bool today = can && i == next - 1;
                bg.color = done ? new Color(0.16f, 0.13f, 0.04f, 0.96f) : today ? new Color(0.2f, 0.1f, 0.3f, 0.97f) : new Color(0.07f, 0.06f, 0.12f, 0.95f);
                glow.color = done ? new Color(1f, 0.85f, 0.35f, 0.35f) : today ? new Color(0.8f, 0.6f, 1f, 0.6f) : new Color(0.4f, 0.4f, 0.5f, 0.12f);
                icon.color = done ? new Color(1f, 1f, 1f, 0.45f) : Color.white;     // 받은 날은 흐리게 + ✔
                mark.gameObject.SetActive(done);
            }
            attendInfo.text = can ? $"오늘 {next}일째 보상을 받을 수 있습니다 · 빠진 날이 있어도 이어서 받아요 · 7일째는 랜덤 Rare 카드 · 받을 때마다 세븐 패스 XP +{SevenPass.XpAttend}"
                                  : $"오늘 출석 완료 — 내일 {next}일째 보상 · 초기화까지 {DailyClock.Countdown(DailyClock.UntilReset)}";
            attendBtn.Interactable = can;
            attendBtn.label.text = can ? "출석 보상 받기" : "내일 또 만나요";
        }

        void ClaimAttend()
        {
            int day = Attendance.NextDay;
            string got = Attendance.Claim(out int packs);
            if (got == null) return;
            Sfx.Play("reveal2");
            Toast(got);
            RefreshAttend();
            RefreshTop();
            if (packs > 0) OpenPacks(BoosterPack.Latest, packs);
            RevealCards($"출석체크 {day}일째 보상", SevenPass.TakeGranted());   // 7일째 Rare 카드: 뒤집어 공개
        }

        /// <summary>홈에 들어올 때 오늘 출석을 아직 안 받았으면 출석 탭을 띄운다 (하루 한 번)</summary>
        string attendShownDay = "";
        public void MaybeShowAttendance()
        {
            if (!Attendance.CanClaim || attendShownDay == DailyClock.Today || AutoTest.Enabled) return;
            attendShownDay = DailyClock.Today;
            Show(Tab.Quest);
            ShowQuestTab(2);
        }
    }
}
