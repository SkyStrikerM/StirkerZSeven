using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 카드 업그레이드 (로지의 부모 로한·벨라의 마력부여실):
    /// 같은 등급 카드 77장을 넣으면 한 단계 위 등급 카드로 무작위 변환.
    ///   Common 77 → Uncommon 7 · Uncommon 77 → Rare 7 · Rare 77 → Unique 1
    /// 결과 카드는 그 등급의 수록 카드(최신 팩 풀) 중에서 모두 같은 확률, 중복 가능.
    /// 가공(Seven·StrikerZ) 사본을 넣어도 결과는 일반 카드다. 잠긴 사본·덱에서 쓰는 장수는 넣을 수 없다.
    /// </summary>
    public static class CardUpgrade
    {
        public const int Input = 77;

        public static readonly Rarity[] Sources = { Rarity.Common, Rarity.Uncommon, Rarity.Rare };

        public static Rarity Target(Rarity from) => from + 1;

        /// <summary>한 번에 받는 장수</summary>
        public static int Output(Rarity from) => from == Rarity.Rare ? 1 : 7;

        /// <summary>결과 후보: 최신 팩 수록 카드 중 그 등급 (영웅 제외)</summary>
        public static List<CardData> Pool(Rarity r)
            => CardDB.All.Where(BoosterPack.Latest.pool).Where(c => c.rarity == r && c.type != CardType.Hero).ToList();

        static readonly CardFinish[] FinishOrder = { CardFinish.Normal, CardFinish.Seven, CardFinish.StrikerZ };

        /// <summary>이 카드를 지금 넣을 수 있는 최대 장수 (덱 사용분 · 잠금 제외)</summary>
        public static int Spare(CardData c)
        {
            if (c == null || c.type == CardType.Hero) return 0;
            int unlocked = FinishOrder.Where(f => !Crafting.IsLocked(c.id, f)).Sum(f => Profile.FinishCount(c.id, f));
            return Math.Max(0, Math.Min(unlocked, Profile.Owned(c.id) - Crafting.UsedInDecks(c.id)));
        }

        /// <summary>한 장 더 넣을 때 쓸 사본의 가공 (일반부터). 더 못 넣으면 null</summary>
        public static CardFinish? NextFinish(CardData c, Dictionary<(string id, CardFinish f), int> picks)
        {
            int total = FinishOrder.Sum(f => Picked(picks, c.id, f));
            if (total >= Spare(c)) return null;
            foreach (var f in FinishOrder)
                if (!Crafting.IsLocked(c.id, f) && Picked(picks, c.id, f) < Profile.FinishCount(c.id, f)) return f;
            return null;
        }

        /// <summary>뺄 때는 넣은 것 중 가장 귀한 가공 사본부터 (실수로 넣은 가공 카드를 먼저 돌려준다)</summary>
        public static CardFinish? LastFinish(CardData c, Dictionary<(string id, CardFinish f), int> picks)
        {
            for (int i = FinishOrder.Length - 1; i >= 0; i--)
                if (Picked(picks, c.id, FinishOrder[i]) > 0) return FinishOrder[i];
            return null;
        }

        public static int Picked(Dictionary<(string id, CardFinish f), int> picks, string id, CardFinish f)
            => picks.TryGetValue((id, f), out var n) ? n : 0;

        /// <summary>자동 채우기: 덱 최대 장수를 넘는 남는 일반 사본부터 (Crafting.Surplus 와 같은 기준)</summary>
        public static int AutoFill(Rarity from, Dictionary<(string id, CardFinish f), int> picks)
        {
            int have = picks.Values.Sum();
            foreach (var (card, finish, n) in Crafting.Surplus().Where(s => s.card.rarity == from).OrderBy(s => s.finish))
            {
                if (have >= Input) break;
                int already = Picked(picks, card.id, finish);
                int add = Math.Min(n - already, Input - have);
                if (add <= 0) continue;
                picks[(card.id, finish)] = already + add;
                have += add;
            }
            return have;
        }

        /// <summary>업그레이드 실행: 넣은 사본을 없애고 결과 카드(일반)를 지급. (카드, 새 카드인지) 목록 반환</summary>
        public static bool Run(Rarity from, Dictionary<(string id, CardFinish f), int> picks, Random rng, out List<(CardData card, bool isNew)> result, out string why)
        {
            result = null; why = null;
            if (from == Rarity.Unique) { why = "Unique 카드는 더 올릴 수 없습니다"; return false; }
            if (picks.Values.Sum() != Input) { why = $"카드 {Input}장을 넣어야 합니다"; return false; }
            foreach (var kv in picks)
            {
                var c = CardDB.Get(kv.Key.id);
                if (c == null || c.rarity != from) { why = "다른 등급 카드가 섞여 있습니다"; return false; }
                if (Crafting.IsLocked(c.id, kv.Key.f)) { why = $"{c.name} 은(는) 잠겨 있습니다"; return false; }
                if (kv.Value > Profile.FinishCount(c.id, kv.Key.f)) { why = $"{c.name} 이(가) 부족합니다"; return false; }
            }
            foreach (var g in picks.GroupBy(kv => kv.Key.id))
                if (g.Sum(kv => kv.Value) > Spare(CardDB.Get(g.Key))) { why = $"{CardDB.Get(g.Key).name} 은(는) 덱에서 쓰고 있어 그만큼 넣을 수 없습니다"; return false; }
            var pool = Pool(Target(from));
            if (pool.Count == 0) { why = "바꿀 수 있는 카드가 없습니다"; return false; }

            foreach (var kv in picks.Where(kv => kv.Value > 0)) Profile.RemoveCard(kv.Key.id, kv.Value, kv.Key.f);
            result = new List<(CardData, bool)>();
            for (int i = 0; i < Output(from); i++)
            {
                var c = pool[rng.Next(pool.Count)];
                bool isNew = Profile.Owned(c.id) == 0;
                Profile.AddCard(c.id, 1, true, CardFinish.Normal);
                result.Add((c, isNew));
            }
            Profile.Save();
            return true;
        }
    }
}
