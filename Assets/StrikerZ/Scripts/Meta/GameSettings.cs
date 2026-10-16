using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrikerZ
{
    /// <summary>단축키로 실행할 수 있는 동작</summary>
    public enum GameAction
    {
        Target1, Target2, Target3, Target4, Target5, Target6, Target7,
        Attack, NoAttack, BattlePhase, EndTime, HeroSkill,
    }

    [Serializable]
    public class KeyBind
    {
        public string action;
        public string key;
    }

    [Serializable]
    public class SettingsData
    {
        public float master = 1f, music = 0.8f, sfx = 1f;
        public bool mute;
        public bool fullscreen = true;
        public int resW, resH;          // 0 = 현재 해상도 유지
        public int fps = 60;            // 0 = 제한 없음
        public bool vsync;
        public float gameSpeed = 1f;
        public bool showLog = true;
        /// <summary>사용 가능한 카드 테두리: 0 끔 · 1 얇게 · 2 굵게</summary>
        public int playableFrame = 2;
        /// <summary>CPU · 스토리 대전 2배속 (대전 화면 ▶▶ 버튼, 다음 대전에도 유지)</summary>
        public bool battleX2;
        public List<KeyBind> keys = new List<KeyBind>();
    }

    /// <summary>
    /// 기기별 설정 (볼륨 · 화면 · 게임 속도 · 키 설정). PlayerPrefs 에 JSON 으로 저장한다.
    /// 프로필(세이브 데이터)과 달리 계정이 아닌 기기에 속한다.
    /// </summary>
    public static class GameSettings
    {
        public static SettingsData Data { get; private set; } = new SettingsData();
        public static event Action Changed;
        /// <summary>지금 2배속을 쓸 수 있는 대전 중인가 (HUD 가 매 프레임 갱신)</summary>
        public static bool BoostActive;
        public static float BattleBoost => BoostActive && Data.battleX2 ? 2f : 1f;

        static string PrefKey => Profile.TestMode ? "strikerz_settings_test" : "strikerz_settings";

        public static readonly Dictionary<GameAction, string> ActionNames = new Dictionary<GameAction, string>
        {
            { GameAction.Target1, "공격 대상 1번 칸" }, { GameAction.Target2, "공격 대상 2번 칸" },
            { GameAction.Target3, "공격 대상 3번 칸" }, { GameAction.Target4, "공격 대상 4번 칸" },
            { GameAction.Target5, "공격 대상 5번 칸" }, { GameAction.Target6, "공격 대상 6번 칸" },
            { GameAction.Target7, "공격 대상 7번 칸" },
            { GameAction.Attack, "공격 (확인)" },
            { GameAction.NoAttack, "공격 안 함" },
            { GameAction.BattlePhase, "배틀 페이즈" },
            { GameAction.EndTime, "타임 종료" },
            { GameAction.HeroSkill, "영웅 능력" },
        };

        public static readonly Dictionary<GameAction, Key> DefaultKeys = new Dictionary<GameAction, Key>
        {
            { GameAction.Target1, Key.Digit1 }, { GameAction.Target2, Key.Digit2 }, { GameAction.Target3, Key.Digit3 },
            { GameAction.Target4, Key.Digit4 }, { GameAction.Target5, Key.Digit5 }, { GameAction.Target6, Key.Digit6 },
            { GameAction.Target7, Key.Digit7 },
            { GameAction.Attack, Key.Enter },
            { GameAction.NoAttack, Key.Space },
            { GameAction.BattlePhase, Key.B },
            { GameAction.EndTime, Key.E },
            { GameAction.HeroSkill, Key.H },
        };

        public static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440), new Vector2Int(3840, 2160),
        };
        public static readonly int[] FpsOptions = { 30, 60, 120, 144, 0 };
        public static readonly float[] SpeedOptions = { 0.75f, 1f, 1.25f, 1.5f, 2f };

        static readonly Dictionary<GameAction, Key> keys = new Dictionary<GameAction, Key>();

        public static void Load()
        {
            Data = new SettingsData();
            try
            {
                string json = PlayerPrefs.GetString(PrefKey, "");
                if (!string.IsNullOrEmpty(json)) Data = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
            }
            catch (Exception e) { Debug.LogWarning("[StrikerZ] 설정 불러오기 실패: " + e.Message); Data = new SettingsData(); }
            RebuildKeys();
        }

        public static void Save()
        {
            Data.keys.Clear();
            foreach (var kv in keys) Data.keys.Add(new KeyBind { action = kv.Key.ToString(), key = kv.Value.ToString() });
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        static void RebuildKeys()
        {
            keys.Clear();
            foreach (var kv in DefaultKeys) keys[kv.Key] = kv.Value;
            if (Data.keys == null) Data.keys = new List<KeyBind>();
            foreach (var b in Data.keys)
                if (Enum.TryParse(b.action, out GameAction a) && Enum.TryParse(b.key, out Key k)) keys[a] = k;
        }

        /// <summary>볼륨 · 화면 · 속도를 실제로 반영</summary>
        public static void Apply(bool display = true)
        {
            // 자동 테스트(빌드 확인용 실행)는 항상 음소거
            AudioListener.volume = Data.mute || AutoTest.Enabled ? 0f : Mathf.Clamp01(Data.master);
            Sfx.volume = 0.55f * Mathf.Clamp01(Data.sfx);
            Sfx.musicVolume = 0.4f * Mathf.Clamp01(Data.music);
            QualitySettings.vSyncCount = Data.vsync ? 1 : 0;
            Application.targetFrameRate = Data.vsync ? -1 : (Data.fps > 0 ? Data.fps : -1);
            if (!AutoTest.Enabled) Time.timeScale = Mathf.Clamp(Data.gameSpeed, 0.5f, 3f) * BattleBoost;
            if (display && !Application.isEditor && !AutoTest.Enabled)
            {
                var mode = Data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                if (Data.resW > 0 && Data.resH > 0) Screen.SetResolution(Data.resW, Data.resH, mode);
                else Screen.fullScreenMode = mode;
            }
            Changed?.Invoke();
        }

        public static void ResetAll()
        {
            Data = new SettingsData();
            RebuildKeys();
            Save();
            Apply();
        }

        // ── 키 ──

        public static Key KeyOf(GameAction a) => keys.TryGetValue(a, out var k) ? k : Key.None;

        /// <summary>키 지정. 이미 다른 동작에 쓰이던 키면 서로 맞바꾼다.</summary>
        public static void Bind(GameAction a, Key k)
        {
            var old = KeyOf(a);
            foreach (var kv in new List<KeyValuePair<GameAction, Key>>(keys))
                if (kv.Key != a && kv.Value == k) keys[kv.Key] = old;
            keys[a] = k;
            Save();
            Changed?.Invoke();
        }

        public static void ResetKeys()
        {
            foreach (var kv in DefaultKeys) keys[kv.Key] = kv.Value;
            Save();
            Changed?.Invoke();
        }

        public static bool Pressed(GameAction a)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            var k = KeyOf(a);
            if (k != Key.None && kb[k].wasPressedThisFrame) return true;
            // 공격 대상은 숫자 패드도 항상 허용
            if (a >= GameAction.Target1 && a <= GameAction.Target7)
                return kb[Key.Numpad1 + (a - GameAction.Target1)].wasPressedThisFrame;
            return false;
        }

        public static string KeyLabel(GameAction a) => KeyName(KeyOf(a));

        public static string KeyName(Key k)
        {
            if (k == Key.None) return "-";
            if (k >= Key.Digit1 && k <= Key.Digit0) return k == Key.Digit0 ? "0" : ((int)(k - Key.Digit1) + 1).ToString();
            if (k >= Key.Numpad0 && k <= Key.Numpad9) return "Num" + (int)(k - Key.Numpad0);
            switch (k)
            {
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Escape: return "Esc";
                case Key.Tab: return "Tab";
                case Key.LeftShift: return "L-Shift";
                case Key.RightShift: return "R-Shift";
                case Key.LeftCtrl: return "L-Ctrl";
                case Key.RightCtrl: return "R-Ctrl";
                case Key.LeftAlt: return "L-Alt";
                case Key.Backspace: return "Backspace";
                default: return k.ToString();
            }
        }
    }
}
