using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>
    /// 룬 해설: 카드 글에 나오는 [키워드] · 능력 표시를 짧게 풀어 준다.
    /// 전투 화면 카드 정보(왼쪽)와 카드 확대 창(오른쪽)에 함께 뜬다.
    /// </summary>
    public static class Runes
    {
        public static readonly (string key, string desc)[] All =
        {
            ("도발", "앞면일 때 상대 소환수는 이 소환수부터 공격해야 한다."),
            ("저격", "가장 앞이 아니라 가장 뒤(오른쪽) 상대 소환수를 공격한다. [도발]이 있으면 도발이 먼저."),
            ("선봉", "공격 턴에 칸 순서보다 먼저 공격한다. 선봉끼리는 왼쪽 칸부터."),
            ("불변", "파괴 외의 상대 카드 효과(개기일식·데미지·약화·행동 불능·되돌리기)를 받지 않는다. 파괴 효과와 전투로만 쓰러진다."),
            ("재생", "배틀 엔드에 HP를 모두 회복한다."),
            ("암살", "직접 공격할 때 영웅의 반격을 받지 않는다."),
            ("상처", "피해를 받은 소환수는 다음 T 시작에 HP가 회복되지 않는다."),
            ("질풍", "바람 속성: 전투 판정 주사위를 4번 굴려 하나를 고른다 (기본 3번)."),
            ("빛의 가호", "빛 속성: 전투 때 코인이 앞면이면 이 전투 동안 ATK·HP가 오른다."),
            ("어둠의 대가", "어둠 속성: 전투 때 코인이 뒷면이면 이 전투 동안 ATK·HP가 내려간다."),
            ("개기일식", "공개되면 양쪽 소환수가 모두 뒷면이 된다. 그 배틀 동안 뒷면 카드는 움직이지 못하고, [불변]끼리만 싸운다."),
        };

        static string Desc(string key) => All.FirstOrDefault(r => r.key == key).desc;

        /// <summary>이 카드에 해당하는 룬 (이름, 설명) — 글의 [키워드] · 능력 표시 · 주사위 효과 · 속성 규칙에서 모은다</summary>
        public static List<(string key, string desc)> For(CardData d)
        {
            var keys = new List<string>();
            if (d == null) return new List<(string, string)>();
            string text = d.text ?? "";
            void Add(string k) { if (!keys.Contains(k) && Desc(k) != null) keys.Add(k); }
            foreach (var r in All) if (text.Contains("[" + r.key + "]")) Add(r.key);
            if (d.taunt) Add("도발");
            if (d.snipe) Add("저격");
            if (d.vanguard) Add("선봉");
            if (d.resist) Add("불변");
            if (d.regen) Add("재생");
            if (d.noCounter) Add("암살");
            if (text.Contains("개기일식") && !d.resist) Add("개기일식");
            if (d.type == CardType.Monster)
            {
                var dice = d.dice ?? Dice.Default;
                foreach (var e in dice)
                {
                    if (e == null) continue;
                    if (e.wound) Add("상처");
                }
                if (d.element == Element.Wind) Add("질풍");
                if (d.element == Element.Light) Add("빛의 가호");
                if (d.element == Element.Dark) Add("어둠의 대가");
            }
            return keys.Select(k => (k, Desc(k))).ToList();
        }

        /// <summary>표시용 글 (없으면 빈 문자열)</summary>
        public static string Text(CardData d)
        {
            var list = For(d);
            if (list.Count == 0) return "";
            return "<color=#ffd77a><b>룬 해설</b></color>\n" + string.Join("\n", list.Select(r => $"<color=#c9b6ff><b>[{r.key}]</b></color> {r.desc}"));
        }
    }
}
