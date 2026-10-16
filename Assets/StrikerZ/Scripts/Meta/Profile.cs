using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    [Serializable]
    public class CardCount
    {
        public string id;
        public int n;
        /// <summary>n 장 중 가공된 장수</summary>
        public int seven, strikerz;
    }

    [Serializable]
    public class DeckData
    {
        public string name = "새 덱";
        public string heroId = "H001";
        public List<string> cards = new List<string>();
        /// <summary>엑스트라 덱 (필드 카드 id, 최대 10장 · 같은 필드 1장씩)</summary>
        public List<string> extra = new List<string>();

        public DeckData Clone() => new DeckData { name = name, heroId = heroId, cards = new List<string>(cards), extra = new List<string>(extra ?? new List<string>()) };
        /// <summary>메인 + 엑스트라 덱에 든 장수</summary>
        public int Count(string id) => cards.Count(c => c == id) + (extra != null && extra.Contains(id) ? 1 : 0);
    }

    /// <summary>플레이어 저장 데이터 (프로필·재화·컬렉션·덱). 스팀/모바일 모두 로컬 JSON 으로 저장.</summary>
    [Serializable]
    public class ProfileData
    {
        public int version = 8;
        public string nickname = "스트라이커";
        public string avatar = "avatar_01";
        /// <summary>재화 오팔 (v1 의 젬을 이어받음)</summary>
        public int opal;
        /// <summary>v1 호환용 (불러올 때 오팔로 옮기고 0 으로 비움)</summary>
        public int gems;
        public QuestSave quests = new QuestSave();
        /// <summary>로지의 상점: 오늘의 특가</summary>
        public ShopSave shop = new ShopSave();
        /// <summary>등급별 제작 포인트 (Common, Uncommon, Rare, Unique)</summary>
        public List<int> cp = new List<int> { 0, 0, 0, 0 };
        /// <summary>분해 잠금 "카드id:가공"</summary>
        public List<string> locks = new List<string>();
        /// <summary>가공 카드를 얻으면 자동으로 잠금</summary>
        public bool autoLockFinish = true;
        /// <summary>전적 = 온라인 대전만 (미구현 — CPU·이벤트·투기장 대전은 세지 않는다. 대전 기록은 듀얼 연대기)</summary>
        public int wins, losses;
        public int packsOpened;
        public List<CardCount> collection = new List<CardCount>();
        public List<DeckData> decks = new List<DeckData>();
        public int activeDeck;
        public List<string> seenCards = new List<string>();   // NEW 표시용
        /// <summary>클리어한 레이드 보스 id (중복 없음) · 레이드 클리어 총 횟수</summary>
        public List<string> raidCleared = new List<string>();
        public int raidClears;
        /// <summary>클리어한 스토리 스테이지 id · 스토리로 해금한 영웅 id</summary>
        public List<string> storyCleared = new List<string>();
        public List<string> unlockedHeroes = new List<string>();
        /// <summary>프로필 대표 카드 6칸 (카드 id, "" = 빈 칸). 비어 있으면 자동 선택</summary>
        public List<string> showcase = new List<string>();
        /// <summary>꾸미기 아이템: 구매한 id · 종류별 장착 id ("" = 기본)</summary>
        public List<string> cosmetics = new List<string>();
        public string sleeve = "", deckCase = "", coin = "", dice = "";
        /// <summary>루콘 공방 첫 방문 인사를 봤는지</summary>
        public bool lukonMet;
        /// <summary>마력부여실(로한·벨라) 첫 방문 인사를 봤는지</summary>
        public bool parentsMet;
        /// <summary>CPU 대전 난이도 (0 수련 · 1 정예 · 2 전설)</summary>
        public int cpuLevel = 1;
        /// <summary>받은 원소 스타터 덱 (속성 이름)</summary>
        public List<string> starters = new List<string>();
        /// <summary>듀얼 연대기: 최근 대전 기록 · 카드별 사용 횟수</summary>
        public List<MatchRecord> chronicle = new List<MatchRecord>();
        public List<StatEntry> cardUse = new List<StatEntry>();
        /// <summary>클리어한 마법진 퍼즐 id</summary>
        public List<string> puzzlesCleared = new List<string>();
        /// <summary>세븐 패스 · 출석체크</summary>
        public PassSave pass = new PassSave();
        public AttendSave attend = new AttendSave();
        /// <summary>원소 이변전: 이번 주 기록</summary>
        public EventSave eventSave = new EventSave();
        /// <summary>원소 투기장 진행</summary>
        public ArenaSave arena = new ArenaSave();
    }

    public static class Profile
    {
        public const int StartOpal = 1500;

        public static readonly string[] Avatars = { "avatar_01", "avatar_02", "avatar_03", "avatar_04", "avatar_05", "avatar_06", "avatar_07", "avatar_08" };
        public const string CpuAvatar = "avatar_cpu";

        static ProfileData data;
        public static event Action Changed;

        /// <summary>자동 테스트 중에는 실제 저장 데이터를 건드리지 않는다</summary>
        public static bool TestMode;

        public static string SavePath => Path.Combine(Application.persistentDataPath, TestMode ? "strikerz_profile_test.json" : "strikerz_profile.json");

        public static ProfileData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SavePath)) data = JsonUtility.FromJson<ProfileData>(File.ReadAllText(SavePath));
            }
            catch (Exception e) { Debug.LogWarning("[StrikerZ] 저장 데이터를 읽지 못했습니다: " + e.Message); }
            if (data == null || data.decks == null || data.decks.Count == 0)
            {
                data = NewProfile();
                Save();
            }
            else if (data.version < 2)
            {
                // v1 → v2: 젬 → 오팔
                data.opal += data.gems;
                data.gems = 0;
                data.version = 2;
                if (data.quests == null) data.quests = new QuestSave();
                Save();
            }
            if (data.quests == null) data.quests = new QuestSave();
            if (data.version < 3)
            {
                // v2 → v3: 몬스터 270종 추가 — 새 N 몬스터를 기본 카드로 3장씩 지급
                foreach (var c in CardDB.All)
                    if (c.type == CardType.Monster && c.rarity == Rarity.Common && Owned(c.id) == 0) AddCard(c.id, 3, markNew: true);
                data.version = 3;
                Save();
            }
            if (data.version < 4)
            {
                // v3 → v4: 등급 개편 (N·R→Common, SR→Uncommon, SSR·UR→Rare, 신화→Unique). 등급별 CP 를 새 등급으로 합친다
                var o = data.cp ?? new List<int>();
                while (o.Count < 6) o.Add(0);
                data.cp = new List<int> { o[0] + o[1], o[2], o[3] + o[4], o[5] };
                if (data.shop != null) data.shop.day = "";          // 오늘의 특가 다시 뽑기
                data.version = 4;
                Save();
            }
            if (data.version < 5)
            {
                // v4 → v5: 스토리 주인공 카일 지급 — 영웅 해금 + 카일 스타터 덱 추가 (사용 덱은 그대로)
                if (data.unlockedHeroes == null) data.unlockedHeroes = new List<string>();
                if (!data.unlockedHeroes.Contains(StoryData.Kai.id)) data.unlockedHeroes.Add(StoryData.Kai.id);
                if (!data.decks.Any(d => d.name == DeckRules.KaiStarterName)) data.decks.Add(DeckRules.KaiStarter());
                data.version = 5;
                Save();
            }
            if (data.version < 6)
            {
                // v5 → v6: 덱 40장 개편 — 40장이 안 되는 덱은 보유 카드로 채운다 (이미 든 카드는 그대로)
                foreach (var d in data.decks)
                    if (d.cards.Count < DeckRules.MinSize) DeckRules.TopUp(d);
                data.version = 6;
                Save();
            }
            if (data.version < 7)
            {
                // v6 → v7: 필드는 엑스트라 덱으로 — 메인 덱의 필드를 옮기고(같은 필드 1장씩) 빈 메인 칸은 보유 카드로 채운다
                foreach (var d in data.decks) DeckRules.MoveFieldsToExtra(d);
                data.version = 7;
                Save();
            }
            if (data.version < 8)
            {
                // v7 → v8: 전적은 온라인 대전 전용 — 지금까지 센 CPU 대전 승패는 지운다 (대전 기록은 듀얼 연대기에 남아 있음)
                data.wins = data.losses = 0;
                data.version = 8;
                Save();
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e) { Debug.LogWarning("[StrikerZ] 저장 실패: " + e.Message); }
            Changed?.Invoke();
        }

        /// <summary>처음 시작: 기본 카드(N/R 일부)·필드·오팔 지급, 스타터 덱 자동 구성</summary>
        static ProfileData NewProfile()
        {
            var p = new ProfileData { opal = StartOpal };
            data = p;
            foreach (var c in CardDB.All)
            {
                int n = 0;
                if (c.rarity == Rarity.Common) n = c.type == CardType.Field ? 1 : 3;
                if (n > 0) AddCard(c.id, n, markNew: false);
            }
            // 처음 시작: 주인공 카일 영웅 + 노멀 카드로 만든 카일 스타터 덱
            p.unlockedHeroes.Add(StoryData.Kai.id);
            p.decks.Add(DeckRules.KaiStarter());
            p.activeDeck = 0;
            return p;
        }

        public static void Reset()
        {
            data = NewProfile();
            Save();
        }

        /// <summary>컬렉션에서 그 카드 항목 (없으면 null)</summary>
        static CardCount Entry(string id) => Data.collection.FirstOrDefault(c => c.id == id);

        public static int Owned(string id) => Entry(id)?.n ?? 0;

        public static void AddCard(string id, int n = 1, bool markNew = true, CardFinish finish = CardFinish.Normal)
        {
            var d = data ?? Data;
            var e = d.collection.FirstOrDefault(c => c.id == id);
            if (e == null) d.collection.Add(e = new CardCount { id = id, n = 0 });
            e.n += n;
            if (finish == CardFinish.Seven) e.seven += n;
            else if (finish == CardFinish.StrikerZ) e.strikerz += n;
            if (data != null) Crafting.OnObtained(id, finish);
            if (!markNew && !d.seenCards.Contains(id)) d.seenCards.Add(id);
        }

        /// <summary>카드 제거 (분해). 해당 가공 사본에서 뺀다</summary>
        public static void RemoveCard(string id, int n, CardFinish finish)
        {
            var e = Entry(id);
            if (e == null) return;
            n = Math.Min(n, FinishCount(id, finish));
            if (n <= 0) return;
            e.n -= n;
            if (finish == CardFinish.Seven) e.seven -= n;
            else if (finish == CardFinish.StrikerZ) e.strikerz -= n;
            if (e.n <= 0) Data.collection.Remove(e);
        }

        /// <summary>사본 1장의 가공을 바꾼다 (마력부여기)</summary>
        public static void ChangeFinish(string id, CardFinish from, CardFinish to)
        {
            var e = Entry(id);
            if (e == null || from == to || FinishCount(id, from) <= 0) return;
            if (from == CardFinish.Seven) e.seven--;
            else if (from == CardFinish.StrikerZ) e.strikerz--;
            if (to == CardFinish.Seven) e.seven++;
            else if (to == CardFinish.StrikerZ) e.strikerz++;
            Crafting.OnObtained(id, to);
        }

        public static int FinishCount(string id, CardFinish f)
        {
            var e = Entry(id);
            if (e == null) return 0;
            return f == CardFinish.Seven ? e.seven : f == CardFinish.StrikerZ ? e.strikerz : e.n - e.seven - e.strikerz;
        }

        /// <summary>보유 중 가장 좋은 가공 (표시용)</summary>
        public static CardFinish BestFinish(string id)
        {
            var e = Entry(id);
            if (e == null) return CardFinish.Normal;
            return e.strikerz > 0 ? CardFinish.StrikerZ : e.seven > 0 ? CardFinish.Seven : CardFinish.Normal;
        }

        /// <summary>덱 카드 목록에 보유 가공을 좋은 것부터 배정 (StrikerZ → Seven → 일반)</summary>
        public static List<CardFinish> AssignFinishes(IList<CardData> cards)
        {
            var left = new Dictionary<string, (int z, int s)>();
            var result = new List<CardFinish>();
            foreach (var c in cards)
            {
                if (c == null) { result.Add(CardFinish.Normal); continue; }
                if (!left.TryGetValue(c.id, out var r))
                {
                    var e = Entry(c.id);
                    r = e == null ? (0, 0) : (e.strikerz, e.seven);
                }
                if (r.z > 0) { result.Add(CardFinish.StrikerZ); r.z--; }
                else if (r.s > 0) { result.Add(CardFinish.Seven); r.s--; }
                else result.Add(CardFinish.Normal);
                left[c.id] = r;
            }
            return result;
        }

        public static bool IsNew(string id) => Owned(id) > 0 && !Data.seenCards.Contains(id);

        public static void MarkSeen(string id)
        {
            if (!Data.seenCards.Contains(id)) Data.seenCards.Add(id);
        }

        public static int OwnedKinds => Data.collection.Count(c => c.n > 0);

        public static DeckData ActiveDeck
        {
            get
            {
                var d = Data;
                if (d.activeDeck < 0 || d.activeDeck >= d.decks.Count) d.activeDeck = 0;
                return d.decks[d.activeDeck];
            }
        }

        public static bool Spend(int opal)
        {
            if (Data.opal < opal) return false;
            Data.opal -= opal;
            return true;
        }

        /// <summary>매치 결과 반영 (오팔은 퀘스트·업적으로 획득). 받을 수 있는 퀘스트 보상 수 반환</summary>
        static readonly System.Random lootRng = new System.Random();

        /// <summary>레이드 결과 기록. 클리어 시 전리품 지급: 오팔 + 등급별 랜덤 카드 1장씩 (지급 내역 반환)</summary>
        public static (int opal, List<CardData> cards) RecordRaid(string bossId, bool won)
        {
            if (Data.raidCleared == null) Data.raidCleared = new List<string>();
            Quests.Add(Stat.MatchPlayed);
            int reward = 0;
            var cards = new List<CardData>();
            if (won)
            {
                reward = RaidData.ClearOpal;
                Data.opal += reward;
                foreach (var r in RaidData.LootRarities)
                {
                    var c = RaidData.RollLoot(r, lootRng);
                    if (c == null) continue;
                    AddCard(c.id, 1, true);
                    cards.Add(c);
                }
                Data.raidClears++;
                if (!Data.raidCleared.Contains(bossId)) Data.raidCleared.Add(bossId);
                Quests.Add(Stat.MatchWon);
            }
            Save();
            return (reward, cards);
        }

        public static bool StoryCleared(string stageId) => Data.storyCleared != null && Data.storyCleared.Contains(stageId);
        public static bool HeroUnlocked(string heroId) => Data != null && Data.unlockedHeroes != null && Data.unlockedHeroes.Contains(heroId);

        /// <summary>스토리 전투 결과 기록. 첫 클리어 시 오팔 + 영웅 해금 (지급 내역 반환)</summary>
        public static (bool first, int opal, string hero) RecordStory(StoryStage st, bool won)
        {
            if (Data.storyCleared == null) Data.storyCleared = new List<string>();
            if (Data.unlockedHeroes == null) Data.unlockedHeroes = new List<string>();
            Quests.Add(Stat.MatchPlayed);
            if (won) Quests.Add(Stat.MatchWon);
            bool cleared = won || st.battle.allowLose;
            bool first = cleared && !Data.storyCleared.Contains(st.id);
            int opal = 0;
            string hero = null;
            if (first)
            {
                Data.storyCleared.Add(st.id);
                opal = st.rewardOpal;
                Data.opal += opal;
                if (!string.IsNullOrEmpty(st.rewardHero) && !Data.unlockedHeroes.Contains(st.rewardHero))
                {
                    Data.unlockedHeroes.Add(st.rewardHero);
                    hero = st.rewardHero;
                }
            }
            Save();
            return (first, opal, hero);
        }

        public static bool RaidCleared(string bossId) => Data.raidCleared != null && Data.raidCleared.Contains(bossId);

        /// <summary>일반 CPU 대전 결과. 승리하면 난이도 보너스 오팔 (bonus) 지급</summary>
        public static int RecordMatch(bool won, int bonus = 0)
        {
            if (won && bonus > 0) Data.opal += bonus;
            Quests.Add(Stat.MatchPlayed);
            if (won) Quests.Add(Stat.MatchWon);
            Save();
            return Quests.ClaimableCount;
        }
    }
}
