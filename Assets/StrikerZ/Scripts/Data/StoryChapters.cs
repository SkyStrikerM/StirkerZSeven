using System;
using System.Collections.Generic;
using System.Linq;

namespace StrikerZ
{
    /// <summary>메인 시나리오 제2장 ~ 제5장. 장면 CG: Art/Story/v{장}_{스테이지}.png (스테이지 썸네일 겸용)</summary>
    public static partial class StoryData
    {
        static StoryVolume Vol(int n, string title, string tagline)
        {
            BuildStoryOpponents();
            return new StoryVolume { number = n, id = $"V{n}", title = title, tagline = tagline, cover = $"v{n}_7", ready = true };
        }

        static StoryStage Stage(StoryVolume v, string title, string subtitle, string synopsis, StoryBattle b, int opal = 50, string hero = null)
        {
            int n = v.stages.Count + 1;
            var s = new StoryStage
            {
                title = title, subtitle = subtitle, synopsis = synopsis, battle = b, rewardOpal = opal, rewardHero = hero,
                volume = v, number = n, id = $"V{v.number}S{n}", thumb = $"v{v.number}_{n}",
            };
            v.stages.Add(s);
            return s;
        }

        static CardData HeroById(string id) => CardList.Heroes.First(h => h.id == id);

        // 지원 스킬 (내 타임 시작 시)
        static StorySupport AriaS(int n) => Sup("아리아", $"치유의 물방울 — HP {n} 회복", StorySupport.Kind.Heal, n);
        static StorySupport LeonS(int n) => Sup("레온", $"홍염 연격 — 상대에게 {n} 데미지", StorySupport.Kind.Burn, n);
        static StorySupport BaneS(int n) => Sup("베인", $"폭풍의 눈 — 상대 몬스터에게 {n} 데미지", StorySupport.Kind.Strike, n);
        static StorySupport IgnisS(int n) => Sup("이그니스", $"업화 — 상대에게 {n} 데미지", StorySupport.Kind.Burn, n);
        static StorySupport MerilS(int n) => Sup("메릴", $"호수의 축복 — HP {n} 회복", StorySupport.Kind.Heal, n);
        static StorySupport ZephyrS(int n) => Sup("제퍼", $"순풍의 노래 — 이번 타임 내 몬스터 ATK +{n}", StorySupport.Kind.Atk, n);
        static StorySupport VoltS(int n) => Sup("볼트", $"뇌신의 주먹 — 상대 몬스터에게 {n} 데미지", StorySupport.Kind.Strike, n);
        static StorySupport TerraS(int n) => Sup("테라", $"대지의 맥박 — HP {n} 회복", StorySupport.Kind.Heal, n);
        static StorySupport LumienS(int n) => Sup("루미엔", $"성광 — HP {n} 회복", StorySupport.Kind.Heal, n);
        static StorySupport ErebosS(int n) => Sup("에레보스", $"심연의 창 — 상대 몬스터에게 {n} 데미지", StorySupport.Kind.Strike, n);
        static StorySupport VarusS(int n) => Sup("바루스", $"회색의 속죄 — 상대에게 {n} 데미지", StorySupport.Kind.Burn, n);
        static StorySupport PrismaS(int n) => Sup("프리스마", $"창세의 숨결 — 이번 타임 내 몬스터 ATK +{n}", StorySupport.Kind.Atk, n);

