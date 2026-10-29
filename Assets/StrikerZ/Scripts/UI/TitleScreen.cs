using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>타이틀 화면 (VARCO 키아트 + 로고 + 회전하는 3D 엠블럼) · 영웅 선택 · 필드 선택 · 규칙</summary>
    public class TitleScreen : MonoBehaviour
    {
        /// <summary>게임 시작 버튼 → 메인 허브</summary>
        public Action onStart;

        public static readonly Vector3 SetOrigin = new Vector3(200, 0, 0);
        // 3D 심볼: 작게, 화면 오른쪽 아래 구석
        static readonly Vector3 EmblemPos = new Vector3(0f, -1.85f, 6f);     // 마스터 듀얼 타이틀의 삼각형 자리: 로고 바로 아래 가운데
        const float EmblemSize = 1.05f;
        // 카메라는 배경 판(40×22.5, 24.5 앞)이 화면에 꼭 차도록: 그림 좌표 = 화면 좌표
        static readonly Vector3 CamPos = new Vector3(0, 0, -2.5f);
        const float CamFov = 49.3f;

        Camera cam;
        Transform set, emblem;
        RectTransform root, mainMenu, heroPanel, rulesPanel, fieldPanel;
        CardFace fieldPreview;
        CardData selectedField;
        UIKit.Btn fieldConfirm;
        Text startHint;
        Action<CardData> onHeroPicked;
        Action<CardData> onFieldPicked;
        float t;

        public void Build(Camera camera, Action<CardData> heroPicked)
        {
            cam = camera;
            onHeroPicked = heroPicked;
            Build3D();
            BuildUI();
        }

        void Build3D()
        {
            set = new GameObject("TitleSet").transform;
            set.position = SetOrigin;

            // 운석을 뺀 배경 (운석은 UI 에서 하나씩 떨어진다). 없으면 예전 키아트
            var tex = GameAssets.Tex("title_bg_clean") ?? GameAssets.Tex("title_bg");
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bg.GetComponent<Collider>());
            bg.name = "KeyArt";
            bg.transform.SetParent(set, false);
            bg.transform.localPosition = new Vector3(0, 0, 22);
            bg.transform.localScale = new Vector3(40f, 22.5f, 1);
            bg.GetComponent<Renderer>().sharedMaterial = GameAssets.UnlitMat(tex != null ? Color.white : new Color(0.05f, 0.04f, 0.1f), tex);
            keyArt = bg.transform;
            BuildStarDrift(set);

            var holder = new GameObject("Emblem").transform;
            holder.SetParent(set, false);
            holder.localPosition = EmblemPos;
            emblem = holder;
            var m = GameAssets.SpawnModel("emblem", holder, 0.72f, 0.4f);
            if (m != null)
            {
                var b = BoardView.Bounds(m.transform);
                float s = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (s > 1e-4f) m.transform.localScale *= EmblemSize / s;
                b = BoardView.Bounds(m.transform);
                m.transform.position -= b.center - holder.position;
            }
            else
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(holder, false);
                c.GetComponent<Renderer>().sharedMaterial = GameAssets.LitMat(UIKit.Gold, null, 0.9f, 1f);
            }

            var key = new GameObject("TitleKey").AddComponent<Light>();
            key.type = LightType.Point;
            key.range = 12;
            key.intensity = 6;
            key.color = new Color(1f, 0.9f, 0.75f);
            key.transform.SetParent(holder, false);                  // 심볼을 따라다니며 정면 위에서 비춘다 (로고처럼 밝은 금색)
            key.transform.localPosition = new Vector3(0.6f, 1.3f, -2.2f);
            key.range = 9;
            key.intensity = 10;
            var fill = new GameObject("TitleFill").AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 7;
            fill.intensity = 5;
            fill.color = new Color(1f, 0.93f, 0.8f);
            fill.transform.SetParent(holder, false);
            fill.transform.localPosition = new Vector3(-1.3f, -0.5f, -1.8f);
            string[] hex = { "#ff4d3a", "#3aa0ff", "#3ae07a", "#ffd83a", "#a04dff" };
            for (int i = 0; i < hex.Length; i++)
            {
                ColorUtility.TryParseHtmlString(hex[i], out var c);
                var l = new GameObject("Accent" + i).AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 5;
                l.intensity = 3;
                l.color = c;
                l.transform.SetParent(holder, false);
                float a = i / (float)hex.Length * Mathf.PI * 2;
                l.transform.localPosition = new Vector3(Mathf.Cos(a) * 2.4f, Mathf.Sin(a) * 2.4f, -1.2f);
            }

            BuildSparkles(holder);
        }

        Transform keyArt;
        /// <summary>배경 판 확대 배율 (1 이상이라 가장자리가 비지 않는다). 운석 층도 같은 배율로 따라간다</summary>
        float BgZoom => 1.018f + 0.018f * Mathf.Sin(Time.unscaledTime * 0.16f);

        /// <summary>우주가 살아 있는 느낌: 배경 앞을 천천히 흘러가며 반짝이는 별 가루</summary>
        void BuildStarDrift(Transform parent)
        {
            var go = new GameObject("StarDrift");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 1.5f, 17f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            main.maxParticles = 420;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.88f, 1f), new Color(1f, 0.8f, 1f));
            var em = ps.emission;
            em.rateOverTime = 30;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(38f, 17f, 2f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, -0.12f);           // 은하수가 천천히 왼쪽으로 흘러간다
            vel.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0.35f, 0.45f), new GradientAlphaKey(0.9f, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1.2f), new Keyframe(0.55f, 0.7f), new Keyframe(0.8f, 1.15f), new Keyframe(1, 0.5f)));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GameAssets.ParticleMat();
        }

        void BuildSparkles(Transform parent)
        {
            var go = new GameObject("Sparkles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, 0.5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(0.6f, 0.7f, 1f));
            var em = ps.emission;
            em.rateOverTime = 45;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 2.4f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = GameAssets.ParticleMat();
        }

        void BuildUI()
        {
            var go = new GameObject("TitleUI", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = UIKit.ScreenCanvas(go, 20);
            root = (RectTransform)go.transform;

            // 메인 메뉴
            mainMenu = UIKit.Rect("Main", root);
            mainMenu.Fill();
            // 배경 위로 7원소 운석이 하나씩 떨어진다 (로고·문구보다 뒤)
            BuildMeteors(mainMenu);
            // 화면 아무 곳이나 클릭하면 시작 (종료 버튼만 따로)
            var catcher = UIKit.Img(mainMenu, "ClickToStart", new Color(0, 0, 0, 0));
            catcher.rectTransform.Fill();
            catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(StartGame);
            var logo = UIKit.Img(mainMenu, "Logo", Color.white, GameAssets.TexSprite("logo"));
            logo.preserveAspect = true;
            logo.rectTransform.Place(new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1060, 588), new Vector2(0.5f, 0.5f));   // 마스터 듀얼 로고와 같은 자리·크기 (가로 가운데, 위에서 40%)
            if (logo.sprite == null)
            {
                var t = UIKit.Txt(logo.transform, "LogoText", "StrikerZ\nSEVEN", 120, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Fill();
            }

            // 가운데 아래: 마스터 듀얼처럼 무난한 흰 글씨 안내 문구 (은은하게 깜빡임)
            startHint = UIKit.Txt(mainMenu, "StartHint", HintText, 30, new Color(0.9f, 0.9f, 0.94f), TextAnchor.MiddleCenter);
            startHint.rectTransform.Place(new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(1000, 60), new Vector2(0.5f, 0.5f));
            startHint.Shadow(new Color(0, 0, 0, 0.85f), 2);
            // 오른쪽 아래는 3D 심볼 자리라 종료는 오른쪽 위로
            var quit = UIKit.Button(mainMenu, "종료", Quit, new Color(0.1f, 0.1f, 0.12f, 0.9f), 22, UIKit.Muted);
            quit.rt.Place(new Vector2(1, 1), new Vector2(-24, -24), new Vector2(150, 50), new Vector2(1, 1));

            BuildHeroSelect();
            BuildRules();
            BuildFieldSelect();
        }

        void BuildHeroSelect()
        {
            var bg = UIKit.Img(root, "HeroSelect", new Color(0.02f, 0.01f, 0.05f, 0.9f));
            bg.raycastTarget = true;
            heroPanel = bg.rectTransform;
            heroPanel.Fill();
            var title = UIKit.Txt(heroPanel, "Title", "영웅(리더) 선택", 60, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1200, 90));
            title.Shadow();
            var sub = UIKit.Txt(heroPanel, "Sub", "영웅의 능력은 매치에 한 번만 사용할 수 있으며, 능력이 강할수록 HP가 낮습니다", 26, UIKit.Ink);
            sub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(1600, 40));

            var row = UIKit.Rect("Row", heroPanel);
            row.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1800, 640));
            var h = row.gameObject.AddComponent<GridLayoutGroup>();
            h.cellSize = new Vector2(206, 288);
            h.spacing = new Vector2(16, 22);
            h.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            h.constraintCount = 8;
            h.childAlignment = TextAnchor.MiddleCenter;
            foreach (var hero in CardList.Heroes.Where(StoryData.HeroAvailable))
            {
                var d = hero;
                var cell = UIKit.Rect("Hero_" + d.name, row);
                cell.sizeDelta = new Vector2(206, 288);
                var face = CardFace.Build(cell, 206, 288, 0.282f);
                face.root.Fill();
                face.Set(d);
                cell.HitArea();
                var btn = cell.gameObject.AddComponent<Button>();
                btn.targetGraphic = face.frame;
                btn.onClick.AddListener(() => PickHero(d));
                var trig = cell.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => { cell.localScale = Vector3.one * 1.08f; face.glow.color = new Color(1, 0.85f, 0.35f, 0.6f); });
                var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => { cell.localScale = Vector3.one; face.glow.color = new Color(1, 0.85f, 0.35f, 0); });
                trig.triggers.Add(enter);
                trig.triggers.Add(exit);
            }
            var back = UIKit.Button(heroPanel, "뒤로", () => heroPanel.gameObject.SetActive(false), new Color(0.1f, 0.1f, 0.12f, 0.9f), 24, UIKit.Muted);
            back.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(260, 60));
            heroPanel.gameObject.SetActive(false);
        }

        void BuildRules()
        {
            var bg = UIKit.Img(root, "Rules", new Color(0.02f, 0.01f, 0.05f, 0.94f));
            bg.raycastTarget = true;
            rulesPanel = bg.rectTransform;
            rulesPanel.Fill();
            var frame = UIKit.Frame(rulesPanel, "Frame");
            frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1400, 860));
            var title = UIKit.Txt(frame.transform, "Title", "게임 규칙", 48, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1);
            title.rectTransform.offsetMin = new Vector2(0, -80);
            title.rectTransform.offsetMax = new Vector2(0, -10);
            var body = UIKit.Txt(frame.transform, "Body", RulesText, 23, UIKit.Ink, TextAnchor.UpperLeft);
            body.rectTransform.Anchor(0, 0, 1, 1, 50, 30, 50, 95);
            body.lineSpacing = 1.12f;
            // 확률 문구 등이 길어져도 마지막 줄까지 보이게 글자 크기를 자동으로 줄인다
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 15;
            body.resizeTextMaxSize = 23;
            var close = UIKit.Button(rulesPanel, "닫기", () => rulesPanel.gameObject.SetActive(false), null, 26);
            close.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(260, 62));
            rulesPanel.gameObject.SetActive(false);
        }

        static string RulesText =>
            "<color=#ffd05c>■ 시작</color>  각자 영웅 1명(리더) · 메인 덱 40장(소환수·마법) · <b>엑스트라 덱</b>(필드, 최대 10장 · 같은 필드 1장씩)으로 시작하며 7장을 드로우하고, <b>운명 재배치</b>로 원하는 카드를 한 번 바꿀 수 있습니다. 첫 필드는 랜덤.\n" +
            "<color=#ffd05c>■ 필드</color>  대전 시작 때 <b>코인 토스</b>가 자동으로 던져지고, <b>앞면</b>이 나오면 내가 <b>지형 우선 선택권</b>(필드 선/후)을 갖습니다(뒷면이면 상대). <b>T2 부터</b> 양쪽이 <b>번갈아</b> 필드 차례를 갖고, 차례인 쪽은 배치 중 엑스트라 덱에서 필드 1장을 전개할 수 있습니다(덮인 필드는 묘지로).\n" +
            "<color=#ffd05c>■ 타임</color>  양쪽이 <b>동시에</b> 진행합니다. T 시작 드로우는 <b>T2 2장 · T3 3장 … T7 부터 7장</b>(T1 은 시작 패만). <b>패는 최대 9장</b>이고, 엔드 타임에 7장이 넘으면 넘는 만큼 골라 버립니다(9장이면 2장).\n" +
            "<color=#ffd05c>■ 원소</color>  매 T 시작에 <b>원소 카드 1장</b>을 골라 영웅 아래에 쌓습니다(원소 덱 = 덱에 든 속성, 영웅 상성에 따라 자기 속성 10 · 이기는 속성 5 · 그 외 3 · 약점 1장까지). 쌓인 원소는 <b>매 T 다시</b> 씁니다. 카드의 <b>비용</b>만큼 같은 속성 원소를 써야 낼 수 있습니다(무속성은 아무 원소, 무지개는 서로 다른 원소).\n" +
            "<color=#ffd05c>■ 배치</color>  30초 동안 소환수를 <b>뒷면으로</b> 원하는 칸에 끌어다 놓습니다(원소가 모자라면 못 냄). 마법도 칸에 <b>뒷면으로 놓아 두면</b> 배틀 때 그 칸 차례에 공개·발동(조건이 안 맞으면 불발). 살아남은 소환수는 다음 T 에 자리를 옮길 수 있습니다.\n" +
            "<color=#ffd05c>■ 배틀</color>  시작할 때 <b>양쪽 카드가 모두 공개</b>됩니다. T 마다 <b>공격 턴</b>이 번갈아 오며(필드 후공 쪽이 T1 공격), <b>공격 턴인 쪽 소환수만 왼쪽 칸부터</b> 차례로 공개(소환 효과) 후 <b>상대의 가장 앞 소환수</b>를 공격합니다([도발] 우선, [저격]은 가장 뒤 · 앞 소환수가 쓰러지면 다음 소환수로). 수비 측 소환수는 먼저 공격하지 않고 <b>공격받으면 맞서 싸워 반격</b>만 합니다. 상대 소환수가 없으면 영웅을 직접 공격(피해 절반). 전투 중 선택지는 <b>7초</b> 안에 고릅니다(지나면 자동 선택).\n" +
            "<color=#ffd05c>■ 전투</color>  양쪽이 주사위를 <b>1개 던집니다</b> (바람 속성은 2개 던져 1개 선택). 나온 눈의 구간 <b>1·2 / 3·4 / 5·6</b>에 따라 몬스터마다 다른 효과가 발동한 뒤 동시에 피해를 주고받습니다. 쓰러진 소환수의 남은 피해 절반은 영웅에게. 영웅이 한 T 에 받는 피해는 최대 10.\n" +
            "<color=#ffd05c>■ 엔드</color>  패가 7장을 넘으면 넘는 만큼 골라 버립니다. 덱이 비면 배틀 엔드마다 <b>영웅 결투</b>로 승부를 냅니다.\n" +
            "<color=#ff8a3d>■ 등급</color>  Common · Uncommon · Rare · <color=#ff8a3d>Unique</color>(최상위, 오버프레임). " + $"팩 {BoosterPack.Latest.cardsPerPack}장, {BoosterPack.Latest.RateText()} (가공 {BoosterPack.FinishRateText}, 상점 [확률 정보]에서 카드별 확률 확인)." + " 덱에 유니크 몬스터 1장, 유니크 마법 1장까지만 넣을 수 있습니다. '순속성' = 아군 필드 몬스터가 모두 같은 속성일 때 추가 효과.\n" +
            "<color=#ffd05c>■ 영웅</color>  능력은 매치에 한 번. 영웅마다 HP가 다릅니다.\n" +
            "<color=#ffd05c>■ 필드</color>  센터에 놓이며 아군·적군 모두 영향을 받습니다. 타임마다 1장. 상성: 물>화염>바람>대지>번개>물, 빛↔어둠 — 속성 필드는 그 속성을 강화하고 상성 약점 속성을 약화합니다. 무지개 몬스터는 모든 속성 조건을 받고, 무속성 몬스터는 필드 영향을 받지 않습니다.\n" +
            "<color=#ffd05c>■ 승리</color>  상대 영웅 HP를 0으로 만들거나, 상대가 드로우할 수 없으면 승리.\n\n" +
            "<color=#aaa>조작: 카드 클릭 → 확대 · 묘지 더미/영웅 패널 [묘지] → 묘지 보기 · 우클릭 → 낼 수 있는 카드(초록 테두리)는 바로 내기, 그 외 행동 선택 / 배틀: 주황빛 몬스터를 대상에게 드래그 / 마우스 올리기 → 상세 · 덱·묘지는 장수 표시 / Esc: 설정 · 항복</color>";

        void BuildFieldSelect()
        {
            var bg = UIKit.Img(root, "FieldSelect", new Color(0.02f, 0.01f, 0.05f, 0.94f));
            bg.raycastTarget = true;
            fieldPanel = bg.rectTransform;
            fieldPanel.Fill();
            var title = UIKit.Txt(fieldPanel, "Title", "다음 필드 선택", 56, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1200, 80));
            var sub = UIKit.Txt(fieldPanel, "Sub", "매치에서 패배한 쪽이 다음 매치의 필드를 결정합니다", 24, UIKit.Ink);
            sub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1200, 40));

            var grid = UIKit.Rect("Grid", fieldPanel);
            grid.Anchor(0.03f, 0.14f, 0.7f, 0.84f);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(236, 76);
            gl.spacing = new Vector2(12, 12);
            gl.childAlignment = TextAnchor.UpperCenter;
            foreach (var f in CardList.Fields)
            {
                var d = f;
                var b = UIKit.Button(grid, d.name, () => SelectField(d), Color.Lerp(new Color(0.08f, 0.06f, 0.1f), CardData.ElementColor(d.element), 0.28f), 22, Color.white);
            }
            var prev = UIKit.Rect("Preview", fieldPanel);
            prev.Place(new Vector2(0.85f, 0.52f), Vector2.zero, new Vector2(380, 532), new Vector2(0.5f, 0.5f));
            fieldPreview = CardFace.Build(prev, 380, 532, 0.52f);
            fieldPreview.root.Fill();
            fieldPreview.glow.color = new Color(1, 0.85f, 0.35f, 0.3f);
            fieldConfirm = UIKit.Button(fieldPanel, "이 필드로 시작", () =>
            {
                if (selectedField == null) return;
                fieldPanel.gameObject.SetActive(false);
                onFieldPicked?.Invoke(selectedField);
            }, new Color(0.45f, 0.08f, 0.05f, 0.96f), 30, Color.white);
            fieldConfirm.rt.Place(new Vector2(0.85f, 0.1f), Vector2.zero, new Vector2(380, 80), new Vector2(0.5f, 0.5f));
            fieldPanel.gameObject.SetActive(false);
        }

        void SelectField(CardData d)
        {
            selectedField = d;
            fieldPreview.Set(d);
        }

        // ══════════════════════════ 흐름 ══════════════════════════

        public void Show()
        {
            root.gameObject.SetActive(true);
            mainMenu.gameObject.SetActive(true);
            heroPanel.gameObject.SetActive(false);
            rulesPanel.gameObject.SetActive(false);
            fieldPanel.gameObject.SetActive(false);
            set.gameObject.SetActive(true);
            cam.transform.position = SetOrigin + CamPos;
            cam.transform.rotation = Quaternion.identity;
            cam.fieldOfView = CamFov;
            ResetMeteors();
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
            set.gameObject.SetActive(false);
        }

        void ShowHeroSelect() => heroPanel.gameObject.SetActive(true);

        // ══════════════════════════ 떨어지는 원소 운석 ══════════════════════════
        // 원래 키아트(title_bg)의 궤적(꼬리 방향)을 따라, 가운데 로고를 무지개처럼 둘러싼 자리에 왼쪽부터 1초마다 하나씩 휘어 들어와 박힌다.
        // 박히는 순간 화면이 흔들리고, 꼬리는 사라지고 보석 머리만 남아 은은하게 빛난다.
        // (그림 좌표 1376×768 기준 머리 위치, 꼬리 방향(화면 위쪽 +), 휘는 정도)
        static readonly (int el, Vector2 head, Vector2 tail, float curl)[] MeteorArt =
        {
            (0, new Vector2(180, 355), new Vector2(-0.92f, 0.38f), 30f),     // 화염: 로고 왼쪽 아래, 왼쪽에서 낮게
            (4, new Vector2(203, 228), new Vector2(-0.94f, 0.34f), 38f),     // 번개: 로고 왼쪽, 왼쪽 무지개 고리에서
            (6, new Vector2(356, 128), new Vector2(-0.72f, 0.69f), 25f),     // 어둠: 로고 왼쪽 위, 왼쪽 위에서
            (5, new Vector2(688, 84), new Vector2(0f, 1f), 0f),               // 빛: 로고 바로 위, 위에서 곧장
            (2, new Vector2(1020, 128), new Vector2(0.55f, 0.83f), -30f),    // 바람: 로고 오른쪽 위, 오른쪽 위에서 휘감아
            (3, new Vector2(1173, 228), new Vector2(0.96f, 0.28f), -36f),    // 대지: 로고 오른쪽, 오른쪽 무지개 고리에서
            (1, new Vector2(1196, 355), new Vector2(0.86f, 0.5f), -32f),     // 물: 로고 오른쪽 아래, 오른쪽에서
        };
        static readonly Color[] MeteorGlow =                                    // title_meteor_{번호} 의 빛 색
        {
            new Color(1f, 0.45f, 0.15f), new Color(0.3f, 0.6f, 1f), new Color(0.35f, 1f, 0.55f), new Color(1f, 0.75f, 0.35f),
            new Color(1f, 0.95f, 0.3f), new Color(1f, 1f, 0.85f), new Color(0.7f, 0.35f, 1f),
        };
        const float MeteorGap = 1.0f, MeteorFall = 0.85f, MeteorStart = 0.5f;
        RectTransform meteorRoot;
        readonly List<(RectTransform rt, Image img, Image head, Image flash, int el)> meteors = new List<(RectTransform, Image, Image, Image, int)>();
        readonly List<(Vector2 p0, Vector2 p1, Vector2 to)> meteorPath = new List<(Vector2, Vector2, Vector2)>();
        float meteorT;
        int meteorsLanded;
        float shakeT = 9f;

        static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;

        void BuildMeteors(RectTransform parent)
        {
            meteorRoot = UIKit.Rect("Meteors", parent);
            meteorRoot.Fill();
            foreach (var (el, headPx, tailDir, curl) in MeteorArt)
            {
                var sp = GameAssets.TexSprite("title_meteor_" + el);
                if (sp == null) continue;
                var c = MeteorGlow[el];
                var flash = UIKit.Img(meteorRoot, "Flash" + el, new Color(c.r, c.g, c.b, 0), GameAssets.Glow);
                var fr = flash.rectTransform;
                fr.anchorMin = fr.anchorMax = Vector2.zero;
                fr.sizeDelta = new Vector2(260, 260);
                var img = UIKit.Img(meteorRoot, "Meteor" + el, Color.white, sp);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.1f);                  // 운석 머리(그림 아래쪽) 기준
                float h = 280, w = h * sp.rect.width / sp.rect.height;
                rt.sizeDelta = new Vector2(w, h);
                var head = UIKit.Img(rt, "Head", new Color(1, 1, 1, 0), GameAssets.TexSprite("title_meteor_" + el + "_head") ?? sp);
                head.rectTransform.Fill();
                meteors.Add((rt, img, head, flash, el));
                // 그림 좌표 → 1920×1080 화면 좌표 (아래가 0)
                var to = new Vector2(headPx.x / 1376f * 1920f, (1 - headPx.y / 768f) * 1080f);
                var t = tailDir.normalized;
                var p1 = to + t * 420f;                                                     // 마지막엔 꼬리 반대 방향으로 들어온다
                var p0 = p1 + (Vector2)(Quaternion.Euler(0, 0, curl) * t) * 900f;           // 그 앞은 무지개 고리처럼 휘어 있다
                meteorPath.Add((p0, p1, to));
            }
            ResetMeteors();
        }

        void ResetMeteors()
        {
            meteorT = -MeteorStart;
            meteorsLanded = 0;
            foreach (var m in meteors)
            {
                m.rt.gameObject.SetActive(false);
                m.flash.color = new Color(m.flash.color.r, m.flash.color.g, m.flash.color.b, 0);
            }
        }

        void UpdateMeteors()
        {
            if (meteors.Count == 0) return;
            meteorT += Time.unscaledDeltaTime;
            float zoom = BgZoom;
            if (keyArt != null) keyArt.localScale = new Vector3(40f * zoom, 22.5f * zoom, 1);
            meteorRoot.localScale = new Vector3(zoom, zoom, 1);                      // 배경과 같이 숨쉰다
            var size = meteorRoot.rect.size;
            var sc = new Vector2(size.x / 1920f, size.y / 1080f);
            for (int n = 0; n < meteors.Count; n++)
            {
                var (rt, img, head, flash, el) = meteors[n];
                var (p0, p1, to) = meteorPath[n];
                var c = MeteorGlow[el];
                float lt = meteorT - n * MeteorGap;
                if (lt < 0) { rt.gameObject.SetActive(false); continue; }
                rt.gameObject.SetActive(true);
                if (lt < MeteorFall)
                {
                    // 휘어진 궤적을 따라 점점 빨라지며 떨어진다
                    float k = lt / MeteorFall;
                    float e = k * k * (1.6f - 0.6f * k);
                    var p = Bezier(p0, p1, to, e);
                    var tan = Bezier(p0, p1, to, Mathf.Min(1, e + 0.02f)) - Bezier(p0, p1, to, Mathf.Max(0, e - 0.02f));
                    rt.anchoredPosition = new Vector2(p.x * sc.x, p.y * sc.y);
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg + 90f);   // 머리(그림 아래)가 진행 방향
                    rt.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.85f, k) * sc.y;
                    img.color = new Color(1, 1, 1, Mathf.Clamp01(k * 5f));
                    head.color = new Color(1, 1, 1, 0);
                    continue;
                }
                // 박힘: 처음 닿는 순간 한 번 화면을 흔든다
                if (n >= meteorsLanded) { meteorsLanded = n + 1; shakeT = 0; }
                float st = lt - MeteorFall;
                var fin = to - p1;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(fin.y, fin.x) * Mathf.Rad2Deg + 90f);
                float sink = Mathf.Exp(-st * 9f) * Mathf.Sin(st * 30f) * 10f;    // 박히며 살짝 튕긴다
                var fd = fin.normalized;
                rt.anchoredPosition = new Vector2((to.x + fd.x * sink) * sc.x, (to.y + fd.y * sink) * sc.y);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f + n);
                rt.localScale = Vector3.one * (0.85f + 0.03f * pulse) * sc.y;
                float fade = Mathf.Clamp01(st / 0.6f);                         // 꼬리는 사라지고 머리만 남는다
                img.color = new Color(1, 1, 1, 1 - fade);
                head.color = new Color(1, 1, 1, fade);
                // 빛: 닿을 때 크게 번쩍 → 이후 은은하게 계속
                float burst = Mathf.Exp(-st * 4f);
                flash.rectTransform.anchoredPosition = new Vector2(to.x * sc.x, to.y * sc.y);
                flash.rectTransform.localScale = Vector3.one * (0.7f + 1.3f * (1 - burst) * burst * 4f + 0.1f * pulse) * sc.y;
                flash.color = new Color(c.r, c.g, c.b, 0.25f + 0.1f * pulse + 0.65f * burst);
            }
            // 화면 흔들림 (박힐 때마다 0.3초)
            shakeT += Time.unscaledDeltaTime;
            float amp = shakeT < 0.35f ? (1 - shakeT / 0.35f) * 18f : 0f;
            var off = amp > 0 ? new Vector2(Mathf.Sin(shakeT * 95f), Mathf.Cos(shakeT * 77f)) * amp : Vector2.zero;
            mainMenu.anchoredPosition = off;
            if (cam != null && set.gameObject.activeSelf)
                cam.transform.position = SetOrigin + CamPos + new Vector3(off.x, off.y, 0) * 0.004f;
        }

        const string HintText = "Tap to Start";
        /// <summary>안내 문구가 은은하게 깜빡인다</summary>
        void AnimateHint()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
            startHint.color = new Color(0.9f, 0.9f, 0.94f, 0.45f + 0.55f * k);
        }

        bool transitioning;

        void StartGame()
        {
            if (transitioning) return;
            if (onStart == null) { Sfx.Play("click"); ShowHeroSelect(); return; }
            StartCoroutine(SlashToLoading());
        }

        // ══════════════════════════ 넘어가기: 카타나 베기 → 로딩 ══════════════════════════
        // 지금 화면을 찍어 대각선으로 두 조각 낸 뒤 벌려서, 뒤에서 7원소 운석이 돌며 Loading 이 나온다.
        // 로딩 중에 메인 메뉴로 바꾸고, 끝나면 로딩 화면이 사라진다.
        IEnumerator SlashToLoading()
        {
            transitioning = true;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();

            var go = new GameObject("TitleTransition", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            UIKit.ScreenCanvas(go, 300);
            var top = (RectTransform)go.transform;
            var blocker = UIKit.Img(top, "Block", new Color(0, 0, 0, 0));
            blocker.rectTransform.Fill();
            blocker.raycastTarget = true;
            yield return null;
            var size = top.rect.size;

            // 로딩 층 (조각들 뒤)
            var load = UIKit.Rect("Loading", top).Fill();
            var loadGroup = load.gameObject.AddComponent<CanvasGroup>();
            UIKit.Img(load, "Bg", new Color(0.015f, 0.008f, 0.035f, 1f)).rectTransform.Fill();
            var halo = UIKit.Img(load, "Halo", new Color(0.55f, 0.4f, 1f, 0.35f), GameAssets.Glow);
            halo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(760, 760));
            var orbit = UIKit.Rect("Orbit", load).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), Vector2.zero);
            var gems = new List<RectTransform>();
            int[] order = { 0, 3, 4, 2, 1, 6, 5 };                        // 화염 대지 번개 바람 물 어둠 빛 (무지개 순)
            foreach (int el in order)
            {
                var sp = GameAssets.TexSprite("title_meteor_" + el);
                if (sp == null) continue;
                var g = UIKit.Img(orbit, "Gem" + el, Color.white, sp);
                var r = g.rectTransform;
                r.pivot = new Vector2(0.5f, 0.1f);                        // 머리 기준
                float h = 170;
                r.sizeDelta = new Vector2(h * sp.rect.width / sp.rect.height, h);
                gems.Add(r);
            }
            // 운석 고리 가운데: 회전하는 3D 심볼 (따로 찍어 RenderTexture 로)
            var emb = BuildLoadEmblem(orbit);
            var loadText = UIKit.Txt(load, "Text", "Loading", 30, new Color(0.9f, 0.9f, 0.94f), TextAnchor.MiddleCenter);
            loadText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(600, 50));
            loadText.Shadow(new Color(0, 0, 0, 0.85f), 2);

            // 화면 두 조각 (대각선: 왼쪽 위 → 오른쪽 아래)
            float ang = -Mathf.Atan2(size.y, size.x) * Mathf.Rad2Deg * 0.85f;
            var halves = new List<RectTransform>();
            foreach (bool upper in new[] { true, false })
            {
                var holder = UIKit.Rect(upper ? "Upper" : "Lower", top);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = Vector2.zero;
                var mask = UIKit.Img(holder, "Mask", Color.white);
                var mr = mask.rectTransform;
                mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.5f);
                mr.pivot = new Vector2(0.5f, upper ? 0f : 1f);            // 베인 선이 마스크 가장자리
                mr.sizeDelta = new Vector2(size.x * 3, size.y * 3);
                mr.localRotation = Quaternion.Euler(0, 0, ang);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var raw = UIKit.Rect("Shot", mr).gameObject.AddComponent<RawImage>();
                raw.texture = shot;
                raw.raycastTarget = false;
                var rr = raw.rectTransform;
                rr.anchorMin = rr.anchorMax = new Vector2(0.5f, upper ? 0f : 1f);
                rr.sizeDelta = size;
                rr.localRotation = Quaternion.Euler(0, 0, -ang);          // 사진은 똑바로
                rr.localPosition = Vector3.zero;
                halves.Add(holder);
            }

            // 칼날: 왼쪽 위에서 오른쪽 아래로 번쩍 그어진다
            var blade = UIKit.Img(top, "Blade", new Color(1f, 1f, 1f, 0.95f), GameAssets.Glow);
            var br = blade.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0, 0.5f);
            float len = size.magnitude * 1.1f;
            var dir = (Vector2)(Quaternion.Euler(0, 0, ang) * Vector2.right);
            br.anchoredPosition = -dir * len * 0.5f;
            br.localRotation = Quaternion.Euler(0, 0, ang);
            br.sizeDelta = new Vector2(0, 26);
            var core = UIKit.Img(br, "Core", Color.white);                // 칼날 한가운데 가는 흰 선
            core.rectTransform.Anchor(0, 0.5f, 1, 0.5f, 0, -3, 0, -3);
            var flash = UIKit.Img(top, "Flash", new Color(1, 1, 1, 0));
            flash.rectTransform.Fill();

            Sfx.Play("whoosh", 0.9f);
            for (float t = 0; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                br.sizeDelta = new Vector2(len * Mathf.SmoothStep(0, 1, t / 0.16f), 26);
                yield return null;
            }
            br.sizeDelta = new Vector2(len, 26);
            Sfx.Play("attack", 1f);
            flash.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSecondsRealtime(0.08f);

            // 두 조각이 베인 선 양쪽으로 미끄러지며 벌어진다
            var normal = (Vector2)(Quaternion.Euler(0, 0, ang + 90) * Vector2.right);
            for (float t = 0; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.7f, e = k * k;
                halves[0].anchoredPosition = normal * e * size.y * 0.9f + dir * e * 160f;
                halves[1].anchoredPosition = -normal * e * size.y * 0.9f - dir * e * 160f;
                halves[0].localRotation = Quaternion.Euler(0, 0, e * 4f);
                halves[1].localRotation = Quaternion.Euler(0, 0, -e * 4f);
                flash.color = new Color(1, 1, 1, 0.2f * (1 - Mathf.Clamp01(k * 5f)));
                blade.color = new Color(1, 1, 1, 0.95f * (1 - Mathf.Clamp01(k * 2.5f)));
                core.color = new Color(1, 1, 1, 1 - Mathf.Clamp01(k * 3f));
                br.sizeDelta = new Vector2(len, 26 * (1 + k * 2));
                OrbitGems(gems, orbit, loadText, halo, t);
                yield return null;
            }
            foreach (var h in halves) Destroy(h.gameObject);
            Destroy(blade.gameObject);
            Destroy(flash.gameObject);
            Destroy(shot);

            // 로딩: 운석이 돌고, 그동안 뒤에서 메인 메뉴로 바꾼다
            float lt = 0.7f;
            bool switched = false;
            for (float t = 0; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                if (!switched && t > 0.25f) { switched = true; onStart?.Invoke(); }
                OrbitGems(gems, orbit, loadText, halo, lt + t);
                yield return null;
            }
            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                loadGroup.alpha = 1 - t / 0.45f;
                OrbitGems(gems, orbit, loadText, halo, lt + 1.6f + t);
                yield return null;
            }
            Destroy(go);
            DestroyLoadEmblem();
            transitioning = false;
        }

        /// <summary>로딩: 7원소 운석이 원을 그리며 꼬리를 끌고 돈다 + Loading 점 애니메이션</summary>
        static void OrbitGems(List<RectTransform> gems, RectTransform orbit, Text text, Image halo, float t)
        {
            const float R = 150f;
            for (int i = 0; i < gems.Count; i++)
            {
                float a = t * 2.4f + i * Mathf.PI * 2 / gems.Count;
                var pos = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R;
                gems[i].anchoredPosition = pos;
                var tangent = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));              // 반시계 방향으로 돈다
                gems[i].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + 90f);
                gems[i].localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(t * 5f + i));
            }
            text.text = "Loading" + new string('.', 1 + (int)(t * 3f) % 3);
            halo.color = new Color(0.55f, 0.4f, 1f, 0.25f + 0.1f * Mathf.Sin(t * 3f));
        }

        public void OpenHeroSelect() => ShowHeroSelect();

        public void ConfirmFieldIfOpen()
        {
            if (!fieldPanel.gameObject.activeInHierarchy) return;
            SelectField(CardList.Fields[UnityEngine.Random.Range(0, CardList.Fields.Count)]);
            fieldPanel.gameObject.SetActive(false);
            onFieldPicked?.Invoke(selectedField);
        }

        void PickHero(CardData d)
        {
            heroPanel.gameObject.SetActive(false);
            onHeroPicked?.Invoke(d);
        }

        public void ShowFieldSelect(Action<CardData> picked)
        {
            onFieldPicked = picked;
            root.gameObject.SetActive(true);
            mainMenu.gameObject.SetActive(false);
            heroPanel.gameObject.SetActive(false);
            rulesPanel.gameObject.SetActive(false);
            fieldPanel.gameObject.SetActive(true);
            fieldPanel.SetAsLastSibling();
            SelectField(CardList.Fields[0]);
        }

        public void HideOverlay() => root.gameObject.SetActive(false);

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── 로딩 화면 가운데 3D 심볼 ──
        // 오버레이 UI 위에는 3D 가 그려지지 않으므로, 타이틀 무대와 떨어진 곳에 심볼 복제 + 전용 카메라를 두고 RenderTexture 로 띄운다.
        // (메인 메뉴로 바뀌며 타이틀 무대(set)가 꺼져도 계속 돈다)
        static readonly Vector3 LoadEmblemPos = new Vector3(0, -800, 0);
        public static float LoadEmblemYaw = 180f;        // 정면(7Z 가 읽히는 방향): 모델 기본 방향은 뒷면(거울 글자)
        GameObject loadEmblemRoot;
        Transform loadEmblemSpin;
        RenderTexture loadEmblemRT;

        RawImage BuildLoadEmblem(RectTransform parent)
        {
            loadEmblemRoot = new GameObject("LoadingEmblem");
            loadEmblemRoot.transform.position = LoadEmblemPos;
            loadEmblemSpin = new GameObject("Spin").transform;
            loadEmblemSpin.SetParent(loadEmblemRoot.transform, false);
            var m = GameAssets.SpawnModel("emblem", loadEmblemSpin, 0.72f, 0.4f);
            if (m == null) { DestroyLoadEmblem(); return null; }
            var b = BoardView.Bounds(m.transform);
            float sz = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (sz > 1e-4f) m.transform.localScale *= 1.3f / sz;
            b = BoardView.Bounds(m.transform);
            m.transform.position -= b.center - loadEmblemSpin.position;
            foreach (var (pos, col, inten) in new[]
            {
                (new Vector3(0.6f, 1.3f, -2.2f), new Color(1f, 0.9f, 0.75f), 10f),
                (new Vector3(-1.3f, -0.5f, -1.8f), new Color(1f, 0.93f, 0.8f), 5f),
                (new Vector3(1.6f, 0.2f, 1.2f), new Color(0.7f, 0.5f, 1f), 4f),     // 뒤쪽 보랏빛 테두리광
            })
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 8;
                l.intensity = inten;
                l.color = col;
                l.transform.SetParent(loadEmblemRoot.transform, false);
                l.transform.localPosition = pos;
            }
            loadEmblemRT = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "LoadEmblem" };
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(loadEmblemRoot.transform, false);
            cam.transform.localPosition = new Vector3(0, 0, -3.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 30;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 8f;
            cam.allowHDR = false;
            cam.depth = -10;
            cam.targetTexture = loadEmblemRT;
            var raw = UIKit.Rect("Emblem", parent).gameObject.AddComponent<RawImage>();
            raw.texture = loadEmblemRT;
            raw.raycastTarget = false;
            raw.rectTransform.anchorMin = raw.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            raw.rectTransform.sizeDelta = new Vector2(230, 230);             // 운석 고리(반지름 150) 안쪽에 꼭 맞게
            raw.rectTransform.anchoredPosition = Vector2.zero;
            return raw;
        }

        void DestroyLoadEmblem()
        {
            if (loadEmblemRoot != null) Destroy(loadEmblemRoot);
            if (loadEmblemRT != null) { loadEmblemRT.Release(); Destroy(loadEmblemRT); }
            loadEmblemRoot = null;
            loadEmblemSpin = null;
            loadEmblemRT = null;
        }

        void Update()
        {
            if (loadEmblemSpin != null)
            {
                // 로딩 화면 심볼은 돌지 않고 정면(7Z)을 보며 살짝 떠 있기만 한다
                float ut = Time.unscaledTime;
                loadEmblemSpin.localRotation = Quaternion.Euler(0, LoadEmblemYaw, 0);
                loadEmblemSpin.localPosition = new Vector3(0, Mathf.Sin(ut * 2f) * 0.04f, 0);
            }
            if (startHint != null && startHint.gameObject.activeInHierarchy) AnimateHint();

            if (meteorRoot != null && meteorRoot.gameObject.activeInHierarchy) UpdateMeteors();
            if (emblem == null || !set.gameObject.activeSelf) return;
            t += Time.deltaTime;
            emblem.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 8f, t * 35f, Mathf.Sin(t * 0.5f) * 4f);
            emblem.localPosition = EmblemPos + new Vector3(0, Mathf.Sin(t * 1.3f) * 0.06f, 0);
        }
    }
}
