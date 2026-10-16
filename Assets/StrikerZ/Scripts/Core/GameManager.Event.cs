using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 원소 이변전 규칙 훅. 규칙은 이변전 대전(mode == Event)에서만 켜진다.
    /// [불변] · [저격] 판정, 드로우 장수, 배치 시간, 직접 공격 배율, 소환수 ATK 보정을 한곳에서.
    /// </summary>
    public partial class GameManager
    {
        public EventRule eventRule;

        EventRule Rule => mode == MatchMode.Event ? eventRule : EventRule.None;

        public bool Resist(CardInstance c) => c != null && c.IsMonster && (c.data.resist || Rule == EventRule.Unyielding);
        public bool Snipe(CardInstance c) => c != null && c.IsMonster && (c.data.snipe || Rule == EventRule.Sniper);

        /// <summary>동시 룰 T 시작 드로우: T2 2장 · T3 3장 … T7 부터 7장 (T1 은 시작 패만)</summary>
        public int DrawCount => Mathf.Clamp(time, 0, MaxDrawPerTime);
        /// <summary>패 최대 장수: 동시 룰 9장 (엔드 타임에 7장 넘는 만큼 버림) · 예전 교대 룰(레이드) 7장</summary>
        public int HandLimit => Simul ? SimulHandMax : HandMax;
        public float PlanTime => Rule == EventRule.Rush ? 15f : PlanSeconds;
        float DirectMul => Rule == EventRule.Rush ? 1f : DirectRate;
        int EventAtkBonus => Rule == EventRule.Frenzy ? 2 : 0;

        /// <summary>폭주의 주간: 양쪽 영웅 HP 1.3배 (SetupPlayer 뒤에)</summary>
        void ApplyEventSetup()
        {
            if (Rule != EventRule.Frenzy) return;
            foreach (var p in players) p.maxHp = p.hp = Mathf.RoundToInt(p.maxHp * 1.3f);
        }
    }
}
