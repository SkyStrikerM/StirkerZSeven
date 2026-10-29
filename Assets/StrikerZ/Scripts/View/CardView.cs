using UnityEngine;
using UnityEngine.EventSystems;

namespace StrikerZ
{
    /// <summary>3D 카드: 얇은 박스 + 앞면 월드 캔버스(VARCO 프레임) + 뒷면(VARCO 카드백)</summary>
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public const float W = 0.75f, H = 1.05f, T = 0.014f;

        public CardInstance card;
        public bool hoverLift;      // 패 카드: 호버 시 들어올림
        public bool hovered;

        Vector3 tPos;
        Quaternion tRot = Quaternion.identity;
        float tScale = 1f;
        public Vector3 lunge;       // 공격 연출 오프셋
        /// <summary>연출용 크기 배율 (파괴 소멸: 1 → 0, 묘지에 닿으면 다시 1). 목표 크기에 곱한다</summary>
        public float fxScale = 1f;
        /// <summary>전투 무대 오프셋: 싸우는 두 소환수를 보드 가운데 위로 띄운다 (BoardView.StageBegin/StageEnd)</summary>
        public Vector3 stage;
        public float stageScale = 1f;
        public Vector3 TargetPos => tPos;

        Transform visual;
        BoxCollider col;
        Canvas canvas;
        CardFace face;
        Material glowMat;
        Transform glowTf;
        Color glowColor;
        float glowPulse;
        int shownAtk = int.MinValue, shownHp = int.MinValue;

        static Material bodyMat, backMat;
        static readonly System.Collections.Generic.Dictionary<string, Material> sleeveMats = new System.Collections.Generic.Dictionary<string, Material>();

        /// <summary>장착한 슬리브 뒷면 재질 (슬리브마다 하나씩 캐시)</summary>
        static Material SleeveMat()
        {
            var item = Cosmetics.Equipped(CosmeticKind.Sleeve);
            if (item.IsDefault) return backMat;
            if (!sleeveMats.TryGetValue(item.id, out var m) || m == null)
                sleeveMats[item.id] = m = GameAssets.UnlitMat(Color.white, Cosmetics.SleeveTex(item), true);
            return m;
        }

