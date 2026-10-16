using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 운명 재배치 (멀리건): 시작 패 7장을 받은 뒤, 양쪽이 동시에 바꿀 카드를 골라 한 번 교체한다 (30초).
    /// 고른 장수만큼 덱에서 새로 뽑고, 돌려보낸 카드는 덱 아무 곳에 섞어 넣는다 (새로 뽑은 카드와 겹치지 않음).
    /// 스토리(튜토리얼 각본) · 마법진 퍼즐에서는 하지 않는다.
    /// </summary>
    public partial class GameManager
    {
        public const float FateSeconds = 30f;

        bool UsesFateRedraw => mode != MatchMode.Story && mode != MatchMode.Puzzle;

        IEnumerator FateRedraw()
        {
            yield return hud.Banner("운명 재배치", "바꾸고 싶은 시작 패를 고르세요 (1회)", 0.7f);
            List<CardInstance> humanPick = null;
            if (HumanDecides(Human))
            {
                bool done = false;
                hud.ShowFate(Human.hand.ToList(), FateSeconds, list => { humanPick = list; done = true; });
                while (!done && !gameOver) yield return null;
            }
            else humanPick = ai.FatePick(Human);
            var cpuPick = raid != null ? new List<CardInstance>() : ai.FatePick(Cpu);
            yield return Redraw(Human, humanPick ?? new List<CardInstance>());
            yield return Redraw(Cpu, cpuPick);
            RefreshAll();
        }

        IEnumerator Redraw(PlayerState p, List<CardInstance> back)
        {
            back = back.Where(c => c != null && c.owner == p && c.zone == Zone.Hand).ToList();
            if (back.Count == 0) { Log($"{p.name}: 운명 재배치 — 그대로 시작"); yield break; }
            foreach (var c in back) p.hand.Remove(c);
            RefreshAll();
            yield return Draw(p, back.Count, 0.08f);
            foreach (var c in back)
            {
                c.zone = Zone.Deck;
                p.deck.Insert(rng.Next(p.deck.Count + 1), c);
            }
            Log($"{p.name}: 운명 재배치 — {back.Count}장 교체");
            RefreshAll();
        }
    }
}
