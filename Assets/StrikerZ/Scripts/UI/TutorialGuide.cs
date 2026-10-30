using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 강제 튜토리얼 오버레이 (메뉴 · 대전 공용): 화면 전체를 막고 "지금 눌러야 하는 것" 하나만 구멍을 내어 깜빡이는 금색 테두리로 보여 준다.
    /// 구멍 밖은 클릭되지 않는다 (UI · 3D 카드 모두 EventSystem 을 타므로 차단 판 4장이 막는다. 마우스 직접 입력인 배치 드래그는 AllowsCard/AllowsSlot 으로 막는다).
    /// 설명 상자에는 제목 · 본문 · (대상이 없을 때) [다음 ▶] · 언제나 [나가기]. Esc 를 누르면 나가기 확인창.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        /// <summary>지금 화면을 막고 있는 가이드 (입력 게이트용). 없으면 null</summary>
        public static TutorialGuide Active { get; private set; }

        RectTransform root, overlay, hi, box, confirm;
        readonly Image[] blockers = new Image[4];
        Image hiImg, hiGlow;
        Text title, text, stepText;
        UIKit.Btn nextBtn, exitBtn;
        Action onNext, onExit;
        RectTransform target;
        Func<Rect?> screenRect;
        bool blocking;
        Rect hole;
        bool hasHole;
        float shownAt;
        bool zoomPrev;

        /// <summary>배치 드래그 게이트: 집을 수 있는 카드 · 놓을 수 있는 칸 (null = 모두 허용)</summary>
        public Func<CardInstance, bool> allowCard;
        public Func<int, bool> allowSlot;

        public bool Blocking => blocking && overlay != null && overlay.gameObject.activeSelf;
        public bool Visible => overlay != null && overlay.gameObject.activeSelf;

        public bool AllowsCard(CardInstance c) => !Blocking || allowCard == null || allowCard(c);
        public bool AllowsSlot(int s) => !Blocking || allowSlot == null || allowSlot(s);

        public static TutorialGuide Create(RectTransform canvasRoot, Action onExit)
        {
            var go = new GameObject("TutorialGuide", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var g = go.AddComponent<TutorialGuide>();
            g.root = canvasRoot;
            g.onExit = onExit;
            g.Build();
            return g;
        }

        void Build()
        {
            overlay = (RectTransform)transform;
            overlay.Fill();
            // 차단 판 4장 (구멍 바깥을 어둡게 + 클릭 차단)
            for (int i = 0; i < 4; i++)
            {
                var b = UIKit.Img(overlay, "Block" + i, new Color(0, 0, 0, 0.45f));
                b.raycastTarget = true;
                b.rectTransform.anchorMin = b.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                blockers[i] = b;
            }
            // 깜빡이는 테두리
            hiGlow = UIKit.Img(overlay, "HiGlow", new Color(1f, 0.82f, 0.35f, 0.12f), GameAssets.Rounded, true);
            hiGlow.raycastTarget = false;
            hiGlow.rectTransform.anchorMin = hiGlow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hiImg = UIKit.Img(overlay, "Hi", new Color(1f, 0.82f, 0.35f, 1f), GameAssets.RoundedOutline, true);
            hiImg.raycastTarget = false;
            hi = hiImg.rectTransform;
            hi.anchorMin = hi.anchorMax = new Vector2(0.5f, 0.5f);

            // 설명 상자
            var bg = UIKit.Frame(overlay, "Box", new Color(0.04f, 0.03f, 0.08f, 0.97f));
            box = bg.rectTransform;
            box.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(760, 210), new Vector2(0.5f, 0));
            bg.raycastTarget = true;
            bg.transform.Find("Border").GetComponent<Image>().color = new Color(1f, 0.82f, 0.35f, 0.6f);
            var tag = UIKit.Img(bg.transform, "Tag", UIKit.Gold, GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(18, -16), new Vector2(92, 30), new Vector2(0, 1));
            UIKit.Txt(tag.transform, "T", "튜토리얼", 16, new Color(0.1f, 0.06f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
            title = UIKit.Txt(bg.transform, "Title", "", 25, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 122, -50, 120, 12);
            stepText = UIKit.Txt(bg.transform, "Step", "", 16, UIKit.Muted, TextAnchor.MiddleRight);
            stepText.rectTransform.Anchor(0.5f, 1, 1, 1, 0, -50, 18, 12);
            text = UIKit.Txt(bg.transform, "Text", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            text.rectTransform.Anchor(0, 0, 1, 1, 20, 62, 20, 56);
            text.lineSpacing = 1.15f;
            exitBtn = UIKit.Button(bg.transform, "나가기 (Esc)", AskExit, null, 17, UIKit.Muted);
            exitBtn.rt.Place(new Vector2(0, 0), new Vector2(18, 12), new Vector2(150, 40), new Vector2(0, 0));
            nextBtn = UIKit.Button(bg.transform, "다음 ▶", () => { var n = onNext; onNext = null; n?.Invoke(); }, new Color(0.45f, 0.08f, 0.05f, 0.97f), 20, Color.white);
            nextBtn.rt.Place(new Vector2(1, 0), new Vector2(-18, 12), new Vector2(200, 44), new Vector2(1, 0));

            // 나가기 확인
            var dim = UIKit.Img(overlay, "Confirm", new Color(0, 0, 0, 0.6f));
            confirm = dim.rectTransform;
            confirm.Fill();
            dim.raycastTarget = true;
            var panel = UIKit.Frame(confirm, "Panel", new Color(0.06f, 0.03f, 0.05f, 0.97f));
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 230));
            panel.raycastTarget = true;
            var q = UIKit.Txt(panel.transform, "Q", "튜토리얼을 나갈까요?\n<size=18><color=#d9c9c9>진행 중인 내용은 저장되지 않습니다.</color></size>", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            q.rectTransform.Anchor(0, 0.42f, 1, 1, 20, 0, 20, 20);
            UIKit.Button(panel.transform, "나가기", () => { confirm.gameObject.SetActive(false); Hide(); onExit?.Invoke(); }, new Color(0.45f, 0.07f, 0.05f, 0.97f), 24, Color.white)
                .rt.Place(new Vector2(0.5f, 0), new Vector2(-120, 22), new Vector2(210, 54), new Vector2(0.5f, 0));
            UIKit.Button(panel.transform, "계속하기", () => confirm.gameObject.SetActive(false), null, 24)
                .rt.Place(new Vector2(0.5f, 0), new Vector2(120, 22), new Vector2(210, 54), new Vector2(0.5f, 0));
            confirm.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
        }

        /// <summary>
        /// 한 단계 보여 주기. target = 눌러야 하는 UI (구멍), screenRect = 3D 대상의 화면 사각형(픽셀) 제공자,
        /// 둘 다 없으면 전체를 막고 [다음 ▶] 로 넘어간다 (onNext). block = false 면 막지 않고 상자만 (자유 조작 단계).
        /// </summary>
        public void Show(string title, string body, RectTransform target = null, Func<Rect?> screenRect = null, Action onNext = null, string nextLabel = "다음 ▶", bool block = true, string step = "")
        {
            Active = this;
            this.target = target;
            this.screenRect = screenRect;
            this.onNext = onNext;
            blocking = block;
            allowCard = null;
            allowSlot = null;
            this.title.text = title;
            text.text = body;
            stepText.text = step;
            nextBtn.label.text = nextLabel;
            nextBtn.bg.gameObject.SetActive(onNext != null);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            Layout();
        }

        public void Hide()
        {
            if (Active == this) Active = null;
            blocking = false;
            target = null;
            screenRect = null;
            onNext = null;
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        void OnDestroy() { if (Active == this) Active = null; }

        /// <summary>테스트: [다음 ▶] 누르기</summary>
        public void TestNext() { var n = onNext; onNext = null; n?.Invoke(); }
        public string TestTitle => title != null ? title.text : "";
        public bool TestHasHole => hasHole;
        public bool TestHasNext => onNext != null;

        public void AskExit()
        {
            if (!Visible) return;
            confirm.gameObject.SetActive(true);
            confirm.SetAsLastSibling();
        }

        void Update()
        {
            if (!Visible) return;
            // 언제나 맨 위 (운명 재배치 창 등이 SetAsLastSibling 을 하므로)
            if (overlay.GetSiblingIndex() != root.childCount - 1) overlay.SetAsLastSibling();
            var kb = Keyboard.current;
            bool zoom = CardZoom.IsOpen;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && Time.unscaledTime - shownAt > 0.2f && !zoom && !zoomPrev)   // 확대 닫는 Esc 는 나가기로 치지 않는다
            {
                if (confirm.gameObject.activeSelf) confirm.gameObject.SetActive(false);
                else AskExit();
            }
            zoomPrev = zoom;
            Layout();
            // 깜빡임
            float a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 7f);
            hiImg.color = new Color(1f, 0.82f, 0.35f, hasHole ? a : 0);
            hiGlow.color = new Color(1f, 0.82f, 0.35f, hasHole ? 0.06f + 0.1f * a : 0);
        }

        Vector2 ToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var lp);
            return lp;
        }

        void Layout()
        {
            Rect? r = null;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                Vector2 min = ToLocal(RectTransformUtility.WorldToScreenPoint(null, corners[0]));
                Vector2 max = ToLocal(RectTransformUtility.WorldToScreenPoint(null, corners[2]));
                r = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
            }
            else if (screenRect != null)
            {
                var sr = screenRect();
                if (sr != null)
                {
                    Vector2 min = ToLocal(sr.Value.min), max = ToLocal(sr.Value.max);
                    r = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
                }
            }
            hasHole = r != null;
            var size = root.rect.size;
            float W = size.x, H = size.y;
            if (!blocking)
            {
                foreach (var b in blockers) b.gameObject.SetActive(false);
            }
            else if (!hasHole)
            {
                for (int i = 0; i < 4; i++) blockers[i].gameObject.SetActive(i == 0);
                SetRect(blockers[0].rectTransform, -W / 2, -H / 2, W / 2, H / 2);
            }
            else
            {
                hole = r.Value;
                foreach (var b in blockers) b.gameObject.SetActive(true);
                SetRect(blockers[0].rectTransform, -W / 2, hole.yMax, W / 2, H / 2);        // 위
                SetRect(blockers[1].rectTransform, -W / 2, -H / 2, W / 2, hole.yMin);       // 아래
                SetRect(blockers[2].rectTransform, -W / 2, hole.yMin, hole.xMin, hole.yMax); // 왼쪽
                SetRect(blockers[3].rectTransform, hole.xMax, hole.yMin, W / 2, hole.yMax);  // 오른쪽
            }
            hi.gameObject.SetActive(hasHole);
            hiGlow.gameObject.SetActive(hasHole);
            if (hasHole)
            {
                var c = r.Value;
                float pad = 10f + 4f * Mathf.Sin(Time.unscaledTime * 7f);
                SetRect(hi, c.xMin - pad, c.yMin - pad, c.xMax + pad, c.yMax + pad);
                SetRect(hiGlow.rectTransform, c.xMin - pad, c.yMin - pad, c.xMax + pad, c.yMax + pad);
            }
            // 상자는 대상과 겹치지 않는 쪽 (대상이 아래쪽이면 위에)
            bool top = hasHole && r.Value.center.y < -H * 0.05f;
            if (hasHole && r.Value.yMin < -H / 2 + 270 && r.Value.yMax > -H / 2 + 20) top = true;
            if (hasHole && top && r.Value.yMax > H / 2 - 270) top = false;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, top ? 1 : 0);
            box.pivot = new Vector2(0.5f, top ? 1 : 0);
            box.anchoredPosition = new Vector2(0, top ? -40 : 40);
            box.SetAsLastSibling();
            if (confirm.gameObject.activeSelf) confirm.SetAsLastSibling();
        }

        static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0, x1 - x0), Mathf.Max(0, y1 - y0));
        }

        /// <summary>3D 카드의 화면 사각형 (렌더러 경계 상자의 8 꼭짓점 투영)</summary>
        public static Rect? CardScreenRect(CardInstance c, Camera cam)
        {
            if (c == null || c.view == null || cam == null) return null;
            var rend = c.view.GetComponentInChildren<Renderer>();
            if (rend == null) return null;
            var b = rend.bounds;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var s = cam.WorldToScreenPoint(p);
                if (s.z < 0) return null;
                minX = Mathf.Min(minX, s.x); maxX = Mathf.Max(maxX, s.x);
                minY = Mathf.Min(minY, s.y); maxY = Mathf.Max(maxY, s.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>보드 칸의 화면 사각형 (칸 중심 둘레)</summary>
        public static Rect? SlotScreenRect(Vector3 worldCenter, Camera cam, float halfW = 0.55f, float halfD = 0.8f)
        {
            if (cam == null) return null;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var p = worldCenter + new Vector3((i & 1) == 0 ? -halfW : halfW, 0, (i & 2) == 0 ? -halfD : halfD);
                var s = cam.WorldToScreenPoint(p);
                minX = Mathf.Min(minX, s.x); maxX = Mathf.Max(maxX, s.x);
                minY = Mathf.Min(minY, s.y); maxY = Mathf.Max(maxY, s.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