        // ═══════════════════════════════ 제2장 ═══════════════════════════════
        static StoryVolume Volume2()
        {
            var v = Vol(2, "회색의 전조", "안개의 발원지를 찾아, 세 사람은 학교를 떠난다");

            var s1 = Stage(v, "마지막 수업", "베인 교수 · 동료 지원", "회색 안개는 숲만의 일이 아니었다. 대륙 곳곳에서 색이 사라지고 있다. 떠나기 전, 베인 교수의 마지막 수업이 시작된다.",
                new StoryBattle { oppName = "베인 교수", oppHero = Bane, oppHp = 34, oppDeck = ElementDeck(Element.Wind, 10, 8, 3, 9), field = FieldNamed("천공의 정원"), ariaHeal = 2, goal = "베인 교수의 마지막 수업을 통과하라" });
            s1.battle.tips.Add(Tip("start", "새로운 여정", "제2장부터는 대마법사들이 동료 지원으로 함께합니다. 지원은 내 타임이 시작될 때 자동으로 발동돼요."));
            s1.pre.Add(S("v2_1",
                T("제2장", "회색의 전조"),
                N("속삭임의 숲 사건으로부터 열흘. 학교 지도실의 대륙 지도 위에, 잿빛 얼룩이 하루가 다르게 번져 가고 있었다."),
                L("bane", "북쪽 농촌 셋, 동쪽 항구 하나. 전부 색을 잃었다. 사람도, 땅도, 하늘도."),
                L("leon", "숲에서 본 그 안개가…… 대륙 전체에?"),
                L("bane", "발원지가 있다. 그리고 그걸 밀어낼 수 있는 건, 지금으로선 단 하나뿐이지."),
                N("세 사람의 시선이 카일에게 모인다."),
                L("kai", "……제가 가겠습니다. 아니, 저희가요."),
                L("bane", "좋다. 그럼 떠나기 전에 마지막 수업이다. 셋이 함께, 나를 쓰러뜨려 봐라.", "shake")));
            s1.post.Add(S("v2_1",
                L("bane", "흠. 이젠 내 바람을 읽는 정도가 아니라, 바람이 불기 전에 움직이는군."),
                L("bane", "잘 들어라. 대륙에는 일곱 원소를 다스리는 일곱 대마법사가 있다. 회색을 막으려면 그들의 힘이 필요해."),
                L("aria", "하지만 대마법사들은 서로 사이가 나쁘다고……"),
                L("bane", "그러니까 너희가 가는 거다. 색이 없는 녀석이야말로, 어느 색의 편도 아니니까."),
                L("kai", "……네. 다녀오겠습니다.")));

            var s2 = Stage(v, "숲길의 습격", "회색 안개 · 아리아 & 레온 지원", "학교를 떠난 첫날 밤. 숲길을 따라 잿빛 짐승들이 세 사람을 쫓기 시작한다.",
                new StoryBattle { oppName = "잿빛 늑대 무리", oppHero = WolfPack, oppHp = 34, oppDeck = BeastDeck(4), field = FieldNamed("고요한 평원"), mist = 1, ariaHeal = 2, leonFire = 2, goal = "잿빛 짐승들을 뿌리쳐라" });
            s2.pre.Add(S("v2_2",
                N("모닥불이 타닥거리는 숲길의 밤. 레온은 말없이 칼날 같은 불꽃을 손끝에 피웠다 지웠다를 반복하고 있었다."),
                L("aria", "레온, 아까부터 한숨도 안 잤지?"),
                L("leon", "……숲에서 내 불이 먹혔던 걸 생각하면 잠이 오겠냐."),
                L("kai", "그래도 마지막엔 네 불꽃이 사슴왕을──"),
                L("leon", "네가 길을 열어 줬으니까지. 그게 더 화가 난다고."),
                N("그때, 모닥불의 불빛이 회색으로 일렁였다.", "shake"),
                L("aria", "안개야……! 짐승들이 와!")));
            s2.post.Add(S("v2_2",
                L("leon", "……이번엔 먹히지 않았어. 카일 네가 앞에서 안개를 밀어내니까, 내 불이 닿는다."),
                L("kai", "그럼 앞으로도 그렇게 하자. 내가 밀고, 네가 태우고, 아리아가 지키고."),
                L("leon", "흥. 그 순서, 내가 정한 걸로 해 두지."),
                L("aria", "(후훗, 조금은 사이가 좋아진 걸까.)")));

            var s3 = Stage(v, "잿빛 마을", "회색 병사 등장", "길 끝의 마을 「로렌」. 사람들의 얼굴에서 색이 빠져 있고, 마을 자경단이 낯선 세 사람에게 창을 겨눈다.",
                new StoryBattle { oppName = "자경단장", oppHero = oppSoldierCaptain, oppHp = 36, oppDeck = GreyDeck(8, 0, 2, 2), field = FieldNamed("황혼의 전장"), mist = 1, ariaHeal = 2, goal = "안개에 홀린 자경단을 진정시켜라" });
            s3.battle.tips.Add(Tip("start", "회색 병사", "색을 빼앗긴 병사는 [도발]을 가진 단단한 방패입니다. 영웅 능력 [척력 반사]로 패로 튕겨내는 것도 방법!"));
            s3.pre.Add(S("v2_3",
                N("로렌 마을. 지붕도, 빨래도, 꽃밭도 모두 잿빛이었다. 그리고 사람들의 눈동자마저."),
                L("child", "……엄마가, 아무 말도 안 해. 웃지도 않고, 울지도 않아."),
                L("aria", "색을 빼앗기면 마음까지…… 비어 버리는 거야?"),
                L("elder", "외지인이다! 또 안개를 데려온 놈들이다!"),
                N("텅 빈 눈의 자경단이 창을 들고 둘러싼다. 그들의 발밑에서 회색 안개가 꿈틀거렸다.", "shake"),
                L("kai", "저희는 적이 아니에요! ……안 들리는 건가."),
                L("leon", "안개가 이 사람들을 조종하고 있어. 다치지 않게 제압한다!")));
            s3.post.Add(S("v2_3",
                N("척력의 파동이 마을 광장을 쓸고 지나가자, 자경단원들이 하나둘 무릎을 꿇었다. 눈동자에 아주 희미하게 색이 돌아온다."),
                L("elder", "……내가, 무슨 짓을……?"),
                L("kai", "괜찮아요. 안개 때문이었어요."),
                L("elder", "우물이…… 우물 바닥에서 안개가 솟아오르기 시작했소. 그날부터 마을이……"),
                L("aria", "발원지는 우물이야. 가 보자.")));

            var s4 = Stage(v, "우물 밑의 골렘", "보스 · 안개 골렘", "마을 우물 밑 동굴. 안개가 뭉쳐 만들어진 거대한 골렘이 마을의 색을 삼키고 있었다.",
                new StoryBattle { oppName = "안개 골렘", oppHero = oppGolem, oppHp = 40, oppDeck = GreyDeck(6, 0, 4, 4), field = FieldNamed("태고의 협곡"), mist = 1, ariaHeal = 3, leonFire = 2, goal = "안개 골렘을 부수고 마을의 색을 되찾아라" },
                hero: "H018");
            s4.pre.Add(S("v2_4",
                N("우물 밑 동굴은 생각보다 깊었다. 그리고 그 끝에, 안개로 빚어진 거인이 웅크리고 있었다."),
                L("leon", "저게 마을의 색을 빨아들이고 있는 건가."),
                L("aria", "골렘 안에서…… 사람들의 색이 보여. 빨강, 노랑, 초록…… 갇혀 있어."),
                L("kai", "부수면 돌아가는 거지? 그럼 망설일 이유 없어."),
                N("골렘이 몸을 일으키자, 동굴 천장에서 돌가루가 비처럼 쏟아졌다.", "shake")));
            s4.post.Add(S("v2_4",
                N("골렘이 무너지는 순간, 갇혀 있던 색들이 빛의 강물처럼 우물을 거슬러 올라갔다.", "flash"),
                N("마을 위로 쏟아지는 색의 비. 아리아가 두 손을 모으자 물빛 장막이 그 비를 사람들에게 고르게 나누어 주었다."),
                L("child", "엄마……! 엄마가 웃었어!"),
                L("aria", "다행이다…… 정말로."),
                L("leon", "아리아의 물이 색을 '나눠' 줬군. 카일이 풀어 주고, 아리아가 돌려준다라."),
                N("── 아리아가 정식으로 동료가 되었다. [영웅 카드 「아리아」 해금]")));

            var s5 = Stage(v, "회색의 사도", "이벤트 전투 · 져도 이야기가 이어집니다", "축제 같던 마을의 밤, 하늘이 회색으로 물든다. 그리고 그가 나타났다. 숲에서 카일을 지켜보던 회색의 사도.",
                new StoryBattle { oppName = "바루스", oppHero = Varus, oppHp = 60, oppDeck = GreyDeck(6, 2, 4, 6), field = FieldNamed("황혼의 전장"), mist = 2, allowLose = true, goal = "회색의 사도를 상대로 버텨라 (패배해도 진행)" });
            s5.pre.Add(S("v2_5",
                N("색을 되찾은 로렌 마을의 밤. 감사의 모닥불이 마을 광장을 붉게 밝히고 있었다."),
                N("──그 불꽃이, 한순간에 잿빛으로 굳었다.", "flash"),
                L("varus", "아름다운 밤이군. 그래서 더 안타깝다."),
                L("leon", "누구냐!"),
                L("varus", "나는 바루스. 회색의 사도라 불리지. ……그리고 너, 색이 없는 아이."),
                L("varus", "색은 고통이다. 불의 가문과 물의 가문이 싸우는 것도, 네가 '버려진 돌'이라 불린 것도. 모두 색이 있기 때문이지."),
                L("varus", "회색 아래에서는 모두가 평등하다. 차별도, 전쟁도 없다. ……너라면 이해할 줄 알았는데."),
                L("kai", "웃지도 울지도 못하는 게 평등이라고? 그런 건…… 인정 못 해!")));
            s5.post.Add(S("v2_5",
                L("varus", "……놀랍군. 나의 회색이 너에게만은 스며들지 않는다.", null, 1),
                L("varus", "역시 아직이군. 너는 아직, 아무것도 모른다.", null, 2),
                L("varus", "언젠가 너도 알게 될 것이다. 색이 사라진 세상이 얼마나 고요하고 평화로운지."),
                L("varus", "그때는 내 곁에 서게 될 테지. 색 없는 자여."),
                N("바루스가 안개 속으로 사라지자, 하늘 위에서 수백 개의 잿빛 날개가 퍼덕였다.", "shake"),
                L("leon", "까마귀 떼다! 마을 사람들을 데리고 물러나!")));

            var s6 = Stage(v, "후퇴", "회색 안개 · 베인 교수 지원", "바루스가 남긴 잿빛 까마귀 떼. 마을 사람들을 지키며 산길로 후퇴하는 세 사람 앞에, 낯익은 바람이 분다.",
                new StoryBattle
                {
                    oppName = "안개 속 그림자", oppHero = MistShade, oppHp = 40, oppDeck = BeastDeck(6), field = FieldNamed("고요한 평원"), mist = 2, ariaHeal = 2,
                    supports = { BaneS(3) }, goal = "마을 사람들이 피할 때까지 버텨라",
                });
            s6.pre.Add(S("v2_6",
                N("산길을 뒤덮는 까마귀 떼. 날갯짓 하나하나가 안개를 뿌리며 사람들의 색을 노린다."),
                L("aria", "아이들이 뒤처지고 있어!"),
                L("kai", "레온, 사람들을 앞으로! 여긴 내가──"),
                N("그 순간, 거센 돌풍이 까마귀 떼의 한가운데를 갈랐다.", "shake"),
                L("bane", "늦지는 않았나 보군. 제자 놈들이 사고를 크게 칠 것 같아서 말이다."),
                L("kai", "베인 교수님!"),
                L("bane", "잡담은 나중에. 바람은 내가 맡는다. 넌 땅에서 밀어내라!")));
            s6.post.Add(S("v2_6",
                L("bane", "바루스…… 정말로 그가 움직였군."),
                L("aria", "교수님, 그 사람을 아세요?"),
                L("bane", "옛날 이야기다. ……한때 일곱 대마법사와 나란히 섰던, 여덟 번째 색의 마법사였지."),
                L("leon", "여덟 번째?"),
                L("bane", "그 이야기는 안개의 문을 닫고 나서 하자. 이 산 너머에, 이 일대 안개의 근원이 있다.")));

            var s7 = Stage(v, "안개의 수문장", "보스 · 동료 총출동", "산 정상의 잿빛 문. 대륙의 안개가 흘러나오는 첫 번째 문을 지키는 수문장이 깨어난다.",
                new StoryBattle
                {
                    oppName = "안개의 수문장", oppHero = oppGatekeeper, oppHp = 48, oppDeck = GreyDeck(6, 2, 4, 6), field = FieldNamed("황혼의 전장"), mist = 2, ariaHeal = 2, leonFire = 2,
                    supports = { BaneS(3) }, goal = "보스: 안개의 문을 닫아라",
                },
                opal: 200, hero: "H019");
            s7.pre.Add(S("v2_7",
                N("산 정상. 하늘을 향해 열린 거대한 잿빛 문에서 안개가 폭포처럼 쏟아져 내리고 있었다."),
                L("bane", "저것이 문이다. 바루스는 대륙 곳곳에 이런 문을 열고 있는 거지."),
                N("문 앞의 돌무더기가 일어선다. 안개를 갑옷처럼 두른 수문장.", "shake"),
                L("kai", "여기서 막지 못하면 로렌 같은 마을이 또 생겨."),
                L("leon", "말 안 해도 알아. 가자, 카일!"),
                L("aria", "모두의 뒤는 내가 지킬게!")));
            s7.post.Add(S("v2_7",
                L("kai", "닫혀라아아──!!", "flash"),
                N("척력이 문을 밀어 닫는다. 쿠웅, 하는 굉음과 함께 안개의 폭포가 끊어졌다.", "shake"),
                L("bane", "……해냈군. 이 일대의 안개는 이제 걷힐 거다."),
                L("bane", "하지만 문은 이것 하나가 아니다. 대마법사들의 힘을 모아야 해. 먼저 남쪽, 불의 왕국과 물의 호국이다."),
                L("leon", "불의 왕국…… 내 고향이군."),
                L("bane", "문제는 그 두 나라가 지금 전쟁 직전이라는 거다. 이그니스와 메릴, 두 대마법사의 반목이 수백 년째거든."),
                L("bane", "나도 함께 가지. 은퇴한 늙은이지만, 길잡이 정도는 해 줄 수 있다. [영웅 카드 「베인」 해금]"),
                T("제2장 · 회색의 전조", "完 ── 다음 장 「엇갈리는 원소들」")));
            return v;
        }

