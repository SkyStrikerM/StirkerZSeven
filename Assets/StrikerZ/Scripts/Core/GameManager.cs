using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 규칙 엔진 + 진행 흐름.
    /// TIME 1(선공) → TIME 2(후공) → ... 홀수 타임 = 선공, 짝수 타임 = 후공.
    /// 각 타임: [상대 t0 창] → 드로우(패 7장까지) → 메인 → 배틀(드래그앤드롭 공격 · t/half 클릭 발동) → 배틀 엔드(t0 창) → 종료
    /// </summary>
    public partial class GameManager : MonoBehaviour
    {
        public const int StartHand = 7;
        /// <summary>패 최대 장수. 매 타임 드로우는 패가 이 장수가 될 때까지, 효과 드로우도 이 이상은 뽑지 않는다</summary>
        public const int HandMax = 7;
        /// <summary>몬스터 전투 시 (파워 차이 × 비율) 만큼 패배 측 영웅에게 데미지</summary>

        public static GameManager I { get; private set; }

        public readonly PlayerState[] players = new PlayerState[2];
        public PlayerState Human => players[0];
        public PlayerState Cpu => players[1];
        public PlayerState first, active;
        public int time;
        public Phase phase;
        public CardInstance center;
        public readonly List<Status> statuses = new List<Status>();
        public bool gameOver;
        public PlayerState winner;
        public bool busy;
        public int matchNumber;

        public BoardView board;
        /// <summary>카드 효과 연출 (없으면 연출 생략)</summary>
        public CardFx fx;
        /// <summary>지금 효과를 처리 중인 카드 — 데미지·파괴 연출이 이 카드에서 날아간다</summary>
        CardInstance fxSource;

        /// <summary>시나리오 테스트용 (연출 포함 효과 실행)</summary>
        public IEnumerator TestRunEffect(CardInstance c) => RunEffect(c);

        /// <summary>카드 효과 실행: 고유 연출(Signature) → 효과. 실행 중에는 fxSource 로 연출 출발점을 기억</summary>
        IEnumerator RunEffect(CardInstance c)
        {
            var prev = fxSource;
            fxSource = c;
            if (fx != null) yield return fx.Signature(this, c);
            if (c.data.effect != null) yield return c.data.effect(this, c);
            fxSource = prev;
        }
        public HUD hud;
        public DiceRoller dice;
        public AIController ai;

        public readonly System.Random rng = new System.Random();
        public CardData humanHero;
        /// <summary>사람 플레이어 덱 (null 이면 랜덤 덱)</summary>
        public List<CardData> humanDeck;
        public string humanName = "플레이어", humanAvatar = "avatar_01";
        public const string CpuName = "CPU";
        /// <summary>자동 테스트: 사람 쪽도 AI 가 조작</summary>
        public bool autoHuman;
        bool HumanDecides(PlayerState p) => p.isHuman && !autoHuman;
        public event Action OnMatchFinished;

        bool endMainRequested, battleRequested;
        readonly Dictionary<int, int> extraAttacks = new Dictionary<int, int>();

        // 선택 대기
        List<CardInstance> choiceOptions;
        CardInstance choiceResult;
        bool choiceDone;

        void Awake() { I = this; }

        public bool IsHumanMain => !gameOver && phase == Phase.Main && active == Human && !busy && choiceOptions == null;
        public bool ChoicePending => choiceOptions != null;
        public bool IsChoiceOption(CardInstance c) => choiceOptions != null && choiceOptions.Contains(c);

        public IEnumerator Wait(float t) { yield return new WaitForSeconds(t); }

        public void Log(string msg) { if (hud != null) hud.Log(msg); ScrollAdd(msg); Debug.Log("[StrikerZ] " + msg); }

        // ══════════════════════════ 매치 ══════════════════════════

        public void StartMatch(CardData heroData, CardData fieldData)
        {
            StopAllCoroutines();
            raid = pendingRaid;            // StartRaid 로 시작하면 레이드, 아니면 일반 대전
            pendingRaid = null;
            story = pendingStory;          // StartStory 로 시작하면 스토리 전투
            pendingStory = null;
            mode = raid != null ? MatchMode.Raid : story != null ? MatchMode.Story : pendingMode;
            pendingMode = MatchMode.Duel;
            puzzle = mode == MatchMode.Puzzle ? pendingPuzzle : null;
            pendingPuzzle = null;
            humanHero = heroData;
            StartCoroutine(MatchRoutine(heroData, fieldData));
        }

        IEnumerator MatchRoutine(CardData heroData, CardData fieldData)
        {
            matchNumber++;
            duelRounds = 0;
            eclipseTime = battleOpenTime = -1;      // 지난 대전의 개기일식이 같은 T 번호에서 다시 켜지지 않게
            ResetExtrasData();
            board.ClearCards();
            BossModel.Clear();
            statuses.Clear();
            extraAttacks.Clear();
            gameOver = false; MatchFinished = false; winner = null; surrendered = false; time = 0; center = null; phase = Phase.None; busy = true;
            choiceOptions = null;

            players[0] = new PlayerState { index = 0, name = humanName, isHuman = true, avatar = humanAvatar };
            players[1] = new PlayerState { index = 1, name = raid != null ? raid.boss.name : story != null ? story.battle.oppName : mode == MatchMode.Practice ? GolemName : CpuName, isHuman = false, avatar = Profile.CpuAvatar };
            players[0].opponent = players[1];
            players[1].opponent = players[0];

            if (mode == MatchMode.Puzzle && puzzle != null) { yield return PuzzleRoutine(fieldData); yield break; }   // GameManager.Puzzle.cs

            var cpuHeroes = CardList.Heroes.Where(h => h != heroData && !StoryData.IsStoryHero(h)).ToList();
            SetupPlayer(players[0], heroData, humanDeck);
            if (raid != null) SetupRaidBoss(players[1]);
            else if (story != null) SetupStoryOpponent(players[1]);
            else if (mode == MatchMode.Practice) { cpuLevel = CpuLevel.Trainee; SetupPracticeGolem(players[1]); }
            else
            {
                SetupPlayer(players[1], cpuHeroes[rng.Next(cpuHeroes.Count)], CpuDeck());
                ApplyCpuLevel(players[1]);
            }
            ApplyEventSetup();

            hud.BeginMatch(this);
            if (raid != null) hud.BeginRaid(this);
            if (story != null) hud.BeginStory(this);
            RefreshAll();
            yield return Wait(0.6f);

            // 필드
            center = new CardInstance(fieldData, null) { zone = Zone.Center };
            Sfx.PlayFieldBgm(fieldData);
            board.CreateView(center, new Vector3(0, 3f, 0));
            Log($"필드 카드: {fieldData.name}");
            RefreshAll();
            yield return hud.Banner(fieldData.name, "FIELD");

            // 선후공
            if (raid != null)
            {
                first = Human;     // 레이드: 파티 선공
                yield return hud.Banner("RAID  " + raid.boss.name, $"{raid.partySize}인 파티 · 보스 HP {Cpu.maxHp} · 약점 {CardData.ElementName(raid.boss.weakness)}", 1.4f);
            }
            else if (story != null)
            {
                first = Human;     // 스토리: 항상 선공 (튜토리얼)
                yield return hud.Banner(story.title, story.battle.goal, 1.3f);
            }
            else
            {
                first = rng.Next(2) == 0 ? Human : Cpu;
                if (mode == MatchMode.Event) yield return hud.Banner("원소 이변전 · " + ElementEvent.Name(eventRule), ElementEvent.Desc(eventRule), 1.8f);
            }

            fieldFirst = null;
            if (raid == null) yield return FieldCoinToss();     // 필드 코인 토스 (GameManager.Field.cs)
            // 공격 턴: 필드 후공 쪽이 T1 공격 턴, 이후 T 마다 번갈아 (스토리·퍼즐은 정해진 순서)
            if (Simul && story == null && mode != MatchMode.Puzzle && fieldFirst != null)
            {
                first = fieldFirst.opponent;
                yield return hud.Banner(first == Human ? "T1 공격 턴" : "T1 수비 턴",
                    first == Human ? "홀수 T 에 내가 공격합니다 (짝수 T 는 수비)" : "홀수 T 에 상대가 공격합니다 (짝수 T 에 내가 공격)", 1.0f);
            }

            // 7장 드로우
            yield return Draw(first, StartHand, 0.07f);
            yield return Draw(first.opponent, StartHand, 0.07f);
            if (UsesFateRedraw) yield return FateRedraw();     // 운명 재배치 (GameManager.Fate.cs)
            busy = false;
            StoryCoach("start");

            while (!gameOver)
            {
                time++;
                if (Simul) { yield return SimulTimeRoutine(); continue; }   // 동시 룰 (GameManager.Simul.cs)
                active = (time % 2 == 1) ? first : first.opponent;
                yield return TimeRoutine();
            }
            yield return FinishMatch();
        }

        IEnumerator FinishMatch()
        {
            if (mode == MatchMode.Duel || mode == MatchMode.Event || mode == MatchMode.Arena) TimeSeal.Clear();   // 끝난 대전의 봉인은 지운다
            phase = Phase.None;
            busy = true;
            choiceOptions = null;
            HumanBattleOpen = false;
            RefreshAll();
            Sfx.StopBgm(0.5f);
            Sfx.Play(winner == Human ? "victory" : "defeat");
            if (mode != MatchMode.Puzzle && mode != MatchMode.Practice) hud.CpuStamp(winner == Human ? 5 : 1);
            yield return hud.Banner(winner == Human ? "VICTORY" : "DEFEAT", winner == Human ? "승리!" : surrendered ? "항복" : "패배...");
            hud.ShowGameOver(winner == Human);
            OnMatchFinished?.Invoke();
            MatchFinished = true;
        }

        bool surrendered;
        /// <summary>결과 처리(보상 기록)까지 끝났는가 — 결과 화면 버튼이 뜬 뒤</summary>
        public bool MatchFinished { get; private set; }
        public bool Surrendered => surrendered;

        /// <summary>항복: 진행 중인 타임(선택 대기 포함)을 즉시 멈추고 패배 처리</summary>
        public void Surrender()
        {
            if (gameOver || players[0] == null) return;
            StopAllCoroutines();
            surrendered = true;
            hud.ResetInteraction();
            Log($"{Human.name}: 항복");
            EndGame(Cpu);
            StartCoroutine(FinishMatch());
        }

        void SetupPlayer(PlayerState p, CardData heroData, List<CardData> deckList)
        {
            p.hero = new CardInstance(heroData, p) { zone = Zone.Hero };
            p.maxHp = p.hp = Simul ? Mathf.RoundToInt(heroData.heroHp * HeroHpRate) : heroData.heroHp;   // 동시 룰: 영웅 HP ×0.75 (2026-10-16 밸런스: 소환수 전투만으로도 끝나는 판)
            board.CreateView(p.hero, board.HeroPos(p) + Vector3.up * 2f);
            HeroModel.Attach(p.hero.view, heroData.id, p.index == 0);
            var cards = deckList != null && deckList.Count > 0 ? deckList.OrderBy(_ => rng.Next()).ToList() : CardList.BuildRandomDeck(rng);
            p.extra.Clear();
            SplitExtra(p, cards);     // 필드 → 엑스트라 덱 (GameManager.Field.cs)
            // 사람: 보유한 가공(StrikerZ → Seven)을 덱 카드에 배정 (표시 전용)
            var finishes = p.isHuman ? Profile.AssignFinishes(cards) : null;
            for (int i = 0; i < cards.Count; i++)
            {
                var d = cards[i];
                var ci = new CardInstance(d, p) { zone = Zone.Deck, finish = finishes != null ? finishes[i] : CardFinish.Normal };
                p.deck.Add(ci);
                board.CreateView(ci, board.DeckPos(p, p.deck.Count));
            }
        }

        /// <summary>패배한 쪽이 다음 필드를 결정 (CPU 가 졌으면 CPU 가 랜덤 선택)</summary>
        public bool LoserIsHuman => winner != null && winner != Human;

        public void EndGame(PlayerState win)
        {
            if (gameOver) return;
            gameOver = true;
            winner = win;
            Log($"게임 종료 — {win.name} 승리");
        }

        // ══════════════════════════ 타임 ══════════════════════════

        IEnumerator TimeRoutine()
        {
            var opp = active.opponent;
            ExpireStatuses(s => s.expire == Expire.CasterNextTime && s.caster == active && s.createdTime < time);

            phase = Phase.TimeStart;
            RegenMonsters();
            raidTime = active == Human ? (time + 1) / 2 : raidTime;
            RefreshAll();
            Sfx.Play("turn", 0.7f);
            yield return hud.Banner($"TIME {time}", raid != null ? (active == Human ? "파티의 타임" : "보스의 타임") : (active == Human ? "나의 타임" : "상대의 타임"));
            if (raid != null && active == Cpu) { yield return BossTimeStart(); if (gameOver) yield break; }
            if (story != null && active == Human) { yield return StoryTimeStart(); if (gameOver) yield break; }

            // 상대 타임 시작 전 t0
            yield return T0Window(Trigger.OpponentTimeStart, opp);
            if (gameOver) yield break;

            active.fieldPlayedThisTime = false;
            // 드로우: 패가 HandMax(7) 장이 될 때까지
            if (active.hand.Count < HandMax && !(raid != null && active == Cpu)) yield return Draw(active, HandMax - active.hand.Count);   // 레이드 보스는 덱 없이 스킬로
            if (gameOver) yield break;

            // 메인
            phase = Phase.Main;
            RefreshAll();
            if (HumanDecides(active))
            {
                StoryMainCoach();
                endMainRequested = battleRequested = false;
                while (!endMainRequested && !gameOver) yield return null;
                while (busy) yield return null;
            }
            else
            {
                busy = true;
                yield return raid != null && active == Cpu ? BossMainPhase() : ai.MainPhase(active);
                busy = false;
                battleRequested = true;
            }
            if (gameOver) yield break;

            // 배틀
            if (battleRequested)
            {
                phase = Phase.Battle;
                RefreshAll();
                yield return hud.Banner("BATTLE PHASE", HumanDecides(active) ? "몬스터를 드래그해 공격 · t/half 마법은 클릭" : "상대의 공격");
                busy = true;
                // 방어 측 t0 창 (상대 배틀 개시 시)
                yield return T0Window(Trigger.OpponentBattle, opp);
                if (gameOver) yield break;
                busy = true;
                yield return BattlePhase();
                busy = false;
                if (gameOver) yield break;
            }

            // 배틀 엔드 — 이후 t0 마법 외 행동 불가
            phase = Phase.BattleEnd;
            RefreshAll();
            busy = true;
            yield return T0Window(Trigger.BattleEnd, active);
            if (!gameOver) yield return T0Window(Trigger.BattleEnd, opp);
            ExpireStatuses(s => s.expire == Expire.NextBattleEnd);
            ClearWeaken();
            if (gameOver) yield break;
            // 레이드: 파티 타임 끝 = AI 팀원 행동, 보스 타임 끝 = 보스가 팀원 전장 공격
            if (raid != null) { yield return active == Human ? AlliesAct() : BossVsAllies(); if (gameOver) yield break; }
            // [재생] 배틀 엔드에 HP 전부 회복
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null && c.IsMonster && c.data.regen && c.damage > 0)
                    {
                        c.damage = 0;
                        Log($"{c.Name}: [재생] HP 모두 회복 ({MaxHp(c)})");
                        if (c.view != null) hud.FloatText(c.view.transform.position, "재생", new Color(0.5f, 1f, 0.6f));
                    }
            RefreshAll();

            phase = Phase.End;
            // 엔드 페이즈: 패를 모두 버리거나 그대로 두거나 (둘 중 하나만)
            RefreshAll();
            if (active == Human) StoryCoach("end");
            yield return EndPhaseDiscard(active);
            if (gameOver) yield break;
            ExpireStatuses(s => s.expire == Expire.EndOfTime);
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null) { c.tempAtk = 0; c.priorityMod = 0; }
            extraAttacks.Clear();
            busy = false;
            RefreshAll();
            yield return Wait(0.3f);
        }

        /// <summary>엔드 페이즈 패 정리: 모두 버리기(묘지로, 다음 타임 7장 새로 드로우) / 그대로 두기</summary>
        IEnumerator EndPhaseDiscard(PlayerState p)
        {
            int n = p.hand.Count;
            if (n == 0) yield break;
            if (Simul) { yield return TrimHand(p); yield break; }   // 동시 룰: 모두 버리기 선택 없음 — 7장 넘는 만큼만 버린다
            int pick = 1;
            yield return ChooseOption(p, $"엔드 페이즈 — 패 {n}장을 어떻게 할까요?" + (Simul && n > EndHandMax ? $"  (그대로 두면 {EndHandMax}장이 넘는 {n - EndHandMax}장을 골라 버립니다)" : ""),
                new[] { $"패 {n}장 모두 버리기", "그대로 두기" }, () => ai.WantsDiscardHand(p) ? 0 : 1, r => pick = r);
            if (pick == 0) { yield return DiscardHand(p); yield break; }
            if (Simul) yield return TrimHand(p);
        }

        /// <summary>동시 룰 엔드 타임: 패가 7장을 넘으면 넘는 만큼 골라서 버린다 (9장이면 2장)</summary>
        IEnumerator TrimHand(PlayerState p)
        {
            int over = p.hand.Count - EndHandMax;
            if (over <= 0) yield break;
            // 사람: 버릴 카드를 한 화면에서 한 번에 고른다 (30초, 시간이 지나면 남은 장수는 아래 AI 기준으로)
            if (HumanDecides(p))
            {
                List<CardInstance> picked = null;
                hud.ShowDiscard(p.hand.ToList(), over, PlanSeconds, r => picked = r);
                while (picked == null && !gameOver) yield return null;
                foreach (var c in picked ?? new List<CardInstance>())
                {
                    if (p.hand.Count <= EndHandMax || !p.hand.Contains(c)) continue;
                    p.hand.Remove(c);
                    c.zone = Zone.Grave;
                    c.faceDown = false;
                    p.grave.Add(c);
                    Log($"{p.name}: 패가 {EndHandMax}장을 넘어 [{c.Name}] 버림");
                }
                Sfx.Play("flip", 0.6f);
                RefreshAll();
                over = p.hand.Count - EndHandMax;   // 시간 초과로 덜 골랐으면 나머지는 아래에서 자동
            }
            for (int i = 0; i < over && p.hand.Count > EndHandMax && !gameOver; i++)
            {
                CardInstance pick = null;
                // AI (또는 시간 초과로 덜 고른 사람): 곧 낼 수 없는 카드 → 약한 카드부터
                pick = p.hand.OrderByDescending(c => (c.IsMonster ? Mathf.Max(0, c.data.minTime - time) * 3f : 0f) - (c.data.atk + c.data.hp) * 0.2f - (int)c.data.rarity * 2f).FirstOrDefault();
                yield return null;
                if (pick == null || !p.hand.Contains(pick)) pick = p.hand[p.hand.Count - 1];
                p.hand.Remove(pick);
                pick.zone = Zone.Grave;
                pick.faceDown = false;
                p.grave.Add(pick);
                Sfx.Play("flip", 0.6f);
                Log($"{p.name}: 패가 {EndHandMax}장을 넘어 [{(p.isHuman ? pick.Name : "카드 1장")}] 버림");
                RefreshAll();
                yield return Wait(0.15f);
            }
        }

        /// <summary>패를 전부 묘지로</summary>
        public IEnumerator DiscardHand(PlayerState p)
        {
            var cards = p.hand.ToList();
            if (cards.Count == 0) yield break;
            foreach (var c in cards)
            {
                p.hand.Remove(c);
                c.zone = Zone.Grave;
                c.faceDown = false;
                p.grave.Add(c);
                Sfx.Play("flip", 0.6f);
                RefreshAll();
                yield return Wait(0.08f);
            }
            Log($"{p.name}: 패 {cards.Count}장을 모두 버렸다");
            if (p.isHuman) hud.FloatText(board.GravePos(p, p.grave.Count), $"패 {cards.Count}장 버림", new Color(0.8f, 0.75f, 1f));
            yield return Wait(0.3f);
        }

        // UI 버튼
        public void RequestBattle() { if (IsHumanMain) { battleRequested = true; endMainRequested = true; } }
        public void RequestEndTime() { if (IsHumanMain) { battleRequested = false; endMainRequested = true; } }

        // ══════════════════════════ 배틀 ══════════════════════════

        // 사람: 공격할 몬스터를 대상(상대 몬스터 / 몬스터가 없으면 상대 영웅)에게 드래그앤드롭, t/half 마법은 클릭해서 발동, [배틀 종료] 로 끝낸다.
        // AI(자동 테스트 포함): 칸 순서대로 공격·발동하고 대상은 점수로 고른다.
        readonly Dictionary<int, int> attacksUsed = new Dictionary<int, int>();
        CardInstance pendingAttacker, pendingTarget, pendingMagic;
        bool battleEndRequested;
        /// <summary>사람의 배틀 입력(드래그 공격 · t/half 클릭)을 받는 중</summary>
        public bool HumanBattleOpen { get; private set; }

        /// <summary>이번 배틀페이즈에 남은 공격 횟수 (기본 1, [연속 공격]·추가 공격 효과 반영)</summary>
        public int AttacksLeft(CardInstance a)
        {
            if (a == null || !a.IsMonster) return 0;
            extraAttacks.TryGetValue(a.uid, out int extra);
            attacksUsed.TryGetValue(a.uid, out int used);
            if (Simul) return 1 - used;                    // 동시 룰: 연속 공격·추가 공격 폐지
            return Mathf.Max(1, a.data.attackCount + extra) - used;
        }

        public bool CanAttackNow(CardInstance a)
            => phase == Phase.Battle && a != null && a.owner == active && a.zone == Zone.Slot && a.IsMonster
               && !a.faceDown && a.stunUntil < time && AttacksLeft(a) > 0;

        /// <summary>공격 가능한 대상: 상대 몬스터가 없으면 상대 영웅(직접 공격), 앞면 [도발] 이 있으면 도발 몬스터만 (자유 대상 효과 중엔 전부)</summary>
        public List<CardInstance> AttackTargets(CardInstance a)
        {
            var opp = a.owner.opponent;
            var mons = opp.Monsters();
            if (mons.Count == 0) return opp.hero != null ? new List<CardInstance> { opp.hero } : new List<CardInstance>();
            if (HasStatus(StatusKind.FreeTarget, a.owner)) return mons;
            var taunts = mons.Where(IsTaunt).ToList();
            return taunts.Count > 0 ? taunts : mons;
        }

        public bool CanActivateInBattle(CardInstance m)
            => phase == Phase.Battle && m != null && m.owner == active && m.zone == Zone.Slot && m.IsMagic
               && m.data.speed == MagicSpeed.THalf && (m.data.canActivate == null || m.data.canActivate(this, m));

        bool AnyBattleAction(PlayerState p)
            => p.slots.Any(c => c != null && ((CanAttackNow(c) && AttackTargets(c).Count > 0) || CanActivateInBattle(c)));

        /// <summary>HUD 드래그앤드롭: a 로 t 를 공격</summary>
        public bool TryHumanAttack(CardInstance a, CardInstance t)
        {
            if (!HumanBattleOpen || pendingAttacker != null || pendingMagic != null) return false;
            if (!CanAttackNow(a) || t == null || !AttackTargets(a).Contains(t)) return false;
            pendingAttacker = a; pendingTarget = t;
            return true;
        }

        /// <summary>HUD 클릭: 세트한 t/half 마법 발동</summary>
        public bool TryHumanMagic(CardInstance m)
        {
            if (!HumanBattleOpen || pendingAttacker != null || pendingMagic != null || !CanActivateInBattle(m)) return false;
            pendingMagic = m;
            return true;
        }

        public void RequestEndBattle() { if (HumanBattleOpen) battleEndRequested = true; }

        IEnumerator BattlePhase()
        {
            attacksUsed.Clear();
            if (HumanDecides(active)) yield return HumanBattle();
            else yield return AutoBattle();
        }

        /// <summary>시나리오 테스트용: 사람 쪽 배틀페이즈(드래그 공격 입력 대기)를 실행</summary>
        public IEnumerator TestHumanBattle()
        {
            var prevPhase = phase; var prevActive = active;
            phase = Phase.Battle; active = Human;
            attacksUsed.Clear();
            yield return HumanBattle();
            phase = prevPhase; active = prevActive;
            RefreshAll();
        }

        IEnumerator HumanBattle()
        {
            battleEndRequested = false;
            StoryCoach("battle");
            while (!gameOver && !battleEndRequested && AnyBattleAction(active))
            {
                pendingAttacker = pendingTarget = pendingMagic = null;
                HumanBattleOpen = true;
                RefreshAll();
                hud.ShowBattleMode();
                while (!gameOver && !battleEndRequested && pendingAttacker == null && pendingMagic == null) yield return null;
                HumanBattleOpen = false;
                hud.HideBattleMode();
                RefreshAll();
                if (pendingMagic != null) { board.Pulse(pendingMagic); yield return ActivateMagic(pendingMagic); }
                else if (pendingAttacker != null) yield return AttackWith(pendingAttacker, pendingTarget);
                RefreshAll();
            }
            HumanBattleOpen = false;
            pendingAttacker = pendingTarget = pendingMagic = null;
            hud.HideBattleMode();
        }

        IEnumerator AutoBattle()
        {
            var done = new HashSet<int>();
            while (!gameOver)
            {
                // 칸 순서대로 (AI 진행 순서)
                CardInstance next = null;
                for (int i = 0; i < PlayerState.SlotCount; i++)
                {
                    var c = active.slots[i];
                    if (c == null || done.Contains(c.uid)) continue;
                    next = c;
                    break;
                }
                if (next == null) break;
                done.Add(next.uid);

                board.Pulse(next);
                if (next.IsMonster) yield return MonsterAttacks(next);
                else if (next.IsMagic && next.data.speed == MagicSpeed.THalf) yield return OfferActivation(next);
            }
        }

        public void GrantExtraAttacks(CardInstance c, int n)
        {
            extraAttacks.TryGetValue(c.uid, out int cur);
            extraAttacks[c.uid] = cur + n;
            if (fx != null && n > 0) fx.Haste(c);
            if (c.view != null && n > 0) hud.FloatText(c.view.transform.position, $"공격 +{n}", new Color(0.5f, 1f, 0.6f));
        }

        /// <summary>시나리오 테스트용: 몬스터 1장의 공격 처리 실행 (남은 공격 횟수만큼, 대상은 AI 점수)</summary>
        public IEnumerator TestAttack(CardInstance a) { attacksUsed.Remove(a.uid); yield return MonsterAttacks(a); }

        /// <summary>AI: 남은 공격 횟수만큼 대상을 점수로 골라 공격</summary>
        IEnumerator MonsterAttacks(CardInstance a)
        {
            if (a.faceDown) { Log($"{a.Name}: 뒷면 표시 — 공격 불가"); yield break; }
            if (a.stunUntil >= time) { Log($"{a.Name}: 행동 불능 — 공격하지 못한다"); yield break; }
            var opp = a.owner.opponent;
            while (!gameOver && a.zone == Zone.Slot && AttacksLeft(a) > 0)
            {
                var targets = AttackTargets(a);
                if (targets.Count == 0) yield break;
                CardInstance t = null;
                yield return ChooseCard(a.owner, targets, $"{a.Name}의 공격 대상",
                    x => x.zone == Zone.Hero ? DirectAttackScore(a, opp) : ai.AttackScore(a, x), r => t = r);
                if (t == null) yield break;
                yield return AttackWith(a, t);
            }
        }

        /// <summary>공격 1회 (공격 횟수 1 소모): 상대 영웅이면 직접 공격, 몬스터면 전투 후 [연쇄] 로 이어서 공격 가능</summary>
        IEnumerator AttackWith(CardInstance a, CardInstance target)
        {
            var opp = a.owner.opponent;
            attacksUsed.TryGetValue(a.uid, out int used);
            attacksUsed[a.uid] = used + 1;
            int total = used + 1 + AttacksLeft(a);
            string label = total > 1 ? $" ({used + 1}/{total})" : "";
            board.Pulse(a);
            if (hud != null) yield return hud.AttackArrow(a, target);
            if (target.zone == Zone.Hero)
            {
                string counter = a.data.noCounter ? "반격 무시" : $"반격 ATK {HeroAtk(opp)}";
                Log($"{a.Name} → 상대 영웅 직접 공격{label} ({counter})");
                yield return DirectAttack(a);
                yield break;
            }
            Log($"{a.Name} → {TargetName(target)} 공격{label}");
            yield return Battle(a, target);
            // [연쇄] 전투로 대상을 파괴하고 살아남았다면 다음 대상을 이어서 공격할 수 있다
            int chain = 1;
            while (!gameOver && !Simul && a.data.attackAll && target.zone != Zone.Slot && a.zone == Zone.Slot && opp.Monsters().Count > 0)
            {
                chain++;
                var opts = AttackTargets(a).Where(t => t.zone == Zone.Slot).ToList();
                if (opts.Count == 0) break;
                Log($"{a.Name}: [연쇄] 이어서 공격 가능");
                CardInstance next = null;
                yield return ChooseAttackTarget(a, opts, $"{a.Name} [연쇄 {chain}] 이어서 공격할 대상", t => ai.AttackScore(a, t), r => next = r, aiCancelBelow: -4f);
                if (next == null) { Log($"{a.Name}: 연쇄 공격하지 않음"); break; }
                if (hud != null) yield return hud.AttackArrow(a, next);
                Log($"{a.Name} → {TargetName(next)} 연쇄 공격");
                yield return Battle(a, next);
                target = next;
            }
        }

        public bool IsTaunt(CardInstance c) => c != null && c.IsMonster && !c.faceDown && (c.data.taunt || c.tauntUntil >= time);

        /// <summary>강제 공격 대상: 앞면 [도발] 몬스터 중 가장 왼쪽, 없으면 가장 왼쪽 몬스터</summary>
        public CardInstance ForcedTarget(PlayerState defender)
        {
            CardInstance first = null;
            for (int i = 0; i < PlayerState.SlotCount; i++)
            {
                var c = defender.slots[i];
                if (c == null || !c.IsMonster) continue;
                if (IsTaunt(c)) return c;
                if (first == null) first = c;
            }
            return first;
        }

        string TargetName(CardInstance t) => HumanCanSee(t) ? t.Name + (IsTaunt(t) ? " [도발]" : "") : $"{t.slot + 1}번 칸 뒷면 몬스터";

        /// <summary>AI: 직접 공격 가치 (치명타면 무조건, 반격으로 파괴되면 회피)</summary>
        float DirectAttackScore(CardInstance a, PlayerState op)
        {
            int atk = EffectiveAtk(a);
            int counter = op.hero != null && !a.data.noCounter ? HeroAtk(op) : 0;
            if (atk >= op.hp) return 100f;
            return atk - counter > 0 ? 3f + atk * 0.3f : -2f;
        }

        /// <summary>
        /// 영웅의 공격 연출: 원거리 영웅(활·지팡이·마법)은 제자리에서 공격 모션 → 발사 순간에 발사체가 날아가 맞는다,
        /// 근접 영웅은 예전처럼 카드째 돌진. 맞는 순간에 끝난다.
        /// </summary>
        IEnumerator HeroStrike(CardInstance hero, Vector3 target)
        {
            if (hero == null) yield break;
            var hm = HeroModel.Of(hero);
            var kind = Projectile.KindOf(hero.data);
            if (hm != null && kind != Projectile.Kind.None)
            {
                hm.Attack(target);
                yield return hm.WaitRelease(Projectile.ReleaseAt(kind));
                Vector3 from = hm.LaunchPoint(target);
                Sfx.Play(kind == Projectile.Kind.Arrow ? "flip" : "attack", 0.6f);
                bool hit = false;
                StartCoroutine(Projectile.Fly(hero.data, kind, from, target + Vector3.up * 0.25f, () => hit = true));
                while (!hit && !gameOver) yield return null;
                Sfx.Play("attack");
                board.Burst(target + Vector3.up * 0.1f, kind == Projectile.Kind.Arrow ? new Color(0.6f, 1f, 0.7f) : CardData.ElementColor(hero.data.element), 1.6f);
                board.Shake(0.1f);
                yield break;
            }
            if (hm != null) { hm.Attack(target); yield return Wait(0.3f); }
            if (hero.view != null) yield return board.AttackLunge(hero, target);
        }

        /// <summary>테스트용: 내 영웅이 상대 영웅을 향해 공격 연출만</summary>
        public void TestHeroStrike() => StartCoroutine(HeroStrike(Human.hero, board.HeroPos(Cpu)));

        /// <summary>직접 공격을 받은 영웅의 반격: 공격 몬스터에게 영웅 ATK 만큼 피해 (HP 0 이면 파괴). battleHp = 이번 전투 HP 증감</summary>
        IEnumerator HeroCounter(CardInstance a, PlayerState op, int battleHp = 0)
        {
            if (gameOver || op.hero == null || a.zone != Zone.Slot) yield break;
            if (a.data.noCounter) { Log($"{a.Name}: [암살] 영웅의 반격을 받지 않는다"); yield break; }
            int dmg = HeroAtk(op);
            if (dmg <= 0) yield break;
            Voice.Hero(op.hero.data, "atk");
            if (a.view != null) yield return HeroStrike(op.hero, a.view.transform.position);
            var hit = PreviewHit(a, dmg, battleHp, false);
            a.damage += hit.lost;
            Sfx.Play("hit", 0.8f);
            if (a.view != null) hud.FloatText(a.view.transform.position, $"반격 -{dmg}", new Color(1f, 0.45f, 0.3f));
            Log($"영웅 {op.hero.Name} 반격! {a.Name}에게 {dmg} 데미지 → HP {(hit.dies ? 0 : CurHp(a))}/{MaxHp(a)}");
            RefreshAll();
            yield return Wait(0.35f);
            if (hit.dies) yield return DestroyCard(a, $"영웅 {op.hero.Name} 반격");
        }

        /// <summary>빛/어둠 속성 몬스터의 전투 코인: 빛 앞면 = +CoinPower, 어둠 뒷면 = -CoinPower (ATK·HP 모두)</summary>
        IEnumerator ElementCoin(CardInstance m, int side, Action<int> result)
        {
            int p = m.data.CoinPower;
            if (p <= 0 || NoDice) { result(0); yield break; }
            bool heads = rng.Next(2) == 0;
            dice.humanSide = m.owner.isHuman ? side : 1 - side;
            yield return dice.FlipCoin(side, heads);
            int mod = m.data.element == Element.Light ? (heads ? p : 0) : (heads ? 0 : -p);
            string rule = m.data.element == Element.Light ? "빛의 가호" : "어둠의 대가";
            Log($"{m.Name} [{rule}] 코인 {(heads ? "앞면" : "뒷면")} → {(mod == 0 ? "변화 없음" : $"이번 전투 ATK·HP {(mod > 0 ? "+" : "")}{mod}")}");
            if (fx != null && mod != 0) fx.AtkChange(m, mod);
            if (mod != 0 && m.view != null)
                hud.FloatText(m.view.transform.position, $"{(mod > 0 ? "+" : "")}{mod}/{(mod > 0 ? "+" : "")}{mod}", mod > 0 ? new Color(1f, 0.95f, 0.55f) : new Color(0.75f, 0.5f, 1f));
            yield return Wait(0.35f);
            result(mod);
        }

        IEnumerator DirectAttack(CardInstance a)
        {
            var op = a.owner.opponent;
            bool coin = false;
            if (!NoDice) yield return ChooseRollType(a, r => coin = r);
            yield return board.AttackLunge(a, board.HeroPos(op));
            int ca = 0;
            yield return ElementCoin(a, 0, r => ca = r);

            int[] ra = { 1 };
            int ia = 0;
            if (!NoDice)
            {
                dice.humanSide = a.owner.isHuman ? 0 : 1;
                yield return dice.RollBattle(Rolls(a), 0, coin, false, false, (x, y) => ra = x);
                yield return ChooseDie(a, ra, coin, e => e.atk + e.pierce * 0.75f + e.burn * 0.8f + e.draw * 1.5f + e.heal * 0.6f + e.hp * 0.4f, r => ia = r);
                dice.Highlight(0, ia);
            }
            var ea = NoDice ? Dice.None() : a.data.DiceFor(ra[ia]);
            int dmg = Mathf.Max(0, EffectiveAtk(a) + ca + ea.atk) + ea.pierce;
            if (Simul) dmg = Mathf.CeilToInt(dmg * DirectMul);   // 동시 룰: 직접 공격은 절반 (한쪽이 전멸하면 한 T 에 끝나는 것 방지)
            dmg = RaidBossDamage(a, op, dmg);   // 레이드: 약점 속성 2배 · 핵심 카드
            Log($"{a.Name} 직접 공격! [{Dice.BandNames[Dice.Band(ra[ia])]}] {ea.Describe()} → {dmg} 데미지");
            yield return hud.ShowClash(new HUD.ClashSide { card = a, baseAtk = EffectiveAtk(a) + ca, hp = CurHp(a) + ca + ea.hp, die = ra[ia], coin = coin, eff = ea, power = dmg }, null);
            dice.Clear();

            yield return ApplyDiceInstant(a, ea);
            if (!gameOver) yield return DamagePlayer(op, dmg, a.Name);
            if (!gameOver) yield return HeroCounter(a, op, ca + ea.hp);
        }

        /// <summary>
        /// 몬스터 전투: 양쪽이 동시에 서로의 ATK 만큼 피해를 준다. HP 가 0 이 되면 파괴되고,
        /// 남은 HP 를 넘긴 초과 피해의 절반(+관통)은 그 몬스터 주인 영웅에게 들어간다.
        /// </summary>
        IEnumerator Battle(CardInstance a, CardInstance d)
        {
            if (d.faceDown)
            {
                if (Simul) yield return Reveal(d);
                else
                {
                    d.faceDown = false;
                    Log($"{d.Name} 이(가) 앞면이 되었다");
                    RefreshAll();
                    yield return Wait(0.5f);
                }
                if (gameOver || d.zone != Zone.Slot || a.zone != Zone.Slot) yield break;   // 공개 효과로 어느 한쪽이 사라졌다
            }

            bool coinA = false, coinD = false;
            if (!NoDice)
            {
                yield return ChooseRollType(a, r => coinA = r);
                yield return ChooseRollType(d, r => coinD = r);
            }

            yield return board.AttackLunge(a, d.view.transform.position);

            int ca = 0, cd = 0;
            yield return ElementCoin(a, 0, r => ca = r);
            yield return ElementCoin(d, 1, r => cd = r);

            int[] ra = { 1 }, rd = { 1 };
            int ia = 0, id = 0;
            int atkA = EffectiveAtk(a) + ca, atkD = EffectiveAtk(d) + cd;
            int hpA = CurHp(a) + ca, hpD = CurHp(d) + cd;
            DiceEffect ea, ed;
            if (NoDice) { ea = Dice.None(); ed = Dice.None(); }    // 마법진 퍼즐: 주사위 효과 없음
            else
            {
                dice.humanSide = a.owner.isHuman ? 0 : 1;
                yield return dice.RollBattle(Rolls(a), Rolls(d), coinA, coinD, true, (x, y) => { ra = x; rd = y; });
                yield return ChooseDie(a, ra, coinA, e => BattleValue(e, atkA, hpA, atkD, hpD, null), r => ia = r);
                dice.Highlight(0, ia);
                ea = a.data.DiceFor(ra[ia]);
                var eaSeen = ea;
                yield return ChooseDie(d, rd, coinD, e => BattleValue(e, atkD, hpD, atkA, hpA, eaSeen), r => id = r);   // 방어측은 공격측 선택을 보고 고른다
                dice.Highlight(1, id);
                ed = d.data.DiceFor(rd[id]);
            }
            int pa = Mathf.Max(0, atkA + ea.atk - ed.enemyAtk);
            int pd = Mathf.Max(0, atkD + ed.atk - ea.enemyAtk);
            var hitD = PreviewHit(d, pa, cd + ed.hp, ed.guard);
            var hitA = PreviewHit(a, pd, ca + ea.hp, ea.guard);
            Log($"{a.Name} [{Dice.BandNames[Dice.Band(ra[ia])]}] {ea.Describe()} → {pa}  vs  {d.Name} [{Dice.BandNames[Dice.Band(rd[id])]}] {ed.Describe()} → {pd}");
            yield return hud.ShowClash(
                new HUD.ClashSide { card = a, baseAtk = atkA, hp = hpA + ea.hp, die = ra[ia], coin = coinA, eff = ea, power = pa, dies = hitA.dies },
                new HUD.ClashSide { card = d, baseAtk = atkD, hp = hpD + ed.hp, die = rd[id], coin = coinD, eff = ed, power = pd, dies = hitD.dies });
            dice.Clear();

            yield return ApplyDiceInstant(a, ea);
            if (gameOver) yield break;
            yield return ApplyDiceInstant(d, ed);
            if (gameOver) yield break;

            // 주사위 "상대 ATK -n": 이 전투 뒤에도 이번 T 배틀 엔드까지 남는다 (이 전투엔 이미 반영됨)
            if (ea.enemyAtk > 0 && !(Resist(d) && a.owner != d.owner)) { d.weaken += ea.enemyAtk; Log($"{d.Name}: 배틀 엔드까지 ATK -{ea.enemyAtk} ({a.Name} 주사위)"); }
            if (ed.enemyAtk > 0 && !(Resist(a) && a.owner != d.owner)) { a.weaken += ed.enemyAtk; Log($"{a.Name}: 배틀 엔드까지 ATK -{ed.enemyAtk} ({d.Name} 주사위)"); }

            // 동시 피해
            d.damage += hitD.lost;
            a.damage += hitA.lost;
            if (pa > 0 && ea.wound && !hitD.dies) d.noRegenUntil = time + 1;
            if (pd > 0 && ed.wound && !hitA.dies) a.noRegenUntil = time + 1;
            if (pa > 0 && d.view != null) hud.FloatText(d.view.transform.position, $"-{pa}", new Color(1f, 0.35f, 0.3f));
            if (pd > 0 && a.view != null) hud.FloatText(a.view.transform.position, $"-{pd}", new Color(1f, 0.35f, 0.3f));
            if (pa > 0 || pd > 0) Sfx.Play("hit", 0.8f);
            RefreshAll();
            if (ed.guard && pa >= hpD + ed.hp && pa > 0) { Log($"{d.Name}: 주사위 효과로 파괴되지 않았다 (HP 1)"); Sfx.Play("guard"); if (fx != null) fx.Guard(d); }
            if (ea.guard && pd >= hpA + ea.hp && pd > 0) { Log($"{a.Name}: 주사위 효과로 파괴되지 않았다 (HP 1)"); Sfx.Play("guard"); if (fx != null) fx.Guard(a); }
            if (fx != null && pa > 0 && ea.wound && !hitD.dies) fx.Wound(d);
            if (fx != null && pd > 0 && ed.wound && !hitA.dies) fx.Wound(a);
            if (fx != null && ea.hp > 0) fx.HpUp(a);
            if (fx != null && ed.hp > 0) fx.HpUp(d);
            if (pa > 0 && ea.wound && !hitD.dies) Log($"{d.Name}: [상처] 다음 타임 HP 가 회복되지 않는다");
            if (pd > 0 && ed.wound && !hitA.dies) Log($"{a.Name}: [상처] 다음 타임 HP 가 회복되지 않는다");
            yield return Wait(0.35f);

            if (hitD.dies) yield return DestroyCard(d, a.Name);
            if (hitA.dies) yield return DestroyCard(a, d.Name);
            if (gameOver) yield break;
            int overD = Overflow(hitD.overkill) + ea.pierce, overA = Overflow(hitA.overkill) + ed.pierce;
            if (hitD.dies) overD = RaidBossDamage(a, d.owner, overD);
            if (hitD.dies && overD > 0) yield return DamagePlayer(d.owner, overD, "전투 초과 피해");
            if (gameOver) yield break;
            if (hitA.dies && overA > 0) yield return DamagePlayer(a.owner, overA, "전투 초과 피해");
            if (!hitD.dies && !hitA.dies)
                Log($"양쪽 모두 생존 — {a.Name} HP {CurHp(a)}/{MaxHp(a)} · {d.Name} HP {CurHp(d)}/{MaxHp(d)}");
        }

        /// <summary>몬스터를 쓰러뜨리고 남은 초과 피해 중 영웅에게 넘어가는 양 (절반, 올림)</summary>
        public const float OverflowRate = 0.5f;
        /// <summary>동시 룰 직접 공격 피해 배율</summary>
        public const float DirectRate = 0.5f;
        /// <summary>동시 룰 영웅 HP 배율</summary>
        public const float HeroHpRate = 0.75f;
        /// <summary>동시 룰: 한 T 에 영웅이 받는 피해 상한 (영웅 대결 제외 — 몰리는 쪽도 최소 몇 T 는 버틴다)</summary>
        public const int DirectCapPerTime = 15;
        readonly Dictionary<int, int> directTaken = new Dictionary<int, int>();
        public void ResetDirectCap() => directTaken.Clear();
        static int Overflow(int overkill) => overkill <= 0 ? 0 : Mathf.CeilToInt(overkill * OverflowRate);

        /// <summary>회복·드로우·상대 영웅 데미지 같은 즉시 효과</summary>
        IEnumerator ApplyDiceInstant(CardInstance m, DiceEffect e)
        {
            var p = m.owner;
            var prev = fxSource;
            fxSource = m;
            if (e.heal > 0) yield return Heal(p, e.heal);
            if (e.draw > 0) yield return Draw(p, e.draw);
            if (e.burn > 0) yield return DamagePlayer(p.opponent, e.burn, m.Name + " (주사위)");
            fxSource = prev;
        }

        /// <summary>AI 판단: 상대를 쓰러뜨리면서 살아남는 선택 > 상대를 쓰러뜨리는 선택 > 살아남는 선택 순</summary>
        static float BattleValue(DiceEffect e, int myAtk, int myHp, int foeAtk, int foeHp, DiceEffect known)
        {
            int myPow = myAtk + e.atk - (known?.enemyAtk ?? 0);
            int foePow = foeAtk + (known?.atk ?? 0) - e.enemyAtk;
            int myBattleHp = myHp + e.hp, foeBattleHp = foeHp + (known?.hp ?? 0);
            bool kill = myPow >= foeBattleHp && !(known?.guard ?? false);
            bool die = foePow >= myBattleHp && !e.guard;
            float s = e.draw * 1.5f + e.heal * 0.6f + e.burn * 0.8f + (e.wound ? 0.6f : 0);
            if (kill) s += 6f + e.pierce * 0.75f + (myPow - foeBattleHp) * 0.3f;
            else s += Mathf.Max(0, myPow) * 0.2f;
            if (!die) s += 4f + Mathf.Max(0, myBattleHp - foePow) * 0.1f;
            return s;
        }

        /// <summary>3개(바람 4개) 중 1개 선택. 사람은 버튼, AI 는 aiValue 가 가장 높은 것</summary>
        IEnumerator ChooseDie(CardInstance m, int[] values, bool coin, Func<DiceEffect, float> aiValue, Action<int> result)
        {
            var labels = new string[values.Length];
            int best = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var e = m.data.DiceFor(values[i]);
                string face = coin ? (values[i] >= 5 ? "앞면" : "뒷면") : $"주사위 {values[i]}";
                labels[i] = $"{i + 1}번째 · {face}  [{Dice.BandNames[Dice.Band(values[i])]}]  {e.Describe()}";
                if (aiValue(e) > aiValue(m.data.DiceFor(values[best]))) best = i;
            }
            int pick = best;
            if (HumanDecides(m.owner))
            {
                // 1·2 / 3·4 / 5·6 세 구간을 항상 표시하고, 나온 눈이 속한 구간만 활성화
                var bandText = new string[3];
                var active = new bool[3];
                var rolled = new string[3];
                for (int b = 0; b < 3; b++)
                {
                    var faces = values.Where(v => Dice.Band(v) == b).ToList();
                    active[b] = faces.Count > 0;
                    bandText[b] = m.data.DiceFor(b * 2 + 1).Describe();
                    rolled[b] = faces.Count == 0 ? "" : coin ? string.Join(" ", faces.Select(v => v >= 5 ? "앞면" : "뒷면")) : string.Join(", ", faces);
                }
                bool done = false; int band = 0;
                hud.ShowBandChoice($"{m.Name} — 효과를 선택하세요", bandText, active, rolled, coin, r => { band = r; done = true; });
                bool ok = true;
                yield return WaitChoice(() => done, r => ok = r);
                if (!ok) { band = Dice.Band(values[best]); Log($"{m.Name}: 선택 시간 초과 — 자동 선택"); }
                hud.HideChoice();
                pick = Array.FindIndex(values, v => Dice.Band(v) == band);
                if (pick < 0) pick = best;
            }
            result(pick);
        }

        /// <summary>판정 방식: 모든 카드가 주사위 (고레어 코인 토스 선택은 2026-10-16 폐지)</summary>
        IEnumerator ChooseRollType(CardInstance m, Action<bool> result)
        {
            // 2026-10-16: 고레어도 코인 토스 없이 주사위로 통일 (판정 방식 선택 폐지 · 빛/어둠 속성 코인은 그대로)
            result(false);
            yield break;
        }

        IEnumerator OfferActivation(CardInstance magic)
        {
            if (magic.data.canActivate != null && !magic.data.canActivate(this, magic)) yield break;
            int pick = 0;
            yield return ChooseOption(magic.owner, $"t/half 마법 [{magic.Name}] 발동?", new[] { "발동", "보류" },
                () => ai.WantsMagic(magic) ? 0 : 1, r => pick = r);
            if (pick == 0) yield return ActivateMagic(magic);
        }

        // ══════════════════════════ t0 창 ══════════════════════════

        public List<CardInstance> EligibleT0(PlayerState p, Trigger trig)
        {
            var list = new List<CardInstance>();
            IEnumerable<CardInstance> src = p.hand.Concat(p.slots.Where(s => s != null));
            foreach (var c in src)
            {
                if (!c.IsMagic || c.data.speed != MagicSpeed.T0 || c.data.trigger != trig) continue;
                if (c.data.canActivate != null && !c.data.canActivate(this, c)) continue;
                list.Add(c);
            }
            return list;
        }

        IEnumerator T0Window(Trigger trig, PlayerState p)
        {
            bool prevBusy = busy;
            busy = true;
            var used = new HashSet<int>();
            while (!gameOver)
            {
                var options = EligibleT0(p, trig).Where(c => !used.Contains(c.uid)).ToList();
                if (options.Count == 0) break;
                CardInstance pick = null;
                if (HumanDecides(p))
                    yield return ChooseFromList(p, options, $"t0 마법 발동 타이밍 — {CardData.TriggerName(trig)}", null, r => pick = r, allowCancel: true, cancelLabel: "패스");
                else
                    pick = ai.PickT0(p, options, trig);
                if (pick == null) break;
                used.Add(pick.uid);
                yield return ActivateMagic(pick);
            }
            busy = prevBusy;
        }

        // ══════════════════════════ 행동 ══════════════════════════

        public bool CanSummon(CardInstance c, out string why)
        {
            why = null;
            if (!c.IsMonster || c.zone != Zone.Hand) { why = "몬스터 카드가 아닙니다"; return false; }
            if (phase != Phase.Main || active != c.owner) { why = "자신의 메인 페이즈에만 소환할 수 있습니다"; return false; }
            if (c.owner.FreeSlot() < 0) { why = "빈 칸이 없습니다 (7칸)"; return false; }
            if (time < c.data.minTime) { why = $"T{c.data.minTime} 부터 소환 가능합니다"; return false; }
            if (c.data.maxTime > 0 && time > c.data.maxTime) { why = $"T{c.data.maxTime} 까지만 소환 가능합니다"; return false; }
            if (StorySummonBlocked(c)) { why = "이번 타임 소환 한도"; return false; }
            return true;
        }

        public bool MustSummonFaceDown => HasStatus(StatusKind.FaceDownSummonOnly, null);

        public bool CanSummonFaceDown(CardInstance c)
            => Simul || c.data.canSummonFaceDown || MustSummonFaceDown || HasStatus(StatusKind.AllowFaceDown, c.owner)
               || (center != null && center.data.fieldAllowsFaceDown);

        public bool CanSummonFaceUp(CardInstance c) => !Simul && !MustSummonFaceDown;

        /// <param name="slotAt">놓을 칸 (드래그 배치). -1 이거나 차 있으면 왼쪽 첫 빈 칸</param>
        public IEnumerator Summon(CardInstance c, bool faceDown, int slotAt = -1)
        {
            if (!CanSummon(c, out _)) yield break;
            if (MustSummonFaceDown || Simul) faceDown = true;
            if (faceDown && !CanSummonFaceDown(c)) yield break;
            var p = c.owner;
            int slot = slotAt >= 0 && slotAt < PlayerState.SlotCount && p.slots[slotAt] == null ? slotAt : p.FreeSlot();
            p.hand.Remove(c);
            p.slots[slot] = c;
            c.slot = slot;
            c.zone = Zone.Slot;
            c.faceDown = faceDown;
            Sfx.Play("summon");
            if (p.isHuman) Quests.Add(Stat.Summon);
            if (p.isHuman && !NoStats) Chronicle.CountUse(c.data.id);
            Log(faceDown ? $"{p.name}: 몬스터를 뒷면 세로 표시로 소환 ({slot + 1}번 칸)" : $"{p.name}: {c.Name} 소환 ({slot + 1}번 칸)");
            RefreshAll();
            board.SummonFx(c);
            yield return Wait(0.45f);
            if (!faceDown) yield return RunEffect(c);
            StoryCountSummon(p);
            if (raid != null && p == Human && IsKeyCard(c.data)) yield return KeyStrike(c);
            RefreshAll();
        }

        public bool CanSet(CardInstance c)
            => c.IsMagic && c.zone == Zone.Hand && phase == Phase.Main && active == c.owner && c.owner.FreeSlot() >= 0;

        public IEnumerator SetMagic(CardInstance c, int slotAt = -1)
        {
            if (!CanSet(c)) yield break;
            var p = c.owner;
            int slot = slotAt >= 0 && slotAt < PlayerState.SlotCount && p.slots[slotAt] == null ? slotAt : p.FreeSlot();
            p.hand.Remove(c);
            p.slots[slot] = c;
            c.slot = slot;
            c.zone = Zone.Slot;
            c.faceDown = true;
            Sfx.Play("flip");
            if (p.isHuman && !NoStats) Chronicle.CountUse(c.data.id);
            Log($"{p.name}: 마법 카드를 세트 ({slot + 1}번 칸)");
            RefreshAll();
            yield return Wait(0.35f);
        }

        public bool CanFlip(CardInstance c)
            => !Simul && c.IsMonster && c.zone == Zone.Slot && c.faceDown && phase == Phase.Main && active == c.owner && !MustSummonFaceDown;

        public IEnumerator Flip(CardInstance c)
        {
            if (!CanFlip(c)) yield break;
            c.faceDown = false;
            Sfx.Play("summon");
            Log($"{c.owner.name}: {c.Name} 반전 소환");
            RefreshAll();
            board.SummonFx(c);
            yield return Wait(0.45f);
            yield return RunEffect(c);
            RefreshAll();
        }

        /// <summary>레이드에서는 보스의 필드가 끝까지 유지된다 (필드 카드 사용 불가)</summary>
        /// <summary>동시 룰: 엑스트라 덱의 필드 · T2 부터 · 이번 T 필드 차례인 쪽만 · T 마다 1장</summary>
        public bool CanPlayField(CardInstance c)
            => raid == null && mode != MatchMode.Puzzle && c != null && c.data.type == CardType.Field && phase == Phase.Main && active == c.owner && !c.owner.fieldPlayedThisTime
               && (Simul ? c.zone == Zone.Extra && FieldTurn == c.owner : c.zone == Zone.Hand);

        /// <summary>센터의 기존 필드를 치운다: 플레이어가 낸 카드는 묘지로, 주인 없는 필드(시작·효과로 바뀐 필드)는 뷰만 제거</summary>
        void RetireCenter()
        {
            var old = center;
            if (old == null) return;
            if (old.owner != null) { old.zone = Zone.Grave; old.owner.grave.Add(old); }
            else board.DestroyView(old);
        }

        public IEnumerator PlayField(CardInstance c)
        {
            if (!CanPlayField(c)) yield break;
            var p = c.owner;
            if (c.zone == Zone.Extra) p.extra.Remove(c); else p.hand.Remove(c);
            if (c.view == null) board.CreateView(c, board.HeroPos(p) + Vector3.up * 2f);
            p.fieldPlayedThisTime = true;
            NoteCpuCard(c);
            if (p.isHuman && !NoStats) Chronicle.CountUse(c.data.id);
            RetireCenter();
            center = c;
            c.zone = Zone.Center;
            Sfx.PlayFieldBgm(c.data);
            c.faceDown = false;
            Sfx.Play("field");
            Log($"{p.name}: 필드 카드 [{c.Name}] 전개");
            RefreshAll();
            board.SummonFx(c);
            if (fx != null) fx.FieldChange(c.data);
            yield return hud.Banner(c.Name, "FIELD CHANGE");
            yield return RunEffect(c);
            yield return FieldDeaths(c.Name);
            RefreshAll();
        }

        public bool CanUseHero(PlayerState p)
            => mode != MatchMode.Puzzle && !p.heroUsed && phase == Phase.Main && active == p && p.hero.data.effect != null
               && (p.hero.data.canActivate == null || p.hero.data.canActivate(this, p.hero));

        public IEnumerator UseHero(PlayerState p)
        {
            if (!CanUseHero(p)) yield break;
            p.heroUsed = true;
            Sfx.Play("hero");
            Voice.Hero(p.hero.data, "skill");
            if (p.isHuman) Quests.Add(Stat.HeroSkill);
            Log($"{p.name}: 영웅 [{p.hero.Name}] 능력 발동!");
            RefreshAll();
            board.SummonFx(p.hero);
            HeroModel.PlayAttack(p.hero, board.HeroPos(p.opponent));
            yield return hud.Banner(p.hero.Name, "HERO SKILL");
            yield return RunEffect(p.hero);
            RefreshAll();
        }

        public IEnumerator ActivateMagic(CardInstance c)
        {
            var p = c.owner;
            if (c.zone == Zone.Hand) p.hand.Remove(c);
            if (c.zone == Zone.Slot && c.slot >= 0) p.slots[c.slot] = null;
            c.slot = -1;
            c.zone = Zone.Resolving;
            c.faceDown = false;
            Sfx.Play("magic");
            if (p.isHuman) Quests.Add(Stat.MagicUse);
            Log($"{p.name}: 마법 [{c.Name}] 발동");
            RefreshAll();
            yield return Wait(0.6f);
            yield return RunEffect(c);
            c.zone = Zone.Grave;
            p.grave.Add(c);
            RefreshAll();
            yield return Wait(0.25f);
        }

        /// <summary>사람 플레이어의 UI 행동을 실행 (애니메이션 중 입력 잠금)</summary>
        public void HumanAct(IEnumerator routine)
        {
            if (busy || gameOver) return;
            StartCoroutine(RunBusy(routine));
        }

        IEnumerator RunBusy(IEnumerator routine)
        {
            busy = true;
            RefreshAll();
            yield return routine;
            busy = false;
            RefreshAll();
        }

        // ══════════════════════════ 기본 처리 ══════════════════════════

        /// <summary>n 장 드로우. 패가 HandMax 장이면 더 뽑지 않는다 (남은 장수는 덱에 그대로)</summary>
        public IEnumerator Draw(PlayerState p, int n, float delay = 0.18f)
        {
            if (raid != null && p == Cpu) yield break;   // 레이드 보스는 덱·패 없이 스킬 카드로 싸운다
            int room = Mathf.Max(0, HandLimit - p.hand.Count);
            if (n > room)
            {
                if (time > 0) Log(room == 0 ? $"{p.name}: 패가 가득 차 드로우하지 않는다 (최대 {HandLimit}장)" : $"{p.name}: 패 최대 {HandLimit}장 — {n}장 중 {room}장만 드로우");
                n = room;
            }
            if (fx != null && n > 0 && time > 0) fx.Draw(p, n);
            for (int i = 0; i < n; i++)
            {
                if (p.deck.Count == 0 && raid != null && p.grave.Count > 0) RecycleGrave(p);   // 레이드: 덱이 비면 묘지 순환
                if (p.deck.Count == 0)
                {
                    // 동시 룰: 덱이 비어도 패배가 아니다 — 대신 배틀 엔드마다 영웅끼리 맞붙는다 (HeroDuel)
                    if (Simul) { if (!p.deckOutWarned) { p.deckOutWarned = true; Log($"{p.name}: 덱이 비었다 — 이제부터 배틀 엔드마다 영웅 대결"); } break; }
                    Log($"{p.name}: 덱이 비어 드로우할 수 없다!");
                    EndGame(p.opponent);
                    yield break;
                }
                var c = p.deck[p.deck.Count - 1];
                p.deck.RemoveAt(p.deck.Count - 1);
                c.zone = Zone.Hand;
                p.hand.Add(c);
                Sfx.Play("draw", 0.6f);
                RefreshAll();
                yield return Wait(delay);
            }
            if (n > 0 && time > 0) Log($"{p.name}: {n}장 드로우");
        }

        public IEnumerator Heal(PlayerState p, int n)
        {
            int before = p.hp;
            p.hp = Mathf.Min(p.maxHp, p.hp + n);
            Log($"{p.name}: HP {p.hp - before} 회복 ({p.hp}/{p.maxHp})");
            Sfx.Play("heal");
            if (fx != null) fx.Heal(p);
            hud.FlashHp(p, true);
            RefreshAll();
            yield return Wait(0.35f);
        }

        public IEnumerator DamagePlayer(PlayerState p, int n, string source)
        {
            if (n <= 0) yield break;
            // 동시 룰: 한 T 에 영웅이 받는 피해 상한 (영웅 대결 피해는 제외) — 몰려도 최소 몇 T 는 버틴다
            if (Simul && !source.StartsWith("영웅 "))
            {
                directTaken.TryGetValue(p.index, out int taken);
                int room = Mathf.Max(0, DirectCapPerTime - taken);
                if (n > room)
                {
                    Log($"피해 상한 — 이번 T {p.name} 은(는) 최대 {DirectCapPerTime} 까지만 받는다 ({n} → {room})");
                    n = room;
                    if (n <= 0) yield break;
                }
                directTaken[p.index] = taken + n;
            }
            n = RaidIncomingBossDamage(p, n);   // 레이드: 대지 보스 암반 갑옷
            if (fx != null)
            {
                // 효과 데미지: 효과를 낸 카드에서 상대 영웅에게 날아간다 (전투 피해는 충돌 연출만)
                if (fxSource != null && fxSource.owner != p && !source.StartsWith("전투"))
                    yield return fx.Projectile(fx.PosOf(fxSource), fx.HeroPos(p), CardFx.StyleOf(fxSource), 1f + Mathf.Min(n, 8) * 0.06f);
                else fx.Impact(fx.HeroPos(p), fxSource != null ? CardFx.StyleOf(fxSource) : CardFx.Style.Neutral, 0.8f);
            }
            p.hp = Mathf.Max(0, p.hp - n);
            Log($"{p.name}: {n} 데미지 ({source}) → HP {p.hp}");
            Sfx.Play("hit");
            if (p.hp > 0 && p.hero != null) Voice.Hero(p.hero.data, "hit");
            HeroModel.PlayHit(p.hero);
            if (!p.isHuman) Quests.Add(Stat.Damage, n);
            hud.FlashHp(p, false);
            hud.FloatText(board.HeroPos(p), $"-{n}", new Color(1f, 0.3f, 0.25f));
            board.Shake(0.25f + Mathf.Min(n, 10) * 0.02f);
            RefreshAll();
            yield return Wait(0.5f);
            if (p.hp <= 0)
            {
                if (raid != null && p == Cpu) BossModel.Defeat();
                EndGame(p.opponent);
            }
        }

        public IEnumerator DestroyCard(CardInstance c, string source)
        {
            var p = c.owner;
            if (fx != null)
            {
                // 효과에 의한 파괴: 속성별 일격 (번개·불기둥·베기 …)
                if (fxSource != null && c != fxSource && c.zone == Zone.Slot) yield return fx.Strike(c, CardFx.StyleOf(fxSource));
                fx.Shatter(c);
            }
            if (c.zone == Zone.Slot && c.slot >= 0) p.slots[c.slot] = null;
            else if (c.zone == Zone.Hand) p.hand.Remove(c);
            c.slot = -1;
            c.faceDown = false;
            c.ClearFieldState();
            c.zone = Zone.Grave;
            p.grave.Add(c);
            Log($"{c.Name} 파괴 ({source})");
            Sfx.Play("destroy");
            if (!p.isHuman && c.IsMonster) Quests.Add(Stat.Destroy);
            board.DestroyFx(c);
            RefreshAll();
            yield return Wait(0.45f);
        }

        public IEnumerator GraveToHand(CardInstance c)
        {
            var p = c.owner;
            if (p.hand.Count >= HandLimit) { Log($"{p.name}: 패가 가득 차 {c.Name} 을(를) 패로 가져올 수 없다"); yield break; }
            p.grave.Remove(c);
            c.zone = Zone.Hand;
            p.hand.Add(c);
            Log($"{p.name}: 묘지의 {c.Name} 을(를) 패로");
            if (fx != null) fx.GraveRise(p);
            RefreshAll();
            yield return Wait(0.35f);
        }

        // ══════════════════════════ 신화 마법용 처리 ══════════════════════════

        /// <summary>이번 타임 동안 공격 불능</summary>
        public void Stun(CardInstance c, string source)
        {
            if (BlockedByResist(c)) return;
            c.stunUntil = time;
            Log($"{c.Name}: 행동 불능 ({source})");
            if (fx != null) fx.Stun(c);
            if (c.view != null) hud.FloatText(c.view.transform.position, "행동 불능", new Color(0.8f, 0.6f, 1f));
            RefreshAll();
        }

        // ── 몬스터 HP ──

        public int MaxHp(CardInstance c)
        {
            if (c == null || !c.IsMonster) return 0;
            int v = c.data.hp + CardData.FieldDelta(center?.data, c).hp;
            foreach (var b in c.hpBuffs) if (b.untilTime >= time) v += b.value;
            return Mathf.Max(1, v);
        }

        /// <summary>전투 판정 횟수 (바람 4 · 그 외 3, 필드 보정 반영, 최소 1)</summary>
        public int Rolls(CardInstance c) => Mathf.Max(1, c.data.DiceRolls + CardData.FieldDelta(center?.data, c).dice);

        /// <summary>필드가 바뀌어 HP 가 줄어든 결과 남은 HP 가 0 이 된 몬스터 파괴</summary>
        IEnumerator FieldDeaths(string source)
        {
            foreach (var p in players)
                foreach (var m in p.Monsters())
                    if (m.zone == Zone.Slot && CurHp(m) <= 0) yield return DestroyCard(m, source);
        }

        /// <summary>시나리오 테스트용: 센터 필드 교체 (연출·이벤트 없이)</summary>
        public void TestSetField(CardData f) { center = new CardInstance(f, null) { zone = Zone.Center }; RefreshAll(); }

        public int CurHp(CardInstance c) => c == null || !c.IsMonster ? 0 : Mathf.Max(0, MaxHp(c) - c.damage);

        /// <summary>times 타임 동안 HP 증감 (1 = 이번 타임만)</summary>
        public void AddHpBuff(CardInstance c, int value, int times)
        {
            if (value == 0 || c == null) return;
            if (value < 0 && BlockedByResist(c)) return;
            c.hpBuffs.Add(new AtkBuff { value = value, untilTime = time + Mathf.Max(1, times) - 1 });
            if (fx != null && value > 0) fx.HpUp(c);
            RefreshAll();
        }

        /// <summary>효과로 몬스터에게 n 데미지. HP 가 0 이 되면 파괴</summary>
        public IEnumerator DamageMonster(CardInstance c, int n, string source)
        {
            if (c == null || c.zone != Zone.Slot || !c.IsMonster || n <= 0 || gameOver) yield break;
            if (BlockedByResist(c)) yield break;
            if (fx != null)
            {
                if (fxSource != null && fxSource != c) yield return fx.Projectile(fx.PosOf(fxSource), fx.PosOf(c), CardFx.StyleOf(fxSource), 0.9f, 0.35f);
                else fx.Impact(fx.PosOf(c), CardFx.Style.Neutral, 0.7f);
            }
            c.damage += n;
            Sfx.Play("hit", 0.7f);
            if (c.view != null) hud.FloatText(c.view.transform.position, $"-{n}", new Color(1f, 0.4f, 0.3f));
            Log($"{c.Name}: {n} 데미지 ({source}) → HP {CurHp(c)}/{MaxHp(c)}");
            RefreshAll();
            yield return Wait(0.3f);
            if (CurHp(c) <= 0) yield return DestroyCard(c, source);
        }

        /// <summary>타임 시작: 필드의 모든 몬스터 HP 전부 회복 ([상처] 예외)</summary>
        void RegenMonsters()
        {
            foreach (var p in players)
                foreach (var c in p.slots)
                {
                    if (c == null || !c.IsMonster || c.damage <= 0) continue;
                    if (c.noRegenUntil >= time) { Log($"{c.Name}: [상처] 이번 타임 HP 가 회복되지 않는다 (HP {CurHp(c)}/{MaxHp(c)})"); continue; }
                    c.damage = 0;
                }
        }

        /// <summary>시나리오 테스트용</summary>
        public void TestRegen() => RegenMonsters();

        /// <summary>
        /// 전투 피해 계산 (상태 변경 없음). battleHp = 이번 전투 HP 증감 (빛/어둠 코인, 주사위 HP+).
        /// 반환: 파괴 여부 · 영웅에게 넘어가는 초과 피해 · 실제로 깎이는 HP
        /// </summary>
        public (bool dies, int overkill, int lost) PreviewHit(CardInstance m, int dmg, int battleHp, bool guard)
        {
            int cur = CurHp(m);
            int eff = cur + battleHp;
            if (dmg <= 0) return (false, 0, 0);
            if (dmg >= eff)
            {
                if (guard) return (false, 0, Mathf.Max(0, cur - 1));
                return (true, dmg - Mathf.Max(0, eff), cur);
            }
            int lost = battleHp > 0 ? Mathf.Max(0, dmg - battleHp) : dmg;
            return (false, 0, Mathf.Min(cur - 1, lost));
        }

        /// <summary>times 타임 동안 ATK 증감 (1 = 이번 타임만, 999 = 사실상 영구)</summary>
        /// <summary>약화: 이번 T 배틀 엔드까지 ATK -n ([불변]은 상대 효과면 무시)</summary>
        public void WeakenUntilBattleEnd(CardInstance c, int n, string src, bool log = true)
        {
            if (c == null || n <= 0 || !c.IsMonster || c.zone != Zone.Slot) return;
            if (BlockedByResist(c)) return;
            c.weaken += n;
            if (log) Log($"{c.Name}: 배틀 엔드까지 ATK -{n} ({src})");
            if (fx != null) fx.AtkChange(c, -n);
            RefreshAll();
        }

        /// <summary>배틀 엔드: 약화 해제</summary>
        public void ClearWeaken()
        {
            foreach (var p in players)
                foreach (var c in p.slots)
                    if (c != null) c.weaken = 0;
        }

        public void AddAtkBuff(CardInstance c, int value, int times)
        {
            if (value == 0) return;
            if (value < 0 && BlockedByResist(c)) return;
            c.buffs.Add(new AtkBuff { value = value, untilTime = time + Mathf.Max(1, times) - 1 });
            if (fx != null) fx.AtkChange(c, value);
            RefreshAll();
        }

        void RemoveFromCurrentZone(CardInstance c)
        {
            var p = c.owner;
            switch (c.zone)
            {
                case Zone.Slot: if (c.slot >= 0 && p.slots[c.slot] == c) p.slots[c.slot] = null; break;
                case Zone.Hand: p.hand.Remove(c); break;
                case Zone.Grave: p.grave.Remove(c); break;
                case Zone.Deck: p.deck.Remove(c); break;
            }
            c.slot = -1;
            c.ClearFieldState();
        }

        /// <summary>필드 카드를 주인의 패로 되돌린다</summary>
        public IEnumerator ReturnToHand(CardInstance c, string source)
        {
            if (BlockedByResist(c)) yield break;
            if (fx != null && c.zone == Zone.Slot) fx.Return(c, false);
            RemoveFromCurrentZone(c);
            c.faceDown = false;
            if (c.owner.hand.Count >= HandLimit)
            {
                // 패 최대 장수 초과분은 묘지로
                c.zone = Zone.Grave;
                c.owner.grave.Add(c);
                Log($"{c.Name}: 패가 가득 차 묘지로 ({source})");
                RefreshAll();
                yield return Wait(0.35f);
                yield break;
            }
            c.zone = Zone.Hand;
            c.owner.hand.Add(c);
            Log($"{c.Name}: 패로 되돌아감 ({source})");
            RefreshAll();
            yield return Wait(0.35f);
        }

        /// <summary>카드를 주인의 덱 무작위 위치로</summary>
        public IEnumerator ToDeck(CardInstance c, string source)
        {
            if (fx != null && c.zone == Zone.Slot) fx.Return(c, true);
            RemoveFromCurrentZone(c);
            c.faceDown = false;
            c.zone = Zone.Deck;
            c.owner.deck.Insert(rng.Next(c.owner.deck.Count + 1), c);
            Log($"{c.Name}: 덱으로 되돌아감 ({source})");
            RefreshAll();
            yield return Wait(0.35f);
        }

        /// <summary>묘지(어느 쪽이든)의 몬스터를 to 의 필드 빈칸에 특수 소환. 상대 카드면 소유권을 가져온다</summary>
        public IEnumerator SpecialSummon(CardInstance c, PlayerState to, bool faceDown, string source)
        {
            int slot = to.FreeSlot();
            if (slot < 0) { Log($"{source}: 빈 칸이 없어 소환 실패"); yield break; }
            RemoveFromCurrentZone(c);
            c.owner = to;
            to.slots[slot] = c;
            c.slot = slot;
            c.zone = Zone.Slot;
            c.faceDown = faceDown;
            Sfx.Play("summon");
            Log($"{to.name}: {c.Name} 특수 소환 ({source})");
            RefreshAll();
            if (fx != null) fx.SpecialSummon(c);
            board.SummonFx(c);
            yield return Wait(0.45f);
        }

        /// <summary>센터 필드를 주인 없는 필드 카드로 교체 (효과에 의한 필드 변경)</summary>
        public IEnumerator ChangeField(CardData fieldData, string source)
        {
            if (raid != null)
            {
                // 레이드: 보스의 필드는 바꿀 수도, 파괴할 수도 없다
                Log($"{source}: 보스의 필드 [{center?.Name}]는 바뀌지 않는다 (레이드)");
                if (center?.view != null) hud.FloatText(center.view.transform.position, "필드 고정", new Color(1f, 0.75f, 0.4f));
                yield return Wait(0.4f);
                yield break;
            }
            RetireCenter();
            center = new CardInstance(fieldData, null) { zone = Zone.Center };
            Sfx.PlayFieldBgm(fieldData);
            board.CreateView(center, BoardView.CenterPos + Vector3.up * 2f);
            Sfx.Play("field");
            Log($"필드가 [{fieldData.name}] (으)로 바뀌었다 ({source})");
            StartCoroutine(FieldDeaths(fieldData.name));
            if (fx != null) fx.FieldChange(fieldData);
            RefreshAll();
            board.SummonFx(center);
            yield return Wait(0.6f);
        }

        /// <summary>순속성: 필드 몬스터가 1장 이상이고 모두 같은 속성 (무지개는 어느 속성과도 일치)</summary>
        public bool IsMonoElement(PlayerState p)
        {
            var mons = p.Monsters();
            if (mons.Count == 0) return false;
            Element e = Element.None;
            foreach (var m in mons)
            {
                var me = m.data.element;
                if (me == Element.Rainbow) continue;
                if (me == Element.None) return false;
                if (e == Element.None) e = me;
                else if (e != me) return false;
            }
            return true;
        }

        // ══════════════════════════ 상태/수치 ══════════════════════════

        public void AddStatus(Status s)
        {
            s.createdTime = time;
            statuses.Add(s);
            if (fx != null) fx.Status(this, s);
            Log($"효과 적용: {s.source}");
            RefreshAll();
        }

        void ExpireStatuses(Predicate<Status> pred)
        {
            var gone = statuses.Where(s => pred(s)).ToList();
            foreach (var s in gone)
            {
                statuses.Remove(s);
                Log($"효과 종료: {s.source}");
            }
            if (gone.Count > 0) RefreshAll();
        }

        public bool HasStatus(StatusKind kind, PlayerState p)
            => statuses.Any(s => s.kind == kind && (s.target == null || p == null || s.target == p));

        /// <summary>뒷면이지만 보여 주는 카드: 배틀 시작 전체 공개 중이거나 [봉화] 등으로 공개</summary>
        public bool IsRevealed(CardInstance c) => c.faceDown && c.zone == Zone.Slot && (BattleOpen || BeaconRevealed(c));

        public bool BeaconRevealed(CardInstance c)
            => c.faceDown && c.zone == Zone.Slot && statuses.Any(s => s.kind == StatusKind.RevealFaceDown && s.target == c.owner);

        public int EffectiveAtk(CardInstance c)
        {
            if (!c.IsMonster) return 0;
            int v = c.data.atk + c.tempAtk - c.weaken + EventAtkBonus;
            foreach (var b in c.buffs) if (b.untilTime >= time) v += b.value;
            v += CardData.FieldDelta(center?.data, c).atk;
            foreach (var s in statuses)
                if (s.kind == StatusKind.AtkBonus && (s.target == null || s.target == c.owner) && !(s.value < 0 && Resist(c) && s.caster != c.owner)) v += s.value;
            if (raid != null && c.owner == Cpu) v = Mathf.RoundToInt(v * raid.atkMul);   // 레이드 보스 진영 공격력 배율
            if (story != null && story.battle.mist > 0 && c.data.element != Element.None) v -= story.battle.mist;   // 스토리: 회색 안개
            if (story != null && story.battle.absorb > 0 && c.owner == Human) v += story.battle.absorb;          // 스토리: 원소 흡수
            return Mathf.Max(0, v);
        }

        /// <summary>사람 플레이어가 이 카드의 앞면을 볼 수 있는가</summary>
        public bool HumanCanSee(CardInstance c)
        {
            if (c.owner == null || c.owner == Human) return true;
            switch (c.zone)
            {
                case Zone.Deck:
                case Zone.Extra:
                case Zone.Hand: return false;
                case Zone.Slot: return !c.faceDown || IsRevealed(c);
                default: return true;
            }
        }

        public void RefreshAll()
        {
            if (board != null) board.Layout(this);
            if (hud != null) hud.Refresh();
        }

        // ══════════════════════════ 선택 ══════════════════════════

        /// <summary>보드 위 카드 중 선택 (사람: 클릭/목록, AI: aiScore 최대)</summary>
        public IEnumerator ChooseCard(PlayerState chooser, List<CardInstance> options, string prompt, Func<CardInstance, float> aiScore,
            Action<CardInstance> result, bool allowCancel = false, string cancelLabel = "취소", float aiCancelBelow = float.NegativeInfinity)
        {
            if (options == null || options.Count == 0) { result(null); yield break; }
            if (!HumanDecides(chooser))
            {
                yield return Wait(0.45f);
                CardInstance best = null; float bestScore = float.NegativeInfinity;
                foreach (var o in options)
                {
                    float s = aiScore != null ? aiScore(o) : UnityEngine.Random.value;
                    if (s > bestScore) { bestScore = s; best = o; }
                }
                if (allowCancel && bestScore < aiCancelBelow) best = null;
                result(best);
                yield break;
            }

            yield return HumanPick(options, pick => hud.ShowChoice(prompt, options, allowCancel ? cancelLabel : null, pick), hud.HideChoice, result,
                () => AutoPick(options, aiScore, allowCancel, aiCancelBelow));
        }

        /// <summary>시간 초과 자동 선택: aiScore 최고 (점수가 없으면 취소 가능 = 취소/패스, 아니면 첫 번째)</summary>
        CardInstance AutoPick(List<CardInstance> options, Func<CardInstance, float> aiScore, bool allowCancel, float aiCancelBelow = float.NegativeInfinity)
        {
            if (aiScore == null) return allowCancel ? null : options.FirstOrDefault();
            CardInstance best = null; float bestScore = float.NegativeInfinity;
            foreach (var o in options) { float sc = aiScore(o); if (sc > bestScore) { bestScore = sc; best = o; } }
            if (allowCancel && bestScore < aiCancelBelow) best = null;
            return best;
        }

        /// <summary>전투(배틀 · 배틀 엔드) 중 사람 선택지 제한 시간 — 지나면 자동 선택</summary>
        public const float BattleChoiceSeconds = 7f;
        bool ChoiceTimed => Simul && (phase == Phase.Battle || phase == Phase.BattleEnd);

        /// <summary>사람 선택 대기. 전투 중이면 7초 제한(일시정지 중엔 멈춤, 2배속과 무관). finished(false) = 시간 초과</summary>
        IEnumerator WaitChoice(Func<bool> done, Action<bool> finished)
        {
            hud.CloseGrave();     // 묘지 보기가 선택지를 가리지 않게
            bool timed = ChoiceTimed;
            float left = BattleChoiceSeconds;
            while (!done() && !gameOver)
            {
                if (timed)
                {
                    if (Time.timeScale > 0) left -= Time.unscaledDeltaTime;
                    hud.ChoiceTimer(left);
                    if (left <= 0) { hud.ChoiceTimer(-1); finished(false); yield break; }
                }
                yield return null;
            }
            hud.ChoiceTimer(-1);
            finished(true);
        }

        /// <summary>
        /// 사람의 카드 선택 대기: show(pick) 로 입력 UI 를 띄우고, UI 선택이나 3D 카드 클릭(TryPickChoice)으로
        /// 고르면 hide 후 결과를 넘긴다. 같은 프레임의 두 번째 선택은 무시.
        /// </summary>
        IEnumerator HumanPick(List<CardInstance> options, Action<Action<CardInstance>> show, Action hide, Action<CardInstance> result, Func<CardInstance> onTimeout = null)
        {
            choiceOptions = options;
            choiceDone = false;
            choiceResult = null;
            RefreshAll();
            show(c => { if (choiceDone) return; choiceResult = c; choiceDone = true; });
            bool ok = true;
            yield return WaitChoice(() => choiceDone, r => ok = r);
            if (!ok)
            {
                choiceResult = onTimeout?.Invoke();
                choiceDone = true;
                Log("선택 시간 초과 — 자동 선택" + (choiceResult != null ? $" ({choiceResult.Name})" : ""));
            }
            hide();
            choiceOptions = null;
            RefreshAll();
            result(choiceResult);
        }

        /// <summary>
        /// 공격 대상 선택. 사람은 선택창 없이 공격 몬스터를 대상에게 드래그하거나,
        /// 대상 카드 클릭 / 단축키(기본 1~7 = 상대 칸 번호, Space = 공격 안 함)로 고른다.
        /// </summary>
        public IEnumerator ChooseAttackTarget(CardInstance attacker, List<CardInstance> options, string prompt, Func<CardInstance, float> aiScore,
            Action<CardInstance> result, float aiCancelBelow = float.NegativeInfinity)
        {
            if (options == null || options.Count == 0) { result(null); yield break; }
            if (!HumanDecides(attacker.owner))
            {
                yield return ChooseCard(attacker.owner, options, prompt, aiScore, result, true, "공격 안 함", aiCancelBelow);
                yield break;
            }
            yield return HumanPick(options, pick => hud.ShowTargetMode(attacker, options, prompt, pick), hud.HideTargetMode, result,
                () => AutoPick(options, aiScore, true, aiCancelBelow) ?? AutoPick(options, aiScore, false));
        }

        /// <summary>목록(묘지/패 등)에서 선택. 사람은 목록 패널 사용</summary>
        public IEnumerator ChooseFromList(PlayerState chooser, List<CardInstance> options, string prompt, Func<CardInstance, float> aiScore,
            Action<CardInstance> result, bool allowCancel = false, string cancelLabel = "취소")
            => ChooseCard(chooser, options, prompt, aiScore, result, allowCancel, cancelLabel);

        /// <summary>3D 카드 클릭으로 선택 완료</summary>
        public bool TryPickChoice(CardInstance c)
        {
            if (!IsChoiceOption(c)) return false;
            choiceResult = c;
            choiceDone = true;
            return true;
        }

        public IEnumerator ChooseOption(PlayerState chooser, string prompt, string[] labels, Func<int> aiPick, Action<int> result)
        {
            if (!HumanDecides(chooser))
            {
                yield return Wait(0.3f);
                result(aiPick != null ? aiPick() : 0);
                yield break;
            }
            bool done = false; int pick = 0;
            hud.ShowOptions(prompt, labels, i => { pick = i; done = true; });
            bool ok = true;
            yield return WaitChoice(() => done, r => ok = r);
            if (!ok) { pick = aiPick != null ? aiPick() : 0; Log($"선택 시간 초과 — 자동 선택 ({labels[Mathf.Clamp(pick, 0, labels.Length - 1)]})"); }
            hud.HideChoice();
            result(pick);
        }
    }
}
