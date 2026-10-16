using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 도전 탭: [마법진 퍼즐] [원소 이변전] [원소 투기장] 세 칸.
    /// 이변전 · 투기장 칸은 MainMenu.Event.cs · MainMenu.Arena.cs 에서 채운다.
    /// </summary>
    public partial class MainMenu
    {
        public Action<PuzzleDef> onPuzzle;

        RectTransform puzzleList;
        Text puzzleProgress;

        RectTransform BuildChallenge()
        {
            var page = UIKit.Rect("Challenge", contentArea).Fill();
            var colP = ChallengeColumn(page, 0, "마법진 퍼즐", new Color(0.75f, 0.6f, 1f));
            var colE = ChallengeColumn(page, 1, "원소 이변전", new Color(1f, 0.7f, 0.4f));
            var colA = ChallengeColumn(page, 2, "원소 투기장", new Color(0.55f, 0.9f, 1f));

            var d = UIKit.Txt(colP, "Desc", "정해진 패와 상대 필드에서 이번 T 안에 상대 영웅을 쓰러뜨려라.\n주사위·코인 효과 없음 · 영웅 능력 없음 · 시간 제한 없음 · 처음 풀면 오팔", 17, UIKit.Ink, TextAnchor.UpperLeft);
            d.rectTransform.Anchor(0, 1, 1, 1, 22, -150, 18, 70);
            puzzleProgress = UIKit.Txt(colP, "Prog", "", 19, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            puzzleProgress.rectTransform.Anchor(0, 1, 1, 1, 22, -186, 18, 152);
            puzzleList = UIKit.Rect("List", colP).Anchor(0, 0, 1, 1, 16, 16, 16, 196);
            var vl = puzzleList.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.childControlWidth = true; vl.childControlHeight = false; vl.childForceExpandHeight = false;

            BuildEventPanel(colE);
            BuildArenaPanel(colA);
            return page;
        }

        static RectTransform ChallengeColumn(RectTransform page, int i, string title, Color col)
        {
            var f = UIKit.Frame(page, "Col" + i, new Color(0.03f, 0.02f, 0.06f, 0.92f));
            f.rectTransform.Anchor(i / 3f, 0, (i + 1) / 3f, 1, i == 0 ? 0 : 8, 0, i == 2 ? 0 : 8, 0);
            var t = UIKit.Txt(f.transform, "T", title, 34, col, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Anchor(0, 1, 1, 1, 22, -64, 16, 20);
            t.Section(col, 26);
            return f.rectTransform;
        }

        void RefreshChallenge()
        {
            UIKit.Clear(puzzleList);
            puzzleProgress.text = $"클리어 {Puzzles.ClearedCount} / {Puzzles.All.Count}";
            for (int i = 0; i < Puzzles.All.Count; i++)
            {
                var p = Puzzles.All[i];
                bool done = Puzzles.Cleared(p.id);
                bool open = i == 0 || Puzzles.Cleared(Puzzles.All[i - 1].id);   // 앞 문제를 풀면 열린다
                var b = UIKit.Button(puzzleList, "", () => { if (open) onPuzzle?.Invoke(p); else Toast("앞 퍼즐을 먼저 풀어야 열립니다"); },
                    done ? new Color(0.1f, 0.2f, 0.12f, 0.95f) : open ? new Color(0.16f, 0.1f, 0.26f, 0.95f) : new Color(0.08f, 0.08f, 0.1f, 0.9f), 20, open ? Color.white : UIKit.Muted);
                b.rt.sizeDelta = new Vector2(0, 74);
                b.label.alignment = TextAnchor.MiddleLeft;
                b.label.rectTransform.Anchor(0, 0, 1, 1, 18, 4, 14, 4);
                b.label.text = $"<b>{i + 1}. {p.title}</b>   <size=16><color=#ccc>상대 HP {p.oppHp} · T{p.time}{(p.humanFirst ? "" : " · 상대 선")}</color></size>\n" +
                               $"<size=16>{(done ? "<color=#8f8>✔ 클리어</color>" : open ? $"<color=#ffd77a>첫 클리어 오팔 {p.reward}</color>" : "<color=#888>잠김</color>")}</size>";
            }
            RefreshEventPanel();
            RefreshArenaPanel();
        }

        partial void BuildEventPanel(RectTransform col);
        partial void RefreshEventPanel();
        partial void BuildArenaPanel(RectTransform col);
        partial void RefreshArenaPanel();
    }
}
