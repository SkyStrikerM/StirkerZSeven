using System.Collections.Generic;

namespace StrikerZ
{
    /// <summary>덱 구성 튜토리얼 한 단계: 덱 화면의 어느 부분(target)을 테두리로 짚으며 설명한다 (null = 화면 전체)</summary>
    public class TutorialStep
    {
        public string target, title, text;
        public TutorialStep(string target, string title, string text) { this.target = target; this.title = title; this.text = text; }
    }

    /// <summary>
    /// 튜토리얼 (홈 › 대전 설정 › [튜토리얼]): ① 덱 화면에서 덱 구성 배우기 ② 수련 CPU 와 실제 대전하며 진행 순서 배우기.
    /// 덱 단계는 MainMenu.Tutorial.cs 가, 대전 코치 팁은 GameManager.StoryCoach 가 (스토리 팁과 같은 상자) 보여 준다.
    /// </summary>
    public static class TutorialData
    {
        public static readonly List<TutorialStep> DeckSteps = new List<TutorialStep>
        {
            new TutorialStep(null, "덱 화면",
                "왼쪽은 <b>카드 상세</b>, 가운데는 <b>지금 편집 중인 덱</b>, 오른쪽은 <b>보유 카드 리스트</b>입니다.\n하나씩 살펴볼게요. [다음 ▶] 을 누르세요."),
            new TutorialStep("tabs", "덱 목록",
                "위쪽 탭이 내 덱들입니다 (최대 8개). <b>[+ 새 덱]</b> 으로 만들고, <b>[사용 덱으로]</b> 를 누른 덱이 대전에 나갑니다.\n이름 칸을 눌러 덱 이름도 바꿀 수 있어요."),
            new TutorialStep("hero", "영웅 (리더)",
                "덱에는 <b>영웅 1장</b>이 있습니다. 영웅 칸을 클릭해 바꿀 수 있어요.\n영웅의 <b>속성</b>이 중요합니다 — 영웅 아래에 쌓을 수 있는 원소 수가 속성마다 달라요: 같은 속성 <b>10</b>, 내가 이기는 속성 <b>5</b>, 상관없는 속성 <b>3</b>, 약점 속성은 <b>1</b>."),
            new TutorialStep("collection", "카드 넣기",
                "오른쪽 리스트의 카드를 <b>더블클릭</b>하면 덱에 들어가고, 덱 안의 카드를 더블클릭하면 빠집니다.\n한 번 클릭하면 왼쪽에 상세가 보여요. 검색·정렬·속성 필터로 카드를 찾으세요."),
            new TutorialStep("deck", "메인 덱 40장",
                "메인 덱은 <b>정확히 40장</b>. 같은 카드는 커먼·언커먼 <b>3장</b>, 레어 <b>2장</b>, 유니크 <b>1장</b>까지. 유니크 몬스터와 유니크 마법은 덱에 각 1장만.\n추천 비율은 소환수 28 · 마법 12 — 비용이 낮은 소환수를 넉넉히 넣어야 첫 T 부터 낼 수 있어요."),
            new TutorialStep("extra", "엑스트라 덱 (필드)",
                "<b>필드 카드</b>는 메인 덱이 아니라 <b>엑스트라 덱</b>에 넣습니다 (최대 10장, 같은 필드는 1장).\n대전 중 내 필드 차례(짝수 또는 홀수 T)에 펼쳐서 전장 효과를 바꿉니다."),
            new TutorialStep("stats", "원소 비용과 덱 분석",
                "카드마다 <b>원소 비용</b>이 있고, 매 T 원소 1장을 영웅 아래에 쌓아 <b>같은 속성 카드의 비용</b>으로 씁니다.\n그래서 덱은 <b>영웅 속성 위주</b>로, 약점 속성은 피하는 게 좋아요. 덱 분석에서 속성 비율과 비용 분포를 확인하세요."),
            new TutorialStep("tools", "도움 버튼",
                "막히면 <b>[자동 구성]</b> 이 보유 카드로 40장을 짜 주고, <b>[스타터 덱]</b> 에서 기본 덱을 받을 수 있어요.\n<b>[훈련장]</b> 에서는 수련 골렘과 기록 없이 연습할 수 있습니다."),
            new TutorialStep(null, "준비 완료",
                "덱 머리말이 <b>'사용 가능'</b> 이면 대전에 나갈 수 있어요.\n이제 실제 대전 순서를 배워 봅시다 — <b>[대전 배우기 ▶]</b> 를 누르면 수련 CPU 와 튜토리얼 대전이 시작됩니다 (기록·보상 없음, 배치 시간 제한 없음)."),
        };

