using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>메인 허브 설정 페이지: 사운드 · 화면 · 게임 · 키 설정</summary>
    public class SettingsPage : MonoBehaviour
    {
        static readonly Color Dark = UIKit.Dark;
        static readonly Color Row = new Color(0.08f, 0.06f, 0.13f, 0.85f);
        static readonly Color Accent = UIKit.Accent;
        const float RowH = 58f;

        RectTransform page;
        readonly List<Action> refreshers = new List<Action>();
        readonly Dictionary<GameAction, UIKit.Btn> keyButtons = new Dictionary<GameAction, UIKit.Btn>();
        GameAction? listening;
        bool dirty;
        float saveAt;
        Text keyNote;

        SettingsData D => GameSettings.Data;

        /// <summary>키 입력 대기 중 (Esc 는 키 변경 취소로 쓰이므로 다른 창이 Esc 를 처리하면 안 됨)</summary>
        public bool IsListening => listening.HasValue;

        public RectTransform Build(RectTransform content)
        {
            page = UIKit.Rect("Settings", content).Fill();

            // ── 왼쪽: 사운드 · 화면 · 게임 ──
            var left = UIKit.Frame(page, "General", Dark);
            left.rectTransform.Anchor(0, 0, 0.5f, 1, 0, 0, 12, 0);
            float y = -26;
            y = Section(left.transform, "사운드", y);
            y = SliderRow(left.transform, "마스터 볼륨", y, () => D.master, v => D.master = v);
            y = SliderRow(left.transform, "배경음악", y, () => D.music, v => D.music = v);
            y = SliderRow(left.transform, "효과음", y, () => D.sfx, v => D.sfx = v, preview: true);
            y = ToggleRow(left.transform, "음소거", y, () => D.mute, v => D.mute = v, false);
            y = Section(left.transform, "화면", y - 10);
            y = ToggleRow(left.transform, "전체 화면", y, () => D.fullscreen, v => D.fullscreen = v, true);
            y = CycleRow(left.transform, "해상도", y, ResLabel, dir => CycleRes(dir), true);
            y = CycleRow(left.transform, "프레임 제한", y, () => D.fps > 0 ? $"{D.fps} FPS" : "제한 없음",
                dir => D.fps = Cycle(GameSettings.FpsOptions, D.fps, dir), false);
            y = ToggleRow(left.transform, "수직 동기화", y, () => D.vsync, v => D.vsync = v, false);
            y = Section(left.transform, "게임", y - 10);
            y = CycleRow(left.transform, "연출 속도", y, () => $"x{D.gameSpeed:0.##}",
                dir => D.gameSpeed = Cycle(GameSettings.SpeedOptions, D.gameSpeed, dir), false);
            y = CycleRow(left.transform, "사용 가능 카드 테두리", y, () => D.playableFrame >= 2 ? "굵게" : D.playableFrame == 1 ? "얇게" : "끔",
                dir => D.playableFrame = (D.playableFrame + dir + 3) % 3, false);
            y = ToggleRow(left.transform, "전투 로그 표시", y, () => D.showLog, v => D.showLog = v, false);
            y = ToggleRow(left.transform, "가공 카드 자동 잠금", y, () => Profile.Data.autoLockFinish, v => { Profile.Data.autoLockFinish = v; Profile.Save(); }, false);

            // ── 오른쪽: 키 설정 ──
            var right = UIKit.Frame(page, "Keys", Dark);
            right.rectTransform.Anchor(0.5f, 0, 1, 1, 12, 0, 0, 0);
            float ky = Section(right.transform, "키 설정", -26);
            foreach (GameAction a in Enum.GetValues(typeof(GameAction)))
            {
                var act = a;
                var row = RowBg(right.transform, GameSettings.ActionNames[a], ky, 50);
                var b = UIKit.Button(row, "", () => StartListen(act), Row, 22, UIKit.Gold);
                b.rt.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(210, 42), new Vector2(1, 0.5f));
                keyButtons[a] = b;
                ky -= 55;
            }
            keyNote = UIKit.Txt(right.transform, "Note", "", 18, UIKit.Muted, TextAnchor.UpperLeft);
            keyNote.rectTransform.Anchor(0, 1, 1, 1);
            keyNote.rectTransform.pivot = new Vector2(0.5f, 1);
            keyNote.rectTransform.offsetMin = new Vector2(34, ky - 70);
            keyNote.rectTransform.offsetMax = new Vector2(-24, ky - 6);
            var rk = UIKit.Button(right.transform, "키 기본값", () => { listening = null; GameSettings.ResetKeys(); RefreshKeys(); }, new Color(0.12f, 0.05f, 0.05f, 0.95f), 20, new Color(1f, 0.6f, 0.55f));
            rk.rt.Place(new Vector2(1, 0), new Vector2(-24, 20), new Vector2(200, 54), new Vector2(1, 0));
            // 모든 설정 기본값: 왼쪽 목록 맨 아래와 겹쳐서 오른쪽 [키 기본값] 옆으로
            var reset = UIKit.Button(right.transform, "모든 설정 기본값", () => { GameSettings.ResetAll(); RefreshAll(); }, new Color(0.12f, 0.05f, 0.05f, 0.95f), 20, new Color(1f, 0.6f, 0.55f));
            reset.rt.Place(new Vector2(1, 0), new Vector2(-236, 20), new Vector2(230, 54), new Vector2(1, 0));

            return page;
        }

        public void Refresh()
        {
            listening = null;
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var r in refreshers) r();
            RefreshKeys();
        }

        void RefreshKeys()
        {
            foreach (var kv in keyButtons)
            {
                bool on = listening == kv.Key;
                kv.Value.label.text = on ? "키를 누르세요…" : GameSettings.KeyLabel(kv.Key);
                kv.Value.bg.color = on ? Accent : Row;
                kv.Value.label.color = on ? Color.white : UIKit.Gold;
            }
            keyNote.text = listening.HasValue
                ? "원하는 키를 누르세요.  Esc = 취소 · 이미 쓰는 키는 서로 맞바뀝니다."
                : "버튼을 누른 뒤 원하는 키를 누르면 변경됩니다.\n공격 대상 1~7은 상대 칸 번호(왼쪽부터)이며 숫자 패드도 사용할 수 있습니다.";
        }

        void StartListen(GameAction a)
        {
            listening = a;
            RefreshKeys();
        }

        void Update()
        {
            if (dirty && Time.unscaledTime > saveAt) { dirty = false; GameSettings.Save(); }
            if (!listening.HasValue || page == null || !page.gameObject.activeInHierarchy) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            foreach (var k in kb.allKeys)
            {
                if (k == null || !k.wasPressedThisFrame) continue;
                if (k.keyCode == Key.Escape) { listening = null; RefreshKeys(); return; }
                GameSettings.Bind(listening.Value, k.keyCode);
                listening = null;
                Sfx.Play("click");
                RefreshKeys();
                return;
            }
        }

        void Changed(bool display)
        {
            GameSettings.Apply(display);
            dirty = true;
            saveAt = Time.unscaledTime + 0.4f;
            foreach (var r in refreshers) r();
        }

        // ── 행 구성 ──

        float Section(Transform parent, string title, float y)
        {
            var t = UIKit.Txt(parent, "Sec_" + title, title, 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(30, y), new Vector2(500, 40), new Vector2(0, 1));
            t.Section(UIKit.SecMe);
            return y - 52;
        }

        RectTransform RowBg(Transform parent, string label, float y, float h = RowH - 6)
        {
            var bg = UIKit.Img(parent, "Row_" + label, Row, GameAssets.Rounded, true);
            bg.rectTransform.Anchor(0, 1, 1, 1);
            bg.rectTransform.pivot = new Vector2(0.5f, 1);
            bg.rectTransform.offsetMin = new Vector2(24, y - h);
            bg.rectTransform.offsetMax = new Vector2(-24, y);
            var t = UIKit.Txt(bg.transform, "Label", label, 22, UIKit.Ink, TextAnchor.MiddleLeft);
            t.rectTransform.Anchor(0, 0, 0.45f, 1, 18, 0, 0, 0);
            return bg.rectTransform;
        }

        float SliderRow(Transform parent, string label, float y, Func<float> get, Action<float> set, bool preview = false)
        {
            var row = RowBg(parent, label, y);
            var val = UIKit.Txt(row, "Val", "", 22, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            val.rectTransform.Anchor(1, 0, 1, 1, -88, 0, 16, 0);
            var slider = MakeSlider(row, get(), v =>
            {
                set(v);
                val.text = $"{Mathf.RoundToInt(v * 100)}%";
                Changed(false);
                if (preview) Sfx.Play("click");
            });
            ((RectTransform)slider.transform).Anchor(0.42f, 0.18f, 1, 0.82f, 0, 0, 100, 0);
            refreshers.Add(() => { slider.SetValueWithoutNotify(get()); val.text = $"{Mathf.RoundToInt(get() * 100)}%"; });
            return y - RowH;
        }

        float ToggleRow(Transform parent, string label, float y, Func<bool> get, Action<bool> set, bool display)
        {
            var row = RowBg(parent, label, y);
            UIKit.Btn b = null;
            b = UIKit.Button(row, "", () => { set(!get()); Changed(display); }, Row, 22, UIKit.Gold);
            b.rt.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(180, 42), new Vector2(1, 0.5f));
            refreshers.Add(() =>
            {
                bool on = get();
                b.label.text = on ? "켜짐" : "꺼짐";
                b.bg.color = on ? Accent : Row;
                b.label.color = on ? Color.white : UIKit.Muted;
            });
            return y - RowH;
        }

        float CycleRow(Transform parent, string label, float y, Func<string> text, Action<int> step, bool display)
        {
            var row = RowBg(parent, label, y);
            var prev = UIKit.Button(row, "◀", () => { step(-1); Changed(display); }, Row, 20, UIKit.Gold);
            prev.rt.Place(new Vector2(1, 0.5f), new Vector2(-270, 0), new Vector2(52, 42), new Vector2(1, 0.5f));
            var next = UIKit.Button(row, "▶", () => { step(1); Changed(display); }, Row, 20, UIKit.Gold);
            next.rt.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(52, 42), new Vector2(1, 0.5f));
            var val = UIKit.Txt(row, "Val", "", 22, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            val.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-70, 0), new Vector2(194, 42), new Vector2(1, 0.5f));
            refreshers.Add(() => val.text = text());
            return y - RowH;
        }

        static Slider MakeSlider(Transform parent, float value, Action<float> onChange)
        {
            var rt = UIKit.Rect("Slider", parent);
            var track = UIKit.Img(rt, "Track", new Color(0.16f, 0.13f, 0.22f, 1f), GameAssets.Rounded, true);
            track.rectTransform.Anchor(0, 0.36f, 1, 0.64f);
            track.raycastTarget = true;
            var fillArea = UIKit.Rect("FillArea", rt).Anchor(0, 0.36f, 1, 0.64f);
            var fill = UIKit.Img(fillArea, "Fill", UIKit.Gold, GameAssets.Rounded, true);
            fill.rectTransform.Anchor(0, 0, 1, 1);
            var handleArea = UIKit.Rect("HandleArea", rt).Anchor(0, 0, 1, 1, 14, 0, 14, 0);
            var handle = UIKit.Img(handleArea, "Handle", Color.white, GameAssets.Circle);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(30, 0);
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0;
            s.maxValue = 1;
            s.SetValueWithoutNotify(value);
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        // ── 값 순환 ──

        static int Cycle(int[] opts, int cur, int dir)
        {
            int i = Array.IndexOf(opts, cur);
            if (i < 0) i = 1;
            return opts[(i + dir + opts.Length) % opts.Length];
        }

        static float Cycle(float[] opts, float cur, int dir)
        {
            int i = 0;
            float best = float.MaxValue;
            for (int k = 0; k < opts.Length; k++) if (Mathf.Abs(opts[k] - cur) < best) { best = Mathf.Abs(opts[k] - cur); i = k; }
            return opts[(i + dir + opts.Length) % opts.Length];
        }

        string ResLabel()
        {
            int w = D.resW > 0 ? D.resW : Screen.width, h = D.resH > 0 ? D.resH : Screen.height;
            return $"{w} × {h}";
        }

        void CycleRes(int dir)
        {
            var list = GameSettings.Resolutions;
            int w = D.resW > 0 ? D.resW : Screen.width;
            int i = 0;
            for (int k = 0; k < list.Length; k++) if (list[k].x <= w) i = k;
            i = (i + dir + list.Length) % list.Length;
            D.resW = list[i].x;
            D.resH = list[i].y;
        }
    }
}
