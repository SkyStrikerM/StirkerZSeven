using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>간단한 휴리스틱 CPU</summary>
    public class AIController : MonoBehaviour
    {
        GameManager g;

        public void Init(GameManager gm) { g = gm; }

        /// <summary>
        /// T 시작 원소 고르기: 패에서 지금 보유 원소로 못 내는 카드의 부족분이 큰 속성 (덱에 많은 속성·무속성/무지개 수요 조금 반영),
        /// 이미 많이 쌓인 속성은 덜. 수련 CPU 는 절반쯤 아무거나.
        /// </summary>
        public Element ChooseElement(PlayerState p)
        {
            var opts = g.ElementOptions(p);
            if (opts.Count == 0) return Element.None;
            var lv = p.isHuman ? CpuLevel.Elite : g.cpuLevel;
            if (lv == CpuLevel.Trainee && Random.value < 0.5f) return opts[Random.Range(0, opts.Count)];
            Element best = Element.None; float bestScore = float.MinValue;
            foreach (var e in opts)
            {
                int pool = p.elemPool[(int)e];
                float demand = p.hand.Where(c => c.data.element == e).Sum(c => Mathf.Max(0, c.data.Cost - pool))
                               + p.deck.Count(c => c.data.element == e) * 0.15f
                               + p.hand.Count(c => c.data.element == Element.None && c.data.Cost > p.TotalAvailable) * 0.3f
                               + (pool == 0 ? p.hand.Count(c => c.data.element == Element.Rainbow) * 0.5f : 0);
                float score = demand - pool * 0.4f + Random.value * 0.05f;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        public IEnumerator MainPhase(PlayerState p)
        {
            yield return g.Wait(0.6f);

            // 1) 영웅 능력: HP 절반 이하 또는 중반 이후
            if (g.CanUseHero(p) && (p.hp <= p.maxHp / 2 || g.time >= 7 || (p.opponent.Monsters().Count >= 2 && p.hero.data.text.Contains("파괴"))))
                yield return g.UseHero(p);
            if (g.gameOver) yield break;

            // 2) 필드: 자기 몬스터에게 더 유리하면 교체
            var field = p.hand.Where(c => c.data.type == CardType.Field).OrderByDescending(FieldValue).FirstOrDefault();
            if (field != null && g.CanPlayField(field) && FieldValue(field) > (g.center != null ? FieldValue(g.center) : 0) + 1)
            {
                yield return g.PlayField(field);
                yield return g.Wait(0.3f);
            }

            // 3) 몬스터 소환 (ATK+HP 높은 순, [도발]은 앞쪽(왼쪽)에 먼저)
            while (!g.gameOver)
            {
                var mon = p.hand.Where(c => c.IsMonster && g.CanSummon(c, out _))
                    .OrderByDescending(c => c.data.taunt ? 1 : 0).ThenByDescending(c => c.data.atk + c.data.hp).FirstOrDefault();
                if (mon == null) break;
                bool faceDown = g.MustSummonFaceDown;
                if (faceDown && !g.CanSummonFaceDown(mon)) break;
                yield return g.Summon(mon, faceDown);
                yield return g.Wait(0.25f);
            }

            // 4) 뒷면 몬스터 반전
            foreach (var c in p.slots.Where(s => s != null && g.CanFlip(s)).ToList())
                yield return g.Flip(c);

            // 5) 마법은 칸에 놓아 둬야 배틀 때 그 칸 차례에 발동한다 — 소환수 자리를 남기고 2장까지
            yield return SetMagics(p);

            yield return g.Wait(0.4f);
        }

        /// <summary>마법을 칸에 뒷면으로 놓는다 (패에서는 발동할 수 없다). 소환수 칸을 남기려고 1~5번 칸이 비어 있을 때만, 2장까지</summary>
        IEnumerator SetMagics(PlayerState p)
        {
            foreach (var m in p.hand.Where(c => c.IsMagic).Take(2).ToList())
            {
                if (p.FreeSlot() < 0 || p.FreeSlot() >= 5 || !g.CanSet(m)) break;
                if (!WantsMagic(m, true)) continue;
                if (m.data.text.Contains("회복") && p.hp >= p.maxHp) continue;   // 회복은 다쳤을 때만 (칸 차례에 조건이 안 맞으면 불발이라 아낀다)
                yield return g.SetMagic(m);
            }
        }

        /// <summary>
        /// 동시 룰 배치: 카드는 모두 뒷면. 마법은 왼쪽(먼저 공개), 소환수는 [도발]·HP 높은 순으로 앞(왼쪽)에
        /// — 상대 공격은 가장 앞 소환수부터 받으므로 튼튼한 카드를 앞에 둔다.
        /// </summary>
        public IEnumerator PlanPhase(PlayerState p)
        {
            yield return g.Wait(0.3f);
            // 난이도: 수련 CPU 는 영웅 능력을 잘 아끼지 못하고(늦게·가끔), 카드를 다 내지 않으며, 배치를 정리하지 않는다
            var lv = p.isHuman ? CpuLevel.Elite : g.cpuLevel;
            // 공격 턴(이번 T 에 내 소환수가 왼쪽부터 공격) / 수비 턴(상대가 공격, 내 소환수는 반격만)
            bool attacking = !g.Simul || g.LaneFirst == p;
            bool heroNow = lv == CpuLevel.Legend
                ? (p.hp <= p.maxHp * 0.7f || g.time >= 4 || p.opponent.Monsters().Count >= 2)
                : (p.hp <= p.maxHp / 2 || g.time >= 5 || (p.opponent.Monsters().Count >= 2 && p.hero.data.text.Contains("파괴")));
            if (lv == CpuLevel.Trainee) heroNow = g.time >= 6 && Random.value < 0.5f;
            if (g.CanUseHero(p) && heroNow)
                yield return g.UseHero(p);
            if (g.gameOver) yield break;

            // 필드 차례면 엑스트라 덱에서 지금 필드보다 나은 필드를 전개 (수련은 가끔만)
            var field = p.extra.Where(g.CanPlayField).OrderByDescending(FieldValue).FirstOrDefault();
            if (field != null && FieldValue(field) > (g.center != null ? FieldValue(g.center) : 0) + 1 && (lv != CpuLevel.Trainee || Random.value < 0.5f))
            {
                yield return g.PlayField(field);
                yield return g.Wait(0.2f);
            }

            int placed = 0;
            while (!g.gameOver)
            {
                var mon = p.hand.Where(c => c.IsMonster && g.CanSummon(c, out _))
                    .OrderByDescending(c => c.data.atk + c.data.hp).FirstOrDefault();
                if (mon == null) break;
                if (lv == CpuLevel.Trainee && placed >= 2 && Random.value < 0.5f) break;   // 수련: 한 번에 다 내지 않는다
                yield return g.Summon(mon, true);
                placed++;
                yield return g.Wait(0.12f);
            }

            yield return SetMagics(p);   // 마법은 놓아 둬야 발동

            if (lv == CpuLevel.Trainee) { yield return g.Wait(0.3f); yield break; }   // 수련: 놓은 순서 그대로 (튼튼한 카드를 앞에 두지 않는다)
            List<CardInstance> order;
            if (attacking)
            {
                // 공격 턴: 강화 마법을 맨 앞(공격보다 먼저 발동) → 상대 앞 소환수를 쓰러뜨릴 센 공격수부터 (ATK 높은 순)
                order = p.slots.Where(s => s != null)
                    .OrderBy(s => s.IsMagic ? 0 : 1)
                    .ThenByDescending(s => s.IsMonster ? g.EffectiveAtk(s) * 2 + g.CurHp(s) : 0)
                    .ToList();
            }
            else
            {
                // 수비 턴: 공격은 가장 앞 소환수부터 받는다 → [도발] · HP 높은 벽을 맨 앞에, 약한 소환수는 뒤로.
                // 마법은 소환수 뒤에 두어 상대 공격 사이에 발동 (회복·방어)
                order = p.slots.Where(s => s != null)
                    .OrderBy(s => s.IsMagic ? 1 : 0)
                    .ThenByDescending(s => s.IsMonster && (s.data.taunt || g.IsTaunt(s)) ? 1 : 0)
                    .ThenByDescending(s => s.IsMonster ? g.CurHp(s) * 2 + g.EffectiveAtk(s) : 0)
                    .ToList();
            }
            g.ArrangeSlots(p, order);
            yield return g.Wait(0.3f);
        }

        /// <summary>
        /// 운명 재배치: 초반(T1~2)에 못 내는 무거운 소환수를 우선 돌려보낸다 (최대 3장).
        /// 소환수가 2장 이하면 마법도 1장 바꿔 소환수를 찾는다. 수련은 바꾸지 않는다.
        /// </summary>
        public List<CardInstance> FatePick(PlayerState p)
        {
            var pick = new List<CardInstance>();
            if (!p.isHuman && g.cpuLevel == CpuLevel.Trainee) return pick;
            pick.AddRange(p.hand.Where(c => c.IsMonster && c.data.minTime >= 3).OrderByDescending(c => c.data.minTime).Take(3));
            int early = p.hand.Count(c => c.IsMonster && c.data.minTime <= 2);
            if (early <= 2)
            {
                var m = p.hand.FirstOrDefault(c => c.IsMagic && !pick.Contains(c));
                if (m != null && pick.Count < 3) pick.Add(m);
            }
            return pick;
        }

        float FieldValue(CardInstance field)
        {
            var p = field.owner ?? g.Cpu;
            float V(CardInstance m) { var d = CardData.FieldDelta(field.data, m); return d.atk + d.hp * 0.8f + d.dice * 1.5f; }
            float mine = p.Monsters().Concat(p.hand.Where(c => c.IsMonster)).Sum(V);
            float theirs = p.opponent.Monsters().Sum(V);
            return mine - theirs;
        }

        /// <summary>자유 대상 공격 점수: 쓰러뜨릴 수 있고 반격에 살아남는 대상 우선</summary>
        public float AttackScore(CardInstance a, CardInstance t)
        {
            int myAtk = g.EffectiveAtk(a), myHp = g.CurHp(a);
            int tAtk = g.EffectiveAtk(t), tHp = g.CurHp(t);
            float s = 0;
            if (myAtk >= tHp) s += 5 + tAtk * 0.5f + (myAtk - tHp) * 0.3f;
            else s += myAtk * 0.3f;
            if (tAtk >= myHp) s -= 3;
            if (t.faceDown) s += 1;
            return s;
        }

        public bool WantsMagic(CardInstance m) => WantsMagic(m, false);

        /// <summary>엔드 페이즈에 패를 모두 버릴지: 곧 쓸 수 없는 카드가 절반 이상이고 덱이 넉넉할 때만</summary>
        public bool WantsDiscardHand(PlayerState p)
        {
            int n = p.hand.Count;
            if (n < 3 || p.deck.Count < n + 12) return false;
            int dead = p.hand.Count(c => c.IsMonster && c.data.minTime > g.time + 2);
            return dead * 2 >= n;
        }

        /// <summary>공격할 때만 쓸모 있는 강화 마법 (ATK 증가 · 관통 · 추가 공격)</summary>
        static bool IsAttackBuff(CardInstance m)
        {
            string t = m.data.text;
            return (t.Contains("ATK +") || t.Contains("관통") || t.Contains("추가 공격") || t.Contains("한 번 더 공격")) && !t.Contains("회복");
        }

        bool WantsMagic(CardInstance m, bool planning)
        {
            var p = m.owner;
            string t = m.data.text;
            if (t.Contains("파괴") && p.opponent.Monsters().Count == 0 && !planning) return false;
            if ((t.Contains("아군 몬스터") || t.Contains("아군 소환수")) && p.Monsters().Count == 0 && !planning) return false;
            if (t.Contains("뒷면") && p.Monsters().Count + p.opponent.Monsters().Count == 0 && !planning) return false;   // 개기일식: 뒤집을 소환수가 없으면 아낀다
            if (t.Contains("뒷면 세로 표시로만")) return p.opponent.hand.Count >= 3;
            return true;
        }

    }
}
