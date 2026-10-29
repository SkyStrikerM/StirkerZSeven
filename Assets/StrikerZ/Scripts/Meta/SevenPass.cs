using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    [Serializable]
    public class PassSave
    {
        public string season = "";
        public int xp;
        public bool premium;
        public List<int> freeClaimed = new List<int>();
        public List<int> premClaimed = new List<int>();
    }

    public enum PassReward { Opal, Pack, CpCommon, CpUncommon, CpRare, SevenCard, UniqueCard, RareCard }

    /// <summary>
    /// 세븐 패스 (시즌 패스): 매달 새 시즌 · 30레벨 · 레벨당 100 XP.
    /// XP: 대전 1판 +40 (승리 +30) · 일일 퀘스트 보상 +50 · 마법진 퍼즐 첫 클리어 +60 · 출석체크 +30.
    /// 무료 트랙은 누구나, 프리미엄 트랙은 오팔로 연다. 시즌이 바뀌면 진행이 초기화된다.
    /// </summary>
    public static class SevenPass
    {
        public const int Levels = 30, XpPerLevel = 100, PremiumPrice = 1500;
        public const int XpMatch = 40, XpWin = 30, XpDaily = 50, XpPuzzle = 60, XpAttend = 30;

        static DateTime Now => DateTime.Now;
        public static string CurrentSeason => Now.ToString("yyyy-MM");
        /// <summary>2026년 10월 = 시즌 1</summary>
        public static int SeasonNumber => Math.Max(1, (Now.Year - 2026) * 12 + Now.Month - 10 + 1);
        public static TimeSpan UntilSeasonEnd => new DateTime(Now.Year, Now.Month, 1).AddMonths(1) - Now;

        public static PassSave S
        {
            get
            {
                var d = Profile.Data;
                if (d.pass == null) d.pass = new PassSave();
                if (d.pass.season != CurrentSeason) d.pass = new PassSave { season = CurrentSeason };
                return d.pass;
            }
        }

        public static int Level => Math.Min(Levels, S.xp / XpPerLevel);
        public static int XpInLevel => Level >= Levels ? XpPerLevel : S.xp % XpPerLevel;

        /// <summary>XP 추가 (저장은 호출 측 Save 에 맡김). 오른 레벨 수 반환</summary>
        public static int AddXp(int n)
        {
            if (n <= 0) return 0;
            int before = Level;
            S.xp = Math.Min(S.xp + n, Levels * XpPerLevel);
            return Level - before;
        }

        public static (PassReward kind, int amount) Free(int lv)
        {
            if (lv % 10 == 0) return (PassReward.CpRare, 10);
            if (lv % 5 == 0) return (PassReward.Pack, 1);
            if (lv % 3 == 0) return (PassReward.CpCommon, 60);
            return (PassReward.Opal, 30);
        }

        public static (PassReward kind, int amount) Premium(int lv)
        {
            if (lv == Levels) return (PassReward.UniqueCard, 1);
            if (lv == 15) return (PassReward.SevenCard, 1);
            if (lv % 5 == 0) return (PassReward.Pack, 2);
            if (lv % 2 == 0) return (PassReward.CpUncommon, 20);
            return (PassReward.Opal, 50);
        }

        public static string Label((PassReward kind, int amount) r)
        {
            switch (r.kind)
            {
                case PassReward.Opal: return $"오팔 {r.amount}";
                case PassReward.Pack: return $"부스터팩 {r.amount}";
                case PassReward.CpCommon: return $"Common CP {r.amount}";
                case PassReward.CpUncommon: return $"Uncommon CP {r.amount}";
                case PassReward.CpRare: return $"Rare CP {r.amount}";
                case PassReward.SevenCard: return "Seven 가공 카드";
                case PassReward.UniqueCard: return "유니크 카드";
                case PassReward.RareCard: return "Rare 카드 (랜덤)";
                default: return "";
            }
        }

        public static bool Claimed(int lv, bool premium) => (premium ? S.premClaimed : S.freeClaimed).Contains(lv);
        public static bool CanClaim(int lv, bool premium) => lv <= Level && !Claimed(lv, premium) && (!premium || S.premium);

        static readonly Random rng = new Random();

        /// <summary>
        /// 보상 지급. 팩 보상은 packs 에 장수를 돌려준다 (호출 측이 개봉 연출). 받은 내용 글 반환 (못 받으면 null)
        /// </summary>
        public static string Claim(int lv, bool premium, out int packs)
        {
            packs = 0;
            if (!CanClaim(lv, premium)) return null;
            (premium ? S.premClaimed : S.freeClaimed).Add(lv);
            var r = premium ? Premium(lv) : Free(lv);
            string got = Grant(r, out packs);
            Profile.Save();
            return got;
        }

        /// <summary>Grant 가 지급한 카드 (호출 측이 TakeGranted 로 가져가 공개 연출에 쓴다)</summary>
        static readonly List<(CardData card, bool isNew, CardFinish finish)> granted = new List<(CardData, bool, CardFinish)>();
        public static List<(CardData card, bool isNew, CardFinish finish)> TakeGranted()
        {
            var l = granted.ToList();
            granted.Clear();
            return l;
        }

        static string GrantCard(CardData c, CardFinish fin)
        {
            bool isNew = Profile.Owned(c.id) == 0;
            Profile.AddCard(c.id, 1, true, fin);
            granted.Add((c, isNew, fin));
            return c.name;
        }

        /// <summary>보상 하나 지급 (출석 보상과 공용). 카드 보상은 granted 에도 남긴다</summary>
        public static string Grant((PassReward kind, int amount) r, out int packs)
        {
            packs = 0;
            var d = Profile.Data;
            switch (r.kind)
            {
                case PassReward.Opal: d.opal += r.amount; return $"오팔 +{r.amount}";
                case PassReward.Pack: packs = r.amount; return $"부스터팩 {r.amount}팩";
                case PassReward.CpCommon: d.cp[0] += r.amount; return $"Common CP +{r.amount}";
                case PassReward.CpUncommon: d.cp[1] += r.amount; return $"Uncommon CP +{r.amount}";
                case PassReward.CpRare: d.cp[2] += r.amount; return $"Rare CP +{r.amount}";
                case PassReward.SevenCard:
                {
                    var pool = CardDB.All.Where(c => (c.rarity == Rarity.Uncommon || c.rarity == Rarity.Rare) && c.type != CardType.Hero).ToList();
                    return $"[{GrantCard(pool[rng.Next(pool.Count)], CardFinish.Seven)}] Seven 가공";
                }
                case PassReward.RareCard:
                {
                    var pool = CardDB.All.Where(c => c.rarity == Rarity.Rare && c.type != CardType.Hero).ToList();
                    return $"Rare [{GrantCard(pool[rng.Next(pool.Count)], CardFinish.Normal)}]";
                }
                case PassReward.UniqueCard:
                {
                    var pool = CardDB.All.Where(c => c.rarity == Rarity.Unique && c.type != CardType.Hero).ToList();
                    return $"유니크 [{GrantCard(pool[rng.Next(pool.Count)], CardFinish.Normal)}]";
                }
            }
            return "";
        }

        public static bool BuyPremium(out string msg)
        {
            if (S.premium) { msg = "이미 프리미엄입니다"; return false; }
            if (!Profile.Spend(PremiumPrice)) { msg = "오팔이 부족합니다"; return false; }
            S.premium = true;
            Profile.Save();
            msg = "세븐 패스 프리미엄을 열었습니다!";
            return true;
        }

        public static int ClaimableCount
            => Enumerable.Range(1, Levels).Count(lv => CanClaim(lv, false)) + Enumerable.Range(1, Levels).Count(lv => CanClaim(lv, true));
    }

    [Serializable]
    public class AttendSave
    {
        /// <summary>이번 주기에서 받은 날 수 (0~7)</summary>
        public int day;
        public string last = "";
    }

    /// <summary>
    /// 출석체크: 하루 한 번 · 1~6일째는 오팔과 CP 를 번갈아, 7일째는 랜덤 Rare 카드 → 처음부터. 빠진 날이 있어도 이어서 받는다.
    /// 가치 기준: 카드 제작 30 CP · 팩 100 오팔(7장) → Common CP 1 ≈ 오팔 0.5, Uncommon CP 1 ≈ 오팔 1.5
    /// </summary>
    public static class Attendance
    {
        public static readonly (PassReward kind, int amount)[] Rewards =
        {
            (PassReward.Opal, 50), (PassReward.CpCommon, 120), (PassReward.Opal, 80), (PassReward.CpUncommon, 60),
            (PassReward.Opal, 100), (PassReward.CpUncommon, 90), (PassReward.RareCard, 1),
        };

        static AttendSave A
        {
            get
            {
                var d = Profile.Data;
                if (d.attend == null) d.attend = new AttendSave();
                return d.attend;
            }
        }

        public static bool CanClaim => A.last != DailyClock.Today;
        /// <summary>다음에 받을 별 (1~7)</summary>
        public static int NextDay => A.day % 7 + 1;
        /// <summary>이번 주기에서 이미 받은 별 수 (오늘 받았으면 그 별까지)</summary>
        public static int ClaimedInCycle => CanClaim && A.day >= 7 ? 0 : A.day;

        public static string Claim(out int packs)
        {
            packs = 0;
            if (!CanClaim) return null;
            int day = NextDay;
            A.day = day;
            A.last = DailyClock.Today;
            string got = SevenPass.Grant(Rewards[day - 1], out packs);
            SevenPass.AddXp(SevenPass.XpAttend);
            Profile.Save();
            return $"출석체크 {day}일째 — {got}";
        }
    }
}
