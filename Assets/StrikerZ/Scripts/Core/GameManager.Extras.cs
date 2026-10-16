using System.Collections.Generic;

namespace StrikerZ
{
    /// <summary>전투 두루마리 · 정찰 노트용 기록</summary>
    public partial class GameManager
    {
        /// <summary>이번 대전의 모든 로그 (T, 글) — 전투 두루마리</summary>
        public readonly List<(int time, string msg)> ScrollLog = new List<(int, string)>();
        /// <summary>이번 대전에서 공개된 상대 카드 (공개 순서)</summary>
        public readonly List<CardData> CpuSeen = new List<CardData>();
        /// <summary>지난 배틀에서 상대 칸마다 행동한 카드 이름 (정찰 노트)</summary>
        public string[] LastCpuLanes;
        string[] curLanes;
        const int ScrollMax = 2500;

        void ResetExtrasData()
        {
            ScrollLog.Clear();
            CpuSeen.Clear();
            LastCpuLanes = null;
            curLanes = null;
        }

        void ScrollAdd(string msg)
        {
            ScrollLog.Add((time, msg));
            if (ScrollLog.Count > ScrollMax) ScrollLog.RemoveAt(0);
        }

        void NoteCpuCard(CardInstance c)
        {
            if (c != null && players[1] != null && c.owner == Cpu) CpuSeen.Add(c.data);
        }
    }
}
