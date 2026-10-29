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
        /// <summary>카드를 누르는 즉시 확정 (원소 고르기 — 확정 버튼 없음)</summary>
        bool fateInstant;
        Image fateDim;
        string fateMark = "교체";
        UIKit.Btn fateOk;
        readonly List<CardInstance> fatePicked = new List<CardInstance>();
        Action<List<CardInstance>> fateDone;
        Coroutine fateCo;

        void BuildFate()
        {
            var dim = fateDim = UIKit.Img(root, "Fate", new Color(0.01f, 0.005f, 0.03f, 0.86f));
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

        /// <summary>
        /// T 시작 원소 생성: 원소 카드를 속성 순서대로 화면 가운데 나열하고, 누르는 즉시 그 원소가 영웅 아래로.
        /// 제목과 남은 시간만 보인다 (설명 줄·카드 아래 글 없음). 시간이 지나면 아무것도 고르지 않은 것 (호출 쪽이 추천 원소로 대신한다).
        /// 보드가 보이도록 어둡기를 낮춘다
        /// </summary>
        public void ShowElementPick(string title, List<CardInstance> cards, float seconds, Action<CardInstance> done)
            => ShowPickCards(title, "", cards, seconds, 1, "", list => done(list.FirstOrDefault()), null, true, 0.6f);

        /// <summary>카드 여러 장 고르기 화면 (운명 재배치 · 패 정리 · 원소 고르기 공용). caption = 카드 아래 한 줄, instant = 누르면 바로 확정</summary>
        void ShowPickCards(string title, string sub, List<CardInstance> hand, float seconds, int exact, string markText, Action<List<CardInstance>> done,
            Func<CardInstance, string> caption = null, bool instant = false, float dimAlpha = 0.86f)
        {
            if (fateRoot == null) BuildFate();
            CloseGrave();
            fateTitle.text = title;
            fateSub.text = sub;
            fateExact = exact;
            fateInstant = instant;
            fateMark = markText;
            fateDone = done;
            fatePicked.Clear();
            var dc = fateDim.color; dc.a = dimAlpha; fateDim.color = dc;
            fateOk.rt.gameObject.SetActive(!instant);
            UIKit.Clear(fateRow);
            // 원소 고르기는 카드를 조금 줄이고 줄을 올려서, 아래 설명 줄이 패와 겹치지 않게
            float cw = instant ? 200 : 220, ch = instant ? 280 : 308;
            fateRow.Anchor(0.03f, instant ? 0.31f : 0.24f, 0.97f, instant ? 0.635f : 0.62f);
            float capH = caption != null ? 44f : 0f;
            foreach (var c in hand)
            {
                var card = c;
                var cell = UIKit.Rect("F_" + c.data.id, fateRow);
                cell.sizeDelta = new Vector2(cw, ch + capH);
                cell.HitArea();
                var cardRt = UIKit.Rect("Card", cell);
                cardRt.Anchor(0, capH / (ch + capH), 1, 1);
                var face = CardFace.Build(cardRt, cw, ch, 0.32f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(c.data);
                face.SetFinish(c.finish);
                if (caption != null)
                {
                    var cap = UIKit.Txt(cell, "Cap", caption(c), 22, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    cap.rectTransform.Anchor(0, 0, 1, capH / (ch + capH));
                    cap.Outline(new Color(0, 0, 0, 0.9f), 1.5f);
                    cap.raycastTarget = false;
                }
                var mark = UIKit.Img(cardRt, "Mark", new Color(0.05f, 0.02f, 0.08f, 0.72f));
                mark.rectTransform.Fill();
                var mt = UIKit.Txt(mark.transform, "T", fateMark, 40, new Color(1f, 0.55f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
                mt.rectTransform.Fill();
                mt.Outline(new Color(0, 0, 0, 0.9f), 2);
                mark.raycastTarget = false;
                mark.gameObject.SetActive(false);
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () =>
                {
                    if (fateInstant)
                    {
                        fatePicked.Clear(); fatePicked.Add(card);
                        Sfx.Play("flip", 0.5f);
                        ConfirmFate();
                        return;
                    }
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
            if (fateInstant) return;
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
            if (fateExact >= 0 && fatePicked.Count != fateExact && !timeout && !fateInstant) return;
            CardZoom.Close();
            fateRoot.gameObject.SetActive(false);
            var cb = fateDone;
            fateDone = null;
            cb?.Invoke(fatePicked.ToList());
        }

        /// <summary>오토 전환: 고르기 화면을 결과 없이 닫는다 (호출 쪽이 AI 기준으로 대신 고른다)</summary>
        public void CancelPick()
        {
            if (fateRoot == null || !fateRoot.gameObject.activeSelf) return;
            CardZoom.Close();
            fateDone = null;
            fateRoot.gameObject.SetActive(false);
        }

        /// <summary>테스트용: 지금 고른 그대로 확정</summary>
        public void TestConfirmFate() => ConfirmFate();
        public bool FateOpen => fateRoot != null && fateRoot.gameObject.activeSelf;
    }
}
