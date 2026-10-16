using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>운명 재배치 화면: 시작 패 7장 중 바꿀 카드를 누르고 확정 (30초 지나면 고른 그대로 확정)</summary>
    public partial class HUD
    {
        RectTransform fateRoot, fateRow;
        Text fateTitle, fateTimer, fateSub;
        /// <summary>정확히 이만큼 골라야 확정 (-1 = 아무 장수나, 운명 재배치)</summary>
        int fateExact = -1;
        string fateMark = "교체";
        UIKit.Btn fateOk;
        readonly List<CardInstance> fatePicked = new List<CardInstance>();
        Action<List<CardInstance>> fateDone;
        Coroutine fateCo;

        void BuildFate()
        {
            var dim = UIKit.Img(root, "Fate", new Color(0.01f, 0.005f, 0.03f, 0.86f));
            fateRoot = dim.rectTransform;
            fateRoot.Fill();
            dim.raycastTarget = true;
            fateTitle = UIKit.Txt(fateRoot, "Title", "운명 재배치", 54, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            fateTitle.rectTransform.Anchor(0, 0.72f, 1, 0.82f);
            fateTitle.Outline(new Color(0.2f, 0.06f, 0, 0.9f), 2);
            var sub = fateSub = UIKit.Txt(fateRoot, "Sub", "", 22, UIKit.Ink, TextAnchor.MiddleCenter);
            sub.rectTransform.Anchor(0, 0.675f, 1, 0.72f);
            fateTimer = UIKit.Txt(fateRoot, "Timer", "", 26, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            fateTimer.rectTransform.Anchor(0, 0.635f, 1, 0.675f);
            fateRow = UIKit.Rect("Row", fateRoot);
            fateRow.Anchor(0.03f, 0.24f, 0.97f, 0.62f);
            var hl = fateRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 18; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            fateOk = UIKit.Button(fateRoot, "그대로 시작", ConfirmFate, new Color(0.45f, 0.08f, 0.05f, 0.96f), 30, Color.white);
            fateOk.rt.Place(new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(380, 84));
            fateRoot.gameObject.SetActive(false);
        }

        public void ShowFate(List<CardInstance> hand, float seconds, Action<List<CardInstance>> done)
            => ShowPickCards("운명 재배치", "바꿀 카드를 누르세요 — 고른 카드는 덱으로 돌아가고 같은 장수를 새로 뽑습니다 (1회) · 우클릭: 확대", hand, seconds, -1, "교체", done);

        /// <summary>엔드 타임 패 정리: 버릴 카드 need 장을 한 번에 골라 확정 (시간이 지나면 남은 장수는 자동)</summary>
        public void ShowDiscard(List<CardInstance> hand, int need, float seconds, Action<List<CardInstance>> done)
            => ShowPickCards("패 정리", $"패는 엔드 타임에 7장까지 — 버릴 카드 {need}장을 고르세요 · 우클릭: 확대", hand, seconds, need, "버림", done);

        /// <summary>카드 여러 장 고르기 화면 (운명 재배치 · 패 정리 공용)</summary>
        void ShowPickCards(string title, string sub, List<CardInstance> hand, float seconds, int exact, string markText, Action<List<CardInstance>> done)
        {
            if (fateRoot == null) BuildFate();
            CloseGrave();
            fateTitle.text = title;
            fateSub.text = sub;
            fateExact = exact;
            fateMark = markText;
            fateDone = done;
            fatePicked.Clear();
            UIKit.Clear(fateRow);
            foreach (var c in hand)
            {
                var card = c;
                var cell = UIKit.Rect("F_" + c.data.id, fateRow);
                cell.sizeDelta = new Vector2(220, 308);
                cell.HitArea();
                var face = CardFace.Build(cell, 220, 308, 0.32f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(c.data);
                face.SetFinish(c.finish);
                var mark = UIKit.Img(cell, "Mark", new Color(0.05f, 0.02f, 0.08f, 0.72f));
                mark.rectTransform.Fill();
                var mt = UIKit.Txt(mark.transform, "T", fateMark, 40, new Color(1f, 0.55f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
                mt.rectTransform.Fill();
                mt.Outline(new Color(0, 0, 0, 0.9f), 2);
                mark.raycastTarget = false;
                mark.gameObject.SetActive(false);
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () =>
                {
                    if (fatePicked.Contains(card)) fatePicked.Remove(card);
                    else if (fateExact < 0 || fatePicked.Count < fateExact) fatePicked.Add(card);
                    else return;    // 버릴 장수를 이미 다 골랐다
                    mark.gameObject.SetActive(fatePicked.Contains(card));
                    Sfx.Play("flip", 0.5f);
                    RefreshFateButton();
                };
                ev.onRightClick = () => CardZoom.Show(card.data, card.finish);
            }
            RefreshFateButton();
            fateRoot.gameObject.SetActive(true);
            fateRoot.SetAsLastSibling();
            if (fateCo != null) StopCoroutine(fateCo);
            fateCo = StartCoroutine(FateTimer(seconds));
        }

        void RefreshFateButton()
        {
            if (fateExact >= 0)
            {
                fateOk.label.text = fatePicked.Count == fateExact ? $"{fateExact}장 버리기" : $"버릴 카드 {fatePicked.Count} / {fateExact}";
                fateOk.Interactable = fatePicked.Count == fateExact;
                return;
            }
            fateOk.Interactable = true;
            fateOk.label.text = fatePicked.Count == 0 ? "그대로 시작" : $"{fatePicked.Count}장 교체하고 시작";
        }

        IEnumerator FateTimer(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (fateRoot.gameObject.activeSelf)
            {
                float left = Mathf.Max(0, end - Time.unscaledTime);
                fateTimer.text = $"남은 시간 {Mathf.CeilToInt(left)}초";
                fateTimer.color = left <= 5 ? new Color(1f, 0.4f, 0.35f) : new Color(1f, 0.85f, 0.45f);
                if (left <= 0) { ConfirmFate(true); yield break; }
                yield return null;
            }
        }

        void ConfirmFate() => ConfirmFate(false);

        void ConfirmFate(bool timeout)
        {
            if (fateRoot == null || !fateRoot.gameObject.activeSelf) return;
            if (fateExact >= 0 && fatePicked.Count != fateExact && !timeout) return;
            CardZoom.Close();
            fateRoot.gameObject.SetActive(false);
            var cb = fateDone;
            fateDone = null;
            cb?.Invoke(fatePicked.ToList());
        }

        /// <summary>테스트용: 지금 고른 그대로 확정</summary>
        public void TestConfirmFate() => ConfirmFate();
        public bool FateOpen => fateRoot != null && fateRoot.gameObject.activeSelf;
    }
}
