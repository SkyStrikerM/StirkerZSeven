using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>3D 보드: VARCO 테이블 + 플레이매트 + 7칸 슬롯 + 센터 필드 + 카드 배치</summary>
    public class BoardView : MonoBehaviour
    {
        public const float SlotSpacing = 0.95f;
        public const float RowZ = 1.25f;
        public const float SideX = 4.15f;
        /// <summary>영웅 아래 원소 카드 줄 (묘지·덱 오른쪽)</summary>
        public const float ElemX = 5.0f;

        public Camera cam;
        public Transform cardRoot;
        readonly List<CardView> views = new List<CardView>();
        readonly List<Renderer> slotMarkers = new List<Renderer>();
        Light fieldLight;
        Vector3 camBasePos;
        Quaternion camBaseRot;
        float shake;

        public static readonly Vector3 CamPos = new Vector3(0, 8.4f, -6.2f);
        public static readonly Vector3 CamLook = new Vector3(0, 0, -0.45f);

        public void Build(Camera camera)
        {
            cam = camera;
            cardRoot = new GameObject("Cards").transform;
            cardRoot.SetParent(transform, false);

            BuildTable();
            BuildMarkers();
            BuildLights();
            BuildBackdrop();
            BuildEnvironment();
        }

        public void FocusCamera()
        {
            cam.transform.position = CamPos;
            cam.transform.LookAt(CamLook);
            cam.fieldOfView = 42;
            camBasePos = cam.transform.position;
            camBaseRot = cam.transform.rotation;
        }

        void BuildTable()
        {
            var t = GameAssets.SpawnModel("table", transform, 0.45f, 0.2f);
            if (t != null)
            {
                FitModel(t.transform, 13.2f, topAtZero: true);
            }
            else
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "TableSlab";
                slab.transform.SetParent(transform, false);
                slab.transform.localScale = new Vector3(12.4f, 0.6f, 7.4f);
                slab.transform.localPosition = new Vector3(0, -0.3f, 0);
                slab.GetComponent<Renderer>().sharedMaterial = GameAssets.LitMat(new Color(0.06f, 0.05f, 0.08f), null, 0.7f, 0.3f);
            }

            var mat = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mat.name = "Playmat";
            Destroy(mat.GetComponent<Collider>());
            mat.transform.SetParent(transform, false);
            mat.transform.localPosition = new Vector3(0, 0.005f, 0);
            mat.transform.localRotation = Quaternion.Euler(90, 0, 0);
            mat.transform.localScale = new Vector3(10.9f, 6.1f, 1);
            var tex = GameAssets.Tex("playmat");
            playmat = mat.GetComponent<Renderer>();
            playmat.sharedMaterial = GameAssets.LitMat(tex != null ? Color.white : new Color(0.1f, 0.09f, 0.13f), tex, 0.55f, 0.1f);
            defaultPlaymat = tex;

            // 기본 문양 레이어: 룬 문자 · 금색 연성진 · 칸 선 · 중앙 문장 (Textures/playmat_overlay, Tools/fields/make_overlay.py)
            // 필드 배경이 바뀌어도 항상 그 위에 겹쳐 그린다
            var otex = GameAssets.Tex("playmat_overlay");
            if (otex != null)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Quad);
                o.name = "PlaymatOverlay";
                Destroy(o.GetComponent<Collider>());
                o.transform.SetParent(transform, false);
                o.transform.localPosition = new Vector3(0, 0.0065f, 0);
                o.transform.localRotation = Quaternion.Euler(90, 0, 0);
                o.transform.localScale = mat.transform.localScale;
                var r = o.GetComponent<Renderer>();
                r.sharedMaterial = GameAssets.UnlitMat(new Color(0.95f, 0.95f, 0.95f, 1f), otex, true);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // ══════════════════════════ 필드 테마 (플레이매트 + 테이블 바깥 환경) ══════════════════════════
        // 필드 카드가 센터에 놓이면 Resources/StrikerZ/Textures/Fields/playmat_{id}(선 없는 배경) · env_{id} 로 바뀐다 (VARCO, Tools/fields).
        // 룬·연성진 문양(PlaymatOverlay)은 그 위에 항상 남는다.
        // 파일이 없는 필드는 기본 플레이매트 + 환경 없음(어두운 배경).

        Renderer playmat, environment;
        Texture defaultPlaymat;
        string themeId = "";
        Coroutine themeFade;
        static readonly Color EnvTint = new Color(0.8f, 0.8f, 0.84f);

        void BuildEnvironment()
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "FieldEnvironment";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0, -1.6f, 4f);     // 테이블 아래 · 카메라에 보이는 바깥 영역 전부를 덮는 바닥
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(64f, 36f, 1f);
            environment = q.GetComponent<Renderer>();
            environment.sharedMaterial = GameAssets.UnlitMat(EnvTint);
            environment.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.SetActive(false);
        }

        /// <summary>센터 필드에 맞춰 플레이매트·바깥 환경 교체 (같은 필드면 무시). null = 기본</summary>
        public void ApplyFieldTheme(CardData field)
        {
            string id = field != null ? field.id : "";
            if (id == themeId || playmat == null) return;
            themeId = id;
            var pm = field != null ? GameAssets.Tex("Fields/playmat_" + id) : null;
            var env = field != null ? GameAssets.Tex("Fields/env_" + id) : null;
            if (themeFade != null) StopCoroutine(themeFade);
            if (!isActiveAndEnabled) { SetTheme(pm, env, 1f); return; }
            themeFade = StartCoroutine(FadeTheme(pm, env));
        }

        void SetTheme(Texture pm, Texture env, float bright)
        {
            var m = playmat.sharedMaterial;
            var t = pm != null ? pm : defaultPlaymat;
            m.SetTexture("_BaseMap", t); m.mainTexture = t;
            var c = Color.white * bright; c.a = 1;
            m.SetColor("_BaseColor", c); m.color = c;
            environment.gameObject.SetActive(env != null);
            if (env != null)
            {
                var em = environment.sharedMaterial;
                em.SetTexture("_BaseMap", env); em.mainTexture = env;
                var ec = EnvTint * bright; ec.a = 1;
                em.SetColor("_BaseColor", ec); em.color = ec;
            }
        }

        /// <summary>어두워졌다가 새 필드 그림으로 밝아지는 전환</summary>
        IEnumerator FadeTheme(Texture pm, Texture env)
        {
            var m = playmat.sharedMaterial;
            var em = environment.sharedMaterial;
            Color start = m.GetColor("_BaseColor"), envStart = em.GetColor("_BaseColor");
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = 1f - t / 0.3f;
                var c = start * Mathf.Lerp(0.15f, 1f, k); c.a = 1; m.SetColor("_BaseColor", c);
                var ec = envStart * Mathf.Lerp(0.1f, 1f, k); ec.a = 1; em.SetColor("_BaseColor", ec);
                yield return null;
            }
            for (float t = 0; t < 0.7f; t += Time.deltaTime)
            {
                SetTheme(pm, env, Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0, 1, t / 0.7f)));
                yield return null;
            }
            SetTheme(pm, env, 1f);
            themeFade = null;
        }

        /// <summary>모델을 가로 폭 width 에 맞추고 윗면을 y=0 에 둔다</summary>
        public static void FitModel(Transform t, float width, bool topAtZero)
        {
            var b = Bounds(t);
            float w = Mathf.Max(b.size.x, b.size.z);
            if (w < 1e-4f) return;
            t.localScale *= width / w;
            b = Bounds(t);
            Vector3 target = t.parent != null ? t.parent.position : Vector3.zero;
            float y = topAtZero ? target.y - b.max.y : target.y - b.center.y;
            t.position += new Vector3(target.x - b.center.x, y, target.z - b.center.z);
        }

        public static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        void BuildMarkers()
        {
            var markerMat = GameAssets.UnlitMat(new Color(1f, 0.82f, 0.4f, 0.16f), null, true);
            GameAssets.MakeTransparent(markerMat, true);
            foreach (int side in new[] { 0, 1 })
            {
                float z = side == 0 ? -RowZ : RowZ;
                for (int i = 0; i < PlayerState.SlotCount; i++)
                    slotMarkers.Add(Marker($"Slot{side}_{i}", new Vector3(SlotX(i), 0.008f, z), markerMat, 1f));
                Marker($"Hero{side}", new Vector3(-SideX, 0.008f, z), markerMat, 1.12f);
                Marker($"Deck{side}", new Vector3(SideX, 0.008f, side == 0 ? -1.85f : 1.85f), markerMat, 1f);
                Marker($"Grave{side}", new Vector3(SideX, 0.008f, side == 0 ? -0.68f : 0.68f), markerMat, 1f);
            }
            var cmat = GameAssets.UnlitMat(new Color(0.8f, 0.6f, 1f, 0.3f), null, true);
            GameAssets.MakeTransparent(cmat, true);
            Marker("Center", new Vector3(0, 0.008f, 0), cmat, 1.18f);
        }

        Renderer Marker(string name, Vector3 pos, Material m, float scale)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            q.transform.localPosition = pos;
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(CardView.W + 0.1f, CardView.H + 0.1f, 1) * scale;
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            return r;
        }

        void BuildLights()
        {
            var sun = new GameObject("KeyLight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.85f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.SetParent(transform, false);
            sun.transform.rotation = Quaternion.Euler(58, -25, 0);

            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.45f, 0.5f, 1f);
            rim.intensity = 0.45f;
            rim.transform.SetParent(transform, false);
            rim.transform.rotation = Quaternion.Euler(35, 160, 0);

            fieldLight = new GameObject("FieldLight").AddComponent<Light>();
            fieldLight.type = LightType.Point;
            fieldLight.range = 7f;
            fieldLight.intensity = 3f;
            fieldLight.transform.SetParent(transform, false);
            fieldLight.transform.localPosition = new Vector3(0, 1.6f, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.26f, 0.34f);
        }

        void BuildBackdrop()
        {
            var tex = GameAssets.Tex("title_bg");
            if (tex == null) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Backdrop";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0, 2f, 34f);
            q.transform.localScale = new Vector3(96f, 54f, 1f);
            q.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            q.GetComponent<Renderer>().sharedMaterial = GameAssets.UnlitMat(new Color(0.55f, 0.55f, 0.65f), tex);
        }

        // ══════════════════════════ 위치 ══════════════════════════

        public static float SlotX(int i) => (i - (PlayerState.SlotCount - 1) * 0.5f) * SlotSpacing;
        public static float Row(PlayerState p) => p.index == 0 ? -RowZ : RowZ;
        public Vector3 SlotPos(PlayerState p, int i) => new Vector3(SlotX(i), 0.03f, Row(p));
        public Vector3 HeroPos(PlayerState p) => new Vector3(-SideX, 0.03f, Row(p));
        public Vector3 DeckPos(PlayerState p, int k) => new Vector3(SideX, 0.02f + k * 0.006f, p.index == 0 ? -1.85f : 1.85f);
        public Vector3 GravePos(PlayerState p, int k) => new Vector3(SideX, 0.02f + k * 0.006f, p.index == 0 ? -0.68f : 0.68f);
        public static readonly Vector3 CenterPos = new Vector3(0, 0.03f, 0);
        public static readonly Vector3 ResolvePos = new Vector3(0, 2.3f, -1.6f);

        public void CreateView(CardInstance c, Vector3 pos)
        {
            var v = CardView.Create(c, pos, cardRoot);
            views.Add(v);
        }

        public void DestroyView(CardInstance c)
        {
            if (c.view == null) return;
            views.Remove(c.view);
            Destroy(c.view.gameObject);
            c.view = null;
        }

        public void ClearCards()
        {
            foreach (var v in views) if (v != null) Destroy(v.gameObject);
            views.Clear();
        }

        /// <summary>게임 상태에서 모든 카드의 목표 위치 계산</summary>
        public void Layout(GameManager g)
        {
            var flat = Quaternion.identity;
            foreach (var p in g.players)
            {
                if (p == null) continue;
                bool human = p.isHuman;

                for (int i = 0; i < p.deck.Count; i++)
                    Set(p.deck[i], DeckPos(p, i), flat, false);

                for (int i = 0; i < p.grave.Count; i++)
                    Set(p.grave[i], GravePos(p, i), flat, true);

                if (p.hero != null) Set(p.hero, HeroPos(p), flat, true, 1.1f);

                for (int i = 0; i < PlayerState.SlotCount; i++)
                {
                    var c = p.slots[i];
                    if (c == null) continue;
                    bool up = !c.faceDown || g.IsRevealed(c);
                    // 자신의 뒷면 카드는 살짝 들어 보이게(내용 확인은 호버)
                    Set(c, SlotPos(p, i), flat, up);
                }

                int n = p.hand.Count;
                for (int i = 0; i < n; i++)
                {
                    float mid = (n - 1) * 0.5f;
                    float spacing = human ? Mathf.Min(0.78f, 7.2f / Mathf.Max(1, n)) : Mathf.Min(0.5f, 5.5f / Mathf.Max(1, n));
                    float x = (i - mid) * spacing;
                    if (human)
                    {
                        float ang = (i - mid) * -2.2f;
                        var pos = new Vector3(x, 1.05f + i * 0.004f - Mathf.Abs(i - mid) * 0.02f, -3.1f + Mathf.Abs(i - mid) * -0.015f);
                        var rot = Quaternion.Euler(-33, 0, 0) * Quaternion.Euler(0, ang, 0);
                        Set(p.hand[i], pos, rot, true);
                        p.hand[i].view.hoverLift = true;
                    }
                    else
                    {
                        var pos = new Vector3(x, 0.35f + i * 0.004f, 3.2f);
                        Set(p.hand[i], pos, Quaternion.Euler(20, 180, 0), false, 0.85f);
                        p.hand[i].view.hoverLift = false;
                    }
                }
                // 영웅 아래 원소 — 묘지·덱 오른쪽에 세로로 겹쳐 (위 카드의 이름 띠가 보이게, 이번 T 쓴 원소는 어둡게)
                var elems = g.ElementCards(p);
                if (elems != null)
                {
                    int m = elems.Count;
                    float step = m > 1 ? Mathf.Min(0.22f, 1.25f / (m - 1)) : 0f;
                    float z0 = p.index == 0 ? -0.95f : 2.2f;
                    for (int k = 0; k < m; k++)
                    {
                        var c = elems[k];
                        bool spent = g.ElementSpent(c);
                        Set(c, new Vector3(ElemX, 0.03f + k * 0.012f, z0 - k * step), flat, true, 0.9f);
                        c.view.hoverLift = false;
                        c.view.SetDim(spent);
                    }
                }
                foreach (var c in p.deck) if (c.view != null) c.view.hoverLift = false;
                foreach (var c in p.grave) if (c.view != null) c.view.hoverLift = false;
                foreach (var c in p.slots) if (c != null && c.view != null) c.view.hoverLift = false;
            }

            if (g.center != null)
            {
                Set(g.center, CenterPos, flat, true, 1.1f);
                if (g.center.view != null) g.center.view.hoverLift = false;
                var ec = CardData.ElementColor(g.center.data.element);
                fieldLight.color = ec;
            }
            ApplyFieldTheme(g.center?.data);

            // 발동 중인 마법
            foreach (var p in g.players)
            {
                if (p == null) continue;
                foreach (var v in views)
                    if (v != null && v.card.zone == Zone.Resolving)
                    {
                        v.SetTarget(ResolvePos, Quaternion.Euler(-38, 0, 0), true, 1.7f);
                        v.hoverLift = false;
                    }
            }

            // 하이라이트
            foreach (var v in views)
            {
                if (v == null) continue;
                v.RefreshStats(g);
                var c = v.card;
                // 사용 가능한 카드: 굵고 반짝이는 테두리 (설정에서 굵게 · 얇게 · 끔)
                Color? frame = null;
                if (c.owner == g.Human && !g.IsChoiceOption(c))
                {
                    if (g.HumanBattleOpen && g.CanAttackNow(c)) frame = new Color(1f, 0.5f, 0.2f);
                    else if (g.HumanBattleOpen && g.CanActivateInBattle(c)) frame = new Color(0.35f, 0.9f, 1f);
                    else if (g.IsHumanMain && c.zone == Zone.Hand && Playable(g, c)) frame = new Color(0.15f, 1f, 0.4f);
                }
                v.SetFrame(frame);
                if (frame != null && GameSettings.Data.playableFrame > 0) v.SetGlow(null);   // 테두리가 있으면 바닥 번짐은 끈다
                else if (g.IsChoiceOption(c)) v.SetGlow(new Color(1f, 0.85f, 0.2f, 0.95f));
                else if (g.HumanBattleOpen && c.owner == g.Human && g.CanAttackNow(c)) v.SetGlow(new Color(1f, 0.42f, 0.2f, 0.85f));     // 배틀: 공격 가능 (드래그)
                else if (g.HumanBattleOpen && c.owner == g.Human && g.CanActivateInBattle(c)) v.SetGlow(new Color(0.35f, 0.9f, 1f, 0.8f)); // 배틀: t/half 발동 가능 (클릭)
                else if (g.IsHumanMain && c.owner == g.Human && c.zone == Zone.Hand && Playable(g, c)) v.SetGlow(new Color(0.3f, 1f, 0.6f, 0.55f));
                else if (c.zone == Zone.Resolving) v.SetGlow(new Color(0.6f, 0.4f, 1f, 0.9f));
                else if (g.BeaconRevealed(c)) v.SetGlow(new Color(1f, 0.4f, 0.2f, 0.6f));
                else v.SetGlow(null);
            }
        }

        static bool Playable(GameManager g, CardInstance c)
        {
            switch (c.data.type)
            {
                case CardType.Monster: return g.CanSummon(c, out _);
                case CardType.Magic: return g.CanSet(c);
                case CardType.Field: return g.CanPlayField(c);
                default: return false;
            }
        }

        void Set(CardInstance c, Vector3 pos, Quaternion rot, bool faceUp, float scale = 1f)
        {
            if (c.view == null) CreateView(c, pos + Vector3.up * 2);
            c.view.SetTarget(pos, rot, faceUp, scale);
        }

        // ══════════════════════════ 연출 ══════════════════════════

        public void Pulse(CardInstance c)
        {
            if (c.view != null) c.view.Pulse(new Color(1f, 0.9f, 0.5f, 0f));
        }

        /// <summary>전투 무대 열: 보드 왼쪽 절반 가운데(내 소환수) · 오른쪽 절반 가운데(상대 소환수). 주사위·코인도 같은 열 아래에 구른다 (DiceRoller.SideX)</summary>
        public const float StageX = 2.4f;
        /// <summary>전투 무대 자리: 양쪽 줄 사이 위쪽에 카메라 정면을 보게 세워 크게 띄운다 (CardView 가 빌보드 회전), 가운데(필드 카드 위)엔 VS</summary>
        public static Vector3 StagePos(bool human) => CenterPos + new Vector3(human ? -StageX : StageX, 1.75f, 0.3f);
        public static Vector3 VsPos => CenterPos + new Vector3(0, 1.9f, 0.3f);

        /// <summary>전투 시작: 싸우는 두 소환수를 가운데 위로 띄워 마주 세운다 (내 쪽 왼쪽 · 상대 오른쪽), 바닥엔 진영 색 마법진</summary>
        public void StageBegin(CardInstance a, CardInstance d)
        {
            foreach (var c in new[] { a, d })
            {
                if (c?.view == null) continue;
                bool human = c.owner != null && c.owner.isHuman;
                var p = StagePos(human);
                c.view.stage = p - c.view.TargetPos;
                c.view.stageScale = 2.3f;
                if (CardFx.I != null)
                {
                    var col = human ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.45f, 0.35f);
                    CardFx.I.Glyph(new Vector3(p.x, 0.03f, p.z), CardFx.Style.Neutral, col, 3.4f, 1.2f);
                    CardFx.I.Ring(new Vector3(p.x, 0.03f, p.z), col, 3.0f, 0.5f, true);
                }
            }
            if (CardFx.I != null) CardFx.I.ShowVs(VsPos);
        }

        public void StageEnd(CardInstance a, CardInstance d)
        {
            foreach (var c in new[] { a, d })
            {
                if (c?.view == null) continue;
                c.view.stage = Vector3.zero;
                c.view.stageScale = 1f;
            }
            if (CardFx.I != null) CardFx.I.HideVs();
        }

        public IEnumerator AttackLunge(CardInstance a, Vector3 target)
        {
            if (a.view == null) yield break;
            var v = a.view;
            Vector3 from = v.transform.position;
            Vector3 dir = (target - from);
            dir.y = 0;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3.2f;
                float s = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                v.lunge = dir * 0.55f * s + Vector3.up * 0.6f * s;
                yield return null;
            }
            v.lunge = Vector3.zero;
            Sfx.Play("attack");
            Burst(target + Vector3.up * 0.1f, new Color(1f, 0.8f, 0.4f), 1.6f);
            Shake(0.12f);
        }

        public void SummonFx(CardInstance c)
        {
            if (c.view == null) return;
            Vector3 p = c.zone == Zone.Slot && c.slot >= 0 ? SlotPos(c.owner, c.slot) : c.view.transform.position;
            if (c.zone == Zone.Center) p = CenterPos;
            if (c.zone == Zone.Hero) p = HeroPos(c.owner);
            if (CardFx.I != null) { CardFx.I.Burst(p, CardFx.StyleOf(c), 2.2f, 0.5f); CardFx.I.Ring(p, CardFx.StyleOf(c), 2.0f, 0.5f); }
            else Burst(p, CardData.ElementColor(c.data.element), 2.2f);
        }

        public void DestroyFx(CardInstance c)
        {
            if (c.view == null) return;
            if (CardFx.I != null) CardFx.I.Burst(c.view.transform.position, new Color(1f, 0.35f, 0.25f), 2f, 0.45f);
            else Burst(c.view.transform.position, new Color(1f, 0.25f, 0.2f), 2f);
        }

        /// <summary>색 폭발: CardFx 가 있으면 VARCO 폭발 그림, 없으면(메뉴 미리보기 등) 빛 원판</summary>
        public void Burst(Vector3 pos, Color c, float size)
        {
            if (CardFx.I != null) CardFx.I.Burst(pos, c, size, 0.5f);
            else StartCoroutine(BurstRoutine(pos, c, size));
        }

        IEnumerator BurstRoutine(Vector3 pos, Color c, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.position = pos + Vector3.up * 0.05f;
            q.transform.rotation = Quaternion.Euler(90, 0, 0);
            var m = GameAssets.UnlitMat(c, GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(m, true);
            q.GetComponent<Renderer>().sharedMaterial = m;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.2f;
                float s = Mathf.Lerp(0.3f, size, 1 - (1 - t) * (1 - t));
                q.transform.localScale = new Vector3(s, s, 1);
                var col = new Color(c.r, c.g, c.b, (1 - t) * 1.2f);
                m.color = col;
                m.SetColor("_BaseColor", col);
                yield return null;
            }
            Destroy(m);
            Destroy(q);
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        void LateUpdate()
        {
            if (cam == null || shake <= 0) return;
            shake = Mathf.Max(0, shake - Time.deltaTime * 0.9f);
            cam.transform.position = camBasePos + Random.insideUnitSphere * shake * 0.25f;
            if (shake <= 0) cam.transform.position = camBasePos;
        }
    }
}
