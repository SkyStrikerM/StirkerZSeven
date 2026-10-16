using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    [Serializable]
    public class SpecialOffer
    {
        public string id;
        public Rarity rarity;
        public CardFinish finish;
        public bool sold;
    }

    [Serializable]
    public class ShopSave
    {
        /// <summary>특가가 정해진 날짜 (yyyy-MM-dd, 로컬 시간)</summary>
        public string day = "";
        public List<SpecialOffer> offers = new List<SpecialOffer>();
    }

    /// <summary>
    /// 로지의 상점: 오늘의 특가(낱장)와 마력부여기.
    /// 특가: 매일 Common · Uncommon · Rare 1장씩, 같은 등급 CP 로 생성 비용의 절반. 가공 확률은 팩과 같고(Seven 10% · StrikerZ 1%) 가공비도 절반.
    /// 마력부여기: 같은 등급 CP 1,000 → Seven, 10,000 → StrikerZ.
    /// </summary>
    public static class ShopDaily
    {
        public static readonly Rarity[] SlotRarities = { Rarity.Common, Rarity.Uncommon, Rarity.Rare };
        public const int SevenEnchantCost = 1000, StrikerZEnchantCost = 10000;

        public static int EnchantCost(CardFinish f) => f == CardFinish.StrikerZ ? StrikerZEnchantCost : f == CardFinish.Seven ? SevenEnchantCost : 0;
        public static int CardPrice => Crafting.CraftCost / 2;
        public static int FinishPrice(CardFinish f) => EnchantCost(f) / 2;
        public static int Price(SpecialOffer o) => CardPrice + FinishPrice(o.finish);

        static string Today => DailyClock.Today;

        static ShopSave Save
        {
            get
            {
                var d = Profile.Data;
                if (d.shop == null) d.shop = new ShopSave();
                return d.shop;
            }
        }

        /// <summary>오늘의 특가 4장. 날짜가 바뀌었으면 새로 뽑는다</summary>
        public static List<SpecialOffer> Offers
        {
            get
            {
                var s = Save;
                if (s.day != Today || s.offers == null || s.offers.Count != SlotRarities.Length) Roll(new Random());
                return s.offers;
            }
        }

        public static void Roll(Random rng, CardFinish[] forceFinish = null)
        {
            var s = Save;
            s.day = Today;
            s.offers = new List<SpecialOffer>();
            for (int i = 0; i < SlotRarities.Length; i++)
            {
                var r = SlotRarities[i];
                var pool = CardDB.All.Where(c => c.type != CardType.Hero && c.rarity == r).ToList();
                if (pool.Count == 0) continue;
                var fin = forceFinish != null && i < forceFinish.Length ? forceFinish[i] : BoosterPack.RollFinish(rng);
                s.offers.Add(new SpecialOffer { id = pool[rng.Next(pool.Count)].id, rarity = r, finish = fin });
            }
            Profile.Save();
        }

        public static TimeSpan UntilReset => DailyClock.UntilReset;

        public static bool CanBuy(SpecialOffer o, out string why)
        {
            why = null;
            if (o == null || CardDB.Get(o.id) == null) { why = "판매하지 않는 카드입니다"; return false; }
            if (o.sold) { why = "오늘은 이미 품절이에요"; return false; }
            int price = Price(o);
            if (Crafting.CP(o.rarity) < price) { why = $"{CardData.RarityLabel(o.rarity)} CP 가 부족합니다 ({Crafting.CP(o.rarity)}/{price})"; return false; }
            return true;
        }

        public static bool Buy(SpecialOffer o, out string why)
        {
            if (!CanBuy(o, out why)) return false;
            Crafting.AddCP(o.rarity, -Price(o));
            Profile.AddCard(o.id, 1, true, o.finish);
            if (o.finish == CardFinish.Seven) Quests.Add(Stat.SevenGot);
            if (o.finish == CardFinish.StrikerZ) Quests.Add(Stat.StrikerZGot);
            o.sold = true;
            Profile.Save();
            return true;
        }

        // ── 마력부여기 ──

        /// <summary>가공할 사본: Seven 은 일반 사본에서, StrikerZ 는 일반 사본(없으면 Seven 사본)에서</summary>
        public static CardFinish? EnchantSource(CardData c, CardFinish target)
        {
            if (c == null || c.type == CardType.Hero || target == CardFinish.Normal) return null;
            if (Profile.FinishCount(c.id, CardFinish.Normal) > 0) return CardFinish.Normal;
            if (target == CardFinish.StrikerZ && Profile.FinishCount(c.id, CardFinish.Seven) > 0) return CardFinish.Seven;
            return null;
        }

        public static bool CanEnchant(CardData c, CardFinish target, out string why)
        {
            why = null;
            if (c == null || c.type == CardType.Hero) { why = "가공할 수 없는 카드입니다"; return false; }
            if (EnchantSource(c, target) == null)
            {
                why = target == CardFinish.Seven ? "가공되지 않은 사본이 없습니다" : "일반 또는 Seven 사본이 없습니다";
                return false;
            }
            int cost = EnchantCost(target);
            if (Crafting.CP(c.rarity) < cost) { why = $"{CardData.RarityLabel(c.rarity)} CP 가 부족합니다 ({Crafting.CP(c.rarity):N0}/{cost:N0})"; return false; }
            return true;
        }

        public static bool Enchant(CardData c, CardFinish target, out string why)
        {
            if (!CanEnchant(c, target, out why)) return false;
            var from = EnchantSource(c, target).Value;
            Crafting.AddCP(c.rarity, -EnchantCost(target));
            Profile.ChangeFinish(c.id, from, target);
            if (target == CardFinish.Seven) Quests.Add(Stat.SevenGot);
            else Quests.Add(Stat.StrikerZGot);
            Profile.Save();
            return true;
        }
    }
}
