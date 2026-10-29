using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 동시 진행 룰 (2026-10-12 사용자 룰 변경, 일반 대전·스토리. 레이드는 아직 예전 교대 룰):
    ///   T 시작 → 양쪽 드로우 (T2 2장 · T3 3장 … T7~ 7장, 패 최대 9장) → 배치(30초): 양쪽이 각자 소환수·마법을 뒷면으로 낸다
    ///   (T2 부터는 살아남은 소환수 자리 이동 가능, 앞면으로 남은 잔존형 마법은 이동 불가)
    ///   → 배틀: 이번 T 의 공격 측만 왼쪽 칸부터 차례로 공개·공격 (수비 측 소환수는 반격만, 마법은 양쪽 모두 그 칸 차례에).
    ///     마법 = 그 차례에 공개·발동, 소환수 = 공개(소환 효과) 후 상대의 가장 앞(왼쪽) 소환수를 공격
    ///     ([도발] 이 있으면 도발 우선), 상대 소환수가 없으면 영웅 직접 공격
    ///   → 배틀 엔드 → 양쪽 패 버리기/보존 선택.
    /// </summary>
    public partial class GameManager
    {
        /// <summary>새 동시 룰을 쓰는가 (레이드는 보스 진행 방식 때문에 아직 교대 룰)</summary>
        public bool Simul => raid == null;
        public const float PlanSeconds = 30f;
        /// <summary>동시 룰 T 시작 드로우 상한 (T 번호만큼, 최대 7장)</summary>
        public const int MaxDrawPerTime = 7;
        /// <summary>동시 룰 패 최대 9장 · 엔드 타임에 남길 수 있는 패 7장</summary>
        public const int SimulHandMax = 9, EndHandMax = 7;
        float planDeadline = -1f;

        /// <summary>배치 남은 시간 (초). 시간 제한이 없거나 배치 중이 아니면 -1</summary>
        public float PlanSecondsLeft => phase == Phase.Main && planDeadline > 0 ? Mathf.Max(0, planDeadline - Time.unscaledTime) : -1f;

        /// <summary>이번 T 에 같은 칸에서 먼저 행동하는 쪽 (선공은 홀수 T, 후공은 짝수 T 에 먼저)</summary>
        public PlayerState LaneFirst => (time % 2 == 1) ? first : first.opponent;

        IEnumerator SimulTimeRoutine()
        {
            SealNow();      // 시간 봉인: 이 T 의 처음 상태 (GameManager.Seal.cs)
            var lf = LaneFirst;
            foreach (var p in players)
                ExpireStatuses(s => s.expire == Expire.CasterNextTime && s.caster == p && s.createdTime < time);

            phase = Phase.TimeStart;
            active = Human;
            ResetDirectCap();
            ResetElementSpent();
            RegenMonsters();
            RefreshAll();
            Sfx.Play("turn", 0.7f);
            yield return hud.Banner($"TIME {time}", (mode != MatchMode.Puzzle ? "원소 1장 · " : "") + "카드를 칸으로 끌어다 놓으세요 (뒷면)" + (story == null && mode != MatchMode.Puzzle ? $" ({PlanTime:0}초)" : ""));
            if (story != null) { yield return StoryTimeStart(); if (gameOver) yield break; }

            // 타임 시작 t0 (예전 '상대 타임 시작 전')
            foreach (var p in new[] { lf, lf.opponent })
            {
                active = p;
                yield return T0Window(Trigger.OpponentTimeStart, p);
                if (gameOver) yield break;
            }

            // 양쪽 드로우: T2 2장 · T3 3장 … T7 부터 7장 (패 최대 9장). 첫 T 는 시작 패 7장으로 시작하므로 드로우 없음
            foreach (var p in new[] { lf, lf.opponent })
            {
                p.fieldPlayedThisTime = false;
                if (time > 1 && p.hand.Count < HandLimit) yield return Draw(p, Mathf.Min(DrawCount, HandLimit - p.hand.Count));
                if (gameOver) yield break;
            }

            // ── 엑스트라 덱: 양쪽이 동시에 원소 1장 (GameManager.Element.cs) ──
            phase = Phase.Main;
            yield return ElementStep();
            if (gameOver) yield break;

            // ── 배치 ──
            phase = Phase.Main;
            active = Human;
            RefreshAll();
            if (HumanDecides(Human))
            {
                StoryMainCoach();
                endMainRequested = battleRequested = false;
                // 배치 시간은 실제 시간 (2배속·연출 속도와 무관하게 30초). 스토리(튜토리얼)는 시간 제한 없음
                planDeadline = story == null && mode != MatchMode.Puzzle ? Time.unscaledTime + PlanTime : -1f;
                while (!endMainRequested && !gameOver && !(planDeadline > 0 && Time.unscaledTime >= planDeadline)) yield return null;
                if (!endMainRequested && !gameOver) { Log("배치 시간 종료 — 놓은 카드로 진행"); hud.CloseMenu(); }
                planDeadline = -1f;
                while (busy) yield return null;
            }
            else
            {
                busy = true;
                yield return ai.PlanPhase(Human);
                busy = false;
            }
            if (gameOver) yield break;
            active = Cpu;
            busy = true;
            if (mode != MatchMode.Puzzle) yield return ai.PlanPhase(Cpu);   // 퍼즐: 상대 배치는 정해져 있다
            busy = false;
            if (gameOver) yield break;

            // ── 배틀 ──
            phase = Phase.Battle;
            RefreshAll();
            yield return hud.Banner("BATTLE", lf == Human ? "내 공격 턴 — 왼쪽 칸부터 차례로 공격 (상대는 반격만)" : "수비 턴 — 상대가 왼쪽 칸부터 공격 (내 소환수는 반격만)");
            busy = true;
            // 배틀 시작: 양쪽 칸의 뒷면 카드를 모두 펼쳐 보여 준다 (보여 주기만 — 소환 효과·마법 발동은 지금처럼 그 칸 차례에)
            battleOpenTime = time;
            if (players.Any(p => p.slots.Any(s => s != null && s.faceDown)))
            {
                Sfx.Play("flip", 0.8f);
                Log("배틀 시작 — 모든 카드 공개");
                RefreshAll();
                yield return Wait(1.1f);
            }
            foreach (var p in new[] { lf, lf.opponent })
            {
                active = p;
                yield return T0Window(Trigger.OpponentBattle, p);
                if (gameOver) yield break;
            }
            yield return LaneBattle(lf);
            if (gameOver || mode == MatchMode.Puzzle) yield break;   // 퍼즐은 이 배틀로 판정

            // ── 배틀 엔드 ──
            phase = Phase.BattleEnd;
            RefreshAll();
            foreach (var p in new[] { lf, lf.opponent })
            {
                active = p;
                yield return T0Window(Trigger.BattleEnd, p);
                if (gameOver) yield break;
            }
            ExpireStatuses(s => s.expire == Expire.NextBattleEnd);
            ClearWeaken();
            yield return HeroDuel();
            if (gameOver) yield break;
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null && c.IsMonster && c.data.regen && c.damage > 0)
                    {
                        c.damage = 0;
                        Log($"{c.Name}: [재생] HP 모두 회복 ({MaxHp(c)})");
                        if (c.view != null) hud.FloatText(c.view.transform.position, "재생", new Color(0.5f, 1f, 0.6f));
                    }
            RefreshAll();

            // ── 엔드: 양쪽 패 버리기 / 보존 ──
            phase = Phase.End;
            RefreshAll();
            StoryCoach("end");
            foreach (var p in new[] { Human, Cpu })
            {
                active = p;
                yield return EndPhaseDiscard(p);
                if (gameOver) yield break;
            }
            ExpireStatuses(s => s.expire == Expire.EndOfTime);
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null) { c.tempAtk = 0; c.priorityMod = 0; }
            extraAttacks.Clear();
            busy = false;
            active = Human;
            RefreshAll();
            yield return Wait(0.3f);
        }

        /// <summary>
        /// 공격 턴 (판타지 마스터즈식): 이번 T 의 공격 측(laneFirst) 소환수만 왼쪽 칸부터 차례로 공개·공격한다.
        /// 수비 측 소환수는 먼저 공격하지 않고, 공격받으면 맞서 싸운다(반격). 공격 대상은 수비 측 가장 앞(왼쪽) 소환수 —
        /// 쓰러지면 다음 소환수로, 없으면 영웅. 마법은 양쪽 모두 그 칸 차례에 공개·발동 (공격 측 → 수비 측).
        /// </summary>
        IEnumerator LaneBattle(PlayerState laneFirst)
        {
            attacksUsed.Clear();
            curLanes = new string[PlayerState.SlotCount];
            var acted = new HashSet<int>();
            var atk = laneFirst;
            var def = laneFirst.opponent;
            // [선봉]: 칸 순서보다 먼저 (공격 측만, 선봉끼리는 왼쪽 칸부터)
            for (int i = 0; i < PlayerState.SlotCount && !gameOver; i++)
            {
                var c = atk.slots[i];
                if (c == null || !c.IsMonster || !c.data.vanguard || acted.Contains(c.uid)) continue;
                acted.Add(c.uid);
                Log($"{atk.name}: [선봉] {i + 1}번 칸이 먼저 나선다");
                yield return ActCard(c, atk, i);
            }
            for (int i = 0; i < PlayerState.SlotCount && !gameOver; i++)
            {
                var c = atk.slots[i];
                if (c != null && !acted.Contains(c.uid)) { acted.Add(c.uid); yield return ActCard(c, atk, i); }
                if (gameOver) yield break;
                // 수비 측은 마법만 그 칸 차례에 발동 (소환수는 공격받을 때만 싸운다)
                var d = def.slots[i];
                if (d != null && d.IsMagic && d.faceDown && !acted.Contains(d.uid)) { acted.Add(d.uid); yield return ActCard(d, def, i); }
            }
            hud.SetLaneMarker(-1);
            LastCpuLanes = curLanes;
        }

        IEnumerator ActCard(CardInstance c, PlayerState p, int i)
        {
            active = p;
            hud.SetLaneMarker(i);
            board.Pulse(c);
            if (p == Cpu && curLanes != null && i >= 0 && i < curLanes.Length) curLanes[i] = c.faceDown && Eclipsed ? "(어둠 속)" : c.Name;
            if (Eclipsed && c.faceDown && !Resist(c))
            {
                Log($"{p.name}: {i + 1}번 칸 — 개기일식의 어둠 속에서 움직이지 못한다");
                yield return Wait(0.2f);
                yield break;
            }
            if (c.IsMagic) { yield return RevealMagic(c); yield break; }
            if (!c.IsMonster) yield break;
            if (c.faceDown) yield return Reveal(c);
            if (gameOver || c.zone != Zone.Slot) yield break;
            if (c.stunUntil >= time) { Log($"{c.Name}: 행동 불능 — 공격하지 못한다"); yield return Wait(0.3f); yield break; }
            if (AttacksLeft(c) <= 0) yield break;
            var t = LaneTarget(c);
            if (t != null) yield return AttackWith(c, t);
        }

        /// <summary>
        /// 공격 대상: 상대 소환수가 없으면 영웅. 기본은 가장 앞(왼쪽) — [도발] 우선.
        /// [저격] 은 가장 뒤(오른쪽) ([도발] 우선), 저격 조준 등 FreeTarget 상태면 [도발] 도 무시하고 가장 뒤.
        /// </summary>
        public CardInstance LaneTarget(CardInstance a)
        {
            var opp = a.owner.opponent;
            var mons = opp.Monsters();
            if (Eclipsed) mons = mons.Where(Resist).ToList();   // 개기일식: [불변] 끼리만 싸우고, 없으면 영웅
            if (mons.Count == 0) return opp.hero;
            bool free = HasStatus(StatusKind.FreeTarget, a.owner);
            if (!free)
            {
                var taunt = mons.FirstOrDefault(IsTaunt);
                if (taunt != null) return taunt;
            }
            return Snipe(a) || free ? mons.OrderByDescending(m => m.slot).First() : mons.OrderBy(m => m.slot).First();
        }

        /// <summary>
        /// 영웅 대결: 덱이 빈 쪽이 있으면 배틀 엔드마다 영웅끼리 맞붙는다.
        /// 덱이 빈 뒤로 지난 T 수만큼 피해가 커져(영웅 ATK + 경과 T) 결국 승부가 난다.
        /// </summary>
        IEnumerator HeroDuel()
        {
            if (!players.Any(p => p.deck.Count == 0)) yield break;
            duelRounds++;
            yield return hud.Banner("HERO DUEL", $"덱이 비었다 — 영웅끼리 맞붙는다 ({duelRounds}번째)", 1.0f);
            foreach (var p in new[] { LaneFirst, LaneFirst.opponent })
            {
                if (gameOver) yield break;
                int dmg = HeroAtk(p) + duelRounds;
                yield return HeroStrike(p.hero, board.HeroPos(p.opponent));
                Log($"영웅 대결: {p.hero.Name} → {p.opponent.hero.Name} {dmg} 데미지");
                yield return DamagePlayer(p.opponent, dmg, $"영웅 {p.hero.Name}");
            }
        }
        int duelRounds;

        // ── [불변] · 개기일식 ──

        int eclipseTime = -1;
        int battleOpenTime = -1;
        /// <summary>배틀 시작 전체 공개 중 (뒷면 카드를 앞면으로 보여 준다, 개기일식 중에는 다시 가려진다)</summary>
        public bool BattleOpen => Simul && battleOpenTime == time && phase == Phase.Battle && !Eclipsed;
        /// <summary>이번 배틀에 개기일식이 발동했다: 뒷면 카드는 행동·피격 불가, [불변] 소환수만 싸운다</summary>
        public bool Eclipsed => eclipseTime == time && phase == Phase.Battle;

        /// <summary>상대 카드 효과(파괴 제외)의 대상이 될 수 있는 소환수 ([불변] 제외). 파괴 효과는 opponent.Monsters() 를 그대로 쓴다</summary>
        public List<CardInstance> EffectTargets(PlayerState victim) => victim.Monsters().Where(m => !Resist(m)).ToList();

        /// <summary>지금 실행 중인 상대 카드 효과를 [불변] 이 막는가 (막으면 표시까지)</summary>
        bool BlockedByResist(CardInstance c)
        {
            if (c == null || !Resist(c) || c.zone != Zone.Slot) return false;
            if (fxSource == null || fxSource.owner == c.owner) return false;
            Log($"{c.Name}: [불변] — {fxSource.Name} 의 효과를 받지 않는다");
            if (c.view != null) hud.FloatText(c.view.transform.position, "불변", new Color(0.9f, 0.85f, 1f));
            return true;
        }

        /// <summary>개기일식: 필드의 앞면 소환수를 모두 뒷면으로 ([불변] 제외). 반환 = 뒷면이 된 수. 이번 T 배틀이 개기일식 상태가 된다 (일반 마법이라 배치 중 발동해도)</summary>
        public int FlipAllFaceDown()
        {
            if (phase == Phase.Main || phase == Phase.Battle) eclipseTime = time;
            int n = 0;
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null && c.IsMonster && !c.faceDown && !Resist(c)) { c.faceDown = true; n++; }
            Log(n > 0 ? $"개기일식 — 소환수 {n}장이 뒷면이 되었다" : "개기일식 — 뒷면이 될 소환수가 없다");
            RefreshAll();
            return n;
        }

        /// <summary>뒷면 소환수 공개: 앞면이 되며 소환 효과 발동</summary>
        public IEnumerator Reveal(CardInstance c)
        {
            if (!c.faceDown) yield break;
            c.faceDown = false;
            if (!c.summonFxDone) NoteCpuCard(c);
            Sfx.Play("summon");
            Log($"{c.owner.name}: {c.slot + 1}번 칸 공개 — {c.Name}");
            RefreshAll();
            board.SummonFx(c);
            if (fx != null) fx.RevealFlip(c);   // 뒤집히며 속성 연출
            yield return Wait(0.45f);
            if (!c.summonFxDone) { c.summonFxDone = true; yield return RunEffect(c); }   // 다시 공개될 때는 소환 효과 없음
            RefreshAll();
        }

        /// <summary>뒷면 마법 공개: 발동 조건이 맞으면 발동, 아니면 불발로 묘지</summary>
        IEnumerator RevealMagic(CardInstance m)
        {
            NoteCpuCard(m);
            if (m.data.canActivate == null || m.data.canActivate(this, m))
            {
                yield return ActivateMagic(m);
                yield break;
            }
            m.faceDown = false;
            RefreshAll();
            yield return Wait(0.4f);
            var p = m.owner;
            if (m.slot >= 0) p.slots[m.slot] = null;
            m.slot = -1;
            m.zone = Zone.Grave;
            p.grave.Add(m);
            Log($"{p.name}: 마법 [{m.Name}] 발동 조건이 맞지 않아 불발");
            if (m.view != null) hud.FloatText(m.view.transform.position, "불발", new Color(0.75f, 0.75f, 0.8f));
            RefreshAll();
            yield return Wait(0.3f);
        }

        // ── 배치 중 자리 이동 ──

        /// <summary>배치 중 내 칸 카드를 왼쪽(-1)/오른쪽(+1) 칸으로 옮길 수 있는가 (앞면 마법 = 잔존형은 이동 불가, 옆 칸과 자리를 바꾼다)</summary>
        public bool CanMoveSlot(CardInstance c, int dir)
        {
            if (!Simul || phase != Phase.Main || c == null || c.zone != Zone.Slot || active != c.owner) return false;
            if (c.IsMagic && !c.faceDown) return false;
            int to = c.slot + dir;
            if (to < 0 || to >= PlayerState.SlotCount) return false;
            var other = c.owner.slots[to];
            return other == null || !(other.IsMagic && !other.faceDown);
        }

        public IEnumerator MoveSlot(CardInstance c, int dir)
        {
            if (!CanMoveSlot(c, dir)) yield break;
            var p = c.owner;
            int from = c.slot, to = from + dir;
            var other = p.slots[to];
            p.slots[to] = c; c.slot = to;
            p.slots[from] = other;
            if (other != null) other.slot = from;
            Sfx.Play("flip", 0.6f);
            RefreshAll();
            yield return Wait(0.15f);
        }

        /// <summary>드래그 배치: 패 카드를 빈 칸 slot 에 놓을 수 있는가</summary>
        public bool CanPlaceAt(CardInstance c, int slot)
        {
            if (!Simul || phase != Phase.Main || c == null || c.zone != Zone.Hand || active != c.owner) return false;
            if (slot < 0 || slot >= PlayerState.SlotCount || c.owner.slots[slot] != null) return false;
            if (c.IsMonster) return CanSummon(c, out _);
            if (c.IsMagic) return CanSet(c);
            return false;
        }

        /// <summary>드래그 이동: 내 칸 카드를 to 칸으로 (차 있으면 자리 바꾸기, 앞면 잔존형 마법은 고정)</summary>
        public bool CanMoveSlotTo(CardInstance c, int to)
        {
            if (!Simul || phase != Phase.Main || c == null || c.zone != Zone.Slot || active != c.owner) return false;
            if (to < 0 || to >= PlayerState.SlotCount || to == c.slot) return false;
            if (c.IsMagic && !c.faceDown) return false;
            var other = c.owner.slots[to];
            return other == null || !(other.IsMagic && !other.faceDown);
        }

        public IEnumerator MoveSlotTo(CardInstance c, int to)
        {
            if (!CanMoveSlotTo(c, to)) yield break;
            var p = c.owner;
            int from = c.slot;
            var other = p.slots[to];
            p.slots[to] = c; c.slot = to;
            p.slots[from] = other;
            if (other != null) other.slot = from;
            Sfx.Play("flip", 0.6f);
            RefreshAll();
            yield return Wait(0.12f);
        }

        /// <summary>AI 배치: 칸 순서를 통째로 다시 정한다 (잔존형 마법은 제자리)</summary>
        public void ArrangeSlots(PlayerState p, List<CardInstance> order)
        {
            var fixedSlots = new HashSet<int>();
            for (int i = 0; i < PlayerState.SlotCount; i++)
                if (p.slots[i] != null && p.slots[i].IsMagic && !p.slots[i].faceDown) fixedSlots.Add(i);
            var movable = order.Where(c => c != null && c.zone == Zone.Slot && !fixedSlots.Contains(c.slot)).ToList();
            for (int i = 0; i < PlayerState.SlotCount; i++) if (!fixedSlots.Contains(i)) p.slots[i] = null;
            int k = 0;
            for (int i = 0; i < PlayerState.SlotCount && k < movable.Count; i++)
            {
                if (fixedSlots.Contains(i)) continue;
                p.slots[i] = movable[k];
                movable[k].slot = i;
                k++;
            }
            RefreshAll();
        }
    }
}
