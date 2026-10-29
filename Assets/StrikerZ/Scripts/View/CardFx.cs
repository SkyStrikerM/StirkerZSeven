using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 카드 효과 연출. VARCO 3D 로 만든 이펙트 텍스처(Resources/StrikerZ/Textures/Fx, Tools/fx/process_fx.py)를
    /// 바닥 평면·빌보드·파티클로 띄운다 — 속성별 폭발(burst)·충격파 고리(ring)·입자(mote)·마법진(glyph) 8종 + 공용 13종.
    /// GameManager 의 기본 처리(데미지/회복/파괴/상태 등)에서 자동으로 호출되고,
    /// 마법·영웅·신화 카드는 발동 순간 Signature() 로 카드 고유 연출을 한 번 더 보여 준다.
    /// </summary>
    public class CardFx : MonoBehaviour
    {
        public static CardFx I { get; private set; }

        public enum Style { Neutral, Fire, Water, Wind, Earth, Thunder, Light, Dark, Rainbow }

        BoardView board;
        Image tint, disk, corona;
        /// <summary>보드 카메라 거리에서 잘 보이도록 파티클 크기 배율</summary>
        const float SizeK = 2.4f;
        /// <summary>입자 텍스처는 그림이 프레임 가운데 작게 들어 있어 더 키운다</summary>
        const float MoteK = 2.1f;

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
            corona = UIKit.Img(root, "Corona", Color.clear, Sprite("eclipse") ?? GameAssets.Glow);
            corona.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(720, 720));
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
            return StyleOf(c.data.element);
        }

        public static Style StyleOf(Element e)
        {
            switch (e)
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

        /// <summary>텍스처 이름 꼬리 (무지개는 무속성 그림을 무지개 색으로 물들인다)</summary>
        static string Suffix(Style s) => s == Style.Rainbow ? "neutral" : s.ToString().ToLower();

        /// <summary>속성 텍스처 색: 그림 자체가 속성 색이라 흰색, 무지개만 색을 돌린다</summary>
        static Color StyleTint(Style s) => s == Style.Rainbow ? ColorOf(s) : Color.white;

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

        // ══════════════════════════ 재료 (VARCO 텍스처) ══════════════════════════

        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>Resources/StrikerZ/Textures/Fx/{name}.png — 없으면 null</summary>
        public static Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (texCache.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>(GameAssets.Root + "Textures/Fx/" + name);
            if (t != null) t.wrapMode = TextureWrapMode.Clamp;
            texCache[name] = t;
            return t;
        }

        /// <summary>속성별 텍스처 (burst·ring·mote·glyph) — 그 속성 그림이 없으면 무속성 그림</summary>
        public static Texture2D Tex(string kind, Style s) => Tex(kind + "_" + Suffix(s)) ?? Tex(kind + "_neutral");

        /// <summary>파티클·라인용 가산 재질 (텍스처별 공유, 색은 파티클/라인 쪽에서)</summary>
        static Material Mat(string name)
        {
            if (matCache.TryGetValue(name, out var m) && m != null) return m;
            var t = Tex(name) ?? GameAssets.GlowTexture;
            m = GameAssets.UnlitMat(Color.white, t, true);
            GameAssets.MakeTransparent(m, true);
            matCache[name] = m;
            return m;
        }

        static Material Mat(string kind, Style s) => Mat(Tex(kind + "_" + Suffix(s)) != null ? kind + "_" + Suffix(s) : kind + "_neutral");

        static Sprite Sprite(string name)
        {
            if (spriteCache.TryGetValue(name, out var s)) return s;
            var t = Tex(name);
            s = t == null ? null : UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[name] = s;
            return s;
        }

        /// <summary>한 번 쓰고 버리는 평면 (재질은 개별 — 색·알파를 프레임마다 바꾼다)</summary>
        static GameObject Quad(string name, string tex, Color c)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            var m = GameAssets.UnlitMat(c, Tex(tex) ?? GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(m, true);
            q.GetComponent<Renderer>().sharedMaterial = m;
            return q;
        }

        static void SetColor(GameObject q, Color c)
        {
            var m = q.GetComponent<Renderer>().sharedMaterial;
            m.color = c; m.SetColor("_BaseColor", c);
        }

        static void Kill(GameObject q)
        {
            if (q == null) return;
            Destroy(q.GetComponent<Renderer>().sharedMaterial);
            Destroy(q);
        }

        /// <summary>처음 15% 에 켜지고 마지막 45% 에 꺼지는 기본 알파 곡선</summary>
        static float Fade(float t) => t < 0.15f ? t / 0.15f : t > 0.55f ? 1f - (t - 0.55f) / 0.45f : 1f;

        /// <summary>
        /// 텍스처 한 장 연출. flat = 바닥에 눕힘(아니면 카메라를 본다), spin = 초당 회전(°), rise = 떠오르는 높이,
        /// alpha = 알파 곡선(null 이면 Fade), aspect = 가로/세로
        /// </summary>
        IEnumerator QuadRoutine(string tex, Vector3 pos, Color c, float size0, float size1, float dur, bool flat,
                                float spin = 0f, float rise = 0f, System.Func<float, float> alpha = null, float rot0 = float.NaN, float aspect = 1f, float peak = 1f)
        {
            var q = Quad("Fx_" + tex, tex, c);
            float r0 = float.IsNaN(rot0) ? Random.Range(0f, 360f) : rot0;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float e = 1 - (1 - t) * (1 - t);
                float s = Mathf.Lerp(size0, size1, e);
                q.transform.localScale = new Vector3(s * aspect, s, 1);
                q.transform.position = pos + Vector3.up * rise * t;
                float rot = r0 + spin * t * dur;
                if (flat) q.transform.rotation = Quaternion.Euler(90, rot, 0);
                else if (Cam != null) q.transform.rotation = Cam.transform.rotation * Quaternion.Euler(0, 0, rot);
                float a = (alpha != null ? alpha(t) : Fade(t)) * peak;
                SetColor(q, new Color(c.r, c.g, c.b, a));
                yield return null;
            }
            Kill(q);
        }

        // ══════════════════════════ 파티클 ══════════════════════════

        /// <summary>
        /// 범용 파티클. tex = 입자 텍스처 이름(null 이면 무속성 입자). vel = 기본 속도(방향 포함), spread = 무작위 속도, gravity = 낙하(음수면 상승).
        /// </summary>
        public void Sparks(Vector3 pos, Color a, Color b, int n, Vector3 vel, float spread, float size, float life, float gravity = 0f, float radius = 0.15f, string tex = null)
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
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat(tex ?? "mote_neutral");
            ps.Play();
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                ep.position = pos + Random.insideUnitSphere * radius;
                ep.velocity = vel + Random.insideUnitSphere * spread;
                ep.startColor = Color.Lerp(a, b, Random.value);
                ep.startSize = size * SizeK * MoteK * Random.Range(0.6f, 1.3f);
                ep.startLifetime = life * Random.Range(0.6f, 1.1f);
                ep.rotation = Random.Range(0f, 360f);
                ps.Emit(ep, 1);
            }
            Destroy(go, life * 1.2f + 0.3f);
        }

        /// <summary>속성 입자 (그 속성 그림 그대로, 흰색~속성색 사이로 살짝 물들인다)</summary>
        public void Motes(Vector3 pos, Style s, int n, Vector3 vel, float spread, float size, float life, float gravity = 0f, float radius = 0.15f)
            => Sparks(pos, Color.white, Color.Lerp(Color.white, ColorOf(s), 0.5f), n, vel, spread, size, life, gravity, radius, Tex("mote_" + Suffix(s)) != null ? "mote_" + Suffix(s) : "mote_neutral");

        /// <summary>속성 색 폭발 (충돌·파괴 지점): 폭발 그림 + 충격파 고리 + 속성 입자</summary>
        public void Impact(Vector3 pos, Style s, float scale = 1f)
        {
            Burst(pos, s, 2.6f * scale, 0.55f);
            Ring(pos, s, 2.4f * scale, 0.5f);
            switch (s)
            {
                case Style.Fire: Motes(pos, s, (int)(26 * scale), Vector3.up * 1.2f, 2.2f * scale, 0.16f, 0.8f, -0.15f); break;
                case Style.Water: Motes(pos, s, (int)(26 * scale), Vector3.up * 2f, 1.8f * scale, 0.12f, 0.9f, 0.6f); break;
                case Style.Wind: Spiral(pos, ColorOf(s), 0.9f * scale, 0.8f); Motes(pos, s, (int)(14 * scale), Vector3.up * 1.5f, 1.6f * scale, 0.11f, 0.7f); break;
                case Style.Earth: Motes(pos, s, (int)(20 * scale), Vector3.up * 2.2f, 1.6f * scale, 0.2f, 0.9f, 1.2f); board.Shake(0.08f * scale); break;
                case Style.Thunder: Motes(pos, s, (int)(22 * scale), Vector3.zero, 3.2f * scale, 0.09f, 0.45f); Flash(new Color(1f, 1f, 0.85f, 0.12f * scale), 0.15f); break;
                case Style.Light: Motes(pos, s, (int)(20 * scale), Vector3.up * 1.4f, 1.4f * scale, 0.12f, 1f, -0.2f); break;
                case Style.Dark: Motes(pos, s, (int)(22 * scale), Vector3.up * 0.6f, 1.6f * scale, 0.2f, 1f, -0.1f); break;
                case Style.Rainbow:
                    for (int i = 0; i < 6; i++)
                        Sparks(pos, Color.HSVToRGB(i / 6f, 0.75f, 1f), Color.HSVToRGB(i / 6f + 0.08f, 0.5f, 1f), (int)(5 * scale), Vector3.up, 2.2f * scale, 0.13f, 0.9f, 0f, 0.15f, "star");
                    break;
                default: Motes(pos, s, (int)(18 * scale), Vector3.up, 2f * scale, 0.12f, 0.7f); break;
            }
        }

        // ══════════════════════════ 형태 ══════════════════════════

        /// <summary>폭발 그림: 바닥에 퍼지는 큰 그림 + 세워진 작은 그림 (부피감)</summary>
        public void Burst(Vector3 pos, Style s, float size = 2.4f, float dur = 0.5f)
        {
            var tex = Tex("burst_" + Suffix(s)) != null ? "burst_" + Suffix(s) : "burst_neutral";
            var c = StyleTint(s);
            StartCoroutine(QuadRoutine(tex, pos + Vector3.up * 0.06f, c, size * 0.35f, size, dur, true, Random.Range(-60f, 60f)));
            StartCoroutine(QuadRoutine(tex, pos + Vector3.up * 0.35f, c, size * 0.25f, size * 0.75f, dur * 0.9f, false, Random.Range(-40f, 40f), 0.35f, null, float.NaN, 1f, 0.85f));
        }

        /// <summary>색만 주는 폭발 (무속성 그림을 물들인다)</summary>
        public void Burst(Vector3 pos, Color c, float size = 2.4f, float dur = 0.5f)
        {
            StartCoroutine(QuadRoutine("burst_neutral", pos + Vector3.up * 0.06f, c, size * 0.35f, size, dur, true, Random.Range(-60f, 60f)));
            StartCoroutine(QuadRoutine("burst_neutral", pos + Vector3.up * 0.35f, c, size * 0.25f, size * 0.75f, dur * 0.9f, false, Random.Range(-40f, 40f), 0.35f, null, float.NaN, 1f, 0.85f));
        }

        /// <summary>바닥에 퍼지는 고리 (in=true 면 안쪽으로 모인다) — 무속성 고리를 색으로 물들인다</summary>
        public void Ring(Vector3 pos, Color c, float size, float dur = 0.7f, bool inward = false)
            => StartCoroutine(RingRoutine("ring_neutral", pos, c, size, dur, inward));

        /// <summary>속성 고리 (그 속성 그림)</summary>
        public void Ring(Vector3 pos, Style s, float size, float dur = 0.7f, bool inward = false)
            => StartCoroutine(RingRoutine(Tex("ring_" + Suffix(s)) != null ? "ring_" + Suffix(s) : "ring_neutral", pos, StyleTint(s), size, dur, inward));

        IEnumerator RingRoutine(string tex, Vector3 pos, Color c, float size, float dur, bool inward)
        {
            float s0 = inward ? size : size * 0.15f, s1 = inward ? size * 0.15f : size;
            yield return QuadRoutine(tex, pos + Vector3.up * 0.06f, c, s0, s1, dur, true, inward ? -90f : 50f, 0f,
                t => (inward ? Mathf.Min(1f, 1.4f - t) : 1f - t) * 1.3f, float.NaN, 1f, 1f);
        }

        /// <summary>마법진: 바닥에서 돌며 커지는 룬 원</summary>
        public void Glyph(Vector3 pos, Style s, float size = 2.2f, float dur = 0.9f) => Glyph(pos, s, StyleTint(s), size, dur);

        public void Glyph(Vector3 pos, Style s, Color tint, float size = 2.2f, float dur = 0.9f)
        {
            var tex = Tex("glyph_" + Suffix(s)) != null ? "glyph_" + Suffix(s) : "glyph_neutral";
            StartCoroutine(QuadRoutine(tex, pos + Vector3.up * 0.07f, tint, size * 0.55f, size, dur, true, 70f, 0f, t => t < 0.12f ? t / 0.12f : t > 0.6f ? 1f - (t - 0.6f) / 0.4f : 1f));
        }

        /// <summary>빛기둥 (아래에서 위로 자라는 세로 그림, 카메라를 향해 서 있다)</summary>
        public void Pillar(Vector3 pos, Color c, float height = 4f, float width = 0.9f, float dur = 1f)
            => StartCoroutine(PillarRoutine(pos, c, height, width, dur));

        IEnumerator PillarRoutine(Vector3 pos, Color c, float height, float width, float dur)
        {
            var q = Quad("FxPillar", "pillar", c);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float grow = Mathf.Clamp01(t * 4f);
                float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                float h = height * grow;
                float w = width * 1.6f * (0.7f + 0.3f * Mathf.Sin(t * Mathf.PI));
                q.transform.localScale = new Vector3(w, h, 1);
                q.transform.position = pos + Vector3.up * (h * 0.5f - 0.1f);
                if (Cam != null)
                {
                    var f = Cam.transform.position - pos; f.y = 0;
                    if (f.sqrMagnitude > 0.001f) q.transform.rotation = Quaternion.LookRotation(-f.normalized, Vector3.up);
                }
                SetColor(q, new Color(c.r, c.g, c.b, fade));
                yield return null;
            }
            Kill(q);
            Sparks(pos, c, Color.white, Mathf.RoundToInt(10 * width), Vector3.up * height * 0.8f, 0.6f, 0.08f, 0.9f, -0.1f, width * 0.3f, "star");
        }

        LineRenderer Line(string name, string tex, Color c, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mat(tex);
            lr.textureMode = LineTextureMode.Stretch;
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            return lr;
        }

        /// <summary>번개 (지그재그 선 위에 번개 그림을 늘여 붙인다, 깜빡임)</summary>
        public void Bolt(Vector3 from, Vector3 to, Color c, float dur = 0.4f, float width = 0.14f)
            => StartCoroutine(BoltRoutine(from, to, c, dur, width));

        IEnumerator BoltRoutine(Vector3 from, Vector3 to, Color c, float dur, float width)
        {
            var lr = Line("FxBolt", "bolt", c, width * 2.6f);
            const int N = 12;
            lr.positionCount = N;
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
                        float j = (i == 0 || i == N - 1) ? 0 : Random.Range(-1f, 1f) * dir.magnitude * 0.05f;
                        lr.SetPosition(i, from + dir * u + side.normalized * j + Vector3.up * Random.Range(-0.04f, 0.04f));
                    }
                }
                float a = Random.value < 0.25f ? 0.45f : 1f;
                lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, a);
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        /// <summary>곧은 빛줄기 (빛 속성 투사체)</summary>
        IEnumerator BeamRoutine(Vector3 from, Vector3 to, Color c, float dur, float width)
        {
            var lr = Line("FxBeam", "bolt", c, width);
            lr.positionCount = 2;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                lr.SetPosition(0, from);
                lr.SetPosition(1, Vector3.Lerp(from, to, Mathf.Clamp01(t * 2.5f)));
                lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 1 - t * 0.6f);
                yield return null;
            }
            Destroy(lr.gameObject);
        }

        /// <summary>초승달 베기 (카드 위에 비스듬히 그어지는 그림)</summary>
        public void Slash(Vector3 pos, Color c, float size = 1f, float dur = 0.35f)
            => StartCoroutine(QuadRoutine("slash", pos + Vector3.up * 0.3f, c, 0.9f * size, 2.0f * size, dur, false, Random.Range(-30f, 30f), 0.15f,
                                          t => t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / 0.9f, Random.Range(-50f, 10f)));

        /// <summary>회오리 (바람): 바닥에서 빠르게 돌며 떠오르는 소용돌이 그림 2겹</summary>
        public void Spiral(Vector3 pos, Color c, float size = 1f, float dur = 0.9f)
        {
            StartCoroutine(QuadRoutine("swirl", pos + Vector3.up * 0.06f, c, 0.8f * size, 2.1f * size, dur, true, -540f, 0.9f * size));
            StartCoroutine(QuadRoutine("swirl", pos + Vector3.up * 0.25f, c, 0.5f * size, 1.5f * size, dur * 0.85f, true, 620f, 1.4f * size, null, float.NaN, 1f, 0.7f));
            Motes(pos, Style.Wind, Mathf.RoundToInt(10 * size), Vector3.up * 1.4f * size, 0.9f, 0.09f, dur * 0.8f, 0f, 0.3f * size);
        }

        /// <summary>조준선 (자유 대상 · 저격): 크게 나타나 카드 위로 조여들며 돈다</summary>
        public void Reticle(Vector3 pos, Color c, float dur = 1.2f)
            => StartCoroutine(QuadRoutine("reticle", pos + Vector3.up * 0.08f, c, 2.4f, 1.15f, dur, true, 90f, 0f, t => t < 0.8f ? 1f : (1 - t) / 0.2f, 0f));

        /// <summary>방어막 (파괴 방지 · HP 증가 · 도발): 카드 위에 떠오르는 육각 보호막</summary>
        public void Bubble(Vector3 pos, Color c, float dur = 0.9f)
            => StartCoroutine(QuadRoutine("shield", pos + Vector3.up * 0.2f, c, 0.6f, 1.7f, dur, false, 0f, 0.15f, t => (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f) * 0.9f, 0f));

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
                yield return BeamRoutine(from + Vector3.up * 0.3f, to + Vector3.up * 0.15f, ColorOf(s), 0.35f, 0.6f * size);
                Impact(to, s, size);
                yield break;
            }
            Sfx.Play("whoosh", 0.45f);
            var q = Quad("FxOrb", "orb", ColorOf(s));
            float arc = Vector3.Distance(from, to) * 0.25f;
            float trailT = 0;
            Vector3 prev = from;
            for (float t = 0; t < 1f; t += Time.deltaTime / time)
            {
                var p = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc + 0.35f);
                q.transform.position = p;
                q.transform.localScale = Vector3.one * 1.6f * size;
                if (Cam != null)
                {
                    // 꼬리(그림 왼쪽)가 진행 방향 반대를 보도록 화면 기준 각도로 돌린다
                    var d = Cam.transform.InverseTransformDirection(p - prev);
                    float ang = d.sqrMagnitude > 1e-6f ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : 0f;
                    q.transform.rotation = Cam.transform.rotation * Quaternion.Euler(0, 0, ang);
                }
                prev = p;
                var c = ColorOf(s);
                SetColor(q, new Color(c.r, c.g, c.b, 1f));
                trailT += Time.deltaTime;
                if (trailT > 0.03f)
                {
                    trailT = 0;
                    Motes(p, s, s == Style.Earth ? 2 : 3, Vector3.zero, 0.25f, 0.12f * size, 0.35f, s == Style.Water || s == Style.Earth ? 0.4f : -0.1f, 0.05f);
                }
                yield return null;
            }
            Kill(q);
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

        /// <summary>개기일식: 화면이 어두워지고 일식 그림(검은 원판 + 코로나)이 화면 가운데 떠오른다</summary>
        public IEnumerator Eclipse()
        {
            Sfx.Play("charge4", 0.8f);
            StartCoroutine(TintRoutine(tint, new Color(0, 0, 0.02f, 0.9f), 0.45f, 1.0f, 0.9f));
            bool pic = Sprite("eclipse") != null;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
            {
                float e = 1 - (1 - t) * (1 - t);
                corona.color = new Color(1f, 1f, 1f, pic ? e : 0f);
                corona.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, e);
                corona.rectTransform.localRotation = Quaternion.Euler(0, 0, -20f * (1 - e));
                if (!pic)
                {
                    disk.color = new Color(0, 0, 0, Mathf.Min(1, t * 1.5f));
                    disk.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-260, 0, e), 120);
                }
                yield return null;
            }
            board.Shake(0.15f);
            float hold = 0.9f;
            for (float t = 0; t < hold; t += Time.deltaTime)
            {
                corona.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 6f);
                corona.rectTransform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 9f));
                yield return null;
            }
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
            {
                corona.color = new Color(1f, 1f, 1f, pic ? 1 - t : 0f);
                disk.color = new Color(0, 0, 0, pic ? 0f : 1 - t);
                yield return null;
            }
            corona.color = Color.clear;
            corona.rectTransform.localScale = Vector3.one;
            corona.rectTransform.localRotation = Quaternion.identity;
            disk.color = Color.clear;
        }

        // ══════════════════════════ 의미 단위 (게임 처리에서 호출) ══════════════════════════

        public void Heal(PlayerState p)
        {
            var pos = HeroPos(p);
            var green = new Color(0.55f, 1f, 0.65f);
            Ring(pos, green, 2.4f, 0.8f);
            StartCoroutine(QuadRoutine("heal", pos + Vector3.up * 0.4f, Color.white, 1.4f, 2.4f, 1.1f, false, 0f, 0.9f, null, 0f));
            Sparks(pos, green, new Color(1f, 0.98f, 0.7f), 18, Vector3.up * 1.6f, 0.6f, 0.08f, 1.2f, -0.05f, 0.45f, "star");
        }

        public void Draw(PlayerState p, int n)
        {
            var pos = DeckPos(p);
            Sparks(pos, new Color(0.55f, 0.8f, 1f), Color.white, 6 + 4 * n, Vector3.up * 1.2f, 0.5f, 0.07f, 0.8f, -0.1f, 0.3f, "star");
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
                    Burst(pos, s, 2.4f, 0.45f);
                    break;
                case Style.Fire:
                    Pillar(pos, new Color(1f, 0.45f, 0.1f), 3f, 1.1f, 0.8f);
                    Burst(pos, s, 2.2f, 0.5f);
                    Motes(pos, s, 26, Vector3.up * 3f, 0.8f, 0.16f, 0.9f, -0.2f, 0.35f);
                    break;
                case Style.Water:
                    Pillar(pos, new Color(0.3f, 0.65f, 1f), 2.5f, 1.1f, 0.8f);
                    Burst(pos, s, 2.2f, 0.5f);
                    Motes(pos, s, 26, Vector3.up * 3.2f, 1.2f, 0.12f, 1f, 1.2f, 0.3f);
                    break;
                case Style.Wind:
                    Spiral(pos, c, 1.1f, 0.8f);
                    Ring(pos, s, 2.4f, 0.5f);
                    break;
                case Style.Earth:
                    Motes(pos + Vector3.up * 3f, s, 18, Vector3.down * 3f, 0.6f, 0.28f, 0.8f, 1.5f, 0.6f);
                    Burst(pos, s, 2.4f, 0.55f);
                    board.Shake(0.2f);
                    break;
                case Style.Light:
                    Pillar(pos, new Color(1f, 0.95f, 0.7f), 6f, 1.2f, 0.9f);
                    Ring(pos, s, 2.6f, 0.6f);
                    break;
                case Style.Dark:
                    Slash(pos, c, 1.1f);
                    Burst(pos, s, 2.2f, 0.55f);
                    Motes(pos, s, 20, Vector3.up * 0.4f, 0.8f, 0.25f, 1.2f, -0.05f, 0.5f);
                    break;
                default:
                    Slash(pos, s == Style.Rainbow ? Color.HSVToRGB(Random.value, 0.6f, 1f) : Color.white, 1.1f);
                    Burst(pos, s, 2f, 0.45f);
                    break;
            }
            yield return new WaitForSeconds(0.28f);
        }

        /// <summary>파괴될 때 (항상) — 파편 + 연기</summary>
        public void Shatter(CardInstance c)
        {
            var pos = PosOf(c);
            var col = CardData.ElementColor(c.data.element);
            Sparks(pos, col, Color.white, 14, Vector3.up * 1.6f, 2.0f, 0.09f, 0.6f, 1.2f, 0.3f, "shard");
            Burst(pos, StyleOf(c.data.element), 1.6f, 0.35f);
        }

        public void AtkChange(CardInstance c, int value)
        {
            if (c == null || value == 0) return;
            var pos = PosOf(c);
            if (value > 0) Sparks(pos, new Color(1f, 0.6f, 0.2f), new Color(1f, 0.95f, 0.4f), 12, Vector3.up * 1.6f, 0.2f, 0.09f, 0.8f, 0, 0.35f, "star");
            else Sparks(pos + Vector3.up * 1.2f, new Color(0.6f, 0.3f, 0.9f), new Color(0.3f, 0.1f, 0.5f), 12, Vector3.down * 1.4f, 0.2f, 0.12f, 0.8f, 0, 0.35f, "mote_dark");
        }

        public void HpUp(CardInstance c) => Bubble(PosOf(c) + Vector3.up * 0.1f, new Color(0.4f, 1f, 0.6f), 0.8f);

        public void Guard(CardInstance c) => Bubble(PosOf(c) + Vector3.up * 0.1f, new Color(0.5f, 0.85f, 1f), 0.9f);

        public void Wound(CardInstance c) => Slash(PosOf(c), new Color(0.75f, 0.1f, 0.25f), 0.8f, 0.3f);

        public void Stun(CardInstance c)
        {
            var pos = PosOf(c);
            Ring(pos, new Color(0.7f, 0.5f, 1f), 1.6f, 0.6f, true);
            Sparks(pos + Vector3.up * 0.6f, new Color(1f, 0.95f, 0.5f), new Color(0.8f, 0.6f, 1f), 10, Vector3.zero, 0.9f, 0.1f, 1f, -0.05f, 0.4f, "star");
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
            var s = StyleOf(c);
            Glyph(pos, s, 2.4f, 1f);
            Pillar(pos, CardData.ElementColor(c.data.element), 4f, 1f, 0.9f);
            Ring(pos, s, 2.6f, 0.7f);
        }

        public void GraveRise(PlayerState p) => Sparks(GravePos(p), new Color(0.8f, 0.7f, 1f), Color.white, 16, Vector3.up * 1.8f, 0.4f, 0.1f, 1f, -0.1f, 0.3f, "mote_light");

        // ══════════════════════════ 카드 놓기 · 공개 · 소멸 ══════════════════════════

        /// <summary>
        /// 카드를 칸에 놓았을 때: 카드가 날아오는 동안 금빛 마법진이 안쪽으로 모이고, 내려앉는 순간 먼지와 반짝임이 퍼진다.
        /// 뒷면 배치라 속성 색은 쓰지 않는다 (상대에게 속성이 새지 않게)
        /// </summary>
        public void Place(CardInstance c) => StartCoroutine(PlaceRoutine(c));

        IEnumerator PlaceRoutine(CardInstance c)
        {
            if (c == null || c.owner == null || c.slot < 0) yield break;
            var pos = board.SlotPos(c.owner, c.slot) + Vector3.up * 0.05f;
            var gold = new Color(1f, 0.85f, 0.45f);
            StartCoroutine(QuadRoutine("glyph_neutral", pos + Vector3.up * 0.07f, gold, 2.4f, 1.2f, 0.55f, true, -120f, 0f, t => t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / 0.9f));
            yield return new WaitForSeconds(0.22f);
            if (c.view == null || c.zone != Zone.Slot) yield break;
            Ring(pos, gold, 1.8f, 0.45f);
            Sparks(pos, new Color(0.85f, 0.8f, 0.7f), new Color(0.5f, 0.45f, 0.4f), 10, Vector3.up * 0.35f, 0.9f, 0.16f, 0.55f, 0.35f, 0.3f, "dust");   // 먼지
            Sparks(pos, gold, Color.white, 8, Vector3.up * 1.2f, 0.4f, 0.07f, 0.5f, 0.2f, 0.25f, "star");                                              // 반짝임
            board.Shake(0.04f);
        }

        /// <summary>
        /// 배틀 시작 공개: 카드가 뒤집히며 속성 연출 — 속성 폭발 그림·충격파 고리·마법진 + 화염 불기둥·물보라·바람 회오리·대지 바위·번개 낙뢰·빛기둥·어둠 안개.
        /// Rare 는 조금, Unique 는 크게 + 섬광
        /// </summary>
        public void RevealFlip(CardInstance c)
        {
            if (c == null) return;
            var s = StyleOf(c);
            var col = ColorOf(s);
            var pos = PosOf(c);
            float k = c.data.rarity == Rarity.Unique ? 1.8f : c.data.rarity == Rarity.Rare ? 1.4f : 1.15f;
            Burst(pos, s, 2.4f * k, 0.5f);
            Ring(pos, s, 3.2f * k, 0.6f);
            Glyph(pos, s, 2.0f * k, 0.7f);
            switch (s)
            {
                case Style.Fire:
                    Pillar(pos, col, 2.2f * k, 0.7f * k, 0.5f);
                    Motes(pos, s, (int)(16 * k), Vector3.up * 1.6f, 1.2f, 0.14f, 0.7f, -0.2f, 0.3f);
                    break;
                case Style.Water:
                    Motes(pos, s, (int)(18 * k), Vector3.up * 2.2f, 1.2f, 0.11f, 0.9f, 0.9f, 0.3f);
                    Ring(pos, Color.white, 1.4f * k, 0.4f, true);
                    break;
                case Style.Wind:
                    Spiral(pos, col, 0.9f * k, 0.7f);
                    break;
                case Style.Earth:
                    Motes(pos, s, (int)(14 * k), Vector3.up * 1.8f, 1.2f, 0.18f, 0.8f, 1.3f, 0.35f);
                    board.Shake(0.06f * k);
                    break;
                case Style.Thunder:
                    Bolt(pos + new Vector3(Random.Range(-0.5f, 0.5f), 3.5f, 0.8f), pos, col, 0.25f, 0.12f);
                    Motes(pos, s, (int)(14 * k), Vector3.zero, 2.6f, 0.07f, 0.4f);
                    break;
                case Style.Light:
                    Pillar(pos, new Color(1f, 0.96f, 0.75f), 3.5f * k, 0.9f * k, 0.6f);
                    Motes(pos, s, (int)(12 * k), Vector3.up * 1.2f, 0.8f, 0.1f, 0.8f, -0.15f, 0.3f);
                    break;
                case Style.Dark:
                    Motes(pos, s, (int)(18 * k), Vector3.up * 0.5f, 1.3f, 0.2f, 0.9f, -0.08f, 0.35f);
                    Ring(pos, new Color(0.3f, 0.05f, 0.5f), 1.8f * k, 0.5f, true);
                    break;
                case Style.Rainbow:
                    for (int i = 0; i < 6; i++)
                        Sparks(pos, Color.HSVToRGB(i / 6f, 0.75f, 1f), Color.HSVToRGB(i / 6f + 0.08f, 0.5f, 1f), (int)(4 * k), Vector3.up * 1.2f, 1.6f, 0.1f, 0.8f, 0f, 0.15f, "star");
                    break;
                default:
                    Motes(pos, s, (int)(12 * k), Vector3.up * 1.2f, 1.4f, 0.11f, 0.6f);
                    break;
            }
            if (c.data.rarity == Rarity.Unique) Flash(new Color(col.r, col.g, col.b, 0.22f), 0.4f);
        }

        ParticleSystem AshSystem(Vector3 pos, string tex, float gravity)
        {
            var go = new GameObject("FxDissolve_" + tex);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var colL = ps.colorOverLifetime; colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0, 1) });
            colL.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.15f));
            var rot = ps.rotationOverLifetime; rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(tex);
            ps.Play();
            return ps;
        }

        ParticleSystem.Particle[] steerBuf;

        /// <summary>파티클을 목표 지점으로 빨아들인다: 나이가 들수록 세게 끌리고, 살짝 소용돌이치며, 닿으면 사라진다</summary>
        void Steer(ParticleSystem ps, Vector3 target, float dt, float pull)
        {
            int n = ps.particleCount;
            if (n == 0) return;
            if (steerBuf == null || steerBuf.Length < n) steerBuf = new ParticleSystem.Particle[Mathf.Max(n, 512)];
            n = ps.GetParticles(steerBuf);
            for (int i = 0; i < n; i++)
            {
                var p = steerBuf[i];
                var d = target - p.position;
                float dist = d.magnitude;
                if (dist < 0.2f) { p.remainingLifetime = 0f; steerBuf[i] = p; continue; }
                float age = 1f - p.remainingLifetime / Mathf.Max(0.01f, p.startLifetime);
                var dir = d / dist;
                p.velocity += dir * (pull * (0.6f + 1.8f * age * age)) * dt;          // 처음부터 끌리고, 갈수록 세게 빨려 들어간다
                p.velocity += Vector3.Cross(Vector3.up, dir) * (1.2f * (1f - age)) * dt;  // 살짝 소용돌이
                p.velocity *= 1f - 1.6f * dt;                                          // 저항
                if (dist < 0.9f) p.velocity = Vector3.Lerp(p.velocity, dir * p.velocity.magnitude, 6f * dt);   // 가까우면 곧장 중심으로
                steerBuf[i] = p;
            }
            ps.SetParticles(steerBuf, n);
        }

        /// <summary>
        /// 파괴: 카드가 가루가 되어 묘지로 빨려 들어간다 — 카드 면에서 아래줄부터 모래알(grain)과 속성 빛가루(mote)가 떨어져 나와
        /// 묘지 위 소용돌이(vortex)로 가속하며 빨려 들고, 카드도 작아지며 묘지 쪽으로 끌려간다.
        /// 레어도가 높을수록 가루가 많고 오래(Common 1 · Uncommon 1.3 · Rare 1.7 · Unique 2.4배), 가공 카드는 은빛(Seven)·금빛(StrikerZ) 가루와 반짝임이 섞인다.
        /// 끝나면 카드는 보이지 않는 크기(fxScale 0)로 묘지로 날아가고, Reappear 가 묘지에서 다시 보이게 한다
        /// </summary>
        public IEnumerator Dissolve(CardInstance c, float dur = 0.75f)
        {
            var v = c?.view;
            if (v == null) yield break;
            var s = StyleOf(c.data.element);
            var col = CardData.ElementColor(c.data.element);
            var grave = c.owner != null ? GravePos(c.owner) : v.transform.position + Vector3.up;
            var start = v.transform.position;
            var sand = new Color(0.85f, 0.78f, 0.65f);
            var sandDark = new Color(0.45f, 0.4f, 0.35f);
            // 레어도 → 가루 양·지속, 가공 → 은빛/금빛
            float k = c.data.rarity == Rarity.Unique ? 2.4f : c.data.rarity == Rarity.Rare ? 1.7f : c.data.rarity == Rarity.Uncommon ? 1.3f : 1f;
            float kk = (k - 1f) / 1.4f;
            dur *= Mathf.Lerp(1f, 1.35f, kk);
            bool finished = c.finish != CardFinish.Normal;
            var shine = c.finish == CardFinish.StrikerZ ? new Color(1f, 0.84f, 0.38f) : new Color(0.88f, 0.93f, 1f);        // 금빛 · 은빛
            var shineDark = c.finish == CardFinish.StrikerZ ? new Color(0.75f, 0.5f, 0.12f) : new Color(0.6f, 0.68f, 0.8f);
            // 묘지 위 흡입 소용돌이 (속성 색, 안으로 돈다 — 가공 카드는 은빛/금빛이 섞인다)
            var vortexCol = Color.Lerp(col, Color.white, 0.35f);
            if (finished) vortexCol = Color.Lerp(vortexCol, shine, 0.5f);
            StartCoroutine(QuadRoutine("vortex", grave + Vector3.up * 0.06f, vortexCol, 2.6f * Mathf.Lerp(1f, 1.5f, kk), 1.1f, dur + 0.9f, true, -720f, 0f,
                                       t => t < 0.12f ? t / 0.12f : t > 0.72f ? 1f - (t - 0.72f) / 0.28f : 1f, float.NaN, 1f, 0.9f));
            var psGrain = AshSystem(start, "grain", 0f);
            var psSpark = AshSystem(start, Tex("mote_" + Suffix(s)) != null ? "mote_" + Suffix(s) : "mote_neutral", 0f);
            var psShine = finished ? AshSystem(start, "star", 0f) : null;   // 가공 반짝임
            var ep = new ParticleSystem.EmitParams();
            float emit = 0;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                if (c.view != v) break;
                v.fxScale = Mathf.Lerp(1f, 0.05f, t * t);
                v.lunge = (grave - start) * (0.45f * t * t) + Vector3.up * 0.2f * t;   // 카드도 묘지 쪽으로 끌려간다
                // 아래쪽 줄부터 위로 쓸려 올라가며 가루가 된다
                emit += Time.deltaTime * 380f * k;
                while (emit > 1)
                {
                    emit--;
                    float sx = Random.Range(-CardView.W, CardView.W) * 0.5f;
                    float sz0 = Mathf.Lerp(-CardView.H * 0.5f, CardView.H * 0.5f, Mathf.Clamp01(t + Random.Range(-0.2f, 0.2f)));
                    float roll = Random.value;
                    bool glint = finished && roll < 0.18f;                 // 가공: 은빛/금빛 반짝임
                    bool shiny = finished && !glint && roll < 0.45f;       // 가공: 은빛/금빛 모래알
                    bool spark = !glint && !shiny && Random.value < 0.35f;
                    float area = Mathf.Lerp(1f, 0.6f, t);
                    ep.position = v.transform.position + v.lunge + v.transform.rotation * new Vector3(sx * area, 0.02f, sz0 * area);
                    ep.velocity = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.7f), Random.Range(-0.5f, 0.5f));
                    ep.startColor = glint ? Color.Lerp(shine, Color.white, Random.value * 0.5f)
                                  : shiny ? Color.Lerp(shine, shineDark, Random.value)
                                  : spark ? Color.Lerp(col, Color.white, Random.value * 0.6f) : Color.Lerp(sand, sandDark, Random.value);
                    ep.startSize = (glint ? 0.07f : spark ? 0.08f : 0.11f) * SizeK * MoteK * Random.Range(0.5f, 1.2f) * Mathf.Lerp(1f, 1.25f, kk);
                    ep.startLifetime = Random.Range(1.2f, 1.7f);
                    ep.rotation = Random.Range(0f, 360f);
                    (glint ? psShine : spark ? psSpark : psGrain).Emit(ep, 1);
                }
                Steer(psGrain, grave, Time.deltaTime, 18f);
                Steer(psSpark, grave, Time.deltaTime, 18f);
                if (psShine != null) Steer(psShine, grave, Time.deltaTime, 18f);
                yield return null;
            }
            if (c.view == v) { v.fxScale = 0.001f; v.lunge = Vector3.zero; }
            StartCoroutine(SuckTail(new[] { psGrain, psSpark, psShine }, grave, s, finished, shine, k));   // 남은 가루는 게임 진행을 막지 않고 계속 빨려 들어간다
        }

        IEnumerator SuckTail(ParticleSystem[] systems, Vector3 grave, Style s, bool finished, Color shine, float k)
        {
            for (float t = 0; t < 2.2f; t += Time.deltaTime)
            {
                bool any = false;
                foreach (var ps in systems) if (ps != null && ps.particleCount > 0) { Steer(ps, grave, Time.deltaTime, 24f); any = true; }
                if (!any) break;
                yield return null;
            }
            Ring(grave, s, 1.3f * k, 0.35f, true);
            if (finished) Sparks(grave, shine, Color.white, Mathf.RoundToInt(8 * k), Vector3.up * 1.2f, 0.5f, 0.07f, 0.7f, -0.1f, 0.25f, "star");   // 가공: 묘지에서 은빛/금빛 반짝 마무리
            foreach (var ps in systems) if (ps != null) Destroy(ps.gameObject, 0.2f);
        }

        /// <summary>소멸한 카드가 묘지에 닿을 때쯤 다시 보이게</summary>
        public void Reappear(CardInstance c, float delay) => StartCoroutine(ReappearRoutine(c, delay));

        IEnumerator ReappearRoutine(CardInstance c, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (c?.view != null) c.view.fxScale = 1f;
        }

        public void FieldChange(CardData field)
        {
            var s = StyleOf(field.element);
            var c = CardData.ElementColor(field.element);
            Flash(new Color(c.r, c.g, c.b, 0.35f), 0.5f);
            Ring(BoardView.CenterPos, s, 9f, 1.1f);
            Ring(BoardView.CenterPos, Color.white, 6f, 0.8f);
            Glyph(BoardView.CenterPos, s, 5f, 1.2f);
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
                    if (mine != null) Motes(RowCenter(mine), Style.Dark, 30, Vector3.up * 0.4f, 0.4f, 0.3f, 1.6f, -0.02f, 2.5f);
                    break;
                case StatusKind.FreeTarget:
                    if (mine != null)
                        foreach (var m in mine.opponent.Monsters()) Reticle(PosOf(m), new Color(1f, 0.35f, 0.3f), 1.3f);
                    break;
            }
        }

        // ══════════════════════════ 배틀 시작: 공격 턴 · 수비 턴 ══════════════════════════

        /// <summary>
        /// 배틀 시작 연출: 공격 측 줄에는 붉은 충격파와 화염 폭발이 줄을 따라 번지고, 수비 측 줄에는 푸른 보호막 고리가 조여들며 소환수마다 방어막이 떠오른다.
        /// 화면도 내 턴에 맞춰 붉게/푸르게 번쩍인다
        /// </summary>
        public void TurnSignal(PlayerState attacker, PlayerState defender)
        {
            if (attacker == null || defender == null) return;
            StartCoroutine(TurnSignalRoutine(attacker, defender));
        }

        IEnumerator TurnSignalRoutine(PlayerState attacker, PlayerState defender)
        {
            var red = new Color(1f, 0.42f, 0.25f);
            var blue = new Color(0.45f, 0.72f, 1f);
            bool mine = attacker.isHuman;
            Flash(mine ? new Color(1f, 0.45f, 0.2f, 0.2f) : new Color(0.3f, 0.55f, 1f, 0.2f), 0.45f);
            // 공격 측: 줄 가운데에서 큰 붉은 충격파 + 왼쪽부터 오른쪽으로 번지는 화염 폭발
            Ring(RowCenter(attacker), red, 8f, 0.9f);
            Glyph(RowCenter(attacker), Style.Fire, 4.5f, 1.0f);
            // 수비 측: 푸른 고리가 안으로 조여들고 소환수마다 방어막
            Ring(RowCenter(defender), blue, 8f, 0.9f, true);
            Glyph(RowCenter(defender), Style.Water, 4.5f, 1.0f);
            foreach (var m in defender.Monsters()) if (m.view != null && !m.faceDown) Bubble(PosOf(m) + Vector3.up * 0.1f, blue, 1.0f);
            for (int i = 0; i < PlayerState.SlotCount; i++)
            {
                var p = board.SlotPos(attacker, i);
                Burst(p, Style.Fire, 1.5f, 0.4f);
                Motes(p, Style.Fire, 5, Vector3.up * 1.4f, 0.5f, 0.1f, 0.6f, -0.15f, 0.3f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        // ══════════════════════════ 지속 오라 (버프·디버프가 걸려 있는 동안) ══════════════════════════

        /// <summary>한 카드의 한 종류(버프/디버프) 오라: 카드 아래에서 도는 고리 + 이따금 떠오르는(가라앉는) 입자</summary>
        class Aura
        {
            public GameObject ring;
            public float age, spin, emitT;
        }

        readonly Dictionary<(CardInstance, int), Aura> auras = new Dictionary<(CardInstance, int), Aura>();
        readonly HashSet<(CardInstance, int)> auraWanted = new HashSet<(CardInstance, int)>();
        readonly List<(CardInstance, int)> auraDead = new List<(CardInstance, int)>();

        const int AuraBuff = 1, AuraDebuff = 2;
        static readonly Color BuffCol = new Color(1f, 0.82f, 0.4f);
        static readonly Color DebuffCol = new Color(0.72f, 0.38f, 1f);

        /// <summary>
        /// 카드에 걸린 지속 효과를 모아 오라 종류를 정한다 (비트: 1 = 버프, 2 = 디버프).
        /// 버프 = 임시 ATK +, 기간 ATK/HP +, 아군 전체 ATK + 상태(대장장이 거한·전투의 함성 등), 자유 대상(저격 조준)
        /// 디버프 = 약화, 임시 ATK −, 기간 ATK/HP −, 상대가 건 ATK − 상태(봉화 등), 행동 불능, [상처]
        /// 뒷면 카드는 정보가 새지 않게 표시하지 않는다
        /// </summary>
        static int AuraKind(GameManager g, CardInstance c)
        {
            if (c == null || !c.IsMonster || c.zone != Zone.Slot || c.view == null || c.faceDown) return 0;
            int plus = 0, minus = 0;
            if (c.tempAtk > 0) plus += c.tempAtk; else minus -= c.tempAtk;
            minus += c.weaken;
            foreach (var b in c.buffs) if (b.untilTime >= g.time) { if (b.value > 0) plus += b.value; else minus -= b.value; }
            foreach (var b in c.hpBuffs) if (b.untilTime >= g.time) { if (b.value > 0) plus += b.value; else minus -= b.value; }
            foreach (var s in g.statuses)
            {
                if (s.target != null && s.target != c.owner) continue;
                if (s.kind == StatusKind.AtkBonus)
                {
                    if (s.value > 0) plus += s.value;
                    else if (s.value < 0 && !(g.Resist(c) && s.caster != c.owner)) minus -= s.value;
                }
                else if (s.kind == StatusKind.FreeTarget && s.target == c.owner) plus += 1;
            }
            if (c.stunUntil >= g.time || c.noRegenUntil >= g.time) minus += 1;
            return (plus > 0 ? AuraBuff : 0) | (minus > 0 ? AuraDebuff : 0);
        }

        void LateUpdate() => UpdateAuras();

        void UpdateAuras()
        {
            var g = GameManager.I;
            auraWanted.Clear();
            if (g != null && g.players != null)
                foreach (var p in g.players)
                {
                    if (p == null) continue;
                    foreach (var c in p.slots)
                    {
                        int k = AuraKind(g, c);
                        if ((k & AuraBuff) != 0) auraWanted.Add((c, AuraBuff));
                        if ((k & AuraDebuff) != 0) auraWanted.Add((c, AuraDebuff));
                    }
                }
            auraDead.Clear();
            foreach (var kv in auras) if (!auraWanted.Contains(kv.Key) || kv.Key.Item1.view == null || kv.Value.ring == null) auraDead.Add(kv.Key);
            foreach (var key in auraDead) { Kill(auras[key].ring); auras.Remove(key); }
            foreach (var key in auraWanted)
            {
                bool buff = key.Item2 == AuraBuff;
                if (!auras.TryGetValue(key, out var a))
                {
                    string tex = buff ? (Tex("aura_buff") != null ? "aura_buff" : "ring_light") : (Tex("aura_debuff") != null ? "aura_debuff" : "ring_dark");
                    a = new Aura { ring = Quad("FxAura", tex, buff ? BuffCol : DebuffCol), spin = Random.Range(0f, 360f) };
                    auras[key] = a;
                }
                var v = key.Item1.view;
                a.age += Time.deltaTime;
                a.spin += Time.deltaTime * (buff ? 45f : -30f);
                float fadeIn = Mathf.Clamp01(a.age / 0.35f);
                float size = (buff ? 1.75f : 1.9f) * (1f + 0.035f * Mathf.Sin(Time.time * 3.2f)) * Mathf.Lerp(1.6f, 1f, fadeIn);
                a.ring.transform.position = v.transform.position + Vector3.up * (buff ? 0.06f : 0.075f);
                a.ring.transform.rotation = Quaternion.Euler(90, a.spin, 0);
                a.ring.transform.localScale = new Vector3(size, size, 1);
                var col = buff ? BuffCol : DebuffCol;
                float alpha = fadeIn * (0.55f + 0.2f * Mathf.Sin(Time.time * 3.2f + (buff ? 0f : 1.5f)));
                SetColor(a.ring, new Color(col.r, col.g, col.b, alpha));
                // 이따금 입자: 버프는 금빛 반짝임이 떠오르고, 디버프는 어둠 입자가 내려앉는다
                a.emitT += Time.deltaTime;
                if (a.emitT > (buff ? 0.4f : 0.5f))
                {
                    a.emitT = 0;
                    var pos = v.transform.position + Vector3.up * 0.1f;
                    if (buff) Sparks(pos, BuffCol, Color.white, 2, Vector3.up * 0.9f, 0.25f, 0.05f, 0.8f, -0.1f, 0.35f, "star");
                    else Sparks(pos + Vector3.up * 0.7f, DebuffCol, new Color(0.3f, 0.1f, 0.5f), 2, Vector3.down * 0.5f, 0.2f, 0.1f, 0.9f, 0.15f, 0.35f, "mote_dark");
                }
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
                    Motes(HeroPos(me), Style.Fire, 40, Vector3.up * 4f, 0.8f, 0.14f, 1.4f, -0.1f, 0.4f);
                    Flash(new Color(1f, 0.55f, 0.2f, 0.2f), 0.5f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S002":   // 포기란없다: 묘지에서 황금 불꽃
                    Pillar(GravePos(me), new Color(1f, 0.85f, 0.4f), 4f, 1f, 1f);
                    Ring(GravePos(me), new Color(1f, 0.85f, 0.4f), 2.6f, 0.8f);
                    Glyph(GravePos(me), Style.Light, 2.2f, 0.9f);
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
                    for (int i = 0; i < 3; i++) { Ring(RowCenter(me), Style.Fire, 4f + i * 2f, 0.7f); yield return new WaitForSeconds(0.12f); }
                    board.Shake(0.25f);
                    break;
                case "S006":   // 화염구: 손에 불꽃을 모은다
                    Motes(HeroPos(me), Style.Fire, 28, Vector3.zero, 1.2f, 0.16f, 0.5f, 0, 1.2f);
                    Ring(HeroPos(me), Style.Fire, 2.2f, 0.4f, true);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case "S007":   // 지혜의 샘: 덱에서 솟는 물
                    Motes(DeckPos(me), Style.Water, 40, Vector3.up * 3.5f, 0.8f, 0.12f, 1.2f, 1.1f, 0.3f);
                    Ring(DeckPos(me), Style.Water, 2.2f, 0.8f);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case "S008":   // 질풍 가속: 바람이 휩쓴다
                    Sfx.Play("whoosh");
                    Motes(RowCenter(me) + Vector3.left * 4f, Style.Wind, 50, Vector3.right * 9f, 0.8f, 0.1f, 0.9f, 0, 0.6f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S009":   // 치유의 빛
                    Pillar(HeroPos(me), new Color(1f, 0.95f, 0.65f), 7f, 1.4f, 1.2f);
                    Glyph(HeroPos(me), Style.Light, 2.6f, 1.1f);
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
                    if (op != null) { int k = 0; foreach (var t in op.Monsters()) { if (k++ >= 2) break; Pillar(PosOf(t), new Color(0.9f, 0.15f, 0.05f), 3.5f, 1.2f, 1.2f); Burst(PosOf(t), Style.Fire, 2.4f, 0.6f); } }
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
                    Glyph(BoardView.CenterPos, Style.Light, 4f, 1f);
                    yield return new WaitForSeconds(0.4f);
                    break;
                case "S106":   // 영광의 최후
                    Sfx.Play("boom", 0.6f);
                    for (int i = 0; i < 3; i++) Ring(BoardView.CenterPos, new Color(1f, 0.8f, 0.3f), 5f + i * 3f, 0.8f + i * 0.15f);
                    Burst(BoardView.CenterPos, Style.Light, 5f, 0.7f);
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
                    if (op != null) { Ring(RowCenter(op), Style.Water, 7f, 1f); foreach (var t in op.Monsters()) Motes(PosOf(t) + Vector3.up * 2f, Style.Water, 10, Vector3.down * 2f, 0.3f, 0.1f, 0.8f, 1f, 0.3f); }
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S109":   // 루미너스 보이드: 화이트 홀
                    Flash(new Color(1f, 1f, 1f, 0.75f), 0.9f);
                    Ring(BoardView.CenterPos, Color.white, 10f, 1f, true);
                    Glyph(BoardView.CenterPos, Style.Light, 6f, 1.1f);
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
                    foreach (var p in g.players) if (p != null) Motes(GravePos(p), Style.Light, 18, Vector3.up * 1.6f, 0.4f, 0.14f, 1.6f, -0.08f, 0.3f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S201":   // 신비의 심해: 물 속
                    Tint(new Color(0f, 0.18f, 0.45f, 0.55f), 0.4f, 0.7f, 0.8f);
                    Motes(RowCenter(me), Style.Water, 50, Vector3.up * 1.5f, 0.5f, 0.12f, 2f, -0.15f, 3f);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case "S202":   // 깊은 암야: 어둠이 내려앉는다
                    Tint(new Color(0.06f, 0f, 0.12f, 0.8f), 0.4f, 0.8f, 0.9f);
                    if (op != null) foreach (var t in op.Monsters()) Motes(PosOf(t), Style.Dark, 14, Vector3.up * 0.3f, 0.4f, 0.3f, 1.6f, -0.02f, 0.5f);
                    yield return new WaitForSeconds(0.7f);
                    break;
                case "S203":   // 들풀의 군세: 잎사귀 폭풍
                    Motes(new Vector3(0, 1.2f, -3f), Style.Wind, 60, Vector3.forward * 4f + Vector3.up, 2f, 0.14f, 1.4f, 0.3f, 1.5f);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case "S204":   // 대지의 분노: 지진
                    Sfx.Play("boom", 0.7f);
                    board.Shake(0.6f);
                    if (op != null) for (int i = 0; i < PlayerState.SlotCount; i++) Motes(board.SlotPos(op, i), Style.Earth, 9, Vector3.up * 3f, 1f, 0.22f, 0.9f, 1.5f, 0.3f);
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
                            Burst(board.SlotPos(op, i), Style.Fire, 1.8f, 0.45f);
                            Motes(board.SlotPos(op, i), Style.Fire, 10, Vector3.up * 2.5f, 0.6f, 0.2f, 0.8f, -0.2f, 0.4f);
                            yield return new WaitForSeconds(0.05f);
                        }
                    break;
                case "S207":   // 균형의 저울: 빛과 어둠
                    Flash(new Color(1f, 1f, 1f, 0.5f), 0.3f);
                    Glyph(BoardView.CenterPos, Style.Light, 5f, 0.6f);
                    yield return new WaitForSeconds(0.25f);
                    Flash(new Color(0f, 0f, 0f, 0.6f), 0.4f);
                    Glyph(BoardView.CenterPos, Style.Dark, 5f, 0.6f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case "S208":   // 앱솔루트 스펠: 무지개 고리
                    for (int i = 0; i < 6; i++) Ring(BoardView.CenterPos, Color.HSVToRGB(i / 6f, 0.7f, 1f), 3f + i * 1.3f, 0.7f + i * 0.08f);
                    Glyph(BoardView.CenterPos, Style.Rainbow, 6f, 1.2f);
                    Flash(new Color(1f, 1f, 1f, 0.35f), 0.4f);
                    yield return new WaitForSeconds(0.5f);
                    break;

                default:
                    if (c.data.type == CardType.Hero)
                    {
                        // 영웅 능력: 영웅 위로 속성 빛기둥 + 고리 + 마법진
                        Pillar(src, col, 6f, 1.3f, 1.1f);
                        Ring(src, s, 3.2f, 0.8f);
                        Glyph(src, s, 2.6f, 1f);
                        Flash(new Color(col.r, col.g, col.b, 0.25f), 0.5f);
                        yield return new WaitForSeconds(0.35f);
                    }
                    else if (c.data.rarity == Rarity.Unique && c.IsMonster)
                    {
                        // 신화 몬스터 강림
                        Sfx.Play("charge5", 0.6f);
                        Pillar(src, col, 7f, 1.5f, 1.3f);
                        Glyph(src, s, 3.4f, 1.3f);
                        Ring(src, s, 4f, 0.9f);
                        Ring(src, Color.white, 2.5f, 0.7f);
                        Flash(new Color(col.r, col.g, col.b, 0.3f), 0.6f);
                        yield return new WaitForSeconds(0.45f);
                    }
                    else if (c.IsMagic)
                    {
                        // 그 밖의 마법: 속성 색 섬광 + 마법진
                        Glyph(src, s, 2.4f, 0.8f);
                        Ring(src, s, 3f, 0.7f);
                        Ring(src, Color.white, 2f, 0.5f, true);
                        Flash(new Color(col.r, col.g, col.b, 0.18f), 0.35f);
                        yield return new WaitForSeconds(0.2f);
                    }
                    break;
            }
        }
    }
}
