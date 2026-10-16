using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>퀘스트·업적이 참조하는 누적 통계</summary>
    public enum Stat
    {
        Login, MatchPlayed, MatchWon, Summon, MagicUse, Destroy, HeroSkill, Damage,
        PackOpened, SevenGot, StrikerZGot, MythicGot, Crafted,
        Kinds,      // 도감 종류 수 (저장하지 않고 실시간 계산)
    }

    [Serializable] public class StatEntry { public string key; public int value; }
    [Serializable] public class DailyEntry { public string id; public int start; public bool claimed; }

    [Serializable]
    public class QuestSave
    {
        public string day = "";
        public List<DailyEntry> daily = new List<DailyEntry>();
        public List<string> achClaimed = new List<string>();
        public List<StatEntry> stats = new List<StatEntry>();
    }

    public class QuestDef
    {
        public string id, title;
        public Stat stat;
        public int goal, reward;
        public QuestDef(string id, string title, Stat stat, int goal, int reward) { this.id = id; this.title = title; this.stat = stat; this.goal = goal; this.reward = reward; }
    }

    /// <summary>
    /// 일일 퀘스트 · 업적. 보상은 오팔.
    /// 진행도는 누적 통계에서 계산한다: 업적 = 통계값, 일일 = 오늘 시작 시점 대비 증가분.
    /// 일일 퀘스트는 로컬 날짜가 바뀌면 [접속하기] + 무작위 4개로 새로 뽑는다.
    /// </summary>
    public static class Quests
    {
        public const int DailyCount = 4;

        public static readonly QuestDef DailyLogin = new QuestDef("d_login", "접속하기", Stat.Login, 1, 30);

        public static readonly List<QuestDef> DailyPool = new List<QuestDef>
        {
            new QuestDef("d_play", "대전 1회 플레이", Stat.MatchPlayed, 1, 60),
            new QuestDef("d_play3", "대전 3회 플레이", Stat.MatchPlayed, 3, 120),
            new QuestDef("d_win", "대전 1회 승리", Stat.MatchWon, 1, 100),
            new QuestDef("d_summon", "몬스터 8회 소환", Stat.Summon, 8, 80),
            new QuestDef("d_magic", "마법 카드 4회 발동", Stat.MagicUse, 4, 80),
            new QuestDef("d_destroy", "상대 몬스터 5장 파괴", Stat.Destroy, 5, 80),
            new QuestDef("d_hero", "영웅 능력 1회 사용", Stat.HeroSkill, 1, 60),
            new QuestDef("d_damage", "상대 영웅에게 30 데미지", Stat.Damage, 30, 80),
            new QuestDef("d_pack", "부스터팩 1팩 개봉", Stat.PackOpened, 1, 50),
        };

        public static readonly List<QuestDef> Achievements = new List<QuestDef>
        {
            new QuestDef("a_win1", "첫 승리", Stat.MatchWon, 1, 200),
            new QuestDef("a_win10", "승리 10회", Stat.MatchWon, 10, 500),
            new QuestDef("a_win50", "승리 50회", Stat.MatchWon, 50, 1500),
            new QuestDef("a_win100", "승리 100회", Stat.MatchWon, 100, 3000),
            new QuestDef("a_play10", "대전 10회 플레이", Stat.MatchPlayed, 10, 300),
            new QuestDef("a_play50", "대전 50회 플레이", Stat.MatchPlayed, 50, 1000),
            new QuestDef("a_pack10", "부스터팩 10팩 개봉", Stat.PackOpened, 10, 300),
            new QuestDef("a_pack50", "부스터팩 50팩 개봉", Stat.PackOpened, 50, 1000),
            new QuestDef("a_pack100", "부스터팩 100팩 개봉", Stat.PackOpened, 100, 2000),
            new QuestDef("a_kind40", "도감 40종 수집", Stat.Kinds, 40, 300),
            new QuestDef("a_kind70", "도감 70종 수집", Stat.Kinds, 70, 800),
            new QuestDef("a_kindAll", "도감 완성", Stat.Kinds, -1, 5000),       // goal -1 = 전체 카드 수
            new QuestDef("a_seven1", "첫 Seven 가공 획득", Stat.SevenGot, 1, 300),
            new QuestDef("a_seven10", "Seven 가공 10장 획득", Stat.SevenGot, 10, 1000),
            new QuestDef("a_strikerz1", "첫 StrikerZ 가공 획득", Stat.StrikerZGot, 1, 1500),
            new QuestDef("a_mythic1", "유니크 카드 획득", Stat.MythicGot, 1, 500),
            new QuestDef("a_craft1", "첫 카드 생성", Stat.Crafted, 1, 200),
            new QuestDef("a_craft10", "카드 10장 생성", Stat.Crafted, 10, 500),
            new QuestDef("a_summon100", "몬스터 100회 소환", Stat.Summon, 100, 500),
            new QuestDef("a_destroy100", "상대 몬스터 100장 파괴", Stat.Destroy, 100, 500),
            new QuestDef("a_damage500", "상대 영웅에게 누적 500 데미지", Stat.Damage, 500, 500),
        };

        static QuestSave Q
        {
            get
            {
                var d = Profile.Data;
                if (d.quests == null) d.quests = new QuestSave();
                return d.quests;
            }
        }

        // ── 통계 ──

        public static int StatValue(Stat s)
        {
            if (s == Stat.Kinds) return Profile.OwnedKinds;
            var e = Q.stats.FirstOrDefault(x => x.key == s.ToString());
            return e?.value ?? 0;
        }

        /// <summary>통계 증가 (저장은 호출 측의 다음 Save 에 맡긴다)</summary>
        public static void Add(Stat s, int n = 1)
        {
            if (n <= 0 || s == Stat.Kinds) return;
            if (GameManager.I != null && GameManager.I.NoStats) return;     // 훈련장 · 퍼즐은 통계에 넣지 않는다
            var e = Q.stats.FirstOrDefault(x => x.key == s.ToString());
            if (e == null) Q.stats.Add(e = new StatEntry { key = s.ToString() });
            e.value += n;
        }

        public static int Goal(QuestDef q) => q.goal > 0 ? q.goal : CardDB.All.Count;

        // ── 일일 ──

        static string Today => DailyClock.Today;

        public static TimeSpan UntilReset => DailyClock.UntilReset;

        /// <summary>날짜가 바뀌었으면 일일 퀘스트를 새로 뽑는다. 바뀌었으면 true</summary>
        public static bool EnsureDaily()
        {
            if (Q.day == Today && Q.daily.Count > 0) return false;
            Q.day = Today;
            Q.daily.Clear();
            var rng = new Random(Today.GetHashCode() ^ Profile.Data.nickname.GetHashCode());
            var picks = DailyPool.OrderBy(_ => rng.Next()).ToList();
            // 같은 통계를 쓰는 퀘스트는 하루에 하나만
            var chosen = new List<QuestDef> { DailyLogin };
            foreach (var p in picks)
            {
                if (chosen.Count >= DailyCount + 1) break;
                if (chosen.Any(c => c.stat == p.stat)) continue;
                chosen.Add(p);
            }
            foreach (var c in chosen) Q.daily.Add(new DailyEntry { id = c.id, start = StatValue(c.stat) });
            Add(Stat.Login);
            Profile.Save();
            return true;
        }

        public static QuestDef DailyDef(string id) => id == DailyLogin.id ? DailyLogin : DailyPool.FirstOrDefault(q => q.id == id);

        public static IEnumerable<(QuestDef def, DailyEntry entry)> Daily
        {
            get
            {
                EnsureDaily();
                foreach (var e in Q.daily)
                {
                    var def = DailyDef(e.id);
                    if (def != null) yield return (def, e);
                }
            }
        }

        public static int DailyProgress(QuestDef q, DailyEntry e) => Math.Min(Goal(q), Math.Max(0, StatValue(q.stat) - e.start));

        // ── 업적 ──

        public static int AchProgress(QuestDef q) => Math.Min(Goal(q), StatValue(q.stat));
        public static bool AchClaimed(QuestDef q) => Q.achClaimed.Contains(q.id);

        // ── 보상 ──

        public static bool ClaimDaily(DailyEntry e)
        {
            var q = DailyDef(e.id);
            if (q == null || e.claimed || DailyProgress(q, e) < Goal(q)) return false;
            e.claimed = true;
            Profile.Data.opal += q.reward;
            SevenPass.AddXp(SevenPass.XpDaily);
            Profile.Save();
            return true;
        }

        public static bool ClaimAch(QuestDef q)
        {
            if (AchClaimed(q) || AchProgress(q) < Goal(q)) return false;
            Q.achClaimed.Add(q.id);
            Profile.Data.opal += q.reward;
            Profile.Save();
            return true;
        }

        /// <summary>받을 수 있는 보상 전부 수령. 받은 오팔 합계 반환</summary>
        public static int ClaimAll()
        {
            int sum = 0;
            foreach (var (def, e) in Daily.ToList())
                if (!e.claimed && DailyProgress(def, e) >= Goal(def)) { e.claimed = true; sum += def.reward; SevenPass.AddXp(SevenPass.XpDaily); }
            foreach (var a in Achievements)
                if (!AchClaimed(a) && AchProgress(a) >= Goal(a)) { Q.achClaimed.Add(a.id); sum += a.reward; }
            if (sum > 0) { Profile.Data.opal += sum; Profile.Save(); }
            return sum;
        }

        public static int ClaimableCount
        {
            get
            {
                int n = Daily.Count(x => !x.entry.claimed && DailyProgress(x.def, x.entry) >= Goal(x.def));
                n += Achievements.Count(a => !AchClaimed(a) && AchProgress(a) >= Goal(a));
                return n;
            }
        }
    }
}
