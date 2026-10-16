using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 카드 효과 연출. 절차적 파티클 · 링 · 빛기둥 · 번개 · 투사체 · 베기 · 조준선 · 방어막 · 화면 틴트.
    /// GameManager 의 기본 처리(데미지/회복/파괴/상태 등)에서 자동으로 호출되고,
    /// 마법·영웅·신화 카드는 발동 순간 Signature() 로 카드 고유 연출을 한 번 더 보여 준다.
    /// </summary>
    public class CardFx : MonoBehaviour
    {
        public static CardFx I { get; private set; }

        public enum Style { Neutral, Fire, Water, Wind, Earth, Thunder, Light, Dark, Rainbow }

        BoardView board;
        Image tint, disk, corona;
        static Material particleMat;
        /// <summary>보드 카메라 거리에서 잘 보이도록 파티클 크기 배율</summary>
        const float SizeK = 2.4f;
        static Texture2D ringTex;

        Camera Cam => board != null && board.cam != null ? board.cam : Camera.main;

        public void Init(BoardView b)
        {
            I = this;
            board = b;
            // 화면 틴트 (HUD 바로 아래 — 보드는 어두워지고 HUD 글자는 보인다)
            var go = new GameObject("FxOverlay", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var root = (RectTransform)go.transform;
            tint = UIKit.Img(root, "Tint", Color.clear);
            tint.rectTransform.Fill();
            tint.raycastTarget = false;
            corona = UIKit.Img(root, "Corona", Color.clear, GameAssets.Glow);
            corona.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(620, 620));
            corona.raycastTarget = false;
            disk = UIKit.Img(root, "Disk", Color.clear, GameAssets.Circle);
            disk.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(300, 300));
            disk.raycastTarget = false;
        }

        // ══════════════════════════ 스타일 ══════════════════════════

        static readonly Dictionary<string, Style> IdStyle = new Dictionary<string, Style>
        {
            { "S001", Style.Fire }, { "S002", Style.Light }, { "S003", Style.Dark }, { "S004", Style.Thunder },
            { "S005", Style.Fire }, { "S006", Style.Fire }, { "S007", Style.Water }, { "S008", Style.Wind },
            { "S009", Style.Light }, { "S010", Style.Fire }, { "S011", Style.Neutral }, { "S012", Style.Fire },
        };

        public static Style StyleOf(CardInstance c)
        {
            if (c == null) return Style.Neutral;
            if (c.data.id != null && IdStyle.TryGetValue(c.data.id, out var s)) return s;
            switch (c.data.element)
            {
                case Element.Fire: return Style.Fire;
                case Element.Water: return Style.Water;
                case Element.Wind: return Style.Wind;
                case Element.Earth: return Style.Earth;
                case Element.Thunder: return Style.Thunder;
                case Element.Light: return Style.Light;
                case Element.Dark: return Style.Dark;
                case Element.Rainbow: return Style.Rainbow;
                default: return Style.Neutral;
            }
        }

        public static Color ColorOf(Style s)
        {
            switch (s)
            {
                case Style.Fire: return new Color(1f, 0.42f, 0.12f);
                case Style.Water: return new Color(0.25f, 0.62f, 1f);
                case Style.Wind: return new Color(0.35f, 1f, 0.5f);
                case Style.Earth: return new Color(0.85f, 0.58f, 0.25f);
                case Style.Thunder: return new Color(1f, 0.92f, 0.3f);
                case Style.Light: return new Color(1f, 0.93f, 0.6f);
                case Style.Dark: return new Color(0.7f, 0.3f, 1f);
                case Style.Rainbow: return Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f, 1f), 0.7f, 1f);
                default: return new Color(0.9f, 0.92f, 1f);
            }
        }

        // ══════════════════════════ 위치 ══════════════════════════

        public Vector3 PosOf(CardInstance c)
        {
            if (c == null) return Vector3.zero;
            if (c.view != null) return c.view.transform.position + Vector3.up * 0.05f;
            if (c.zone == Zone.Hero && c.owner != null) return board.HeroPos(c.owner);
            return Vector3.zero;
        }

        public Vector3 HeroPos(PlayerState p) => board.HeroPos(p) + Vector3.up * 0.1f;
        public Vector3 RowCenter(PlayerState p) => new Vector3(0, 0.08f, BoardView.Row(p));
        public Vector3 DeckPos(PlayerState p) => board.DeckPos(p, p.deck.Count) + Vector3.up * 0.1f;
        public Vector3 GravePos(PlayerState p) => board.GravePos(p, p.grave.Count) + Vector3.up * 0.1f;

        // ══════════════════════════ 기본 재료 ══════════════════════════

        static Material PMat
        {
            get
            {
                if (particleMat == null) particleMat = GameAssets.ParticleMat();
                return particleMat;
            }
        }

        static Texture2D RingTex
        {
            get
            {
                if (ringTex != null) return ringTex;
                const int S = 128;
                ringTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
                        a = a * a * (3 - 2 * a);
                        px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                ringTex.SetPixels32(px);
                ringTex.Apply();
                return ringTex;
            }
        }

        // ══════════════════════════ 파티클 ══════════════════════════

        /// <summary>
        /// 불꽃·물방울·잎 등 범용 파티클. vel = 기본 속도(방향 포함), spread = 무작위 속도, gravity = 낙하(음수면 상승).
        /// </summary>
        public void Sparks(Vector3 pos, Color a, Color b, int n, Vector3 vel, float spread, float size, float life, float gravity = 0f, float radius = 0.15f)
        {
            if (n <= 0) return;
            var go = new GameObject("FxSparks");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = Mathf.Max(64, n);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = PMat;
            ps.Play();
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                ep.position = pos + Random.insideUnitSphere * radius;
                ep.velocity = vel + Random.insideUnitSphere * spread;
                ep.startColor = Color.Lerp(a, b, Random.value);
                ep.startSize = size * SizeK * Random.Range(0.6f, 1.3f);
                ep.startLifetime = life * Random.Range(0.6f, 1.1f);
                ps.Emit(ep, 1);
            }
            Destroy(go, life * 1.2f + 0.3f);
        }

        /// <summary>속성 색 폭발 파티클 (충돌·파괴 지점)</summary>
        public void Impact(Vector3 pos, Style s, float scale = 1f)
        {
            var c = ColorOf(s);
            board.Burst(pos, c, 2.4f * scale);
            switch (s)
            {
                case Style.Fire:
                    Sparks(pos, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.25f, 0.05f), (int)(40 * scale), Vector3.up * 1.2f, 2.4f * scale, 0.16f, 0.8f, -0.15f);
                    break;
                case Style.Water:
                    Sparks(pos, new Color(0.7f, 0.9f, 1f), new Color(0.2f, 0.5f, 1f), (int)(40 * scale), Vector3.up * 2f, 1.8f * scale, 0.12f, 0.9f, 0.6f);
                    break;
                case Style.Wind:
                    Spiral(pos, ColorOf(Style.Wind), 0.9f * scale, 0.8f);
                    break;
                case Style.Earth:
                    Sparks(pos, new Color(0.75f, 0.55f, 0.3f), new Color(0.4f, 0.28f, 0.15f), (int)(30 * scale), Vector3.up * 2.2f, 1.6f * scale, 0.2f, 0.9f, 1.2f);
                    break;
                case Style.Thunder:
                    Sparks(pos, Color.white, new Color(1f, 0.9f, 0.3f), (int)(36 * scale), Vector3.zero, 3.4f * scale, 0.08f, 0.45f);
                    break;
                case Style.Light:
                    Sparks(pos, Color.white, new Color(1f, 0.9f, 0.5f), (int)(34 * scale), Vector3.up * 1.4f, 1.6f * scale, 0.12f, 1f, -0.2f);
                    break;
                case Style.Dark:
                    Sparks(pos, new Color(0.8f, 0.4f, 1f), new Color(0.25f, 0.05f, 0.4f), (int)(36 * scale), Vector3.up * 0.6f, 1.8f * scale, 0.2f, 1f, -0.1f);
                    break;
                case Style.Rainbow:
                    for (int i = 0; i < 6; i++)
                        Sparks(pos, Color.HSVToRGB(i / 6f, 0.75f, 1f), Color.HSVToRGB(i / 6f + 0.08f, 0.5f, 1f), (int)(8 * scale), Vector3.up, 2.2f * scale, 0.13f, 0.9f);
                    break;
                default:
                    Sparks(pos, Color.white, new Color(0.7f, 0.75f, 0.9f), (int)(28 * scale), Vector3.up, 2f * scale, 0.12f, 0.7f);
                    break;
            }
        }

        // ══════════════════════════ 형태 ══════════════════════════

        /// <summary>바닥에 퍼지는 링 (in=true 면 안쪽으로 모인다)</summary>
        public void Ring(Vector3 pos, Color c, float size, float dur = 0.7f, bool inward = false)
            => StartCoroutine(RingRoutine(pos, c, size, dur, inward));

        IEnumerator RingRoutine(Vector3 pos, Color c, float size, float dur, bool inward)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "FxRing";
            q.transform.position = pos + Vector3.up * 0.06f;
            q.transform.rotation = Quaternion.Euler(90, 0, 0);
            var m = GameAssets.UnlitMat(c, RingTex, true);
            GameAssets.MakeTransparent(m, true);
            q.GetComponent<Renderer>().sharedMaterial = m;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float e = 1 - (1 - t) * (1 - t);
                float s = inward ? Mathf.Lerp(size, 0.2f, e) : Mathf.Lerp(0.2f, size, e);
                q.transform.localScale = new Vector3(s, s, 1);
                var col = new Color(c.r, c.g, c.b, (1 - t) * 1.3f);
                m.color = col; m.SetColor("_BaseColor", col);
                yield return null;
            }
            Destroy(m); Destroy(q);
        }

        LineRenderer Line(string name, Color c, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = PMat;
            lr.textureMode = LineTextureMode.Stretch;
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.numCapVertices = 4;
            lr.useWorldSpace = true;
            return lr;
        }

        /// <summary>빛기둥 (아래에서 위로)</summary>
        public void Pillar(Vector3 pos, Color c, float height = 4f, float width = 0.9f, float dur = 1f)
            => StartCoroutine(PillarRoutine(pos, c, height, width, dur));

        IEnumerator PillarRoutine(Vector3 pos, Color c, float height, float width, float dur)
        {
            var lr = Line("FxPillar", c, width);
            lr.positionCount = 2;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float grow = Mathf.Clamp01(t * 4f);
                float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                lr.SetPosition(0, pos - Vector3.up * 0.2f);
                lr.SetPosition(1, pos + Vector3.up * height * grow);
                lr.widthMultiplier = width * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI));
                var col = new Color(c.r, c.g, c.b, fade);
                lr.startColor = lr.endColor = col;
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        /// <summary>번개 (지그재그, 깜빡임)</summary>
        public void Bolt(Vector3 from, Vector3 to, Color c, float dur = 0.4f, float width = 0.14f)
            => StartCoroutine(BoltRoutine(from, to, c, dur, width));

        IEnumerator BoltRoutine(Vector3 from, Vector3 to, Color c, float dur, float width)
        {
            var lr = Line("FxBolt", c, width);
            const int N = 12;
            lr.positionCount = N;
            var core = Line("FxBoltCore", Color.white, width * 0.35f);
            core.positionCount = N;
            float t = 0, next = 0;
            while (t < dur)
            {
                if (t >= next)
                {
                    next = t + 0.05f;
                    Vector3 dir = to - from;
                    Vector3 side = Vector3.Cross(dir.normalized, Vector3.up);
                    if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                    for (int i = 0; i < N; i++)
                    {
                        float u = i / (float)(N - 1);
                        float j = (i == 0 || i == N - 1) ? 0 : Random.Range(-1f, 1f) * dir.magnitude * 0.07f;
                        var p = from + dir * u + side.normalized * j + Vector3.up * Random.Range(-0.05f, 0.05f);
                        lr.SetPosition(i, p);
                        core.SetPosition(i, p);
                    }
                }
                float a = Random.value < 0.25f ? 0.4f : 1f;
                lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, a);
                core.startColor = core.endColor = new Color(1, 1, 1, a);
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(lr.gameObject); Destroy(core.gameObject);
        }

        /// <summary>초승달 베기 (카드 위 사선 호)</summary>
        public void Slash(Vector3 pos, Color c, float size = 1f, float dur = 0.35f)
            => StartCoroutine(SlashRoutine(pos, c, size, dur));

        IEnumerator SlashRoutine(Vector3 pos, Color c, float size, float dur)
        {
            var lr = Line("FxSlash", c, 0.16f * size);
            const int N = 16;
            lr.positionCount = N;
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            lr.widthCurve = curve;
            float ang0 = Random.Range(20f, 60f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float sweep = Mathf.Lerp(0.2f, 1f, t * 2f);
                for (int i = 0; i < N; i++)
                {
                    float u = i / (float)(N - 1) * sweep;
                    float a = (ang0 + u * 140f) * Mathf.Deg2Rad;
                    lr.SetPosition(i, pos + new Vector3(Mathf.Cos(a) * 0.7f * size, 0.25f + u * 0.3f, Mathf.Sin(a) * 0.45f * size - 0.2f * size));
                }
                var col = new Color(c.r, c.g, c.b, 1 - t);
                lr.startColor = lr.endColor = col;
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        /// <summary>회오리 (바람)</summary>
        public void Spiral(Vector3 pos, Color c, float size = 1f, float dur = 0.9f)
            => StartCoroutine(SpiralRoutine(pos, c, size, dur));

        IEnumerator SpiralRoutine(Vector3 pos, Color c, float size, float dur)
        {
            var lr = Line("FxSpiral", c, 0.08f * size);
            const int N = 40;
            lr.positionCount = N;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                for (int i = 0; i < N; i++)
                {
                    float u = i / (float)(N - 1);
                    float a = u * 12f + t * 18f;
                    float r = (0.15f + u * 0.55f) * size;
                    lr.SetPosition(i, pos + new Vector3(Mathf.Cos(a) * r, u * 1.6f * size, Mathf.Sin(a) * r));
                }
                lr.startColor = new Color(c.r, c.g, c.b, (1 - t) * 0.3f);
                lr.endColor = new Color(c.r, c.g, c.b, (1 - t));
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        /// <summary>조준선 (자유 대상 · 저격)</summary>
        public void Reticle(Vector3 pos, Color c, float dur = 1.2f) => StartCoroutine(ReticleRoutine(pos, c, dur));

        IEnumerator ReticleRoutine(Vector3 pos, Color c, float dur)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.position = pos + Vector3.up * 0.08f;
            var m = GameAssets.UnlitMat(c, RingTex, true);
            GameAssets.MakeTransparent(m, true);
            q.GetComponent<Renderer>().sharedMaterial = m;
            var cross = Line("FxCross", c, 0.04f);
            cross.positionCount = 5;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float s = Mathf.Lerp(2.2f, 0.95f, Mathf.Clamp01(t * 3f));
                q.transform.localScale = new Vector3(s, s, 1);
                q.transform.rotation = Quaternion.Euler(90, t * 240f, 0);
                float a = t < 0.8f ? 1f : (1 - t) / 0.2f;
                var col = new Color(c.r, c.g, c.b, a);
                m.color = col; m.SetColor("_BaseColor", col);
                float h = 0.55f * s / 0.95f;
                var y = Vector3.up * 0.1f;
                cross.SetPositions(new[] { pos + y + Vector3.left * h, pos + y + Vector3.right * h, pos + y, pos + y + Vector3.forward * h, pos + y + Vector3.back * h });
                cross.startColor = cross.endColor = col;
                yield return null;
            }
            Destroy(m); Destroy(q); Destroy(cross.gameObject);
        }

        /// <summary>반구형 방어막 (파괴 방지 · HP 증가)</summary>
        public void Bubble(Vector3 pos, Color c, float dur = 0.9f) => StartCoroutine(BubbleRoutine(pos, c, dur));

        IEnumerator BubbleRoutine(Vector3 pos, Color c, float dur)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.name = "FxBubble";
            s.transform.position = pos;
            var m = GameAssets.UnlitMat(c, GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(m, true);
            s.GetComponent<Renderer>().sharedMaterial = m;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float k = Mathf.Lerp(0.4f, 1.15f, 1 - (1 - t) * (1 - t));
                s.transform.localScale = new Vector3(k, k * 0.75f, k * 1.2f);
                var col = new Color(c.r, c.g, c.b, (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f) * 0.55f);
                m.color = col; m.SetColor("_BaseColor", col);
                yield return null;
            }
            Destroy(m); Destroy(s);
        }

        /// <summary>날아가는 구체 (불덩이·물방울·암흑구 …) — 도착하면 Impact</summary>
        public IEnumerator Projectile(Vector3 from, Vector3 to, Style s, float size = 1f, float time = 0.42f)
        {
            if (s == Style.Thunder)
            {
                Sfx.Play("whoosh", 0.5f);
                Bolt(from + Vector3.up * 0.3f, to + Vector3.up * 0.1f, ColorOf(s), 0.35f);
                yield return new WaitForSeconds(0.12f);
                Impact(to, s, size);
                yield break;
            }
            if (s == Style.Light)
            {
                var lr = Line("FxBeam", ColorOf(s), 0.28f * size);
                lr.positionCount = 2;
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
                {
                    lr.SetPosition(0, from + Vector3.up * 0.3f);
                    lr.SetPosition(1, Vector3.Lerp(from, to, Mathf.Clamp01(t * 2.5f)) + Vector3.up * 0.15f);
                    var c = ColorOf(s);
                    lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 1 - t * 0.6f);
                    yield return null;
                }
                Destroy(lr.gameObject);
                Impact(to, s, size);
                yield break;
            }
            Sfx.Play("whoosh", 0.45f);
            var core = new GameObject("FxOrb");
            var sr = core.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.Glow;
            float baseScale = 1.1f * size / Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            core.transform.localScale = Vector3.one * baseScale;
            var col0 = ColorOf(s);
            sr.color = new Color(col0.r, col0.g, col0.b, 1f);
            float arc = Vector3.Distance(from, to) * 0.25f;
            float trailT = 0;
            for (float t = 0; t < 1f; t += Time.deltaTime / time)
            {
                var p = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc + 0.35f);
                core.transform.position = p;
                if (Cam != null) core.transform.rotation = Cam.transform.rotation;
                if (s == Style.Rainbow) sr.color = ColorOf(s);
                trailT += Time.deltaTime;
                if (trailT > 0.03f)
                {
                    trailT = 0;
                    var c = ColorOf(s);
                    Sparks(p, c, Color.Lerp(c, Color.white, 0.4f), s == Style.Earth ? 2 : 4, Vector3.zero, 0.25f, 0.18f * size, 0.35f, s == Style.Water || s == Style.Earth ? 0.4f : -0.1f, 0.05f);
                }
                yield return null;
            }
            Destroy(core);
            Impact(to, s, size);
        }

        // ══════════════════════════ 화면 ══════════════════════════

        public void Tint(Color c, float fadeIn, float hold, float fadeOut) => StartCoroutine(TintRoutine(tint, c, fadeIn, hold, fadeOut));

        public void Flash(Color c, float dur = 0.3f) => StartCoroutine(TintRoutine(tint, c, 0.04f, 0.02f, dur));

        IEnumerator TintRoutine(Image img, Color c, float fadeIn, float hold, float fadeOut)
        {
            if (img == null) yield break;
            for (float t = 0; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, fadeIn)) { img.color = new Color(c.r, c.g, c.b, c.a * t); yield return null; }
            img.color = c;
            yield return new WaitForSeconds(hold);
            for (float t = 0; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, fadeOut)) { img.color = new Color(c.r, c.g, c.b, c.a * (1 - t)); yield return null; }
            img.color = Color.clear;
        }

        /// <summary>개기일식: 화면이 어두워지고 검은 원판이 빛 고리(코로나)를 가린다</summary>
        public IEnumerator Eclipse()
        {
            Sfx.Play("charge4", 0.8f);
            StartCoroutine(TintRoutine(tint, new Color(0, 0, 0.02f, 0.9f), 0.45f, 1.0f, 0.9f));
            StartCoroutine(TintRoutine(corona, new Color(1f, 0.85f, 0.55f, 0.95f), 0.5f, 0.95f, 0.8f));
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
            {
                disk.color = new Color(0, 0, 0, Mathf.Min(1, t * 1.5f));
                disk.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-260, 0, 1 - (1 - t) * (1 - t)), 120);
                yield return null;
            }
            disk.rectTransform.anchoredPosition = new Vector2(0, 120);
            board.Shake(0.15f);
            yield return new WaitForSeconds(0.9f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
            {
                disk.color = new Color(0, 0, 0, 1 - t);
                yield return null;
            }
            disk.color = Color.clear;
        }

        // ══════════════════════════ 의미 단위 (게임 처리에서 호출) ══════════════════════════

        public void Heal(PlayerState p)
        {
            var pos = HeroPos(p);
            Ring(pos, new Color(0.5f, 1f, 0.6f), 2.2f, 0.8f);
            Sparks(pos, new Color(0.6f, 1f, 0.6f), new Color(1f, 0.95f, 0.55f), 36, Vector3.up * 1.6f, 0.6f, 0.13f, 1.2f, -0.05f, 0.45f);
        }

        public void Draw(PlayerState p, int n)
        {
            var pos = DeckPos(p);
            Sparks(pos, new Color(0.55f, 0.8f, 1f), Color.white, 10 + 6 * n, Vector3.up * 1.2f, 0.5f, 0.1f, 0.8f, -0.1f, 0.3f);
        }

        /// <summary>효과로 몬스터를 파괴하는 일격 (속성별)</summary>
        public IEnumerator Strike(CardInstance target, Style s)
        {
            var pos = PosOf(target);
            var c = ColorOf(s);
            switch (s)
            {
                case Style.Thunder:
                    Sfx.Play("boom", 0.5f);
                    Flash(new Color(1f, 1f, 0.9f, 0.35f), 0.25f);
                    Bolt(pos + new Vector3(Random.Range(-0.8f, 0.8f), 6f, 1.5f), pos, c, 0.45f, 0.2f);
                    break;
                case Style.Fire:
                    Pillar(pos, new Color(1f, 0.45f, 0.1f), 3f, 1.1f, 0.8f);
                    Sparks(pos, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.2f, 0.05f), 40, Vector3.up * 3f, 0.8f, 0.18f, 0.9f, -0.2f, 0.35f);
                    break;
                case Style.Water:
                    Pillar(pos, new Color(0.3f, 0.65f, 1f), 2.5f, 1.1f, 0.8f);
                    Sparks(pos, Color.white, new Color(0.3f, 0.6f, 1f), 40, Vector3.up * 3.2f, 1.2f, 0.12f, 1f, 1.2f, 0.3f);
                    break;
                case Style.Wind:
                    Spiral(pos, c, 1.1f, 0.8f);
                    Sparks(pos, c, Color.white, 30, Vector3.up * 2f, 1.5f, 0.1f, 0.8f, 0, 0.4f);
                    break;
                case Style.Earth:
                    Sparks(pos + Vector3.up * 3f, new Color(0.6f, 0.45f, 0.3f), new Color(0.35f, 0.25f, 0.15f), 24, Vector3.down * 3f, 0.6f, 0.28f, 0.8f, 1.5f, 0.6f);
                    board.Shake(0.2f);
                    break;
                case Style.Light:
                    Pillar(pos, new Color(1f, 0.95f, 0.7f), 6f, 1.2f, 0.9f);
                    break;
                case Style.Dark:
                    Slash(pos, c, 1.1f);
                    Sparks(pos, new Color(0.6f, 0.2f, 0.9f), new Color(0.15f, 0.02f, 0.25f), 30, Vector3.up * 0.4f, 0.8f, 0.25f, 1.2f, -0.05f, 0.5f);
                    break;
                default:
                    Slash(pos, s == Style.Rainbow ? Color.HSVToRGB(Random.value, 0.6f, 1f) : Color.white, 1.1f);
                    break;
            }
            yield return new WaitForSeconds(0.28f);
        }

        /// <summary>파괴될 때 (항상) — 파편 + 연기</summary>
        public void Shatter(CardInstance c)
        {
            var pos = PosOf(c);
            var col = CardData.ElementColor(c.data.element);
            Sparks(pos, col, Color.white, 26, Vector3.up * 1.8f, 2.2f, 0.12f, 0.8f, 1f, 0.35f);
            Sparks(pos, new Color(0.3f, 0.3f, 0.32f), new Color(0.1f, 0.1f, 0.1f), 14, Vector3.up * 0.5f, 0.5f, 0.45f, 1.2f, -0.05f, 0.35f);
        }

        public void AtkChange(CardInstance c, int value)
        {
            if (c == null || value == 0) return;
            var pos = PosOf(c);
            if (value > 0) Sparks(pos, new Color(1f, 0.6f, 0.2f), new Color(1f, 0.95f, 0.4f), 16, Vector3.up * 1.6f, 0.2f, 0.12f, 0.8f, 0, 0.35f);
            else Sparks(pos + Vector3.up * 1.2f, new Color(0.6f, 0.3f, 0.9f), new Color(0.3f, 0.1f, 0.5f), 16, Vector3.down * 1.4f, 0.2f, 0.14f, 0.8f, 0, 0.35f);
        }

        public void HpUp(CardInstance c) => Bubble(PosOf(c) + Vector3.up * 0.1f, new Color(0.4f, 1f, 0.6f), 0.8f);

        public void Guard(CardInstance c) => Bubble(PosOf(c) + Vector3.up * 0.1f, new Color(0.5f, 0.85f, 1f), 0.9f);

        public void Wound(CardInstance c) => Slash(PosOf(c), new Color(0.75f, 0.1f, 0.25f), 0.8f, 0.3f);

        public void Stun(CardInstance c)
        {
            var pos = PosOf(c);
            Ring(pos, new Color(0.7f, 0.5f, 1f), 1.6f, 0.6f, true);
            Sparks(pos + Vector3.up * 0.6f, new Color(1f, 0.95f, 0.5f), new Color(0.8f, 0.6f, 1f), 14, Vector3.zero, 0.9f, 0.12f, 1f, -0.05f, 0.4f);
        }

        public void Taunt(CardInstance c)
        {
            var pos = PosOf(c);
            Ring(pos, new Color(1f, 0.25f, 0.2f), 2.6f, 0.7f);
            Ring(pos, new Color(1f, 0.55f, 0.2f), 1.8f, 0.9f);
            Bubble(pos + Vector3.up * 0.1f, new Color(1f, 0.3f, 0.2f), 0.8f);
        }

        public void Haste(CardInstance c) => Spiral(PosOf(c), ColorOf(Style.Wind), 0.8f, 0.8f);

        public void Return(CardInstance c, bool toDeck)
        {
            var pos = PosOf(c);
            Ring(pos, toDeck ? Color.white : new Color(0.5f, 0.75f, 1f), 1.8f, 0.6f, true);
            Spiral(pos, toDeck ? Color.white : new Color(0.5f, 0.75f, 1f), 0.7f, 0.6f);
        }

        public void SpecialSummon(CardInstance c)
        {
            var pos = PosOf(c);
            var col = CardData.ElementColor(c.data.element);
            Pillar(pos, col, 4f, 1f, 0.9f);
            Ring(pos, col, 2.4f, 0.7f);
        }

        public void GraveRise(PlayerState p) => Sparks(GravePos(p), new Color(0.8f, 0.7f, 1f), Color.white, 22, Vector3.up * 1.8f, 0.4f, 0.12f, 1f, -0.1f, 0.3f);

        public void FieldChange(CardData field)
        {
            var c = CardData.ElementColor(field.element);
            Flash(new Color(c.r, c.g, c.b, 0.35f), 0.5f);
            Ring(BoardView.CenterPos, c, 9f, 1.1f);
            Ring(BoardView.CenterPos, Color.white, 6f, 0.8f);
        }

        /// <summary>상태 효과 시각화</summary>
        public void Status(GameManager g, Status s)
        {
            var mine = s.caster;
            switch (s.kind)
            {
                case StatusKind.AtkBonus:
                    foreach (var p in g.players)
                    {
                        if (p == null || (s.target != null && s.target != p)) continue;
                        foreach (var m in p.Monsters()) AtkChange(m, s.value);
                        Ring(RowCenter(p), s.value >= 0 ? new Color(1f, 0.55f, 0.2f) : new Color(0.6f, 0.3f, 0.9f), 6.5f, 0.9f);
                    }
                    break;
                case StatusKind.FaceDownSummonOnly:
                    break;   // 개기일식 연출은 Signature 에서
                case StatusKind.RevealFaceDown:
                    if (s.target != null)
                        foreach (var c in s.target.slots)
                            if (c != null && c.faceDown) Pillar(PosOf(c), new Color(1f, 0.6f, 0.2f), 2.5f, 0.7f, 1f);
                    break;
                case StatusKind.AllowFaceDown:
                    if (mine != null) Sparks(RowCenter(mine), new Color(0.35f, 0.15f, 0.5f), new Color(0.1f, 0.05f, 0.15f), 50, Vector3.up * 0.4f, 0.4f, 0.4f, 1.6f, -0.02f, 2.5f);
                    break;
                case StatusKind.FreeTarget:
                    if (mine != null)
                        foreach (var m in mine.opponent.Monsters()) Reticle(PosOf(m), new Color(1f, 0.25f, 0.2f), 1.3f);
                    break;
            }
        }

        // ══════════════════════════ 카드 고유 연출 (발동 순간) ══════════════════════════

        public IEnumerator Signature(GameManager g, CardInstance c)
        {
            if (c == null) yield break;
            var me = c.owner;
            var op = me?.opponent;
            var s = StyleOf(c);
            var col = ColorOf(s);
            var src = PosOf(c);
            switch (c.data.id)
            {
                case "S001":   // 봉화: 내 쪽에서 불기둥, 상대 쪽 뒷면 카드가 드러난다
                    Pillar(HeroPos(me), new Color(1f, 0.55f, 0.15f), 6f, 1.3f, 1.4f);
                    Sparks(HeroPos(me), new Color(1f, 0.8f, 0.3f), new Color(1f, 0.3f, 0.05f), 60, Vector3.up * 4f, 0.8f, 0.14f, 1.4f, -0.1f, 0.4f);
                    Flash(new Color(1f, 0.55f, 0.2f, 0.2f), 0.5f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S002":   // 포기란없다: 묘지에서 황금 불꽃
                    Pillar(GravePos(me), new Color(1f, 0.85f, 0.4f), 4f, 1f, 1f);
                    Ring(GravePos(me), new Color(1f, 0.85f, 0.4f), 2.6f, 0.8f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case "S003":   // 개기일식
                    yield return Eclipse();
                    break;
                case "S004":   // 번개 일격: 먹구름 + 섬광
                    Tint(new Color(0.05f, 0.05f, 0.15f, 0.55f), 0.15f, 0.25f, 0.4f);
                    yield return new WaitForSeconds(0.2f);
                    Flash(new Color(1f, 1f, 0.9f, 0.4f), 0.2f);
                    break;
                case "S005":   // 전투의 함성: 충격파
                    Sfx.Play("boom", 0.4f);
                    for (int i = 0; i < 3; i++) { Ring(RowCenter(me), new Color(1f, 0.35f, 0.2f), 4f + i * 2f, 0.7f); yield return new WaitForSeconds(0.12f); }
                    board.Shake(0.25f);
                    break;
                case "S006":   // 화염구: 손에 불꽃을 모은다
                    Sparks(HeroPos(me), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.3f, 0.05f), 40, Vector3.zero, 1.2f, 0.16f, 0.5f, 0, 1.2f);
                    Ring(HeroPos(me), new Color(1f, 0.4f, 0.1f), 2.2f, 0.4f, true);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case "S007":   // 지혜의 샘: 덱에서 솟는 물
                    Sparks(DeckPos(me), new Color(0.6f, 0.85f, 1f), new Color(0.2f, 0.5f, 1f), 60, Vector3.up * 3.5f, 0.8f, 0.12f, 1.2f, 1.1f, 0.3f);
                    Ring(DeckPos(me), new Color(0.4f, 0.75f, 1f), 2.2f, 0.8f);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case "S008":   // 질풍 가속: 바람이 휩쓴다
                    Sfx.Play("whoosh");
                    Sparks(RowCenter(me) + Vector3.left * 4f, ColorOf(Style.Wind), Color.white, 70, Vector3.right * 9f, 0.8f, 0.1f, 0.9f, 0, 0.6f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S009":   // 치유의 빛
                    Pillar(HeroPos(me), new Color(1f, 0.95f, 0.65f), 7f, 1.4f, 1.2f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S010":   // 반격의 결의
                    Ring(RowCenter(me), new Color(1f, 0.3f, 0.15f), 7f, 0.8f);
                    Flash(new Color(1f, 0.3f, 0.15f, 0.25f), 0.4f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S011":   // 저격 조준 (조준선은 상태 효과에서)
                    Sfx.Play("charge3", 0.6f);
                    break;
                case "S012":   // 도발의 깃발
                    Ring(RowCenter(me), new Color(1f, 0.2f, 0.15f), 6f, 0.7f);
                    Flash(new Color(0.8f, 0.1f, 0.05f, 0.2f), 0.35f);
                    break;

                // ── 신화 마법 ──
                case "S101":   // 저스티아: 심판의 빛
                    Flash(new Color(1f, 0.95f, 0.7f, 0.5f), 0.6f);
                    if (op != null) foreach (var t in op.slots) if (t != null) Pillar(PosOf(t), new Color(1f, 0.95f, 0.7f), 7f, 0.9f, 1.1f);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S102":   // 블래스트 플레어: 유성우
                    Flash(new Color(1f, 0.45f, 0.1f, 0.35f), 0.5f);
                    if (op != null)
                        foreach (var t in op.Monsters())
                        {
                            StartCoroutine(Projectile(PosOf(t) + new Vector3(-1.5f, 5f, 2f), PosOf(t), Style.Fire, 1.4f, 0.35f));
                            yield return new WaitForSeconds(0.1f);
                        }
                    yield return new WaitForSeconds(0.45f);
                    break;
                case "S103":   // 게헨나 플레임: 지옥불
                    Tint(new Color(0.35f, 0.02f, 0f, 0.5f), 0.2f, 0.5f, 0.6f);
                    if (op != null) { int k = 0; foreach (var t in op.Monsters()) { if (k++ >= 2) break; Pillar(PosOf(t), new Color(0.9f, 0.15f, 0.05f), 3.5f, 1.2f, 1.2f); } }
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S104":   // 데스 사이즈: 낫 베기
                    Tint(new Color(0.15f, 0f, 0.25f, 0.5f), 0.15f, 0.3f, 0.5f);
                    if (op != null) { int k = 0; foreach (var t in op.Monsters()) { if (k++ >= 2) break; Slash(PosOf(t), new Color(0.75f, 0.3f, 1f), 1.4f, 0.4f); } }
                    yield return new WaitForSeconds(0.45f);
                    break;
                case "S105":   // 세인트 리벤저: 성검
                    Flash(new Color(1f, 0.95f, 0.75f, 0.4f), 0.5f);
                    Pillar(BoardView.CenterPos, new Color(1f, 0.95f, 0.7f), 8f, 1.6f, 1f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case "S106":   // 영광의 최후
                    Sfx.Play("boom", 0.6f);
                    for (int i = 0; i < 3; i++) Ring(BoardView.CenterPos, new Color(1f, 0.8f, 0.3f), 5f + i * 3f, 0.8f + i * 0.15f);
                    board.Shake(0.35f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case "S107":   // 천둥 포화
                    Tint(new Color(0.05f, 0.05f, 0.15f, 0.5f), 0.1f, 0.4f, 0.4f);
                    if (op != null)
                    {
                        var mine = me.Monsters(); var theirs = op.Monsters();
                        for (int i = 0; i < 2 && i < mine.Count && i < theirs.Count; i++) { Bolt(PosOf(mine[i]) + Vector3.up * 0.3f, PosOf(theirs[i]), col, 0.5f, 0.2f); Impact(PosOf(theirs[i]), Style.Thunder); }
                    }
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S108":   // 세이렌의 눈물: 물결
                    Tint(new Color(0f, 0.15f, 0.4f, 0.45f), 0.25f, 0.5f, 0.6f);
                    if (op != null) { Ring(RowCenter(op), new Color(0.4f, 0.75f, 1f), 7f, 1f); foreach (var t in op.Monsters()) Sparks(PosOf(t) + Vector3.up * 2f, Color.white, new Color(0.4f, 0.7f, 1f), 14, Vector3.down * 2f, 0.3f, 0.1f, 0.8f, 1f, 0.3f); }
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S109":   // 루미너스 보이드: 화이트 홀
                    Flash(new Color(1f, 1f, 1f, 0.75f), 0.9f);
                    Ring(BoardView.CenterPos, Color.white, 10f, 1f, true);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S110":   // 테라 사이클론: 왼쪽부터 휩쓰는 회오리
                    Sfx.Play("whoosh");
                    if (op != null)
                    {
                        for (int i = 0; i < PlayerState.SlotCount; i++)
                        {
                            Spiral(board.SlotPos(op, i), ColorOf(Style.Wind), 1f, 0.6f);
                            yield return new WaitForSeconds(0.05f);
                        }
                    }
                    break;
                case "S111":   // 종말의 마차
                    Tint(new Color(0.1f, 0f, 0.05f, 0.65f), 0.3f, 0.5f, 0.7f);
                    board.Shake(0.5f);
                    if (op != null) Impact(HeroPos(op), Style.Dark, 2f);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S112":   // 소울 레스큐: 영혼이 떠오른다
                    foreach (var p in g.players) if (p != null) Sparks(GravePos(p), new Color(0.7f, 0.9f, 1f), Color.white, 26, Vector3.up * 1.6f, 0.4f, 0.16f, 1.6f, -0.08f, 0.3f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S201":   // 신비의 심해: 물 속
                    Tint(new Color(0f, 0.18f, 0.45f, 0.55f), 0.4f, 0.7f, 0.8f);
                    Sparks(RowCenter(me), new Color(0.8f, 0.95f, 1f), new Color(0.3f, 0.6f, 1f), 70, Vector3.up * 1.5f, 0.5f, 0.12f, 2f, -0.15f, 3f);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S202":   // 깊은 암야: 어둠이 내려앉는다
                    Tint(new Color(0.06f, 0f, 0.12f, 0.8f), 0.4f, 0.8f, 0.9f);
                    if (op != null) foreach (var t in op.Monsters()) Sparks(PosOf(t), new Color(0.35f, 0.1f, 0.5f), Color.black, 20, Vector3.up * 0.3f, 0.4f, 0.4f, 1.6f, -0.02f, 0.5f);
                    yield return new WaitForSeconds(0.7f);
                    break;
                case "S203":   // 들풀의 군세: 잎사귀 폭풍
                    Sparks(new Vector3(0, 1.2f, -3f), new Color(0.4f, 1f, 0.4f), new Color(0.15f, 0.6f, 0.2f), 90, Vector3.forward * 4f + Vector3.up, 2f, 0.14f, 1.4f, 0.3f, 1.5f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S204":   // 대지의 분노: 지진
                    Sfx.Play("boom", 0.7f);
                    board.Shake(0.6f);
                    if (op != null) for (int i = 0; i < PlayerState.SlotCount; i++) Sparks(board.SlotPos(op, i), new Color(0.6f, 0.45f, 0.3f), new Color(0.3f, 0.2f, 0.1f), 12, Vector3.up * 3f, 1f, 0.22f, 0.9f, 1.5f, 0.3f);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S205":   // 인간의 긍지: 묘지에서 영웅들이 일어선다
                    Pillar(GravePos(me), new Color(1f, 0.85f, 0.45f), 6f, 1.2f, 1.1f);
                    Flash(new Color(1f, 0.85f, 0.45f, 0.3f), 0.5f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case "S206":   // 화염 군단: 불의 파도
                    Flash(new Color(1f, 0.4f, 0.1f, 0.35f), 0.5f);
                    if (op != null)
                        for (int i = 0; i < PlayerState.SlotCount; i++)
                        {
                            Sparks(board.SlotPos(op, i), new Color(1f, 0.8f, 0.3f), new Color(1f, 0.2f, 0.05f), 16, Vector3.up * 2.5f, 0.6f, 0.2f, 0.8f, -0.2f, 0.4f);
                            yield return new WaitForSeconds(0.05f);
                        }
                    break;
                case "S207":   // 균형의 저울: 빛과 어둠
                    Flash(new Color(1f, 1f, 1f, 0.5f), 0.3f);
                    yield return new WaitForSeconds(0.25f);
                    Flash(new Color(0f, 0f, 0f, 0.6f), 0.4f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S208":   // 앱솔루트 스펠: 무지개 고리
                    for (int i = 0; i < 6; i++) Ring(BoardView.CenterPos, Color.HSVToRGB(i / 6f, 0.7f, 1f), 3f + i * 1.3f, 0.7f + i * 0.08f);
                    Flash(new Color(1f, 1f, 1f, 0.35f), 0.4f);
                    yield return new WaitForSeconds(0.5f);
                    break;

                default:
                    if (c.data.type == CardType.Hero)
                    {
                        // 영웅 능력: 영웅 위로 속성 빛기둥 + 고리
                        Pillar(src, col, 6f, 1.3f, 1.1f);
                        Ring(src, col, 3.2f, 0.8f);
                        Flash(new Color(col.r, col.g, col.b, 0.25f), 0.5f);
                        yield return new WaitForSeconds(0.35f);
                    }
                    else if (c.data.rarity == Rarity.Unique && c.IsMonster)
                    {
                        // 신화 몬스터 강림
                        Sfx.Play("charge5", 0.6f);
                        Pillar(src, col, 7f, 1.5f, 1.3f);
                        Ring(src, col, 4f, 0.9f);
                        Ring(src, Color.white, 2.5f, 0.7f);
                        Flash(new Color(col.r, col.g, col.b, 0.3f), 0.6f);
                        yield return new WaitForSeconds(0.45f);
                    }
                    else if (c.IsMagic)
                    {
                        // 그 밖의 마법: 속성 색 섬광 + 마법진
                        Ring(src, col, 3f, 0.7f);
                        Ring(src, Color.white, 2f, 0.5f, true);
                        Flash(new Color(col.r, col.g, col.b, 0.18f), 0.35f);
                        yield return new WaitForSeconds(0.2f);
                    }
                    break;
            }
        }
    }
}
