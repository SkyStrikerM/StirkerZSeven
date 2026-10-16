using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    [Serializable]
    public class ArenaSave
    {
        public bool active;
        public string hero = "";
        public List<string> heroChoices = new List<string>();
        public List<string> deck = new List<string>();
        public List<string> choices = new List<string>();
        public int wins, losses;
        /// <summary>끝난 뒤 메뉴에서 열어 줄 보상 팩</summary>
        public int pendingPacks;
    }

    /// <summary>
    /// 원소 투기장 (드래프트): 오팔로 입장 → 영웅 3명 중 1명 → 카드 3장 중 1장씩 30번 골라 즉석 덱.
    /// CPU(정예)와 싸워 7승 또는 3패에서 끝나고 승수만큼 보상. 고른 카드는 컬렉션에 들어가지 않는다.
    /// 픽 순서: 1~20 소환수 (T1~2 9 · T3~4 7 · T5~7 4) · 21~27 마법 · 28~30 필드.
    /// </summary>
    public static class Arena
    {
        public const int EntryOpal = 300, DeckSize = 30, MaxWins = 7, MaxLosses = 3;
        public static readonly int[] RewardOpal = { 80, 120, 160, 220, 300, 400, 520, 700 };

        public static ArenaSave S
        {
            get
            {
                var d = Profile.Data;
                if (d.arena == null) d.arena = new ArenaSave();
                return d.arena;
            }
        }

        public static bool Active => S.active;
        public static bool PickingHero => S.active && string.IsNullOrEmpty(S.hero);
        public static bool Drafting => S.active && (string.IsNullOrEmpty(S.hero) || S.deck.Count < DeckSize);
        public static bool Ready => S.active && !Drafting;

        public static int Packs(int wins) => wins >= 6 ? 2 : wins >= 3 ? 1 : 0;

        static readonly Random rng = new Random();

        public static bool Enter(out string msg)
        {
            if (S.active) { msg = "이미 진행 중인 투기장이 있습니다"; return false; }
            if (!Profile.Spend(EntryOpal)) { msg = "오팔이 부족합니다"; return false; }
            var heroes = CardList.Heroes.Where(h => !StoryData.IsStoryHero(h)).OrderBy(_ => rng.Next()).Take(3).Select(h => h.id).ToList();
            Profile.Data.arena = new ArenaSave { active = true, heroChoices = heroes };
            Profile.Save();
            msg = "원소 투기장 입장! 영웅을 고르세요";
            return true;
        }

        public static void PickHero(string id)
        {
            if (!PickingHero || !S.heroChoices.Contains(id)) return;
            S.hero = id;
            Roll();
            Profile.Save();
        }

        public static void Pick(string id)
        {
            if (!Drafting || PickingHero || !S.choices.Contains(id)) return;
            S.deck.Add(id);
            if (S.deck.Count < DeckSize) Roll(); else S.choices.Clear();
            Profile.Save();
        }

        static Rarity RollRarity()
        {
            int r = rng.Next(100);
            return r < 2 ? Rarity.Unique : r < 12 ? Rarity.Rare : r < 40 ? Rarity.Uncommon : Rarity.Common;
        }

        /// <summary>다음 픽 후보 3장 (같은 등급, 서로 다른 카드)</summary>
        static void Roll()
        {
            int i = S.deck.Count;
            IEnumerable<CardData> pool;
            if (i < 20)
            {
                int lo = i < 9 ? 1 : i < 16 ? 3 : 5, hi = i < 9 ? 2 : i < 16 ? 4 : 7;
                pool = CardDB.All.Where(c => c.type == CardType.Monster && c.minTime >= lo && c.minTime <= hi);
            }
            else if (i < 27) pool = CardDB.All.Where(c => c.type == CardType.Magic);
            else pool = CardDB.All.Where(c => c.type == CardType.Field);
            var all = pool.ToList();
            var rar = RollRarity();
            List<CardData> picks = null;
            for (var r = rar; r >= Rarity.Common; r--)
            {
                var l = all.Where(c => c.rarity == r).ToList();
                if (l.Count >= 3) { picks = l; break; }
            }
            if (picks == null) picks = all;
            S.choices = picks.OrderBy(_ => rng.Next()).Take(3).Select(c => c.id).ToList();
        }

        public static List<CardData> DeckCards => S.deck.Select(CardDB.Get).Where(c => c != null).ToList();

        /// <summary>대전 결과 기록. 끝났으면 보상 지급 후 요약 글, 아니면 진행 글</summary>
        public static string Record(bool won)
        {
            var s = S;
            if (!s.active) return "";
            if (won) s.wins++; else s.losses++;
            Quests.Add(Stat.MatchPlayed);
            if (won) Quests.Add(Stat.MatchWon);
            string msg;
            if (s.wins >= MaxWins || s.losses >= MaxLosses) msg = Finish();
            else msg = $"투기장 {s.wins}승 {s.losses}패 — {(won ? "승리!" : "패배")} 다음 대전을 준비하세요";
            Profile.Save();
            return msg;
        }

        /// <summary>투기장 종료: 승수 보상 (오팔 · 팩)</summary>
        public static string Finish()
        {
            var s = S;
            if (!s.active) return "";
            int w = Math.Min(s.wins, MaxWins);
            int opal = RewardOpal[w];
            int packs = Packs(w);
            Profile.Data.opal += opal;
            s.active = false;
            s.pendingPacks += packs;
            Profile.Save();
            return $"원소 투기장 종료 — {s.wins}승 {s.losses}패 · 보상 오팔 +{opal}" + (packs > 0 ? $" · 부스터팩 {packs}팩" : "");
        }
    }
}
