using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>메인 허브 퀘스트 페이지: 일일 퀘스트 · 업적 (보상 = 오팔)</summary>
    public class QuestPage : MonoBehaviour
    {
        static readonly Color Dark = UIKit.Dark;
        static readonly Color RowC = new Color(0.08f, 0.06f, 0.13f, 0.9f);
        static readonly Color DoneC = new Color(0.14f, 0.11f, 0.04f, 0.92f);
        static readonly Color Claim = new Color(0.55f, 0.09f, 0.06f, 0.97f);

        public Action<string> toast;
        RectTransform page, dailyList, achList, leftFrame, rightFrame;
        Text resetText, achSummary;
        UIKit.Btn claimAll;
        float nextTimer;

        public RectTransform Build(RectTransform content)
        {
            page = UIKit.Rect("Quests", content).Fill();

            var left = UIKit.Frame(page, "Daily", Dark);
            left.rectTransform.Anchor(0, 0, 0.44f, 1, 0, 0, 12, 72);     // 위 72 = [퀘스트 · 세븐 패스 · 출석] 탭 자리
            leftFrame = left.rectTransform;
            Title(left.transform, "일일 퀘스트");
            resetText = UIKit.Txt(left.transform, "Reset", "", 20, UIKit.Muted, TextAnchor.MiddleRight);
            resetText.rectTransform.Place(new Vector2(1, 1), new Vector2(-26, -30), new Vector2(360, 40), new Vector2(1, 1));
            dailyList = UIKit.Rect("List", left.transform).Anchor(0, 0, 1, 1, 22, 110, 22, 86);
            var v = dailyList.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12; v.childControlHeight = false; v.childControlWidth = true; v.childForceExpandHeight = false;
            claimAll = UIKit.Button(left.transform, "모두 받기", () =>
            {
                int got = Quests.ClaimAll();
                if (got > 0) { Sfx.Play("reveal2"); toast?.Invoke($"오팔 +{got:N0} 획득!"); }
                Refresh();
            }, Claim, 26, Color.white);
            claimAll.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(300, 68), new Vector2(0.5f, 0));

            var right = UIKit.Frame(page, "Achievements", Dark);
            right.rectTransform.Anchor(0.44f, 0, 1, 1, 12, 0, 0, 72);
            rightFrame = right.rectTransform;
            Title(right.transform, "업적");
            achSummary = UIKit.Txt(right.transform, "Summary", "", 20, UIKit.Muted, TextAnchor.MiddleRight);
            achSummary.rectTransform.Place(new Vector2(1, 1), new Vector2(-26, -30), new Vector2(400, 40), new Vector2(1, 1));
            var scrollHolder = UIKit.Rect("Scroll", right.transform).Anchor(0, 0, 1, 1, 14, 16, 14, 86);
            achList = UIKit.ScrollGrid(scrollHolder, "AchGrid", new Vector2(420, 112), new Vector2(12, 12), out _);
            ((RectTransform)achList.parent).Fill();
            return page;
        }

        /// <summary>퀘스트 · 업적 화면 보이기 (다른 탭일 때 숨김)</summary>
        public void SetBodyVisible(bool on)
        {
            if (leftFrame != null) leftFrame.gameObject.SetActive(on);
            if (rightFrame != null) rightFrame.gameObject.SetActive(on);
        }

        static void Title(Transform parent, string text)
        {
            var t = UIKit.Txt(parent, "Title", text, 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(28, -22), new Vector2(400, 44), new Vector2(0, 1));
            t.Section(UIKit.SecMe);
        }

        void Update()
        {
            if (page == null || !page.gameObject.activeInHierarchy || Time.unscaledTime < nextTimer) return;
            nextTimer = Time.unscaledTime + 1f;
            if (Quests.EnsureDaily()) { Refresh(); return; }
            var r = Quests.UntilReset;
            resetText.text = "초기화까지 " + DailyClock.Countdown(r);
        }

        public void Refresh()
        {
            if (page == null) return;
            UIKit.Clear(dailyList);
            foreach (var (def, e) in Quests.Daily.ToList())
            {
                int prog = Quests.DailyProgress(def, e), goal = Quests.Goal(def);
                var entry = e;
                Row(dailyList, def, prog, goal, e.claimed, 104, () => { if (Quests.ClaimDaily(entry)) Claimed(def); });
            }
            UIKit.Clear(achList);
            int done = 0;
            // 받을 수 있는 것 → 진행 중 → 완료 순
            var ordered = Quests.Achievements
                .Select(a => (a, prog: Quests.AchProgress(a), goal: Quests.Goal(a), claimed: Quests.AchClaimed(a)))
                .OrderBy(x => x.claimed ? 2 : x.prog >= x.goal ? 0 : 1).ThenByDescending(x => (float)x.prog / x.goal).ToList();
            foreach (var x in ordered)
            {
                if (x.claimed) done++;
                var def = x.a;
                Row(achList, def, x.prog, x.goal, x.claimed, 112, () => { if (Quests.ClaimAch(def)) Claimed(def); });
            }
            achSummary.text = $"달성 {done} / {Quests.Achievements.Count}";
            claimAll.Interactable = Quests.ClaimableCount > 0;
            var r = Quests.UntilReset;
            resetText.text = "초기화까지 " + DailyClock.Countdown(r);
        }

        void Claimed(QuestDef def)
        {
            Sfx.Play("reveal1");
            toast?.Invoke($"[{def.title}] 보상  오팔 +{def.reward:N0}");
            Refresh();
        }

        void Row(RectTransform parent, QuestDef def, int prog, int goal, bool claimed, float h, Action onClaim)
        {
            bool complete = prog >= goal;
            var bg = UIKit.Img(parent, "Row_" + def.id, claimed ? DoneC : RowC, GameAssets.Rounded, true);
            bg.rectTransform.sizeDelta = new Vector2(0, h);
            if (complete && !claimed)
            {
                var border = UIKit.Img(bg.transform, "Ready", UIKit.Gold, GameAssets.RoundedOutline, true);
                border.rectTransform.Fill();
            }
            var t = UIKit.Txt(bg.transform, "Title", def.title, 24, claimed ? UIKit.Muted : UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Anchor(0, 0.52f, 1, 1, 18, 0, 170, 6);
            // 진행 막대
            var barBg = UIKit.Img(bg.transform, "Bar", new Color(0.02f, 0.02f, 0.05f, 1f), GameAssets.Rounded, true);
            barBg.rectTransform.Anchor(0, 0.2f, 1, 0.42f, 18, 0, 262, 0);
            var fill = UIKit.Img(barBg.transform, "Fill", complete ? UIKit.Gold : new Color(0.35f, 0.7f, 1f), GameAssets.Rounded, true);
            fill.rectTransform.Anchor(0, 0, Mathf.Clamp01((float)prog / goal), 1, 2, 2, 2, 2);
            var pt = UIKit.Txt(bg.transform, "Prog", $"{prog:N0} / {goal:N0}", 18, UIKit.Muted, TextAnchor.MiddleLeft);
            pt.rectTransform.Anchor(1, 0.1f, 1, 0.52f, -256, 0, 160, 0);
            // 보상
            var icon = UIKit.Img(bg.transform, "Opal", Color.white, GameAssets.TexSprite("opal"));
            icon.preserveAspect = true;
            icon.rectTransform.Place(new Vector2(1, 0.72f), new Vector2(-126, 0), new Vector2(34, 34));
            var rw = UIKit.Txt(bg.transform, "Reward", def.reward.ToString("N0"), 22, new Color(0.75f, 0.95f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            rw.rectTransform.Place(new Vector2(1, 0.72f), new Vector2(-104, 0), new Vector2(96, 34), new Vector2(0, 0.5f));
            var b = UIKit.Button(bg.transform, claimed ? "완료" : complete ? "받기" : "진행 중", () => onClaim(),
                complete && !claimed ? Claim : new Color(0.1f, 0.1f, 0.14f, 0.9f), 20, complete && !claimed ? Color.white : UIKit.Muted);
            b.rt.Place(new Vector2(1, 0.28f), new Vector2(-14, 0), new Vector2(140, 42), new Vector2(1, 0.5f));
            b.Interactable = complete && !claimed;
            if (claimed) b.label.text = "완료";
        }
    }
}