        // ═══════════════════════════════ 제3장 ═══════════════════════════════
        static StoryVolume Volume3()
        {
            var v = Vol(3, "엇갈리는 원소들", "불과 물, 수백 년의 반목 한가운데에 서다");
            var ignis = HeroById("H007");

            var s1 = Stage(v, "화염의 국경", "불의 왕국 · 필드 효과", "불의 왕국 「솔라리스」의 국경. 경비대는 물의 나라에서 온 아리아를 보자마자 창끝을 겨눈다.",
                new StoryBattle { oppName = "화염 경비대장", oppHero = oppFireCaptain, oppHp = 38, oppDeck = ElementDeck(Element.Fire, 10, 8, 3, 9), field = FieldNamed("작열의 화산"), ariaHeal = 2, supports = { BaneS(2) }, goal = "국경을 통과하라" });
            s1.battle.tips.Add(Tip("start", "상대의 필드", "작열의 화산은 불 속성 몬스터를 강하게 만듭니다. 카일의 무속성 몬스터는 필드의 영향을 받지 않아요."));
            s1.pre.Add(S("v3_1",
                T("제3장", "엇갈리는 원소들"),
                N("붉은 바위와 용암의 강. 불의 왕국 솔라리스의 국경 요새는 하늘까지 열기로 일렁였다."),
                L("guard", "멈춰라! ……푸른 머리. 물의 호국 놈이군!"),
                L("aria", "저는 그냥 여행자예요. 학교에서──"),
                L("guard", "닥쳐라! 지난달에도 물의 첩자가 우리 성채의 불씨를 꺼뜨렸다!"),
                L("leon", "난 레온 이그니타. 이그니타 가문의 장남이다. 길을 열어."),
                L("guard", "이그니타 가문의 도련님이 물의 계집과 함께라니…… 더더욱 수상하군! 붙잡아라!")));
            s1.post.Add(S("v3_1",
                L("guard", "크윽…… 불꽃이 전부 튕겨 나가다니……"),
                L("leon", "……미안하다, 아리아. 우리 나라 사람들이 원래 이렇진 않았는데."),
                L("aria", "괜찮아. 그런데 다들 너무 날이 서 있어. 마치…… 누가 일부러 불을 지피고 있는 것처럼."),
                L("bane", "안개는 색을 먹을 뿐 아니라, 색과 색 사이의 '틈'도 벌린다. 이 전쟁, 냄새가 나는군.")));

            var s2 = Stage(v, "업화의 마녀", "대마법사 · 이그니스", "솔라리스의 왕궁, 불타는 옥좌. 불의 대마법사 이그니스는 회색과 싸우자는 말에 코웃음을 친다.",
                new StoryBattle { oppName = "이그니스", oppHero = ignis, oppHp = 46, oppDeck = ElementDeck(Element.Fire, 9, 9, 4, 8), field = FieldNamed("작열의 화산"), ariaHeal = 2, leonFire = 2, goal = "대마법사 이그니스에게 힘을 증명하라" });
            s2.pre.Add(S("v3_2",
                L("ignis", "회색 안개? 그딴 건 내 업화로 태워 버리면 그만이야."),
                L("kai", "태울 수 없었어요. 숲에서도, 마을에서도. 불도 물도 전부 안개에 먹혔어요."),
                L("ignis", "그건 네 놈들의 불이 약해서지. ……그리고 그 옆의 물 계집. 메릴의 나라에서 왔다며?"),
                L("aria", "메릴 님과는 상관없어요. 저는──"),
                L("ignis", "물의 것이라면 전부 상관있어! 메릴 그 여자는 300년 전 내 화산의 심장을 얼려 버렸단 말이다!"),
                L("leon", "스승님! 지금은 그런 얘기를 할 때가──"),
                L("ignis", "레온. 네가 데려온 이 '색 없는 꼬마'가 그렇게 대단하다면, 내 불꽃 앞에서 증명해 봐!", "flash")));
            s2.post.Add(S("v3_2",
                L("ignis", "……하! 내 업화를 정면으로 밀어내다니. 몇 백 년 만이지, 이런 기분."),
                L("ignis", "좋아, 인정하지. 회색이 위험하다는 것도. 하지만 메릴과 손잡는 건 사양이야."),
                L("kai", "그럼 메릴 님을 직접 만나 보고 올게요. 두 분이 왜 싸우는지, 제 눈으로 확인하고 싶어요."),
                L("ignis", "……맘대로 해. 얼어 죽지나 마라, 꼬마.")));

            var s3 = Stage(v, "호수 근위대", "물의 호국", "물의 호국 「아쿠아리스」. 거울 같은 호수 위의 성. 이번엔 불의 가문 레온이 표적이 된다.",
                new StoryBattle { oppName = "호수 근위대장", oppHero = oppLakeCaptain, oppHp = 40, oppDeck = ElementDeck(Element.Water, 10, 8, 3, 9), field = FieldNamed("심해의 신전"), leonFire = 2, supports = { BaneS(2) }, goal = "근위대를 뚫고 여왕을 만나라" });
            s3.pre.Add(S("v3_3",
                N("아쿠아리스의 호수는 하늘을 그대로 비추고 있었다. 그 고요함 속에 날카로운 긴장이 서려 있었다."),
                L("guard", "불의 냄새가 난다. ……이그니타 가문의 문장! 불의 첩자다!"),
                L("leon", "……이번엔 내 차례냐."),
                L("aria", "기다려 주세요! 저는 아쿠아리스 출신 아리아예요. 이 사람들은──"),
                L("guard", "불과 어울리는 물은 물이 아니다. 여왕 폐하의 명이다, 모두 호수 감옥으로!")));
            s3.post.Add(S("v3_3",
                N("호수의 물결이 잦아든다. 근위대장이 창을 거두고, 그 뒤에서 은빛 물결 같은 목소리가 들려왔다."),
                L("meril", "그만. ……재미있는 아이들이네. 불과 물과, 그리고 아무 색도 없는 아이."),
                L("aria", "메릴 님……!"),
                L("meril", "이그니스에게서 왔다고? 그 불덩이 여자가 뭐라고 하던가요. 내가 화산을 얼렸다고?"),
                L("meril", "웃기는군요. 300년 전 내 호수를 증발시킨 건 그쪽인데.")));

            var s4 = Stage(v, "호수의 여왕", "대마법사 · 메릴", "물의 대마법사 메릴. 차갑고 우아한 여왕은 카일의 '색 없음'이 얼마나 단단한지 시험하려 한다.",
                new StoryBattle { oppName = "메릴", oppHero = Meril, oppHp = 50, oppDeck = ElementDeck(Element.Water, 9, 9, 4, 8), field = FieldNamed("심해의 신전"), ariaHeal = 2, leonFire = 2, goal = "대마법사 메릴에게 힘을 증명하라" },
                hero: "H020");
            s4.pre.Add(S("v3_4",
                L("meril", "물은 모든 것을 비추고, 모든 것에 스며들어요. 그런데 당신에게는…… 비칠 것이 없네요."),
                L("kai", "그래서 '버려진 돌'이라고 불렸어요."),
                L("meril", "아니. 비칠 것이 없다는 건, 무엇이든 비출 수 있다는 뜻이에요. 맑은 물처럼."),
                L("meril", "보여 주세요. 당신이 정말로 회색을 밀어낼 수 있는 그릇인지. 내 호수가 판단하겠어요.", "flash")));
            s4.post.Add(S("v3_4",
                L("meril", "……훌륭해요. 내 호수가 당신을 삼키지 못했어요."),
                L("meril", "회색과 싸우는 일이라면 기꺼이 힘을 빌려 드리죠. 다만──"),
                L("meril", "이그니스와 같은 편에 서는 건 별개의 문제예요. 그 여자가 먼저 사과하지 않는 한."),
                L("leon", "(똑같은 소리를 하는군, 두 사람 다.)"),
                N("── 메릴이 힘을 빌려주기로 했다. [영웅 카드 「메릴」 해금]")));

            var s5 = Stage(v, "국경의 불씨", "회색 안개 · 양군의 충돌", "그날 밤, 두 나라의 국경에서 전투가 터졌다. 하지만 카일의 눈에는 보였다. 양쪽 병사들의 발밑에서 꿈틀대는 회색이.",
                new StoryBattle { oppName = "안개에 홀린 양군", oppHero = oppSoldierCaptain, oppHp = 44, oppDeck = DualDeck(Element.Fire, Element.Water, 2), field = FieldNamed("황혼의 전장"), mist = 1, ariaHeal = 2, leonFire = 2, supports = { BaneS(3) }, goal = "양군 사이에 숨은 안개를 몰아내라" });
            s5.pre.Add(S("v3_5",
                N("국경의 강, 불과 물의 병사들이 맞부딪친다. 증기와 비명이 강 위를 뒤덮었다.", "shake"),
                L("soldier", "물의 놈들이 먼저 쐈다!"),
                L("soldier", "거짓말 마라, 불의 놈들이 다리를 불태웠어!"),
                L("kai", "아니야…… 둘 다 아니야. 봐, 병사들 발밑에!"),
                L("aria", "회색 안개……! 서로를 적으로 보이게 만들고 있어!"),
                L("bane", "역시 바루스의 짓인가. 싸움을 부추기고, 지친 색을 집어삼킨다."),
                L("leon", "그럼 할 일은 하나다. 양쪽 다 정신 차리게 해 주자고!")));
            s5.post.Add(S("v3_5",
                N("척력의 파동이 강을 가로지르자, 양군의 발밑에서 안개가 비명을 지르며 흩어졌다."),
                L("soldier", "……우리가, 왜 서로를……"),
                L("aria", "이제 알겠죠? 적은 서로가 아니에요."),
                N("하지만 강 양쪽 언덕 위. 불의 대마법사와 물의 대마법사가 서로를 노려보며 걸어 나오고 있었다.")));

            var s6 = Stage(v, "두 원소의 격돌", "보스 · 이그니스 & 메릴", "300년의 원한이 폭발한다. 이그니스의 업화와 메릴의 대해일 사이로, 카일이 뛰어든다.",
                new StoryBattle { oppName = "이그니스 & 메릴", oppHero = ignis, oppHp = 56, oppDeck = DualDeck(Element.Fire, Element.Water, 4), field = FieldNamed("황혼의 전장"), ariaHeal = 3, leonFire = 2, supports = { BaneS(3) }, goal = "두 대마법사의 격돌을 멈춰라" });
            s6.battle.tips.Add(Tip("start", "두 원소 한꺼번에", "불과 물이 섞인 덱입니다. 무속성은 어느 쪽의 상성에도 휘둘리지 않아요. 영웅 능력은 큰 몬스터가 나올 때까지 아껴 두세요."));
            s6.pre.Add(S("v3_6",
                L("ignis", "메릴! 네가 안개를 불러들여 내 병사들을 홀린 거지!"),
                L("meril", "그 말 그대로 돌려드리죠, 이그니스. 당신 불꽃이 내 강을 끓였어요."),
                N("하늘이 둘로 갈라진다. 한쪽은 업화, 한쪽은 해일.", "flash"),
                L("leon", "이대로면 국경이 통째로 날아가!"),
                L("kai", "……둘 다 멈추게 할 거야."),
                L("aria", "카일, 설마 저 한가운데로──"),
                L("kai", "불도 물도 섞이지 않는 내 척력이라면, 둘 다 밀어낼 수 있어!", "shake")));
            s6.post.Add(S("v3_6",
                N("업화와 해일이 카일의 양손 앞에서 동시에 멈췄다. 그리고 천천히, 서로에게서 밀려났다.", "shake"),
                L("ignis", "……이 꼬마, 우리 둘을 한꺼번에?"),
                L("meril", "300년 동안 아무도 우리 사이에 서지 못했는데."),
                L("kai", "두 분이 싸울 상대는 서로가 아니에요. 지금 이 순간에도 회색이 두 나라를 먹고 있다고요!"),
                N("그 말을 증명하듯, 강 하류에서 거대한 회색의 물결이 밀려오기 시작했다.", "shake")));

            var s7 = Stage(v, "안개 습격", "보스 · 회색의 부관 · 대마법사 지원", "지친 두 나라를 노린 회색 군단의 기습. 그 선두에 바루스의 부관, 세레스가 있었다.",
                new StoryBattle
                {
                    oppName = "회색의 부관 세레스", oppHero = oppLieutenant, oppHp = 50, oppDeck = GreyDeck(6, 3, 4, 6), field = FieldNamed("황혼의 전장"), mist = 2, ariaHeal = 2, leonFire = 2,
                    supports = { IgnisS(2), MerilS(2) }, goal = "보스: 불과 물이 함께 회색을 막아라",
                },
                opal: 200);
            s7.battle.tips.Add(Tip("start", "대마법사 지원", "이그니스가 상대에게 2 데미지, 메릴이 HP 2 회복! 지원은 내 타임이 시작될 때마다 자동으로 발동합니다."));
            s7.pre.Add(S("v3_7",
                L("varus", "(목소리만) 아쉽군. 두 나라가 서로를 조금만 더 태웠으면 좋았을 것을."),
                N("회색 군단의 선두. 잿빛 망토를 두른 여인이 무표정하게 손을 들었다."),
                L("varus", "세레스. 지친 색들을 거둬라."),
                L("ignis", "……메릴. 300년 묵은 얘기는 나중에 하자."),
                L("meril", "동감이에요. 지금은 저 회색이 더 거슬리니까."),
                L("kai", "가자, 모두!")));
            s7.post.Add(S("v3_7",
                N("업화와 해일이 처음으로 같은 방향을 향했다. 그 길을 카일의 척력이 열어젖힌다.", "flash"),
                N("세레스는 아무 말 없이 안개 속으로 물러났다. 사라지기 직전, 그 텅 빈 눈이 카일을 오래 바라보았다."),
                L("ignis", "……메릴. 그때, 화산의 심장을 얼린 거. 사실 용암이 넘쳐서 내 백성을 덮치려던 걸 막아 준 거였지?"),
                L("meril", "……이제 와서요? 그럼 내 호수를 증발시킨 것도──"),
                L("ignis", "호수에 독이 퍼지고 있었어. 끓여서 정화한 거야."),
                L("aria", "(300년 동안…… 서로를 지켜 주고 있었던 거네.)"),
                L("bane", "자, 다음은 동쪽이다. 대지와 바람, 그리고 번개. 셋 다 만만한 상대는 아니지."),
                T("제3장 · 엇갈리는 원소들", "完 ── 다음 장 「대지와 바람의 시련」")));
            return v;
        }

