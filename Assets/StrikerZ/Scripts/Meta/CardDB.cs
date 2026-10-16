using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>id → CardData 조회. CardList 의 모든 리스트를 합친다.</summary>
    public static class CardDB
    {
        static Dictionary<string, CardData> byId;

        /// <summary>팩에서 나오고 덱에 넣을 수 있는 카드 (영웅 제외)</summary>
        public static List<CardData> Collectible { get; private set; }

        static void Ensure()
        {
            if (byId != null) return;
            Collectible = CardList.Monsters
                .Concat(CardList.MythicMonsters)
                .Concat(CardList.Magics)
                .Concat(CardList.Mythics)
                .Concat(CardList.Fields)
                .ToList();
            byId = new Dictionary<string, CardData>();
            foreach (var c in Collectible.Concat(CardList.Heroes)) byId[c.id] = c;
        }

        public static CardData Get(string id)
        {
            Ensure();
            return id != null && byId.TryGetValue(id, out var c) ? c : null;
        }

        public static List<CardData> All
        {
            get { Ensure(); return Collectible; }
        }

        /// <summary>도감/덱 편집 정렬: 종류 → 등급(높은 순) → id</summary>
        public static int SortKey(CardData c)
        {
            int t = c.type == CardType.Monster ? 0 : c.type == CardType.Magic ? 1 : 2;
            return t * 1000 + (10 - (int)c.rarity) * 100;
        }
    }
}
