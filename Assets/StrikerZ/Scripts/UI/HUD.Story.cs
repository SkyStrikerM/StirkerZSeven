using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>스토리 전투 HUD: 목표·규칙 띠 · 튜토리얼 코치 상자 · 동료 지원 알림</summary>
    public partial class HUD
    {
        RectTransform storyChip, coachRoot, storyToastRt;
        Text storyChipText, coachTitle, coachText, storyToastText;
        CanvasGroup coachGroup, storyToastGroup;
        float coachUntil, storyToastUntil;

        void BuildStory()
        {
            var chip = UIKit.Img(root, "StoryChip", new Color(0.03f, 0.025f, 0.06f, 0.82f), GameAssets.Rounded, true);
            storyChip = chip.rectTransform;
            storyChip.Place(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(760, 56), new Vector2(0.5f, 1));
            storyChipText = UIKit.Txt(chip.transform, "T", "", 18, UIKit.Ink, TextAnchor.MiddleCenter);
            storyChipText.rectTransform.Anchor(0, 0, 1, 1, 14, 2, 14, 2);
            storyChipText.verticalOverflow = VerticalWrapMode.Overflow;
            storyChip.gameObject.SetActive(false);

            // 코치 상자 (왼쪽 가운데, 클릭하면 닫힘)
            var bg = UIKit.Frame(root, "Coach", new Color(0.05f, 0.04f, 0.1f, 0.95f));
            coachRoot = bg.rectTransform;
            coachRoot.Place(new Vector2(1, 1), new Vector2(-24, -222), new Vector2(470, 214), new Vector2(1, 1));   // 오른쪽 위 (필드 정보 아래 · 배틀 버튼 위) — 영웅 카드를 가리지 않게
            coachGroup = bg.gameObject.AddComponent<CanvasGroup>();
            var icon = UIKit.Txt(bg.transform, "Icon", "TIP", 18, new Color(0.1f, 0.06f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var iconBg = UIKit.Img(bg.transform, "IconBg", UIKit.Gold, GameAssets.Rounded, true);
            iconBg.rectTransform.Place(new Vector2(0, 1), new Vector2(16, -14), new Vector2(54, 28), new Vector2(0, 1));
            icon.transform.SetParent(iconBg.transform, false);
            icon.rectTransform.Fill();
            coachTitle = UIKit.Txt(bg.transform, "Title", "", 24, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            coachTitle.rectTransform.Anchor(0, 1, 1, 1, 80, -44, 14, 10);
            coachText = UIKit.Txt(bg.transform, "Text", "", 19, UIKit.Ink, TextAnchor.UpperLeft);
            coachText.rectTransform.Anchor(0, 0, 1, 1, 18, 30, 16, 52);
            var close = UIKit.Txt(bg.transform, "Close", "클릭하여 닫기", 14, UIKit.Muted, TextAnchor.LowerRight);
            close.rectTransform.Anchor(0, 0, 1, 0, 0, 6, 14, -26);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => coachUntil = 0);
            coachRoot.gameObject.SetActive(false);

            // 동료 지원 알림 (화면 왼쪽 아래, 내 패널 위)
            var t = UIKit.Img(root, "StoryToast", new Color(0.03f, 0.04f, 0.08f, 0.88f), GameAssets.Rounded, true);
            storyToastRt = t.rectTransform;
            storyToastRt.Place(new Vector2(0, 0), new Vector2(18, 182), new Vector2(470, 50), new Vector2(0, 0));
            storyToastText = UIKit.Txt(t.transform, "T", "", 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            storyToastText.rectTransform.Anchor(0, 0, 1, 1, 16, 0, 10, 0);
            storyToastGroup = t.gameObject.AddComponent<CanvasGroup>();
            storyToastGroup.blocksRaycasts = false;
            storyToastRt.gameObject.SetActive(false);
        }

        public void BeginStory(GameManager gm)
        {
            var b = gm.story.battle;
            var rules = new List<string>();
            if (b.mist > 0) rules.Add($"<color=#bbb>회색 안개</color>: 속성 몬스터 ATK -{b.mist}");
            if (b.ariaHeal > 0) rules.Add($"<color=#7cf>아리아</color> 내 타임 HP +{b.ariaHeal}");
            if (b.leonFire > 0) rules.Add($"<color=#f86>레온</color> 내 타임 상대 -{b.leonFire}");
            if (b.allowLose) rules.Add("<color=#fd8>져도 이야기 진행</color>");
            storyChipText.text = $"<color=#ffd05c><b>STAGE {gm.story.number}</b></color>  {b.goal}" + (rules.Count > 0 ? "\n<size=15>" + string.Join("   ·   ", rules) + "</size>" : "");
            storyChip.sizeDelta = new Vector2(760, rules.Count > 0 ? 58 : 38);
            storyChip.gameObject.SetActive(true);
        }

        void EndStoryHud()
        {
            if (storyChip != null) storyChip.gameObject.SetActive(false);
            if (coachRoot != null) coachRoot.gameObject.SetActive(false);
            if (storyToastRt != null) storyToastRt.gameObject.SetActive(false);
        }

        public void ShowCoach(string title, string text)
        {
            coachTitle.text = title;
            coachText.text = text;
            coachRoot.gameObject.SetActive(true);
            coachRoot.SetAsLastSibling();
            coachGroup.alpha = 0;
            coachUntil = Time.unscaledTime + 14f;
            Sfx.Play("click", 0.6f);
        }

        public void StoryToast(string who, string msg)
        {
            storyToastText.text = $"<color=#ffd05c>{who}</color>  {msg}";
            storyToastRt.gameObject.SetActive(true);
            storyToastGroup.alpha = 1;
            storyToastUntil = Time.unscaledTime + 2.4f;
        }

        void UpdateStory()
        {
            if (coachRoot != null && coachRoot.gameObject.activeSelf)
            {
                bool on = Time.unscaledTime < coachUntil;
                coachGroup.alpha = Mathf.MoveTowards(coachGroup.alpha, on ? 1 : 0, Time.unscaledDeltaTime * 4f);
                if (!on && coachGroup.alpha <= 0) coachRoot.gameObject.SetActive(false);
            }
            if (storyToastRt != null && storyToastRt.gameObject.activeSelf && Time.unscaledTime > storyToastUntil)
            {
                storyToastGroup.alpha = Mathf.MoveTowards(storyToastGroup.alpha, 0, Time.unscaledDeltaTime * 2f);
                if (storyToastGroup.alpha <= 0) storyToastRt.gameObject.SetActive(false);
            }
        }

        // ── 테스트 ──
        public bool TestCoachVisible => coachRoot != null && coachRoot.gameObject.activeSelf;
    }
}