        /// <summary>대전 튜토리얼 코치 팁 (key = 보여줄 때: start · fate · element · main · magic · hero · battle · end)</summary>
        public static readonly List<StoryTip> MatchTips = new List<StoryTip>
        {
            new StoryTip { key = "fate", title = "① 운명 재배치", text = "시작 패 7장 중 마음에 안 드는 카드를 눌러 고르고 [교체] 하면 같은 장수를 새로 뽑습니다 (1회). [그대로 시작] 도 됩니다.\n첫 T 에 낼 수 있는 비용 1~2 소환수는 남겨 두세요." },
            new StoryTip { key = "start", title = "대전의 흐름", text = "매 TIME 은 ① 원소 고르기 → ② 카드 배치(뒷면) → ③ 배틀 → ④ 엔드 순서로 진행됩니다.\n튜토리얼에서는 배치 시간 제한이 없으니 천천히 보세요. (이 상자는 클릭하면 닫힙니다)" },
            new StoryTip { key = "element", title = "② 원소 고르기", text = "T 시작마다 원소 1장을 골라 영웅 아래에 쌓습니다. 쌓인 원소는 매 T 다시 쓸 수 있고, 같은 속성 카드의 비용이 됩니다.\n영웅 속성 원소를 먼저 쌓는 게 기본! 영웅 아래 원소 더미를 클릭하면 한눈에 볼 수 있어요." },
            new StoryTip { key = "main", title = "③ 카드 배치", text = "패의 카드를 빈 칸으로 <b>끌어다 놓으면</b> 뒷면으로 배치됩니다. 비용(원소)이 되는 카드만 <color=#8f8>초록 테두리</color>. 우클릭하면 바로 낼 수도 있어요.\n왼쪽 칸부터 차례로 싸우니 튼튼한 소환수를 앞에! 다 놓았으면 오른쪽 <b>[배치 완료]</b>." },
            new StoryTip { key = "magic", title = "마법 카드", text = "마법은 칸에 <b>세트</b>해 두면 배틀 때 그 칸 차례에 발동합니다. 소환수처럼 칸에 끌어다 놓으세요.\n't' · 'half' 마법은 배틀 중 클릭해서 쓰는 즉발 마법입니다." },
            new StoryTip { key = "hero", title = "영웅 능력", text = "영웅 능력은 대전 중 쓸 수 있는 강력한 한 수 — 왼쪽 아래 <b>[영웅 능력]</b> 버튼이나 H 키.\n아껴 두었다가 큰 몬스터가 나올 때 쓰세요." },
            new StoryTip { key = "battle", title = "④ 배틀", text = "양쪽 카드가 공개되고 <b>왼쪽 칸부터</b> 차례로 싸웁니다. 공격 턴(쌍검)엔 내 소환수가 공격, 수비 턴(방패)엔 반격만 — 턴은 T 마다 번갈아요.\n상대 칸이 비어 있으면 영웅이 직접 맞습니다. 주사위 효과가 나오면 구간을 골라 주세요." },
            new StoryTip { key = "end", title = "⑤ 엔드", text = "엔드 타임엔 패가 7장을 넘으면 버릴 카드를 고릅니다. 다음 T 엔 T 번호만큼 드로우해요.\n상대 영웅의 HP 를 0 으로 만들면 승리! 덱이 바닥나면 영웅끼리 결투합니다." },
        };
    }
}
