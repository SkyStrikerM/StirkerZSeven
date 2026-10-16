using System;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 카드 확대 창 (덱 편집 · 전투 공용). 열기 → 1단계 확대 → 클릭 → 2단계(화면 높이) → 클릭 → 닫기. 우클릭·Esc 는 바로 닫기.
    /// 자체 Canvas(정렬 순서 45)라서 메뉴·HUD·덱 페이지 위에 그려지고 입력도 먼저 받는다.
    /// </summary>
    public class CardZoom : MonoBehaviour
    {
        static CardZoom inst;
        static readonly float[] Scales = { 0f, 0.6f, 0.98f };   // 카드 원본 750×1050 기준

        RectTransform card;
        CardFace face;
        Text hint;
        RectTransform runeBox;
        Text runeText;
        int level;
        Coroutine anim;

        public static bool IsOpen => inst != null && inst.level > 0;
        public static int Level => inst != null ? inst.level : 0;

        /// <summary>atk/hp 가 -1 이면 카드 기본 수치</summary>
        public static void Show(CardData d, CardFinish finish, int atk = -1, int hp = -1)
        {
            if (d == null) return;
            if (inst == null) inst = Build();
            inst.Open(d, finish, atk, hp);
        }

        public static void Close() { if (inst != null) inst.CloseNow(); }

        /// <summary>
        /// 칸 클릭(왼쪽·오른쪽) = 그 순간의 카드를 확대. card() 가 null 이면 무시.
        /// finish 가 없으면 일반, stats 가 없으면 카드 기본 ATK·HP.
        /// </summary>
        public static CellEvents Bind(RectTransform rt, Func<CardData> card, Func<CardFinish> finish = null, Func<(int atk, int hp)> stats = null)
        {
            rt.HitArea();
            var ev = rt.GetComponent<CellEvents>();
            if (ev == null) ev = rt.gameObject.AddComponent<CellEvents>();
            ev.onLeftClick = ev.onRightClick = () =>
            {
                var c = card();
                if (c == null) return;
                var (atk, hp) = stats?.Invoke() ?? (-1, -1);
                Show(c, finish?.Invoke() ?? CardFinish.Normal, atk, hp);
            };
            return ev;
        }

        /// <summary>확대 창을 한 번 클릭한 것과 같다 (1단계 → 2단계 → 닫기)</summary>
        public static void Advance() { if (inst != null) inst.Next(); }

        static CardZoom Build()
        {
            var go = new GameObject("CardZoom", typeof(RectTransform));
            UIKit.ScreenCanvas(go, 45);
            var z = go.AddComponent<CardZoom>();

            // 프로젝트가 리니어 색 공간이라 알파를 높게 줘야 충분히 어두워진다
            var bg = UIKit.Img(go.transform, "Dim", new Color(0, 0, 0, 0.92f));
            bg.raycastTarget = true;
            bg.rectTransform.Fill();
            var ev = bg.gameObject.AddComponent<CellEvents>();
            ev.onLeftClick = z.Next;
            ev.onRightClick = z.CloseNow;

            z.card = UIKit.Rect("Card", go.transform);
            z.card.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(750, 1050));
            z.face = CardFace.Build(z.card, 750, 1050);
            z.face.root.Fill();
            z.face.glow.gameObject.SetActive(false);
            z.hint = UIKit.Txt(go.transform, "Hint", "", 20, UIKit.Muted, TextAnchor.LowerCenter);
            z.hint.rectTransform.Anchor(0, 0, 1, 0, 0, 14, 0, -50);
            z.hint.Shadow();
            // 룬 해설 (화면 오른쪽)
            var rb = UIKit.Frame(go.transform, "Runes", new Color(0.05f, 0.03f, 0.09f, 0.94f));
            z.runeBox = rb.rectTransform;
            z.runeBox.Place(new Vector2(1, 0.5f), new Vector2(-36, 0), new Vector2(470, 300), new Vector2(1, 0.5f));
            rb.raycastTarget = false;
            z.runeText = UIKit.Txt(rb.transform, "T", "", 22, UIKit.Ink, TextAnchor.UpperLeft);
            z.runeText.rectTransform.Anchor(0, 0, 1, 1, 22, 18, 20, 18);
            z.runeText.lineSpacing = 1.15f;
            go.SetActive(false);
            return z;
        }

        void Open(CardData d, CardFinish finish, int atk, int hp)
        {
            face.Set(d, atk, hp);
            face.SetFinish(finish);
            string runes = Runes.Text(d);
            runeBox.gameObject.SetActive(runes != "");
            runeText.text = runes;
            if (runes != "") runeBox.sizeDelta = new Vector2(470, 40 + runeText.preferredHeight);
            gameObject.SetActive(true);
            card.localScale = Vector3.one * Scales[1] * 0.85f;
            SetLevel(1);
        }

        void Next()
        {
            if (level >= 2) CloseNow();
            else if (level > 0) SetLevel(level + 1);
        }

        void SetLevel(int l)
        {
            level = l;
            hint.text = "클릭: 더 크게  ·  우클릭: 닫기";
            hint.gameObject.SetActive(l == 1);
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(ScaleTo(Scales[l]));
        }

        System.Collections.IEnumerator ScaleTo(float target)
        {
            float from = card.localScale.x;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.14f)
            {
                float k = 1 - (1 - t) * (1 - t);
                card.localScale = Vector3.one * Mathf.Lerp(from, target, k);
                yield return null;
            }
            card.localScale = Vector3.one * target;
            anim = null;
        }

        void CloseNow()
        {
            if (anim != null) StopCoroutine(anim);
            anim = null;
            level = 0;
            gameObject.SetActive(false);
        }
    }
}
