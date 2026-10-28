using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>레이드 진행 상태 (파티 · 보스 배율 · 팀원)</summary>
    public class RaidState
    {
        public RaidBoss boss;
        public int partySize = 1;
        public float hpMul = 1f, atkMul = 1f;
        /// <summary>나를 제외한 팀원. 지금은 AI 가 조작하는 시뮬레이션 팀원 — 온라인 연결 시 같은 자리에 원격 플레이어 상태가 들어온다</summary>
        public readonly List<RaidAlly> allies = new List<RaidAlly>();
        public int humanDealt;
        public int keyStrikes;
    }

    /// <summary>팀원 전장의 간이 몬스터 (팀원 화면 표시 + 시뮬레이션 전투용)</summary>
    public class SimMon
    {
        public CardData data;
        public int atk, hp, maxHp;
        public bool key;
    }

    /// <summary>
    /// 레이드 팀원 1명. 보스 본체(HP)는 파티가 공유하고, 각 팀원 전장에는 보스가 따로 졸개를 보낸다.
    /// isRemote = true 가 되면(온라인) AlliesAct 대신 네트워크에서 받은 상태로 갱신하면 된다.
    /// </summary>
    public class RaidAlly
    {
        public string name, avatar;
        public CardData hero;
        public int hp, maxHp;
        public bool isRemote;
        public readonly List<CardData> deck = new List<CardData>();
        public readonly List<CardData> hand = new List<CardData>();
        public readonly List<SimMon> lane = new List<SimMon>();
        public readonly List<SimMon> minions = new List<SimMon>();
        public readonly List<string> log = new List<string>();
        public int dealt;
        public bool KO => hp <= 0;

        public void Log(string s)
        {
            log.Add(s);
            if (log.Count > 9) log.RemoveAt(0);
        }
    }

    public partial class GameManager
    {
        public RaidState raid;
        RaidState pendingRaid;
        /// <summary>레이드에서 파티·보스가 공유하는 "라운드" 번호 (파티 타임 기준 1, 2, 3 …) — 팀원 소환 가능 시점</summary>
        int raidTime;
        /// <summary>다음 보스 피해 1회는 대지 보스의 암반 갑옷을 무시 (약점 공격 · 핵심 카드)</summary>
        bool armorBypass;
        /// <summary>팀원 피해 처리 중 (내가 준 피해 집계에서 제외)</summary>
        bool allyDamaging;

        public bool IsRaid => raid != null;
        public int RaidRound => raidTime;

        static readonly string[] AllyNames = { "아린", "카이른", "세나", "도윤", "루카", "하나", "미르", "레아" };
        static readonly string[] AllyAvatars = { "avatar_02", "avatar_03", "avatar_04", "avatar_05", "avatar_06", "avatar_07" };

        /// <summary>팀원 공격력 보정 (시뮬레이션 밸런스: 팀원 1명 ≈ 사람 1명 몫)</summary>
        public const float AllyPower = 1f;
        /// <summary>보스가 맞이한 자신의 타임 수 (1, 2, 3 …) — 스킬 순서 결정</summary>
        int bossTurn;
        bool bossUltUsed;
        public int BossTurn => bossTurn;

        /// <summary>레이드 시작: 보스 · 파티 인원 · 내 영웅/덱. 필드는 보스 속성 필드에서 시작</summary>
        public void StartRaid(CardData hero, List<CardData> deck, RaidBoss boss, int partySize)
        {
            partySize = Mathf.Clamp(partySize, 1, RaidData.MaxParty);
            var (hm, am) = RaidData.Scale(partySize);
            var st = new RaidState { boss = boss, partySize = partySize, hpMul = hm, atkMul = am };
            // 팀원 영웅: 약점 속성 영웅 우선, 나와 겹치지 않게
            var heroes = CardList.Heroes.Where(h => h != hero && h.element != Element.None && !StoryData.IsStoryHero(h))
                .OrderByDescending(h => h.element == boss.weakness ? 1 : 0).ThenBy(_ => rng.Next()).ToList();
            var names = AllyNames.OrderBy(_ => rng.Next()).ToList();
            for (int i = 0; i < partySize - 1; i++)
            {
                var h = heroes[i % heroes.Count];
                var a = new RaidAlly { name = names[i], avatar = AllyAvatars[rng.Next(AllyAvatars.Length)], hero = h, hp = h.heroHp, maxHp = h.heroHp };
                foreach (var c in RaidData.RecommendedDeck(boss, rng).Where(c => c.type == CardType.Monster).OrderBy(_ => rng.Next())) a.deck.Add(c);
                AllyDraw(a, StartHand);
                a.Log($"{h.name}(으)로 참전");
                st.allies.Add(a);
            }
            humanDeck = deck;
            pendingRaid = st;
            StartMatch(hero, RaidData.BossField(boss));
        }

        void SetupRaidBoss(PlayerState p)
        {
            var b = raid.boss;
            var hd = b.Hero;
            p.hero = new CardInstance(hd, p) { zone = Zone.Hero };
            p.freeCost = true;      // 보스는 원소 없이 하수인을 낸다
            p.maxHp = p.hp = Mathf.RoundToInt(b.baseHp * raid.hpMul);
            board.CreateView(p.hero, board.HeroPos(p) + Vector3.up * 2f);
            // 보스는 카드 위가 아니라 보드 뒤편에 거대하게 선다. 덱·패 없이 스킬 카드로만 싸운다
            BossModel.Spawn(b.id, p.hero);
            bossTurn = 0;
            bossUltUsed = false;
        }

        // ══════════════════════════ 보스 스킬 (메인 페이즈) ══════════════════════════

        /// <summary>
        /// 보스 메인 페이즈: 일반 카드 대신 스킬 카드를 사용한다.
        /// 1타임: 하수인 소환 · 짝수 타임: 하수인 소환 + 공격 스킬 · 3타임 이후 홀수: 정예 소환 + 하수인 소환.
        /// HP 50% 이하가 되면 궁극기를 1회 먼저 쓴다.
        /// </summary>
        IEnumerator BossMainPhase()
        {
            bossTurn++;
            var b = raid.boss;
            var plan = new List<BossSkill.Kind>();
            if (!bossUltUsed && Cpu.hp <= Cpu.maxHp / 2) { plan.Add(BossSkill.Kind.Ultimate); bossUltUsed = true; }
            plan.Add(bossTurn >= 3 && bossTurn % 2 == 1 ? BossSkill.Kind.SummonB : BossSkill.Kind.SummonA);
            if (bossTurn >= 2) plan.Add(bossTurn % 2 == 0 ? BossSkill.Kind.Attack : BossSkill.Kind.SummonA);
            yield return Wait(0.4f);
            foreach (var k in plan)
            {
                if (gameOver) yield break;
                yield return UseBossSkill(b.Skill(k));
            }
            yield return Wait(0.3f);
        }

        /// <summary>스킬 카드 1장 사용: 카드를 화면 중앙에 띄우고 효과 실행 → 팀원 전장에도 같은 스킬 피해</summary>
        IEnumerator UseBossSkill(BossSkill s)
        {
            var c = new CardInstance(s.card, Cpu) { zone = Zone.Resolving };
            board.CreateView(c, board.HeroPos(Cpu) + Vector3.up * 1.5f);
            Sfx.Play("magic");
            Log($"{Cpu.name}: 스킬 [{s.card.name}]");
            hud.RaidToast($"보스 스킬 — {s.card.name}");
            BossModel.Roar();
            RefreshAll();
            yield return Wait(0.9f);
            yield return RunEffect(c);
            if (!gameOver) SkillHitsAllies(s);
            yield return Wait(0.3f);
            board.DestroyView(c);
            RefreshAll();
        }

        /// <summary>스킬 효과에서 호출: 보스 전용 하수인 소환 (최대 RaidData.MaxMinions 장)</summary>
        public IEnumerator BossSummon(CardData minion, int n, string source)
        {
            for (int i = 0; i < n && !gameOver; i++)
            {
                if (Cpu.FieldCount >= RaidData.MaxMinions || Cpu.FreeSlot() < 0) { Log($"{source}: 하수인이 가득 차 더 소환할 수 없다"); yield break; }
                var c = new CardInstance(minion, Cpu) { zone = Zone.Resolving };
                board.CreateView(c, board.HeroPos(Cpu) + Vector3.up * 1.2f);
                yield return SpecialSummon(c, Cpu, false, source);
            }
        }

        void SkillHitsAllies(BossSkill s)
        {
            foreach (var a in raid.allies)
            {
                if (a.KO || a.isRemote) continue;
                int mul(int v) => Mathf.RoundToInt(v * raid.atkMul);
                if (s.laneAll > 0) foreach (var m in a.lane) m.hp -= mul(s.laneAll);
                if (s.laneOne > 0 && a.lane.Count > 0) a.lane.OrderByDescending(m => m.atk).First().hp -= mul(s.laneOne);
                int dead = a.lane.RemoveAll(m => m.hp <= 0);
                if (s.laneAll > 0 || s.laneOne > 0) a.Log($"[{s.card.name}] 내 몬스터 피해{(dead > 0 ? $" · {dead}장 파괴" : "")}");
                if (s.hero > 0) AllyHit(a, mul(s.hero), s.card.name);
            }
        }

        /// <summary>레이드: 덱이 비면 묘지를 섞어 덱으로 되돌린다 (보스·파티 모두 덱 아웃으로 지지 않는다 — 승부는 HP 로)</summary>
        void RecycleGrave(PlayerState p)
        {
            var list = p.grave.OrderBy(_ => rng.Next()).ToList();
            p.grave.Clear();
            foreach (var c in list)
            {
                c.zone = Zone.Deck;
                c.damage = 0; c.tempAtk = 0; c.weaken = 0; c.faceDown = false; c.buffs.Clear();
                p.deck.Add(c);
            }
            Log($"{p.name}: 묘지의 카드 {list.Count}장이 덱으로 돌아왔다");
            RefreshAll();
        }

        /// <summary>영웅 반격 ATK (레이드 보스는 공격력 배율 적용)</summary>
        public int HeroAtk(PlayerState p)
        {
            int v = p.hero.data.heroAtk;
            if (raid != null && p == Cpu) v = Mathf.RoundToInt(v * raid.atkMul);
            return v;
        }

        public bool IsWeaknessCard(CardData d) => raid != null && d != null && d.element == raid.boss.weakness;
        public bool IsKeyCard(CardData d) => raid != null && d != null && raid.boss.KeyCards.Any(k => k.id == d.id);

        /// <summary>보스에게 들어가는 몬스터 피해: 약점 속성 2배 (+ 대지 갑옷 무시)</summary>
        int RaidBossDamage(CardInstance a, PlayerState target, int dmg)
        {
            if (raid == null || target != Cpu || a == null || a.owner == Cpu || dmg <= 0) return dmg;
            if (!IsWeaknessCard(a.data)) return dmg;
            armorBypass = true;
            Log($"약점 공격! {a.Name} ({CardData.ElementName(raid.boss.weakness)}) → 보스 피해 2배 {dmg} → {dmg * 2}");
            if (a.view != null) hud.FloatText(a.view.transform.position, "약점 ×2", new Color(1f, 0.85f, 0.3f));
            return dmg * 2;
        }

        /// <summary>보스가 받는 피해 최종 보정: 대지 보스 암반 갑옷 -2 (최소 1)</summary>
        int RaidIncomingBossDamage(PlayerState p, int n)
        {
            if (raid == null || p != Cpu) return n;
            bool bypass = armorBypass;
            armorBypass = false;
            if (raid.boss.element == Element.Earth && !bypass)
            {
                int r = Mathf.Max(1, n - 2);
                if (r < n) Log($"[암반 갑옷] 보스 피해 {n} → {r}");
                n = r;
            }
            if (!allyDamaging) raid.humanDealt += Mathf.Min(n, p.hp);
            return n;
        }

        /// <summary>핵심 카드 소환: 보스 최대 HP 10% 피해 (갑옷 무시)</summary>
        IEnumerator KeyStrike(CardInstance c)
        {
            if (gameOver) yield break;
            int n = KeyStrikeDamage;
            raid.keyStrikes++;
            Log($"★ 핵심 카드 {c.Name}! 보스에게 최대 HP 10% ({n}) 피해");
            if (c.view != null) hud.FloatText(c.view.transform.position, "핵심 카드!", new Color(1f, 0.9f, 0.35f));
            Sfx.Play("victory", 0.35f);
            var prev = fxSource;
            fxSource = c;
            armorBypass = true;
            yield return DamagePlayer(Cpu, n, "핵심 카드 " + c.Name);
            fxSource = prev;
        }

        int KeyStrikeDamage => Mathf.CeilToInt(Cpu.maxHp * 0.10f);

        // ══════════════════════════ 보스 속성 특성 (보스 타임 시작) ══════════════════════════

        IEnumerator BossTimeStart()
        {
            var b = raid.boss;
            var boss = Cpu;
            var alive = raid.allies.Where(a => !a.KO).ToList();
            yield return hud.Banner(b.traitName, b.traitText, 0.7f);
            switch (b.element)
            {
                case Element.Fire:
                    foreach (var a in alive) AllyHit(a, 2, b.traitName);
                    yield return DamagePlayer(Human, 2, b.traitName);
                    break;
                case Element.Water:
                    if (boss.hp < boss.maxHp) yield return Heal(boss, Mathf.CeilToInt(boss.maxHp * 0.04f));
                    break;
                case Element.Wind:
                {
                    var m = boss.Monsters().Where(c => !c.faceDown).OrderByDescending(EffectiveAtk).FirstOrDefault();
                    if (m != null) { GrantExtraAttacks(m, 1); Log($"[{b.traitName}] {m.Name}: 이번 타임 추가 공격 +1"); yield return Wait(0.4f); }
                    else
                    {
                        // 공격할 몬스터가 없으면 돌풍이 파티를 직접 친다
                        foreach (var a in alive) AllyHit(a, 2, b.traitName);
                        yield return DamagePlayer(Human, 2, b.traitName);
                    }
                    break;
                }
                case Element.Earth:
                {
                    var m = boss.Monsters().Where(c => !c.faceDown).OrderByDescending(CurHp).FirstOrDefault();
                    if (m != null) { m.tauntUntil = time + 1; Log($"[{b.traitName}] {m.Name}: [도발] (다음 파티 타임까지)"); if (m.view != null) hud.FloatText(m.view.transform.position, "도발", new Color(0.85f, 0.7f, 0.45f)); }
                    yield return Wait(0.3f);
                    break;
                }
                case Element.Thunder:
                    foreach (var a in alive)
                    {
                        foreach (var s in a.lane) s.hp -= 2;
                        int dead = a.lane.RemoveAll(s => s.hp <= 0);
                        a.Log($"[낙뢰] 내 몬스터 전체 2 데미지{(dead > 0 ? $" · {dead}장 파괴" : "")}");
                    }
                    foreach (var c in Human.Monsters().ToList())
                    {
                        yield return DamageMonster(c, 2, b.traitName);
                        if (gameOver) yield break;
                    }
                    break;
                case Element.Light:
                {
                    bool heads = rng.Next(2) == 0;
                    dice.humanSide = 0;     // 보스(1)의 코인
                    yield return dice.FlipCoin(1, heads);
                    Log($"[{b.traitName}] 코인 {(heads ? "앞면 — 보스 몬스터 ATK +2 · HP 5% 회복" : "뒷면 — 변화 없음")}");
                    if (heads)
                    {
                        AddStatus(new Status { kind = StatusKind.AtkBonus, target = boss, caster = boss, expire = Expire.EndOfTime, value = 2, createdTime = time, source = b.traitName });
                        yield return Heal(boss, Mathf.CeilToInt(boss.maxHp * 0.05f));
                    }
                    yield return Wait(0.3f);
                    dice.Clear();
                    break;
                }
                case Element.Dark:
                {
                    bool heads = rng.Next(2) == 0;
                    dice.humanSide = 0;     // 보스(1)의 코인
                    yield return dice.FlipCoin(1, heads);
                    if (heads)
                    {
                        Log($"[{b.traitName}] 코인 앞면 — 파티 전원에게서 HP 3 흡수");
                        int drained = 0;
                        foreach (var a in alive) drained += AllyHit(a, 3, "흡수");
                        int before = Human.hp;
                        yield return DamagePlayer(Human, 3, "흡수");
                        drained += before - Human.hp;
                        if (!gameOver && drained > 0) yield return Heal(boss, drained);
                    }
                    else
                    {
                        Log($"[{b.traitName}] 코인 뒷면 — 어둠의 대가: 보스가 최대 HP 5%를 잃는다");
                        armorBypass = true;
                        yield return DamagePlayer(boss, Mathf.CeilToInt(boss.maxHp * 0.05f), "어둠의 대가");
                    }
                    dice.Clear();
                    break;
                }
            }
            RefreshAll();
        }

        int AllyHit(RaidAlly a, int n, string source)
        {
            if (a.KO || n <= 0) return 0;
            int d = Mathf.Min(a.hp, n);
            a.hp -= d;
            a.Log($"[{source}] 영웅 {d} 데미지 → HP {a.hp}");
            if (a.KO) { a.Log("쓰러졌다…"); Log($"팀원 {a.name} 쓰러짐!"); }
            return d;
        }

        // ══════════════════════════ AI 팀원 (파티 타임 끝) ══════════════════════════

        void AllyDraw(RaidAlly a, int n)
        {
            for (int i = 0; i < n && a.hand.Count < HandMax; i++)
            {
                if (a.deck.Count == 0) break;
                a.hand.Add(a.deck[a.deck.Count - 1]);
                a.deck.RemoveAt(a.deck.Count - 1);
            }
        }

        SimMon MakeSim(CardData d, float atkMul = 1f)
        {
            int hp = d.hp;
            return new SimMon { data = d, atk = Mathf.RoundToInt(d.atk * atkMul), hp = hp, maxHp = hp, key = IsKeyCard(d) };
        }

        /// <summary>팀원 전원 행동: 드로우 → 소환(핵심 카드는 보스 10%) → 졸개 처리 → 보스 직접 공격</summary>
        IEnumerator AlliesAct()
        {
            foreach (var a in raid.allies)
            {
                if (gameOver) yield break;
                if (a.KO || a.isRemote) continue;
                foreach (var x in a.lane) x.hp = x.maxHp;   // 타임 시작: 몬스터 HP 회복 (본 게임 규칙과 같음)
                AllyDraw(a, HandMax - a.hand.Count);
                int t = Mathf.Max(1, time);
                // 소환: 타임당 최대 2장, 전장 5칸. 팀원은 원소 풀을 따로 두지 않고 "라운드 수 = 쌓인 원소 수"로 본다 (약점 속성 단색 덱이라 비용 ≤ 라운드면 낼 수 있음)
                var picks = a.hand.Where(d => d.Cost <= t && (d.maxTime == 0 || d.maxTime >= t))
                    .OrderByDescending(d => IsKeyCard(d) ? 1 : 0).ThenByDescending(d => d.atk + d.hp).Take(2).ToList();
                int keyDmg = 0;
                foreach (var d in picks)
                {
                    if (a.lane.Count >= 5) break;
                    a.hand.Remove(d);
                    var m = MakeSim(d);
                    a.lane.Add(m);
                    a.Log($"{d.name} 소환");
                    if (m.key) { keyDmg += KeyStrikeDamage; raid.keyStrikes++; a.Log($"★ 핵심 카드! 보스에게 {KeyStrikeDamage} 피해"); }
                }
                // 공격: 졸개가 있으면 졸개부터, 없으면 보스 본체 (보스 반격을 받는다)
                int bossDmg = 0;
                // 보스 반격은 본체를 친 몬스터 중 가장 강한 1장에게만 (전원이 반격으로 전멸하지 않게)
                SimMon striker = null;
                foreach (var m in a.lane.ToList())
                {
                    if (a.minions.Count > 0)
                    {
                        // 이기거나 살아남는 교환만 한다 (아니면 남아서 방어)
                        var target = a.minions.Where(x => x.hp <= m.atk || x.atk < m.hp)
                            .OrderBy(x => x.hp <= m.atk ? 0 : 1).ThenByDescending(x => x.atk).FirstOrDefault();
                        if (target == null) continue;
                        target.hp -= m.atk;
                        m.hp -= target.atk;
                        a.Log($"{m.data.name} → 졸개 {target.data.name}{(target.hp <= 0 ? " 격파" : "")}");
                        a.minions.RemoveAll(x => x.hp <= 0);
                    }
                    else
                    {
                        bool weak = IsWeaknessCard(m.data);
                        int d = Mathf.Max(1, Mathf.RoundToInt(m.atk * AllyPower)) * (weak ? 2 : 1);
                        if (raid.boss.element == Element.Earth && !weak) d = Mathf.Max(1, d - 2);
                        bossDmg += d;
                        if (striker == null || m.atk > striker.atk) striker = m;
                    }
                }
                if (striker != null) striker.hp -= HeroAtk(Cpu);
                int lost = a.lane.RemoveAll(x => x.hp <= 0);
                if (lost > 0) a.Log($"내 몬스터 {lost}장 쓰러짐");
                int total = bossDmg + keyDmg;
                if (total > 0)
                {
                    a.Log($"보스에게 {total} 피해");
                    a.dealt += Mathf.Min(total, Cpu.hp);
                    hud.RaidToast($"{a.name}의 공격!  보스에게 {total} 피해" + (keyDmg > 0 ? "  (★ 핵심 카드)" : ""));
                    var prev = fxSource;
                    fxSource = null;
                    allyDamaging = true;
                    armorBypass = true;   // 갑옷은 위에서 타격마다 이미 적용
                    yield return DamagePlayer(Cpu, total, $"팀원 {a.name}");
                    allyDamaging = false;
                    fxSource = prev;
                }
                else yield return Wait(0.2f);
                RefreshAll();
            }
        }

        /// <summary>보스 타임 끝: 팀원 전장마다 졸개를 보내고, 졸개가 팀원 몬스터·영웅을 공격</summary>
        IEnumerator BossVsAllies()
        {
            int t = Mathf.Max(1, time);
            var pool = t >= 6 ? new List<CardData> { raid.boss.minionA, raid.boss.minionA, raid.boss.minionB } : new List<CardData> { raid.boss.minionA };
            foreach (var a in raid.allies)
            {
                if (gameOver) yield break;
                if (a.KO || a.isRemote) continue;
                foreach (var x in a.minions) x.hp = x.maxHp;
                var can = pool;
                int spawn = t >= 7 ? 2 : 1;
                for (int i = 0; i < spawn && a.minions.Count < 3 && can.Count > 0; i++)
                {
                    var d = can[rng.Next(can.Count)];
                    a.minions.Add(MakeSim(d, raid.atkMul));
                    a.Log($"보스의 졸개 {d.name} 등장");
                }
                int heroDmg = 0;
                foreach (var m in a.minions.ToList())
                {
                    if (a.lane.Count > 0)
                    {
                        var target = a.lane.OrderBy(x => x.hp <= m.atk ? 0 : 1).ThenByDescending(x => x.atk).First();
                        target.hp -= m.atk;
                        m.hp -= target.atk;
                        if (target.hp <= 0) a.Log($"졸개 {m.data.name} → {target.data.name} 파괴");
                        a.lane.RemoveAll(x => x.hp <= 0);
                    }
                    else heroDmg += m.atk;
                }
                a.minions.RemoveAll(x => x.hp <= 0);
                if (heroDmg > 0) AllyHit(a, heroDmg, "졸개 공격");
                if (a.KO) hud.RaidToast($"팀원 {a.name} 쓰러짐!");
            }
            RefreshAll();
            yield return Wait(0.2f);
        }

        // ── 테스트 ──
        public IEnumerator TestAlliesAct() => AlliesAct();
        public IEnumerator TestBossVsAllies() => BossVsAllies();
        public IEnumerator TestBossTimeStart() => BossTimeStart();
    }
}
