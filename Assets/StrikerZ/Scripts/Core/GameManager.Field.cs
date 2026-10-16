using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 필드 · 엑스트라 덱 (동시 룰):
    ///   필드 카드는 메인 덱이 아닌 엑스트라 덱(최대 10장, 같은 필드 1장씩)에 둔다.
    ///   T1 은 시작 필드(랜덤). T2 부터 양쪽이 번갈아 필드 차례를 갖는다 — 필드 차례인 쪽만 배치 중 엑스트라 덱에서 필드 1장을 전개.
    ///   누가 T2(먼저)에 필드를 낼지는 대전 시작 때 자동 코인 토스 → 앞면이면 플레이어, 뒷면이면 상대가 선/후 선택(지형 우선 선택권). (스토리는 플레이어가 먼저)
    ///   덮인 필드는 낸 사람의 묘지로.
    /// </summary>
    public partial class GameManager
    {
        /// <summary>T2 에 먼저 필드를 내는 쪽</summary>
        public PlayerState fieldFirst;

        /// <summary>이번 T 에 필드를 낼 수 있는 쪽 (T1 은 없음)</summary>
        public PlayerState FieldTurn => !Simul || time < 2 || fieldFirst == null ? null : (time % 2 == 0 ? fieldFirst : fieldFirst.opponent);

        /// <summary>대전 시작: 필드 코인 토스 — 자동으로 내 코인(프로필에 장착한 코인)을 던져 앞면이면 내가, 뒷면이면 상대가 지형 우선 선택권(필드 선/후)을 갖는다</summary>
        IEnumerator FieldCoinToss()
        {
            if (mode == MatchMode.Story || mode == MatchMode.Puzzle) { fieldFirst = Human; yield break; }
            yield return hud.Banner("필드 코인 토스", "앞면이 나오면 지형 우선 선택권을 갖습니다", 0.9f);
            bool heads = rng.Next(2) == 0;
            yield return dice.FlipCoinCenter(heads);
            var win = heads ? Human : Cpu;
            hud.ShowCoinResult(heads, heads ? "지형 우선 선택권 획득!" : "상대가 지형 우선 선택권을 가져갑니다");
            Log($"필드 코인 토스: {(heads ? "앞면" : "뒷면")} → {win.name} 지형 우선 선택권");
            yield return Wait(1.3f);
            hud.HideCoinToss();
            dice.Clear();
            bool takeFirst = true;
            if (win == Human && HumanDecides(Human))
            {
                int pick = 0;
                yield return ChooseOption(Human, "앞면! 지형 우선 선택권 — 필드 전개 순서를 고르세요", new[] { "선 — T2 에 먼저 필드", "후 — T3 에 필드" }, () => 0, r => pick = r);
                takeFirst = pick == 0;
            }
            else takeFirst = rng.Next(100) < 70;    // CPU 는 대개 선
            fieldFirst = takeFirst ? win : win.opponent;
            yield return hud.Banner(fieldFirst == Human ? "필드 선" : "필드 후", fieldFirst == Human ? "짝수 T(2·4·6…)에 필드를 낼 수 있습니다" : "홀수 T(3·5·7…)에 필드를 낼 수 있습니다", 1.0f);
        }

        /// <summary>사람: 엑스트라 덱에서 필드 고르기 → 전개 (HUD 필드 버튼)</summary>
        public IEnumerator ChooseAndPlayField()
        {
            var options = Human.extra.Where(CanPlayField).ToList();
            if (options.Count == 0) yield break;
            CardInstance pick = null;
            yield return ChooseFromList(Human, options, "엑스트라 덱 — 전개할 필드를 고르세요", c => 0, r => pick = r, true, "취소");
            if (pick != null) yield return PlayField(pick);
        }

        public bool CanPlayAnyField(PlayerState p) => p != null && p.extra.Any(CanPlayField);

        /// <summary>SetupPlayer: 필드 카드는 엑스트라 덱으로 (같은 필드 1장씩, 최대 10장 · 넘치는 건 버림)</summary>
        void SplitExtra(PlayerState p, List<CardData> cards)
        {
            foreach (var f in cards.Where(c => c.type == CardType.Field).ToList())
            {
                cards.Remove(f);
                if (p.extra.Count >= DeckRules.MaxExtra || p.extra.Any(x => x.data == f)) continue;
                p.extra.Add(new CardInstance(f, p) { zone = Zone.Extra });
            }
        }
    }
}
