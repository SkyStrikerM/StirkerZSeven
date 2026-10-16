using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 스토리 컷신 플레이어 (코믹스 패널 연출).
    /// 배경 일러스트를 천천히 확대·이동(켄 번즈)하고 장면이 바뀔 때 교차 페이드, 대사는 타자기 효과.
    /// 클릭 · Space · Enter = 다음 (타자 중이면 문장 완성), SKIP = 전부 건너뛰기.
    /// 대사 fx: "shake" 화면 흔들림 · "flash" 섬광. who "#title" = 장 제목 카드.
    /// </summary>
    public class StoryPlayer : MonoBehaviour
    {
        RectTransform root, bgHolder, box, nameplate, portraitRing, titleRoot;
        RawImage bgA, bgB;
        Image flash, portrait, nextMark, nameBg;
        Text nameText, lineText, titleMain, titleSub, skipLabel;
        CanvasGroup boxGroup, titleGroup, rootGroup;

        bool advance, skipAll, typing;
        float shake;
        Vector2 panFrom, panTo;
        float kbT, kbDur = 9f, kbScaleFrom = 1.04f, kbScaleTo = 1.12f;
        RawImage currentBg;

        public bool Playing { get; private set; }
        /// <summary>자동 테스트: 대사를 자동으로 넘김</summary>
        public static bool AutoAdvance;

        public void Build()
        {
            var go = new GameObject("StoryUI", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = UIKit.ScreenCanvas(go, 60);
            root = (RectTransform)go.transform;
            rootGroup = go.AddComponent<CanvasGroup>();

            var black = UIKit.Img(root, "Black", Color.black);
            black.rectTransform.Fill();
            black.raycastTarget = true;
            var clicker = black.gameObject.AddComponent<Button>();
            clicker.transition = Selectable.Transition.None;
            clicker.onClick.AddListener(() => advance = true);

            bgHolder = UIKit.Rect("Bg", root).Fill();
            bgA = MakeBg("A");
            bgB = MakeBg("B");

            // 비네팅: 가장자리를 부드럽게 어둡게 (딱딱한 띠 대신 빛 번짐 스프라이트를 뒤집어 사용)
            var vig = UIKit.Img(root, "Vignette", new Color(0, 0, 0, 0.5f), GameAssets.Glow);
            vig.rectTransform.Anchor(-0.35f, -0.9f, 1.35f, 0.32f);
            vig.raycastTarget = false;

            flash = UIKit.Img(root, "Flash", new Color(1, 1, 1, 0));
            flash.rectTransform.Fill();
            flash.raycastTarget = false;

            // 대사 상자
            var b = UIKit.Frame(root, "Box", new Color(0.03f, 0.025f, 0.07f, 0.9f));
            box = b.rectTransform;
            box.Anchor(0.06f, 0, 0.94f, 0, 0, 34, 0, -264);
            b.raycastTarget = false;
            boxGroup = b.gameObject.AddComponent<CanvasGroup>();
            boxGroup.blocksRaycasts = false;

            portraitRing = UIKit.Img(box, "PortraitRing", UIKit.Gold, GameAssets.Rounded, true).rectTransform;
            portraitRing.Place(new Vector2(0, 0.5f), new Vector2(22, 8), new Vector2(196, 196), new Vector2(0, 0.5f));
            var mask = UIKit.Img(portraitRing, "Mask", Color.white, GameAssets.Rounded, true);
            mask.rectTransform.Anchor(0, 0, 1, 1, 4, 4, 4, 4);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            portrait = UIKit.Img(mask.transform, "Face", Color.white);
            portrait.rectTransform.Anchor(-0.3f, -0.62f, 1.3f, 1.08f);   // 얼굴 쪽으로 확대
            portrait.preserveAspect = true;

            nameBg = UIKit.Img(box, "NameBg", new Color(0.12f, 0.08f, 0.03f, 0.97f), GameAssets.Rounded, true);
            nameplate = nameBg.rectTransform;
            nameplate.Place(new Vector2(0, 1), new Vector2(238, 26), new Vector2(260, 52), new Vector2(0, 0.5f));
            var nb = UIKit.Img(nameplate, "Border", UIKit.Gold, GameAssets.RoundedOutline, true);
            nb.rectTransform.Fill();
            nameText = UIKit.Txt(nameplate, "Name", "", 28, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            nameText.rectTransform.Fill();
            nameText.Shadow();

            lineText = UIKit.Txt(box, "Line", "", 30, UIKit.Ink, TextAnchor.UpperLeft);
            lineText.rectTransform.Anchor(0, 0, 1, 1, 242, 26, 40, 42);
            lineText.lineSpacing = 1.15f;
            lineText.Shadow(new Color(0, 0, 0, 0.8f), 1.5f);

            nextMark = UIKit.Img(box, "Next", UIKit.Gold, GameAssets.Circle);
            nextMark.rectTransform.Place(new Vector2(1, 0), new Vector2(-34, 26), new Vector2(16, 16));

            // 장 제목 카드
            titleRoot = UIKit.Rect("Title", root).Fill();
            titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titleGroup.blocksRaycasts = false;
            var tbg = UIKit.Img(titleRoot, "Dim", new Color(0, 0, 0, 0.55f));
            tbg.rectTransform.Fill();
            titleMain = UIKit.Txt(titleRoot, "Main", "", 96, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleMain.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1800, 140));
            titleMain.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleMain.Outline(new Color(0.1f, 0.05f, 0, 0.9f), 3f).Shadow(new Color(0, 0, 0, 0.8f), 5f);
            titleSub = UIKit.Txt(titleRoot, "Sub", "", 40, Color.white, TextAnchor.MiddleCenter);
            titleSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1800, 70));
            titleSub.Shadow(new Color(0, 0, 0, 0.9f), 2f);
            titleGroup.alpha = 0;

            var skip = UIKit.Button(root, "SKIP ▶▶", () => skipAll = true, new Color(0.05f, 0.04f, 0.08f, 0.8f), 22);
            skip.rt.Place(new Vector2(1, 1), new Vector2(-24, -20), new Vector2(170, 52), new Vector2(1, 1));
            skipLabel = skip.label;

            root.gameObject.SetActive(false);
        }

        RawImage MakeBg(string n)
        {
            var r = new GameObject("Bg" + n, typeof(RectTransform)).AddComponent<RawImage>();
            r.transform.SetParent(bgHolder, false);
            r.rectTransform.anchorMin = r.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            r.rectTransform.sizeDelta = new Vector2(1920, 1080);
            r.raycastTarget = false;
            r.color = new Color(1, 1, 1, 0);
            return r;
        }

        /// <summary>장면 재생. result: 1 승리 · 2 패배 · 0 (전투 전) — 그에 맞는 cond 대사만</summary>
        public void Play(List<StoryScene> scenes, int result, Action onDone)
        {
            StopAllCoroutines();
            StartCoroutine(Run(scenes, result, onDone));
        }

        IEnumerator Run(List<StoryScene> scenes, int result, Action onDone)
        {
            Playing = true;
            skipAll = false;
            root.gameObject.SetActive(true);
            rootGroup.alpha = 0;
            bgA.color = bgB.color = new Color(1, 1, 1, 0);
            currentBg = null;
            boxGroup.alpha = 0;
            titleGroup.alpha = 0;
            yield return Fade(rootGroup, 1, 0.35f);

            foreach (var sc in scenes)
            {
                if (skipAll) break;
                yield return ShowBg(sc.bg);
                foreach (var ln in sc.lines)
                {
                    if (skipAll) break;
                    if (ln.cond != 0 && ln.cond != result) continue;
                    if (ln.who == "#title") { yield return TitleCard(ln.text); continue; }
                    yield return Say(ln);
                }
            }
            yield return Fade(rootGroup, 0, 0.35f);
            root.gameObject.SetActive(false);
            Playing = false;
            onDone?.Invoke();
        }

        IEnumerator ShowBg(string bg)
        {
            var tex = string.IsNullOrEmpty(bg) ? null : Resources.Load<Texture2D>(StoryData.Root + bg);
            var next = currentBg == bgA ? bgB : bgA;
            next.texture = tex;
            next.transform.SetAsLastSibling();
            // 켄 번즈: 장면마다 확대 방향·이동 방향을 바꾼다
            bool zoomIn = UnityEngine.Random.value < 0.65f;
            kbScaleFrom = zoomIn ? 1.03f : 1.12f;
            kbScaleTo = zoomIn ? 1.12f : 1.04f;
            panFrom = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-20f, 20f));
            panTo = -panFrom;
            kbT = 0;
            var prev = currentBg;
            currentBg = next;
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0, 1, t / 0.6f);
                next.color = new Color(1, 1, 1, k);
                if (prev != null) prev.color = new Color(1, 1, 1, 1 - k);
                yield return null;
            }
            next.color = Color.white;
            if (prev != null) prev.color = new Color(1, 1, 1, 0);
        }

        IEnumerator Say(StoryLine ln)
        {
            bool narration = string.IsNullOrEmpty(ln.who);
            StoryData.Speakers.TryGetValue(ln.who ?? "", out var sp);
            nameplate.gameObject.SetActive(!narration);
            nameText.text = sp != null ? sp.name : ln.who;
            nameText.color = sp != null ? sp.color : UIKit.Gold;
            var face = sp != null && !string.IsNullOrEmpty(sp.face) ? GameAssets.Sprite(StoryData.Root + "face_" + sp.face) : null;
            portraitRing.gameObject.SetActive(face != null);
            portrait.sprite = face;
            bool inner = ln.text.StartsWith("(");
            lineText.rectTransform.offsetMin = new Vector2(face != null ? 242 : 46, 26);
            lineText.fontStyle = narration || inner ? FontStyle.Italic : FontStyle.Normal;
            lineText.color = narration ? new Color(0.86f, 0.84f, 0.92f) : inner ? new Color(0.8f, 0.86f, 0.95f) : UIKit.Ink;
            if (boxGroup.alpha < 1) StartCoroutine(Fade(boxGroup, 1, 0.2f));

            if (ln.fx == "shake") shake = 1f;
            if (ln.fx == "flash") StartCoroutine(Flash());

            // 타자기
            advance = false;
            typing = true;
            string full = ln.text;
            float cps = 42f, shown = 0;
            while (shown < full.Length && !skipAll)
            {
                if (advance || AutoAdvance) { advance = false; break; }
                shown += Time.unscaledDeltaTime * cps;
                lineText.text = full.Substring(0, Mathf.Min(full.Length, (int)shown));
                yield return null;
            }
            lineText.text = full;
            typing = false;
            // 다음 입력 대기
            float wait = 0;
            while (!skipAll)
            {
                wait += Time.unscaledDeltaTime;
                nextMark.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.5f + 0.5f * Mathf.Sin(wait * 6f));
                if (advance) break;
                if (AutoAdvance && wait > 0.35f) break;
                yield return null;
            }
            advance = false;
        }

        IEnumerator TitleCard(string text)
        {
            var parts = text.Split('\n');
            titleMain.text = parts[0];
            titleSub.text = parts.Length > 1 ? parts[1] : "";
            boxGroup.alpha = 0;
            yield return Fade(titleGroup, 1, 0.5f);
            float t = 0;
            advance = false;
            while (t < (AutoAdvance ? 0.6f : 2.6f) && !advance && !skipAll) { t += Time.unscaledDeltaTime; yield return null; }
            advance = false;
            yield return Fade(titleGroup, 0, 0.5f);
        }

        IEnumerator Flash()
        {
            float t = 0;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                flash.color = new Color(1, 1, 1, Mathf.Lerp(0.85f, 0, t / 0.45f));
                yield return null;
            }
            flash.color = new Color(1, 1, 1, 0);
        }

        static IEnumerator Fade(CanvasGroup g, float to, float dur)
        {
            float from = g.alpha, t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            g.alpha = to;
        }

        void Update()
        {
            if (!Playing) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) advance = true;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) skipAll = true;

            // 켄 번즈 + 흔들림
            kbT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(kbT / kbDur);
            float s = Mathf.Lerp(kbScaleFrom, kbScaleTo, k);
            shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2.2f);
            var jitter = shake > 0 ? UnityEngine.Random.insideUnitCircle * 22f * shake : Vector2.zero;
            if (currentBg != null)
            {
                currentBg.rectTransform.localScale = Vector3.one * s;
                currentBg.rectTransform.anchoredPosition = Vector2.Lerp(panFrom, panTo, k) + jitter;
            }
        }
    }
}
