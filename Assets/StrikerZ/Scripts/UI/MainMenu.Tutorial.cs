using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 튜토리얼 (홈 › 대전 설정의 [튜토리얼] 칸, 수련 옆): 안내 창 → ① 덱 구성 배우기 ② 대전 배우기.
    /// 덱 구성은 "강제 진행": 화면을 막고 눌러야 하는 것 하나만 깜빡이게 보여 주며(TutorialGuide), 그걸 실제로 해야 다음 단계로 간다.
    /// Esc / [나가기] 로만 빠져나갈 수 있다. 대전 배우기는 TutorialMatch (GameRoot.StartTutorial).
    /// </summary>
    public partial class MainMenu
    {
        /// <summary>대전 배우기: 수련 CPU 와 튜토리얼 대전 시작 (GameRoot.StartTutorial)</summary>
        public Action onTutorialDuel;

        RectTransform tutRoot;
        Text tutDeckState, tutDuelState;
        TutorialGuide deckGuide;
        Coroutine deckGuideRun;

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
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
            panel.raycastTarget = true;
            var title = UIKit.Txt(panel.transform, "Title", "튜토리얼", 34, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 34, -74, 160, 22);
            title.Section(UIKit.SecMe, 34);
            var sub = UIKit.Txt(panel.transform, "Sub", "화면이 알려 주는 깜빡이는 곳만 누르면 됩니다. 그만두려면 언제든 Esc 또는 [나가기].", 19, UIKit.Ink, TextAnchor.MiddleLeft);
            sub.rectTransform.Anchor(0, 1, 1, 1, 36, -112, 30, 80);
            UIKit.Button(panel.transform, "닫기", CloseTutorial, null, 20).rt
                .Place(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(120, 46), new Vector2(1, 1));

            tutDeckState = BuildTutorialCard(panel.transform, 0, "1", "덱 구성 배우기",
                "덱 화면에서 새 덱 만들기 → 영웅 바꾸기 → 카드 넣고 빼기 → 자동 구성 → 사용 덱 지정까지,\n눌러야 할 곳을 하나씩 짚어 주며 직접 해 봅니다.",
                "덱 구성 배우기 ▶", new Color(0.08f, 0.16f, 0.3f, 0.97f), StartDeckGuide);
            tutDuelState = BuildTutorialCard(panel.transform, 1, "2", "대전 배우기",
                "수련 CPU 와 실제로 한 판 — 운명 재배치 · 원소 · 배치 · 배치 완료 · 배틀 · 주사위 · 필드 · 마법 · 영웅 능력 · 원소 더미까지\n순서대로 눌러 보며 익힙니다. 기록·보상 없음.",
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
            if (tutDeckState != null) tutDeckState.text = Profile.Data.tutorialDeckDone ? "✓ 완료 — 다시 할 수 있어요" : "";
            if (tutDuelState != null) tutDuelState.text = Profile.Data.tutorialDuelDone ? "✓ 완료 — 다시 할 수 있어요" : "";
        }

        // ══════════════════════════ 덱 구성 강제 튜토리얼 ══════════════════════════

        void StartDeckGuide()
        {
            CloseTutorial();
            Show(Tab.Deck);
            if (deckGuide == null) deckGuide = TutorialGuide.Create(root, CloseDeckGuide);
            if (deckGuideRun != null) StopCoroutine(deckGuideRun);
            deckGuideRun = StartCoroutine(DeckGuideRoutine());
        }

        public void CloseDeckGuide()
        {
            if (deckGuideRun != null) { StopCoroutine(deckGuideRun); deckGuideRun = null; }
            if (deckGuide != null) deckGuide.Hide();
        }

        /// <summary>[다음 ▶] 을 누를 때까지 전체를 막고 설명</summary>
        IEnumerator Tell(string title, string body, string step, RectTransform focus = null)
        {
            bool next = false;
            deckGuide.Show(title, body, focus, null, () => next = true, "다음 ▶", true, step);
            while (!next) yield return null;
        }

        /// <summary>대상 하나만 열어 두고, done 이 참이 될 때까지 기다린다 (대상이 없으면 바로 넘어간다)</summary>
        IEnumerator Do(string title, string body, string step, RectTransform target, Func<bool> done, float timeout = 0)
        {
            if (target == null) yield break;
            deckGuide.Show(title, body, target, null, null, "", true, step);
            float t0 = Time.unscaledTime;
            while (!done() && (timeout <= 0 || Time.unscaledTime - t0 < timeout)) yield return null;
        }

        IEnumerator DeckGuideRoutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            const int N = 10;
            yield return Tell("덱 화면", "왼쪽은 <b>카드 상세</b>, 가운데는 <b>지금 편집 중인 덱</b>, 오른쪽은 <b>보유 카드 리스트</b>입니다.\n이제부터 깜빡이는 곳을 눌러 가며 덱을 하나 만들어 봅니다. 그 외의 곳은 눌리지 않아요.", $"1 / {N}");

            // 1) 새 덱
            int before = Profile.Data.decks.Count;
            if (before < MaxDecks)
            {
                yield return Do("새 덱 만들기", "덱은 최대 8개. 깜빡이는 <b>[+ 새 덱]</b> 을 눌러 새 덱을 만드세요.", $"2 / {N}", deckNewBtn?.rt, () => Profile.Data.decks.Count > before);
                yield return null;
            }
            else yield return Tell("새 덱 만들기", "덱이 이미 8개(최대)라 지금 덱으로 이어서 해 볼게요. 새 덱은 <b>[+ 새 덱]</b> 으로 만듭니다.", $"2 / {N}", deckNewBtn?.rt);

            // 2) 영웅 교체
            string heroBefore = editing.heroId;
            var heroCell = CollectionCell(c => c.type == CardType.Hero && c.id != heroBefore);
            yield return Do("영웅 (리더) 바꾸기", "덱에는 <b>영웅 1장</b>. 영웅의 속성이 영웅 아래에 쌓을 수 있는 원소 수를 정합니다 (같은 속성 10 · 이기는 속성 5 · 상관없는 3 · 약점 1).\n깜빡이는 영웅 카드를 <b>더블클릭</b>해 영웅을 바꿔 보세요.", $"3 / {N}", heroCell, () => editing.heroId != heroBefore);
            yield return null;

            // 3) 카드 넣기
            int cnt = editing.cards.Count;
            var addCell = CollectionCell(c => c.type != CardType.Hero && c.type != CardType.Field && DeckRules.CanAdd(editing, c, out _));
            string addedId = addCell != null ? CellCardId(addCell) : null;
            yield return Do("카드 넣기", "오른쪽 리스트의 카드를 <b>더블클릭</b>하면 덱에 들어갑니다. 한 번 클릭하면 왼쪽에 상세가 보여요.\n깜빡이는 카드를 더블클릭해 넣어 보세요.", $"4 / {N}", addCell, () => editing.cards.Count > cnt);
            yield return null;

            // 4) 카드 빼기
            cnt = editing.cards.Count;
            var deckCell = addedId != null ? DeckCell(addedId) : DeckCell(null);
            yield return Do("카드 빼기", "덱 안의 카드를 <b>더블클릭</b>하면 빠집니다. 같은 카드는 커먼·언커먼 3장, 레어 2장, 유니크 1장까지.\n방금 넣은 카드를 더블클릭해 빼 보세요.", $"5 / {N}", deckCell, () => editing.cards.Count < cnt);
            yield return null;

            // 5) 자동 구성
            int cntAuto = editing.cards.Count;
            yield return Do("자동 구성", "메인 덱은 <b>정확히 40장</b> (추천: 소환수 28 · 마법 12). 하나하나 넣어도 되지만, <b>[자동 구성]</b> 이 보유 카드로 영웅 속성에 맞춰 40장을 짜 줍니다.\n깜빡이는 [자동 구성] 을 누르세요.", $"6 / {N}", deckAutoBtn?.rt, () => editing.cards.Count != cntAuto);
            yield return null;

            // 6) 엑스트라 덱 · 분석 설명
            yield return Tell("엑스트라 덱 (필드)", "<b>필드 카드</b>는 메인 덱이 아니라 <b>엑스트라 덱</b>에 들어갑니다 (최대 10장, 같은 필드 1장). 자동 구성이 채워 두었어요.\n대전 중 내 필드 차례(짝수 또는 홀수 T)에 펼쳐 전장 효과를 바꿉니다.", $"7 / {N}", extraRow);
            yield return Tell("원소 비용과 덱 분석", "카드마다 <b>원소 비용</b>이 있고, 매 T 원소 1장을 영웅 아래에 쌓아 같은 속성 카드의 비용으로 씁니다.\n그래서 덱은 영웅 속성 위주로, 약점 속성은 피하는 게 좋아요. 여기서 속성 비율과 유니크 수를 확인하세요.", $"8 / {N}", deckStats != null ? deckStats.rectTransform : null);

            // 7) 사용 덱으로
            if (Profile.Data.activeDeck != editIndex)
                yield return Do("사용 덱으로", "대전에 나가는 덱은 <b>[사용 덱으로]</b> 를 누른 덱입니다. 머리말이 '사용 가능'이어야 해요.\n깜빡이는 [사용 덱으로] 를 누르세요.", $"9 / {N}", deckUseBtn?.rt, () => Profile.Data.activeDeck == editIndex);
            else yield return Tell("사용 덱으로", "이 덱은 이미 <b>사용 덱</b>입니다. 다른 덱으로 나가고 싶을 땐 그 덱에서 <b>[사용 덱으로]</b> 를 누르면 돼요.", $"9 / {N}", deckUseBtn?.rt);

            Profile.Data.tutorialDeckDone = true;
            Profile.Save();
            bool go = false;
            deckGuide.Show("덱 준비 완료", "덱 구성은 여기까지! 그 밖의 도구: <b>[스타터 덱]</b> 기본 덱 받기 · <b>[훈련장]</b> 수련 골렘과 기록 없이 연습 · <b>[덱 코드]</b> 복사/입력.\n이제 실제 대전 순서를 배워 봅시다.", null, null, () => go = true, "대전 배우기 ▶", true, $"{N} / {N}");
            while (!go) yield return null;
            deckGuideRun = null;
            deckGuide.Hide();
            onTutorialDuel?.Invoke();
        }

        // ── 테스트 ──
        public void TestOpenTutorial() => OpenTutorial();
        public void TestDeckGuide() => StartDeckGuide();
        public bool TestGuideOpen => deckGuide != null && deckGuide.Visible;
        public TutorialGuide TestGuide => deckGuide;
        public DeckData TestEditingDeck => editing;
    }
}
