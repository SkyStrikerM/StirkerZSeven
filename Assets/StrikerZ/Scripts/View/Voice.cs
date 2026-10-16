using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 캐릭터 음성 (VARCO Voice 로 만든 대사). Resources/StrikerZ/Audio/Voice/ 아래:
    ///   영웅  hero_{카드id}_{atk|hit|skill}   — 영웅 반격 · 피격 · 영웅 능력 사용
    ///   NPC   npc_{화자}_{대사 MD5 앞 8자리}   — 상점 대화창에 뜨는 고정 대사 (Tools/voice/make_lines.py 와 같은 규칙)
    /// 파일이 없으면 조용히 넘어간다. 새 대사가 나오면 하던 말은 끊는다 (효과음과 따로 재생).
    /// </summary>
    public static class Voice
    {
        public const string Dir = "StrikerZ/Audio/Voice/";
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> lastHit = new Dictionary<string, float>();

        static AudioSource Src
        {
            get
            {
                if (src != null) return src;
                var go = new GameObject("Voice");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0;
                return src;
            }
        }

        static AudioClip Load(string name)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>(Dir + name);
            cache[name] = c;
            return c;
        }

        static void Play(AudioClip c, float vol = 1f)
        {
            if (c == null || Sfx.Disabled) return;
            var s = Src;
            s.Stop();
            s.clip = c;
            s.volume = Mathf.Clamp01(Sfx.volume * 1.6f * vol);
            s.Play();
        }

        /// <summary>영웅 음성은 일단 뺀다 (녹음본은 Tools/voice/hero_ogg 에 보관 — 다시 쓰려면 Voice/ 로 옮기고 true)</summary>
        public static bool HeroVoices = false;

        /// <summary>영웅 음성: kind = atk(반격) · hit(피격) · skill(능력). 피격은 1.2초에 한 번만</summary>
        public static void Hero(CardData hero, string kind)
        {
            if (!HeroVoices || hero == null || hero.type != CardType.Hero) return;
            if (kind == "hit")
            {
                if (lastHit.TryGetValue(hero.id, out var t) && Time.unscaledTime - t < 1.2f) return;
                lastHit[hero.id] = Time.unscaledTime;
            }
            Play(Load($"hero_{hero.id}_{kind}"));
        }

        /// <summary>NPC 대사: 대화창에 뜬 문장과 같은 음성이 있으면 재생 (이름이 들어간 대사처럼 녹음이 없으면 조용히)</summary>
        public static void Line(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Play(Load($"npc_{speaker}_{Key(text)}"));
        }

        /// <summary>대사 → 파일 키 (UTF-8 MD5 앞 8자리)</summary>
        public static string Key(string text)
        {
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(8);
                for (int i = 0; i < 4; i++) sb.Append(h[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static void Stop() { if (src != null) src.Stop(); }
    }
}
