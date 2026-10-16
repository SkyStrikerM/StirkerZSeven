using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 꾸미기 상점용 3D 무대: 화면 밖(아래 멀리)에 주사위·코인을 띄워 전용 카메라로 RenderTexture 에 그린다.
    /// - Live : 고른 아이템 1개가 천천히 돌며 여러 면을 보여 준다 (큰 미리보기)
    /// - Icons: 목록 칸용. 주사위 전부를 한 줄로 세워 한 장에 찍고 칸마다 uvRect 로 잘라 쓴다
    /// 전투와 같은 BuildDie/BuildCoin 을 쓰므로 상점에서 본 모습 그대로 굴러간다.
    /// 상점 화면이 꺼지면 Stop() 으로 카메라·조명을 끈다.
    /// </summary>
    public class DiceStage : MonoBehaviour
    {
        static DiceStage inst;
        static DiceStage I => inst != null ? inst : (inst = new GameObject("DiceStage").AddComponent<DiceStage>());

        static readonly Vector3 LivePos = new Vector3(0, -600, 0), IconPos = new Vector3(0, -640, 0);
        /// <summary>큰 미리보기 카메라 위치 (LivePos 기준) — 위에서 내려다보는 각도</summary>
        static readonly Vector3 LiveCamOffset = new Vector3(0, 0.9f, -3.6f);
        static float CamPitch => Mathf.Atan2(LiveCamOffset.y, -LiveCamOffset.z) * Mathf.Rad2Deg;
        const float IconGap = 1.6f;

        Camera liveCam, iconCam;
        RenderTexture liveRT, iconRT;
        Transform liveRoot, iconRoot;
        GameObject liveObj;
        CosmeticItem liveItem;
        string iconKey;
        Light key, rim, fill;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            liveRoot = new GameObject("Live").transform;
            liveRoot.SetParent(transform, false);
            liveRoot.position = LivePos;
            iconRoot = new GameObject("Icons").transform;
            iconRoot.SetParent(transform, false);
            iconRoot.position = IconPos;

            liveRT = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8, name = "DiceLive" };
            liveCam = MakeCam("LiveCam", LivePos + LiveCamOffset, LivePos, liveRT);
            liveCam.fieldOfView = 28;

            key = MakeLight("Key", Quaternion.Euler(42, -32, 0), new Color(1f, 0.96f, 0.9f), 1.25f);
            rim = MakeLight("Rim", Quaternion.Euler(18, 150, 0), new Color(0.55f, 0.75f, 1f), 0.9f);
            // 카메라 쪽에서 비추는 보조광: 없으면 코인 옆테·주사위 그늘면이 까맣게 비어 보인다
            fill = MakeLight("Fill", Quaternion.Euler(12, 8, 0), new Color(0.95f, 0.93f, 1f), 0.55f);
            SetActive(false);
        }

        Camera MakeCam(string name, Vector3 pos, Vector3 look, RenderTexture rt)
        {
            var c = new GameObject(name).AddComponent<Camera>();
            c.transform.SetParent(transform, false);
            c.transform.position = pos;
            c.transform.LookAt(look);
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0, 0, 0, 0);
            c.nearClipPlane = 0.5f;
            c.farClipPlane = 12f;
            c.targetTexture = rt;
            c.depth = -10;
            c.allowHDR = false;
            return c;
        }

        Light MakeLight(string name, Quaternion rot, Color c, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.type = LightType.Directional;
            l.transform.rotation = rot;
            l.color = c;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        void SetActive(bool on)
        {
            liveCam.enabled = on;
            if (iconCam != null) iconCam.enabled = on;
            key.enabled = rim.enabled = fill.enabled = on;
        }

        static GameObject Build(CosmeticItem item) => item.kind == CosmeticKind.Coin ? DiceRoller.BuildCoin(item) : DiceRoller.BuildDie(item);

        /// <summary>큰 미리보기: 아이템 1개가 도는 RenderTexture</summary>
        public static Texture Live(CosmeticItem item)
        {
            var s = I;
            if (s.liveItem != item)
            {
                if (s.liveObj != null) Destroy(s.liveObj);
                s.liveObj = Build(item);
                s.liveObj.transform.SetParent(s.liveRoot, false);
                s.liveObj.transform.localScale = Vector3.one * (item.kind == CosmeticKind.Coin ? 1.45f : 1.05f);
                s.liveItem = item;
                s.spinT = 0;
            }
            s.SetActive(true);
            return s.liveRT;
        }

        /// <summary>목록 칸용: items 를 한 줄로 찍은 텍스처 + 칸별 uvRect</summary>
        public static Texture Icons(IList<CosmeticItem> items, out Rect[] uv)
        {
            var s = I;
            int n = items.Count;
            string k = string.Join(",", System.Linq.Enumerable.Select(items, i => i.id));
            if (k != s.iconKey)
            {
                s.iconKey = k;
                for (int i = s.iconRoot.childCount - 1; i >= 0; i--) Destroy(s.iconRoot.GetChild(i).gameObject);
                for (int i = 0; i < n; i++)
                {
                    var go = Build(items[i]);
                    go.transform.SetParent(s.iconRoot, false);
                    go.transform.localPosition = new Vector3((i - (n - 1) * 0.5f) * IconGap, 0, 0);
                    // 위·앞·옆 세 면이 보이게 (1 · 5 · 3)
                    go.transform.localRotation = Quaternion.Euler(0, 38, 0) * Quaternion.Euler(-18, 0, 0);
                }
                const float half = 0.82f;
                int h = 256, w = Mathf.RoundToInt(h * n * IconGap / (half * 2));
                if (s.iconRT != null) { s.iconRT.Release(); Destroy(s.iconRT); }
                s.iconRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8, name = "DiceIcons" };
                if (s.iconCam == null)
                {
                    s.iconCam = s.MakeCam("IconCam", IconPos + new Vector3(0, 1.6f, -4.4f), IconPos, s.iconRT);
                    s.iconCam.orthographic = true;
                }
                s.iconCam.targetTexture = s.iconRT;
                s.iconCam.orthographicSize = half;
                s.iconCam.aspect = (float)w / h;
            }
            uv = new Rect[n];
            for (int i = 0; i < n; i++) uv[i] = new Rect((float)i / n, 0, 1f / n, 1);
            s.SetActive(true);
            return s.iconRT;
        }

        /// <summary>상점 화면을 떠날 때: 카메라·조명 끄기</summary>
        public static void Stop()
        {
            if (inst != null) inst.SetActive(false);
        }

        float spinT;

        void Update()
        {
            if (liveObj == null || !liveCam.enabled) return;
            spinT += Time.unscaledDeltaTime;
            var t = liveObj.transform;
            if (liveItem.kind == CosmeticKind.Coin)
            {
                // 세워서 세로축으로 회전: 앞면 → 옆테 → 뒷면.
                // 카메라가 살짝 위에서 내려다보므로 그 각도만큼 코인을 카메라 쪽으로 눕혀 면이 정면(동그랗게)으로 보이고 화면 정중앙에 온다
                t.localRotation = Quaternion.Euler(CamPitch, 0, 0) * Quaternion.AngleAxis(spinT * 75f, Vector3.up) * Quaternion.Euler(-90, 0, 0);
                t.localPosition = Vector3.zero;
            }
            else
            {
                // 천천히 구르며 여러 면을 보여 준다
                t.localRotation = Quaternion.AngleAxis(spinT * 38f, Vector3.up) * Quaternion.Euler(22 + Mathf.Sin(spinT * 0.6f) * 28f, 0, 18 + Mathf.Sin(spinT * 0.43f) * 20f);
                t.localPosition = new Vector3(0, Mathf.Sin(spinT * 1.4f) * 0.06f, 0);
            }
        }
    }
}
