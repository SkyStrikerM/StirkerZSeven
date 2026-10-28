using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 마법진 퍼즐 한 문제: 정해진 패 · 상대 필드에서 "이번 T 안에 상대 영웅 HP 를 0 으로".
    /// 퍼즐에서는 주사위·코인 효과가 없고(결과가 늘 같다), 영웅 능력은 쓸 수 없으며, 시간 제한도 없다.
    /// 피해 상한(T당 15)·직접 공격 절반·초과 피해 절반 등 나머지 규칙은 대전과 같다.
    /// </summary>
    public class PuzzleDef
    {
        public string id, title, hint;
        public int time = 3;
        /// <summary>이번 T 같은 칸에서 내가 먼저 행동하는가</summary>
        public bool humanFirst = true;
        public string hero = "H001", oppHero = "H013";
        public int oppHp = 8, myHp = 30;
        public string[] hand;
        /// <summary>상대 칸 1~7 (null = 빈 칸). 모두 앞면</summary>
        public string[] oppSlots;
        public string field = "고요한 평원";
        public int reward = 60;

        public CardData Field => CardList.Fields.FirstOrDefault(f => f.name == field) ?? CardList.Fields[0];
    }

    public static class Puzzles
    {
        static string[] Opp(params string[] ids)
        {
            var a = new string[PlayerState.SlotCount];
            for (int i = 0; i < ids.Length && i < a.Length; i++) a[i] = ids[i];
            return a;
        }

        public static readonly List<PuzzleDef> All = new List<PuzzleDef>
        {
            new PuzzleDef { id = "P01", title = "첫 일격", time = 2, oppHp = 5, reward = 50,
                hint = "상대 필드가 비어 있으면 소환수는 영웅을 직접 공격한다 — 피해는 ATK 의 절반(올림). 소환 효과 데미지도 더해진다.",
                hand = new[] { "M232", "M201" }, oppSlots = Opp() },
            new PuzzleDef { id = "P02", title = "도발의 벽", time = 2, oppHp = 5, reward = 60,
                hint = "[도발] 소환수가 있으면 그쪽부터 공격해야 한다. 소환수를 쓰러뜨리고 남은 초과 피해는 절반이 영웅에게 간다.",
                hand = new[] { "M345", "M339" }, oppSlots = Opp("M009") },
            new PuzzleDef { id = "P03", title = "방패를 앞에", time = 3, humanFirst = false, oppHp = 8, reward = 70,
                hint = "이번 T 는 상대가 먼저 움직인다. 상대 소환수는 내 가장 앞(왼쪽) 소환수부터 공격한다 — 무엇을 앞에 둘까?",
                hand = new[] { "M206", "M011", "M231" }, oppSlots = Opp("M341") },
            new PuzzleDef { id = "P04", title = "개기일식", time = 3, oppHp = 6, reward = 80,
                hint = "개기일식은 일반 마법 — 배치 중에 패에서 바로 발동한다. 개기일식 뒤로 뒷면 카드는 움직이지 못하고, [불변] 소환수만 싸울 수 있다.",
                hand = new[] { "S003", "M011", "M343" }, oppSlots = Opp("M266", "M263") },
            new PuzzleDef { id = "P05", title = "선봉의 질주", time = 4, humanFirst = false, oppHp = 6, reward = 90,
                hint = "[선봉] 소환수는 칸 순서보다 먼저 공격한다. 상대보다 먼저 치면 내 소환수가 살아남는다.",
                hand = new[] { "M344", "M231", "M201" }, oppSlots = Opp("M345", "M342") },
            new PuzzleDef { id = "P06", title = "벼락과 바람", time = 4, oppHp = 4, reward = 110,
                hint = "일반 마법은 배치 중에 패에서 바로 발동한다. 번개 일격으로 상대 앞을 치우고, 질풍 가속은 '발동하는 순간 가장 앞'인 내 소환수를 강하게 한다 — 소환한 뒤에 쓰자.",
                hand = new[] { "S004", "S008", "M231", "M201" }, oppSlots = Opp("M209", "M260") },
        };

        public static PuzzleDef Get(string id) => All.FirstOrDefault(p => p.id == id);
        public static bool Cleared(string id) => Profile.Data.puzzlesCleared != null && Profile.Data.puzzlesCleared.Contains(id);
        public static int ClearedCount => All.Count(p => Cleared(p.id));

        /// <summary>클리어 기록. 처음이면 보상 오팔 지급 (지급량 반환, 이미 깼으면 0)</summary>
        public static int RecordClear(PuzzleDef p)
        {
            var d = Profile.Data;
            if (d.puzzlesCleared == null) d.puzzlesCleared = new List<string>();
            if (d.puzzlesCleared.Contains(p.id)) return 0;
            d.puzzlesCleared.Add(p.id);
            d.opal += p.reward;
            Profile.Save();
            return p.reward;
        }
    }
}
