using System;
using System.Globalization;

namespace StrikerZ
{
    /// <summary>원소 이변전 특수 규칙 (주마다 하나)</summary>
    public enum EventRule { None, Unyielding, Sniper, Frenzy, Gale, Rush }

    /// <summary>
    /// 원소 이변전: 매주(월요일 0시) 바뀌는 특수 규칙으로 CPU(정예)와 대전.
    /// 보상: 승리마다 오팔 30 · 그 주 첫 승리 오팔 300.
    /// </summary>
    public static class ElementEvent
    {
        public const int WinOpal = 30, FirstWinOpal = 300;

        // 질풍 드로우(Gale)는 2026-10-13 기본 룰이 패 9장 · 드로우 T 번호만큼으로 바뀌면서 삭제 (값은 저장 호환용으로 남김)
        static readonly EventRule[] Rotation = { EventRule.Unyielding, EventRule.Sniper, EventRule.Frenzy, EventRule.Rush };

        /// <summary>이번 주 키 (ISO 주차)</summary>
        public static string WeekKey
        {
            get
            {
                var now = DateTime.Now;
                int w = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                return $"{now.Year}-W{w:00}";
            }
        }

        /// <summary>테스트용: 이번 주 규칙 강제</summary>
        public static EventRule? TestOverride;

        public static EventRule ThisWeek
        {
            get
            {
                if (TestOverride.HasValue) return TestOverride.Value;
                var now = DateTime.Now;
                // 2026-10-12(월) 주 = 0번 규칙부터 순환
                int weeks = (int)Math.Floor((now.Date - new DateTime(2026, 10, 12)).TotalDays / 7.0);
                int i = ((weeks % Rotation.Length) + Rotation.Length) % Rotation.Length;
                return Rotation[i];
            }
        }

        public static TimeSpan UntilNextWeek
        {
            get
            {
                var now = DateTime.Now;
                int daysToMon = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                if (daysToMon == 0) daysToMon = 7;
                return now.Date.AddDays(daysToMon) - now;
            }
        }

        public static string Name(EventRule r)
        {
            switch (r)
            {
                case EventRule.Unyielding: return "불변의 주간";
                case EventRule.Sniper: return "저격의 주간";
                case EventRule.Frenzy: return "폭주의 주간";
                case EventRule.Gale: return "질풍 드로우";
                case EventRule.Rush: return "속도전";
                default: return "평소 규칙";
            }
        }

        public static string Desc(EventRule r)
        {
            switch (r)
            {
                case EventRule.Unyielding: return "모든 소환수가 [불변] — 파괴 외의 카드 효과를 받지 않는다. 전투와 파괴 효과가 승부를 가른다.";
                case EventRule.Sniper: return "모든 소환수가 [저격] — 가장 뒤(오른쪽) 소환수부터 노린다. [도발]은 여전히 먼저.";
                case EventRule.Frenzy: return "모든 소환수 ATK +2, 양쪽 영웅 HP 1.3배. 화끈한 난타전.";
                case EventRule.Gale: return "T마다 드로우 4장 · 패 최대 9장 (평소 3장 · 7장). 패가 넘치는 빠른 전개.";
                case EventRule.Rush: return "배치 시간 15초 · 직접 공격 피해가 절반으로 줄지 않는다. 빠른 판단이 승부.";
                default: return "";
            }
        }

        static EventSave E
        {
            get
            {
                var d = Profile.Data;
                if (d.eventSave == null) d.eventSave = new EventSave();
                if (d.eventSave.week != WeekKey) d.eventSave = new EventSave { week = WeekKey };
                return d.eventSave;
            }
        }

        public static bool WonThisWeek => E.wins > 0;
        public static int WinsThisWeek => E.wins;
        public static int PlaysThisWeek => E.plays;

        /// <summary>결과 기록 · 보상 (지급 오팔 반환)</summary>
        public static int Record(bool won)
        {
            var e = E;
            e.plays++;
            int opal = 0;
            if (won)
            {
                opal = WinOpal + (e.wins == 0 ? FirstWinOpal : 0);
                e.wins++;
                Profile.Data.opal += opal;
            }
            Quests.Add(Stat.MatchPlayed);
            if (won) Quests.Add(Stat.MatchWon);
            Profile.Save();
            return opal;
        }
    }

    [Serializable]
    public class EventSave
    {
        public string week = "";
        public int wins, plays;
    }
}
