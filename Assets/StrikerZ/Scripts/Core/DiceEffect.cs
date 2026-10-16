using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 몬스터의 주사위 효과 (한 구간 분량). 전투 시 3번(바람 4번) 굴려 1개를 고르면,
    /// 고른 눈의 구간(1·2 / 3·4 / 5·6)에 해당하는 효과가 그 전투에만 적용된다. 눈의 숫자 자체는 더해지지 않는다.
    /// CardList 에서 dice = Dice.Of(Dice.Atk(1), Dice.Guard(), Dice.Atk(2).Pierce(2)) 처럼 지정.
    /// </summary>
    public class DiceEffect
    {
        public int atk;          // 이 전투 동안 자신 ATK 증감 (주는 피해)
        public int enemyAtk;     // 상대 몬스터 ATK 감소량 — 이 전투부터 이번 T 배틀 엔드까지 (CardInstance.weaken)
        public int hp;           // 이 전투 동안 자신 HP 증가 (먼저 깎이는 보호막)
        public bool guard;       // HP 가 0 이 되어도 HP 1 로 버티고 파괴되지 않음
        public int pierce;       // 상대 몬스터를 파괴했을 때 상대 영웅에게 추가 데미지
        public int heal;         // 자신 영웅 HP 회복
        public int draw;         // 카드 드로우
        public int burn;         // 상대 영웅에게 즉시 데미지
        public bool wound;       // [상처] 피해를 준 상대 몬스터는 다음 타임 시작 때 HP 가 회복되지 않음
        public string custom;    // 설명 덮어쓰기 (비우면 자동 생성)

        public DiceEffect Plus(DiceEffect o)
        {
            atk += o.atk; enemyAtk += o.enemyAtk; hp += o.hp; guard |= o.guard; pierce += o.pierce;
            heal += o.heal; draw += o.draw; burn += o.burn; wound |= o.wound;
            return this;
        }

        // 체이닝: Dice.Atk(2).Pierce(1).Heal(2)
        // 2026-10-16 밸런스: 카드 데이터의 수치는 그대로 두고 여기서 약 40% 줄인다 (Dice.N).
        //   파괴되지 않음(Guard) 폐지 → HP +2 / 관통(Pierce) 폐지 → 상대 영웅 데미지(Burn) / 드로우는 1장 줄임
        public DiceEffect Atk(int n) { atk += Dice.N(n); return this; }
        public DiceEffect Weaken(int n) { enemyAtk += Dice.N(n); return this; }
        public DiceEffect Shield(int n) { hp += Dice.N(n); return this; }
        public DiceEffect Guard() { hp += Dice.N(2); return this; }
        public DiceEffect Pierce(int n) { burn += Dice.N(n); return this; }
        public DiceEffect Heal(int n) { heal += Dice.N(n); return this; }
        public DiceEffect Draw(int n) { draw += Dice.D(n); return this; }
        public DiceEffect Burn(int n) { burn += Dice.N(n); return this; }
        public DiceEffect Wound() { wound = true; return this; }

        /// <summary>밸류 기준 점수 (구간끼리 비슷하게 맞출 때 사용): ATK·약화·HP 1 / 파괴 방지 2 / 관통 0.75 / 회복 0.6 / 드로우 1.5 / 영웅 데미지 0.8 / 상처 1</summary>
        public float Score => atk + enemyAtk + hp + (guard ? 2f : 0) + pierce * 0.75f + heal * 0.6f + draw * 1.5f + burn * 0.8f + (wound ? 1f : 0);

        public string Describe()
        {
            if (!string.IsNullOrEmpty(custom)) return custom;
            var p = new List<string>();
            if (atk != 0) p.Add($"ATK {(atk > 0 ? "+" : "")}{atk}");
            if (enemyAtk != 0) p.Add($"상대 ATK -{enemyAtk}(배틀 엔드까지)");
            if (hp != 0) p.Add($"HP {(hp > 0 ? "+" : "")}{hp}");
            if (guard) p.Add("파괴되지 않음");
            if (wound) p.Add("상처");
            if (pierce != 0) p.Add($"관통 +{pierce}");
            if (heal != 0) p.Add($"영웅 HP {heal} 회복");
            if (draw != 0) p.Add($"{draw}장 드로우");
            if (burn != 0) p.Add($"상대 영웅 {burn} 데미지");
            return p.Count == 0 ? "효과 없음" : string.Join(" · ", p);
        }
    }

    public static class Dice
    {
        public static readonly string[] BandNames = { "1·2", "3·4", "5·6" };

        /// <summary>[0] = 1·2, [1] = 3·4, [2] = 5·6</summary>
        public static DiceEffect[] Of(DiceEffect low, DiceEffect mid, DiceEffect high) => new[] { low, mid, high };

        /// <summary>주사위 효과를 지정하지 않은 몬스터의 기본값</summary>
        public static DiceEffect[] Default => Of(Guard(), Weaken(2), Atk(2));

        public static int Band(int value) => value <= 2 ? 0 : value <= 4 ? 1 : 2;

        public static DiceEffect None() => new DiceEffect();
        public static DiceEffect Atk(int n) => new DiceEffect().Atk(n);
        public static DiceEffect Weaken(int n) => new DiceEffect().Weaken(n);
        public static DiceEffect Shield(int n) => new DiceEffect().Shield(n);
        /// <summary>(폐지) 예전 "파괴되지 않음" 자리 → HP +2</summary>
        public static DiceEffect Guard() => new DiceEffect().Guard();
        /// <summary>(폐지) 예전 "관통" 자리 → 상대 영웅 데미지</summary>
        public static DiceEffect Pierce(int n) => new DiceEffect().Pierce(n);
        public static DiceEffect Heal(int n) => new DiceEffect().Heal(n);
        public static DiceEffect Draw(int n) => new DiceEffect().Draw(n);
        public static DiceEffect Burn(int n) => new DiceEffect().Burn(n);

        /// <summary>주사위 수치 너프: 1→1 · 2→2 · 3→2 · 4→3 · 5→3 · 6→4 · 7→5 · 8→5 (×0.6 올림)</summary>
        public static int N(int n) => n <= 0 ? n : Mathf.CeilToInt(n * 0.6f);
        /// <summary>드로우 너프: 1→1 · 2→1 · 3→2</summary>
        public static int D(int n) => n <= 1 ? n : n - 1;
        public static DiceEffect Wound() => new DiceEffect { wound = true };
    }
}
