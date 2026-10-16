using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>부스터팩 정의. 새 팩은 Packs 리스트에 추가.</summary>
    public class BoosterPack
    {
        public string id;
        public string code;          // 짧은 이름 (SSS)
        public string name;          // 정식 이름
        public string description;
        public string art;           // Resources/StrikerZ/Textures/ 아래 이미지 이름
        public string boxArt;        // 상점 진열장의 부스터 박스 이미지 (없으면 팩 이미지)
        public int price = 100;      // 1팩 오팔
        public int cardsPerPack = 7;
        public Func<CardData, bool> pool = c => true;

        /// <summary>슬롯별 등급 확률 (합 1). 슬롯 수 = cardsPerPack</summary>
        public List<(Rarity rarity, float chance)[]> slots;

        public static readonly List<BoosterPack> Packs = new List<BoosterPack>
        {
            new BoosterPack
            {
                id = "SSS", code = "SSS", name = "START STRIKER SEVEN",
                // 확률 숫자는 여기 쓰지 않는다 — 표시는 모두 slots·가공 상수에서 만든다 (RateText · FinishRateText · Breakdown)
                description = "StrikerZ Seven의 첫 부스터팩.\n7속성 몬스터·마법·필드와 유니크 카드가 수록되어 있습니다.",
                art = "pack_sss", boxArt = "pack_sss_box", price = 100, cardsPerPack = 7,
                slots = Slots(),
            },
        };

        static List<(Rarity, float)[]> Slots()
        {
            // 7장 모두 같은 확률: Common 55% · Uncommon 35% · Rare 9% · Unique 1%
            var any = new[] { (Rarity.Common, 0.55f), (Rarity.Uncommon, 0.35f), (Rarity.Rare, 0.09f), (Rarity.Unique, 0.01f) };
            return Enumerable.Repeat(any, 7).ToList();
        }

        /// <summary>최신 팩 (상점 마스코트가 들고 있는 팩)</summary>
        public static BoosterPack Latest => Packs[Packs.Count - 1];

        // ══════════════ 확률 표시 (확률형 아이템 정보 공개) ══════════════
        // 화면·문서에 나가는 확률 숫자는 전부 아래에서 만든다. 실제 뽑기(Open · RollFinish)와 같은 데이터만 쓴다.

        /// <summary>모든 장의 등급 확률이 같은지</summary>
        public bool UniformSlots => slots.All(s => s.SequenceEqual(slots[0]));

        /// <summary>확률 표기: 1% 이상은 소수 둘째 자리까지, 그보다 작으면 넷째 자리까지</summary>
        public static string Pct(double p) => p >= 0.01 ? $"{p * 100:0.##}%" : $"{p * 100:0.####}%";

        static string RarityRates((Rarity rarity, float chance)[] slot, string sep)
            => string.Join(sep, slot.Where(s => s.chance > 0).Select(s => $"{CardData.RarityLabel(s.rarity)} {Pct(s.chance)}"));

        /// <summary>등급 확률. 모든 장이 같으면 "카드마다 Common 55% · …", 다르면 장마다 한 줄</summary>
        public string RateText(string sep = " · ")
            => UniformSlots ? "카드마다 " + RarityRates(slots[0], sep)
                            : string.Join("\n", slots.Select((s, i) => $"{i + 1}번째 장 {RarityRates(s, sep)}"));

        /// <summary>가공 확률 "◆ Seven 10% · ★ StrikerZ 1%" (모든 카드에 따로 판정)</summary>
        public static string FinishRateText
            => $"{FinishStyle.Symbol(CardFinish.Seven)} Seven {Pct(SevenChance)} · {FinishStyle.Symbol(CardFinish.StrikerZ)} StrikerZ {Pct(StrikerZChance)}";

        /// <summary>
        /// 등급별 상세: 수록 종수, 카드 1장을 뽑을 때 특정 카드 1종이 나올 확률(같은 등급 안에서는 모두 같음),
        /// 1팩(cardsPerPack 장)에 그 카드가 1장 이상 들어 있을 확률.
        /// </summary>
        public List<(Rarity rarity, double chance, int kinds, double perCard, double perPack)> Breakdown()
        {
            var pool = CardDB.All.Where(this.pool).ToList();
            var rows = new List<(Rarity, double, int, double, double)>();
            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
            {
                int kinds = pool.Count(c => c.rarity == r);
                double chance = slots.Average(s => (double)s.Where(x => x.rarity == r).Sum(x => x.chance));
                if (chance <= 0) continue;
                double perCard = kinds > 0 ? chance / kinds : 0;
                double miss = 1;
                foreach (var s in slots) miss *= 1 - (kinds > 0 ? s.Where(x => x.rarity == r).Sum(x => x.chance) / kinds : 0);
                rows.Add((r, chance, kinds, perCard, 1 - miss));
            }
            return rows;
        }

        /// <summary>
        /// 확률표 검사 (비어 있으면 정상): 장마다 합이 100%, 확률이 있는 등급에 카드가 1종 이상.
        /// 카드가 없는 등급은 Open 에서 한 단계 아래로 내려가 공개 확률과 실제가 달라지므로 반드시 막는다.
        /// </summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (slots.Count != cardsPerPack) problems.Add($"{id}: 장 수 {slots.Count} ≠ cardsPerPack {cardsPerPack}");
            var pool = CardDB.All.Where(this.pool).ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                float sum = slots[i].Sum(s => s.chance);
                if (Math.Abs(sum - 1f) > 0.0001f) problems.Add($"{id}: {i + 1}번째 장 확률 합 {sum * 100:0.##}%");
                foreach (var (r, ch) in slots[i])
                    if (ch > 0 && !pool.Any(c => c.rarity == r)) problems.Add($"{id}: {CardData.RarityLabel(r)} 확률 {Pct(ch)} 인데 수록 카드가 없음");
            }
            if (SevenChance + StrikerZChance > 1f) problems.Add("가공 확률 합이 100% 초과");
            return problems;
        }

        public const float SevenChance = 0.10f, StrikerZChance = 0.01f;

        /// <summary>카드마다 가공 판정: 1% StrikerZ, 10% Seven</summary>
        public static CardFinish RollFinish(Random rng)
        {
            double r = rng.NextDouble();
            if (r < StrikerZChance) return CardFinish.StrikerZ;
            if (r < StrikerZChance + SevenChance) return CardFinish.Seven;
            return CardFinish.Normal;
        }

        public List<CardData> Open(Random rng)
        {
            var list = CardDB.All.Where(pool).ToList();
            var result = new List<CardData>();
            foreach (var slot in slots)
            {
                double roll = rng.NextDouble(), acc = 0;
                Rarity r = slot[0].rarity;
                foreach (var (rarity, chance) in slot)
                {
                    acc += chance;
                    if (roll <= acc) { r = rarity; break; }
                }
                // 해당 등급 카드가 없으면 한 단계씩 낮춘다 (공개 확률과 달라지는 상황 — Validate 가 미리 잡아야 한다)
                List<CardData> candidates;
                while ((candidates = list.Where(c => c.rarity == r).ToList()).Count == 0 && r > Rarity.Common)
                {
                    UnityEngine.Debug.LogError($"[StrikerZ] {id} 팩: {CardData.RarityLabel(r)} 카드가 없어 한 단계 낮춥니다 (공개 확률과 불일치)");
                    r--;
                }
                if (candidates.Count == 0) candidates = list;
                result.Add(candidates[rng.Next(candidates.Count)]);
            }
            return result;
        }
    }
}
