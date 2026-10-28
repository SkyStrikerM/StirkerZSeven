using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>CPU 난이도: 수련 · 정예 · 전설</summary>
    public enum CpuLevel { Trainee, Elite, Legend }

    /// <summary>대전 종류 (보상·기록·규칙이 갈린다)</summary>
    public enum MatchMode { Duel, Story, Raid, Practice, Puzzle, Event, Arena }

    /// <summary>
    /// 대전 종류 · CPU 난이도.
    ///   수련: CPU 영웅 HP ×0.85, 무작위 덱, 배치가 서툴다 (승리 보너스 없음)
    ///   정예: 기본 (승리 보너스 오팔 15)
    ///   전설: CPU 영웅 HP ×1.15, 강한 카드 위주 덱, 배치·영웅 능력을 아끼지 않는다 (승리 보너스 오팔 40)
    /// </summary>
    public partial class GameManager
    {
        public CpuLevel cpuLevel = CpuLevel.Elite;
        public MatchMode mode;
        MatchMode pendingMode = MatchMode.Duel;

        /// <summary>다음 StartMatch 의 대전 종류 (스토리·레이드는 각자 시작 함수가 정한다)</summary>
        public void SetNextMode(MatchMode m) => pendingMode = m;

        public static string LevelName(CpuLevel l) => l == CpuLevel.Trainee ? "수련" : l == CpuLevel.Legend ? "전설" : "정예";

        public static string LevelDesc(CpuLevel l) => l == CpuLevel.Trainee
            ? "가볍게 연습 · CPU 영웅 HP 85% · 승리 보너스 없음"
            : l == CpuLevel.Legend
                ? "강한 덱 · CPU 영웅 HP 115% · 승리 보너스 오팔 40"
                : "기본 상대 · 승리 보너스 오팔 15";

        /// <summary>일반 대전 승리 보너스 오팔 (난이도별)</summary>
        public static int WinBonus(CpuLevel l) => l == CpuLevel.Trainee ? 0 : l == CpuLevel.Legend ? 40 : 15;

        float CpuHpRate => cpuLevel == CpuLevel.Trainee ? 0.85f : cpuLevel == CpuLevel.Legend ? 1.15f : 1f;

        /// <summary>CPU 덱: 전설은 강한 카드 위주, 나머지는 무작위 (null = SetupPlayer 가 영웅에 맞춘 무작위 덱). 영웅 원소 상한에 맞춘다</summary>
        List<CardData> CpuDeck(CardData hero) => cpuLevel == CpuLevel.Legend ? CardList.BuildStrongDeck(rng, hero) : null;

        void ApplyCpuLevel(PlayerState p)
        {
            p.maxHp = p.hp = Mathf.Max(1, Mathf.RoundToInt(p.maxHp * CpuHpRate));
        }

        // ── 훈련장: 수련 골렘 ──
        public const string GolemName = "수련 골렘";
        public const int GolemHp = 150;

        /// <summary>훈련장 상대: 대지 영웅 모습의 수련 골렘 (HP 150, 수련 AI, 무작위 덱). 기록·보상·퀘스트 진행 없음</summary>
        void SetupPracticeGolem(PlayerState p)
        {
            var hero = CardList.Heroes.Find(h => h.element == Element.Earth && !StoryData.IsStoryHero(h)) ?? CardList.Heroes[0];
            SetupPlayer(p, hero, null);
            p.maxHp = p.hp = GolemHp;
        }

        /// <summary>이 대전은 퀘스트·업적 통계를 올리지 않는다 (훈련장 · 마법진 퍼즐)</summary>
        public bool NoStats => players[0] != null && !MatchFinished && (mode == MatchMode.Practice || mode == MatchMode.Puzzle);
    }
}
