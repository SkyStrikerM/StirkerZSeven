using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 카드 효과를 조립하는 헬퍼. CardList 에서 effect = Effects.Draw(2) 처럼 사용.
    /// 직접 IEnumerator 메서드를 만들어 effect 에 넣어도 된다.
    /// </summary>
    public static class Effects
    {
        public static Func<GameManager, CardInstance, IEnumerator> Seq(params Func<GameManager, CardInstance, IEnumerator>[] list)
            => (g, c) => SeqRoutine(g, c, list);

        static IEnumerator SeqRoutine(GameManager g, CardInstance c, Func<GameManager, CardInstance, IEnumerator>[] list)
        {
            foreach (var e in list) yield return e(g, c);
        }

        /// <summary>즉시 실행되는 간단한 효과</summary>
        public static Func<GameManager, CardInstance, IEnumerator> Do(Action<GameManager, CardInstance> action)
            => (g, c) => DoRoutine(g, c, action);

        static IEnumerator DoRoutine(GameManager g, CardInstance c, Action<GameManager, CardInstance> action)
        {
            action(g, c);
            g.RefreshAll();
            yield return g.Wait(0.3f);
        }

        public static Func<GameManager, CardInstance, IEnumerator> Draw(int n) => (g, c) => g.Draw(c.owner, n);
        public static Func<GameManager, CardInstance, IEnumerator> Heal(int n) => (g, c) => g.Heal(c.owner, n);
        public static Func<GameManager, CardInstance, IEnumerator> Burn(int n) => (g, c) => g.DamagePlayer(c.owner.opponent, n, c.Name);

        /// <summary>상대 몬스터 count 장 파괴 (선택)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> DestroyEnemyMonster(int count = 1)
            => (g, c) => DestroyEnemyRoutine(g, c, count);

        static IEnumerator DestroyEnemyRoutine(GameManager g, CardInstance c, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var targets = c.owner.opponent.Monsters();   // [불변] 도 파괴는 된다
                if (targets.Count == 0) yield break;
                CardInstance chosen = null;
                yield return g.ChooseCard(c.owner, targets, $"[{c.Name}] 파괴할 상대 몬스터를 선택", t => g.EffectiveAtk(t), r => chosen = r);
                if (chosen != null) yield return g.DestroyCard(chosen, c.Name);
            }
        }

        public static Func<GameManager, CardInstance, IEnumerator> AddStatus(StatusKind kind, Expire expire, bool onOpponent = false, bool onBoth = false, int value = 0)
            => Do((g, c) => g.AddStatus(new Status
            {
                kind = kind,
                expire = expire,
                value = value,
                caster = c.owner,
                target = onBoth ? null : (onOpponent ? c.owner.opponent : c.owner),
                source = c.Name
            }));

        /// <summary>아군 몬스터 ATK +n</summary>
        public static Func<GameManager, CardInstance, IEnumerator> BuffAllies(int n, Expire expire = Expire.EndOfTime)
            => AddStatus(StatusKind.AtkBonus, expire, value: n);

        /// <summary>묘지에서 몬스터 monsters 장, 마법 magics 장을 패로</summary>
        public static Func<GameManager, CardInstance, IEnumerator> ReturnFromGrave(int monsters, int magics)
            => (g, c) => ReturnRoutine(g, c, monsters, magics);

        static IEnumerator ReturnRoutine(GameManager g, CardInstance c, int monsters, int magics)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool wantMonster = pass == 0;
                int n = wantMonster ? monsters : magics;
                for (int i = 0; i < n; i++)
                {
                    var options = c.owner.grave.Where(x => x != c && (wantMonster ? x.IsMonster : x.IsMagic)).ToList();
                    if (options.Count == 0) break;
                    CardInstance chosen = null;
                    yield return g.ChooseFromList(c.owner, options, $"[{c.Name}] 묘지에서 패로 가져올 {(wantMonster ? "몬스터" : "마법")}", x => wantMonster ? x.data.atk : (int)x.data.rarity, r => chosen = r);
                    if (chosen != null) yield return g.GraveToHand(chosen);
                }
            }
        }

        /// <summary>아군 몬스터 1장: 공격 순서를 가장 앞으로 + 추가 공격</summary>
        public static Func<GameManager, CardInstance, IEnumerator> Haste(int extraPriority, int extraAttacks)
            => (g, c) => HasteRoutine(g, c, extraPriority, extraAttacks);

        /// <summary>상대 몬스터 1장(선택)에게 n 데미지. HP 가 0 이 되면 파괴</summary>
        public static Func<GameManager, CardInstance, IEnumerator> StrikeEnemy(int n) => (g, c) => StrikeRoutine(g, c, n);

        static IEnumerator StrikeRoutine(GameManager g, CardInstance c, int n)
        {
            var targets = g.EffectTargets(c.owner.opponent);
            if (targets.Count == 0) yield break;
            CardInstance chosen = null;
            // AI: 쓰러뜨릴 수 있는 몬스터 중 ATK 가 높은 것, 없으면 ATK 가 가장 높은 것
            yield return g.ChooseCard(c.owner, targets, $"[{c.Name}] {n} 데미지를 줄 상대 몬스터를 선택",
                t => (g.CurHp(t) <= n ? 100f : 0f) + g.EffectiveAtk(t), r => chosen = r);
            if (chosen != null) yield return g.DamageMonster(chosen, n, c.Name);
        }

        /// <summary>상대 몬스터 전체에 n 데미지</summary>
        public static Func<GameManager, CardInstance, IEnumerator> DamageAllEnemies(int n) => (g, c) => QuakeRoutine(g, c, n);

        static IEnumerator QuakeRoutine(GameManager g, CardInstance c, int n)
        {
            foreach (var t in g.EffectTargets(c.owner.opponent))
                yield return g.DamageMonster(t, n, c.Name);
        }

        /// <summary>이번 타임 아군 몬스터 전체 HP +n</summary>
        public static Func<GameManager, CardInstance, IEnumerator> FortifyAllies(int n)
            => Do((g, c) =>
            {
                foreach (var m in c.owner.Monsters()) g.AddHpBuff(m, n, 1);
                g.Log($"{c.Name}: 이번 타임 아군 몬스터 HP +{n}");
            });

        /// <summary>상대의 가장 앞(왼쪽) 소환수에게 n 데미지 (동시 룰용 — 대상 선택 없음)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> StrikeFront(int n) => (g, c) => StrikeFrontRoutine(g, c, n);

        static IEnumerator StrikeFrontRoutine(GameManager g, CardInstance c, int n)
        {
            var t = g.EffectTargets(c.owner.opponent).OrderBy(m => m.slot).FirstOrDefault();
            if (t == null) { g.Log($"{c.Name}: 대상이 없다"); yield break; }
            yield return g.DamageMonster(t, n, c.Name);
        }

        /// <summary>아군 가장 앞(왼쪽) 소환수 이번 타임 ATK +n</summary>
        public static Func<GameManager, CardInstance, IEnumerator> BuffFront(int n)
            => Do((g, c) =>
            {
                var t = c.owner.Monsters().OrderBy(m => m.slot).FirstOrDefault();
                if (t == null) { g.Log($"{c.Name}: 강화할 아군 소환수가 없다"); return; }
                t.tempAtk += n;
                g.Log($"{t.Name}: 이번 타임 ATK +{n}");
                if (g.fx != null) g.fx.AtkChange(t, n);
                g.RefreshAll();
            });

        /// <summary>상대 소환수 count 장(ATK 높은 순, front=가장 앞부터)의 ATK 를 이번 T 배틀 엔드까지 -n ([불변] 제외)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> WeakenEnemies(int count, int n, bool front = false)
            => Do((g, c) =>
            {
                var pool = g.EffectTargets(c.owner.opponent);
                var list = (front ? pool.OrderBy(m => m.slot) : pool.OrderByDescending(g.EffectiveAtk)).Take(count).ToList();
                if (list.Count == 0) { g.Log($"{c.Name}: 약화할 상대 소환수가 없다"); return; }
                foreach (var m in list) g.WeakenUntilBattleEnd(m, n, c.Name);
            });

        /// <summary>개기일식: 필드의 앞면 소환수를 모두 뒷면으로 ([불변] 제외)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> Eclipse() => Do((g, c) => g.FlipAllFaceDown());

        /// <summary>상대 앞면 소환수 n장(ATK 높은 순)을 뒷면으로 ([불변] 제외)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> VeilEnemies(int n)
            => Do((g, c) =>
            {
                var list = g.EffectTargets(c.owner.opponent).Where(m => !m.faceDown).OrderByDescending(g.EffectiveAtk).Take(n).ToList();
                foreach (var m in list) m.faceDown = true;
                g.Log($"{c.Name}: 상대 소환수 {list.Count}장을 뒷면으로");
                g.RefreshAll();
            });

        /// <summary>아군 몬스터 1장에게 이번 타임 [도발] 부여 (AI: ATK 가 가장 높은 몬스터)</summary>
        public static Func<GameManager, CardInstance, IEnumerator> GrantTaunt() => GrantTauntRoutine;

        static IEnumerator GrantTauntRoutine(GameManager g, CardInstance c)
        {
            var options = c.owner.Monsters().Where(m => !m.faceDown).ToList();
            if (options.Count == 0) { g.Log($"{c.Name}: 도발을 부여할 몬스터가 없다"); yield break; }
            CardInstance chosen = null;
            yield return g.ChooseCard(c.owner, options, $"[{c.Name}] 도발을 부여할 아군 몬스터를 선택", t => g.EffectiveAtk(t), r => chosen = r);
            if (chosen == null) yield break;
            chosen.tauntUntil = g.time;
            g.Log($"{chosen.Name}: 이번 타임 [도발]");
            if (g.fx != null) g.fx.Taunt(chosen);
            g.RefreshAll();
        }

        static IEnumerator HasteRoutine(GameManager g, CardInstance c, int extraPriority, int extraAttacks)
        {
            var options = c.owner.Monsters().Where(m => !m.faceDown).ToList();
            if (options.Count == 0) yield break;
            CardInstance chosen = null;
            yield return g.ChooseCard(c.owner, options, $"[{c.Name}] 강화할 아군 몬스터를 선택", t => g.EffectiveAtk(t), r => chosen = r);
            if (chosen == null) yield break;
            chosen.priorityMod += extraPriority;
            g.GrantExtraAttacks(chosen, extraAttacks);
            g.Log(extraPriority != 0 ? $"{chosen.Name}: 공격 순서 앞당김, 추가 공격 +{extraAttacks}" : $"{chosen.Name}: 이번 타임 추가 공격 +{extraAttacks}");
            g.RefreshAll();
        }
    }
}
