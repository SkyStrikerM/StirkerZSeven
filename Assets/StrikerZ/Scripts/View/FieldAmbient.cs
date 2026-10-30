using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StrikerZ
{
    /// <summary>
    /// 필드별 주기 연출. 센터 필드가 바뀌면 그 필드의 분위기를 돌린다:
    /// 늘 떠다니는 입자(꽃잎·불티·거품·재…) + 몇 초에 한 번씩 일어나는 사건(새 떼·박쥐·물고기 떼·낙석·번개·빛기둥…).
    /// BoardView 에 붙어 보드가 꺼지면 같이 멈추고, 다시 켜지면 같은 필드로 이어진다.
    /// 스프라이트는 Textures/Fx/amb_*.png (VARCO 생성, 마젠타 키 → 알파, 일반 알파 합성), 입자는 기존 Fx 텍스처(가산).
    /// </summary>
    public class FieldAmbient : MonoBehaviour
    {
        const float Span = 9.5f;         // 하늘을 가로지르는 x 범위 (±, 화면 밖에서 들어와 밖으로 나간다)
        BoardView board;
        Transform root;
        CardData field;
        string fieldId = "";
        readonly List<GameObject> owned = new List<GameObject>();

        public void Init(BoardView b) { board = b; }

        /// <summary>센터 필드에 맞춰 연출 교체 (같은 필드면 무시, null = 없음)</summary>
        public void Play(CardData f)
        {
            string id = f != null ? f.id : "";
            if (id == fieldId) return;
            StopAllCoroutines();
            Clear();
            field = f; fieldId = id;
            if (f != null && isActiveAndEnabled) Begin();
        }

        void OnEnable() { if (field != null && root == null) Begin(); }
        void OnDisable() { Clear(); }

        void Clear()
        {
            foreach (var o in owned) if (o != null) { var r = o.GetComponent<Renderer>(); if (r != null) Destroy(r.sharedMaterial); Destroy(o); }
            owned.Clear();
            if (root != null) Destroy(root.gameObject);
            root = null;
        }

        // ══════════════════════════ 필드별 장면 ══════════════════════════

        void Begin()
        {
            var e = field.element;
            var ec = CardData.ElementColor(e);
            switch (field.name)
            {
                // ── 화염 ──
                case "작열의 화산":
                    Embers(5f);
                    Every(7f, 2f, () => { var p = Ground(); Fx?.Burst(p, CardFx.Style.Fire, 1.4f, 0.6f); Fx?.Sparks(p, new Color(1f, 0.5f, 0.1f), new Color(1f, 0.9f, 0.3f), 14, Vector3.up * 2.2f, 1f, 0.12f, 0.9f, 0.8f, 0.2f, "mote_fire"); });
                    Every(26f, 6f, () => { StartCoroutine(Flyer("amb_dragon", new Color(0.55f, 0.12f, 0.08f, 0.95f), 2f, Random.Range(0.6f, 1.3f), Random.Range(3.6f, 4.3f), Dir(), 1.6f, 0.18f, 1.1f, 0.15f)); Fx?.Flash(new Color(1f, 0.4f, 0.1f, 0.12f), 0.8f); });
                    Every(18f, 5f, () => board.Shake(0.05f));
                    break;
                case "업화의 폭주":
                    Embers(11f);
                    Every(9f, 3f, () => Fx?.Pillar(Ground(), new Color(1f, 0.45f, 0.1f, 0.8f), 3.2f, 1.1f, 1.3f));
                    Every(5f, 1.5f, () => Fx?.Spiral(Ground(), new Color(1f, 0.5f, 0.15f), 1.4f, 1f));
                    Every(20f, 6f, () => { board.Shake(0.07f); Fx?.Flash(new Color(1f, 0.35f, 0.1f, 0.15f), 0.4f); });
                    break;
                case "불꽃의 결계":
                    Drift("mote_fire", new Color(1f, 0.6f, 0.2f, 0.8f), new Color(1f, 0.85f, 0.4f, 0.8f), 3f, new Vector3(0, 0.6f, 0), new Vector3(14f, 1f, 9f), Vector3.up * 0.25f, 0.25f, 0.07f, 4f);
                    Every(8f, 1f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Fire, 10f, 1.4f));
                    Every(13f, 4f, () => Fx?.Glyph(Ground(), CardFx.Style.Fire, 2.4f, 1.6f));
                    break;

                // ── 물 ──
                case "심해의 신전":
                    Bubbles(5f);
                    Every(11f, 3f, () => StartCoroutine(Flyer("amb_fish", new Color(0.8f, 1f, 1f, 0.9f), 1.5f, Random.Range(0.2f, 1.2f), SkyZ, Dir(), Random.Range(1.4f, 2.2f), 0f, 0f, 0.12f, 0f, false, true)));
                    Every(17f, 5f, () => Jelly(0.85f, 11f));
                    Every(6f, 2f, () => Fx?.Ring(Ground(), CardFx.Style.Water, Random.Range(1.2f, 2.2f), 1.4f));
                    break;
                case "해일의 폭주":
                    Bubbles(9f);
                    Every(9f, 2f, () => { var p = Ground(); Fx?.Ring(p, CardFx.Style.Water, 12f, 1.6f); Fx?.Sparks(p, new Color(0.5f, 0.85f, 1f), Color.white, 30, Vector3.up * 3f, 2.2f, 0.14f, 1f, 1.1f, 0.5f, "mote_water"); board.Shake(0.04f); });
                    Every(14f, 4f, () => StartCoroutine(Flyer("amb_fish", new Color(0.8f, 1f, 1f, 0.9f), 1.4f, Random.Range(0.2f, 1.2f), SkyZ, Dir(), 3.4f, 0f, 0f, 0.2f)));
                    break;
                case "물안개 결계":
                    Drift("amb_cloud", new Color(0.8f, 0.9f, 1f, 0.22f), new Color(0.9f, 0.95f, 1f, 0.28f), 0.5f, new Vector3(0, 0.3f, 1f), new Vector3(20f, 0.6f, 10f), new Vector3(0.35f, 0.02f, 0), 0.15f, 3.2f, 14f, 0f, false, 0f);
                    Every(5f, 2f, () => Fx?.Ring(Ground(), CardFx.Style.Water, Random.Range(0.8f, 1.6f), 1.5f));
                    Every(21f, 6f, () => Jelly(0.7f, 13f));
                    break;

                // ── 바람 ──
                case "천공의 정원":
                    Petals(2.5f, new Color(1f, 0.8f, 0.88f, 0.95f));
                    Every(10f, 3f, () => Birds(new Color(0.12f, 0.1f, 0.18f, 0.95f), 1.7f, 2.6f));
                    Every(25f, 6f, () => Cloud(new Color(1f, 1f, 1f, 0.65f), 0.55f));
                    Every(15f, 5f, () => Butterfly(new Color(1f, 1f, 1f, 0.95f)));
                    break;
                case "폭풍의 폭주":
                    Drift("amb_leaf", new Color(0.7f, 0.9f, 0.5f, 0.95f), new Color(0.5f, 0.75f, 0.3f, 0.95f), 4f, new Vector3(0, 1.2f, 1f), new Vector3(22f, 2f, 9f), new Vector3(4.5f, 0.3f, 0), 1.2f, 0.3f, 4f, 0.05f, false, 3f);
                    Every(6f, 2f, () => { var p = Ground(); Fx?.Spiral(p, CardData.ElementColor(Element.Wind), 1.6f, 1.1f); Fx?.Sparks(p, new Color(0.7f, 1f, 0.8f), Color.white, 12, Vector3.up * 2.5f, 1.4f, 0.1f, 0.9f, -0.1f, 0.4f, "mote_wind"); });
                    Every(14f, 4f, () => { Fx?.Flash(new Color(1f, 1f, 1f, 0.12f), 0.25f); var s = Sky(); Fx?.Bolt(s + Vector3.up * 2f, s + new Vector3(Random.Range(-1f, 1f), -2.5f, 0), new Color(0.85f, 0.9f, 1f), 0.35f, 0.1f); });
                    Every(20f, 6f, () => Birds(new Color(0.12f, 0.1f, 0.18f, 0.95f), 1.5f, 4.5f));
                    break;
                case "산들바람 결계":
                    Drift("amb_leaf", new Color(0.75f, 0.95f, 0.55f, 0.95f), new Color(0.55f, 0.8f, 0.35f, 0.95f), 1.5f, new Vector3(0, 1.5f, 1f), new Vector3(20f, 2.5f, 9f), new Vector3(1.2f, -0.2f, 0), 0.5f, 0.28f, 7f, 0.04f, false, 1.5f);
                    Drift("mote_wind", new Color(0.8f, 1f, 0.9f, 0.5f), Color.white, 2f, new Vector3(0, 0.8f, 0), new Vector3(16f, 1.5f, 10f), new Vector3(0.8f, 0.1f, 0), 0.3f, 0.06f, 5f);
                    Every(8f, 2f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Wind, 10f, 1.8f));
                    Every(12f, 4f, () => Butterfly(new Color(1f, 1f, 1f, 0.95f)));
                    Every(18f, 5f, () => Birds(new Color(0.12f, 0.1f, 0.18f, 0.95f), 1.5f, 2.2f));
                    break;

                // ── 대지 ──
                case "태고의 협곡":
                    Drift("dust", new Color(0.8f, 0.7f, 0.5f, 0.18f), new Color(0.9f, 0.8f, 0.6f, 0.22f), 1f, new Vector3(0, 0.4f, 1f), new Vector3(20f, 0.8f, 10f), new Vector3(0.5f, 0.05f, 0), 0.2f, 2.2f, 9f, 0f, true, 0.2f);
                    Every(9f, 3f, () => Rockfall(1));
                    Every(22f, 6f, () => Birds(new Color(0.3f, 0.2f, 0.12f, 0.95f), 1.2f, 1.6f, 3.2f));
                    Every(30f, 8f, () => Fx?.Glyph(Ground(), CardFx.Style.Earth, 2.6f, 1.8f));
                    break;
                case "지각의 폭주":
                    Drift("dust", new Color(0.8f, 0.7f, 0.5f, 0.2f), new Color(0.9f, 0.8f, 0.6f, 0.25f), 2f, new Vector3(0, 0.4f, 1f), new Vector3(20f, 0.8f, 10f), new Vector3(0.3f, 0.1f, 0), 0.3f, 2f, 8f, 0f, true, 0.2f);
                    Every(12f, 3f, () => { board.Shake(0.12f); Rockfall(2); for (int i = 0; i < 3; i++) Fx?.Sparks(Ground(), new Color(0.75f, 0.6f, 0.4f), new Color(0.9f, 0.8f, 0.6f), 10, Vector3.up * 1.5f, 1f, 0.3f, 1.2f, 0.4f, 0.5f, "dust"); });
                    Every(7f, 2f, () => Fx?.Glyph(Ground(), CardFx.Style.Earth, 2f, 1.2f));
                    break;
                case "암석의 결계":
                    Drift("mote_earth", new Color(0.9f, 0.75f, 0.5f, 0.6f), Color.white, 1.5f, new Vector3(0, 0.5f, 0), new Vector3(14f, 1f, 9f), Vector3.up * 0.2f, 0.15f, 0.07f, 5f);
                    Every(8f, 1f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Earth, 10f, 1.6f));
                    Every(15f, 4f, () => Fx?.Pillar(Ground(), new Color(0.8f, 0.6f, 0.3f, 0.7f), 2.2f, 1f, 1.4f));
                    break;

                // ── 번개 ──
                case "뇌운의 탑":
                    Drift("mote_thunder", new Color(1f, 1f, 0.6f, 0.7f), Color.white, 2f, new Vector3(0, 1f, 1f), new Vector3(18f, 2f, 10f), Vector3.zero, 0.6f, 0.06f, 1.2f);
                    Every(7f, 3f, () => Lightning(1));
                    Every(18f, 5f, () => Cloud(new Color(0.45f, 0.4f, 0.6f, 0.7f), 0.7f));
                    break;
                case "번개의 폭주":
                    Drift("mote_thunder", new Color(1f, 1f, 0.6f, 0.8f), Color.white, 5f, new Vector3(0, 1f, 1f), new Vector3(18f, 2f, 10f), Vector3.zero, 1.2f, 0.07f, 0.8f);
                    Every(4f, 1.5f, () => Lightning(2));
                    Every(10f, 3f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Thunder, 10f, 1f));
                    break;
                case "전류의 결계":
                    Drift("mote_thunder", new Color(1f, 1f, 0.6f, 0.7f), Color.white, 3f, new Vector3(0, 0.3f, 0), new Vector3(15f, 0.4f, 10f), Vector3.zero, 0.9f, 0.06f, 0.7f);
                    Every(6f, 1f, () => { Fx?.Ring(BoardView.CenterPos, CardFx.Style.Thunder, 10f, 1.2f); for (int i = 0; i < 2; i++) Fx?.Sparks(Edge(), new Color(1f, 1f, 0.7f), Color.white, 10, Vector3.zero, 1.6f, 0.08f, 0.5f, 0f, 0.3f, "mote_thunder"); });
                    Every(13f, 4f, () => Fx?.Glyph(Ground(), CardFx.Style.Thunder, 2.4f, 1.2f));
                    break;

                // ── 빛 ──
                case "광휘의 성당":
                    Feathers(0.8f);
                    Drift("star", new Color(1f, 0.95f, 0.7f, 0.7f), Color.white, 2f, new Vector3(0, 1.2f, 1f), new Vector3(16f, 2.4f, 10f), Vector3.up * 0.1f, 0.1f, 0.08f, 2.5f);
                    Every(8f, 2f, () => Fx?.Pillar(Ground(), new Color(1f, 0.95f, 0.75f, 0.6f), 5.5f, 1.3f, 2.2f));
                    Every(14f, 4f, () => Birds(new Color(1f, 1f, 1f, 0.95f), 1.5f, 2.2f));
                    break;
                case "섬광의 폭주":
                    Drift("star", new Color(1f, 1f, 0.8f, 0.8f), Color.white, 5f, new Vector3(0, 1.2f, 1f), new Vector3(16f, 2.4f, 10f), Vector3.zero, 0.3f, 0.09f, 1.5f);
                    Every(6f, 2f, () => { Fx?.Flash(new Color(1f, 1f, 0.9f, 0.18f), 0.35f); Fx?.Burst(Sky(), CardFx.Style.Light, 2.4f, 0.6f); });
                    Every(10f, 3f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Light, 10f, 1.2f));
                    break;
                case "성광의 결계":
                    Feathers(0.5f);
                    Drift("star", new Color(1f, 0.95f, 0.7f, 0.6f), Color.white, 2f, new Vector3(0, 1f, 1f), new Vector3(16f, 2f, 10f), Vector3.up * 0.1f, 0.1f, 0.07f, 3f);
                    Every(8f, 1f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Light, 10f, 1.8f));
                    Every(12f, 4f, () => Fx?.Glyph(Ground(), CardFx.Style.Light, 2.6f, 2f));
                    break;

                // ── 어둠 ──
                case "칠흑의 심연":
                    Drift("mote_dark", new Color(0.6f, 0.4f, 0.9f, 0.7f), new Color(0.3f, 0.2f, 0.5f, 0.7f), 3f, new Vector3(0, 0.8f, 0), new Vector3(16f, 1.6f, 10f), Vector3.up * 0.15f, 0.2f, 0.09f, 5f);
                    Every(10f, 3f, () => Bats(1.5f));
                    Every(16f, 5f, () => Wisp());
                    Every(25f, 6f, () => Fx?.Glyph(Ground(), CardFx.Style.Dark, 2.6f, 2f));
                    break;
                case "암흑의 폭주":
                    Drift("mote_dark", new Color(0.6f, 0.35f, 0.9f, 0.8f), new Color(0.3f, 0.15f, 0.5f, 0.8f), 6f, new Vector3(0, 0.8f, 0), new Vector3(16f, 1.6f, 10f), Vector3.up * 0.3f, 0.5f, 0.1f, 3f);
                    Every(9f, 3f, () => StartCoroutine(Spin("vortex", new Color(0.6f, 0.3f, 1f, 0.8f), Ground(), 3f, 2.4f, 180f, true)));
                    Every(14f, 4f, () => Fx?.Flash(new Color(0.4f, 0.1f, 0.6f, 0.2f), 0.5f));
                    Every(20f, 6f, () => Bats(2.6f));
                    break;
                case "그림자 결계":
                    Drift("mote_dark", new Color(0.5f, 0.35f, 0.8f, 0.6f), new Color(0.25f, 0.15f, 0.45f, 0.6f), 2f, new Vector3(0, 0.6f, 0), new Vector3(15f, 1.2f, 10f), Vector3.up * 0.1f, 0.15f, 0.08f, 6f);
                    Every(8f, 1f, () => Fx?.Ring(BoardView.CenterPos, CardFx.Style.Dark, 10f, 1.8f));
                    Every(12f, 4f, () => { Wisp(); Wisp(); });
                    Every(20f, 5f, () => Fx?.Glyph(Ground(), CardFx.Style.Dark, 2.4f, 2f));
                    break;

                // ── 특수 ──
                case "무지개 파라다이스":
                    Petals(1.5f, new Color(1f, 0.85f, 0.9f, 0.95f));
                    Drift("star", new Color(1f, 0.7f, 0.9f, 0.7f), new Color(0.7f, 0.9f, 1f, 0.7f), 4f, new Vector3(0, 1.2f, 1f), new Vector3(16f, 2.4f, 10f), Vector3.up * 0.1f, 0.2f, 0.08f, 2.5f);
                    Every(7f, 2f, () => Butterfly(Color.HSVToRGB(Random.value, 0.5f, 1f)));
                    Every(15f, 4f, () => Fx?.Ring(BoardView.CenterPos, Color.HSVToRGB(Random.value, 0.7f, 1f), 10f, 1.6f));
                    Every(20f, 6f, () => Birds(new Color(1f, 1f, 1f, 0.95f), 1.5f, 2.2f));
                    break;
                case "고요한 평원":
                    Drift("mote_neutral", new Color(1f, 1f, 0.85f, 0.4f), Color.white, 1.5f, new Vector3(0, 0.8f, 1f), new Vector3(18f, 1.5f, 10f), new Vector3(0.3f, 0.1f, 0), 0.15f, 0.05f, 6f);
                    Every(14f, 4f, () => Birds(new Color(0.12f, 0.1f, 0.18f, 0.95f), 1.6f, 2.2f));
                    Every(22f, 6f, () => Butterfly(new Color(1f, 1f, 1f, 0.95f)));
                    Every(35f, 8f, () => Cloud(new Color(1f, 1f, 1f, 0.6f), 0.45f));
                    break;
                case "황혼의 전장":
                    Drift("ash", new Color(0.6f, 0.55f, 0.5f, 0.6f), new Color(0.35f, 0.3f, 0.3f, 0.6f), 2f, new Vector3(0, 2.2f, 1f), new Vector3(18f, 1f, 10f), new Vector3(0.3f, -0.25f, 0), 0.15f, 0.08f, 9f, 0.02f);
                    Embers(1.5f);
                    Every(12f, 4f, () => Birds(new Color(0.05f, 0.03f, 0.06f, 1f), 1.6f, 2.4f, 1.4f));
                    Every(18f, 5f, () => { Fx?.Flash(new Color(1f, 0.5f, 0.2f, 0.1f), 0.8f); Fx?.Spiral(Ground(), new Color(0.6f, 0.5f, 0.4f), 1.4f, 1.2f); });
                    break;
                case "혼돈의 균열":
                    Drift("shard", new Color(1f, 0.6f, 0.9f, 0.8f), new Color(0.6f, 0.9f, 1f, 0.8f), 2f, new Vector3(0, 1.2f, 1f), new Vector3(16f, 2.4f, 10f), Vector3.zero, 0.8f, 0.14f, 3f, 0f, true, 4f);
                    Every(5f, 2f, () => { Fx?.Flash(Color.HSVToRGB(Random.value, 0.8f, 1f) * new Color(1, 1, 1, 0.14f), 0.15f); StartCoroutine(Flyer("shard", Color.HSVToRGB(Random.value, 0.6f, 1f), 0.8f, SkyY, SkyZ, Dir(), 5f, 0f, 0f, 0.3f, 0f, true, true)); });
                    Every(11f, 3f, () => StartCoroutine(Spin("vortex", Color.HSVToRGB(Random.value, 0.7f, 1f), Ground(), 3f, 2.2f, 240f, true)));
                    Every(17f, 5f, () => Fx?.Ring(BoardView.CenterPos, Color.HSVToRGB(Random.value, 0.7f, 1f), 10f, 1.2f));
                    break;

                default:
                    // 이름을 모르는 필드: 그 속성 입자만 천천히
                    Drift("mote_" + CardFx.Suffix(CardFx.StyleOf(e)), new Color(ec.r, ec.g, ec.b, 0.6f), Color.white, 2f, new Vector3(0, 0.8f, 0), new Vector3(16f, 1.6f, 10f), Vector3.up * 0.15f, 0.2f, 0.08f, 5f);
                    Every(10f, 3f, () => Fx?.Ring(BoardView.CenterPos, CardFx.StyleOf(e), 10f, 1.6f));
                    break;
            }
        }

        // ══════════════════════════ 자주 쓰는 장면 조각 ══════════════════════════

        void Embers(float rate) => Drift("mote_fire", new Color(1f, 0.55f, 0.15f, 0.8f), new Color(1f, 0.85f, 0.35f, 0.8f), rate, new Vector3(0, 0.2f, 1f), new Vector3(18f, 0.4f, 10f), Vector3.up * 0.9f, 0.4f, 0.08f, 3.5f, -0.05f);
        void Bubbles(float rate) => Drift("mote_water", new Color(0.6f, 0.9f, 1f, 0.7f), Color.white, rate, new Vector3(0, 0.2f, 1f), new Vector3(18f, 0.4f, 10f), Vector3.up * 0.7f, 0.25f, 0.07f, 4f, -0.08f);
        void Petals(float rate, Color c) => Drift("amb_petal", c, c, rate, new Vector3(0, 3.2f, 1f), new Vector3(20f, 0.6f, 10f), new Vector3(0.6f, -0.35f, 0), 0.3f, 0.22f, 9f, 0.03f, false, 2f);
        void Feathers(float rate) => Drift("amb_feather", new Color(1f, 1f, 1f, 0.95f), new Color(1f, 0.95f, 0.8f, 0.95f), rate, new Vector3(0, 3.4f, 1f), new Vector3(18f, 0.4f, 10f), new Vector3(0.2f, -0.25f, 0), 0.2f, 0.3f, 12f, 0.015f, false, 1f);

        void Birds(Color c, float size, float speed, float yMin = 0.2f)
            => StartCoroutine(Flyer("amb_birds", c, size, Random.Range(yMin, 1.1f), SkyZ, Dir(), speed, 0.22f, 2.2f, 0.12f));
        void Bats(float speed)
            => StartCoroutine(Flyer("amb_bats", new Color(0.15f, 0.08f, 0.25f, 0.95f), 1.4f, Random.Range(0.3f, 1.2f), SkyZ, Dir(), speed, 0.35f, 5f, 0.25f, 0.4f));
        void Butterfly(Color c)
            => StartCoroutine(Flyer("amb_butterfly", c, 0.6f, Random.Range(0.15f, 0.9f), Random.Range(2.8f, 3.8f), Dir(), Random.Range(0.9f, 1.4f), 0.5f, 6f, 0.15f, 0.4f));
        void Cloud(Color c, float speed)
            => StartCoroutine(Flyer("amb_cloud", c, 2.4f, Random.Range(0.9f, 1.3f), Random.Range(4f, 4.4f), Dir(), speed, 0f, 0f, 0.02f));
        /// <summary>해파리: 하늘 띠에서 천천히 떠오르며 옆으로 흐른다</summary>
        void Jelly(float alpha, float dur)
        {
            var p = new Vector3(Random.Range(-6f, 6f), 0.1f, SkyZ);
            StartCoroutine(Drifter("amb_jelly", new Color(0.8f, 0.95f, 1f, alpha), 1.1f, p, p + new Vector3(Random.Range(-1.5f, 1.5f), 1.2f, 0), dur, 0.4f, 0.12f));
        }
        void Wisp()
        {
            var p = Sky();
            StartCoroutine(Drifter("amb_wisp", new Color(0.85f, 0.7f, 1f, 0.85f), 0.9f, p, p + new Vector3(Random.Range(-3f, 3f), Random.Range(-0.4f, 0.5f), Random.Range(-0.5f, 0.5f)), Random.Range(7f, 10f), 0.5f, 0.15f));
        }
        void Rockfall(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var p = Ground();
                StartCoroutine(Faller("amb_rock", new Color(0.8f, 0.75f, 0.7f, 1f), Random.Range(0.5f, 0.9f), p + Vector3.up * Random.Range(4f, 6f), p, Random.Range(0.9f, 1.3f), Random.Range(-200f, 200f),
                    () => { Fx?.Sparks(p, new Color(0.75f, 0.6f, 0.4f), new Color(0.9f, 0.8f, 0.6f), 12, Vector3.up * 1.2f, 1.2f, 0.32f, 1.1f, 0.5f, 0.3f, "dust"); board.Shake(0.03f); }));
            }
        }
        void Lightning(int n)
        {
            Fx?.Flash(new Color(1f, 1f, 0.9f, 0.16f), 0.3f);
            for (int i = 0; i < n; i++)
            {
                var top = Sky() + Vector3.up * 2.5f;
                var hit = new Vector3(top.x + Random.Range(-1.5f, 1.5f), -1.4f, top.z);
                Fx?.Bolt(top, hit, new Color(0.9f, 0.95f, 1f), 0.4f, 0.16f);
                Fx?.Sparks(hit, new Color(1f, 1f, 0.7f), Color.white, 14, Vector3.up * 1.5f, 1.8f, 0.09f, 0.6f, 0.5f, 0.2f, "mote_thunder");
            }
        }

        // ══════════════════════════ 위치 ══════════════════════════

        static int Dir() => Random.value < 0.5f ? -1 : 1;
        // 카메라(0, 8.4, -6.2 → 0, 0, -0.45 · FOV 42)에서 테이블 너머로 보이는 띠는 좁다:
        // z 3.3~4.3 에서는 y 0.15~1.4 만 화면 위 0~180px 에 들어오고, 바닥(y -1.4)은 z 5~7.5 가 보인다.
        static float SkyY => Random.Range(0.1f, 1.1f);
        static float SkyZ => Random.Range(3.3f, 4.3f);
        /// <summary>테이블 너머 하늘 (화면 위쪽 띠)</summary>
        static Vector3 Sky() => new Vector3(Random.Range(-7f, 7f), SkyY, SkyZ);
        /// <summary>테이블 바깥 바닥 (대부분 멀리, 가끔 양옆 좁은 띠)</summary>
        static Vector3 Ground()
        {
            if (Random.value < 0.8f) return new Vector3(Random.Range(-8f, 8f), -1.4f, Random.Range(5f, 7.5f));
            return new Vector3(Random.Range(6.9f, 7.7f) * Dir(), -1.4f, Random.Range(0f, 4f));
        }
        /// <summary>테이블 가장자리 위</summary>
        static Vector3 Edge()
        {
            if (Random.value < 0.5f) return new Vector3(Random.Range(-6f, 6f), 0.08f, Random.Range(0f, 1f) < 0.5f ? -3.9f : 3.9f);
            return new Vector3(6.3f * Dir(), 0.08f, Random.Range(-3.5f, 3.5f));
        }

        // ══════════════════════════ 기본 도구 ══════════════════════════

        static CardFx Fx => CardFx.I;
        Camera Cam => board != null && board.cam != null ? board.cam : Camera.main;
        Transform Root
        {
            get
            {
                if (root == null) { root = new GameObject("Ambient").transform; root.SetParent(transform, false); }
                return root;
            }
        }

        void Every(float every, float jitter, Action act) => StartCoroutine(LoopRoutine(every, jitter, act));
        IEnumerator LoopRoutine(float every, float jitter, Action act)
        {
            yield return new WaitForSeconds(Random.Range(every * 0.15f, every * 0.7f));
            while (true)
            {
                act();
                yield return new WaitForSeconds(Mathf.Max(0.5f, every + Random.Range(-jitter, jitter)));
            }
        }

        GameObject Quad(string tex, Color c, bool additive)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Amb_" + tex;
            q.transform.SetParent(Root, false);
            var m = GameAssets.UnlitMat(c, CardFx.Tex(tex) ?? GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(m, additive);
            q.GetComponent<Renderer>().sharedMaterial = m;
            owned.Add(q);
            return q;
        }
        static void Tint(GameObject q, Color c)
        {
            var m = q.GetComponent<Renderer>().sharedMaterial;
            m.color = c; m.SetColor("_BaseColor", c);
        }
        void Kill(GameObject q)
        {
            if (q == null) return;
            owned.Remove(q);
            Destroy(q.GetComponent<Renderer>().sharedMaterial);
            Destroy(q);
        }
        void Face(Transform t, float roll)
        {
            var cam = Cam;
            if (cam != null) t.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, roll);
        }
        static float EdgeFade(float t) => Mathf.Clamp01(Mathf.Min(t, 1f - t) * 8f);

        /// <summary>
        /// 화면을 가로지르는 것 (새 떼·박쥐·물고기·구름…). dir +1 = 왼→오른. flap = 날갯짓(세로 눌림 비율), bob = 위아래 출렁임, wobble = 좌우 흔들림(나비).
        /// faceRight = 그림이 오른쪽을 보고 있으면 true (진행 방향에 맞춰 뒤집는다)
        /// </summary>
        IEnumerator Flyer(string tex, Color c, float size, float y, float z, int dir, float speed, float flap = 0f, float flapHz = 3f, float bob = 0.2f, float wobble = 0f, bool additive = false, bool faceRight = true)
        {
            var q = Quad(tex, c, additive);
            float x0 = -Span * dir, x1 = Span * dir;
            float dur = Span * 2f / speed;
            float phase = Random.value * 10f;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float x = Mathf.Lerp(x0, x1, t) + (wobble > 0 ? Mathf.Sin(Time.time * 3.1f + phase) * wobble : 0f);
                float yy = y + Mathf.Sin(t * Mathf.PI * 2f * 1.5f + phase) * bob;
                q.transform.position = new Vector3(x, yy, z);
                float fl = flap > 0 ? 1f - flap * (0.5f + 0.5f * Mathf.Sin(Time.time * flapHz * Mathf.PI * 2f + phase)) : 1f;
                float sx = size * (faceRight ? dir : -dir);
                q.transform.localScale = new Vector3(sx, size * fl, 1f);
                Face(q.transform, 0f);
                Tint(q, new Color(c.r, c.g, c.b, c.a * EdgeFade(t)));
                yield return null;
            }
            Kill(q);
        }

        /// <summary>한 점에서 다른 점으로 흔들리며 떠가는 것 (해파리·정령). pulse = 숨쉬듯 크기 변화 비율</summary>
        IEnumerator Drifter(string tex, Color c, float size, Vector3 from, Vector3 to, float dur, float sway, float pulse, bool additive = false)
        {
            var q = Quad(tex, c, additive);
            float phase = Random.value * 10f;
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                var p = Vector3.Lerp(from, to, t);
                p.x += Mathf.Sin(t * Mathf.PI * 2f * 1.2f + phase) * sway;
                q.transform.position = p;
                float s = size * (1f + pulse * Mathf.Sin(Time.time * 2.4f + phase));
                q.transform.localScale = new Vector3(s, s * (1f - pulse * 0.5f * Mathf.Sin(Time.time * 2.4f + phase)), 1f);
                Face(q.transform, Mathf.Sin(t * 6f + phase) * 8f);
                Tint(q, new Color(c.r, c.g, c.b, c.a * EdgeFade(t)));
                yield return null;
            }
            Kill(q);
        }

        /// <summary>떨어져 바닥에 닿는 것 (낙석). 닿으면 onLand</summary>
        IEnumerator Faller(string tex, Color c, float size, Vector3 from, Vector3 to, float dur, float spin, Action onLand)
        {
            var q = Quad(tex, c, false);
            float r0 = Random.Range(0f, 360f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float k = t * t;   // 가속 낙하
                q.transform.position = Vector3.Lerp(from, to, k);
                q.transform.localScale = new Vector3(size, size, 1f);
                Face(q.transform, r0 + spin * t);
                Tint(q, new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(t * 6f)));
                yield return null;
            }
            onLand?.Invoke();
            // 잠깐 바닥에 머물다 사라진다
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
            {
                Tint(q, new Color(c.r, c.g, c.b, c.a * (1f - t)));
                yield return null;
            }
            Kill(q);
        }

        /// <summary>바닥에 누워 도는 평면 (소용돌이)</summary>
        IEnumerator Spin(string tex, Color c, Vector3 pos, float dur, float size, float degPerSec, bool additive)
        {
            var q = Quad(tex, c, additive);
            float r0 = Random.Range(0f, 360f);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float s = size * Mathf.SmoothStep(0.2f, 1f, Mathf.Min(1f, t * 3f));
                q.transform.position = pos + Vector3.up * 0.05f;
                q.transform.localScale = new Vector3(s, s, 1f);
                q.transform.rotation = Quaternion.Euler(90f, r0 + degPerSec * t * dur, 0f);
                Tint(q, new Color(c.r, c.g, c.b, c.a * EdgeFade(t)));
                yield return null;
            }
            Kill(q);
        }

        /// <summary>
        /// 늘 떠다니는 입자 (계속 뿜는 파티클). box 안에서 생겨 vel ± spread 로 움직이고 life 뒤 사라진다.
        /// additive = 빛나는 입자(검은 배경 Fx 텍스처), 아니면 일반 알파(꽃잎·잎·깃털). spin = 회전 속도
        /// </summary>
        ParticleSystem Drift(string tex, Color a, Color b, float rate, Vector3 center, Vector3 box, Vector3 vel, float spread, float size, float life, float gravity = 0f, bool additive = true, float spin = 1f)
        {
            var go = new GameObject("AmbDrift_" + tex);
            go.transform.SetParent(Root, false);
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.maxParticles = Mathf.Max(16, Mathf.CeilToInt(rate * life * 1.3f));
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = box;
            var vo = ps.velocityOverLifetime;
            vo.enabled = true;
            vo.space = ParticleSystemSimulationSpace.World;
            vo.x = new ParticleSystem.MinMaxCurve(vel.x - spread, vel.x + spread);
            vo.y = new ParticleSystem.MinMaxCurve(vel.y - spread * 0.5f, vel.y + spread * 0.5f);
            vo.z = new ParticleSystem.MinMaxCurve(vel.z - spread * 0.5f, vel.z + spread * 0.5f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var rot = ps.rotationOverLifetime;
            rot.enabled = spin != 0f;
            rot.z = new ParticleSystem.MinMaxCurve(-spin, spin);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = GameAssets.UnlitMat(Color.Lerp(a, b, 0.5f), CardFx.Tex(tex) ?? GameAssets.GlowTexture, true);
            GameAssets.MakeTransparent(m, additive);
            r.sharedMaterial = m;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            owned.Add(go);
            return ps;
        }
    }
}
