using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 주사위 3번 / 코인 3번 연출. 보드 중앙에 3D 로 굴러 나온다.
    /// 주사위 면은 VARCO 주사위 텍스처로 만든 1~6 면(Models/dice_faces.png)을 방향이 정해진 큐브에 입혀,
    /// 라벨 숫자와 위로 보이는 눈이 항상 일치한다.
    /// </summary>
    public class DiceRoller : MonoBehaviour
    {
        /// <summary>눈(1~6)이 있는 면의 로컬 법선 — 표준 주사위 (마주 보는 면 합 7)</summary>
        public static readonly Vector3[] DieFaceNormals =
        {
            Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down
        };

        public float dieSize = 0.5f;
        public float coinSize = 0.5f;
        /// <summary>보드 위 착지 위치 z (0 = 중앙, 음수 = 내 쪽)</summary>
        public float landZ = -0.25f;
        /// <summary>보드 정중앙 (필드 카드 · 마법진 자리 = BoardView.CenterPos)</summary>
        static readonly Vector3 BoardCenter = new Vector3(0, 0.02f, 0);
        bool noLabel;

        readonly List<GameObject> spawned = new List<GameObject>();
        Canvas labelCanvas;
        readonly System.Random rng = new System.Random();
        const int MaxDice = 5;      // 바람 4회 + 코인 1개 자리
        readonly Transform[][] dieTf = { new Transform[MaxDice], new Transform[MaxDice] };
        readonly Text[][] labels = { new Text[MaxDice], new Text[MaxDice] };
        const float LabelScale = 0.004f;

        static Mesh dieMesh;
        static Material dieMat, coinMat, headsMat, tailsMat;
        static readonly Dictionary<string, Material> skinMats = new Dictionary<string, Material>();

        /// <summary>내(사람) 쪽 자리: 0 = 공격측, 1 = 방어측. GameManager 가 굴리기 전에 정한다. 장착한 꾸미기 주사위·코인은 이쪽에만 쓴다</summary>
        public int humanSide;

        Camera Cam => Camera.main;

        public void Init()
        {
            var go = new GameObject("DiceLabels", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            labelCanvas = go.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            labelCanvas.sortingOrder = 50;
        }

        /// <summary>공격측(0)과 방어측(1)이 각각 3번 던진다. withDefender=false 면 공격측만 (직접 공격)</summary>
        public IEnumerator RollBattle(bool coinA, bool coinD, bool withDefender, Action<int[], int[]> result)
            => RollBattle(3, 3, coinA, coinD, withDefender, result);

        /// <summary>countA / countD = 던지는 횟수 (바람 속성 4, 그 외 3)</summary>
        public IEnumerator RollBattle(int countA, int countD, bool coinA, bool coinD, bool withDefender, Action<int[], int[]> result)
        {
            Clear();
            Sfx.Play("dice");
            countA = Mathf.Clamp(countA, 1, MaxDice - 1);
            countD = Mathf.Clamp(countD, 1, MaxDice - 1);
            var ra = new int[countA];
            var rd = withDefender ? new int[countD] : null;
            for (int i = 0; i < countA; i++) ra[i] = coinA ? (rng.Next(2) == 0 ? 6 : 1) : rng.Next(1, 7);
            if (withDefender) for (int i = 0; i < countD; i++) rd[i] = coinD ? (rng.Next(2) == 0 ? 6 : 1) : rng.Next(1, 7);

            int running = 0;
            float cA = withDefender ? -1.8f : 0f, cD = 1.8f;
            for (int i = 0; i < countA; i++)
            {
                var pa = new Vector3(cA + (i - (countA - 1) * 0.5f) * 0.68f, 0.02f, (withDefender && !coinA ? (i - 1) * 0.12f : 0));     // 보드 중앙선 위, 코인은 엇갈리지 않게 한 줄
                running++;
                StartCoroutine(Track(RollOne(0, i, coinA, ra[i], pa, new Vector3(-0.5f, 0, -3f), i * 0.3f, new Color(0.45f, 0.82f, 1f)), () => running--));
            }
            if (withDefender)
                for (int i = 0; i < countD; i++)
                {
                    var pd = new Vector3(cD + (i - (countD - 1) * 0.5f) * 0.68f, 0.02f, coinD ? 0f : -(i - 1) * 0.12f);
                    running++;
                    StartCoroutine(Track(RollOne(1, i, coinD, rd[i], pd, new Vector3(0.5f, 0, 3f), i * 0.3f + 0.1f, new Color(1f, 0.55f, 0.45f)), () => running--));
                }
            while (running > 0) yield return null;
            yield return new WaitForSeconds(0.3f);
            result(ra, rd);
        }

        /// <summary>선택한 주사위 강조, 나머지는 흐리게</summary>
        public void Highlight(int side, int index)
        {
            for (int i = 0; i < MaxDice; i++)
            {
                bool on = i == index;
                if (dieTf[side][i] != null) dieTf[side][i].localScale = BaseScale(dieTf[side][i]) * (on ? 1.3f : 0.72f);
                if (labels[side][i] != null)
                {
                    labels[side][i].color = on ? new Color(1f, 0.85f, 0.3f) : new Color(0.45f, 0.45f, 0.5f);
                    labels[side][i].transform.parent.localScale = Vector3.one * LabelScale * (on ? 1.3f : 0.8f);
                }
            }
        }

        readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();
        Vector3 BaseScale(Transform t) => baseScales.TryGetValue(t, out var s) ? s : t.localScale;

        IEnumerator Track(IEnumerator r, Action done)
        {
            yield return r;
            done();
        }

        IEnumerator RollOne(int side, int idx, bool coin, int value, Vector3 land, Vector3 throwFrom, float delay, Color labelColor)
        {
            yield return new WaitForSeconds(delay);
            bool mine = side == humanSide;
            var skin = mine ? Cosmetics.Equipped(coin ? CosmeticKind.Coin : CosmeticKind.Dice) : null;
            var go = coin ? SpawnCoin(mine) : SpawnDie(mine);
            var t = go.transform;
            dieTf[side][idx] = t;
            baseScales[t] = t.localScale;
            Color fx = TrailColor(skin);
            var trail = AddTrail(go, fx, coin ? coinSize * 0.8f : dieSize * 0.9f);
            var shadow = Blob("Shadow", new Color(0, 0, 0, 0.55f), false);
            var shadowR = shadow.GetComponent<Renderer>();
            Vector3 start = land + throwFrom + Vector3.up * 2.4f;
            Quaternion final = FinalRotation(coin, value);
            Vector3 spinAxis = UnityEngine.Random.onUnitSphere;
            float spinSpeed = UnityEngine.Random.Range(900f, 1400f);
            float dur = coin ? 1.05f : 1.1f;
            float restY = coin ? coinSize * 0.06f : dieSize * 0.5f;
            float size = coin ? coinSize : dieSize;
            Quaternion spin = UnityEngine.Random.rotation;
            int bounce = 0;
            for (float el = 0; el < dur; el += Time.deltaTime)
            {
                float u = Mathf.Clamp01(el / dur);
                Vector3 flat = Vector3.Lerp(start, land, 1 - (1 - u) * (1 - u));
                float hop;
                if (u < 0.5f) hop = Mathf.Lerp(2.4f, 0, u / 0.5f) + Mathf.Sin(u / 0.5f * Mathf.PI) * (coin ? 1.4f : 0.7f);
                else
                {
                    // 바닥에 닿은 뒤 두 번 튕긴다 (점점 낮게). 닿을 때마다 충격파
                    float k = (u - 0.5f) / 0.5f;
                    hop = k < 0.55f ? Mathf.Sin(k / 0.55f * Mathf.PI) * 0.34f : Mathf.Sin((k - 0.55f) / 0.45f * Mathf.PI) * 0.1f;
                    int nb = k < 0.55f ? 1 : 2;
                    if (nb != bounce)
                    {
                        bounce = nb;
                        Impact(new Vector3(flat.x, 0.02f, flat.z), fx, nb == 1 ? 1f : 0.55f);
                        if (nb == 1) Sparks(new Vector3(flat.x, 0.05f, flat.z), fx);
                    }
                }
                t.position = new Vector3(flat.x, restY + hop, flat.z);
                spin = Quaternion.AngleAxis(spinSpeed * Time.deltaTime * (1 - u) * (coin ? 1.4f : 1f), coin ? Vector3.right : spinAxis) * spin;
                t.rotation = Quaternion.Slerp(spin, final, Mathf.SmoothStep(0, 1, (u - 0.62f) / 0.38f));
                // 바닥 그림자: 높을수록 크고 옅게
                float sz = size * (1.5f + hop * 0.5f);
                shadow.position = new Vector3(t.position.x, 0.016f, t.position.z);
                shadow.localScale = new Vector3(sz, sz, 1);
                SetColor(shadowR, new Color(0, 0, 0, Mathf.Lerp(0.6f, 0.15f, Mathf.Clamp01(hop / 2.4f))));
                yield return null;
            }
            t.position = new Vector3(land.x, restY, land.z);
            t.rotation = final;
            shadow.position = new Vector3(land.x, 0.016f, land.z);
            shadow.localScale = new Vector3(size * 1.5f, size * 1.5f, 1);
            if (trail != null) trail.emitting = false;
            // 멈춘 자리에 은은한 빛 (결과 강조)
            var halo = Blob("Halo", new Color(fx.r, fx.g, fx.b, 0.7f), true);
            halo.position = new Vector3(land.x, 0.018f, land.z);
            halo.localScale = new Vector3(size * 2.8f, size * 2.8f, 1);
            if (noLabel) yield break;
            string label = coin ? (value >= 5 ? "앞" : "뒤") : value.ToString();
            labels[side][idx] = Label(t.position + Vector3.up * 0.95f + Vector3.forward * 0.15f, label, labelColor);
        }

        /// <summary>결과 면이 위(하늘)를 향하도록</summary>
        Quaternion FinalRotation(bool coin, int value)
        {
            Vector3 n = coin ? (value >= 5 ? Vector3.up : Vector3.down) : DieFaceNormals[Mathf.Clamp(value - 1, 0, 5)];
            return Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * Quaternion.FromToRotation(n, Vector3.up);
        }

        // ══════════════════════════ 절차적 주사위 / 코인 ══════════════════════════

        /// <summary>꾸미기 재질 (아이템마다 캐시). key 예: DICE_JADE, COIN_MOON_h</summary>
        static Material SkinMat(string key, Func<Material> make)
        {
            if (!skinMats.TryGetValue(key, out var m) || m == null) skinMats[key] = m = make();
            return m;
        }

        GameObject SpawnDie(bool mine)
        {
            var go = BuildDie(mine ? Cosmetics.Equipped(CosmeticKind.Dice) : null);
            go.transform.localScale = Vector3.one * dieSize;
            go.transform.SetParent(transform, true);
            spawned.Add(go);
            return go;
        }

        GameObject SpawnCoin(bool mine)
        {
            var go = BuildCoin(mine ? Cosmetics.Equipped(CosmeticKind.Coin) : null);
            go.transform.localScale = Vector3.one * coinSize;
            go.transform.SetParent(transform, true);
            spawned.Add(go);
            return go;
        }

        /// <summary>주사위 1개 (크기 1, 부모 없음). skin = null/기본 → 흑요 황금. 상점 3D 미리보기도 같은 것을 쓴다</summary>
        public static GameObject BuildDie(CosmeticItem skin)
        {
            if (dieMesh == null) dieMesh = BuildDieMesh();
            if (dieMat == null)
            {
                var faces = Resources.Load<Texture2D>(GameAssets.Root + "Models/dice_faces");
                dieMat = GameAssets.LitMat(Color.white, faces != null ? faces : BuildDieTexture(), 0.62f, 0.05f);
            }
            var go = new GameObject("Die");
            go.AddComponent<MeshFilter>().sharedMesh = dieMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = skin == null || skin.IsDefault ? dieMat
                : SkinMat(skin.id, () => GameAssets.LitMat(Color.white, Cosmetics.DiceTex(skin) ?? BuildDieTexture(), 0.62f, 0.05f));
            return go;
        }

        static Texture2D reedTex;
        /// <summary>
        /// 코인 옆테 무늬 (진짜 동전처럼): 둘레를 따라 촘촘한 세로 홈(톱니 · 150줄) + 위아래 가장자리의 테 선.
        /// 원기둥 옆면 UV 는 u = 둘레, v = 높이. 색은 재질 색과 곱해진다.
        /// </summary>
        static Texture2D ReedTex
        {
            get
            {
                if (reedTex != null) return reedTex;
                const int W = 600, H = 32;     // 150줄 × 4px
                reedTex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, name = "CoinReed" };
                float[] ridge = { 1.0f, 0.86f, 0.58f, 0.8f };      // 홈 하나의 밝기 (볼록 → 오목)
                var px = new Color[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float b = ridge[x % 4];
                        if (y <= 2 || y >= H - 3) b = 0.55f;                    // 위아래 테 (깎인 모서리)
                        else if (y == 3 || y == H - 4) b = 1.0f;               // 테 안쪽 하이라이트 선
                        else if (y == 4 || y == H - 5) b = 0.7f;               // 테 선 그늘
                        px[y * W + x] = new Color(b, b, b, 1);
                    }
                reedTex.SetPixels(px);
                reedTex.Apply(true);
                return reedTex;
            }
        }

        /// <summary>코인 1개 (지름 1, 앞면 = 로컬 위). skin = null/기본 → 황금 별</summary>
        public static GameObject BuildCoin(CosmeticItem skin)
        {
            if (coinMat == null)
            {
                coinMat = GameAssets.LitMat(new Color(1f, 0.78f, 0.3f), ReedTex, 0.55f, 0.25f);   // 금속도를 높이면 비출 환경이 없어 옆테가 까맣게 비어 보인다
                var h = Resources.Load<Texture2D>(GameAssets.Root + "Models/coin_heads");
                var tl = Resources.Load<Texture2D>(GameAssets.Root + "Models/coin_tails");
                headsMat = GameAssets.UnlitMat(Color.white, h != null ? h : BuildCoinFace(true), true);
                tailsMat = GameAssets.UnlitMat(Color.white, tl != null ? tl : BuildCoinFace(false), true);
            }
            bool def = skin == null || skin.IsDefault;
            var go = new GameObject("Coin");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(1f, 0.05f, 1f);        // 두께 0.1
            // 옆테 색은 코인 그림 테두리 색에 맞춘다
            body.GetComponent<Renderer>().sharedMaterial = def ? coinMat : SkinMat(skin.id + "_rim", () => GameAssets.LitMat(Color.Lerp(Cosmetics.CoinRim(skin), Color.white, 0.15f), ReedTex, 0.55f, 0.25f));
            foreach (bool heads in new[] { true, false })
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(f.GetComponent<Collider>());
                f.transform.SetParent(go.transform, false);
                f.transform.localPosition = new Vector3(0, heads ? 0.0505f : -0.0505f, 0);
                f.transform.localRotation = Quaternion.Euler(heads ? 90 : -90, 0, 0);
                f.transform.localScale = Vector3.one * 1.005f;          // 옆테 끝까지 덮는다
                f.GetComponent<Renderer>().sharedMaterial = def ? (heads ? headsMat : tailsMat)
                    : SkinMat(skin.id + (heads ? "_h" : "_t"), () => GameAssets.UnlitMat(Color.white, Cosmetics.CoinTex(skin, heads) ?? BuildCoinFace(heads), true));
            }
            return go;
        }

        // ── 던지기 연출: 궤적 · 그림자 · 착지 충격파 · 불꽃 ──

        static Material fxMat, shadowMat;

        /// <summary>궤적·충격파 색: 기본 = 황금, 꾸미기 = 아이템 대표색</summary>
        static Color TrailColor(CosmeticItem skin) => skin == null || skin.IsDefault ? new Color(1f, 0.8f, 0.35f) : skin.Tint;

        static void SetColor(Renderer r, Color c)
        {
            r.material.color = c;
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
        }

        static TrailRenderer AddTrail(GameObject go, Color c, float width)
        {
            if (fxMat == null) fxMat = GameAssets.ParticleMat();
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = fxMat;
            tr.time = 0.38f;
            tr.minVertexDistance = 0.04f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            tr.widthMultiplier = width;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.55f), 0), new GradientColorKey(c, 0.5f) },
                      new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        /// <summary>바닥에 눕힌 둥근 판 (additive = 빛, 아니면 그림자)</summary>
        Transform Blob(string name, Color c, bool additive)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(transform, true);
            q.transform.rotation = Quaternion.Euler(90, 0, 0);
            if (fxMat == null) fxMat = GameAssets.ParticleMat();
            if (shadowMat == null) shadowMat = GameAssets.UnlitMat(Color.black, GameAssets.GlowTexture, true);
            var r = q.GetComponent<Renderer>();
            r.material = new Material(additive ? fxMat : shadowMat);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetColor(r, c);
            spawned.Add(q);
            return q.transform;
        }

        /// <summary>착지 충격파: 퍼지며 사라지는 빛 고리</summary>
        void Impact(Vector3 pos, Color c, float power)
        {
            var ring = Blob("Impact", new Color(c.r, c.g, c.b, 0.9f * power), true);
            ring.position = pos;
            StartCoroutine(ImpactFx(ring, c, power));
        }

        /// <summary>첫 착지 불꽃: 사방으로 튀었다 떨어지는 작은 빛 알갱이</summary>
        void Sparks(Vector3 pos, Color c)
        {
            if (fxMat == null) fxMat = GameAssets.ParticleMat();
            var go = new GameObject("Sparks");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(c, Color.white, 0.5f), c);
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = 0.08f;
            sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(c, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fxMat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            spawned.Add(go);
        }

        IEnumerator ImpactFx(Transform ring, Color c, float power)
        {
            var r = ring.GetComponent<Renderer>();
            for (float t = 0; t < 0.4f && ring != null; t += Time.deltaTime)
            {
                float k = t / 0.4f;
                float s = Mathf.Lerp(0.3f, 2f * power, 1 - (1 - k) * (1 - k));
                ring.localScale = new Vector3(s, s, 1);
                SetColor(r, new Color(c.r, c.g, c.b, (1 - k) * 0.9f * power));
                yield return null;
            }
            if (ring != null) { spawned.Remove(ring.gameObject); Destroy(ring.gameObject); }
        }

        /// <summary>
        /// 둥근 모서리 주사위 메시 (크기 1). 면마다 격자를 깔고 안쪽 상자에서 반지름 R 만큼 부풀려
        /// 모서리·꼭짓점이 매끈하게 둥글다. 면마다 3×2 아틀라스의 해당 눈 칸을 매핑
        /// </summary>
        static Mesh BuildDieMesh()
        {
            const float R = 0.14f, In = 0.5f - R;
            // 모서리 쪽을 촘촘하게 (둥근 부분 해상도)
            float[] ts = { 0f, 0.012f, 0.03f, 0.055f, 0.09f, 0.14f, 0.25f, 0.5f, 0.75f, 0.86f, 0.91f, 0.945f, 0.97f, 0.988f, 1f };
            int N = ts.Length;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int v = 1; v <= 6; v++)
            {
                Vector3 n = DieFaceNormals[v - 1];
                Vector3 up = Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up;
                Vector3 u = Vector3.Cross(up, n).normalized;
                Vector3 w = Vector3.Cross(n, u).normalized;
                Vector3 c = n * 0.5f;
                int col = (v - 1) % 3, row = (v - 1) / 3;
                float x0 = col / 3f + 0.004f, x1 = (col + 1) / 3f - 0.004f, y0 = row / 2f + 0.006f, y1 = (row + 1) / 2f - 0.006f;
                int b = verts.Count;
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < N; j++)
                    {
                        Vector3 p = c + u * (ts[i] - 0.5f) + w * (ts[j] - 0.5f);
                        var inner = new Vector3(Mathf.Clamp(p.x, -In, In), Mathf.Clamp(p.y, -In, In), Mathf.Clamp(p.z, -In, In));
                        Vector3 d = p - inner;
                        Vector3 nn = d.sqrMagnitude > 1e-8f ? d.normalized : n;
                        verts.Add(inner + nn * R);
                        norms.Add(nn);
                        uvs.Add(new Vector2(Mathf.Lerp(x0, x1, ts[i]), Mathf.Lerp(y0, y1, ts[j])));
                    }
                // 감김 방향: 면 가운데 칸의 법선이 면 법선과 같은 쪽이 되게
                int mid = b + (N / 2) * N + N / 2;
                Vector3 a0 = verts[mid], a1 = verts[mid + N], a2 = verts[mid + N + 1];
                bool front = Vector3.Dot(Vector3.Cross(a1 - a0, a2 - a0), n) > 0;
                for (int i = 0; i < N - 1; i++)
                    for (int j = 0; j < N - 1; j++)
                    {
                        int q0 = b + i * N + j, q1 = b + (i + 1) * N + j, q2 = b + (i + 1) * N + j + 1, q3 = b + i * N + j + 1;
                        if (front) tris.AddRange(new[] { q0, q1, q2, q0, q2, q3 });
                        else tris.AddRange(new[] { q0, q2, q1, q0, q3, q2 });
                    }
            }
            var m = new Mesh { name = "StrikerZDie" };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>흑요석 바탕 + 금테 + 금색 눈 (칸 순서: 1 2 3 / 4 5 6, 아래 줄부터)</summary>
        static Texture2D BuildDieTexture()
        {
            const int S = 256;
            var tex = new Texture2D(S * 3, S * 2, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[S * 3 * S * 2];
            const float L = 0.27f, C = 0.5f, R = 0.73f;
            var pips = new[]
            {
                new[] { new Vector2(C, C) },
                new[] { new Vector2(L, R), new Vector2(R, L) },
                new[] { new Vector2(L, R), new Vector2(C, C), new Vector2(R, L) },
                new[] { new Vector2(L, L), new Vector2(L, R), new Vector2(R, L), new Vector2(R, R) },
                new[] { new Vector2(L, L), new Vector2(L, R), new Vector2(C, C), new Vector2(R, L), new Vector2(R, R) },
                new[] { new Vector2(L, L), new Vector2(L, C), new Vector2(L, R), new Vector2(R, L), new Vector2(R, C), new Vector2(R, R) },
            };
            Color baseA = new Color(0.05f, 0.045f, 0.075f), baseB = new Color(0.13f, 0.11f, 0.17f);
            Color gold = new Color(1f, 0.8f, 0.32f), goldDark = new Color(0.55f, 0.38f, 0.1f);
            for (int v = 0; v < 6; v++)
            {
                int ox = (v % 3) * S, oy = (v / 3) * S;
                float pr = v == 0 ? 0.13f : 0.085f;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float fx = (x + 0.5f) / S, fy = (y + 0.5f) / S;
                        float edge = Mathf.Min(Mathf.Min(fx, 1 - fx), Mathf.Min(fy, 1 - fy));
                        Color col = Color.Lerp(baseB, baseA, Mathf.Clamp01(Vector2.Distance(new Vector2(fx, fy), new Vector2(0.4f, 0.6f)) * 1.6f));
                        // 금테 (가장자리 베벨)
                        if (edge < 0.055f) col = Color.Lerp(goldDark, gold, Mathf.Clamp01(edge / 0.055f));
                        else if (edge < 0.075f) col = Color.Lerp(gold, col, (edge - 0.055f) / 0.02f);
                        foreach (var p in pips[v])
                        {
                            float d = Vector2.Distance(new Vector2(fx, fy), p);
                            if (d < pr * 1.6f) col = Color.Lerp(col, gold * 0.6f, Mathf.Clamp01(1 - (d - pr) / (pr * 0.6f)) * 0.35f);   // 글로우
                            if (d < pr)
                            {
                                float k = d / pr;
                                col = Color.Lerp(new Color(1f, 0.95f, 0.7f), v == 0 ? new Color(1f, 0.35f, 0.2f) : gold, Mathf.Clamp01(k * 1.2f));
                                if (k > 0.85f) col = Color.Lerp(col, goldDark, (k - 0.85f) / 0.15f);
                            }
                        }
                        col.a = 1;
                        px[(oy + y) * S * 3 + ox + x] = col;
                    }
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>코인 면: 앞 = 칠각 별, 뒤 = 동심원</summary>
        static Texture2D BuildCoinFace(bool heads)
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            Color gold = new Color(1f, 0.8f, 0.32f), dark = new Color(0.62f, 0.42f, 0.12f), light = new Color(1f, 0.95f, 0.7f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / S - 0.5f, (y + 0.5f) / S - 0.5f) * 2f;
                    float r = p.magnitude;
                    Color col = new Color(0, 0, 0, 0);
                    if (r <= 1f)
                    {
                        col = Color.Lerp(gold, dark, r * 0.6f);
                        if (r > 0.86f) col = dark;
                        if (heads)
                        {
                            float ang = Mathf.Atan2(p.y, p.x) + Mathf.PI / 2f;
                            float seg = Mathf.PI * 2f / 7f;
                            float a = Mathf.Repeat(ang, seg) / seg;
                            float starR = Mathf.Lerp(0.3f, 0.72f, Mathf.Abs(a - 0.5f) * 2f < 1 ? 1 - Mathf.Abs(a - 0.5f) * 2f : 0);
                            if (r < starR) col = Color.Lerp(light, gold, r / starR);
                        }
                        else if (Mathf.Abs(r - 0.55f) < 0.05f || Mathf.Abs(r - 0.3f) < 0.04f) col = light;
                        col.a = 1;
                    }
                    px[y * S + x] = col;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        Text Label(Vector3 pos, string text, Color c)
        {
            var rt = UIKit.Rect("Label", labelCanvas.transform);
            rt.sizeDelta = new Vector2(300, 140);
            rt.position = pos;
            rt.localScale = Vector3.one * LabelScale;
            if (Cam != null) rt.rotation = Cam.transform.rotation;
            var bg = UIKit.Img(rt, "Bg", new Color(0, 0, 0, 0.75f), GameAssets.Rounded, true);
            bg.rectTransform.Anchor(0.2f, 0.1f, 0.8f, 0.9f);
            var t = UIKit.Txt(rt, "T", text, 90, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill();
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.Outline(new Color(0, 0, 0, 0.9f), 3f);
            spawned.Add(rt.gameObject);
            return t;
        }

        /// <summary>빛/어둠 속성 전투 코인 1개 (side 0 = 공격측, 1 = 방어측). 다음 RollBattle 이 치운다</summary>
        public IEnumerator FlipCoin(int side, bool heads)
        {
            Sfx.Play("dice", 0.7f);
            // 보드 중앙선(z 0) 위에 좌우 대칭으로 (예전엔 위아래로 엇갈려 대각선이 됐다)
            var land = new Vector3(side == 0 ? -1.8f : 1.8f, 0.02f, 0f);
            yield return RollOne(side, MaxDice - 1, true, heads ? 6 : 1, land, new Vector3(side == 0 ? -0.5f : 0.5f, 0, side == 0 ? -3f : 3f), 0f,
                side == 0 ? new Color(0.45f, 0.82f, 1f) : new Color(1f, 0.55f, 0.45f));
            yield return new WaitForSeconds(0.25f);
        }

        /// <summary>대전 시작 필드 코인 토스: 내 코인(장착한 꾸미기 코인)을 보드 가운데에 크게 던진다</summary>
        public IEnumerator FlipCoinCenter(bool heads)
        {
            Sfx.Play("dice", 0.7f);
            float keep = coinSize;
            coinSize = keep * 2.2f;
            noLabel = true;     // 결과(앞면/뒷면)는 HUD 가 크게 보여 준다 — 코인 위 "앞/뒤" 글자는 겹쳐서 생략
            yield return RollOne(humanSide, MaxDice - 1, true, heads ? 6 : 1, BoardCenter, new Vector3(0, 0, -3f), 0f, new Color(1f, 0.85f, 0.4f));
            noLabel = false;
            coinSize = keep;
            yield return new WaitForSeconds(0.35f);
        }

        public void Clear()
        {
            foreach (var g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            baseScales.Clear();
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < MaxDice; i++) { dieTf[s][i] = null; labels[s][i] = null; }
        }
    }
}