        public static CardView Create(CardInstance c, Vector3 pos, Transform parent)
        {
            var go = new GameObject("Card_" + c.data.name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var v = go.AddComponent<CardView>();
            v.card = c;
            c.view = v;
            v.tPos = pos;
            v.tRot = Quaternion.Euler(0, 0, 180);
            go.transform.rotation = v.tRot;
            v.Build();
            return v;
        }

        void Build()
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(W, T * 3f, H);

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            if (bodyMat == null) bodyMat = GameAssets.LitMat(new Color(0.08f, 0.07f, 0.1f), null, 0.6f, 0.2f);
            if (backMat == null)
            {
                var backTex = GameAssets.Tex("card_back");
                backMat = GameAssets.UnlitMat(Color.white, backTex, true);
                if (backTex == null) backMat.color = new Color(0.15f, 0.1f, 0.3f);
            }

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(visual, false);
            body.transform.localScale = new Vector3(W * 0.985f, T, H * 0.985f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(back.GetComponent<Collider>());
            back.name = "Back";
            back.transform.SetParent(visual, false);
            back.transform.localPosition = new Vector3(0, -T * 0.5f - 0.0015f, 0);
            back.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            back.transform.localScale = new Vector3(W, H, 1);
            // 내 카드는 장착한 슬리브, 상대 카드는 기본 뒷면
            back.GetComponent<Renderer>().sharedMaterial = card.owner != null && card.owner.isHuman ? SleeveMat() : backMat;

            // 글로우 (하이라이트)
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(glow.GetComponent<Collider>());
            glow.name = "Glow";
            glowTf = glow.transform;
            glowTf.SetParent(transform, false);
            glowTf.localPosition = new Vector3(0, -0.004f, 0);
            glowTf.localRotation = Quaternion.Euler(90, 0, 0);
            glowTf.localScale = new Vector3(W * 1.7f, H * 1.5f, 1);
            glowMat = GameAssets.UnlitMat(new Color(1, 1, 1, 0), GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(glowMat, true);
            glow.GetComponent<Renderer>().sharedMaterial = glowMat;
            glow.SetActive(false);
        }

        void EnsureFace()
        {
            if (face != null) return;
            var cgo = new GameObject("FrontCanvas", typeof(RectTransform));
            cgo.transform.SetParent(visual, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)cgo.transform;
            rt.sizeDelta = new Vector2(750, 1050);
            rt.localScale = Vector3.one * 0.001f;
            rt.localPosition = new Vector3(0, T * 0.5f + 0.0015f, 0);
            rt.localRotation = Quaternion.Euler(90, 0, 0);
            face = CardFace.Build(rt, 750, 1050);
            face.root.anchorMin = face.root.anchorMax = new Vector2(0.5f, 0.5f);
            face.root.anchoredPosition = Vector2.zero;
            face.glow.gameObject.SetActive(false);
            face.Set(card.data);
            face.SetFinish(card.finish);
            if (dim) SetDim(true);
        }

        bool dim;
        UnityEngine.UI.Image dimImg;

        /// <summary>어둡게 덮기 (이번 T 이미 쓴 원소 카드)</summary>
        public void SetDim(bool on)
        {
            dim = on;
            if (face == null) return;
            if (dimImg == null)
            {
                if (!on) return;
                dimImg = UIKit.Img(canvas.transform, "Dim", new Color(0, 0, 0, 0.5f));
                dimImg.rectTransform.Fill();
                dimImg.raycastTarget = false;
            }
            dimImg.transform.SetAsLastSibling();
            dimImg.gameObject.SetActive(on);
        }

        public void SetTarget(Vector3 pos, Quaternion rot, bool faceUp, float scale = 1f)
        {
            tPos = pos;
            tRot = faceUp ? rot : rot * Quaternion.Euler(0, 0, 180);
            tScale = scale;
            if (faceUp) EnsureFace();
        }

        public void Snap()
        {
            transform.position = tPos;
            transform.rotation = tRot;
            transform.localScale = Vector3.one * tScale * fxScale;
        }

        public void RefreshStats(GameManager g)
        {
            if (face == null || !card.IsMonster) return;
            int atk = g.EffectiveAtk(card);
            int hp = card.zone == Zone.Slot ? g.CurHp(card) : card.data.hp;
            if (atk == shownAtk && hp == shownHp) return;
            shownAtk = atk;
            shownHp = hp;
            face.Set(card.data, atk, hp);
        }

        /// <summary>null = 끔</summary>
        public void SetGlow(Color? c)
        {
            if (c == null)
            {
                glowColor.a = 0;
                if (glowPulse <= 0) glowTf.gameObject.SetActive(false);
                return;
            }
            glowColor = c.Value;
            glowTf.gameObject.SetActive(true);
        }

        public void Pulse(Color c) { glowPulse = 1f; glowColor = c; glowTf.gameObject.SetActive(true); }

        // ── 사용 가능 테두리: 카드 둘레의 굵은 띠 + 바깥 번짐, 빛 두 줄기가 둘레를 돌며 반짝인다 ──
        GameObject frameGo;
        Mesh frameMesh;
        Color[] frameCols;
        Vector3[] frameEdge;
        Transform[] sparks;
        Color frameColor;
        int frameMode = -1;
        static Material frameMat, sparkMat;

        /// <summary>null = 끔. 굵기는 설정(GameSettings.playableFrame)을 따른다</summary>
        public void SetFrame(Color? c)
        {
            int mode = GameSettings.Data.playableFrame;
            if (c == null || mode <= 0)
            {
                if (frameGo != null) frameGo.SetActive(false);
                return;
            }
            if (frameMode != mode) BuildFrame(mode);
            frameColor = c.Value;
            frameGo.SetActive(true);
        }

        void BuildFrame(int mode)
        {
            frameMode = mode;
            if (frameGo == null)
            {
                frameGo = new GameObject("PlayableFrame");
                frameGo.transform.SetParent(transform, false);
                frameGo.transform.localPosition = new Vector3(0, T * 0.5f + 0.004f, 0);
                frameMesh = new Mesh();
                frameMesh.MarkDynamic();
                frameGo.AddComponent<MeshFilter>().sharedMesh = frameMesh;
                if (frameMat == null)
                {
                    frameMat = GameAssets.ParticleMat();
                    frameMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
                    frameMat.mainTexture = Texture2D.whiteTexture;
                    GameAssets.MakeTransparent(frameMat, false);   // 알파 블렌드: 밝은 카드 위에서도 색이 진하게
                    if (frameMat.HasProperty("_Cull")) frameMat.SetFloat("_Cull", 0);
                    frameMat.renderQueue = 3100;          // 앞면 캔버스 위에
                }
                if (sparkMat == null)
                {
                    sparkMat = GameAssets.ParticleMat();
                    sparkMat.renderQueue = 3101;
                }
                frameGo.AddComponent<MeshRenderer>().sharedMaterial = frameMat;
                sparks = new Transform[2];
                for (int i = 0; i < sparks.Length; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.name = "Spark";
                    q.transform.SetParent(frameGo.transform, false);
                    q.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    q.GetComponent<Renderer>().sharedMaterial = sparkMat;
                    sparks[i] = q.transform;
                }
            }
            bool thick = mode >= 2;
            float inset = thick ? 0.022f : 0.01f, band = thick ? 0.03f : 0.012f, bloom = thick ? 0.06f : 0.035f;
            // 둘레를 같은 간격으로 나눈다 (i / n = 둘레 위치)
            const float step = 0.025f;
            int sx = Mathf.Max(2, Mathf.RoundToInt(W / step)), sz = Mathf.Max(2, Mathf.RoundToInt(H / step));
            int n = 2 * (sx + sz);
            var unit = new Vector2[n];
            int k = 0;
            for (int i = 0; i < sx; i++) unit[k++] = new Vector2(-1 + 2f * i / sx, -1);
            for (int i = 0; i < sz; i++) unit[k++] = new Vector2(1, -1 + 2f * i / sz);
            for (int i = 0; i < sx; i++) unit[k++] = new Vector2(1 - 2f * i / sx, 1);
            for (int i = 0; i < sz; i++) unit[k++] = new Vector2(-1, 1 - 2f * i / sz);
            // 고리 4개: [안쪽 ─ 띠 바깥] = 진한 띠, [띠 바깥(복제) ─ 번짐 끝] = 바깥으로 옅어지는 빛
            float[] hx = { W * 0.5f - inset, W * 0.5f + band, W * 0.5f + band, W * 0.5f + band + bloom };
            float[] hz = { H * 0.5f - inset, H * 0.5f + band, H * 0.5f + band, H * 0.5f + band + bloom };
            var verts = new Vector3[n * 4];
            for (int r = 0; r < 4; r++)
                for (int i = 0; i < n; i++) verts[r * n + i] = new Vector3(unit[i].x * hx[r], 0, unit[i].y * hz[r]);
            var tris = new int[n * 2 * 6];
            int t = 0;
            foreach (int r in new[] { 0, 2 })
                for (int i = 0; i < n; i++)
                {
                    int a = r * n + i, b = r * n + (i + 1) % n, c = (r + 1) * n + i, d = (r + 1) * n + (i + 1) % n;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            frameMesh.Clear();
            frameMesh.vertices = verts;
            frameMesh.triangles = tris;
            frameCols = new Color[verts.Length];
            frameMesh.colors = frameCols;
            frameMesh.RecalculateBounds();
            frameEdge = new Vector3[n];
            float mid = (band - inset) * 0.5f;
            for (int i = 0; i < n; i++)
                frameEdge[i] = new Vector3(unit[i].x * (W * 0.5f + mid), 0.001f, unit[i].y * (H * 0.5f + mid));
        }

        static float Peak(float f, float h)
        {
            float d = Mathf.Abs(f - h);
            d = Mathf.Min(d, 1f - d);
            return Mathf.Exp(-d * d / 0.0018f);
        }

        void UpdateFrame()
        {
            if (frameGo == null || !frameGo.activeSelf) return;
            float tm = Time.unscaledTime;
            float h0 = tm * 0.3f % 1f, h1 = (h0 + 0.5f) % 1f;
            float pulse = 0.85f + 0.15f * Mathf.Sin(tm * 4.5f);
            int n = frameEdge.Length;
            for (int i = 0; i < n; i++)
            {
                float hl = Peak((float)i / n, h0) + Peak((float)i / n, h1);
                var c = Color.Lerp(frameColor * pulse, Color.white, Mathf.Clamp01(hl * 0.9f));
                c.a = 1f;
                frameCols[i] = c;
                frameCols[n + i] = c;
                frameCols[2 * n + i] = new Color(c.r, c.g, c.b, 0.55f * pulse + 0.3f * hl);
                frameCols[3 * n + i] = new Color(c.r, c.g, c.b, 0f);
            }
            frameMesh.colors = frameCols;
            float size = frameMode >= 2 ? 0.2f : 0.13f;
            for (int s = 0; s < sparks.Length; s++)
            {
                float h = s == 0 ? h0 : h1;
                sparks[s].localPosition = frameEdge[Mathf.FloorToInt(h * n) % n];
                float tw = size * (0.8f + 0.35f * Mathf.Sin(tm * 11f + s * 2.1f));
                sparks[s].localScale = new Vector3(tw, tw, 1);
            }
        }

        void Update()
        {
            UpdateFrame();
            float k = 1f - Mathf.Exp(-Time.deltaTime * 11f);
            if (card != null && card.zone != Zone.Slot && (stage != Vector3.zero || stageScale != 1f)) { stage = Vector3.zero; stageScale = 1f; }   // 칸을 떠나면 무대에서 내려온다
            Vector3 pos = tPos + lunge + stage;
            Quaternion rot = tRot;
            if (stageScale > 1.01f && Camera.main != null)
            {
                // 전투 무대: 카메라 정면을 보도록 세운다 (앞면 = 로컬 +y 가 카메라 쪽, 카드 위쪽 = 로컬 +z 가 하늘 쪽). 아직 뒷면이면 뒷면을 보인다
                var toCam = (Camera.main.transform.position - (tPos + stage)).normalized;
                var top = Vector3.ProjectOnPlane(Vector3.up, toCam).normalized;
                rot = Quaternion.LookRotation(top, toCam);
                if (card != null && card.faceDown) rot *= Quaternion.Euler(0, 0, 180);
            }
            float scale = tScale * fxScale * stageScale;
            bool lifted = hovered && hoverLift;
            if (lifted)
            {
                pos += new Vector3(0, 0.55f, 0.35f);
                scale *= 1.35f;
            }
            // 들어올린 카드가 포인터에서 벗어나 떨리지 않도록 콜라이더를 아래로 늘림
            col.size = lifted ? new Vector3(W, T * 3f, H + 0.9f) : new Vector3(W, T * 3f, H);
            col.center = lifted ? new Vector3(0, 0, -0.45f) : Vector3.zero;
            transform.position = Vector3.Lerp(transform.position, pos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, k);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, k);

            // 앞면이 카메라를 향할 때만 앞면 캔버스 렌더
            if (canvas != null && Camera.main != null)
            {
                Vector3 toCam = Camera.main.transform.position - transform.position;
                canvas.enabled = Vector3.Dot(transform.up, toCam) > 0f;
            }

            if (glowTf.gameObject.activeSelf)
            {
                float a = glowColor.a * (0.75f + 0.25f * Mathf.Sin(Time.time * 5f));
                if (glowPulse > 0)
                {
                    glowPulse -= Time.deltaTime * 1.6f;
                    a = Mathf.Max(a, glowPulse);
                    if (glowPulse <= 0 && glowColor.a <= 0.01f) glowTf.gameObject.SetActive(false);
                }
                glowMat.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);
                glowMat.SetColor("_BaseColor", glowMat.color);
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true;
            if (HUD.I != null) HUD.I.OnCardHover(card, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            if (HUD.I != null) HUD.I.OnCardHover(card, false);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (HUD.I == null) return;
            if (e.button == PointerEventData.InputButton.Right) HUD.I.OnCardRightClicked(card);
            else HUD.I.OnCardClicked(card);
        }
    }
}
