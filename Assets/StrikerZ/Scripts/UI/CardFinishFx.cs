using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>가공 색 정의</summary>
    public static class FinishStyle
    {
        public static Color Base(CardFinish f) => f == CardFinish.StrikerZ ? new Color(1f, 0.7f, 0.14f) : new Color(0.74f, 0.78f, 0.86f);
        public static Color High(CardFinish f) => f == CardFinish.StrikerZ ? new Color(1f, 0.98f, 0.78f) : new Color(1f, 1f, 1f);
        public static Color Deep(CardFinish f) => f == CardFinish.StrikerZ ? new Color(0.62f, 0.3f, 0f) : new Color(0.4f, 0.43f, 0.52f);
        public static string Label(CardFinish f) => f == CardFinish.StrikerZ ? "STRIKERZ" : f == CardFinish.Seven ? "SEVEN" : "";

        /// <summary>UI 글자색 (틀 색 Base 보다 밝게 읽히는 금·은)</summary>
        public const string StrikerZHex = "ffd45a", SevenHex = "dfe6f5";
        public static string Symbol(CardFinish f) => f == CardFinish.StrikerZ ? "★" : f == CardFinish.Seven ? "◆" : "";
        public static string Name(CardFinish f) => f == CardFinish.StrikerZ ? "StrikerZ" : f == CardFinish.Seven ? "Seven" : "일반";

        /// <summary>
        /// 가공 표시 리치 텍스트. 기본 "★ StrikerZ" / "◆ Seven" (일반은 "일반").
        /// text 를 주면 기호 뒤에 그 글자 (예: Rich(f, "3") → "★3", Rich(f, "") → 기호만, 일반은 text 그대로)
        /// </summary>
        public static string Rich(CardFinish f, string text = null)
        {
            if (f == CardFinish.Normal) return text ?? "일반";
            return $"<color=#{(f == CardFinish.StrikerZ ? StrikerZHex : SevenHex)}>{Symbol(f)}{text ?? " " + Name(f)}</color>";
        }
    }

    /// <summary>
    /// 이름 글자에 금속 광택: 세로 그라데이션 + 좌→우로 지나가는 하이라이트 띠.
    /// 같은 오브젝트의 Outline 이 만든 복제 정점은 건드리지 않는다 (원본 정점만 채색).
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ShimmerText : BaseMeshEffect
    {
        public Color baseColor = Color.white, highColor = Color.white, deepColor = Color.gray;
        public float phase;
        public float strength = 1f;
        readonly List<UIVertex> verts = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            verts.Clear();
            vh.GetUIVertexStream(verts);
            int n = verts.Count;
            // Outline 이 켜져 있으면 원본은 마지막 1/5
            var outline = GetComponent<Outline>();
            int start = outline != null && outline.enabled && outline.IsActive() ? n - n / 5 : 0;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = start; i < n; i++)
            {
                var p = verts[i].position;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float w = Mathf.Max(1f, maxX - minX), h = Mathf.Max(1f, maxY - minY);
            for (int i = start; i < n; i++)
            {
                var v = verts[i];
                float u = (v.position.x - minX) / w, t = (v.position.y - minY) / h;
                var c = Color.Lerp(deepColor, baseColor, 0.35f + 0.65f * t);      // 위가 밝은 금속 그라데이션
                float d = u - phase + (t - 0.5f) * 0.25f;
                float band = Mathf.Exp(-d * d * 30f) * strength;
                c = Color.Lerp(c, highColor, band);
                c.a = v.color.a / 255f;
                v.color = c;
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

        public void Refresh() { if (graphic != null) graphic.SetVerticesDirty(); }
    }

    /// <summary>
    /// 가공 카드 연출: 금속 테두리 · 반짝이는 이름 · 10초에 걸쳐 차오르는 은/금빛 글로우 · 포일 광택 · 반짝이.
    /// CardFace.SetFinish 가 켜고 끈다. 시간은 unscaled (일시정지·배속과 무관).
    /// </summary>
    public class FinishFx : MonoBehaviour
    {
        public const float Cycle = 10f;      // 페이드 인 주기 (초)
        public CardFinish finish;
        public Image border, glow, sheen;
        public bool texturedBorder;
        public RawImage secret;
        public ShimmerText shimmer;
        public readonly List<Image> sparkles = new List<Image>();
        public float width = 300, height = 420;
        float offset;
        readonly List<float> sparkPhase = new List<float>();

        void OnEnable()
        {
            offset = Random.value * Cycle;
        }

        void Update()
        {
            if (finish == CardFinish.Normal) return;
            float now = Time.unscaledTime + offset;
            bool z = finish == CardFinish.StrikerZ;
            var bc = FinishStyle.Base(finish);
            var hc = FinishStyle.High(finish);

            // 10초 페이드 인: 0 → 1 로 천천히 차오르고, 마지막 1초에 절정 후 빠르게 사그라듦
            float c = Mathf.Repeat(now, Cycle);
            float fade = c < Cycle - 1f ? Mathf.SmoothStep(0f, 1f, c / (Cycle - 1f)) : Mathf.Lerp(1f, 0.05f, (c - (Cycle - 1f)));
            float peak = c > Cycle - 1.4f && c < Cycle - 0.6f ? 1f - Mathf.Abs(c - (Cycle - 1f)) / 0.4f : 0f;   // 절정 플래시

            if (glow != null)
            {
                var g = Color.Lerp(bc, hc, 0.3f + 0.4f * peak);
                g.a = (z ? 0.25f : 0.15f) + (z ? 0.7f : 0.55f) * fade + 0.25f * peak;
                glow.color = g;
                float s = 1f + 0.06f * fade + 0.08f * peak;
                glow.rectTransform.localScale = new Vector3(s, s, 1);
            }
            if (border != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(now * (z ? 3.2f : 2.2f));
                if (texturedBorder)
                {
                    // 금속 테두리 원래 색 유지, 밝기만 숨쉬듯 (절정에서 최대)
                    float b = Mathf.Lerp(0.8f, 1f, 0.3f + 0.4f * fade * pulse + 0.3f * peak);
                    border.color = new Color(b, b, b, 1f);
                }
                else border.color = Color.Lerp(bc, hc, 0.15f + 0.45f * fade * pulse + 0.4f * peak);
            }
            if (shimmer != null)
            {
                shimmer.phase = Mathf.Repeat(now / (z ? 1.6f : 2.2f), 1f) * 1.8f - 0.4f;
                shimmer.strength = 0.65f + 0.35f * fade;
                shimmer.Refresh();
            }
            if (sheen != null)
            {
                float u = Mathf.Repeat(now / (z ? 2.6f : 3.4f), 1f);
                sheen.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.9f, width * 0.9f, u), 0);
                var sc = hc;
                sc.a = (z ? 0.16f : 0.1f) + (z ? 0.3f : 0.22f) * fade + 0.3f * peak;
                sheen.color = sc;
            }
            if (secret != null)
            {
                // 사선 포일: 빛이 지나갈 때(sheen 위치) 무지개가 흐르고 밝아진다
                float sweep = Mathf.Repeat(now / (z ? 2.6f : 3.4f), 1f);
                float glint = Mathf.Exp(-Mathf.Pow((sweep - 0.5f) * 3.2f, 2));
                var r = secret.uvRect;
                r.x = now * (z ? 0.06f : 0.04f) + glint * 0.18f;
                r.y = -now * 0.015f;
                secret.uvRect = r;
                var tint = z ? new Color(1f, 0.86f, 0.5f) : new Color(0.88f, 0.94f, 1f);
                tint.a = (z ? 0.13f : 0.1f) + (z ? 0.14f : 0.11f) * fade + (z ? 0.17f : 0.13f) * glint + 0.08f * peak;
                secret.color = tint;
            }
            while (sparkPhase.Count < sparkles.Count) sparkPhase.Add(Random.value * 3f);
            for (int i = 0; i < sparkles.Count; i++)
            {
                float p = Mathf.Repeat(now * (0.55f + 0.1f * i) + sparkPhase[i], 1.6f);
                float a = p < 0.5f ? Mathf.Sin(p / 0.5f * Mathf.PI) : 0f;
                var sp = sparkles[i];
                if (p < 0.02f) sp.rectTransform.anchoredPosition = new Vector2(Random.Range(-0.42f, 0.42f) * width, Random.Range(-0.45f, 0.45f) * height);
                var col = hc;
                col.a = a * (0.35f + 0.65f * fade);
                sp.color = col;
                float s = (z ? 1.25f : 1f) * (0.6f + 0.6f * a);
                sp.rectTransform.localScale = new Vector3(s, s, 1);
                sp.rectTransform.localRotation = Quaternion.Euler(0, 0, now * 40f + i * 30f);
            }
        }
    }
}
