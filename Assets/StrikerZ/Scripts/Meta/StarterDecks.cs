using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 원소 스타터 덱 (7속성): 그 속성의 Common·Uncommon 카드로 짠 40장 덱.
    /// 오팔로 한 번 구매하면 덱 카드 40장을 컬렉션에 받고, 같은 이름의 덱이 덱 목록에 생긴다.
    /// 구성: 소환수 20 (T1~2 9 · T3~4 7 · T5~7 4) · 마법 7 · 필드 3. 같은 카드는 2장까지 (구성이 단조롭지 않게).
    /// </summary>
    public static class StarterDecks
    {
        public const int Price = 400;
        /// <summary>스타터 엑스트라 덱 필드 수</summary>
        public const int ExtraFields = 6;
        const int Copies = 2;

        public static readonly Element[] Elements =
            { Element.Fire, Element.Water, Element.Wind, Element.Earth, Element.Thunder, Element.Light, Element.Dark };

        public static string Name(Element e) => $"{CardData.ElementName(e)} 스타터 덱";

        /// <summary>속성 대표 영웅 (스토리 영웅 제외, 그 속성 첫 영웅)</summary>
        public static CardData Hero(Element e)
            => CardList.Heroes.FirstOrDefault(h => h.element == e && !StoryData.IsStoryHero(h)) ?? CardList.Heroes[0];

        public static bool Bought(Element e) => Profile.Data.starters != null && Profile.Data.starters.Contains(e.ToString());

        static readonly Dictionary<Element, List<CardData>> cache = new Dictionary<Element, List<CardData>>();

        /// <summary>덱 카드 40장 (항상 같은 구성)</summary>
        public static List<CardData> Cards(Element e)
        {
            if (cache.TryGetValue(e, out var c)) return c;
            var deck = new List<CardData>();
            bool Low(CardData d) => d.rarity <= Rarity.Uncommon;
            var mons = CardDB.All.Where(d => d.type == CardType.Monster && Low(d))
                .OrderByDescending(d => d.element == e ? 1 : 0).ThenByDescending(d => (int)d.rarity).ThenByDescending(d => d.atk + d.hp).ThenBy(d => d.id).ToList();
            void Fill(IEnumerable<CardData> pool, int n)
            {
                int added = 0;
                foreach (var d in pool)
                    for (int k = 0; k < Copies && added < n; k++)
                        if (deck.Count(x => x == d) < Copies) { deck.Add(d); added++; }
            }
            // 속성 소환수 먼저, 모자라면 무속성
            IEnumerable<CardData> Pool(int lo, int hi) => mons.Where(d => d.minTime >= lo && d.minTime <= hi && (d.element == e || d.element == Element.None));
            Fill(Pool(1, 2), 13);
            Fill(Pool(3, 4), 10);
            Fill(Pool(5, 7), 5);
            Fill(mons.Where(d => d.element == e || d.element == Element.None), DeckRules.Monsters - deck.Count);
            var magics = CardDB.All.Where(d => d.type == CardType.Magic && Low(d))
                .OrderByDescending(d => d.element == e ? 2 : d.element == Element.None ? 1 : 0).ThenByDescending(d => (int)d.rarity).ThenBy(d => d.id);
            Fill(magics, DeckRules.Magics);
            Fill(CardDB.All.Where(d => d.type == CardType.Magic && Low(d)), DeckRules.MinSize - deck.Count);
            Fill(mons, DeckRules.MinSize - deck.Count);
            // 엑스트라 덱: 이 덱 소환수에게 가장 유리한 필드 6종 (1장씩)
            var sample = deck.Where(d => d.type == CardType.Monster).Select(d => new CardInstance(d, null)).ToList();
            var fields = CardDB.All.Where(d => d.type == CardType.Field && Low(d))
                .OrderByDescending(f => sample.Sum(m => { var x = CardData.FieldDelta(f, m); return x.atk + x.hp + x.dice; })).ThenBy(f => f.id).Take(ExtraFields);
            deck.AddRange(fields);
            cache[e] = deck;
            return deck;
        }

        public static DeckData ToDeck(Element e)
        {
            var all = Cards(e);
            var d = new DeckData { name = Name(e), heroId = Hero(e).id,
                cards = all.Where(x => x.type != CardType.Field).Select(x => x.id).ToList(),
                extra = all.Where(x => x.type == CardType.Field).Select(x => x.id).Distinct().ToList() };
            DeckRules.SortCards(d);
            return d;
        }

        /// <summary>구매: 오팔 차감 → 카드 40장 지급 → 덱 추가 (이미 샀으면 false)</summary>
        public static bool Buy(Element e, out string msg)
        {
            var p = Profile.Data;
            if (p.starters == null) p.starters = new List<string>();
            if (Bought(e)) { msg = "이미 받은 스타터 덱입니다"; return false; }
            if (!Profile.Spend(Price)) { msg = "오팔이 부족합니다"; return false; }
            foreach (var c in Cards(e)) Profile.AddCard(c.id, 1, true);
            p.decks.Add(ToDeck(e));
            p.starters.Add(e.ToString());
            Profile.Save();
            msg = $"{Name(e)}를 받았습니다 — 덱 목록에 추가됨";
            return true;
        }
    }
}
