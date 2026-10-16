using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 전투 화면 보조 기능 (로그 위 버튼 줄):
    ///   마나 스탬프 — 짧은 인사 말풍선 (CPU 가 가끔 답한다)
    ///   전투 두루마리 — 이번 대전의 모든 기록을 T 별로
    ///   정찰 노트 — 상대 영웅 · 패/덱/묘지 수 · 지난 배틀 배치 · 공개된 카드
    /// </summary>
    public partial class HUD
    {
        public static readonly string[] Stamps = { "안녕하세요!", "좋은 수네요!", "고마워요", "위험한데…", "놀라워!", "다음엔 지지 않아!" };

        RectTransform extrasBar, stampMenu, scrollRoot, scrollContent, scoutRoot;
        Text scoutText;
        readonly RectTransform[] stampBubble = new RectTransform[2];
        readonly Text[] stampText = new Text[2];
        readonly Coroutine[] stampCo = new Coroutine[2];
        float stampReadyAt;

        void BuildExtras()
        {
            extrasBar = UIKit.Rect("Extras", root);
            extrasBar.Place(new Vector2(1, 0), new Vector2(-18, 276), new Vector2(430, 50), new Vector2(1, 0));
            var hl = extrasBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 6; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = true;
            UIKit.Button(extrasBar, "스탬프", ToggleStampMenu, null, 16).Icon(GameAssets.TexSprite("btl_stamp"), 32);
            UIKit.Button(extrasBar, "두루마리", OpenScroll, null, 16).Icon(GameAssets.TexSprite("nav_quest"), 32);
            UIKit.Button(extrasBar, "정찰 노트", OpenScout, null, 16).Icon(GameAssets.TexSprite("btl_scout"), 32);

            // 스탬프 고르기
            var sm = UIKit.Frame(root, "StampMenu", new Color(0.05f, 0.03f, 0.1f, 0.96f));
            stampMenu = sm.rectTransform;
            stampMenu.Place(new Vector2(1, 0), new Vector2(-18, 330), new Vector2(430, 196), new Vector2(1, 0));
            var grid = UIKit.Rect("Grid", stampMenu).Anchor(0, 0, 1, 1, 10, 10, 10, 10);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(200, 52); gl.spacing = new Vector2(10, 10);
            for (int i = 0; i < Stamps.Length; i++)
            {
                int k = i;
                UIKit.Button(grid, Stamps[i], () => { stampMenu.gameObject.SetActive(false); SendStamp(k); }, UIKit.Dark, 19);
            }
            stampMenu.gameObject.SetActive(false);

            // 말풍선: 0 = 나 (왼쪽 아래 영웅 패널 위), 1 = 상대 (왼쪽 위 영웅 패널 아래)
            for (int side = 0; side < 2; side++)
            {
                var b = UIKit.Frame(root, "Bubble" + side, new Color(0.98f, 0.95f, 0.88f, 0.97f));
                stampBubble[side] = b.rectTransform;
                if (side == 0) stampBubble[side].Place(new Vector2(0, 0), new Vector2(40, 196), new Vector2(330, 74), new Vector2(0, 0));
                else stampBubble[side].Place(new Vector2(0, 1), new Vector2(40, -150), new Vector2(330, 74), new Vector2(0, 1));
                b.raycastTarget = false;
                stampText[side] = UIKit.Txt(b.transform, "T", "", 24, new Color(0.15f, 0.08f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                stampText[side].rectTransform.Fill();
                stampBubble[side].gameObject.SetActive(false);
            }

            // 전투 두루마리
            var sr = UIKit.Img(root, "Scroll", new Color(0, 0, 0, 0.82f));
            scrollRoot = sr.rectTransform;
            scrollRoot.Fill();
            sr.raycastTarget = true;
            var panel = UIKit.Frame(scrollRoot, "Panel", new Color(0.035f, 0.028f, 0.07f, 0.97f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 820));
            var title = UIKit.Txt(panel.transform, "Title", "전투 두루마리", 34, new Color(1f, 0.85f, 0.55f), TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 28, -62, 160, 18);
            title.Section(new Color(1f, 0.82f, 0.45f), 26);
            UIKit.Button(panel.transform, "닫기", () => scrollRoot.gameObject.SetActive(false), UIKit.Dark, 20).rt
                .Place(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(120, 46), new Vector2(1, 1));
            var view = UIKit.Rect("View", panel.transform).Anchor(0, 0, 1, 1, 20, 20, 20, 80);
            view.HitArea();
            view.gameObject.AddComponent<RectMask2D>();
            scrollContent = UIKit.Rect("Content", view);
            scrollContent.anchorMin = new Vector2(0, 1); scrollContent.anchorMax = new Vector2(1, 1); scrollContent.pivot = new Vector2(0.5f, 1);
            scrollContent.offsetMin = scrollContent.offsetMax = Vector2.zero;
            var vl = scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false;
            scrollContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rect = view.gameObject.AddComponent<ScrollRect>();
            rect.content = scrollContent; rect.viewport = view; rect.horizontal = false; rect.movementType = ScrollRect.MovementType.Clamped; rect.scrollSensitivity = 50;
            scrollRoot.gameObject.SetActive(false);

            // 정찰 노트
            var so = UIKit.Img(root, "Scout", new Color(0, 0, 0, 0.6f));
            scoutRoot = so.rectTransform;
            scoutRoot.Fill();
            so.raycastTarget = true;
            var sp = UIKit.Frame(scoutRoot, "Panel", new Color(0.035f, 0.028f, 0.07f, 0.97f));
            sp.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 760));
            var st = UIKit.Txt(sp.transform, "Title", "정찰 노트", 34, new Color(0.55f, 0.95f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            st.rectTransform.Anchor(0, 1, 1, 1, 28, -62, 160, 18);
            st.Section(new Color(0.55f, 0.95f, 1f), 26);
            UIKit.Button(sp.transform, "닫기", () => scoutRoot.gameObject.SetActive(false), UIKit.Dark, 20).rt
                .Place(new Vector2(1, 1), new Vector2(-18, -16), new Vector2(120, 46), new Vector2(1, 1));
            scoutText = UIKit.Txt(sp.transform, "T", "", 20, UIKit.Ink, TextAnchor.UpperLeft);
            scoutText.rectTransform.Anchor(0, 0, 1, 1, 30, 24, 30, 84);
            scoutText.lineSpacing = 1.2f;
            scoutRoot.gameObject.SetActive(false);
        }

        /// <summary>대전 시작 때: 말풍선 · 창 정리</summary>
        void ResetExtras()
        {
            if (extrasBar == null) BuildExtras();
            stampMenu.gameObject.SetActive(false);
            scrollRoot.gameObject.SetActive(false);
            scoutRoot.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++) stampBubble[i].gameObject.SetActive(false);
            extrasBar.gameObject.SetActive(true);
        }

        void ToggleStampMenu()
        {
            stampMenu.gameObject.SetActive(!stampMenu.gameObject.activeSelf);
            stampMenu.SetAsLastSibling();
        }

        void SendStamp(int k)
        {
            if (Time.unscaledTime < stampReadyAt) return;
            stampReadyAt = Time.unscaledTime + 2.5f;
            ShowStamp(0, Stamps[k]);
            // CPU 는 가끔 답한다
            if (Random.value < 0.4f) StartCoroutine(CpuReply(k));
        }

        IEnumerator CpuReply(int k)
        {
            yield return new WaitForSecondsRealtime(1.1f);
            int[] replies = k == 0 ? new[] { 0 } : k == 1 || k == 4 ? new[] { 2 } : k == 2 ? new[] { 0, 1 } : new[] { 5, 4 };
            ShowStamp(1, Stamps[replies[Random.Range(0, replies.Length)]]);
        }

        /// <summary>CPU 쪽 스탬프 (큰 피해 · 승부처에서 가끔)</summary>
        public void CpuStamp(int k) { if (extrasBar != null && Random.value < 0.5f) ShowStamp(1, Stamps[k]); }

        void ShowStamp(int side, string text)
        {
            if (stampCo[side] != null) StopCoroutine(stampCo[side]);
            stampCo[side] = StartCoroutine(StampRoutine(side, text));
        }

        IEnumerator StampRoutine(int side, string text)
        {
            var b = stampBubble[side];
            stampText[side].text = text;
            b.gameObject.SetActive(true);
            b.SetAsLastSibling();
            Sfx.Play("click", 0.6f);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.15f) { b.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t); yield return null; }
            b.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(2.4f);
            b.gameObject.SetActive(false);
        }

        void OpenScroll()
        {
            UIKit.Clear(scrollContent);
            foreach (var grp in g.ScrollLog.GroupBy(x => x.time))
            {
                var head = UIKit.Txt(scrollContent, "H", grp.Key > 0 ? $"── TIME {grp.Key} ──" : "── 대전 준비 ──", 24, new Color(1f, 0.85f, 0.55f), TextAnchor.MiddleLeft, FontStyle.Bold);
                head.gameObject.AddComponent<LayoutElement>().minHeight = 34;
                var body = UIKit.Txt(scrollContent, "B", string.Join("\n", grp.Select(x => Colorize(x.msg))), 18, UIKit.Ink, TextAnchor.UpperLeft);
                body.lineSpacing = 1.1f;
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
                var fit = body.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            scrollRoot.SetAsLastSibling();
            scrollRoot.gameObject.SetActive(true);
            // 맨 아래(최근)부터 보이게
            var sr = scrollContent.parent.GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0;
        }

        string Colorize(string msg)
        {
            if (g == null || g.players[0] == null) return msg;
            if (msg.StartsWith(g.Human.name + ":")) return $"<color=#9fd0ff>{msg}</color>";
            if (msg.StartsWith(g.Cpu.name + ":")) return $"<color=#ffb0a0>{msg}</color>";
            if (msg.Contains("파괴")) return $"<color=#ffcf8a>{msg}</color>";
            return msg;
        }

        void OpenScout()
        {
            var o = g.Cpu;
            if (o == null) return;
            var h = o.hero?.data;
            var lines = new List<string>
            {
                $"<color=#ffd77a><b>{o.name}</b></color>   영웅 <b>{h?.name}</b> · HP {o.hp}/{o.maxHp} · 영웅 ATK {g.HeroAtk(o)}",
                $"영웅 능력: {h?.text}  {(o.heroUsed ? "<color=#f99>(사용함)</color>" : "<color=#9f9>(아직 사용 안 함)</color>")}",
                $"패 {o.hand.Count}장 · 덱 {o.deck.Count}장 · 묘지 {o.grave.Count}장",
                "",
                "<color=#ffd77a><b>지난 배틀 상대 칸</b></color>",
                g.LastCpuLanes == null ? "<color=#999>아직 배틀 전</color>" : string.Join("   ", g.LastCpuLanes.Select((n, i) => $"{i + 1}: {(string.IsNullOrEmpty(n) ? "-" : n)}")),
                "",
                "<color=#ffd77a><b>이번 대전에서 공개된 상대 카드</b></color>",
                g.CpuSeen.Count == 0 ? "<color=#999>없음</color>" : string.Join(" · ", g.CpuSeen.GroupBy(c => c.name).Select(x => x.Count() > 1 ? $"{x.Key} ×{x.Count()}" : x.Key)),
                "",
                "<color=#ffd77a><b>상대 묘지</b></color>",
                o.grave.Count == 0 ? "<color=#999>비어 있음</color>" : string.Join(" · ", o.grave.GroupBy(c => c.data.name).Select(x => x.Count() > 1 ? $"{x.Key} ×{x.Count()}" : x.Key)),
            };
            scoutText.text = string.Join("\n", lines);
            scoutRoot.SetAsLastSibling();
            scoutRoot.gameObject.SetActive(true);
        }
    }
}
