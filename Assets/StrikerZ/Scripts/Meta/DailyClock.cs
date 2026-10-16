using System;

namespace StrikerZ
{
    /// <summary>
    /// 일일 초기화 시계 (일일 퀘스트 · 오늘의 특가 공통). 지금은 기기 시간 기준 자정 초기화.
    /// 온라인 전환 시 서버 시간으로 바꿀 곳은 여기 한 곳.
    /// </summary>
    public static class DailyClock
    {
        /// <summary>오늘 날짜 키 (저장 데이터의 "day" 값과 비교)</summary>
        public static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>다음 초기화(자정)까지 남은 시간</summary>
        public static TimeSpan UntilReset => DateTime.Today.AddDays(1) - DateTime.Now;

        /// <summary>"HH:MM:SS" (24시간을 넘어도 시간 자리를 그대로 표시)</summary>
        public static string Countdown(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
}
