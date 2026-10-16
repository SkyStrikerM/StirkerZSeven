using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrikerZ
{
    /// <summary>
    /// 카드 분해 · 생성 (마스터듀얼 방식).
    /// 등급별 제작 포인트(CP)를 따로 모은다. 분해: 일반 Common 3 · Uncommon 5 · Rare 10 · Unique 10, Seven ×3, StrikerZ ×10.
    /// 생성: 같은 등급 CP 30 → 일반 가공 1장. 잠긴 사본과 덱에서 쓰는 사본은 분해할 수 없다.
    /// </summary>
    public static class Crafting
    {
        public const int CraftCost = 30;
        static readonly int[] DismantleBase = { 3, 5, 10, 10 };   // Common Uncommon Rare Unique

        public static int FinishMultiplier(CardFinish f) => f == CardFinish.StrikerZ ? 10 : f == CardFinish.Seven ? 3 : 1;
        public static int DismantleValue(CardData c, CardFinish f) => DismantleBase[(int)c.rarity] * FinishMultiplier(f);

        static List<int> CpList
        {
            get
            {
                var d = Profile.Data;
                if (d.cp == null) d.cp = new List<int>();
                while (d.cp.Count < 4) d.cp.Add(0);
                return d.cp;
            }
        }

        public static int CP(Rarity r) => CpList[(int)r];
        public static void AddCP(Rarity r, int n) => CpList[(int)r] += n;

        /// <summary>"Rare 10" (등급 이름은 등급 색)</summary>
        public static string CpLabel(Rarity r) => CardData.RarityRich(r, $" {CP(r)}");

        public static string CpText() => string.Join("  ", Enum.GetValues(typeof(Rarity)).Cast<Rarity>().Select(CpLabel));

        // ── 잠금 ──

        static List<string> Locks
        {
            get
            {
                var d = Profile.Data;
                if (d.locks == null) d.locks = new List<string>();
                return d.locks;
            }
        }

        static string LockKey(string id, CardFinish f) => id + ":" + f;
        public static bool IsLocked(string id, CardFinish f) => Locks.Contains(LockKey(id, f));

        public static void SetLock(string id, CardFinish f, bool on)
        {
            var k = LockKey(id, f);
            if (on && !Locks.Contains(k)) Locks.Add(k);
            if (!on) Locks.Remove(k);
            Profile.Save();
        }

        /// <summary>가공 카드를 얻으면 자동 잠금 (설정: 자동 잠금 켜짐일 때)</summary>
        public static void OnObtained(string id, CardFinish f)
        {
            if (f != CardFinish.Normal && Profile.Data.autoLockFinish && !Locks.Contains(LockKey(id, f))) Locks.Add(LockKey(id, f));
        }

        // ── 분해 ──

        /// <summary>모든 덱 중 이 카드를 가장 많이 쓰는 장수</summary>
        public static int UsedInDecks(string id) => Profile.Data.decks.Count == 0 ? 0 : Profile.Data.decks.Max(d => d.Count(id));

        /// <summary>이 가공으로 지금 분해할 수 있는 장수 (잠금 · 덱 사용분 제외)</summary>
        public static int Dismantlable(CardData c, CardFinish f)
        {
            if (c == null || c.type == CardType.Hero || IsLocked(c.id, f)) return 0;
            int have = Profile.FinishCount(c.id, f);
            int spare = Profile.Owned(c.id) - UsedInDecks(c.id);
            return Math.Max(0, Math.Min(have, spare));
        }

        public static bool Dismantle(CardData c, CardFinish f, int n, out int gained, out string why)
        {
            gained = 0; why = null;
            if (n <= 0) { why = "분해할 카드가 없습니다"; return false; }
            if (IsLocked(c.id, f)) { why = "잠긴 카드는 분해할 수 없습니다"; return false; }
            if (Dismantlable(c, f) < n) { why = "덱에서 사용 중인 카드는 분해할 수 없습니다"; return false; }
            Profile.RemoveCard(c.id, n, f);
            gained = DismantleValue(c, f) * n;
            AddCP(c.rarity, gained);
            Profile.Save();
            return true;
        }

        // ── 생성 ──

        public static bool CanCraft(CardData c, out string why)
        {
            why = null;
            if (c == null || c.type == CardType.Hero) { why = "생성할 수 없는 카드입니다"; return false; }
            if (Profile.Owned(c.id) >= DeckRules.MaxCopies(c)) { why = $"이미 최대 장수({DeckRules.MaxCopies(c)}장)를 보유하고 있습니다"; return false; }
            if (CP(c.rarity) < CraftCost) { why = $"{CardData.RarityLabel(c.rarity)} CP 가 부족합니다 ({CP(c.rarity)}/{CraftCost})"; return false; }
            return true;
        }

        public static bool Craft(CardData c, out string why)
        {
            if (!CanCraft(c, out why)) return false;
            AddCP(c.rarity, -CraftCost);
            Profile.AddCard(c.id, 1, true);
            Quests.Add(Stat.Crafted);
            Profile.Save();
            return true;
        }

        // ── 초과분 일괄 분해 ──

        /// <summary>덱 최대 장수(또는 덱 사용분)를 넘는 잠기지 않은 사본: 일반 → Seven → StrikerZ 순으로</summary>
        public static List<(CardData card, CardFinish finish, int n)> Surplus()
        {
            var list = new List<(CardData, CardFinish, int)>();
            foreach (var e in Profile.Data.collection)
            {
                var c = CardDB.Get(e.id);
                if (c == null || c.type == CardType.Hero) continue;
                int keep = Math.Max(DeckRules.MaxCopies(c), UsedInDecks(c.id));
                int extra = e.n - keep;
                foreach (var f in new[] { CardFinish.Normal, CardFinish.Seven, CardFinish.StrikerZ })
                {
                    if (extra <= 0) break;
                    if (IsLocked(c.id, f)) continue;
                    int k = Math.Min(extra, Profile.FinishCount(c.id, f));
                    if (k > 0) { list.Add((c, f, k)); extra -= k; }
                }
            }
            return list;
        }

        public static int SurplusValue(IEnumerable<(CardData card, CardFinish finish, int n)> s) => s.Sum(x => DismantleValue(x.card, x.finish) * x.n);

        public static int DismantleSurplus()
        {
            int total = 0;
            foreach (var (c, f, n) in Surplus())
            {
                Profile.RemoveCard(c.id, n, f);
                int v = DismantleValue(c, f) * n;
                AddCP(c.rarity, v);
                total += v;
            }
            Profile.Save();
            return total;
        }

        // ── 덱 코드 ──

        /// <summary>덱 코드: "SZ1-" + Base64("영웅|M001*3,S005*2,...")</summary>
        public static string ExportDeck(DeckData d)
        {
            var body = d.heroId + "|" + string.Join(",", d.cards.Concat(d.extra ?? new List<string>()).GroupBy(x => x).OrderBy(g => g.Key).Select(g => g.Count() > 1 ? $"{g.Key}*{g.Count()}" : g.Key));
            return "SZ1-" + Convert.ToBase64String(Encoding.UTF8.GetBytes(body));
        }

        /// <summary>덱 코드 해석. 알 수 없는 카드는 무시. 실패 시 null</summary>
        public static DeckData ParseDeck(string code, string name)
        {
            try
            {
                code = (code ?? "").Trim();
                if (!code.StartsWith("SZ1-")) return null;
                var body = Encoding.UTF8.GetString(Convert.FromBase64String(code.Substring(4)));
                var parts = body.Split('|');
                var d = new DeckData { name = name, heroId = CardDB.Get(parts[0]) != null ? parts[0] : "H001" };
                if (parts.Length > 1 && parts[1].Length > 0)
                    foreach (var tok in parts[1].Split(','))
                    {
                        var kv = tok.Split('*');
                        int n = kv.Length > 1 && int.TryParse(kv[1], out int k) ? k : 1;
                        var cd = CardDB.Get(kv[0]);
                        if (cd == null) continue;
                        if (cd.type == CardType.Field) { if (!d.extra.Contains(cd.id) && d.extra.Count < DeckRules.MaxExtra) d.extra.Add(cd.id); continue; }
                        for (int i = 0; i < Math.Min(n, 3); i++) d.cards.Add(kv[0]);
                    }
                return d;
            }
            catch { return null; }
        }

        /// <summary>덱을 완성하려면 더 필요한 카드 (id, 장수)</summary>
        public static List<(CardData card, int n)> Missing(DeckData d)
            => d.cards.Concat(d.extra ?? new List<string>()).GroupBy(x => x).Select(g => (CardDB.Get(g.Key), g.Count() - Profile.Owned(g.Key))).Where(x => x.Item1 != null && x.Item2 > 0).ToList();

        /// <summary>부족한 카드를 CP 로 전부 생성할 수 있는지, 등급별 필요 CP</summary>
        public static bool CanCraftAll(List<(CardData card, int n)> missing, out string need)
        {
            var req = missing.GroupBy(m => m.card.rarity).Select(g => (r: g.Key, cp: g.Sum(x => x.n) * CraftCost)).ToList();
            need = string.Join(" · ", req.Select(x => $"{CardData.RarityLabel(x.r)} {x.cp}"));
            return req.All(x => CP(x.r) >= x.cp);
        }

        public static void CraftAll(List<(CardData card, int n)> missing)
        {
            foreach (var (c, n) in missing)
                for (int i = 0; i < n; i++)
                {
                    AddCP(c.rarity, -CraftCost);
                    Profile.AddCard(c.id, 1, true);
                    Quests.Add(Stat.Crafted);
                }
            Profile.Save();
        }
    }
}
