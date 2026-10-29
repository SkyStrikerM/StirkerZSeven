using System.Collections;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 마법진 퍼즐 진행: 정해진 상태로 한 T 만 진행 → 그 T 의 배틀이 끝날 때 상대 영웅 HP 가 0 이면 성공.
    /// 퍼즐 중에는 주사위·코인 효과 없음(NoDice), 영웅 능력 사용 불가, 드로우 없음, CPU 는 배치하지 않는다.
    /// </summary>
    public partial class GameManager
    {
        public PuzzleDef puzzle;
        PuzzleDef pendingPuzzle;

        /// <summary>전투 주사위·코인 효과를 쓰지 않는다 (퍼즐: 결과가 늘 같게)</summary>
        public bool NoDice => mode == MatchMode.Puzzle;

        public void StartPuzzle(PuzzleDef p)
        {
            pendingPuzzle = p;
            SetNextMode(MatchMode.Puzzle);
            var hero = CardDB.Get(p.hero) ?? CardList.Heroes[0];
            StartMatch(hero, p.Field);
        }

        /// <summary>MatchRoutine 이 퍼즐이면 이쪽으로: 상태 배치 → 한 T → 판정</summary>
        IEnumerator PuzzleRoutine(CardData fieldData)
        {
            var p = puzzle;
            var me = Human;
            var opp = Cpu;
            var oppHero = CardDB.Get(p.oppHero) ?? CardList.Heroes[1];
            SetupHeroOnly(me, humanHero, p.myHp);
            SetupHeroOnly(opp, oppHero, p.oppHp);
            me.freeCost = opp.freeCost = true;     // 퍼즐은 정해진 패로 푼다 — 원소 비용 없음
            foreach (var id in p.hand)
            {
                var d = CardDB.Get(id);
                if (d == null) continue;
                var c = new CardInstance(d, me) { zone = Zone.Hand };
                me.hand.Add(c);
                board.CreateView(c, board.DeckPos(me, 1));
            }
            for (int i = 0; i < PlayerState.SlotCount && p.oppSlots != null && i < p.oppSlots.Length; i++)
            {
                var d = p.oppSlots[i] != null ? CardDB.Get(p.oppSlots[i]) : null;
                if (d == null) continue;
                var c = new CardInstance(d, opp) { zone = Zone.Slot, slot = i, faceDown = false, summonFxDone = true };
                opp.slots[i] = c;
                board.CreateView(c, board.SlotPos(opp, i) + Vector3.up * 2f);
            }
            me.deckOutWarned = opp.deckOutWarned = true;
            hud.BeginMatch(this);
            center = new CardInstance(fieldData, null) { zone = Zone.Center };
            board.CreateView(center, new Vector3(0, 3f, 0));
            RefreshAll();
            yield return Wait(0.4f);
            yield return hud.Banner("마법진 퍼즐  " + p.title, $"이번 T 안에 상대 영웅 HP {p.oppHp} 를 0 으로", 1.3f);
            // 이번 T 의 같은 칸 선후: LaneFirst = 홀수 T → first
            bool odd = p.time % 2 == 1;
            first = p.humanFirst == odd ? Human : Cpu;
            busy = false;
            time = p.time - 1;
            time++;
            yield return SimulTimeRoutine();
            if (!gameOver)
            {
                Log($"퍼즐 실패 — 상대 영웅 HP {Cpu.hp} 남음");
                EndGame(Cpu);
            }
            yield return FinishMatch();
        }

        void SetupHeroOnly(PlayerState p, CardData heroData, int hp)
        {
            p.hero = new CardInstance(heroData, p) { zone = Zone.Hero };
            p.maxHp = p.hp = hp;
            board.CreateView(p.hero, board.HeroPos(p) + Vector3.up * 2f);
            HeroModel.Attach(p.hero.view, heroData.id, p.index == 0);
        }

        /// <summary>테스트용: 퍼즐 배치를 코드로 (손패 index → 칸)</summary>
        public void TestPlace(CardInstance c, int slot)
        {
            if (c == null || c.zone != Zone.Hand) return;
            if (c.IsMonster) StartCoroutine(Summon(c, true, slot));
            else if (c.IsMagic) StartCoroutine(c.data.speed == MagicSpeed.Set ? SetMagic(c, slot) : CastMagic(c));   // 일반 마법은 패에서 바로
        }
    }
}
