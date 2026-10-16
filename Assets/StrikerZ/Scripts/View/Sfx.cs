using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 효과음. Resources/StrikerZ/Audio/{이름} (VARCO Sound 생성 음원)을 우선 사용하고,
    /// 파일이 없으면 절차적으로 합성한 소리로 대신한다. 팩 개봉 연출 등에서 사용.
    /// </summary>
    public static class Sfx
    {
        public static float volume = 0.55f;
        public static float musicVolume = 0.4f;
        const int Rate = 44100;
        static AudioSource src;
        static readonly AudioSource[] bgm = new AudioSource[2];
        static int bgmCur;
        static string bgmName;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly System.Random rnd = new System.Random(7);

        static AudioSource Src
        {
            get
            {
                if (src != null) return src;
                var go = new GameObject("Sfx");
                UnityEngine.Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0;
                for (int i = 0; i < 2; i++)
                {
                    bgm[i] = go.AddComponent<AudioSource>();
                    bgm[i].playOnAwake = false;
                    bgm[i].loop = true;
                    bgm[i].spatialBlend = 0;
                    bgm[i].volume = 0;
                }
                go.AddComponent<BgmFader>();
                if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
                return src;
            }
        }

        /// <summary>같은 소리를 여러 버전으로 둔 경우 무작위로 고른다 (rip → rip_a~d)</summary>
        static readonly Dictionary<string, string[]> variants = new Dictionary<string, string[]>
        {
            { "rip", new[] { "rip_a", "rip_b", "rip_c", "rip_d" } },
            { "attack", new[] { "attack", "attack2" } },
            { "bgm_menu", new[] { "bgm_menu", "bgm_menu2" } },
            { "bgm_battle", new[] { "bgm_battle", "bgm_battle2" } },
            { "bgm_pack", new[] { "bgm_pack", "bgm_pack2" } },
            { "bgm_rare", new[] { "bgm_rare", "bgm_rare2" } },
            { "bgm_shop", new[] { "bgm_shop", "bgm_shop2" } },   // 상점 (VARCO Sound, Tools/shop_bgm)
        };

        /// <summary>진단용: 실행 인자 -strikerz-noaudio 면 모든 소리를 끈다</summary>
        public static readonly bool Disabled = Environment.GetCommandLineArgs().Contains("-strikerz-noaudio");

        public static void Play(string name, float vol = 1f)
        {
            if (Disabled) return;
            if (variants.TryGetValue(name, out var vs)) name = vs[rnd.Next(vs.Length)];
            var c = Get(name);
            if (c != null) Src.PlayOneShot(c, vol * volume);
        }

        // ── 배경음악: 두 AudioSource 를 교차 페이드 ──

        static readonly float[] bgmTarget = new float[2];
        static float bgmFade = 1f;

        /// <summary>배경음악 전환 (같은 곡이면 무시). null 이면 페이드 아웃.</summary>
        public static void PlayBgm(string name, float fade = 1.2f)
        {
            if (Disabled) return;
            if (bgmName == name) return;
            bgmName = name;
            var _ = Src;
            string file = name != null && variants.TryGetValue(name, out var vs) ? vs[rnd.Next(vs.Length)] : name;
            var clip = file == null ? null : Resources.Load<AudioClip>("StrikerZ/Audio/" + file);
            bgmTarget[bgmCur] = 0;
            bgmFade = Mathf.Max(0.05f, fade);
            if (clip == null) return;
            bgmCur = 1 - bgmCur;
            var s = bgm[bgmCur];
            s.clip = clip;
            s.volume = 0;
            s.Play();
            bgmTarget[bgmCur] = 1;
        }

        public static void StopBgm(float fade = 1.2f) => PlayBgm(null, fade);

        /// <summary>지금 메뉴 화면의 BGM (상점 탭이면 bgm_shop). 팩 개봉이 끝나면 이 곡으로 돌아간다</summary>
        public static string MenuBgm = "bgm_menu";

        /// <summary>필드 전용 BGM (Audio/bgm_{필드 id}, VARCO Sound). 없으면 전투 BGM</summary>
        public static void PlayFieldBgm(CardData field, float fade = 1.6f)
        {
            if (Disabled) return;
            string name = field != null ? "bgm_" + field.id : null;
            bool has = name != null && Resources.Load<AudioClip>("StrikerZ/Audio/" + name) != null;
            if (bgmName != (has ? name : "bgm_battle")) Debug.Log($"[StrikerZ] BGM → {(has ? name : "bgm_battle")} ({field?.name})");
            PlayBgm(has ? name : "bgm_battle", fade);
        }

        class BgmFader : MonoBehaviour
        {
            void Update()
            {
                for (int i = 0; i < 2; i++)
                {
                    var s = bgm[i];
                    if (s == null) continue;
                    float target = bgmTarget[i] * musicVolume;
                    s.volume = Mathf.MoveTowards(s.volume, target, Time.unscaledDeltaTime * musicVolume / bgmFade);
                    if (s.isPlaying && target <= 0 && s.volume <= 0.001f) s.Stop();
                }
            }
        }

        /// <summary>등급 공개 효과음 (0=N … 5=신화)</summary>
        public static void Reveal(int tier) => Play("reveal" + Mathf.Clamp(tier, 0, 5));

        static AudioClip Get(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("StrikerZ/Audio/" + name);
            if (c != null) { clips[name] = c; return c; }
            float[] d = Make(name.Split('_')[0]);
            if (d == null) return null;
            Normalize(d, 0.85f);
            c = AudioClip.Create(name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            clips[name] = c;
            return c;
        }

        static float Noise() => (float)(rnd.NextDouble() * 2 - 1);

        static float[] Buf(float sec) => new float[Mathf.CeilToInt(sec * Rate)];

        static float[] Make(string name)
        {
            switch (name)
            {
                case "rip": return Rip(0.09f, 0.5f);
                case "tear": return Rip(0.5f, 1f);
                case "whoosh": return Whoosh(0.4f);
                case "flip": return Flip();
                case "boom": return Boom(1.6f);
                case "drop": return Drop();
            }
            if (name.StartsWith("charge")) return Charge(new[] { 0.5f, 0.5f, 0.5f, 0.85f, 1.15f, 1.7f }[Mathf.Clamp(name[name.Length - 1] - '0', 0, 5)]);
            if (name.StartsWith("reveal")) return RevealClip(name[name.Length - 1] - '0');
            return null;
        }

        // ── 합성 ──

        /// <summary>종이/포일 찢는 소리: 고역 노이즈 + 불규칙한 크래클</summary>
        static float[] Rip(float sec, float crackle)
        {
            var d = Buf(sec);
            float prev = 0, amp = 1;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)d.Length;
                if (i % 220 == 0) amp = 0.35f + (float)rnd.NextDouble() * 0.65f * crackle + (1 - crackle) * 0.5f;
                float n = Noise();
                float hp = n - prev; prev = n;
                float env = Mathf.Min(1, t * 30) * Mathf.Pow(1 - t, 1.4f);
                d[i] = hp * amp * env;
            }
            return d;
        }

        /// <summary>바람 가르는 소리: 저역 필터 노이즈, 필터가 열렸다 닫힘</summary>
        static float[] Whoosh(float sec)
        {
            var d = Buf(sec);
            float y = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)d.Length;
                float k = Mathf.Lerp(0.02f, 0.25f, Mathf.Sin(t * Mathf.PI));
                y += (Noise() - y) * k;
                d[i] = y * Mathf.Sin(t * Mathf.PI);
            }
            return d;
        }

        static float[] Flip()
        {
            var d = Buf(0.09f);
            for (int i = 0; i < d.Length; i++)
            {
                float s = i / (float)Rate;
                d[i] = (Noise() * 0.6f + Mathf.Sin(2 * Mathf.PI * 1700 * s) * 0.5f) * Mathf.Exp(-s * 70);
            }
            return d;
        }

        static float[] Drop()
        {
            var d = Buf(0.35f);
            for (int i = 0; i < d.Length; i++)
            {
                float s = i / (float)Rate;
                float f = Mathf.Lerp(180, 70, s / 0.35f);
                d[i] = Mathf.Sin(2 * Mathf.PI * f * s) * Mathf.Exp(-s * 12) + Noise() * 0.15f * Mathf.Exp(-s * 30);
            }
            return d;
        }

        /// <summary>기 모으는 소리: 상승하는 톤 + 트레몰로</summary>
        static float[] Charge(float sec)
        {
            var d = Buf(sec);
            double ph = 0, ph2 = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)d.Length;
                float f = Mathf.Lerp(160, 1100, t * t);
                ph += 2 * Math.PI * f / Rate;
                ph2 += 2 * Math.PI * f * 1.5 / Rate;
                float trem = 0.6f + 0.4f * Mathf.Sin(t * sec * 2 * Mathf.PI * Mathf.Lerp(6, 22, t));
                d[i] = ((float)Math.Sin(ph) * 0.6f + (float)Math.Sin(ph2) * 0.25f + Noise() * 0.08f) * trem * t * Mathf.Min(1, (1 - t) * 40);
            }
            return d;
        }

        /// <summary>폭발 저음: 하강 사인 + 럼블</summary>
        static float[] Boom(float sec)
        {
            var d = Buf(sec);
            double ph = 0;
            float y = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float s = i / (float)Rate;
                float f = Mathf.Lerp(120, 32, Mathf.Min(1, s / 0.8f));
                ph += 2 * Math.PI * f / Rate;
                y += (Noise() - y) * 0.05f;
                d[i] = ((float)Math.Sin(ph) * 0.9f + y * 1.6f) * Mathf.Exp(-s * 2.6f) * Mathf.Min(1, s * 200);
            }
            return d;
        }

        static void Bell(float[] d, float start, float freq, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            for (int i = s0; i < d.Length; i++)
            {
                float s = (i - s0) / (float)Rate;
                float e = Mathf.Exp(-s * decay) * Mathf.Min(1, s * 400);
                if (e < 0.0005f) break;
                d[i] += amp * e * (Mathf.Sin(2 * Mathf.PI * freq * s) + 0.35f * Mathf.Sin(2 * Mathf.PI * freq * 2.01f * s) + 0.12f * Mathf.Sin(2 * Mathf.PI * freq * 3.02f * s));
            }
        }

        /// <summary>등급이 높을수록 음이 많아지고 길어지는 아르페지오</summary>
        static float[] RevealClip(int tier)
        {
            float[] notes;   // C 장조 펜타토닉
            float gap;
            switch (tier)
            {
                case 0: notes = new[] { 784f }; gap = 0; break;
                case 1: notes = new[] { 784f, 1175f }; gap = 0.06f; break;
                case 2: notes = new[] { 659f, 880f, 1319f }; gap = 0.07f; break;
                case 3: notes = new[] { 523f, 659f, 784f, 1047f, 1568f }; gap = 0.07f; break;
                case 4: notes = new[] { 523f, 659f, 784f, 1047f, 1319f, 1568f, 2093f }; gap = 0.06f; break;
                default: notes = new[] { 392f, 523f, 659f, 784f, 1047f, 1319f, 1568f, 2093f, 2637f }; gap = 0.055f; break;
            }
            float len = 0.35f + tier * 0.3f + notes.Length * gap;
            var d = Buf(len);
            float decay = Mathf.Lerp(14, 3.2f, tier / 5f);
            for (int n = 0; n < notes.Length; n++) Bell(d, n * gap, notes[n], 0.5f, decay);
            if (tier >= 3)
            {
                // 최종 화음
                float at = notes.Length * gap;
                Bell(d, at, notes[0] * 2, 0.35f, decay * 0.7f);
                Bell(d, at, notes[2] * 2, 0.3f, decay * 0.7f);
                // 반짝이
                for (int k = 0; k < tier * 5; k++)
                    Bell(d, at + (float)rnd.NextDouble() * (len - at) * 0.7f, 2500 + (float)rnd.NextDouble() * 2500, 0.08f, 18);
            }
            if (tier >= 4)
            {
                // 저음 임팩트
                double ph = 0;
                for (int i = 0; i < d.Length; i++)
                {
                    float s = i / (float)Rate;
                    ph += 2 * Math.PI * Mathf.Lerp(110, 45, Mathf.Min(1, s / 0.6f)) / Rate;
                    d[i] += (float)Math.Sin(ph) * 0.7f * Mathf.Exp(-s * 3.5f) * Mathf.Min(1, s * 300);
                }
            }
            return d;
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f;
            foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }
    }
}
