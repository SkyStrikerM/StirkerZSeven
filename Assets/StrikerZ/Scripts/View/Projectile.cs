using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 원거리 영웅의 발사체 (제자리에서 쏜다). 영웅마다 무기·속성에 맞는 모양:
    ///   화살(활) · 마력구(지팡이·손 마법, 속성색) · 화염구 · 번개 · 바위.
    /// GameManager.HeroStrike 가 공격 모션의 발사 순간에 손/무기 끝에서 띄워 과녁까지 날린다.
    /// </summary>
    public static class Projectile
    {
        public enum Kind { None, Arrow, Orb, Fire, Bolt, Rock }

        /// <summary>원거리 영웅 → 발사체 종류 (없으면 근접: 기존처럼 돌진)</summary>
        static readonly Dictionary<string, Kind> Ranged = new Dictionary<string, Kind>
        {
            { "H002", Kind.Orb },   // 마리나 — 산호 지팡이 물 구슬
            { "H005", Kind.Orb },   // 루미나 — 성스러운 빛 구슬
            { "H007", Kind.Fire },  // 이그니스 — 화염구
            { "H009", Kind.Arrow }, // 실피아 — 바람 화살
            { "H012", Kind.Bolt },  // 라이카 — 번개
            { "H015", Kind.Orb },   // 모르가나 — 어둠 구슬
            { "H018", Kind.Orb },   // 아리아 (물)
            { "H019", Kind.Orb },   // 베인 (바람)
            { "H020", Kind.Orb },   // 메릴 (물)
            { "H021", Kind.Rock },  // 테라 — 바위
            { "H023", Kind.Orb },   // 루미엔 (빛)
            { "H024", Kind.Orb },   // 에레보스 (어둠)
            { "H025", Kind.Orb },   // 바루스 (무속성)
            { "H026", Kind.Orb },   // 프리스마 (무지개)
        };

        /// <summary>발사체 크기 배율 (보드 카메라가 멀어 실물 비율이면 안 보인다)</summary>
        const float Scale = 1.8f;

        public static Kind KindOf(CardData hero) => hero != null && Ranged.TryGetValue(hero.id, out var k) ? k : Kind.None;

        /// <summary>공격 모션에서 발사하는 순간 (Attack 상태 정규화 시간)</summary>
        public static float ReleaseAt(Kind k) => k == Kind.Arrow ? 0.62f : 0.42f;

        static Material addMat;
        static Material AddMat(Color c)
        {
            var m = GameAssets.ParticleMat();
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static TrailRenderer Trail(GameObject go, Color c, float width, float time)
        {
            if (addMat == null) addMat = GameAssets.ParticleMat();
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = addMat;
            tr.time = time;
            tr.minVertexDistance = 0.03f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            tr.widthMultiplier = width;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.6f), 0), new GradientColorKey(c, 0.45f) },
                      new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        static Transform Glow(Transform parent, Color c, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "Glow";
            q.transform.SetParent(parent, false);
            q.transform.localScale = Vector3.one * size;
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = AddMat(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        static GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(t);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (rot.HasValue) g.transform.localRotation = rot.Value;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        static Color ColorOf(CardData hero)
        {
            if (hero.element == Element.None) return new Color(0.85f, 0.85f, 0.95f);
            return CardData.ElementColor(hero.element);
        }

        /// <summary>from 에서 to 까지 날아가 맞으면 끝난다 (맞는 순간 onHit)</summary>
        public static IEnumerator Fly(CardData hero, Kind kind, Vector3 from, Vector3 to, System.Action onHit)
        {
            Color c = ColorOf(hero);
            if (kind == Kind.Bolt) { yield return BoltRoutine(from, to, c); onHit?.Invoke(); yield break; }

            var root = new GameObject("Projectile_" + kind);
            var t = root.transform;
            t.position = from;
            t.localScale = Vector3.one * Scale;     // 보드 카메라가 멀어서 크게
            Object.Destroy(root, 3f);                // 대전이 끝나 코루틴이 멈춰도 남지 않게
            Transform glow = null, spin = null;
            float speed = 5.5f, arc = 0.12f, minDur = 0.45f;
            switch (kind)
            {
                case Kind.Arrow:
                {
                    speed = 8f; arc = 0.06f; minDur = 0.32f;
                    var wood = GameAssets.LitMat(new Color(0.45f, 0.3f, 0.16f), null, 0.3f);
                    var head = GameAssets.LitMat(new Color(0.75f, 0.95f, 0.85f), null, 0.7f, 0.6f);
                    var feather = GameAssets.UnlitMat(new Color(0.55f, 0.95f, 0.6f));
                    // 축 = +Z (진행 방향)
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, 0, -0.1f), new Vector3(0.016f, 0.15f, 0.016f), wood, Quaternion.Euler(90, 0, 0));
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0, 0.065f), new Vector3(0.035f, 0.035f, 0.06f), head, Quaternion.Euler(0, 0, 45));
                    Part(PrimitiveType.Quad, t, new Vector3(0, 0.018f, -0.22f), new Vector3(0.03f, 0.06f, 1), feather, Quaternion.Euler(0, 90, 0));
                    Part(PrimitiveType.Quad, t, new Vector3(0, -0.018f, -0.22f), new Vector3(0.03f, 0.06f, 1), feather, Quaternion.Euler(0, 90, 0));
                    glow = Glow(t, new Color(0.5f, 1f, 0.6f, 0.9f), 0.22f);
                    glow.localPosition = new Vector3(0, 0, 0.06f);
                    Trail(root, new Color(0.45f, 1f, 0.6f), 0.07f * Scale, 0.3f);
                    break;
                }
                case Kind.Fire:
                    speed = 5f; arc = 0.18f; minDur = 0.5f;
                    Part(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.11f, GameAssets.UnlitMat(new Color(1f, 0.95f, 0.7f)));
                    glow = Glow(t, new Color(1f, 0.45f, 0.1f, 1f), 0.55f);
                    Trail(root, new Color(1f, 0.4f, 0.08f), 0.26f * Scale, 0.4f);
                    break;
                case Kind.Rock:
                {
                    speed = 5f; arc = 0.35f; minDur = 0.5f;
                    // 울퉁불퉁한 바윗덩이: 크기·각도가 다른 덩어리 3개를 겹친다
                    var stone = GameAssets.LitMat(new Color(0.5f, 0.4f, 0.28f), null, 0.12f);
                    var lump = new GameObject("Rock").transform;
                    lump.SetParent(t, false);
                    Part(PrimitiveType.Sphere, lump, Vector3.zero, new Vector3(0.15f, 0.12f, 0.14f), stone);
                    Part(PrimitiveType.Sphere, lump, new Vector3(0.04f, 0.03f, -0.02f), new Vector3(0.1f, 0.09f, 0.11f), stone, Quaternion.Euler(30, 40, 0));
                    Part(PrimitiveType.Cube, lump, new Vector3(-0.03f, -0.01f, 0.03f), new Vector3(0.08f, 0.07f, 0.09f), stone, Quaternion.Euler(25, 15, 35));
                    spin = lump;
                    glow = Glow(t, new Color(0.9f, 0.7f, 0.35f, 0.6f), 0.35f);
                    Trail(root, new Color(0.8f, 0.6f, 0.3f), 0.12f * Scale, 0.35f);
                    break;
                }
                default: // Orb
                    Part(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.1f, GameAssets.UnlitMat(Color.Lerp(c, Color.white, 0.75f)));
                    glow = Glow(t, new Color(c.r, c.g, c.b, 1f), 0.45f);
                    Trail(root, c, 0.18f * Scale, 0.38f);
                    break;
            }

            float dist = Vector3.Distance(from, to);
            float dur = Mathf.Clamp(dist / speed, minDur, 1.1f);
            Vector3 prev = from;
            var cam = Camera.main;
            for (float u = 0; u < 1f; )
            {
                u = Mathf.Min(1f, u + Time.deltaTime / dur);
                Vector3 p = Vector3.Lerp(from, to, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * arc * dist);
                t.position = p;
                Vector3 v = p - prev;
                if (v.sqrMagnitude > 1e-6f) t.rotation = Quaternion.LookRotation(v);
                prev = p;
                if (glow != null && cam != null)
                {
                    glow.rotation = Quaternion.LookRotation(glow.position - cam.transform.position);
                    float pulse = 1f + Mathf.Sin(Time.time * 30f) * (kind == Kind.Fire ? 0.15f : 0.06f);
                    glow.localScale = Vector3.one * (kind == Kind.Arrow ? 0.22f : kind == Kind.Fire ? 0.55f : kind == Kind.Rock ? 0.35f : 0.45f) * pulse;
                }
                if (spin != null) spin.Rotate(400f * Time.deltaTime, 260f * Time.deltaTime, 0, Space.Self);
                yield return null;
            }
            onHit?.Invoke();
            // 꼬리(트레일)가 사라질 때까지 남겼다가 지운다
            foreach (Transform ch in t) ch.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.35f);
            if (root != null) Object.Destroy(root);
        }

        /// <summary>번개: 지그재그 빛줄기가 순간적으로 내리꽂힌다 (3번 깜빡임)</summary>
        static IEnumerator BoltRoutine(Vector3 from, Vector3 to, Color c)
        {
            var go = new GameObject("Bolt");
            Object.Destroy(go, 1.5f);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = AddMat(Color.Lerp(c, Color.white, 0.35f));
            lr.widthMultiplier = 0.14f;
            lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.7f), new Keyframe(0.5f, 1f), new Keyframe(1, 0.5f));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int N = 12;
            lr.positionCount = N;
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < N; i++)
                {
                    float u = i / (float)(N - 1);
                    Vector3 p = Vector3.Lerp(from, to, u);
                    if (i > 0 && i < N - 1) p += new Vector3(Random.Range(-1f, 1f), Random.Range(-0.6f, 0.6f), Random.Range(-1f, 1f)) * 0.16f;
                    lr.SetPosition(i, p);
                }
                lr.enabled = true;
                yield return new WaitForSeconds(0.06f);
                lr.enabled = k == 2;
                yield return new WaitForSeconds(0.03f);
            }
            yield return new WaitForSeconds(0.1f);
            if (go != null) Object.Destroy(go);
        }
    }
}
