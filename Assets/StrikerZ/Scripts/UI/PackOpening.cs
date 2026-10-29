using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 부스터팩 개봉 연출.
    /// 팩 윗부분을 옆으로 밀어 찢기 → 카드 뭉치 꺼내기 → 한 장씩 뽑아 뒤집기 → 결과 그리드.
    /// 등급(Common·Uncommon·Rare·Unique)이 높을수록 예고 빛·떨림·섬광·광선·파티클·배너·효과음이 단계적으로 강해진다.
    /// 팩 안에서 가장 높은 등급이 찢기 전 팩의 빛깔/떨림으로 미리 암시된다.
    /// </summary>
    public class PackOpening : MonoBehaviour
    {
        /// <summary>자동 테스트: 입력 없이 진행</summary>
        public bool auto;
        public bool IsPlaying { get; private set; }
        public string Stage { get; private set; } = "";

        const float PackH = 640f, PackW = PackH * 419f / 771f;
        const float TearY = 0.905f;                     // 팩 텍스처 상단 크림프 바로 아래
        const float CardW = 300f, CardH = 420f;
        static readonly Vector2 CenterPos = new Vector2(0, 70);
        static readonly Vector2 StackPos = new Vector2(-640, 70);
        const float StackScale = 0.8f, RowScale = 0.5f, GridScale = 236f / 300f;
        static readonly Color Clear = new Color(1, 1, 1, 0);

        RectTransform root, stage, underLayer, cardsLayer, fxLayer, packRt, stripRt, shineBar, bannerRt;
        CanvasGroup packGroup;
        RawImage packBody, packStrip;
        Image bgGlow, bgRays, dim, flash, beam, packAura, tearTip;
        Text title, hint, counter, banner, bannerSub;
        UIKit.Btn nextBtn, skipAllBtn, closeBtn, skipPackBtn;
        Canvas canvas;

        bool tapped, nextPressed, skipAll, skipPack;
        float tearProgress, shake, bgRaysSpeed = 12f;
        int ripStep;
        bool embers;

        class CardView
        {
            public RectTransform rt, fx, faceRoot;
            public Image back, aura, rays, holo;
            public CardData card;
            public int tier;
            public CardFinish finish;
            /// <summary>연출 강도: 등급과 가공 중 높은 쪽 (Seven ≥ Rare급, StrikerZ = Unique급)</summary>
            public int hype;
            public bool revealed, holoOn;
            public float phase;
        }

        readonly List<CardView> views = new List<CardView>();
        readonly HashSet<Graphic> rainbow = new HashSet<Graphic>();

        // ══════════════════════════ 구성 ══════════════════════════

        public void Build(RectTransform parent)
        {
            canvas = parent.GetComponentInParent<Canvas>();
            var bg = UIKit.Img(parent, "Opening", new Color(0.012f, 0.006f, 0.03f, 1f));
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Catcher>().onClick = () => tapped = true;
            root = bg.rectTransform;
            root.Fill();

            var c = new Vector2(0.5f, 0.5f);
            bgGlow = UIKit.Img(root, "BgGlow", Clear, GameAssets.Glow);
            bgGlow.rectTransform.Place(c, new Vector2(0, 40), new Vector2(2000, 1500));
            bgRays = UIKit.Img(root, "BgRays", Clear, RaysSprite);
            bgRays.rectTransform.Place(c, CenterPos, new Vector2(2600, 2600));
            dim = UIKit.Img(root, "Dim", new Color(0, 0, 0, 0));
            dim.rectTransform.Fill();

            stage = UIKit.Rect("Stage", root).Fill();
            underLayer = UIKit.Rect("Under", stage).Fill();
            cardsLayer = UIKit.Rect("Cards", stage).Fill();

            // ── 팩 ──
            packRt = UIKit.Rect("Pack", stage).Place(c, new Vector2(0, -30), new Vector2(PackW, PackH));
            packGroup = packRt.gameObject.AddComponent<CanvasGroup>();
            var pc = packRt.gameObject.AddComponent<Catcher>();
            pc.onClick = () => tapped = true;
            pc.onDrag = OnPackDrag;
            packAura = UIKit.Img(packRt, "Aura", Clear, GameAssets.Glow);
            packAura.rectTransform.Anchor(-0.75f, -0.4f, 1.75f, 1.4f);
            beam = UIKit.Img(packRt, "Beam", Clear, GameAssets.Glow);
            beam.rectTransform.Place(new Vector2(0.5f, TearY), Vector2.zero, new Vector2(PackW, 40), new Vector2(0.5f, 0.3f));
            packBody = RawImg(packRt, "Body");
            packBody.rectTransform.Anchor(0, 0, 1, TearY);
            packBody.uvRect = new Rect(0, 0, 1, TearY);
            packStrip = RawImg(packRt, "Strip");
            stripRt = packStrip.rectTransform;
            packStrip.uvRect = new Rect(0, TearY, 1, 1 - TearY);

            var shineMask = UIKit.Rect("ShineMask", packRt).Fill();
            shineMask.gameObject.AddComponent<RectMask2D>();
            // 포일 광택: 딱딱한 흰 띠 대신 부드러운 빛 번짐
            shineBar = UIKit.Img(shineMask, "Shine", new Color(1, 1, 1, 0.13f), GameAssets.Glow).rectTransform;
            shineBar.Place(c, Vector2.zero, new Vector2(190, 1400));
            shineBar.localRotation = Quaternion.Euler(0, 0, -28);

            tearTip = UIKit.Img(packRt, "TearTip", Clear, GameAssets.Glow);
            tearTip.rectTransform.Place(new Vector2(0, TearY), Vector2.zero, new Vector2(90, 90));

            fxLayer = UIKit.Rect("Fx", stage).Fill();

            bannerRt = UIKit.Rect("Banner", stage).Place(c, new Vector2(0, 385), new Vector2(1400, 170));
            banner = UIKit.Txt(bannerRt, "Main", "", 104, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            banner.rectTransform.Anchor(0, 0.3f, 1, 1);
            banner.horizontalOverflow = HorizontalWrapMode.Overflow;
            banner.verticalOverflow = VerticalWrapMode.Overflow;
            banner.Outline(new Color(0.05f, 0.02f, 0.08f, 0.95f), 4f);
            banner.Shadow(new Color(0, 0, 0, 0.8f), 6f);
            bannerSub = UIKit.Txt(bannerRt, "Sub", "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerSub.rectTransform.Anchor(0, 0, 1, 0.3f);
            bannerSub.verticalOverflow = VerticalWrapMode.Overflow;
            bannerSub.Outline(new Color(0, 0, 0, 0.9f), 2f);
            bannerRt.gameObject.SetActive(false);

            flash = UIKit.Img(root, "Flash", Clear);
            flash.rectTransform.Fill();

            title = UIKit.Txt(root, "Title", "", 40, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(1200, 60));
            title.Shadow();
            counter = UIKit.Txt(root, "Counter", "", 26, UIKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            counter.rectTransform.Place(new Vector2(0, 1), new Vector2(40, -52), new Vector2(400, 50), new Vector2(0, 0.5f));
            hint = UIKit.Txt(root, "Hint", "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            hint.rectTransform.Place(c, new Vector2(0, -222), new Vector2(1200, 50));
            hint.Shadow(new Color(0, 0, 0, 0.9f), 2f);

            skipPackBtn = UIKit.Button(root, "SKIP ▶▶", () => skipPack = true, UIKit.Panel, 24);
            skipPackBtn.rt.Place(new Vector2(1, 1), new Vector2(-36, -24), new Vector2(190, 60), new Vector2(1, 1));
            nextBtn = UIKit.Button(root, "다음 팩", () => nextPressed = true, new Color(0.55f, 0.09f, 0.06f, 0.97f), 28, Color.white);
            nextBtn.rt.Place(new Vector2(0.5f, 0), new Vector2(-160, 40), new Vector2(280, 72));
            skipAllBtn = UIKit.Button(root, "남은 팩 모두 공개", () => { skipAll = true; nextPressed = true; }, UIKit.Dark, 24);
            skipAllBtn.rt.Place(new Vector2(0.5f, 0), new Vector2(160, 40), new Vector2(280, 72));
            closeBtn = UIKit.Button(root, "확인", () => nextPressed = true, new Color(0.55f, 0.09f, 0.06f, 0.97f), 30, Color.white);
            closeBtn.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(300, 76));

            root.gameObject.SetActive(false);
        }

        static RawImage RawImg(Transform parent, string name)
        {
            var rt = UIKit.Rect(name, parent);
            return rt.gameObject.AddComponent<RawImage>();
        }

        /// <summary>자동 테스트/외부에서 진행 입력</summary>
        public void Continue() { tapped = true; nextPressed = true; }

        // ══════════════════════════ 흐름 ══════════════════════════

        public IEnumerator Play(BoosterPack pack, List<List<(CardData card, bool isNew, CardFinish finish)>> packs)
        {
            IsPlaying = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            skipAll = false;
            Sfx.PlayBgm("bgm_pack", 0.8f);
            var tex = GameAssets.Tex(pack.art) ?? GameAssets.Tex("card_back");
            packBody.texture = packStrip.texture = tex;

            for (int p = 0; p < packs.Count; p++)
            {
                skipPack = false;
                title.text = packs.Count > 1 ? $"{pack.code}  {p + 1} / {packs.Count}" : pack.code;
                ShowButtons(false, false, false);
                skipPackBtn.bg.gameObject.SetActive(false);
                ResetFx();

                Sfx.PlayBgm("bgm_pack", 0.8f);
                // 연출 강도 낮은 순 → 마지막에 레어/가공 (실제 팩처럼 뒤쪽에 레어)
                var list = packs[p].OrderBy(x => Hype(x.card.rarity, x.finish)).ThenBy(x => x.card.rarity).ThenBy(x => x.finish).ToList();
                int best = list.Max(x => Hype(x.card.rarity, x.finish));
                var topFinish = list.Max(x => x.finish);
                // 첫 카드가 뭉치 맨 위(마지막 형제)가 되도록 역순 생성
                for (int i = list.Count - 1; i >= 0; i--) views.Insert(0, MakeCard(list[i].card, list[i].isNew, list[i].finish));

                cardsLayer.gameObject.SetActive(skipAll);
                underLayer.gameObject.SetActive(skipAll);
                if (!skipAll)
                {
                    yield return TearPack(best, topFinish);
                    cardsLayer.gameObject.SetActive(true);
                    underLayer.gameObject.SetActive(true);
                    yield return Emerge();
                    skipPackBtn.bg.gameObject.SetActive(true);
                    yield return RevealAll();
                    skipPackBtn.bg.gameObject.SetActive(false);
                }
                else
                {
                    packRt.gameObject.SetActive(false);
                    foreach (var v in views) QuickReveal(v);
                }
                yield return Summary(skipAll);

                bool last = p == packs.Count - 1;
                nextPressed = false;
                ShowButtons(!last, !last, last);
                while (!nextPressed) yield return null;
                // 남은 팩 모두 공개: 마스터 듀얼처럼 남은 팩에서 나온 카드를 한 화면에 높은 등급부터 위쪽에 쭉 보여 준다
                if (skipAll && !last)
                {
                    yield return ShowAll(pack, packs.Skip(p + 1).SelectMany(x => x).ToList(), packs.Count - p - 1);
                    break;
                }
            }
            ResetFx();
            root.gameObject.SetActive(false);
            Sfx.PlayBgm(Sfx.MenuBgm, 1.2f);
            IsPlaying = false;
            Stage = "";
        }

        /// <summary>
        /// 보상 카드 공개 (출석 7일째 · 세븐 패스 카드 보상 · 업그레이드 결과 등 단일 카드 획득 공용, 2026-10-29):
        /// 팩 찢기 없이 카드가 뒤집힌 채 가운데에 떠오르고, 터치하면 뒤집혀 등급·가공대로 빛난다 (팩 개봉과 같은 Charge/Flip/Impact).
        /// 여러 장이면 뭉치로 놓고 한 장씩.
        /// </summary>
        public IEnumerator PlayCards(string titleText, List<(CardData card, bool isNew, CardFinish finish)> cards)
        {
            if (cards == null || cards.Count == 0) yield break;
            IsPlaying = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            skipAll = false; skipPack = false;
            title.text = titleText;
            ShowButtons(false, false, false);
            skipPackBtn.bg.gameObject.SetActive(false);
            ResetFx();
            packRt.gameObject.SetActive(false);
            Sfx.PlayBgm("bgm_pack", 0.8f);

            var list = cards.OrderBy(x => Hype(x.card.rarity, x.finish)).ThenBy(x => x.card.rarity).ThenBy(x => x.finish).ToList();
            for (int i = list.Count - 1; i >= 0; i--) views.Insert(0, MakeCard(list[i].card, list[i].isNew, list[i].finish));
            cardsLayer.gameObject.SetActive(true);
            underLayer.gameObject.SetActive(true);
            bool single = views.Count == 1;

            // 뒤집힌 채 아래에서 떠오른다 (한 장이면 가운데, 여러 장이면 왼쪽 뭉치)
            Stage = "emerge";
            Sfx.Play("whoosh");
            yield return Tween(0.5f, k =>
            {
                float e = OutCubic(k);
                for (int i = 0; i < views.Count; i++)
                {
                    var to = single ? CenterPos : StackSlot(i);
                    views[i].rt.anchoredPosition = Vector2.Lerp(to + new Vector2(0, -760), to, e);
                    views[i].rt.localScale = Vector3.one * StackScale;
                    views[i].rt.localRotation = Quaternion.Euler(0, 0, single ? 0 : StackRot(i));
                }
            });

            // 터치해야 뒤집힌다
            hint.text = single ? "터치하여 카드 공개" : "터치하여 한 장씩 공개";
            tapped = false;
            float w = 0;
            while (!tapped && !(auto && w > 0.6f))
            {
                w += Time.unscaledDeltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(w * 6f);
                SetA(hint, 0.6f + 0.4f * pulse);
                if (single) views[0].rt.anchoredPosition = CenterPos + new Vector2(0, Mathf.Sin(w * 2f) * 6f);
                yield return null;
            }
            tapped = false;
            hint.text = "";
            SetA(hint, 1);

            yield return RevealAll();
            yield return Summary(false);
            nextPressed = false;
            ShowButtons(false, false, true);
            while (!nextPressed) yield return null;

            ResetFx();
            root.gameObject.SetActive(false);
            Sfx.PlayBgm(Sfx.MenuBgm, 1.2f);
            IsPlaying = false;
            Stage = "";
        }

        void ShowButtons(bool next, bool skip, bool close)
        {
            nextBtn.bg.gameObject.SetActive(next);
            skipAllBtn.bg.gameObject.SetActive(skip);
            closeBtn.bg.gameObject.SetActive(close);
        }

        void ResetFx()
        {
            foreach (var v in views) { Destroy(v.rt.gameObject); Destroy(v.fx.gameObject); }
            views.Clear();
            rainbow.Clear();
            foreach (var pt in parts) { pt.img.gameObject.SetActive(false); pool.Push(pt.img); }
            parts.Clear();
            embers = false;
            shake = 0;
            SetA(dim, 0); SetA(flash, 0); SetA(bgRays, 0); SetA(bgGlow, 0);
            bannerRt.gameObject.SetActive(false);
            title.enabled = true;
            hint.text = counter.text = "";
        }

        // ── 1. 팩 찢기 ──

        IEnumerator TearPack(int best, CardFinish topFinish)
        {
            Stage = "pack";
            var hc = topFinish != CardFinish.Normal && best <= Hype(Rarity.Unique, topFinish) && (topFinish == CardFinish.StrikerZ || best <= 3)
                ? FinishStyle.Base(topFinish) : TierColor(Mathf.Max(best, 1));
            packRt.gameObject.SetActive(true);
            packRt.localRotation = Quaternion.identity;
            packGroup.alpha = 1;
            stripRt.Anchor(0, TearY, 1, 1);
            stripRt.pivot = new Vector2(1, 0);
            stripRt.localRotation = Quaternion.identity;
            packStrip.color = Color.white;
            shineBar.gameObject.SetActive(true);
            SetA(tearTip, 0);
            tearTip.color = new Color(hc.r, hc.g, hc.b, 0);
            beam.color = new Color(hc.r, hc.g, hc.b, 0);
            beam.rectTransform.sizeDelta = new Vector2(PackW, 40);
            packAura.color = new Color(hc.r, hc.g, hc.b, 0);
            if (best == 4 && topFinish == CardFinish.Normal) { rainbow.Add(packAura); rainbow.Add(beam); rainbow.Add(tearTip); }
            float auraA = new[] { 0f, 0.12f, 0.3f, 0.5f, 0.65f, 0.85f }[best];
            SetA(bgGlow, 0);
            bgGlow.color = new Color(hc.r, hc.g, hc.b, 0);

            tearProgress = 0;
            ripStep = 0;
            tapped = false;
            Sfx.Play("whoosh");
            yield return Tween(0.5f, k => packRt.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(-1100, -30, OutBack(k))));

            hint.text = "팩 윗부분을 옆으로 밀어서 개봉하세요";
            float t = 0, prev = 0;
            bool autoTear = false;
            while (tearProgress < 1f)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                if (tapped || (auto && t > 1.1f)) { autoTear = true; tapped = false; }
                if (autoTear) tearProgress = Mathf.Min(1f, tearProgress + dt / 0.5f);

                // 대기 모션: 둥실 + 등급 암시 떨림
                float rum = best >= 5 ? 3.5f : best >= 4 ? 2f : best >= 3 && Mathf.Repeat(t, 2.1f) < 0.22f ? 2.5f : 0f;
                packRt.anchoredPosition = new Vector2(0, -30 + Mathf.Sin(t * 2f) * 6f) + UnityEngine.Random.insideUnitCircle * rum;
                packRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * 1.4f);
                shineBar.anchoredPosition = new Vector2(Mathf.Lerp(-PackW * 1.2f, PackW * 1.2f, Mathf.Repeat(t / 2.4f, 1f)), 0);
                SetA(packAura, auraA * (0.75f + 0.25f * Mathf.Sin(t * 4f)) * (1f + 0.4f * tearProgress));
                SetA(bgGlow, auraA * 0.35f);
                SetA(hint, 0.6f + 0.4f * Mathf.Sin(t * 5f));
                if (best >= 5 && UnityEngine.Random.value < dt * 18f) Ember(packRt.anchoredPosition + new Vector2(UnityEngine.Random.Range(-PackW, PackW) * 0.7f, -PackH * 0.4f));

                // 찢김 표시: 선 없이 윗띠가 들리고 찢기는 끝에서 불꽃만
                tearTip.rectTransform.anchoredPosition = new Vector2(tearProgress * PackW, 0);
                SetA(tearTip, tearProgress > 0 ? 1f : 0f);
                stripRt.localRotation = Quaternion.Euler(0, 0, tearProgress * 10f);
                if (tearProgress > prev)
                {
                    var tip = TearWorld(tearProgress);
                    if (UnityEngine.Random.value < 0.7f) Burst(tip, 1, hc, 80, 260, 14, 26, 0.5f, best == 4, 300);
                    shake = Mathf.Max(shake, 1.5f);
                }
                while (tearProgress * 9f > ripStep + 1) { ripStep++; Sfx.Play("rip", 0.8f); }
                prev = tearProgress;
                yield return null;
            }
            hint.text = "";
            shineBar.gameObject.SetActive(false);
            SetA(tearTip, 0);

            // 찢김 완료: 띠 날아감 + 빛기둥
            Sfx.Play("tear");
            shake = 6 + best * 3;
            Burst(TearWorld(0.5f), 14 + best * 8, hc, 200, 520 + best * 60, 16, 34, 0.8f, best == 4, 250);
            StartCoroutine(Flash(Color.Lerp(Color.white, hc, 0.4f), 0.15f + best * 0.06f, 0.35f));
            var s0 = stripRt.anchoredPosition;
            StartCoroutine(Tween(0.6f, k =>
            {
                float e = OutCubic(k);
                stripRt.anchoredPosition = s0 + new Vector2(260, 300) * e;
                stripRt.localRotation = Quaternion.Euler(0, 0, 7 + 48 * e);
                packStrip.color = new Color(1, 1, 1, 1 - k * k);
            }));
            yield return Tween(0.4f, k =>
            {
                float e = OutCubic(k);
                beam.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(PackW * 0.8f, PackW * 1.35f, e), Mathf.Lerp(40, 720 + best * 120, e));
                SetA(beam, 0.9f * e);
                SetA(packAura, Mathf.Lerp(auraA, Mathf.Max(0.35f, auraA), e));
            });
            yield return Wait(0.2f);
        }

        void OnPackDrag(PointerEventData e)
        {
            if (Stage != "pack") return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            tearProgress = Mathf.Min(1f, tearProgress + Mathf.Abs(e.delta.x) / scale / (PackW * 0.85f));
        }

        Vector2 TearWorld(float prog) => packRt.anchoredPosition + new Vector2(-PackW / 2 + prog * PackW, (TearY - 0.5f) * PackH);

        // ── 2. 카드 뭉치 꺼내기 ──

        IEnumerator Emerge()
        {
            Stage = "emerge";
            Vector2 inPack = packRt.anchoredPosition + new Vector2(0, -60);
            for (int i = 0; i < views.Count; i++)
            {
                views[i].rt.anchoredPosition = inPack;
                views[i].rt.localScale = Vector3.one * 0.74f;
            }
            Sfx.Play("whoosh");
            yield return Tween(0.6f, k =>
            {
                for (int i = 0; i < views.Count; i++)
                {
                    float kk = Mathf.Clamp01(k * 1.25f - (views.Count - 1 - i) * 0.025f);
                    views[i].rt.anchoredPosition = inPack + new Vector2(0, 420 * OutCubic(kk));
                    views[i].rt.localRotation = Quaternion.Euler(0, 0, (i - 3.5f) * 1.6f * kk);
                }
                SetA(beam, 0.9f * (1 - k * 0.6f));
            });
            yield return Wait(0.08f);
            Sfx.Play("drop", 0.6f);
            var from = views.Select(v => v.rt.anchoredPosition).ToList();
            Vector2 p0 = packRt.anchoredPosition;
            yield return Tween(0.55f, k =>
            {
                float e = OutCubic(k);
                packRt.anchoredPosition = p0 + new Vector2(0, -1100 * k * k);
                packRt.localRotation = Quaternion.Euler(0, 0, -14 * k);
                packGroup.alpha = 1 - k;
                SetA(bgGlow, Mathf.Lerp(bgGlow.color.a, 0, k));
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].rt.anchoredPosition = Vector2.Lerp(from[i], StackSlot(i), e);
                    views[i].rt.localScale = Vector3.one * Mathf.Lerp(0.74f, StackScale, e);
                    views[i].rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp((i - 3.5f) * 1.6f, StackRot(i), e));
                }
            });
            packRt.gameObject.SetActive(false);
        }

        static Vector2 StackSlot(int i) => StackPos + new Vector2(i * 3f, -i * 3f);
        static float StackRot(int i) => (i % 2 == 0 ? 1 : -1) * i * 0.6f;
        static Vector2 RowSlot(int i) => new Vector2((i - 3.5f) * 168f, -398f);
        /// <summary>결과 그리드: 윗줄 4장, 아랫줄 나머지(가운데 정렬)</summary>
        Vector2 GridSlot(int i)
        {
            int n = Mathf.Max(1, views.Count), top = Mathf.Min(4, n);
            bool row1 = i < top;
            int cols = row1 ? top : n - top, col = row1 ? i : i - top;
            return new Vector2((col - (cols - 1) * 0.5f) * 262f, row1 ? 188f : -170f);
        }

        // ── 3. 한 장씩 공개 ──

        IEnumerator RevealAll()
        {
            Stage = "reveal";
            for (int i = 0; i < views.Count && !skipPack; i++)
            {
                var v = views[i];
                counter.text = $"남은 카드  {views.Count - i}";
                v.rt.SetAsLastSibling();
                Sfx.Play("whoosh", 0.7f);
                Vector2 from = v.rt.anchoredPosition;
                float r0 = v.rt.localEulerAngles.z;
                yield return Tween(0.24f, k =>
                {
                    float e = OutCubic(k);
                    v.rt.anchoredPosition = Vector2.Lerp(from, CenterPos, e);
                    v.rt.localScale = Vector3.one * Mathf.Lerp(StackScale, 1f, e);
                    v.rt.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(r0, 0, e));
                });
                if (v.hype >= 2) yield return Charge(v);
                if (skipPack) break;
                yield return Flip(v);
                Impact(v);

                // 결과 감상 — 레어 이상은 터치해야 넘어감
                hint.text = i == views.Count - 1 ? "터치하여 결과 보기" : "터치하여 다음 카드";
                if (v.hype >= 2) yield return WaitTap(1.1f + v.hype * 0.2f + (v.finish == CardFinish.StrikerZ ? 1.5f : 0f));
                else yield return WaitTap(0.5f, 0.85f);
                hint.text = "";
                EndImpact(v);
                counter.text = $"남은 카드  {views.Count - i - 1}";
                if (i < views.Count - 1) StartCoroutine(FlyTo(v, RowSlot(i), RowScale, 0.28f));
                else yield return FlyTo(v, RowSlot(i), RowScale, 0.28f);
            }
            if (skipPack)
            {
                EndImpactAll();
                foreach (var v in views) if (!v.revealed) QuickReveal(v);
            }
            counter.text = "";
        }

        IEnumerator Charge(CardView v)
        {
            int t = v.hype;
            float dur = new[] { 0f, 0f, 0.5f, 0.85f, 1.15f, 1.7f }[t] + (v.finish == CardFinish.StrikerZ ? 0.6f : 0f);
            var c = v.finish != CardFinish.Normal ? FinishStyle.Base(v.finish) : TierColor(t);
            v.aura.color = new Color(c.r, c.g, c.b, 0);
            if (t == 4 && v.finish == CardFinish.Normal) rainbow.Add(v.aura);
            if (t >= 3) Sfx.Play("charge" + t, 0.8f);
            if (t >= 5)
            {
                StartCoroutine(FadeA(dim, 0.82f, 0.5f));
                bgRays.color = new Color(c.r, c.g, c.b, 0);
                bgRaysSpeed = 30f;
            }
            float emit = 0;
            for (float x = 0; x < dur && !skipPack; x += Time.unscaledDeltaTime)
            {
                float k = x / dur;
                float amp = Mathf.Lerp(0.5f, 3f + t * 2.6f, k * k);
                v.rt.anchoredPosition = CenterPos + UnityEngine.Random.insideUnitCircle * amp;
                v.rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-1f, 1f) * amp * 0.25f);
                v.rt.localScale = Vector3.one * (1 + 0.04f * k);
                SetA(v.aura, Mathf.Lerp(0.1f, 0.95f, k) * (0.8f + 0.2f * Mathf.Sin(x * 30f)));
                v.back.color = Color.Lerp(Color.white, Color.Lerp(Color.white, c, 0.45f), k);
                emit += Time.unscaledDeltaTime * (10 + t * 10);
                while (emit > 1) { emit--; Converge(CenterPos, c, 380 + t * 40, 0.45f, t == 4); }
                if (t >= 5) { shake = Mathf.Max(shake, 7 * k); SetA(bgRays, 0.35f * k); }
                yield return null;
            }
            v.rt.anchoredPosition = CenterPos;
            v.rt.localRotation = Quaternion.identity;
            if (t >= 4 && !skipPack)
            {
                // 최고 등급은 스스로 뒤집게 해서 한 번 더 긴장감
                hint.text = "터치하여 공개!";
                tapped = false;
                float w = 0;
                while (!tapped && !skipPack && !(auto && w > 0.6f))
                {
                    w += Time.unscaledDeltaTime;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(w * 9f);
                    v.rt.localScale = Vector3.one * (1.04f + 0.03f * pulse);
                    SetA(v.aura, 0.75f + 0.25f * pulse);
                    SetA(hint, 0.6f + 0.4f * pulse);
                    v.rt.anchoredPosition = CenterPos + UnityEngine.Random.insideUnitCircle * (t >= 5 ? 3f : 1.5f);
                    emit += Time.unscaledDeltaTime * 20;
                    while (emit > 1) { emit--; Converge(CenterPos, c, 420, 0.5f, t == 4); }
                    yield return null;
                }
                tapped = false;
                hint.text = "";
                v.rt.anchoredPosition = CenterPos;
            }
        }

        IEnumerator Flip(CardView v)
        {
            float half = v.tier >= 3 ? 0.15f : 0.1f;
            float s = v.rt.localScale.y;
            Sfx.Play("flip");
            yield return Tween(half, k => v.rt.localScale = new Vector3(s * (1 - k), s * (1 + 0.08f * k), 1));
            ShowFace(v);
            float peak = v.tier >= 3 ? 1.12f : 1.04f;
            yield return Tween(half * 1.4f, k => v.rt.localScale = new Vector3(Mathf.LerpUnclamped(0, 1, OutBack(k)), Mathf.Lerp(1.08f, 1, k), 1) * Mathf.Lerp(peak, 1, k));
            v.rt.localScale = Vector3.one;
        }

        void ShowFace(CardView v)
        {
            v.back.gameObject.SetActive(false);
            v.faceRoot.gameObject.SetActive(true);
            v.revealed = true;
        }

        void Impact(CardView v)
        {
            int t = v.tier;
            var c = TierColor(t);
            bool rb = t == 4 && v.finish == CardFinish.Normal;
            Sfx.Reveal(t);
            if (v.hype >= 4) Sfx.PlayBgm("bgm_rare", 0.6f);
            if (t >= 4) Sfx.Play("boom", t >= 5 ? 1f : 0.7f);
            if (t >= 1) StartCoroutine(Flash(t >= 3 ? Color.Lerp(Color.white, c, 0.25f) : Color.white, new[] { 0f, 0.08f, 0.3f, 0.55f, 0.75f, 0.95f }[t], 0.2f + 0.1f * t));
            shake = Mathf.Max(shake, new[] { 0f, 0f, 5f, 14f, 22f, 36f }[t]);

            v.aura.color = new Color(c.r, c.g, c.b, new[] { 0f, 0.3f, 0.7f, 0.9f, 1f, 1f }[t]);
            if (rb) rainbow.Add(v.aura);
            if (t >= 3)
            {
                v.rays.color = new Color(c.r, c.g, c.b, new[] { 0f, 0f, 0f, 0.55f, 0.7f, 0.85f }[t]);
                if (rb) rainbow.Add(v.rays);
            }
            if (t >= 4)
            {
                bgRays.color = new Color(c.r, c.g, c.b, t >= 5 ? 0.55f : 0.4f);
                if (rb) rainbow.Add(bgRays);
                bgRaysSpeed = t >= 5 ? 26f : 16f;
                bgGlow.color = new Color(c.r, c.g, c.b, 0.35f);
                if (rb) rainbow.Add(bgGlow);
            }
            if (t >= 2)
            {
                v.holoOn = true;
                v.holo.color = new Color(1, 1, 1, 0.3f + 0.06f * t);
                if (rb) rainbow.Add(v.holo);
                StartCoroutine(Ring(CenterPos, c, 900 + t * 220, 0.55f + 0.05f * t, rb));
                if (t >= 5) StartCoroutine(Ring(CenterPos, Color.white, 1700, 0.9f, false, 0.12f));
                if (v.finish == CardFinish.Normal) ShowBanner(v);
            }
            int n = new[] { 0, 6, 18, 34, 50, 80 }[t];
            if (n > 0) Burst(CenterPos, n, c, 220, 560 + t * 110, 16 + t * 2, 30 + t * 5, 0.8f + t * 0.12f, rb, t >= 3 ? 220 : 0);
            if (t >= 3) Burst(CenterPos, t * 4, Color.white, 120, 400, 10, 18, 1f, false, 0);
            embers = t >= 5;
            if (v.finish != CardFinish.Normal) FinishImpact(v);
        }

        /// <summary>가공 카드 공개: Seven = 은빛 폭발, StrikerZ = 금빛 대폭발 (금가루 비 · 3중 충격파 · 화면 흔들림)</summary>
        void FinishImpact(CardView v)
        {
            bool z = v.finish == CardFinish.StrikerZ;
            var c = FinishStyle.Base(v.finish);
            var hi = FinishStyle.High(v.finish);
            Sfx.Play(z ? "strikerz" : "seven");
            if (z) Sfx.Play("boom", 1f);
            StartCoroutine(Flash(Color.Lerp(Color.white, c, 0.45f), z ? 1f : 0.6f, z ? 1.2f : 0.6f));
            shake = Mathf.Max(shake, z ? 48f : 14f);
            rainbow.Remove(v.aura); rainbow.Remove(v.rays); rainbow.Remove(v.holo);
            v.aura.color = new Color(c.r, c.g, c.b, 1f);
            v.rays.color = new Color(hi.r, hi.g, hi.b, z ? 0.95f : 0.6f);
            v.holoOn = true;
            v.holo.color = new Color(hi.r, hi.g, hi.b, z ? 0.6f : 0.45f);
            StartCoroutine(Ring(CenterPos, hi, z ? 2300 : 1400, z ? 0.9f : 0.7f, false));
            Burst(CenterPos, z ? 150 : 55, c, 250, z ? 1300 : 820, 16, z ? 46 : 32, z ? 1.7f : 1.1f, false, z ? 260 : 120);
            Burst(CenterPos, z ? 60 : 22, hi, 150, 520, 10, 22, 1.2f, false, 0);
            if (z)
            {
                rainbow.Remove(bgRays); rainbow.Remove(bgGlow);
                StartCoroutine(Ring(CenterPos, c, 2700, 1.2f, false, 0.15f));
                StartCoroutine(Ring(CenterPos, Color.white, 1900, 0.8f, false, 0.4f));
                bgRays.color = new Color(c.r, c.g, c.b, 0.8f);
                bgRaysSpeed = 36f;
                bgGlow.color = new Color(c.r, c.g, c.b, 0.55f);
                StartCoroutine(FadeA(dim, 0.72f, 0.2f));
                StartCoroutine(GoldRain(c, hi, 4f));
                StartCoroutine(Punch(v, 1.3f));
            }
            else StartCoroutine(Punch(v, 1.12f));
            ShowBanner(v);
        }

        IEnumerator GoldRain(Color c, Color hi, float dur)
        {
            float emit = 0;
            for (float t = 0; t < dur && !skipPack; t += Time.unscaledDeltaTime)
            {
                emit += Time.unscaledDeltaTime * Mathf.Lerp(90f, 20f, t / dur);
                while (emit > 1)
                {
                    emit--;
                    var p = Spawn(UnityEngine.Random.value < 0.6f ? GameAssets.Sparkle : GameAssets.Glow, Color.Lerp(c, hi, UnityEngine.Random.value),
                        UnityEngine.Random.Range(14f, 34f), UnityEngine.Random.Range(1.6f, 2.6f));
                    p.pos = new Vector2(UnityEngine.Random.Range(-980f, 980f), 600f);
                    p.vel = new Vector2(UnityEngine.Random.Range(-60f, 60f), UnityEngine.Random.Range(-420f, -160f));
                    p.grav = 240f;
                    p.drag = 0.3f;
                    p.spin = UnityEngine.Random.Range(-300f, 300f);
                }
                yield return null;
            }
        }

        IEnumerator Punch(CardView v, float peak)
        {
            yield return Tween(0.55f, k => v.rt.localScale = Vector3.one * Mathf.LerpUnclamped(peak, 1f, OutBack(k)));
        }

        void ShowBanner(CardView v)
        {
            int t = v.tier;
            string main, sub;
            switch (t)
            {
                case 0: main = "C"; sub = "COMMON"; break;
                case 1:
                case 2: main = "UC"; sub = "UNCOMMON"; break;
                case 3:
                case 4: main = "RARE!"; sub = "RARE"; break;
                default: main = "UNIQUE!!!"; sub = "유니크 강림"; break;
            }
            var c = TierColor(t);
            if (v.finish != CardFinish.Normal)
            {
                bool z = v.finish == CardFinish.StrikerZ;
                sub = $"{(z ? "금빛 StrikerZ 가공" : "은빛 Seven 가공")}  ·  {CardData.RarityLabel(v.card.rarity)}";
                main = z ? "STRIKERZ!!!" : "SEVEN!";
                c = FinishStyle.High(v.finish);
            }
            banner.text = main;
            banner.color = Color.Lerp(c, Color.white, 0.25f);
            bannerSub.text = $"{sub}  ·  {v.card.name}";
            bannerSub.color = Color.Lerp(c, Color.white, 0.55f);
            if (t == 4 && v.finish == CardFinish.Normal) rainbow.Add(banner);
            bannerRt.gameObject.SetActive(true);
            bannerRt.SetAsLastSibling();
            title.enabled = false;
            StartCoroutine(Tween(0.35f, k =>
            {
                bannerRt.localScale = Vector3.one * Mathf.LerpUnclamped(2.4f, 1f, OutBack(k));
                SetA(banner, Mathf.Clamp01(k * 2.5f));
                SetA(bannerSub, Mathf.Clamp01(k * 2f - 0.6f));
            }));
        }

        void EndImpact(CardView v)
        {
            bannerRt.gameObject.SetActive(false);
            title.enabled = true;
            rainbow.Remove(banner);
            embers = false;
            StartCoroutine(FadeA(dim, 0, 0.3f));
            StartCoroutine(FadeA(bgRays, 0, 0.4f));
            StartCoroutine(FadeA(bgGlow, 0, 0.4f));
            bgRaysSpeed = 12f;
            if (v.hype >= 3) StartCoroutine(FadeA(v.rays, 0.32f, 0.3f));
        }

        void EndImpactAll()
        {
            bannerRt.gameObject.SetActive(false);
            title.enabled = true;
            embers = false;
            SetA(dim, 0); SetA(bgRays, 0); SetA(bgGlow, 0);
            hint.text = "";
            foreach (var v in views) if (v.hype >= 3 && v.revealed) SetA(v.rays, 0.32f);
        }

        void QuickReveal(CardView v)
        {
            ShowFace(v);
            var c = TierColor(v.tier);
            v.aura.color = new Color(c.r, c.g, c.b, new[] { 0f, 0.3f, 0.7f, 0.9f, 1f, 1f }[v.tier]);
            v.back.color = Color.white;
            if (v.tier >= 3) v.rays.color = new Color(c.r, c.g, c.b, 0.32f);
            if (v.tier >= 2) { v.holoOn = true; v.holo.color = new Color(1, 1, 1, 0.3f + 0.05f * v.tier); }
            if (v.tier == 4 && v.finish == CardFinish.Normal) { rainbow.Add(v.aura); rainbow.Add(v.rays); rainbow.Add(v.holo); }
            if (v.finish != CardFinish.Normal)
            {
                var fc = FinishStyle.Base(v.finish);
                v.aura.color = new Color(fc.r, fc.g, fc.b, 1f);
                v.rays.color = new Color(fc.r, fc.g, fc.b, 0.32f);
            }
        }

        IEnumerator FlyTo(CardView v, Vector2 to, float scale, float dur)
        {
            Vector2 from = v.rt.anchoredPosition;
            float s0 = v.rt.localScale.x;
            yield return Tween(dur, k =>
            {
                float e = OutCubic(k);
                v.rt.anchoredPosition = Vector2.Lerp(from, to, e);
                v.rt.localScale = Vector3.one * Mathf.Lerp(s0, scale, e);
            });
        }

        // ── 4. 결과 ──

        IEnumerator Summary(bool instant)
        {
            Stage = "summarizing";
            hint.text = counter.text = "";
            // 결과 그리드는 원래 팩 순서 대신 공개 순서(낮은 등급 → 높은 등급) 그대로
            if (instant)
            {
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].rt.anchoredPosition = GridSlot(i);
                    views[i].rt.localScale = Vector3.one * GridScale;
                    views[i].rt.localRotation = Quaternion.identity;
                }
                StartCoroutine(Flash(Color.white, 0.25f, 0.3f));
            }
            else
            {
                var from = views.Select(v => v.rt.anchoredPosition).ToList();
                var s0 = views.Select(v => v.rt.localScale.x).ToList();
                var r0 = views.Select(v => v.rt.localEulerAngles.z).ToList();
                yield return Tween(0.45f, k =>
                {
                    for (int i = 0; i < views.Count; i++)
                    {
                        float e = OutCubic(Mathf.Clamp01(k * 1.3f - i * 0.04f));
                        views[i].rt.anchoredPosition = Vector2.Lerp(from[i], GridSlot(i), e);
                        views[i].rt.localScale = Vector3.one * Mathf.Lerp(s0[i], GridScale, e);
                        views[i].rt.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(r0[i], 0, e));
                    }
                });
            }
            Stage = "summary";
        }

        // ── 남은 팩 한꺼번에: 결과 목록 ──

        const float AllW = 150f, AllH = 210f;

        IEnumerator ShowAll(BoosterPack pack, List<(CardData card, bool isNew, CardFinish finish)> cards, int packCount)
        {
            ResetFx();
            ShowButtons(false, false, false);
            packRt.gameObject.SetActive(false);
            Stage = "summary";
            // 높은 등급·가공부터 (같으면 새 카드 먼저)
            var sorted = cards.OrderByDescending(x => Hype(x.card.rarity, x.finish))
                .ThenByDescending(x => x.card.rarity).ThenByDescending(x => x.finish)
                .ThenByDescending(x => x.isNew).ThenBy(x => x.card.id).ToList();

            title.text = $"{pack.code}  남은 {packCount}팩 · {cards.Count}장";
            var counts = cards.GroupBy(x => x.card.rarity).OrderByDescending(g => g.Key)
                .Select(g => $"{g.Key} {g.Count()}");
            int fin = cards.Count(x => x.finish != CardFinish.Normal), fresh = cards.Count(x => x.isNew);
            counter.text = "";
            hint.text = "";

            var panel = UIKit.Rect("AllResults", root).Fill();
            var info = UIKit.Txt(panel, "Info", string.Join("   ·   ", counts) + (fin > 0 ? $"   ·   가공 {fin}" : "") + (fresh > 0 ? $"   ·   NEW {fresh}" : ""),
                24, UIKit.Ink, TextAnchor.MiddleCenter);
            info.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(1600, 40));
            var grid = UIKit.ScrollGrid(panel, "Grid", new Vector2(AllW, AllH), new Vector2(14, 18), out var scroll);
            ((RectTransform)grid.parent).Anchor(0.03f, 0.13f, 0.97f, 0.86f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(12, 12, 14, 14);
            var cells = new List<RectTransform>();
            foreach (var (card, isNew, finish) in sorted)
            {
                var cell = UIKit.Rect("Card_" + card.id, grid);
                var inner = UIKit.Rect("In", cell).Fill();
                var face = CardFace.Build(inner, AllW, AllH, 0.21f);
                face.root.Fill();
                face.Set(card);
                face.SetFinish(finish);
                int tier = CardData.RarityTier(card.rarity);
                var gc = finish != CardFinish.Normal ? FinishStyle.Base(finish) : TierColor(tier);
                face.glow.gameObject.SetActive(tier >= 2 || finish != CardFinish.Normal);
                face.glow.color = new Color(gc.r, gc.g, gc.b, tier >= 3 || finish != CardFinish.Normal ? 0.85f : 0.5f);
                if (isNew)
                {
                    var b = UIKit.Img(inner, "New", new Color(0.9f, 0.2f, 0.25f, 0.95f), GameAssets.Rounded, true);
                    b.rectTransform.Anchor(0.55f, 0.9f, 1.04f, 1.03f);
                    UIKit.Txt(b.transform, "T", "NEW", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Fill();
                }
                cell.HitArea();
                var c = card; var f = finish;
                cell.gameObject.AddComponent<Catcher>().onClick = () => CardZoom.Show(c, f);
                inner.localScale = Vector3.zero;
                cells.Add(inner);
            }
            closeBtn.rt.SetAsLastSibling();
            scroll.verticalNormalizedPosition = 1;

            // 왼쪽 위부터 차례로 톡톡 나타난다 (높은 등급은 번쩍)
            StartCoroutine(Flash(Color.white, 0.2f, 0.25f));
            tapped = false;                                   // 화면을 누르면 나머지는 한 번에
            for (int i = 0; i < cells.Count; i++)
            {
                var r = cells[i];
                int tier = CardData.RarityTier(sorted[i].card.rarity);
                StartCoroutine(Tween(0.22f, k => r.localScale = Vector3.one * OutBack(k)));
                if (tier >= 3 || sorted[i].finish != CardFinish.Normal) Sfx.Play("flip", 0.6f);
                if (!tapped) yield return Wait(i < 14 ? 0.05f : 0.015f);
                else r.localScale = Vector3.one;
            }
            nextPressed = false;
            ShowButtons(false, false, true);
            while (!nextPressed) yield return null;
            Destroy(panel.gameObject);
        }

        // ══════════════════════════ 카드 ══════════════════════════

        static int Hype(Rarity r, CardFinish f) => Mathf.Max(CardData.RarityTier(r), f == CardFinish.StrikerZ ? 5 : f == CardFinish.Seven ? 3 : 0);

        CardView MakeCard(CardData card, bool isNew, CardFinish finish)
        {
            var v = new CardView { card = card, tier = CardData.RarityTier(card.rarity), finish = finish, hype = Hype(card.rarity, finish), phase = UnityEngine.Random.value * 1.5f };
            v.rt = UIKit.Rect("Card_" + card.id, cardsLayer).Place(new Vector2(0.5f, 0.5f), StackPos, new Vector2(CardW, CardH));
            // 결과 화면에서 카드 클릭 = 확대. 공개 연출 중에는 지금처럼 탭(다음 진행)으로 넘긴다
            v.rt.HitArea();
            v.rt.gameObject.AddComponent<Catcher>().onClick = () =>
            {
                if (Stage == "summary") CardZoom.Show(card, finish);
                else tapped = true;
            };
            // 광선·오라는 모든 카드 뒤 레이어에 두고 Update 에서 카드 위치를 따라가게 한다
            v.fx = UIKit.Rect("Fx_" + card.id, underLayer).Place(new Vector2(0.5f, 0.5f), StackPos, new Vector2(CardW, CardH));
            v.rays = UIKit.Img(v.fx, "Rays", Clear, RaysSprite);
            v.rays.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1050, 1050));
            v.aura = UIKit.Img(v.fx, "Aura", Clear, GameAssets.Glow);
            v.aura.rectTransform.Anchor(-0.55f, -0.4f, 1.55f, 1.4f);
            v.faceRoot = UIKit.Rect("FaceRoot", v.rt).Fill();
            var face = CardFace.Build(v.faceRoot, CardW, CardH, 0.42f);
            face.root.Fill();
            face.glow.gameObject.SetActive(false);
            face.Set(card);
            face.SetFinish(finish);
            var mask = UIKit.Rect("Holo", v.faceRoot).Anchor(0.02f, 0.01f, 0.98f, 0.99f);
            mask.gameObject.AddComponent<RectMask2D>();
            v.holo = UIKit.Img(mask, "Bar", Clear, GameAssets.Glow);
            v.holo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-500, 0), new Vector2(170, 1000));
            v.holo.rectTransform.localRotation = Quaternion.Euler(0, 0, -24);
            if (isNew)
            {
                var b = UIKit.Img(v.faceRoot, "New", new Color(0.9f, 0.2f, 0.25f, 0.95f), GameAssets.Rounded, true);
                b.rectTransform.Anchor(0.6f, 0.905f, 1.03f, 1.02f);
                var tx = UIKit.Txt(b.transform, "T", "NEW", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                tx.rectTransform.Fill();
            }
            v.faceRoot.gameObject.SetActive(false);
            v.back = UIKit.Img(v.rt, "Back", Color.white, GameAssets.TexSprite("card_back"));
            v.back.rectTransform.Fill();
            return v;
        }

        static Color TierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.8f, 0.86f, 0.96f);
                case 1: return new Color(0.4f, 0.8f, 1f);
                case 2: return new Color(0.76f, 0.45f, 1f);
                case 3: return new Color(1f, 0.8f, 0.25f);
                case 4: return new Color(1f, 0.4f, 0.7f);
                default: return new Color(1f, 0.38f, 0.07f);
            }
        }

        // ══════════════════════════ 매 프레임 ══════════════════════════

        void Update()
        {
            if (root == null || !root.gameObject.activeInHierarchy) return;
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;

            stage.anchoredPosition = shake > 0.3f ? UnityEngine.Random.insideUnitCircle * shake : Vector2.zero;
            shake *= Mathf.Exp(-dt * 7f);

            bgRays.rectTransform.localRotation = Quaternion.Euler(0, 0, -now * bgRaysSpeed);
            foreach (var v in views)
            {
                v.fx.anchoredPosition = v.rt.anchoredPosition;
                v.fx.localScale = v.rt.localScale;
                if (v.rays.color.a > 0.01f) v.rays.rectTransform.localRotation = Quaternion.Euler(0, 0, now * (v.tier >= 5 ? 32f : 18f));
                if (v.holoOn)
                {
                    float u = Mathf.Repeat(now * 0.55f + v.phase, 1.7f);
                    v.holo.rectTransform.anchoredPosition = new Vector2(-340 + u * 400, 0);
                }
            }
            if (rainbow.Count > 0)
            {
                int i = 0;
                foreach (var g in rainbow)
                {
                    if (g == null) continue;
                    var c = Color.HSVToRGB(Mathf.Repeat(now * 0.45f + i++ * 0.13f, 1f), 0.6f, 1f);
                    c.a = g.color.a;
                    g.color = c;
                }
            }
            if (embers && UnityEngine.Random.value < dt * 40f) Ember(CenterPos + new Vector2(UnityEngine.Random.Range(-420f, 420f), -300));
            UpdateParticles(dt);
        }

        // ══════════════════════════ 파티클 (UI) ══════════════════════════

        class Part
        {
            public Image img;
            public Vector2 pos, vel, from, to;
            public float life, max, size, spin, grav, drag;
            public bool converge, rainbow;
            public Color col;
        }

        readonly List<Part> parts = new List<Part>();
        readonly Stack<Image> pool = new Stack<Image>();

        Part Spawn(Sprite s, Color c, float size, float life)
        {
            var img = pool.Count > 0 ? pool.Pop() : UIKit.Img(fxLayer, "P", Clear);
            img.gameObject.SetActive(true);
            img.sprite = s;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var p = new Part { img = img, col = c, size = size, max = life, drag = 2f };
            parts.Add(p);
            return p;
        }

        void Burst(Vector2 at, int n, Color c, float spMin, float spMax, float szMin, float szMax, float life, bool rb, float grav)
        {
            for (int i = 0; i < n; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                var p = Spawn(UnityEngine.Random.value < 0.7f ? GameAssets.Sparkle : GameAssets.Glow, Color.Lerp(c, Color.white, UnityEngine.Random.value * 0.5f),
                    UnityEngine.Random.Range(szMin, szMax), life * UnityEngine.Random.Range(0.6f, 1.2f));
                p.pos = at + dir * UnityEngine.Random.Range(0f, 40f);
                p.vel = dir * UnityEngine.Random.Range(spMin, spMax);
                p.spin = UnityEngine.Random.Range(-360f, 360f);
                p.grav = grav;
                p.rainbow = rb;
            }
        }

        void Converge(Vector2 at, Color c, float radius, float life, bool rb)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            var p = Spawn(GameAssets.Sparkle, Color.Lerp(c, Color.white, 0.3f), UnityEngine.Random.Range(12f, 24f), life);
            p.converge = true;
            p.from = at + dir * radius * UnityEngine.Random.Range(0.8f, 1.2f);
            p.to = at + dir * 30f;
            p.pos = p.from;
            p.rainbow = rb;
        }

        void Ember(Vector2 at)
        {
            var p = Spawn(GameAssets.Glow, new Color(1f, UnityEngine.Random.Range(0.3f, 0.7f), 0.1f), UnityEngine.Random.Range(10f, 26f), UnityEngine.Random.Range(1.2f, 2.2f));
            p.pos = at;
            p.vel = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(120f, 280f));
            p.drag = 0.2f;
        }

        void UpdateParticles(float dt)
        {
            float now = Time.unscaledTime;
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                p.life += dt;
                float t = p.life / p.max;
                if (t >= 1f)
                {
                    p.img.gameObject.SetActive(false);
                    pool.Push(p.img);
                    parts.RemoveAt(i);
                    continue;
                }
                float a;
                if (p.converge)
                {
                    p.pos = Vector2.Lerp(p.from, p.to, t * t);
                    a = Mathf.Min(1, t * 4f);
                }
                else
                {
                    p.vel *= Mathf.Exp(-p.drag * dt);
                    p.vel.y -= p.grav * dt;
                    p.pos += p.vel * dt;
                    a = Mathf.Min(1, t * 10f) * (1 - t);
                }
                var c = p.rainbow ? Color.HSVToRGB(Mathf.Repeat(now * 0.8f + i * 0.07f, 1f), 0.55f, 1f) : p.col;
                c.a = a;
                p.img.color = c;
                var rt = p.img.rectTransform;
                rt.anchoredPosition = p.pos;
                rt.localRotation = Quaternion.Euler(0, 0, p.spin * p.life);
                rt.localScale = Vector3.one * (p.converge ? 1f - 0.5f * t : 1f - 0.4f * t);
            }
        }

        IEnumerator Ring(Vector2 at, Color c, float maxSize, float dur, bool rb, float delay = 0)
        {
            if (delay > 0) yield return Wait(delay);
            var img = UIKit.Img(fxLayer, "Ring", new Color(c.r, c.g, c.b, 0), RingSprite);
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), at, new Vector2(120, 120));
            if (rb) rainbow.Add(img);
            yield return Tween(dur, k =>
            {
                float e = OutCubic(k);
                img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(120, maxSize, e);
                SetA(img, 0.9f * (1 - k));
            });
            rainbow.Remove(img);
            Destroy(img.gameObject);
        }

        IEnumerator Flash(Color c, float peak, float dur)
        {
            if (peak <= 0) yield break;
            flash.color = new Color(c.r, c.g, c.b, peak);
            yield return Tween(dur, k => SetA(flash, peak * (1 - k) * (1 - k)));
        }

        // ══════════════════════════ 유틸 ══════════════════════════

        IEnumerator WaitTap(float autoDelay, float timeout = -1f)
        {
            tapped = false;
            float t = 0;
            while (!tapped && !skipPack)
            {
                t += Time.unscaledDeltaTime;
                if (auto && t > autoDelay) break;
                if (timeout > 0 && t > timeout) break;
                SetA(hint, 0.55f + 0.45f * Mathf.Sin(t * 5f));
                yield return null;
            }
            tapped = false;
        }

        static IEnumerator Wait(float sec)
        {
            for (float t = 0; t < sec; t += Time.unscaledDeltaTime) yield return null;
        }

        static IEnumerator Tween(float dur, Action<float> f)
        {
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                f(t / dur);
                yield return null;
            }
            f(1f);
        }

        IEnumerator FadeA(Graphic g, float to, float dur)
        {
            float from = g.color.a;
            yield return Tween(dur, k => SetA(g, Mathf.Lerp(from, to, k)));
        }

        static void SetA(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        static float OutCubic(float x) => 1 - Mathf.Pow(1 - x, 3);

        static float OutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }

        // ── 절차적 스프라이트 ──

        static Sprite rays, ring;

        static Sprite RaysSprite
        {
            get
            {
                if (rays != null) return rays;
                const int n = 256;
                return rays = GameAssets.ProcSprite(n, (x, y) =>
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float r = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(ang * 14), 5f) * 0.8f + Mathf.Pow(0.5f + 0.5f * Mathf.Cos(ang * 9 + 1.3f), 8f) * 0.5f;
                    return GameAssets.White(Mathf.Clamp01(r) * Mathf.Pow(Mathf.Clamp01(1 - d), 1.6f) * Mathf.Clamp01(d * 6f));
                });
            }
        }

        static Sprite RingSprite
        {
            get
            {
                if (ring != null) return ring;
                const int n = 256;
                return ring = GameAssets.ProcSprite(n, (x, y) =>
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Exp(-Mathf.Pow((d - 0.86f) / 0.07f, 2)) + 0.25f * Mathf.Exp(-Mathf.Pow((d - 0.7f) / 0.18f, 2));
                    return GameAssets.White(Mathf.Clamp01(a) * Mathf.Clamp01((1 - d) * 20));
                });
            }
        }
    }

    /// <summary>클릭 · 드래그 입력 수신기</summary>
    public class Catcher : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action onClick;
        public Action<PointerEventData> onDrag;
        public void OnPointerClick(PointerEventData e) => onClick?.Invoke();
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
        public void OnEndDrag(PointerEventData e) { }
    }
}
