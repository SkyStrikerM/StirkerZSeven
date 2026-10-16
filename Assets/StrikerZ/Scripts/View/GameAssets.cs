using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>Resources/StrikerZ 아래의 VARCO 생성 에셋 로더 (없으면 절차적 대체물)</summary>
    public static class GameAssets
    {
        public const string Root = "StrikerZ/";
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 32);
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static Texture2D Tex(string name)
        {
            if (textures.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>(Root + "Textures/" + name);
            textures[name] = t;
            return t;
        }

        public static Sprite Sprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (sprites.TryGetValue(path, out var s)) return s;
            var tex = Resources.Load<Texture2D>(path);
            s = tex != null ? UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            sprites[path] = s;
            return s;
        }

        public static Sprite TexSprite(string name) => Sprite(Root + "Textures/" + name);

        /// <summary>프로필 아바타: Resources/StrikerZ/Avatars/{id}</summary>
        public static Sprite Avatar(string id) => string.IsNullOrEmpty(id) ? null : Sprite(Root + "Avatars/" + id);

        public static Sprite FrameSprite(string name) => Sprite(Root + "Textures/Frames/" + name);

        /// <summary>
        /// 카드 아트: data.art → Resources/StrikerZ/Art/Cards/{카드 id} → 속성/마법 기본 아트.
        /// 새 카드는 id 이름으로 이미지를 Art/Cards 에 넣기만 하면 된다.
        /// </summary>
        public static Sprite ArtFor(CardData d)
        {
            if (!string.IsNullOrEmpty(d.art))
            {
                var custom = Sprite(d.art);
                if (custom != null) return custom;
            }
            if (!string.IsNullOrEmpty(d.id))
            {
                var byId = Sprite(Root + "Art/Cards/" + d.id);
                if (byId != null) return byId;
            }
            if (d.type == CardType.Magic) return Sprite(Root + "Art/magic");
            return Sprite(Root + "Art/elem_" + d.element.ToString().ToLower());
        }

        /// <summary>오버프레임 일러스트: Art/Cards/{id}_full.png (카드 전체 비율 5:7). 있으면 오버프레임으로 표시</summary>
        public static Sprite FullArtFor(CardData d) => string.IsNullOrEmpty(d.id) ? null : Sprite(Root + "Art/Cards/" + d.id + "_full");

        static readonly Dictionary<Texture, RenderTexture> blurred = new Dictionary<Texture, RenderTexture>();

        /// <summary>흐림 유리용 블러 텍스처 (GPU 다운샘플, 텍스처당 1회 생성 후 캐시)</summary>
        public static Texture Blurred(Texture src)
        {
            if (src == null) return null;
            if (blurred.TryGetValue(src, out var cached) && cached != null && cached.IsCreated()) return cached;
            int w = src.width, h = src.height;
            RenderTexture prev = null;
            while (w > 48)
            {
                w = Mathf.Max(1, w / 2);
                h = Mathf.Max(1, h / 2);
                var rt = new RenderTexture(w, h, 0) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                Graphics.Blit(prev != null ? (Texture)prev : src, rt);
                if (prev != null) prev.Release();
                prev = rt;
            }
            blurred[src] = prev;
            return prev;
        }

        public static GameObject Model(string name) => Resources.Load<GameObject>(Root + "Models/" + name);

        static readonly Dictionary<string, Material> modelMats = new Dictionary<string, Material>();

        /// <summary>VARCO 모델 생성 + 추출해 둔 albedo/normal 텍스처 적용 (없으면 null)</summary>
        public static GameObject SpawnModel(string name, Transform parent, float smooth = 0.55f, float metal = 0.3f)
        {
            var prefab = Model(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent);
            go.name = name + "(VARCO)";
            if (!modelMats.TryGetValue(name, out var mat) || mat == null)
            {
                var albedo = Resources.Load<Texture2D>(Root + "Models/" + name + "_albedo");
                var normal = Resources.Load<Texture2D>(Root + "Models/" + name + "_normal");
                if (albedo != null)
                {
                    mat = new Material(Template(normal != null ? "LitNormal" : "Lit", "Universal Render Pipeline/Lit"));
                    mat.SetColor("_BaseColor", Color.white);
                    mat.SetTexture("_BaseMap", albedo);
                    mat.mainTexture = albedo;
                    if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                    mat.SetFloat("_Smoothness", smooth);
                    mat.SetFloat("_Metallic", metal);
                }
                modelMats[name] = mat;
            }
            if (mat != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                    r.sharedMaterials = arr;
                }
            return go;
        }

        // ── 머티리얼 ──
        // 빌드에서 URP 셰이더(및 투명 variant)가 제외되지 않도록 Resources 의 템플릿 머티리얼을 복제해서 사용
        static readonly Dictionary<string, Material> templates = new Dictionary<string, Material>();

        static Material Template(string name, string fallbackShader)
        {
            if (templates.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Material>(Root + "Materials/" + name);
            if (t == null)
            {
                var sh = Shader.Find(fallbackShader) ?? Shader.Find("Sprites/Default");
                t = new Material(sh);
            }
            templates[name] = t;
            return t;
        }

        public static Material ParticleMat()
        {
            var m = new Material(Template("ParticleAdditive", "Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", GlowTexture);
            m.mainTexture = GlowTexture;
            MakeTransparent(m, true);
            return m;
        }

        public static Material LitMat(Color c, Texture tex = null, float smooth = 0.5f, float metal = 0f)
        {
            var m = new Material(Template("Lit", "Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            return m;
        }

        public static Material UnlitMat(Color c, Texture tex = null, bool transparent = false)
        {
            var m = new Material(transparent ? Template("UnlitTransparent", "Universal Render Pipeline/Unlit") : Template("Unlit", "Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            if (transparent) MakeTransparent(m, false);
            return m;
        }

        public static void MakeTransparent(Material m, bool additive)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            // 알파 채널도 섞는다 (기본값이면 투명한 부분이 아래 물체의 알파까지 0 으로 덮어써서,
            // RenderTexture 로 찍는 3D 미리보기에서 그 자리가 뚫려 보인다 — 코인 옆테 사라짐)
            if (m.HasProperty("_SrcBlendAlpha"))
            {
                m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            }
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (additive && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
        }

        // ── 절차적 스프라이트 ──
        static Sprite rounded, outline, glow, circle;

        static float RoundedSdf(int x, int y, int size, float r)
        {
            float h = size / 2f - r;
            float px = Mathf.Abs(x + 0.5f - size / 2f) - h;
            float py = Mathf.Abs(y + 0.5f - size / 2f) - h;
            return new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - r;
        }

        /// <summary>
        /// 절차 생성 스프라이트 (n×n, 가운데 피벗, 가장자리 Clamp). pixel(x, y) = 그 칸의 색.
        /// border 를 주면 9-slice (FullRect).
        /// </summary>
        public static Sprite ProcSprite(int n, System.Func<int, int, Color> pixel, Vector4? border = null)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            var rect = new Rect(0, 0, n, n);
            return border == null
                ? UnityEngine.Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f))
                : UnityEngine.Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border.Value);
        }

        /// <summary>흰색 + 알파 (절차 스프라이트용. 색은 Image.color 로 입힌다)</summary>
        public static Color White(float a) => new Color(1, 1, 1, a);

        /// <summary>9-slice 둥근 사각형 외곽선 (2.5px)</summary>
        public static Sprite RoundedOutline
        {
            get
            {
                if (outline != null) return outline;
                const int size = 64, r = 18;
                return outline = ProcSprite(size, (x, y) =>
                {
                    float d = RoundedSdf(x, y, size, r);
                    return White(Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 3f));
                }, new Vector4(r, r, r, r));
            }
        }

        /// <summary>9-slice 둥근 사각형 (테두리 포함)</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64, r = 18;
                return rounded = ProcSprite(size, (x, y) => White(Mathf.Clamp01(0.5f - RoundedSdf(x, y, size, r))), new Vector4(r, r, r, r));
            }
        }

        /// <summary>부드러운 원형 글로우</summary>
        public static Sprite Glow
        {
            get
            {
                if (glow != null) return glow;
                const int size = 128;
                return glow = ProcSprite(size, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    return White(Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f));
                });
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int size = 96;
                return circle = ProcSprite(size, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    return White(Mathf.Clamp01(size / 2f - d));
                });
            }
        }

        static Sprite finishBorder, sparkle;

        /// <summary>가공 전용 테두리 아트: Resources/StrikerZ/Textures/Frames/finish_seven · finish_strikerz</summary>
        public static Sprite FinishFrame(CardFinish f) =>
            f == CardFinish.Seven ? FrameSprite("finish_seven") : f == CardFinish.StrikerZ ? FrameSprite("finish_strikerz") : null;

        static readonly Dictionary<Element, Sprite> arrows = new Dictionary<Element, Sprite>();

        /// <summary>
        /// 공격 화살표 (VARCO, 속성별): Resources/StrikerZ/Textures/Fx/arrow_{속성}.png — 오른쪽을 향한 1024x256, 투명 배경.
        /// 9-슬라이스: 왼쪽 꼬리 8% · 오른쪽 화살촉 30% 는 고정, 가운데 몸통만 늘어난다 (Tools/fx/process_arrow.py).
        /// </summary>
        public static Sprite AttackArrow(Element e)
        {
            if (arrows.TryGetValue(e, out var s)) return s;
            var tex = Resources.Load<Texture2D>(Root + "Textures/Fx/arrow_" + e.ToString().ToLower());
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(tex.width * 0.08f, 0, tex.width * 0.30f, 0));
            }
            arrows[e] = s;
            return s;
        }

        /// <summary>속성 프레임 유지용 얇은 가공 테두리 (VARCO): Frames/finish_rim_seven · finish_rim_strikerz (Tools/frames/process_rim.py)</summary>
        public static Sprite FinishRim(CardFinish f) =>
            f == CardFinish.Seven ? FrameSprite("finish_rim_seven") : f == CardFinish.StrikerZ ? FrameSprite("finish_rim_strikerz") : null;
        static Texture2D secretRare;

        /// <summary>
        /// 시크릿 레어 포일: 45° 사선의 가는 평행선 + 선마다 반대 대각선을 따라 무지개 색상 변화.
        /// 256px 타일(선 간격 4px = 64줄, 카드 폭에 약 150줄)이며 가로·세로로 이음매 없이 반복된다. 선은 가늘고 옅게.
        /// </summary>
        public static Texture2D SecretRare
        {
            get
            {
                if (secretRare != null) return secretRare;
                const int n = 256, period = 4;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Repeat(x + y, period) / period;                 // 선을 가로지르는 위치 0..1
                        float line = Mathf.Pow(Mathf.Max(0f, Mathf.Cos((d - 0.5f) * Mathf.PI * 2f)), 12f);  // 아주 가는 밝은 선
                        float hue = Mathf.Repeat((x - y) / (float)n, 1f);                // 선을 따라 무지개
                        float band = 0.65f + 0.35f * Mathf.Sin((x - y) / (float)n * Mathf.PI * 6f); // 넓은 반사 줄무늬
                        var c = Color.HSVToRGB(hue, 0.42f, 1f);
                        c.a = Mathf.Clamp01(line * band * 0.8f);
                        px[y * n + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(true);
                return secretRare = tex;
            }
        }

        /// <summary>가공 테두리: 둥근 사각 외곽선 (9-slice, 흰색 → 색은 Image.color)</summary>
        public static Sprite FinishBorder
        {
            get
            {
                if (finishBorder != null) return finishBorder;
                const int n = 64, r = 14, th = 8;
                return finishBorder = ProcSprite(n, (x, y) =>
                {
                    // 둥근 사각형까지의 부호 거리
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - n / 2f) - (n / 2f - r), 0);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - n / 2f) - (n / 2f - r), 0);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - r;          // 0 = 바깥 경계, 음수 = 안쪽
                    float a = Mathf.Clamp01(-d) * Mathf.Clamp01(d + th + 1);
                    // 안쪽 모서리로 갈수록 살짝 어둡게 → 금속 베벨 느낌
                    float bevel = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(-d / th));
                    return new Color(bevel, bevel, bevel, a);
                }, new Vector4(16, 16, 16, 16));
            }
        }

        /// <summary>네 갈래 반짝이 (팩 개봉 · 가공 광택 공용)</summary>
        public static Sprite Sparkle
        {
            get
            {
                if (sparkle != null) return sparkle;
                const int n = 64;
                return sparkle = ProcSprite(n, (x, y) =>
                {
                    float u = Mathf.Abs((x + 0.5f) / n * 2 - 1), v = Mathf.Abs((y + 0.5f) / n * 2 - 1);
                    float a = Mathf.Exp(-u * 16) * Mathf.Exp(-v * 2.4f) + Mathf.Exp(-v * 16) * Mathf.Exp(-u * 2.4f) + Mathf.Exp(-(u * u + v * v) * 20) * 0.9f;
                    a *= Mathf.Clamp01((1 - Mathf.Max(u, v)) * 6);
                    return White(Mathf.Clamp01(a));
                });
            }
        }

        static Texture2D glowTex;
        public static Texture2D GlowTexture => glowTex != null ? glowTex : (glowTex = Glow.texture);
    }
}