        // ═══════════════════════════════ 제4장 ═══════════════════════════════
        static StoryVolume Volume4()
        {
            var v = Vol(4, "대지와 바람의 시련", "자유로운 바람, 굳건한 대지, 그리고 번개의 주먹");
            var volt = HeroById("H011");

            var s1 = Stage(v, "방랑하는 음유시인", "대마법사 · 제퍼", "동쪽 고원의 주막. 류트를 튕기는 녹색 머리의 청년이 카일 일행에게 내기를 건다.",
                new StoryBattle { oppName = "제퍼", oppHero = Zephyr, oppHp = 42, oppDeck = ElementDeck(Element.Wind, 10, 8, 3, 9), field = FieldNamed("천공의 정원"), ariaHeal = 2, leonFire = 2, goal = "바람의 대마법사 제퍼와의 내기에서 이겨라" });
            s1.pre.Add(S("v4_1",
                T("제4장", "대지와 바람의 시련"),
                N("고원 마을의 주막. 낡은 류트 소리가 바람처럼 가볍게 흘렀다."),
                L("zephyr", "~ 색 없는 소년이 불과 물을 화해시켰다네~ 하하, 소문 빠르지?"),
                L("leon", "……뭐야, 이 수상한 놈은."),
                L("bane", "수상하지만 대마법사다. 바람의 제퍼. 어디에도 묶이지 않는 자지."),
                L("zephyr", "회색이랑 싸우자고? 좋아, 근데 나는 '자유'가 제일 소중해서 말이야. 내기 하나 하자."),
                L("zephyr", "날 이기면 같이 가 줄게. 지면…… 너희 여행 이야기를 내 노래로 팔 거야!")));
            s1.post.Add(S("v4_1",
                L("zephyr", "와, 진짜로 졌네! 내 바람을 그렇게 뒤집는 건 처음 봐."),
                L("kai", "약속대로, 같이 가 주실 거죠?"),
                L("zephyr", "음~ 가는 건 좋은데…… 사실 부탁이 하나 있어. 내 고향 계곡에 '잿빛 폭풍'이 눌러앉았거든."),
                L("zephyr", "바람이 갇혀 있으면, 바람의 마법사는 숨이 막혀. ……좀 도와줄래?")));

            var s2 = Stage(v, "잿빛 폭풍", "회색 안개 · 제퍼 지원", "하늘섬으로 오르는 바람길. 회색에 물든 폭풍이 길을 막고 있다.",
                new StoryBattle { oppName = "잿빛 폭풍", oppHero = oppStormSpirit, oppHp = 44, oppDeck = ElementDeck(Element.Wind, 9, 9, 4, 8), field = FieldNamed("폭풍의 폭주"), mist = 1, ariaHeal = 2, supports = { ZephyrS(1) }, goal = "잿빛 폭풍을 흩어 버려라" });
            s2.pre.Add(S("v4_2",
                N("하늘섬으로 오르는 바람길. 소용돌이치는 회색 구름이 번개 대신 잿가루를 뿌리고 있었다."),
                L("zephyr", "저게 원래는 우리 계곡을 지키던 바람의 정령이야. 지금은 보다시피."),
                L("aria", "울고 있는 것 같아. 바람 소리가."),
                L("kai", "그럼 풀어 주자. 숲의 사슴왕처럼."),
                L("zephyr", "좋아, 순풍은 내가 불어 줄게. 네 몬스터들, 바람 타고 날아 봐!")));
            s2.post.Add(S("v4_2",
                N("폭풍의 눈이 열리고, 회색 구름 사이로 맑은 하늘이 쏟아져 내렸다."),
                L("zephyr", "……고마워. 정말로."),
                N("그때, 하늘 저편에서 번개가 거꾸로 솟구쳤다. 땅에서 하늘로.", "flash"),
                L("bane", "저 번개…… 볼트로군. 기다리고 있었나 보다.")));

            var s3 = Stage(v, "뇌신의 시험", "대마법사 · 볼트", "뇌운의 탑 꼭대기. 번개의 대마법사 볼트는 마법보다 주먹을 믿는 무인이다.",
                new StoryBattle { oppName = "볼트", oppHero = volt, oppHp = 50, oppDeck = ElementDeck(Element.Thunder, 9, 9, 4, 8), field = FieldNamed("뇌운의 탑"), ariaHeal = 2, leonFire = 2, supports = { ZephyrS(1) }, goal = "볼트의 주먹을 받아 내라" });
            s3.pre.Add(S("v4_3",
                L("volt", "네가 그 '색 없는 주먹'이냐."),
                L("kai", "주먹……이요?"),
                L("volt", "마법사들은 하나같이 멀리서 쏘기만 하지. 하지만 넌 다르다고 들었다. 상대의 힘이 닿는 순간까지 물러서지 않는다고."),
                L("volt", "나도 그렇다. 번개는 쏘는 게 아니라, 꽂는 거다."),
                L("volt", "말은 필요 없다. 주먹으로 대화하자.", "shake"),
                L("leon", "……이번 대마법사는 또 왜 이래.")));
            s3.post.Add(S("v4_3",
                L("volt", "……좋은 주먹이다. 가볍지만, 흔들리지 않아."),
                L("kai", "(손이 아직도 저려……)"),
                L("volt", "회색과의 싸움, 함께하지. 다만 하나 알아 둬라. 동쪽 계곡에 대지의 테라가 있다. 그 노인은 나보다 훨씬 고집이 세다."),
                L("zephyr", "그 전에! 우리 고향 계곡 먼저! 약속했잖아!")));

            var s4 = Stage(v, "바람의 고향", "회색 군단 · 제퍼 & 볼트 지원", "제퍼의 고향, 노래하는 계곡. 바람 대신 회색 병사들의 발소리만이 메아리친다.",
                new StoryBattle { oppName = "회색 병사 대장", oppHero = oppSoldierCaptain, oppHp = 46, oppDeck = GreyDeck(8, 2, 4, 4), field = FieldNamed("천공의 정원"), mist = 2, ariaHeal = 2, supports = { ZephyrS(1), VoltS(3) }, goal = "노래하는 계곡을 되찾아라" },
                hero: "H022");
            s4.pre.Add(S("v4_4",
                N("노래하는 계곡. 바람이 지나가면 바위틈이 피리처럼 울던 곳. 지금은 아무 소리도 나지 않았다."),
                L("zephyr", "……다들, 회색이 됐어."),
                L("zephyr", "난 언제나 떠돌아다녔어. 자유롭고 싶어서. 근데 돌아올 곳이 없어지니까…… 하나도 자유롭지 않네."),
                L("kai", "돌려놓자, 제퍼. 네 노래가 다시 울리게."),
                L("volt", "말이 길다. 병사들이 온다.", "shake")));
            s4.post.Add(S("v4_4",
                N("회색 병사들이 흩어지고, 바위틈을 지나는 바람이 첫 음을 냈다. 이어서 둘째, 셋째 음이."),
                L("zephyr", "~ 색 없는 소년이 바람의 고향을 노래하게 했다네~"),
                L("zephyr", "……이 노래는 팔지 않을래. 내가 간직할 거야."),
                L("zephyr", "좋아, 이제 진짜로 같이 가 줄게. 자유로운 바람이 스스로 고른 길이야."),
                N("── 제퍼가 동료가 되었다. [영웅 카드 「제퍼」 해금]")));

            var s5 = Stage(v, "대지의 수호자", "대마법사 · 테라", "태고의 협곡. 대지의 대마법사 테라는 수백 년째 바위처럼 그 자리를 지키고 있다.",
                new StoryBattle { oppName = "테라", oppHero = Terra, oppHp = 52, oppDeck = ElementDeck(Element.Earth, 9, 9, 4, 8), field = FieldNamed("태고의 협곡"), ariaHeal = 2, leonFire = 3, supports = { ZephyrS(1), VoltS(2) }, goal = "대지의 수호자 테라를 움직여라" });
            s5.pre.Add(S("v4_5",
                L("terra", "……돌아가라. 대지는 움직이지 않는다."),
                L("kai", "회색 안개가 대륙을 먹고 있어요. 이 협곡까지 오는 건 시간문제예요!"),
                L("terra", "대지는 수천 년을 버텼다. 불이 타오르고 물이 넘쳐도, 대지는 그 자리에 있었지. 회색도 지나갈 것이다."),
                L("volt", "노인네. 지나가는 동안 사라지는 사람들은?"),
                L("terra", "……."),
                L("terra", "그렇게 원한다면, 이 대지를 한 걸음이라도 움직여 봐라.", "shake")));
            s5.post.Add(S("v4_5",
                L("terra", "……밀렸군. 대지가."),
                L("terra", "수천 년 동안 처음이다. 내가 서 있던 자리에서 밀려난 것은."),
                N("그때 협곡 깊은 곳에서 땅이 울었다. 테라의 얼굴이 처음으로 굳었다.", "shake"),
                L("terra", "협곡 바닥에서…… 무언가가 깨어난다. 회색이, 대지 깊숙한 곳까지 스며들었어.")));

            var s6 = Stage(v, "안개 거인", "보스 · 테라 & 볼트 지원", "협곡 바닥을 뚫고 일어선 거인. 대지의 색을 통째로 삼킨 안개의 괴물이다.",
                new StoryBattle { oppName = "안개 거인", oppHero = oppGiant, oppHp = 54, oppDeck = GreyDeck(6, 2, 2, 8), field = FieldNamed("태고의 협곡"), mist = 2, ariaHeal = 2, leonFire = 2, supports = { TerraS(3), VoltS(3) }, goal = "보스: 안개 거인을 쓰러뜨려라" });
            s6.pre.Add(S("v4_6",
                N("협곡이 갈라지고, 산만 한 거인이 몸을 일으켰다. 그 몸은 바위가 아니라 굳어 버린 안개였다.", "shake"),
                L("terra", "내가 지키던 대지의 심장을…… 먹어 치웠구나."),
                L("terra", "꼬마. 네가 밀어내라. 쓰러지는 건 내가 받아 내겠다."),
                L("volt", "틈이 생기면 내가 꽂는다."),
                L("kai", "네!")));
            s6.post.Add(S("v4_6",
                N("거인이 무너진다. 그 안에서 흙빛, 이끼빛, 수정빛의 색이 쏟아져 나와 협곡에 스며들었다."),
                L("terra", "……대지가 다시 숨을 쉰다."),
                L("terra", "꼬마, 아니, 카일. 나는 틀렸다. 지나가길 기다리는 것만으로는 아무것도 지킬 수 없더군."),
                L("varus", "(협곡 위에서) ……그럼, 이번엔 지키지 못하게 해 주지.")));

            var s7 = Stage(v, "회색의 부관", "보스 · 세레스 재전 · 대마법사 3인 지원", "협곡 위에 다시 나타난 세레스. 이번에는 회색 군단 전체를 이끌고 왔다.",
                new StoryBattle
                {
                    oppName = "회색의 부관 세레스", oppHero = oppLieutenant, oppHp = 56, oppDeck = GreyDeck(6, 4, 4, 6), field = FieldNamed("태고의 협곡"), mist = 2, ariaHeal = 2, leonFire = 2,
                    supports = { ZephyrS(1), TerraS(2), VoltS(3) }, goal = "보스: 세레스를 쓰러뜨려라",
                },
                opal: 200, hero: "H021");
            s7.pre.Add(S("v4_7",
                L("varus", "(목소리만) 세레스. 대지와 바람과 번개. 셋이 한자리에 모였으니 한 번에 거둬라."),
                N("세레스가 걸어 나온다. 무표정한 얼굴, 그러나 그 손끝이 아주 조금 떨리고 있었다."),
                L("aria", "저 사람…… 어딘가 아파 보여."),
                L("kai", "세레스! 당신도 원래는 색이 있었던 거죠?"),
                L("varus", "(목소리만) 대답할 필요 없다, 세레스. 그 아이에게 회색의 고요함을 가르쳐 줘라.")));
            s7.post.Add(S("v4_7",
                N("세레스가 무릎을 꿇는다. 그 눈에서, 아주 잠깐 바다빛 파란색이 반짝였다가 사라졌다."),
                L("terra", "……저 여자. 원래는 물의 마법사였군."),
                L("kai", "세레스, 같이 가요! 색을 되찾을 수 있어요!"),
                L("varus", "(목소리만) 돌아와라, 세레스. 우리는 성도로 간다. 빛과 어둠이 서로를 증오하는 그곳으로."),
                N("세레스는 안개와 함께 사라졌다. 남은 것은 파란 물방울 하나."),
                L("terra", "이 늙은이도 가겠다. 대지는 움직이지 않는다는 말, 오늘부로 철회하지. [영웅 카드 「테라」 해금]"),
                L("bane", "빛의 루미엔과 어둠의 에레보스. ……가장 어려운 둘이 남았군."),
                T("제4장 · 대지와 바람의 시련", "完 ── 다음 장 「빛과 어둠의 성전」")));
            return v;
        }

