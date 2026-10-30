using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 튜토리얼: 홈 › 대전 설정의 [튜토리얼] 칸(수련 옆) → 안내 창 → ① 덱 구성 배우기 (덱 화면 위에 단계별 설명 + 해당 부분 테두리)
    /// ② 대전 배우기 (수련 CPU 와 튜토리얼 대전, 코치 팁은 GameManager.StoryCoach). 처음 홈에 들어오면 안내 창이 한 번 열린다.
    /// </summary>
    public partial class MainMenu
    {
        /// <summary>대전 배우기: 수련 CPU 와 튜토리얼 대전 시작 (GameRoot.StartTutorial)</summary>
        public Action onTutorialDuel;

        RectTransform tutRoot, tutPanel, guideRoot, guideBox, guideHi;
        Text tutDeckState, tutDuelState, guideTitle, guideText, guideStepText;
        UIKit.Btn guidePrev, guideNext;
        int guideStep = -1;
        Coroutine guidePlace;

        // ── 안내 창 ──

        /// <summary>처음 홈에 들어오면 튜토리얼 안내 창을 한 번 띄운다</summary>
        public void MaybeShowTutorial()
        {
            if (Profile.Data.tutorialSeen || AutoTest.Enabled) return;
            Profile.Data.tutorialSeen = true;
            Profile.Save();
            OpenTutorial();
        }

        public void OpenTutorial()
        {
            if (tutRoot == null) BuildTutorial();
            CloseDeckGuide();
            tutRoot.gameObject.SetActive(true);
            tutRoot.SetAsLastSibling();
            RefreshTutorial();
        }

        void CloseTutorial() { if (tutRoot != null) tutRoot.gameObject.SetActive(false); }

        void BuildTutorial()
        {
            var dim = UIKit.Img(root, "Tutorial", new Color(0, 0, 0, 0.62f));
            tutRoot = dim.rectTransform;
            tutRoot.Fill();
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseTutorial);

            var panel = UIKit.Frame(tutRoot, "Panel", new Color(0.04f, 0.03f, 0.08f, 0.97f));
            tutPanel = panel.rectTransform;
            tutPanel.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
            panel.raycastTarget = true;
            var title = UIKit.Txt(panel.transform, "Title", "튜토리얼", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 34, -74, 160, 22);
            title.Section(UIKit.SecMe, 34);
            var sub = UIKit.Txt(panel.transform, "Sub", "처음이라면 두 가지만 익히면 됩니다. 순서대로 해도, 하나만 골라도 좋아요.", 19, UIKit.Ink, TextAnchor.MiddleLeft);
            sub.rectTransform.Anchor(0, 1, 1, 1, 36, -112, 30, 80);
            UIKit.Button(panel.transform, "닫기", CloseTutorial, null, 20).rt
                .Place(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(120, 46), new Vector2(1, 1));

            tutDeckState = BuildTutorialCard(panel.transform, 0, "1", "덱 구성 배우기",
                "덱 화면에서 영웅 · 메인 덱 40장 · 엑스트라 덱(필드) · 원소 비용을 단계별로 짚어 줍니다.\n설명을 보면서 그대로 덱을 만져 볼 수 있어요.",
                "덱 구성 배우기 ▶", new Color(0.08f, 0.16f, 0.3f, 0.97f), StartDeckGuide);
            tutDuelState = BuildTutorialCard(panel.transform, 1, "2", "대전 배우기",
                "수련 CPU 와 실제로 한 판 — 운명 재배치 → 원소 → 배치 → 배틀 → 엔드 순서를 코치 팁이 차례로 안내합니다.\n기록·보상 없음, 배치 시간 제한 없음.",
                "대전 배우기 ▶", new Color(0.45f, 0.08f, 0.05f, 0.97f), () => { CloseTutorial(); onTutorialDuel?.Invoke(); });
            tutRoot.gameObject.SetActive(false);
        }

        Text BuildTutorialCard(Transform parent, int col, string num, string head, string desc, string btnLabel, Color btnColor, Action onClick)
        {
            var card = UIKit.Frame(parent, "Card" + num, new Color(0.08f, 0.06f, 0.14f, 0.96f));
            card.rectTransform.Anchor(col == 0 ? 0.04f : 0.52f, 0.06f, col == 0 ? 0.48f : 0.96f, 0.74f);
            var badge = UIKit.Img(card.transform, "Num", UIKit.Gold, GameAssets.Rounded, true);
            badge.rectTransform.Place(new Vector2(0, 1), new Vector2(18, -18), new Vector2(44, 44), new Vector2(0, 1));
            UIKit.Txt(badge.transform, "T", num, 26, new Color(0.1f, 0.06f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            var h = UIKit.Txt(card.transform, "Head", head, 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            h.rectTransform.Anchor(0, 1, 1, 1, 74, -62, 16, 16);
            var d = UIKit.Txt(card.transform, "Desc", desc, 18, UIKit.Ink, TextAnchor.UpperLeft);
            d.rectTransform.Anchor(0, 0, 1, 1, 20, 112, 18, 78);
            d.lineSpacing = 1.15f;
            var state = UIKit.Txt(card.transform, "State", "", 16, new Color(0.55f, 0.95f, 0.65f), TextAnchor.MiddleLeft, FontStyle.Bold);
            state.rectTransform.Anchor(0, 0, 1, 0, 22, 84, 18, -108);
            var b = UIKit.Button(card.transform, btnLabel, onClick, btnColor, 24, Color.white);
            b.rt.Anchor(0, 0, 1, 0, 20, 18, 20, -74);
            return state;
        }

        void RefreshTutorial()
        {
            if (tutDeckState != null) tutDeckState.text = Profile.Data.tutorialDeckDone ? "✓ 완료 — 다시 볼 수 있어요" : "";
            if (tutDuelState != null) tutDuelState.text = Profile.Data.tutorialDuelDone ? "✓ 완료 — 다시 할 수 있어요" : "";
        }

        // ── 덱 구성 가이드 (덱 화면 위 단계별 상자 + 테두리) ──

        void StartDeckGuide()
        {
            CloseTutorial();
            Show(Tab.Deck);
            if (guideRoot == null) BuildDeckGuide();
            guideRoot.gameObject.SetActive(true);
            guideRoot.SetAsLastSibling();
            SetGuideStep(0);
        }

        public void CloseDeckGuide()
        {
            if (guideRoot == null) return;
            if (guidePlace != null) { StopCoroutine(guidePlace); guidePlace = null; }
            guideRoot.gameObject.SetActive(false);
            guideStep = -1;
        }

        void BuildDeckGuide()
        {
            guideRoot = UIKit.Rect("DeckGuide", root);
            guideRoot.Fill();
            // 짚어 주는 테두리 (금색, 설명 상자 뒤)
            var hi = UIKit.Img(guideRoot, "Hi", new Color(1f, 0.82f, 0.35f, 0.95f), GameAssets.RoundedOutline, true);
            guideHi = hi.rectTransform;
            guideHi.anchorMin = guideHi.anchorMax = new Vector2(0.5f, 0.5f);
            hi.raycastTarget = false;
            var glow = UIKit.Img(guideHi, "Glow", new Color(1f, 0.82f, 0.35f, 0.12f), GameAssets.Rounded, true);
            glow.rectTransform.Fill();
            glow.raycastTarget = false;

            var box = UIKit.Frame(guideRoot, "Box", new Color(0.04f, 0.03f, 0.08f, 0.97f));
            guideBox = box.rectTransform;
            guideBox.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(760, 230), new Vector2(0.5f, 0));
            box.raycastTarget = true;
            box.transform.Find("Border").GetComponent<Image>().color = new Color(1f, 0.82f, 0.35f, 0.6f);
            var tag = UIKit.Img(box.transform, "Tag", UIKit.Gold, GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(18, -16), new Vector2(92, 30), new Vector2(0, 1));
            UIKit.Txt(tag.transform, "T", "튜토리얼", 16, new Color(0.1f, 0.06f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            guideTitle = UIKit.Txt(box.transform, "Title", "", 25, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            guideTitle.rectTransform.Anchor(0, 1, 1, 1, 122, -50, 120, 12);
            guideStepText = UIKit.Txt(box.transform, "Step", "", 16, UIKit.Muted, TextAnchor.MiddleRight);
            guideStepText.rectTransform.Anchor(0.5f, 1, 1, 1, 0, -50, 118, 12);
            guideText = UIKit.Txt(box.transform, "Text", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            guideText.rectTransform.Anchor(0, 0, 1, 1, 20, 66, 20, 56);
            guideText.lineSpacing = 1.15f;
            UIKit.Button(box.transform, "✕", CloseDeckGuide, null, 18, UIKit.Muted).rt
                .Place(new Vector2(1, 1), new Vector2(-12, -10), new Vector2(44, 36), new Vector2(1, 1));
            guidePrev = UIKit.Button(box.transform, "◀ 이전", () => SetGuideStep(guideStep - 1), null, 19);
            guidePrev.rt.Place(new Vector2(0, 0), new Vector2(18, 14), new Vector2(130, 44), new Vector2(0, 0));
            guideNext = UIKit.Button(box.transform, "다음 ▶", () => SetGuideStep(guideStep + 1), new Color(0.45f, 0.08f, 0.05f, 0.97f), 20, Color.white);
            guideNext.rt.Place(new Vector2(1, 0), new Vector2(-18, 14), new Vector2(200, 44), new Vector2(1, 0));
        }

        void SetGuideStep(int i)
        {
            var steps = TutorialData.DeckSteps;
            if (i < 0) i = 0;
            if (i >= steps.Count)
            {
                // 마지막 [대전 배우기 ▶]
                Profile.Data.tutorialDeckDone = true;
                Profile.Save();
                CloseDeckGuide();
                onTutorialDuel?.Invoke();
                return;
            }
            guideStep = i;
            var s = steps[i];
            guideTitle.text = s.title;
            guideText.text = s.text;
            guideStepText.text = $"{i + 1} / {steps.Count}";
            guidePrev.Interactable = i > 0;
            bool last = i == steps.Count - 1;
            guideNext.label.text = last ? "대전 배우기 ▶" : "다음 ▶";
            if (last && !Profile.Data.tutorialDeckDone) { Profile.Data.tutorialDeckDone = true; Profile.Save(); }
            if (guidePlace != null) StopCoroutine(guidePlace);
            guidePlace = StartCoroutine(PlaceGuide(s.target));
        }

        /// <summary>레이아웃이 끝난 다음 프레임에 대상 영역 위로 테두리를 맞추고, 설명 상자는 대상과 겹치지 않는 쪽(위/아래)에 둔다</summary>
        IEnumerator PlaceGuide(string key)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var target = DeckRegion(key);
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                guideHi.gameObject.SetActive(false);
                guideBox.anchorMin = guideBox.anchorMax = new Vector2(0.5f, 0.5f);
                guideBox.pivot = new Vector2(0.5f, 0.5f);
                guideBox.anchoredPosition = Vector2.zero;
                yield break;
            }
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = root.InverseTransformPoint(corners[0]), max = root.InverseTransformPoint(corners[2]);
            var center = (min + max) * 0.5f;
            guideHi.gameObject.SetActive(true);
            guideHi.pivot = new Vector2(0.5f, 0.5f);
            guideHi.anchoredPosition = center;
            guideHi.sizeDelta = (max - min) + new Vector2(16, 16);
            // 대상이 화면 아래쪽이면 상자를 위에, 위쪽이면 아래에
            bool boxTop = center.y < 0;
            guideBox.anchorMin = guideBox.anchorMax = new Vector2(0.5f, boxTop ? 1 : 0);
            guideBox.pivot = new Vector2(0.5f, boxTop ? 1 : 0);
            guideBox.anchoredPosition = new Vector2(0, boxTop ? -120 : 40);
        }

        /// <summary>덱 화면의 영역 (튜토리얼이 짚어 주는 곳)</summary>
        RectTransform DeckRegion(string key)
        {
            switch (key)
            {
                case "tabs": return deckTabs != null ? (RectTransform)deckTabs.parent : null;
                case "hero": return heroSlot;
                case "collection": return deckCollectionRt;
                case "deck": return deckGrid != null ? (RectTransform)deckGrid.parent : null;
                case "extra": return extraRow;
                case "stats": return deckStats != null ? deckStats.rectTransform : null;
                case "tools": return deckToolsRt;
                case "detail": return deckDetailRt;
                default: return null;
            }
        }

        // ── 테스트 ──
        public void TestOpenTutorial() => OpenTutorial();
        public void TestDeckGuide(int step) { StartDeckGuide(); SetGuideStep(step); }
        public bool TestGuideOpen => guideRoot != null && guideRoot.gameObject.activeSelf;
    }
}
