using System;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>도전 › 원소 이변전 칸: 이번 주 규칙 · 보상 · 참가</summary>
    public partial class MainMenu
    {
        public Action<DeckData> onEvent;
        Text eventName, eventDesc, eventState;
        UIKit.Btn eventBtn;

        partial void BuildEventPanel(RectTransform col)
        {
            eventName = UIKit.Txt(col, "Rule", "", 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            eventName.rectTransform.Anchor(0, 1, 1, 1, 22, -130, 18, 82);
            eventDesc = UIKit.Txt(col, "Desc", "", 20, UIKit.Ink, TextAnchor.UpperLeft);
            eventDesc.rectTransform.Anchor(0, 1, 1, 1, 22, -300, 18, 140);
            eventDesc.lineSpacing = 1.2f;
            eventState = UIKit.Txt(col, "State", "", 19, UIKit.Muted, TextAnchor.UpperLeft);
            eventState.rectTransform.Anchor(0, 0, 1, 0, 22, 130, 18, -330);
            eventState.lineSpacing = 1.25f;
            eventBtn = UIKit.Button(col, "이변전 참가", () =>
            {
                var deck = Profile.ActiveDeck;
                string err = DeckRules.Validate(deck);
                if (err != null) { Toast(err); Show(Tab.Deck); return; }
                onEvent?.Invoke(deck);
            }, Red, 30, Color.white);
            eventBtn.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(340, 88), new Vector2(0.5f, 0));
        }

        partial void RefreshEventPanel()
        {
            var r = ElementEvent.ThisWeek;
            var left = ElementEvent.UntilNextWeek;
            eventName.text = $"이번 주 : <color=#ffb36b>{ElementEvent.Name(r)}</color>";
            eventDesc.text = ElementEvent.Desc(r) + "\n\n<color=#aaa>상대: CPU 정예 · 내 사용 덱으로 참가</color>";
            eventState.text =
                $"보상  승리마다 오팔 {ElementEvent.WinOpal} · 이번 주 첫 승리 오팔 {ElementEvent.FirstWinOpal}\n" +
                $"이번 주 기록  {ElementEvent.WinsThisWeek}승 / {ElementEvent.PlaysThisWeek}판{(ElementEvent.WonThisWeek ? "  <color=#8f8>✔ 첫 승리 보상 받음</color>" : "")}\n" +
                $"다음 규칙까지  {left.Days}일 {left.Hours}시간  ·  사용 덱 [{Profile.ActiveDeck.name}]";
        }
    }
}