        // ═══════════════════════════════ 제5장 ═══════════════════════════════
        static StoryVolume Volume5()
        {
            var v = Vol(5, "빛과 어둠의 성전", "서로를 증오하는 빛과 어둠, 그리고 일곱이 모이는 날");

            var s1 = Stage(v, "성도의 기사단", "빛의 성도", "빛의 성도 「루미나리아」. 성기사단은 '어둠의 기척'을 이유로 카일 일행을 막아선다.",
                new StoryBattle { oppName = "성기사단장 아우렐", oppHero = oppPaladin, oppHp = 46, oppDeck = ElementDeck(Element.Light, 10, 8, 3, 9), field = FieldNamed("광휘의 성당"), ariaHeal = 2, leonFire = 2, supports = { VoltS(2) }, goal = "성기사단을 통과하라" });
            s1.pre.Add(S("v5_1",
                T("제5장", "빛과 어둠의 성전"),
                N("하얀 대리석과 금빛 첨탑. 빛의 성도 루미나리아는 눈이 부실 만큼 찬란했다."),
                L("knight", "멈춰라. 너에게서…… 빛이 비치지 않는다."),
                L("kai", "저는 무속성이라서──"),
                L("knight", "빛이 없는 것은 어둠이다. 어둠의 첩자를 성도에 들일 수는 없다!"),
                L("leon", "빛 아니면 어둠이라니, 여기도 똑같군."),
                L("knight", "성광의 이름으로, 심판한다!", "flash")));
            s1.post.Add(S("v5_1",
                L("knight", "……이 힘, 어둠이 아니다. 빛도 아니지만, 더럽혀져 있지도 않다."),
                N("성당의 종이 울린다. 대성당의 문이 스스로 열렸다."),
                L("lumienQ", "들어오세요. ……기다리고 있었어요, 아주 오래.")));

            var s2 = Stage(v, "빛의 성녀", "대마법사 · 루미엔", "빛의 대마법사 루미엔. 제1장의 프롤로그에서 들려온 그 목소리의 주인이다.",
                new StoryBattle { oppName = "루미엔", oppHero = Lumien, oppHp = 50, oppDeck = ElementDeck(Element.Light, 9, 9, 4, 8), field = FieldNamed("광휘의 성당"), ariaHeal = 2, leonFire = 2, supports = { TerraS(2) }, goal = "성녀 루미엔의 시련을 넘어라" },
                hero: "H023");
            s2.pre.Add(S("v5_2",
                L("lumien", "처음 회색이 대륙에 스며들던 밤, 나는 기도했어요. 누군가, 이 회색을…… 이라고."),
                L("kai", "그 목소리…… 꿈에서 들은 적이 있어요."),
                L("lumien", "그 기도에 응답한 것이 빛이 아니라 '무색'이었을 줄은 몰랐네요."),
                L("lumien", "하지만 빛은 확인해야 해요. 당신이 회색과 정말로 다른지. 당신의 무색이, 빛 앞에서도 무너지지 않는지.", "flash")));
            s2.post.Add(S("v5_2",
                L("lumien", "……눈부신 빛 속에서도, 당신은 그림자를 만들지 않네요."),
                L("lumien", "빛은 당신의 편이에요. 하지만, 한 가지만 부탁할게요."),
                L("lumien", "에레보스. 어둠의 대마법사만은…… 믿지 마세요. 그는 빛을 증오해요."),
                L("aria", "(또…… 같은 이야기야.)"),
                N("── 루미엔이 힘을 빌려주기로 했다. [영웅 카드 「루미엔」 해금]")));

            var s3 = Stage(v, "어둠의 군세", "칠흑의 심연 · 루미엔 지원", "성도의 반대편, 칠흑의 심연. 어둠의 군세가 성도를 향해 진군을 시작한다.",
                new StoryBattle { oppName = "어둠의 장군 모르드", oppHero = oppDarkGeneral, oppHp = 48, oppDeck = ElementDeck(Element.Dark, 10, 8, 3, 9), field = FieldNamed("칠흑의 심연"), ariaHeal = 2, leonFire = 2, supports = { LumienS(3) }, goal = "어둠의 군세의 진군을 멈춰라" });
            s3.pre.Add(S("v5_3",
                N("성도의 경계선. 땅이 검게 물들고, 그 위를 그림자 병사들이 밀물처럼 덮쳐 왔다."),
                L("soldier", "어둠의 군세다! 결국 쳐들어왔어!"),
                L("leon", "잠깐, 저 병사들 발밑. 회색이 없어."),
                L("kai", "이번엔 안개가 아니야. 진짜로…… 어둠의 의지야."),
                L("bane", "그렇다면 왜 지금이지? 회색이 대륙을 덮치는 이 시기에."),
                L("kai", "만나서 물어보면 돼요. 일단 진군부터 멈추고!")));
            s3.post.Add(S("v5_3",
                L("kai", "왜 성도를 공격하는 거예요?"),
                L("mord", "……공격이 아니다. 피난이다. 우리 심연이 회색에 먹히고 있다."),
                L("knight", "빛의 성도는 회색이 닿지 않는 유일한 땅. 하지만 저들은 우리에게 문을 열지 않았다."),
                L("aria", "그래서 무기를 들고……."),
                L("mord", "에레보스 님께 가라. 그분이 너희를 부르셨다.")));

            var s4 = Stage(v, "심연의 왕", "대마법사 · 에레보스", "칠흑의 왕좌. 어둠의 대마법사 에레보스는 빛을 증오한다고 알려져 있다.",
                new StoryBattle { oppName = "에레보스", oppHero = Erebos, oppHp = 50, oppDeck = ElementDeck(Element.Dark, 9, 9, 4, 8), field = FieldNamed("칠흑의 심연"), ariaHeal = 2, leonFire = 2, supports = { VoltS(3) }, goal = "어둠의 왕 에레보스와 마주하라" });
            s4.pre.Add(S("v5_4",
                L("erebos", "빛의 성녀에게서 왔군. 그 여자가 뭐라 하던가. 나를 믿지 말라고?"),
                L("kai", "……네."),
                L("erebos", "솔직하군. 마음에 든다."),
                L("erebos", "나는 빛을 증오한다. 빛은 언제나 어둠을 '악'이라 부르며 몰아냈으니까. 하지만──"),
                L("erebos", "회색은 더 증오한다. 회색은 어둠조차 남기지 않는다. 그림자도, 밤도, 꿈도."),
                L("erebos", "너의 무색이 회색과 무엇이 다른지, 이 심연에서 보여 다오.", "shake")));
            s4.post.Add(S("v5_4",
                L("erebos", "……그렇군. 너는 아무것도 없지만, 무엇도 지우지 않는다."),
                L("erebos", "좋다. 어둠도 회색과 싸우겠다. 단, 루미엔이 우리 백성에게 문을 연다면."),
                L("kai", "제가 부탁해 볼게요. 아니, 꼭 열게 할게요."),
                N("그때, 성도와 심연 사이의 하늘이 동시에 회색으로 물들었다.", "flash")));

            var s5 = Stage(v, "회색 군단", "짙은 회색 안개 · 빛과 어둠의 지원", "빛과 어둠이 서로를 경계하는 사이, 회색 군단이 두 땅을 한꺼번에 덮친다.",
                new StoryBattle
                {
                    oppName = "회색 군단", oppHero = oppGreyLegion, oppHp = 52, oppDeck = GreyDeck(8, 4, 4, 6), field = FieldNamed("황혼의 전장"), mist = 3, ariaHeal = 2, leonFire = 2,
                    supports = { LumienS(3), ErebosS(3) }, goal = "빛과 어둠을 지켜라 (속성 ATK -3)",
                });
            s5.battle.tips.Add(Tip("start", "가장 짙은 안개", "속성 몬스터 ATK -3. 카일의 무속성 몬스터와 동료 지원이 승부의 열쇠입니다."));
            s5.pre.Add(S("v5_5",
                N("지평선 끝까지 늘어선 회색 병사들. 그 수는 지금까지와 비교도 되지 않았다.", "shake"),
                L("lumien", "에레보스의 군세가 회색을 불러들인 거예요!"),
                L("erebos", "헛소리. 너의 성도가 문을 닫은 사이에 우리 땅이 먹혔다!"),
                L("kai", "둘 다 그만해요!!", "flash"),
                L("kai", "회색은 빛도 어둠도 가리지 않아요. 지금 싸우면, 둘 다 사라져요."),
                L("lumien", "……."),
                L("erebos", "……흥. 성녀, 이번만은 등을 맡기지.")));
            s5.post.Add(S("v5_5",
                N("빛의 장막과 어둠의 창이 나란히 회색 군단을 꿰뚫었다. 처음 보는 광경에 양쪽 병사들이 숨을 삼켰다."),
                L("lumien", "……어둠이 이렇게 따뜻할 줄은 몰랐어요."),
                L("erebos", "빛이 이렇게 조용할 줄도 몰랐군."),
                N("회색 군단이 갈라지며 길을 연다. 그 끝에서, 바루스가 걸어 나왔다.")));

            var s6 = Stage(v, "바루스", "이벤트 전투 · 져도 이야기가 이어집니다", "회색의 사도 바루스. 여덟 번째 색이었던 남자가 마침내 진실을 이야기한다.",
                new StoryBattle
                {
                    oppName = "바루스", oppHero = Varus, oppHp = 62, oppDeck = GreyDeck(6, 4, 4, 8), field = FieldNamed("황혼의 전장"), mist = 2, ariaHeal = 2, leonFire = 2, allowLose = true,
                    supports = { LumienS(2), ErebosS(2) }, goal = "바루스와 맞서라 (패배해도 진행)",
                });
            s6.pre.Add(S("v5_6",
                L("varus", "빛과 어둠이 나란히라. ……300년 전에 보았다면 좋았을 광경이군."),
                L("bane", "바루스. 아니, '은빛의 바루스'. 여덟 번째 색의 대마법사."),
                L("varus", "오랜만이군, 베인. 그 이름을 아직 기억하는 자가 있었나."),
                L("varus", "나는 은빛을 품고 태어났다. 일곱 색 어디에도 속하지 않는 색. 그래서 일곱 대마법사들은 나를 두려워했고, 몰아냈지."),
                L("varus", "그리고 나는 보았다. 색과 색이 서로를 몰아내는 이 세계의 끝을. ……차라리 모든 색이 없어지는 편이 낫다."),
                L("kai", "그래서 회색으로 덮겠다고요? 당신이 당한 걸 모든 사람에게?"),
                L("varus", "덮는 것은 내가 아니다. 나는 그저 문을 여는 자. ……곧 오실 분을 위해.", "shake")));
            s6.post.Add(S("v5_6",
                L("varus", "……역시 넌 흥미롭다. 은빛조차 너를 물들이지 못하는군.", null, 1),
                L("varus", "아직은 그 정도인가. 하지만 포기하지 않는 눈이다.", null, 2),
                L("varus", "일곱 대마법사. 모두 모였군. 마침 잘 됐다."),
                L("varus", "망각의 군주께서는 가장 진한 색을 가장 먼저 원하시니까."),
                L("erebos", "망각의…… 군주?"),
                N("바루스가 안개 속으로 물러난다. 하늘 높은 곳에서, 무언가가 '금'이 가는 소리가 들렸다.")));

            var s7 = Stage(v, "일곱이 모이다", "보스 · 일곱 대마법사 총출동", "성도와 심연 사이의 평원. 일곱 대마법사가 처음으로 한자리에 모여 회색의 문을 봉인한다.",
                new StoryBattle
                {
                    oppName = "회색 군단 본대", oppHero = oppGreyLegion, oppHp = 60, oppDeck = GreyDeck(6, 5, 5, 8), field = FieldNamed("황혼의 전장"), mist = 2, ariaHeal = 2, leonFire = 2,
                    supports = { IgnisS(2), MerilS(2), LumienS(2), ErebosS(3) }, goal = "보스: 일곱 색으로 회색의 문을 봉인하라",
                },
                opal: 200);
            s7.battle.tips.Add(Tip("start", "일곱 색의 지원", "이그니스·메릴·루미엔·에레보스가 매 타임 지원합니다. 제퍼·테라·볼트는 봉인 의식 중!"));
            s7.pre.Add(S("v5_7",
                N("성도와 심연 사이, 황혼의 평원. 일곱 대마법사가 원을 그리며 섰다. 불, 물, 바람, 대지, 번개, 빛, 어둠."),
                L("ignis", "살다 보니 이런 날도 오는군. 메릴이랑 같은 원 안에 서다니."),
                L("meril", "300년 만에 처음으로 의견이 맞네요."),
                L("zephyr", "~ 일곱 색이 손을 잡았다네~"),
                L("volt", "노래는 끝나고 불러라."),
                L("terra", "봉인 의식은 우리가 맡겠다. 그동안 의식을 지켜라, 카일."),
                L("kai", "맡겨 주세요!")));
            s7.post.Add(S("v5_7",
                N("일곱 색의 기둥이 하늘로 솟아오르고, 회색의 문이 하나, 또 하나 닫혀 갔다.", "flash"),
                L("lumien", "대륙의 문들이…… 닫히고 있어요."),
                L("aria", "해냈어, 카일! 우리가──"),
                N("──쩌적.", "shake"),
                N("하늘이 갈라졌다. 문이 아니었다. 하늘 그 자체가, 유리처럼 금이 가고 있었다."),
                L("varus", "(목소리만) 문을 닫아 주어 고맙다. 덕분에 색이 한곳에 모였군."),
                L("varus", "보아라. 망각의 군주께서 강림하신다."),
                T("제5장 · 빛과 어둠의 성전", "完 ── 다음 장 「붕괴하는 하늘」")));
            return v;
        }
    }
}
