using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 대전 강제 튜토리얼 (MatchMode.Tutorial): 수련 CPU 와 실제 대전을 하면서, 매 순간 눌러야 하는 것 하나만 깜빡이게 열어 두고
    /// (TutorialGuide) 그걸 해야 다음으로 간다. T1~T3 에 운명 재배치 · 원소 · 배치 드래그 · 배치 완료 · 배틀(자동) · 주사위 구간 ·
    /// 필드 전개 · 마법 세트 · 영웅 능력 · 원소 더미 보기를 차례로 익히고, 그 뒤는 자유 진행. Esc / [나가기] 로 홈으로.
    /// </summary>
    public class TutorialMatch : MonoBehaviour
    {
        GameManager g;
        HUD hud;
        TutorialGuide guide;
        Action onQuit;
        int step;

        public static TutorialMatch Current { get; private set; }
        public TutorialGuide Guide => guide;

        public static TutorialMatch Begin(GameManager g, HUD hud, Action onQuit)
        {
            var old = hud.GetComponentInChildren<TutorialMatch>();
            if (old != null) Destroy(old.gameObject);
            var go = new GameObject("TutorialMatch");
            go.transform.SetParent(hud.transform, false);
            var t = go.AddComponent<TutorialMatch>();
            t.g = g; t.hud = hud; t.onQuit = onQuit;
            t.guide = TutorialGuide.Create(hud.Root, () => { t.Stop(); onQuit?.Invoke(); });
            Current = t;
            t.StartCoroutine(t.Run());
            return t;
        }

        public void Stop()
        {
            StopAllCoroutines();
            if (guide != null) guide.Hide();
            foreach (var p in g.players) if (p != null) foreach (var c in p.hand) c.view?.SetGlow(null);
        }

        void OnDestroy() { if (guide != null) Destroy(guide.gameObject); }

        string Step() => $"{++step}단계";
        Camera Cam => g.board != null ? g.board.cam : Camera.main;
        bool Over => g == null || g.gameOver || g.mode != MatchMode.Tutorial;

        IEnumerator Tell(string title, string body)
        {
            bool next = false;
            guide.Show(title, body, null, null, () => next = true, "다음 ▶", true, Step());
            while (!next && !Over) yield return null;
        }

        /// <summary>until 까지 전체를 막고 지켜본다. 그동안 선택창(필드 선/후 · 주사위 구간 · 효과 대상 등)이 열리면 그 버튼을 짚어 준다</summary>
        IEnumerator Watch(string title, string body, Func<bool> until)
        {
            string st = Step();
            guide.Show(title, body, null, null, null, "", true, st);
            bool choice = false;
            while (!until() && !Over)
            {
                bool now = hud.ChoiceOpen && hud.FirstChoiceButton != null;
                if (now != choice)
                {
                    choice = now;
                    if (choice) guide.Show("선택하세요", ChoiceHint(), hud.FirstChoiceButton, null, null, "", true, st);
                    else guide.Show(title, body, null, null, null, "", true, st);
                }
                yield return null;
            }
        }

        /// <summary>지금 열린 선택창에 맞는 설명</summary>
        string ChoiceHint()
        {
            if (g.time == 0) return "코인 토스에서 앞면이 나오면 <b>필드 선/후</b>를 고릅니다. '선'이면 T2(짝수 T)에, '후'면 T3(홀수 T)에 내 필드를 펼칠 수 있어요.\n깜빡이는 버튼을 누르세요 (어느 쪽이든 좋습니다).";
            return "주사위(또는 코인)가 나오면 <b>나온 눈이 속한 구간</b>의 효과를 고릅니다. 1·2 / 3·4 / 5·6 구간은 값이 같은 다른 효과예요.\n깜빡이는 버튼을 누르세요 (효과 대상 선택창도 같은 방식).";
        }

        IEnumerator DoUi(string title, string body, RectTransform target, Func<bool> done)
        {
            if (target == null) yield break;
            guide.Show(title, body, target, null, null, "", true, Step());
            while (!done() && !Over) yield return null;
        }

        IEnumerator DoCard(string title, string body, CardInstance card, Func<bool> done, int slot = -1)
        {
            if (card == null) yield break;
            guide.Show(title, body, null, () => TutorialGuide.CardScreenRect(card, Cam), null, "", true, Step());
            guide.allowCard = c => c == card;
            guide.allowSlot = slot < 0 ? (Func<int, bool>)null : s => s == slot;
            card.view?.SetGlow(new Color(1f, 0.82f, 0.35f, 0.9f));
            while (!done() && !Over) yield return null;
            card.view?.SetGlow(null);
        }

        /// <summary>선택창(주사위 구간 등)이 열리면 눌러야 할 버튼을 짚어 주며, until 까지 지켜본다</summary>
        IEnumerator WatchBattle(Func<bool> until)
            => Watch("배틀 (자동 진행)", "양쪽 카드가 공개되고 <b>왼쪽 칸부터</b> 차례로 싸웁니다. 공격 턴(쌍검)엔 내 소환수가 공격, 수비 턴(방패)엔 반격만 — 턴은 T 마다 번갈아요.\n상대 칸이 비어 있으면 영웅이 직접 맞습니다. 배틀이 끝나면 엔드(패 7장 초과면 버리기) → 다음 T.", until);

        /// <summary>사람이 배치를 끝냈는가 ([배치 완료] 를 눌러 CPU 배치/배틀로 넘어감). 연출 중 busy 로 잠깐 IsHumanMain 이 꺼지는 것과 구분</summary>
        bool PlacementDone => g.phase != Phase.Main || g.active != g.Human;

        IEnumerator WaitMain(int time)
        {
            while (!(g.time >= time && g.IsHumanMain) && !Over) yield return null;
        }

        IEnumerator Run()
        {
            yield return null;
            // 0) 소개 (코인 토스 · 드로우 동안)
            yield return Tell("튜토리얼 대전", $"상대는 <b>수련 CPU</b>. 매 TIME 은 ① 원소 고르기 → ② 카드 배치(뒷면) → ③ 배틀 → ④ 엔드 순서예요.\n지금부터 깜빡이는 것만 누르면 됩니다. 그만두려면 Esc 또는 [나가기]. 먼저 코인 토스로 필드 선후공이 정해지고 7장을 뽑습니다.");

            // 1) 코인 토스 (앞면이면 필드 선/후 선택창) → 운명 재배치
            yield return Watch("코인 토스 · 드로우", "필드 코인 토스로 누가 먼저 필드를 펼칠지 정하고(앞면이면 내가 선/후를 고릅니다), 양쪽이 7장을 뽑습니다.\n잠시 지켜보세요.", () => hud.FateOpen);
            yield return DoUi("① 운명 재배치", "시작 패 7장 중 마음에 안 드는 카드를 눌러 고른 뒤 [교체] 하면 같은 장수를 새로 뽑습니다 (1회).\n이번엔 그대로 가 볼게요 — 깜빡이는 <b>[그대로 시작]</b> 을 누르세요.", hud.FateOkRt, () => !hud.FateOpen);

            // 2) T1 원소
            yield return ElementStep("② 원소 고르기", "T 시작마다 원소 1장을 골라 영웅 아래에 쌓습니다. 쌓인 원소는 매 T 다시 쓸 수 있고 <b>같은 속성 카드의 비용</b>이 돼요.\n영웅 속성 원소를 먼저 쌓는 게 기본 — 깜빡이는 원소를 누르세요.");

            // 3) T1 배치: 소환수 1장을 1번 칸에
            yield return WaitMain(1);
            var mon = Playable(c => c.IsMonster);
            if (mon != null)
            {
                int slot = FirstEmpty();
                yield return DoCard("③ 카드 배치 (소환수)", $"비용을 낼 수 있는 카드는 <color=#8f8>초록 테두리</color>. 깜빡이는 소환수를 <b>{slot + 1}번 칸으로 끌어다 놓으세요</b> (뒷면 배치).\n카드를 우클릭하면 빈 칸에 바로 놓을 수도 있어요. 왼쪽 칸부터 싸우니 튼튼한 소환수를 앞에!", mon, () => mon.zone == Zone.Slot, slot);
            }
            else if (Playable(c => c.IsMagic) is CardInstance mg1)
            {
                int slot = FirstEmpty();
                yield return DoCard("③ 카드 배치 (마법)", $"지금 낼 수 있는 소환수는 없고 마법이 있네요. 비용을 낼 수 있는 카드는 <color=#8f8>초록 테두리</color>. 깜빡이는 마법을 <b>{slot + 1}번 칸으로 끌어다 놓으세요</b> (뒷면 세트).\n세트한 마법은 배틀 때 그 칸 차례에 발동합니다.", mg1, () => mg1.zone == Zone.Slot, slot);
            }
            else yield return Tell("③ 카드 배치", "지금은 낼 수 있는 카드가 없네요. 패에서 비용(원소)이 되는 카드는 초록 테두리로 표시되고, 빈 칸으로 끌어다 놓으면 뒷면으로 배치됩니다.");
            while (g.busy && !Over) yield return null;
            yield return DoUi("배치 완료", "다 놓았으면 <b>[배치 완료]</b>. 양쪽이 동시에 뒷면으로 놓고, 배틀에서 한꺼번에 공개됩니다. 배치 시간은 보통 30초 (튜토리얼은 무제한).", hud.BattleBtnRt, () => PlacementDone);

            // 4) T1 배틀 지켜보기
            yield return WatchBattle(() => hud.FateOpen && g.time >= 2);

            // 5) T2 원소 · 필드 · 마법
            yield return ElementStep("T2 · 원소", "원소를 하나 더. 속성별 상한(영웅 속성 10 · 이기는 속성 5 · 보통 3 · 약점 1)까지 쌓을 수 있어요. 깜빡이는 원소를 누르세요.");
            yield return WaitMain(2);
            if (g.CanPlayAnyField(g.Human))
            {
                yield return DoUi("필드 전개", "내 필드 차례(짝수 또는 홀수 T — 코인 토스 결과)에는 엑스트라 덱의 필드를 펼쳐 전장 효과를 바꿉니다.\n깜빡이는 <b>[필드 전개]</b> 를 누르세요.", hud.FieldBtnRt, () => hud.ChoiceOpen || g.Human.fieldPlayedThisTime);
                yield return DoUi("필드 고르기", "엑스트라 덱의 필드 중 하나를 고릅니다. 깜빡이는 필드를 누르세요.", hud.FirstChoiceButton, () => !hud.ChoiceOpen);
                while (g.busy && !Over) yield return null;
            }
            else yield return Tell("필드 전개", "이번 T 는 상대의 필드 차례예요. 내 차례 T 에는 왼쪽 아래 <b>[필드 전개]</b> 로 엑스트라 덱의 필드를 펼칠 수 있습니다.");
            var mag = Playable(c => c.IsMagic && g.CanSet(c));
            if (mag != null)
            {
                int slot = FirstEmpty();
                yield return DoCard("마법 카드 세트", $"마법은 칸에 <b>세트</b>해 두면 배틀 때 그 칸 차례에 발동합니다. 깜빡이는 마법을 <b>{slot + 1}번 칸</b>으로 끌어다 놓으세요.\n't'·'half' 마법은 배틀 중 클릭해서 쓰는 즉발 마법이에요.", mag, () => mag.zone == Zone.Slot, slot);
            }
            else
            {
                var mon2 = Playable(c => c.IsMonster);
                if (mon2 != null)
                {
                    int slot = FirstEmpty();
                    yield return DoCard("소환수 하나 더", $"낼 수 있는 마법이 없으니 소환수를 하나 더. 깜빡이는 카드를 <b>{slot + 1}번 칸</b>으로 끌어다 놓으세요.\n(마법은 칸에 세트해 두면 배틀 때 그 칸 차례에 발동합니다)", mon2, () => mon2.zone == Zone.Slot, slot);
                }
            }
            while (g.busy && !Over) yield return null;
            yield return DoUi("배치 완료", "<b>[배치 완료]</b> 를 누르세요.", hud.BattleBtnRt, () => PlacementDone);

            // 6) T2 배틀
            yield return WatchBattle(() => hud.FateOpen && g.time >= 3);

            // 7) T3 원소 · 영웅 능력 · 원소 더미
            yield return ElementStep("T3 · 원소", "깜빡이는 원소를 누르세요. 영웅 아래 원소는 매 T 다시 쓸 수 있으니 쌓일수록 큰 카드를 낼 수 있어요.");
            yield return WaitMain(3);
            if (g.CanUseHero(g.Human))
                yield return DoUi("영웅 능력", "영웅 능력은 매치에 <b>1회</b> 쓸 수 있는 강력한 한 수 (왼쪽 아래 버튼 또는 H 키). 아껴 두었다가 큰 몬스터가 나올 때 쓰는 게 보통이지만, 지금 한번 써 볼게요.\n깜빡이는 <b>[영웅 능력]</b> 을 누르세요.", hud.HeroBtnRt, () => g.Human.heroUsed || g.busy);
            else yield return Tell("영웅 능력", "영웅 능력은 매치에 1회, 왼쪽 아래 <b>[영웅 능력]</b> 버튼이나 H 키로 씁니다. 지금은 조건이 안 돼 넘어갑니다.");
            while (g.busy && !Over) yield return null;
            var elems = g.ElementCards(g.Human);
            if (elems != null && elems.Count > 0)
            {
                var ec = elems[0];
                yield return DoCard("원소 더미 보기", "영웅 아래에 쌓인 원소 더미(또는 상대 것)를 <b>클릭</b>하면 속성별로 한눈에 볼 수 있어요. 깜빡이는 원소 더미를 클릭하세요.", ec, () => hud.GraveOpen);
                yield return DoUi("원소 보기 창", "속성별 장수와 이번 T 남은 수가 보입니다. 어두운 카드는 이번 T 이미 쓴 원소. 묘지 더미를 클릭하면 같은 창에서 묘지도 볼 수 있어요.\n<b>[닫기]</b> 를 누르세요.", hud.GraveCloseRt, () => !hud.GraveOpen);
            }
            yield return Tell("자유 배치", "이번 T 는 자유롭게 놓아 보세요. 손패의 초록 테두리 카드를 빈 칸에 끌어다 놓고(소환수·마법), 놓은 카드를 끌어 다른 칸으로 옮길 수도 있어요.\n다 놓았으면 [배치 완료]. 카드 클릭 = 확대, 우클릭 = 바로 내기.");
            guide.Show("자유 배치 중", "원하는 만큼 놓고 <b>[배치 완료]</b> 를 누르세요. (이 상자는 막지 않습니다)", null, null, null, "", false, Step());
            while (!PlacementDone && !Over) yield return null;

            // 8) T3 배틀 · 마무리
            yield return WatchBattle(() => g.time >= 4 && (hud.FateOpen || g.phase == Phase.Main));
            bool done = false;
            guide.Show("튜토리얼 끝!", "모든 순서를 익혔어요. 남은 대전은 자유롭게 진행하세요 (수련 CPU, 기록·보상 없음).\n그 밖에: 오른쪽 <b>[AUTO]</b> 는 자동 진행(1분 동안 입력이 없으면 저절로 켜짐), <b>[▶▶ x2]</b> 는 2배속, 상대 영웅 HP 를 0 으로 만들면 승리, 덱이 바닥나면 영웅끼리 결투합니다.",
                null, null, () => done = true, "자유 진행 ▶", true, "마지막");
            while (!done && !Over) yield return null;
            guide.Hide();
        }

        IEnumerator ElementStep(string title, string body)
        {
            while (!hud.FateOpen && !Over) yield return null;
            var heroEl = g.Human.hero != null ? g.Human.hero.data.element : Element.None;
            var cell = hud.FateCellRt("ELEM_" + heroEl) ?? hud.FirstFateCell;
            yield return DoUi(title, body, cell, () => !hud.FateOpen);
        }

        CardInstance Playable(Func<CardInstance, bool> pred)
            => g.Human.hand.Where(c => pred(c) && c.view != null && (c.IsMagic ? g.CanSet(c) : g.CanSummon(c, out _)))
                .OrderBy(c => c.data.Cost).FirstOrDefault();

        int FirstEmpty()
        {
            for (int i = 0; i < PlayerState.SlotCount; i++) if (g.Human.slots[i] == null) return i;
            return 0;
        }
    }
}
