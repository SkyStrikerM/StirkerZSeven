using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StrikerZ
{
    [Serializable] public class SealBuff { public int value, until; }

    [Serializable]
    public class SealCard
    {
        public string id;
        public int finish, slot = -1;
        public bool faceDown, fx;
        public int damage, tempAtk, weaken, stunUntil = -1, tauntUntil = -1, noRegenUntil = -1;
        public List<SealBuff> buffs = new List<SealBuff>();
        public List<SealBuff> hpBuffs = new List<SealBuff>();
    }

    [Serializable]
    public class SealPlayer
    {
        public string hero;
        public int hp, maxHp;
        public bool heroUsed, deckOutWarned;
        public List<SealCard> deck = new List<SealCard>(), hand = new List<SealCard>(), grave = new List<SealCard>(), slots = new List<SealCard>(), extra = new List<SealCard>();
    }

    [Serializable]
    public class SealStatus
    {
        public int kind, expire, value, createdTime, target = -1, caster = -1;
        public string source;
    }

    /// <summary>시간 봉인: T 가 시작될 때마다 남기는 대전 상태 (일반 대전 · 원소 이변전 · 원소 투기장)</summary>
    [Serializable]
    public class SealData
    {
        public int version = 1;
        public string savedAt;
        public int mode, cpuLevel, eventRule;
        public int time;
        public bool firstHuman;
        /// <summary>필드 선(T2 에 먼저 필드)이 사람인가</summary>
        public bool fieldFirstHuman;
        public int duelRounds;
        public string cpuName, deckName, field = "";
        public int fieldOwner = -1;
        public List<string> deckIds = new List<string>();
        public SealPlayer human = new SealPlayer(), cpu = new SealPlayer();
        public List<SealStatus> statuses = new List<SealStatus>();
    }

    /// <summary>시간 봉인 파일 (기기 로컬, 대전이 끝나면 지운다)</summary>
    public static class TimeSeal
    {
        static string PathOf => Path.Combine(Application.persistentDataPath, Profile.TestMode ? "strikerz_seal_test.json" : "strikerz_seal.json");

        public static bool Exists => File.Exists(PathOf);

        public static void Save(SealData d)
        {
            try { File.WriteAllText(PathOf, JsonUtility.ToJson(d)); }
            catch (Exception e) { Debug.LogWarning("[StrikerZ] 시간 봉인 저장 실패: " + e.Message); }
        }

        public static SealData Load()
        {
            try { return Exists ? JsonUtility.FromJson<SealData>(File.ReadAllText(PathOf)) : null; }
            catch (Exception e) { Debug.LogWarning("[StrikerZ] 시간 봉인 읽기 실패: " + e.Message); return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(PathOf)) File.Delete(PathOf); } catch { }
        }

        public static string Describe(SealData d)
        {
            if (d == null) return "";
            var mode = (MatchMode)d.mode;
            string m = mode == MatchMode.Event ? "원소 이변전" : mode == MatchMode.Arena ? "원소 투기장" : $"CPU 대전 · {GameManager.LevelName((CpuLevel)d.cpuLevel)}";
            return $"{m} · TIME {d.time} · HP {d.human.hp} : {d.cpu.hp} · {d.savedAt}";
        }
    }
}
