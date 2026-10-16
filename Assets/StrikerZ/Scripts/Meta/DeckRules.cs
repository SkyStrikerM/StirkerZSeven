using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>덱 구성 규칙</summary>
    public static class DeckRules
    {
        public const int MinSize = 40, MaxSize = 40;   // 2026-10-13: 덱 40장 (드로우가 T 번호만큼 늘어 30장은 T5~6 에 소진)
        /// <summary>엑스트라 덱 (필드) 최대 장수 · 같은 필드는 1장씩</summary>
        public const int MaxExtra = 10;
        public const int MaxMythicMonsters = 1;
        public const int MaxMythicMagics = 1;

        /// <summary>같은 카드 최대 장수 (등급별)</summary>
        public static int MaxCopies(CardData d)
        {
            switch (d.rarity)
            {
                case Rarity.Unique: return 1;
                case Rarity.Rare: return 2;
                default: return 3;
            }
        }

        public static int MythicCount(DeckData deck, CardType type)
            => deck.cards.Select(CardDB.Get).Count(c => c != null && c.rarity == Rarity.Unique && c.type == type);

        public static int FieldCount(DeckData deck) => deck.extra?.Count ?? 0;

        /// <summary>필드 카드 → 엑스트라 덱 (최대 10장, 같은 필드 1장씩)</summary>
        public static bool CanAddExtra(DeckData deck, CardData c, out string why)
        {
            why = null;
            if (deck.extra == null) deck.extra = new List<string>();
            if (c.type != CardType.Field) { why = "엑스트라 덱에는 필드 카드만 넣을 수 있습니다"; return false; }
            if (deck.extra.Count >= MaxExtra) { why = $"엑스트라 덱은 {MaxExtra}장까지입니다"; return false; }
            if (deck.extra.Contains(c.id)) { why = "같은 필드는 엑스트라 덱에 1장만 넣을 수 있습니다"; return false; }
            if (Profile.Owned(c.id) <= 0) { why = "보유한 카드가 부족합니다"; return false; }
            return true;
        }

        public static bool CanAdd(DeckData deck, CardData c, out string why)
        {
            why = null;
            if (c.type == CardType.Field) { why = "필드 카드는 엑스트라 덱에 넣습니다"; return false; }
            if (deck.cards.Count >= MaxSize) { why = $"덱은 최대 {MaxSize}장까지입니다"; return false; }
            int inDeck = deck.Count(c.id);
            if (inDeck >= Profile.Owned(c.id)) { why = "보유한 카드가 부족합니다"; return false; }
            if (inDeck >= MaxCopies(c)) { why = $"{CardData.RarityLabel(c.rarity)} 카드는 {MaxCopies(c)}장까지 넣을 수 있습니다"; return false; }
            if (c.rarity == Rarity.Unique)
            {
                if (c.type == CardType.Monster && MythicCount(deck, CardType.Monster) >= MaxMythicMonsters) { why = "유니크 몬스터는 덱에 1장만 넣을 수 있습니다"; return false; }
                if (c.type == CardType.Magic && MythicCount(deck, CardType.Magic) >= MaxMythicMagics) { why = "유니크 마법은 덱에 1장만 넣을 수 있습니다"; return false; }
            }
            return true;
        }

        /// <summary>null = 사용 가능</summary>
        public static string Validate(DeckData deck)
        {
            if (CardDB.Get(deck.heroId) == null) return "영웅을 선택하세요";
            if (deck.cards.Count < MinSize || deck.cards.Count > MaxSize) return MinSize == MaxSize ? $"덱은 {MinSize}장이어야 합니다 (현재 {deck.cards.Count}장)" : $"덱은 {MinSize}~{MaxSize}장이어야 합니다 (현재 {deck.cards.Count}장)";
            if (deck.cards.Select(CardDB.Get).Any(c => c != null && c.type == CardType.Field)) return "필드 카드는 엑스트라 덱에 넣어야 합니다";
            var ex = deck.extra ?? new List<string>();
            if (ex.Count > MaxExtra) return $"엑스트라 덱은 {MaxExtra}장까지";
            if (ex.Distinct().Count() != ex.Count) return "엑스트라 덱에 같은 필드가 2장 있습니다";
            foreach (var id in ex)
            {
                var f = CardDB.Get(id);
                if (f == null || f.type != CardType.Field) return "엑스트라 덱에는 필드 카드만";
                if (Profile.Owned(id) <= 0) return $"{f.name}: 보유하지 않은 필드";
            }
            foreach (var g in deck.cards.GroupBy(x => x))
            {
                var c = CardDB.Get(g.Key);
                if (c == null) return "알 수 없는 카드가 있습니다";
                if (g.Count() > MaxCopies(c)) return $"{c.name}: 최대 {MaxCopies(c)}장";
                if (g.Count() > Profile.Owned(c.id)) return $"{c.name}: 보유 수량 초과";
            }
            if (MythicCount(deck, CardType.Monster) > MaxMythicMonsters) return "유니크 몬스터는 1장만";
            if (MythicCount(deck, CardType.Magic) > MaxMythicMagics) return "유니크 마법은 1장만";
            if (!deck.cards.Select(CardDB.Get).Any(c => c.type == CardType.Monster)) return "몬스터가 1장 이상 필요합니다";
            return null;
        }

        /// <summary>대전에 넣을 카드 (메인 + 엑스트라). 필드는 SetupPlayer 가 엑스트라 덱으로 나눈다</summary>
        public static List<CardData> ToCards(DeckData deck)
            => deck.cards.Concat(deck.extra ?? new List<string>()).Select(CardDB.Get).Where(c => c != null).ToList();

        /// <summary>엑스트라 덱을 보유 필드로 채운다 (pool 순서대로, 같은 필드 1장씩, 최대 n장까지)</summary>
        public static void FillExtra(DeckData deck, IEnumerable<CardData> pool, int n)
        {
            if (deck.extra == null) deck.extra = new List<string>();
            foreach (var f in pool)
            {
                if (deck.extra.Count >= n) break;
                if (CanAddExtra(deck, f, out _)) deck.extra.Add(f.id);
            }
        }

        /// <summary>v7 이전 덱: 메인의 필드를 엑스트라 덱으로 옮기고(같은 필드 1장씩) 빈 메인 칸은 보유 카드로 채운다</summary>
        public static void MoveFieldsToExtra(DeckData deck)
        {
            if (deck.extra == null) deck.extra = new List<string>();
            var fields = deck.cards.Where(id => CardDB.Get(id)?.type == CardType.Field).ToList();
            foreach (var id in fields) deck.cards.Remove(id);
            foreach (var id in fields.Distinct()) if (!deck.extra.Contains(id) && deck.extra.Count < MaxExtra) deck.extra.Add(id);
            TopUp(deck);
        }

        /// <summary>pool 을 돌며 덱 규칙(CanAdd)이 허락하는 만큼 target 장까지 넣는다 (한 바퀴에 못 넣으면 멈춤)</summary>
        static void Fill(DeckData deck, IEnumerable<CardData> pool, int target)
        {
            int added = 0;
            bool progress = true;
            while (added < target && progress)
            {
                progress = false;
                foreach (var c in pool)
                {
                    if (added >= target) break;
                    if (CanAdd(deck, c, out _)) { deck.cards.Add(c.id); added++; progress = true; }
                }
            }
        }

        /// <summary>덱 카드 순서: 종류 → 등급(높은 순) → id (덱 편집·저장 공통)</summary>
        public static void SortCards(DeckData deck) => deck.cards = deck.cards.OrderBy(id => CardDB.SortKey(CardDB.Get(id))).ThenBy(id => id).ToList();

        public const string KaiStarterName = "카일 스타터 덱";

        /// <summary>
        /// 카일 스타터 덱: 영웅 카일 + 노멀(Common) 카드 40장 + 엑스트라 덱(노멀 필드, 최대 10장). 무속성 몬스터를 먼저 채우고(카일과 같은 '색 없는' 덱),
        /// 모자라면 다른 속성 노멀 몬스터 · 노멀 마법 · 무속성 필드 순.
        /// </summary>
        public static DeckData KaiStarter()
        {
            var deck = new DeckData { name = KaiStarterName, heroId = StoryData.Kai.id };
            var commons = CardDB.All.Where(c => c.rarity == Rarity.Common && Profile.Owned(c.id) > 0).ToList();
            var mons = commons.Where(c => c.type == CardType.Monster)
                .OrderByDescending(c => c.element == Element.None ? 1 : 0).ThenByDescending(c => c.atk + c.hp).ToList();
            Fill(deck, mons.Where(c => c.minTime <= 2), 13);
            Fill(deck, mons.Where(c => c.minTime >= 3 && c.minTime <= 4), 10);
            Fill(deck, mons.Where(c => c.minTime >= 5), 5);
            Fill(deck, mons, Monsters - deck.cards.Count);
            Fill(deck, commons.Where(c => c.type == CardType.Magic), Magics);
            Fill(deck, commons.Where(c => c.type != CardType.Field), MinSize - deck.cards.Count);
            FillExtra(deck, commons.Where(c => c.type == CardType.Field).OrderByDescending(c => c.element == Element.None ? 1 : 0), MaxExtra);
            SortCards(deck);
            return deck;
        }

        /// <summary>메인 덱 40장 기본 구성: 소환수 28 (T1~2 13 · T3~4 10 · T5~7 5) · 마법 12. 필드는 엑스트라 덱 (최대 10)</summary>
        public const int Monsters = 28, Magics = 12;

        /// <summary>보유 카드로 자동 구성: 메인 소환수 28 · 마법 12 + 엑스트라 덱 필드 10, 강한 카드 우선</summary>
        public static DeckData AutoBuild(string name, string heroId)
        {
            var deck = new DeckData { name = name, heroId = heroId };
            TopUp(deck);
            FillExtra(deck, CardDB.All.Where(c => c.type == CardType.Field && Profile.Owned(c.id) > 0).OrderByDescending(c => (int)c.rarity), MaxExtra);
            return deck;
        }

        /// <summary>
        /// 덱을 보유 카드로 40장까지 채운다 (이미 든 카드는 그대로). 소환수는 타임 곡선대로, 강한 카드 우선.
        /// 덱 40장 개편 때 기존 30장대 덱을 채우는 데도 쓴다.
        /// </summary>
        public static void TopUp(DeckData deck)
        {
            var owned = CardDB.All.Where(c => Profile.Owned(c.id) > 0).ToList();
            var mons = owned.Where(c => c.type == CardType.Monster).OrderByDescending(c => (int)c.rarity).ThenByDescending(c => c.atk + c.hp).ToList();
            int Have(Func<CardData, bool> f) => deck.cards.Select(CardDB.Get).Count(c => c != null && f(c));
            Fill(deck, mons.Where(c => c.minTime <= 2), 13 - Have(c => c.type == CardType.Monster && c.minTime <= 2));
            Fill(deck, mons.Where(c => c.minTime >= 3 && c.minTime <= 4), 10 - Have(c => c.type == CardType.Monster && c.minTime >= 3 && c.minTime <= 4));
            Fill(deck, mons.Where(c => c.minTime >= 5), 5 - Have(c => c.type == CardType.Monster && c.minTime >= 5));
            Fill(deck, mons, Monsters - Have(c => c.type == CardType.Monster));
            Fill(deck, owned.Where(c => c.type == CardType.Magic).OrderByDescending(c => (int)c.rarity), Magics - Have(c => c.type == CardType.Magic));
            Fill(deck, owned.Where(c => c.type != CardType.Field).OrderByDescending(c => (int)c.rarity), MinSize - deck.cards.Count);
            SortCards(deck);
        }
    }
}
