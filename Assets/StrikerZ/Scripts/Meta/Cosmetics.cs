using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    public enum CosmeticKind { Sleeve, Case, Coin, Dice }

    /// <summary>꾸미기 아이템 1개. tex = Resources/StrikerZ/Textures/ 기준 경로 (코인은 _h/_t 를 붙인다)</summary>
    public class CosmeticItem
    {
        public string id, name, desc, tex;
        public CosmeticKind kind;
        /// <summary>테마 속성 (None = 기본 아이템)</summary>
        public Element element;
        public int price;
        public bool IsDefault => price == 0;
        /// <summary>대표색: 주사위 궤적·코인 옆테·목록 표시에 쓴다 (기본 = 황금)</summary>
        public Color Tint => element == Element.None ? new Color(1f, 0.8f, 0.35f) : CardData.ElementColor(element);
    }

    /// <summary>
    /// 꾸미기 상점: 카드 슬리브 · 덱케이스 · 코인 · 주사위 (종류마다 기본 1 + 7속성 테마, 슬리브·코인은 무지개 1 추가).
    /// 오팔로 사서 종류별로 하나씩 장착한다. 장착한 꾸미기는 내 쪽에만 보인다 (슬리브 = 내 카드 뒷면, 주사위·코인 = 내가 던질 때).
    /// 그림은 게임 안 카드백·코인·주사위·레이드 보스를 바탕으로 VARCO 에서 만들었다 (Tools/cosmetics).
    /// </summary>
    public static class Cosmetics
    {
        public const int SleevePrice = 500, CasePrice = 500, CoinPrice = 300, DicePrice = 300;

        static CosmeticItem I(string id, CosmeticKind k, Element e, string name, string tex, int price, string desc)
            => new CosmeticItem { id = id, kind = k, element = e, name = name, tex = tex, price = price, desc = desc };

        const CosmeticKind S = CosmeticKind.Sleeve, C = CosmeticKind.Case, O = CosmeticKind.Coin, D = CosmeticKind.Dice;

        /// <summary>종류별로 기본 → 화염 · 물 · 바람 · 대지 · 번개 · 빛 · 어둠 · 무지개 순</summary>
        public static readonly List<CosmeticItem> All = new List<CosmeticItem>
        {
            // ── 카드 슬리브 (카드 뒷면) ──
            I("SLV_DEFAULT", S, Element.None, "스트라이커즈 기본", "card_back", 0, "흑금 룬 테두리와 일곱 보석 별. 모든 스트라이커가 처음 받는 카드 뒷면."),
            I("SLV_LAVA", S, Element.Fire, "작열의 화산", "Cosmetics/sleeve_lava", SleevePrice, "흑요석 틈으로 용암이 흐르는 뒷면. 손에 쥐면 불씨가 튈 것 같다."),
            I("SLV_SEA", S, Element.Water, "심해의 신전", "Cosmetics/sleeve_sea", SleevePrice, "산호와 진주로 장식한 심해 신전의 문양. 푸른 룬이 은은하게 빛난다."),
            I("SLV_SKY", S, Element.Wind, "천공의 정원", "Cosmetics/sleeve_sky", SleevePrice, "구름 위 대리석 정원과 비취 별. 바람 깃털이 테두리를 감싼다."),
            I("SLV_EARTH", S, Element.Earth, "대지의 성채", "Cosmetics/sleeve_earth", SleevePrice, "사암과 청동에 새긴 산맥과 뿌리. 호박 보석이 따뜻하게 빛난다."),
            I("SLV_THUNDER", S, Element.Thunder, "뇌명의 폭풍", "Cosmetics/sleeve_thunder", SleevePrice, "먹구름을 가르며 별에서 번개가 뻗어 나간다."),
            I("SLV_LIGHT", S, Element.Light, "성광의 성역", "Cosmetics/sleeve_light", SleevePrice, "상아빛 성역 위로 햇살이 퍼지고 황금 광륜이 별을 감싼다."),
            I("SLV_VOID", S, Element.Dark, "칠흑의 심연", "Cosmetics/sleeve_void", SleevePrice, "보랏빛 성운이 소용돌이치는 심연. 자수정 별이 어둠 속에서 빛난다."),
            I("SLV_RAINBOW", S, Element.Rainbow, "무지개 파라다이스", "Cosmetics/sleeve_rainbow", SleevePrice, "보는 각도마다 색이 바뀌는 홀로그램 진주 슬리브. 오팔 보석 별."),

            // ── 덱케이스 (레이드 보스 테마) ──
            I("CASE_DEFAULT", C, Element.None, "스트라이커즈 기본", "Cosmetics/case_default", 0, "검은 가죽에 금빛 룬. 카드 뒷면과 짝을 이루는 기본 덱케이스."),
            I("CASE_IGNIR", C, Element.Fire, "업화룡 이그니르", "Cosmetics/case_ignir", CasePrice, "붉은 용비늘 가죽과 타오르는 용의 눈. 화산을 삼킨 용의 덱케이스."),
            I("CASE_LEVIAS", C, Element.Water, "심해왕 레비아스", "Cosmetics/case_levias", CasePrice, "자개 물결 위로 은빛 해룡이 휘감긴 덱케이스."),
            I("CASE_SILVERN", C, Element.Wind, "폭풍군주 실베른", "Cosmetics/case_silvern", CasePrice, "은빛 폭풍 독수리의 날개가 감싼 청록 가죽 덱케이스."),
            I("CASE_GORDUN", C, Element.Earth, "산맥거신 고르둔", "Cosmetics/case_gordun", CasePrice, "청동 띠를 두른 화강암 상자. 갈라진 틈으로 호박빛이 새어 나온다."),
            I("CASE_VOLTEON", C, Element.Thunder, "뇌제 볼테온", "Cosmetics/case_volteon", CasePrice, "철갑에 번개가 새겨진 덱케이스. 열 때마다 찌릿하다."),
            I("CASE_SERAPHIEL", C, Element.Light, "광휘천사 세라피엘", "Cosmetics/case_seraphiel", CasePrice, "백색 법랑과 황금 날개, 머리 위의 광륜. 성스러운 덱케이스."),
            I("CASE_NOCTERA", C, Element.Dark, "심연군주 녹테라", "Cosmetics/case_noctera", CasePrice, "검은 벨벳에 초승달과 별, 은빛 가시 장식."),

            // ── 코인 (빛/어둠 전투 코인) ──
            I("COIN_DEFAULT", O, Element.None, "황금 별 코인", "", 0, "빛과 어둠의 운명을 가르는 기본 황금 코인."),
            I("COIN_PHOENIX", O, Element.Fire, "불사조 로즈골드 코인", "Cosmetics/coin_phoenix", CoinPrice, "불사조 깃털 문양과 루비가 박힌 로즈골드."),
            I("COIN_SAPPHIRE", O, Element.Water, "사파이어 결정 코인", "Cosmetics/coin_sapphire", CoinPrice, "빛이 안에서 굴절하는 푸른 결정 코인."),
            I("COIN_GALE", O, Element.Wind, "질풍 비취 코인", "Cosmetics/coin_gale", CoinPrice, "옅은 비취에 은빛 바람 소용돌이와 깃털을 새겼다."),
            I("COIN_BRONZE", O, Element.Earth, "고대 청동 코인", "Cosmetics/coin_bronze", CoinPrice, "녹청이 앉은 청동 테두리에 산맥 부조, 가운데 호박 보석."),
            I("COIN_TOPAZ", O, Element.Thunder, "뇌광 토파즈 코인", "Cosmetics/coin_topaz", CoinPrice, "강철 위로 번개 문양이 번쩍이는 토파즈 코인."),
            I("COIN_MOON", O, Element.Light, "은월 코인", "Cosmetics/coin_moon", CoinPrice, "초승달이 새겨진 순은 테두리와 월장석."),
            I("COIN_OBSIDIAN", O, Element.Dark, "흑요 자수정 코인", "Cosmetics/coin_obsidian", CoinPrice, "흑요석 몸체에 박힌 보랏빛 자수정 별."),
            I("COIN_OPAL", O, Element.Rainbow, "무지개 오팔 코인", "Cosmetics/coin_opal", CoinPrice, "오팔 특유의 무지갯빛이 일렁이는 코인."),

            // ── 주사위 ──
            I("DICE_DEFAULT", D, Element.None, "흑요 황금 주사위", "", 0, "흑요석에 황금 눈. 기본 전투 주사위."),
            I("DICE_LAVA", D, Element.Fire, "용암 유리 주사위", "Cosmetics/dice_lava", DicePrice, "속에서 용암이 끓는 화산 유리 주사위."),
            I("DICE_ICE", D, Element.Water, "빙결 수정 주사위", "Cosmetics/dice_ice", DicePrice, "서리가 맺힌 얼음 수정에 은빛 눈."),
            I("DICE_JADE", D, Element.Wind, "비취 주사위", "Cosmetics/dice_jade", DicePrice, "구름무늬 비취에 황금 눈. 바람을 부른다고 한다."),
            I("DICE_AMBER", D, Element.Earth, "호박 마노 주사위", "Cosmetics/dice_amber", DicePrice, "켜켜이 쌓인 호박빛 마노 무늬에 황금 눈."),
            I("DICE_STORM", D, Element.Thunder, "뇌운 주사위", "Cosmetics/dice_storm", DicePrice, "먹구름 돌 속으로 번개가 갈라지는 주사위."),
            I("DICE_MARBLE", D, Element.Light, "백대리석 주사위", "Cosmetics/dice_marble", DicePrice, "금맥이 흐르는 하얀 대리석에 황금 눈."),
            I("DICE_NEBULA", D, Element.Dark, "성운 주사위", "Cosmetics/dice_nebula", DicePrice, "자수정 수지 속에 은하가 떠 있는 주사위."),
        };

        /// <summary>3D 코인 옆테 색: 코인 그림 바깥 테두리의 평균색 (Tools/coins/process.py 가 출력)</summary>
        static readonly Dictionary<string, Color> CoinRims = new Dictionary<string, Color>
        {
            // 2026-10-14 새 정면·양각 코인 그림 (Tools/coins) 의 바깥 테 평균색
            { "COIN_PHOENIX", new Color(0.75f, 0.53f, 0.46f) }, { "COIN_SAPPHIRE", new Color(0.57f, 0.56f, 0.57f) },
            { "COIN_GALE", new Color(0.53f, 0.54f, 0.49f) }, { "COIN_BRONZE", new Color(0.31f, 0.34f, 0.25f) },
            { "COIN_TOPAZ", new Color(0.25f, 0.24f, 0.22f) }, { "COIN_MOON", new Color(0.57f, 0.56f, 0.54f) },
            { "COIN_OBSIDIAN", new Color(0.18f, 0.13f, 0.19f) }, { "COIN_OPAL", new Color(0.59f, 0.57f, 0.55f) },
        };

        public static Color CoinRim(CosmeticItem c) => c != null && CoinRims.TryGetValue(c.id, out var col) ? col : new Color(1f, 0.78f, 0.3f);

        public static readonly CosmeticKind[] Kinds ={ CosmeticKind.Sleeve, CosmeticKind.Case, CosmeticKind.Coin, CosmeticKind.Dice };

        public static string KindLabel(CosmeticKind k) => k switch
        {
            CosmeticKind.Sleeve => "카드 슬리브",
            CosmeticKind.Case => "덱케이스",
            CosmeticKind.Coin => "코인",
            _ => "주사위",
        };

        public static CosmeticItem Get(string id) => All.FirstOrDefault(c => c.id == id);
        public static IEnumerable<CosmeticItem> OfKind(CosmeticKind k) => All.Where(c => c.kind == k);
        public static CosmeticItem Default(CosmeticKind k) => All.First(c => c.kind == k && c.IsDefault);

        static List<string> OwnedList => Profile.Data.cosmetics ??= new List<string>();

        public static bool Owned(CosmeticItem c) => c.IsDefault || OwnedList.Contains(c.id);

        static string Slot(CosmeticKind k)
        {
            var d = Profile.Data;
            return k switch { CosmeticKind.Sleeve => d.sleeve, CosmeticKind.Case => d.deckCase, CosmeticKind.Coin => d.coin, _ => d.dice };
        }

        /// <summary>장착 중인 아이템 (없거나 미보유면 기본)</summary>
        public static CosmeticItem Equipped(CosmeticKind k)
        {
            var c = Get(Slot(k));
            return c != null && c.kind == k && Owned(c) ? c : Default(k);
        }

        public static bool IsEquipped(CosmeticItem c) => Equipped(c.kind) == c;

        public static bool Buy(CosmeticItem c, out string why)
        {
            why = null;
            if (Owned(c)) { why = "이미 가지고 있습니다"; return false; }
            if (!Profile.Spend(c.price)) { why = $"오팔이 부족합니다 (필요 {c.price:N0})"; return false; }
            OwnedList.Add(c.id);
            Profile.Save();
            return true;
        }

        public static bool Equip(CosmeticItem c)
        {
            if (!Owned(c)) return false;
            string v = c.IsDefault ? "" : c.id;
            var d = Profile.Data;
            switch (c.kind)
            {
                case CosmeticKind.Sleeve: d.sleeve = v; break;
                case CosmeticKind.Case: d.deckCase = v; break;
                case CosmeticKind.Coin: d.coin = v; break;
                default: d.dice = v; break;
            }
            Profile.Save();
            return true;
        }

        // ── 그림 ──

        /// <summary>슬리브 = 카드 뒷면 텍스처</summary>
        public static Texture2D SleeveTex(CosmeticItem c) => GameAssets.Tex(c?.tex ?? "card_back") ?? GameAssets.Tex("card_back");

        /// <summary>코인 앞/뒷면 텍스처 (기본 = Models/coin_heads·tails)</summary>
        public static Texture2D CoinTex(CosmeticItem c, bool heads)
        {
            if (c == null || string.IsNullOrEmpty(c.tex)) return Resources.Load<Texture2D>(GameAssets.Root + (heads ? "Models/coin_heads" : "Models/coin_tails"));
            return GameAssets.Tex(c.tex + (heads ? "_h" : "_t"));
        }

        /// <summary>주사위 3×2 아틀라스 (기본 = Models/dice_faces)</summary>
        public static Texture2D DiceTex(CosmeticItem c)
        {
            if (c == null || string.IsNullOrEmpty(c.tex)) return Resources.Load<Texture2D>(GameAssets.Root + "Models/dice_faces");
            return GameAssets.Tex(c.tex);
        }

        /// <summary>상점·덱 화면에 보여 줄 대표 그림</summary>
        public static Sprite Icon(CosmeticItem c)
        {
            switch (c.kind)
            {
                case CosmeticKind.Coin:
                    return CoinSprite(c, true);
                case CosmeticKind.Dice:
                    return DiceFace(c, 6);
                default:
                    return GameAssets.TexSprite(c.tex);
            }
        }

        public static Sprite CoinSprite(CosmeticItem c, bool heads)
            => string.IsNullOrEmpty(c.tex) ? GameAssets.Sprite(GameAssets.Root + (heads ? "Models/coin_heads" : "Models/coin_tails")) : GameAssets.TexSprite(c.tex + (heads ? "_h" : "_t"));

        static readonly Dictionary<string, Sprite> faceSprites = new Dictionary<string, Sprite>();

        /// <summary>주사위 아틀라스에서 한 면 (윗줄 4·5·6, 아랫줄 1·2·3)</summary>
        public static Sprite DiceFace(CosmeticItem c, int pip)
        {
            string key = c.id + pip;
            if (faceSprites.TryGetValue(key, out var s)) return s;
            var tex = DiceTex(c);
            if (tex == null) return null;
            int col = (pip - 1) % 3, row = pip <= 3 ? 0 : 1;      // 텍스처 좌표는 아래가 0 → 아랫줄 = 1·2·3
            float w = tex.width / 3f, h = tex.height / 2f;
            s = Sprite.Create(tex, new Rect(col * w, row * h, w, h), new Vector2(0.5f, 0.5f), 100f);
            faceSprites[key] = s;
            return s;
        }
    }
}
