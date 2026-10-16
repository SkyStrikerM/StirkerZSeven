using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>듀얼 연대기 한 줄: 대전 1판 기록</summary>
    [Serializable]
    public class MatchRecord
    {
        public string date;      // "MM-dd HH:mm"
        public string mode;      // MatchMode 이름
        public int level;        // CpuLevel (일반 대전)
        public string hero, oppHero, oppName, deck;
        public bool won;
        public int time;         // 끝난 T
        public int hp, oppHp;    // 끝났을 때 남은 HP
    }

    /// <summary>
    /// 듀얼 연대기: 최근 대전 기록(최대 60판) · 많이 쓴 카드. (승패 집계·승률은 보여 주지 않는다 — 전적은 온라인 대전 전용)
    /// 훈련장 · 마법진 퍼즐은 기록하지 않는다.
    /// </summary>
    public static class Chronicle
    {
        public const int Keep = 60;

        public static List<MatchRecord> Records
        {
            get
            {
                var d = Profile.Data;
                if (d.chronicle == null) d.chronicle = new List<MatchRecord>();
                return d.chronicle;
            }
        }

        static List<StatEntry> Use
        {
            get
            {
                var d = Profile.Data;
                if (d.cardUse == null) d.cardUse = new List<StatEntry>();
                return d.cardUse;
            }
        }

        public static void Add(GameManager g, string deckName)
        {
            if (g == null || g.players[0] == null) return;
            if (g.mode == MatchMode.Practice || g.mode == MatchMode.Puzzle) return;
            Records.Insert(0, new MatchRecord
            {
                date = DateTime.Now.ToString("MM-dd HH:mm"),
                mode = g.mode.ToString(),
                level = (int)g.cpuLevel,
                hero = g.Human.hero?.data.id,
                oppHero = g.Cpu.hero?.data.id,
                oppName = g.Cpu.name,
                deck = deckName ?? "",
                won = g.winner == g.Human,
                time = g.time,
                hp = g.Human.hp,
                oppHp = g.Cpu.hp,
            });
            while (Records.Count > Keep) Records.RemoveAt(Records.Count - 1);
        }

        /// <summary>사람이 카드를 냈다 (소환 · 세트 · 필드). 저장은 대전 끝 기록 때</summary>
        public static void CountUse(string cardId)
        {
            var e = Use.FirstOrDefault(x => x.key == cardId);
            if (e == null) Use.Add(e = new StatEntry { key = cardId });
            e.value++;
        }

        public static IEnumerable<(CardData card, int n)> TopCards(int n)
            => Use.OrderByDescending(x => x.value).Select(x => (CardDB.Get(x.key), x.value)).Where(x => x.Item1 != null).Take(n);

        public static string ModeName(string mode, int level)
        {
            switch (mode)
            {
                case nameof(MatchMode.Duel): return $"CPU 대전 · {GameManager.LevelName((CpuLevel)level)}";
                case nameof(MatchMode.Story): return "스토리";
                case nameof(MatchMode.Raid): return "레이드";
                case nameof(MatchMode.Event): return "원소 이변전";
                case nameof(MatchMode.Arena): return "원소 투기장";
                default: return mode;
            }
        }
    }
}
