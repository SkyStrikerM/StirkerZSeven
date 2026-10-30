using System;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>필드 전개 버튼 (엑스트라 덱) · 필드 코인 토스 화면</summary>
    public partial class HUD
    {
        UIKit.Btn fieldBtn;
        public RectTransform FieldBtnRt => fieldBtn?.rt;
        RectTransform coinRoot;
        Text coinTitle, coinResult;

        void BuildFieldButton()
        {
            fieldBtn = UIKit.Button(root, "필드 전개", () => { CloseMenu(); if (g != null && g.CanPlayAnyField(g.Human)) g.HumanAct(g.ChooseAndPlayField()); },
                new Color(0.12f, 0.3f, 0.2f, 0.96f), 21, Color.white);
            fieldBtn.rt.Place(new Vector2(0, 0), new Vector2(720, 30), new Vector2(270, 60));
            fieldBtn.Icon(GameAssets.TexSprite("btl_field"), 46);
        }

        /// <summary>필드 버튼: 내 필드 차례 · 엑스트라 덱 남은 장수</summary>
        void RefreshFieldButton()
        {
            if (fieldBtn == null || g == null || g.players[0] == null) return;
            bool show = g.Simul && g.raid == null && g.mode != MatchMode.Puzzle;
            fieldBtn.bg.gameObject.SetActive(show);
            if (!show) return;
            int n = g.Human.extra.Count;
            var turn = g.FieldTurn;
            bool can = g.IsHumanMain && g.CanPlayAnyField(g.Human);
            fieldBtn.label.text = g.time < 2 ? $"필드: T2 부터 (EX {n})"
                : turn == g.Human ? (g.Human.fieldPlayedThisTime ? $"필드 전개함 (EX {n})" : $"필드 전개 ▶ (EX {n})")
                : $"필드: 상대 차례 (EX {n})";
            fieldBtn.Interactable = can;
        }

        // ── 필드 코인 토스 (결과만 표시 — 코인은 자동으로 던진다) ──

        void BuildCoinToss()
        {
            var dim = UIKit.Img(root, "CoinToss", new Color(0.01f, 0.005f, 0.03f, 0.2f));
            coinRoot = dim.rectTransform;
            coinRoot.Fill();
            dim.raycastTarget = true;
            coinTitle = UIKit.Txt(coinRoot, "T", "", 64, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            coinTitle.rectTransform.Anchor(0, 0.7f, 1, 0.82f);
            coinTitle.Outline(new Color(0.2f, 0.06f, 0, 0.9f), 2);
            coinResult = UIKit.Txt(coinRoot, "R", "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            coinResult.rectTransform.Anchor(0, 0.62f, 1, 0.7f);
            coinResult.Outline(new Color(0, 0, 0, 0.9f), 2);
            coinRoot.gameObject.SetActive(false);
        }

        public void ShowCoinResult(bool heads, string verdict)
        {
            if (coinRoot == null) BuildCoinToss();
            coinRoot.gameObject.SetActive(true);
            coinRoot.SetAsLastSibling();
            coinTitle.text = heads ? "앞면!" : "뒷면";
            coinTitle.color = heads ? UIKit.Gold : new Color(0.75f, 0.78f, 0.9f);
            coinResult.text = verdict;
        }

        public void HideCoinToss() { if (coinRoot != null) coinRoot.gameObject.SetActive(false); }
    }
}
