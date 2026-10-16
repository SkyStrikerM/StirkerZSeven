using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 신화 등급 마법 효과. (판타지 마스터즈 유니크 마법을 이 게임 규칙에 맞게 변형)
    /// 몬스터는 ATK만 있으므로 원작의 HP/DP 데미지는 파괴·ATK 감소·영웅 데미지로 바꿨다.
    /// </summary>
    public static class MythicEffects
    {
        const int Permanent = 999;

        // ── 조건 헬퍼 ──
        static bool Is(CardInstance c, Element e) => c.data.element == e || c.data.element == Element.Rainbow;
        public static bool HasElement(PlayerState p, Element e) => p.Monsters().Any(m => Is(m, e));
        public static bool OpponentHasField(GameManager g, CardInstance c) => c.owner.opponent.FieldCount > 0;
        public static bool AnyGraveMonster(GameManager g, CardInstance c)
            => c.owner.FreeSlot() >= 0 && c.owner.grave.Concat(c.owner.opponent.grave).Any(x => x.IsMonster);

        static List<CardInstance> LeftMonsters(PlayerState p, int n) => p.Monsters().OrderBy(m => m.slot).Take(n).ToList();

        // S101 저스티아
        public static IEnumerator Judgment(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            int light = me.hand.Concat(me.slots.Where(s => s != null)).Count(c => c != self && Is(c, Element.Light)) + 1;
            var magics = op.slots.Where(s => s != null && s.IsMagic).Take(light).ToList();
            foreach (var m in magics) yield return g.DestroyCard(m, self.Name);
            if (magics.Count == 0) { g.Log($"{self.Name}: 파괴할 마법이 없다"); yield break; }
            foreach (var t in g.EffectTargets(op).OrderByDescending(g.EffectiveAtk).Take(magics.Count).ToList())
                g.Stun(t, self.Name);
        }

        // S102 블래스트 플레어
        public static IEnumerator BurstFire(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            foreach (var t in op.Monsters().Where(t => g.EffectiveAtk(t) <= 4).ToList())
                yield return g.DestroyCard(t, self.Name);
            var back = me.Monsters().Where(m => Is(m, Element.Fire)).OrderByDescending(m => m.slot).FirstOrDefault();
            if (back != null) g.Stun(back, self.Name);
        }

        // S103 게헨나 플레임
        public static IEnumerator Hellfire(GameManager g, CardInstance self)
        {
            var op = self.owner.opponent;
            foreach (var t in LeftMonsters(op, 2)) g.AddAtkBuff(t, -3, 3);
            g.Log($"{self.Name}: 지옥불 — 3타임 동안 ATK -3");
            yield return g.DamagePlayer(op, 3, self.Name);
        }

        // S104 데스 사이즈
        public static IEnumerator DeathBlade(GameManager g, CardInstance self)
        {
            var targets = LeftMonsters(self.owner.opponent, 2).Where(t => t.data.atk <= 5).ToList();
            if (targets.Count == 0) g.Log($"{self.Name}: 조건을 만족하는 대상이 없다");
            foreach (var t in targets) yield return g.DestroyCard(t, self.Name);
        }

        // S105 세인트 리벤저
        public static IEnumerator HolyAvenger(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            var top = g.EffectTargets(op).OrderByDescending(g.EffectiveAtk).FirstOrDefault();
            if (top == null) yield break;
            int atk = g.EffectiveAtk(top);
            int cut = atk - (atk + 1) / 2;
            g.WeakenUntilBattleEnd(top, cut, self.Name, false);
            foreach (var m in me.Monsters()) g.AddAtkBuff(m, cut, 1);
            g.Log($"{self.Name}: {top.Name} ATK -{cut}, 아군 전체 ATK +{cut}");
            if (g.IsMonoElement(me)) yield return g.Heal(me, cut);
            yield return g.Wait(0.3f);
        }

        // S106 영광의 최후
        public static IEnumerator GloryWarrior(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            bool mono = g.IsMonoElement(me);
            for (int i = 0; i < 1; i++)   // 동시 룰 밸런스: 2장 → 1장
            {
                var options = op.Monsters();
                if (options.Count == 0) break;
                CardInstance pick = null;
                yield return g.ChooseCard(me, options, $"[{self.Name}] 파괴할 상대 몬스터", x => g.EffectiveAtk(x), r => pick = r, allowCancel: i > 0, cancelLabel: "그만");
                if (pick == null) break;
                yield return g.DestroyCard(pick, self.Name);
            }
            if (mono) { g.Log($"{self.Name}: 순속성 — 아군은 무사하다"); yield break; }
            var mine = me.Monsters().OrderByDescending(g.EffectiveAtk).FirstOrDefault();
            if (mine != null) yield return g.DestroyCard(mine, self.Name + " (영광의 죽음)");
        }

        // S107 천둥 포화
        public static IEnumerator Volley(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            var mine = LeftMonsters(me, 2);
            var theirs = LeftMonsters(op, 2);
            int kills = 0;
            for (int i = 0; i < mine.Count && i < theirs.Count; i++)
            {
                if (g.EffectiveAtk(mine[i]) >= g.EffectiveAtk(theirs[i]))
                {
                    yield return g.DestroyCard(theirs[i], $"{self.Name} ({mine[i].Name})");
                    kills++;
                }
                else g.Log($"{self.Name}: {mine[i].Name}의 사격이 {theirs[i].Name}에게 막혔다");
            }
            if (kills > 0 && g.IsMonoElement(me)) yield return g.DamagePlayer(op, kills * 3, self.Name);
        }

        // S108 세이렌의 눈물
        public static IEnumerator MermaidTear(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            int div = System.Math.Max(2, me.hand.Count(c => Is(c, Element.Water)));
            foreach (var t in LeftMonsters(op, 3))
            {
                int a = g.EffectiveAtk(t);
                g.WeakenUntilBattleEnd(t, a - a / div, self.Name, false);
            }
            g.Log($"{self.Name}: 상대 몬스터 ATK ÷{div}");
            var magic = op.slots.FirstOrDefault(s => s != null && s.IsMagic);
            if (magic != null) yield return g.ReturnToHand(magic, self.Name);
            yield return g.Wait(0.3f);
        }

        // S109 루미너스 보이드
        public static IEnumerator WhiteHole(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            var lights = me.Monsters().Where(m => Is(m, Element.Light)).OrderByDescending(m => m.slot).ToList();
            foreach (var l in lights) yield return g.ReturnToHand(l, self.Name);
            var targets = op.slots.Where(s => s != null)
                .OrderByDescending(s => s.IsMonster ? 1 : 0).ThenByDescending(g.EffectiveAtk)
                .Take(lights.Count).ToList();
            foreach (var t in targets) yield return g.ToDeck(t, self.Name);
        }

        // S110 테라 사이클론
        public static IEnumerator GigaWind(GameManager g, CardInstance self)
        {
            var me = self.owner;
            var mons = me.Monsters();
            int budget = mons.Count > 0 && mons.All(m => Is(m, Element.Wind)) ? 9 : 7;
            foreach (var t in me.opponent.Monsters().OrderBy(m => m.slot).ToList())
            {
                if (t.data.atk > budget) break;
                budget -= t.data.atk;
                yield return g.DestroyCard(t, self.Name);
            }
        }

        // S111 종말의 마차
        public static IEnumerator DoomChariot(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            var top = g.EffectTargets(op).OrderByDescending(g.EffectiveAtk).FirstOrDefault();
            if (top == null) yield break;
            int dmg = g.EffectiveAtk(top);
            yield return g.DamagePlayer(op, dmg, self.Name);
            if (!g.gameOver) yield return g.DamagePlayer(me, (dmg + 1) / 2, self.Name + " (반동)");
        }

        // S112 소울 레스큐
        public static IEnumerator LifeRescue(GameManager g, CardInstance self)
        {
            var me = self.owner;
            var light = me.Monsters().Where(m => Is(m, Element.Light)).OrderBy(m => m.slot).FirstOrDefault();
            if (light != null) yield return g.ReturnToHand(light, self.Name);
            var options = me.grave.Concat(me.opponent.grave).Where(x => x.IsMonster).ToList();
            if (options.Count == 0 || me.FreeSlot() < 0) yield break;
            CardInstance pick = null;
            yield return g.ChooseFromList(me, options, $"[{self.Name}] 구출할 몬스터 (양쪽 묘지)", x => x.data.atk, r => pick = r);
            if (pick == null) yield break;
            yield return g.SpecialSummon(pick, me, false, self.Name);
            g.AddAtkBuff(pick, 1, Permanent);
        }

        // ══════════════════ 신화 몬스터 소환 효과 (판타지 마스터즈 이카젠 등급 변형) ══════════════════

        static bool MonoOf(GameManager g, PlayerState p, Element e) => g.IsMonoElement(p) && p.Monsters().Any(m => m.data.element == e);

        // M101 엔시아나 — 상대 필드 마법 1장을 상대 패로
        public static IEnumerator ControlCard(GameManager g, CardInstance self)
        {
            var magic = self.owner.opponent.slots.FirstOrDefault(s => s != null && s.IsMagic);
            if (magic != null) yield return g.ReturnToHand(magic, self.Name);
        }

        // M105 무한의 성수 — 아군 필드 몬스터 수만큼 이번 타임 ATK 증가
        public static IEnumerator SacredBeast(GameManager g, CardInstance self)
        {
            int n = self.owner.Monsters().Count;
            g.AddAtkBuff(self, n, 1);
            g.Log($"{self.Name}: 위업 — 이번 타임 ATK +{n}");
            yield return g.Wait(0.3f);
        }

        // M106 카로넬리아 — 상대 필드 마법 1장 파괴
        public static IEnumerator MagicBreak(GameManager g, CardInstance self)
        {
            var magic = self.owner.opponent.slots.FirstOrDefault(s => s != null && s.IsMagic);
            if (magic != null) yield return g.DestroyCard(magic, self.Name);
        }

        // M109 판타즘 마스터 — 상대 몬스터 전체 배틀 엔드까지 ATK -2
        public static IEnumerator SkillBreak(GameManager g, CardInstance self)
        {
            foreach (var t in g.EffectTargets(self.owner.opponent)) g.WeakenUntilBattleEnd(t, 2, self.Name, false);
            g.Log($"{self.Name}: 기술 파괴 — 상대 몬스터 전체 ATK -2");
            yield return g.Wait(0.3f);
        }

        // M110 이세리브 — 상대 최고 ATK 몬스터 이번 타임 행동 불능
        public static IEnumerator HistoryRevenge(GameManager g, CardInstance self)
        {
            var top = g.EffectTargets(self.owner.opponent).OrderByDescending(g.EffectiveAtk).FirstOrDefault();
            if (top != null) g.Stun(top, self.Name);
            yield return g.Wait(0.3f);
        }

        // M112 오르브레크 — 양쪽 필드 마법 전부 파괴
        public static IEnumerator ChaosLord(GameManager g, CardInstance self)
        {
            var all = self.owner.slots.Concat(self.owner.opponent.slots).Where(s => s != null && s.IsMagic).ToList();
            foreach (var m in all) yield return g.DestroyCard(m, self.Name);
        }

        // ══════════════════ 신화 마법 (이카젠 등급 변형) ══════════════════

        static CardData FieldNamed(string name) => CardList.FieldNamed(name);

        // S201 신비의 심해 — 순속성(물): 묘지의 물 몬스터 최대 2장 특수 소환, 2장이면 필드를 '심해의 신전'으로
        public static bool AbyssCan(GameManager g, CardInstance c)
            => MonoOf(g, c.owner, Element.Water) && c.owner.FreeSlot() >= 0 && c.owner.grave.Any(x => x.IsMonster && Is(x, Element.Water));

        public static IEnumerator MysticAbyss(GameManager g, CardInstance self)
        {
            var me = self.owner;
            int n = 0;
            foreach (var m in me.grave.Where(x => x.IsMonster && Is(x, Element.Water)).OrderByDescending(x => x.data.atk).Take(2).ToList())
            {
                if (me.FreeSlot() < 0) break;
                yield return g.SpecialSummon(m, me, false, self.Name);
                n++;
            }
            var f = FieldNamed("심해의 신전");
            if (n >= 2 && f != null) yield return g.ChangeField(f, self.Name);
        }

        // S202 깊은 암야 — 순속성(어둠): 아군 어둠 몬스터 수만큼 상대 몬스터 ATK를 배틀 엔드까지 1로. 실패 시 상대 왼쪽 2장 행동 불능
        public static IEnumerator DeepNight(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            if (MonoOf(g, me, Element.Dark))
            {
                int n = me.Monsters().Count(m => Is(m, Element.Dark));
                foreach (var t in g.EffectTargets(op).OrderByDescending(g.EffectiveAtk).Take(n).ToList())
                    g.WeakenUntilBattleEnd(t, g.EffectiveAtk(t) - 1, self.Name, false);
                g.Log($"{self.Name}: 상대 몬스터 {n}장의 ATK가 1이 되었다");
            }
            else
            {
                foreach (var t in LeftMonsters(op, 2)) g.Stun(t, self.Name);
            }
            yield return g.Wait(0.3f);
        }

        // S203 들풀의 군세 — 순속성(바람): 패의 바람 몬스터 최대 2장 특수 소환, 2장이면 필드를 '천공의 정원'으로
        public static bool WildgrassCan(GameManager g, CardInstance c)
            => MonoOf(g, c.owner, Element.Wind) && c.owner.FreeSlot() >= 0 && c.owner.hand.Any(x => x.IsMonster && Is(x, Element.Wind));

        public static IEnumerator WildgrassLegion(GameManager g, CardInstance self)
        {
            var me = self.owner;
            int n = 0;
            foreach (var m in me.hand.Where(x => x.IsMonster && Is(x, Element.Wind)).OrderByDescending(x => x.data.atk).Take(2).ToList())
            {
                if (me.FreeSlot() < 0) break;
                yield return g.SpecialSummon(m, me, false, self.Name);
                n++;
            }
            var f = FieldNamed("천공의 정원");
            if (n >= 2 && f != null) yield return g.ChangeField(f, self.Name);
        }

        // S204 대지의 분노 — 순속성(대지): 대지 몬스터 최고 ATK×2(최대 20) 한도로 상대 몬스터를 왼쪽부터 파괴.
        //                   아군 몬스터가 없으면 패의 대지 몬스터 1장을 ATK+5 로 특수 소환
        public static bool EarthCan(GameManager g, CardInstance c)
            => MonoOf(g, c.owner, Element.Earth) || (c.owner.Monsters().Count == 0 && c.owner.hand.Any(x => x.IsMonster && Is(x, Element.Earth)));

        public static IEnumerator WrathOfEarth(GameManager g, CardInstance self)
        {
            var me = self.owner;
            if (MonoOf(g, me, Element.Earth))
            {
                int budget = System.Math.Min(14, me.Monsters().Where(m => Is(m, Element.Earth)).Max(m => g.MaxHp(m)));   // 대지 = 고HP 콘셉트: 최고 HP 만큼
                foreach (var t in LeftMonsters(me.opponent, 7))
                {
                    int a = g.EffectiveAtk(t);
                    if (a > budget) break;
                    budget -= a;
                    yield return g.DestroyCard(t, self.Name);
                }
            }
            else if (me.Monsters().Count == 0)
            {
                var m = me.hand.Where(x => x.IsMonster && Is(x, Element.Earth)).OrderByDescending(x => x.data.atk).FirstOrDefault();
                if (m == null) yield break;
                yield return g.SpecialSummon(m, me, false, self.Name);
                g.AddAtkBuff(m, 5, Permanent);
            }
        }

        // S205 인간의 긍지 — 순속성(번개): 아군 최고 ATK보다 기본 ATK가 높은 묘지 몬스터를 최근 순으로 빈칸만큼 특수 소환.
        //                   1장만 소환되면 그 몬스터는 이번 타임 행동 불능
        public static bool PrideCan(GameManager g, CardInstance c)
        {
            if (!MonoOf(g, c.owner, Element.Thunder) || c.owner.FreeSlot() < 0) return false;
            int top = c.owner.Monsters().Max(m => g.EffectiveAtk(m));
            return c.owner.grave.Any(x => x.IsMonster && x.data.atk > top);
        }

        public static IEnumerator PrideOfMankind(GameManager g, CardInstance self)
        {
            var me = self.owner;
            int top = me.Monsters().Count > 0 ? me.Monsters().Max(m => g.EffectiveAtk(m)) : 0;
            var list = me.grave.Where(x => x.IsMonster && x.data.atk > top).Reverse().ToList();
            var summoned = new List<CardInstance>();
            foreach (var m in list)
            {
                if (me.FreeSlot() < 0) break;
                yield return g.SpecialSummon(m, me, false, self.Name);
                summoned.Add(m);
            }
            if (summoned.Count == 1) g.Stun(summoned[0], self.Name + " (금욕)");
        }

        // S206 화염 군단 — 순속성(화염): (화염 몬스터 최고 ATK+4) 포인트로 상대 몬스터를 왼쪽부터 파괴, 남은 포인트의 1/3 만큼 상대 영웅 데미지
        public static IEnumerator FlameLegion(GameManager g, CardInstance self)
        {
            var me = self.owner;
            int points = me.Monsters().Where(m => Is(m, Element.Fire)).Max(m => g.EffectiveAtk(m)) + 2;
            foreach (var t in LeftMonsters(me.opponent, 7))
            {
                int a = g.EffectiveAtk(t);
                if (a > points) break;
                points -= a;
                yield return g.DestroyCard(t, self.Name);
            }
            if (points / 3 > 0) yield return g.DamagePlayer(me.opponent, points / 3, self.Name);
        }

        // S207 균형의 저울 — 순속성(빛): 양쪽 필드 카드 수, 패 수가 같아질 때까지 많은 쪽의 카드를 뒤(오른쪽)부터 묘지로
        public static bool BalanceCan(GameManager g, CardInstance c)
            => MonoOf(g, c.owner, Element.Light) && c.owner.hand.Count > 0 && c.owner.opponent.hand.Count > 0;

        public static IEnumerator ScalesOfBalance(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            while (me.FieldCount != op.FieldCount)
            {
                var big = me.FieldCount > op.FieldCount ? me : op;
                var last = big.slots.Last(s => s != null);
                yield return g.DestroyCard(last, self.Name);
            }
            while (me.hand.Count != op.hand.Count)
            {
                var big = me.hand.Count > op.hand.Count ? me : op;
                yield return g.DestroyCard(big.hand[big.hand.Count - 1], self.Name);
            }
        }

        // S208 앱솔루트 스펠 — 아군 몬스터가 없거나 속성이 2종류 이상이면: 양쪽 필드 마법 전부 파괴,
        //                     파괴한 상대 마법 수만큼 상대 몬스터를 오른쪽부터 패로
        public static bool AbsoluteCan(GameManager g, CardInstance c)
            => !g.IsMonoElement(c.owner) && c.owner.slots.Concat(c.owner.opponent.slots).Any(s => s != null && s.IsMagic);

        public static IEnumerator AbsoluteSpell(GameManager g, CardInstance self)
        {
            var me = self.owner; var op = me.opponent;
            int enemyMagics = op.slots.Count(s => s != null && s.IsMagic);
            foreach (var m in me.slots.Concat(op.slots).Where(s => s != null && s.IsMagic).ToList())
                yield return g.DestroyCard(m, self.Name);
            foreach (var t in g.EffectTargets(op).OrderByDescending(m => m.slot).Take(enemyMagics).ToList())
                yield return g.ReturnToHand(t, self.Name);
        }
    }
}
