using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 상점 전체 구성: [로지의 카드 상점] [로한·벨라의 마력부여실] [루콘의 꾸미기 공방] 세 방.
    /// 상점 탭에 들어오면 정문(양문)이 열리며 가운데 마력부여실로 걸어 들어간다.
    /// 옆 방으로 갈 때는 방문(외문)이 다가와 열리고 그 문을 지나 들어간다 (화살표 · 스와이프 · ←/→ · A/D).
    /// 방마다 오른쪽 위 ? 아이콘에 설명(확률 · 장수 · 가격)이 있다.
    /// </summary>
    public partial class MainMenu
    {
        const int SecRosie = 0, SecHall = 1, SecLukon = 2;

        RectTransform shopCam, shopStrip, hallView, doorRoot, doorL, doorR, navL, navR;
        Image doorLight;
        CanvasGroup doorGroup, hallBoard;
        Text navLText, navRText, hallStatus;
        int shopSection = SecHall, pendingSection = -1;
        bool shopMoving, doorPlaying, doorSkip, lukonGreetedThisVisit, parentsGreetedThisVisit;
        float swipeDx;

        float PageW => Mathf.Max(1f, shopPage.rect.width);

        RectTransform BuildShop()
        {
            var page = UIKit.Rect("Shop", contentArea).Fill();
            shopPage = page;
            page.gameObject.AddComponent<RectMask2D>();      // 옆 방이 사이드바 위로 비치지 않게

            // 카메라(확대용) 안에 세 방을 가로로 이어 둔 띠. 한 번에 한 방만 보인다
            shopCam = UIKit.Rect("Cam", page).Fill();
            shopStrip = UIKit.Rect("Strip", shopCam);
            shopStrip.anchorMin = Vector2.zero;
            shopStrip.anchorMax = new Vector2(3, 1);
            shopStrip.offsetMin = shopStrip.offsetMax = Vector2.zero;
            shopStrip.HitArea();                              // 빈 곳 스와이프도 받도록
            var drag = shopStrip.gameObject.AddComponent<ShopDrag>();
            drag.begin = () => { swipeDx = 0; return true; };
            drag.move = dx => swipeDx += dx;
            drag.end = () => { if (Mathf.Abs(swipeDx) > 90) GoSection(shopSection + (swipeDx < 0 ? 1 : -1)); swipeDx = 0; };

            BuildShopView(shopStrip);
            shopView.Anchor(0, 0, 1 / 3f, 1, 0, 0, 6, 0);
            BuildHall(shopStrip);
            BuildCosmeticView(shopStrip);
            cosmeticView.Anchor(2 / 3f, 0, 1, 1, 6, 0, 0, 0);
            BuildEnchantView(page);
            BuildUpgradeView(page);
            BuildShopNav(page);
            BuildRoomDoor(page);
            BuildDoor(page);
            return page;
        }

        // ══════════════════════════ 가운데: 로한 · 벨라의 마력부여실 ══════════════════════════

        RectTransform rohanImg, bellaImg;
        Text parentLine, parentName;
        Image parentTag;
        Coroutine parentCo;

        static readonly (bool rohan, string line)[] ParentGreet =
        {
            (true, "어서 와라! 오늘은 어떤 카드에 마력을 불어넣어 볼까?"),
            (false, "어서 오렴. 차 한잔하고 가도 좋단다."),
            (true, "남는 카드가 77장 모이면 가져와! 더 좋은 카드로 바꿔 주마."),
            (false, "가공은 섬세한 작업이라 CP 가 꽤 들어. 신중하게 고르렴."),
            (false, "업그레이드로 나온 카드는 가공 없는 일반 카드란다. 잊지 마~"),
        };
        static readonly string[] RohanPoke =
        {
            "이 팔뚝 보이지? 마력부여도 힘이 필요하다고!",
            "아버지는 아직도 그 골뱅이 안경을 안 바꾸셨나?",
            "77장! 행운의 숫자지. 7이 두 개니까!",
            "로지 녀석, 손님한테 친절하게 굴고 있겠지?",
        };
        static readonly string[] BellaPoke =
        {
            "간지러워.",
            "로지가 장사 잘 하고 있니? 가끔 걱정이 돼서.",
            "Unique 카드는 Rare 카드를 77장 모아도 1장밖에 안 나올 만큼 강력한 카드란다.",
            "StrikerZ 가공은 마력이 아주 많이 들어. 그래도 그만큼 반짝이지!",
        };

        void BuildHall(RectTransform strip)
        {
            hallView = UIKit.Rect("HallView", strip).Anchor(1 / 3f, 0, 2 / 3f, 1, 6, 0, 6, 0);
            var frame = UIKit.Frame(hallView, "Scene", new Color(0.02f, 0.01f, 0.03f, 1f));
            frame.rectTransform.Fill();
            var mask = UIKit.Img(frame.transform, "Mask", Color.white, GameAssets.Rounded, true);
            mask.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var bg = UIKit.Img(mask.transform, "Bg", new Color(0.92f, 0.9f, 0.9f), GameAssets.TexSprite("shop_hall_bg") ?? GameAssets.TexSprite("shop_bg"));
            bg.rectTransform.Fill();
            // 방 전체에 어둑한 막만 씌워 배경이 비쳐 보이게 (창 없이 로지·루콘 방처럼 화면 가득)
            var shade = UIKit.Img(mask.transform, "Shade", new Color(0.02f, 0.01f, 0.05f, 0.5f));
            shade.rectTransform.Fill();
            var floor = UIKit.Img(mask.transform, "FloorShade", new Color(0.02f, 0.01f, 0.05f, 0.55f));
            floor.rectTransform.Anchor(0, 0, 1, 0.42f);                    // 아래 패널 쪽은 조금 더 어둡게
            frame.transform.GetChild(0).SetAsLastSibling();

            var board = UIKit.Rect("Board", hallView).Anchor(0, 0, 1, 1, 52, 12, 52, 12);   // 양옆 ◀ ▶ 이동 버튼 자리만 비운다
            hallBoard = board.gameObject.AddComponent<CanvasGroup>();
            var b = (Transform)board;
            var title = UIKit.Txt(b, "Title", "마력부여실", 38, new Color(0.88f, 0.78f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0.25f, 1, 0.75f, 1, 0, -66, 0, 14);
            title.Outline(new Color(0.15f, 0.05f, 0.25f, 0.9f), 2);
            var sub = UIKit.Txt(b, "Sub", "로지의 엄마 아빠가 카드에 마력을 불어넣는 곳   ·   ◀ 카드 상점      악세서리 상점 ▶", 17, UIKit.Ink, TextAnchor.MiddleCenter);
            sub.rectTransform.Anchor(0.2f, 1, 0.8f, 1, 0, -98, 0, 66);
            sub.Outline(new Color(0.02f, 0.01f, 0.05f, 0.9f), 1);
            HelpIcon((RectTransform)b, ParentsHelp).Place(new Vector2(1, 1), new Vector2(-8, -8), new Vector2(44, 44), new Vector2(1, 1));

            // 부모님 + 대화 (가운데에 마력부여기). 부모님은 양옆에 크게 서 있다
            var stage = UIKit.Rect("Parents", b).Anchor(0, 0.4f, 1, 1);
            var machineGlow = UIKit.Img(stage, "Glow", new Color(0.75f, 0.5f, 1f, 0.35f), GameAssets.Glow);
            machineGlow.rectTransform.Anchor(0.33f, 0.24f, 0.67f, 0.86f);
            var machine = UIKit.Img(stage, "Machine", Color.white, GameAssets.TexSprite("enchanter"));
            machine.preserveAspect = true;
            machine.rectTransform.Anchor(0.38f, 0.33f, 0.62f, 0.8f);
            rohanImg = ParentPortrait(stage, true);
            bellaImg = ParentPortrait(stage, false);
            var bubble = UIKit.Frame(stage, "Dialog", new Color(0.05f, 0.03f, 0.09f, 0.93f));
            bubble.rectTransform.Anchor(0.31f, 0.0f, 0.69f, 0.3f);
            parentTag = UIKit.Img(bubble.transform, "Tag", new Color(0.4f, 0.22f, 0.62f, 1f), GameAssets.Rounded, true);
            parentTag.rectTransform.Place(new Vector2(0, 1), new Vector2(14, 16), new Vector2(150, 34), new Vector2(0, 1));
            parentName = UIKit.Txt(parentTag.transform, "N", "로한", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            parentName.rectTransform.Fill();
            parentLine = UIKit.Txt(bubble.transform, "Line", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            parentLine.rectTransform.Anchor(0, 0, 1, 1, 18, 8, 14, 24);
            parentLine.lineSpacing = 1.08f;

            // 두 가지 일: 마력부여(가공) · 카드 업그레이드
            ServicePanel(b, new Vector4(0.08f, 0.085f, 0.495f, 0.385f), "마력부여 (가공)", new Color(0.85f, 0.72f, 1f),
                $"같은 등급 CP 로 일반 카드에 마력을 입힌다\n{FinishStyle.Rich(CardFinish.Seven, " Seven")} CP {ShopDaily.SevenEnchantCost:N0}   ·   {FinishStyle.Rich(CardFinish.StrikerZ, " StrikerZ")} CP {ShopDaily.StrikerZEnchantCost:N0}",
                "가공하러 가기", new Color(0.38f, 0.18f, 0.6f, 0.97f), () => OpenEnchanter(null));
            ServicePanel(b, new Vector4(0.505f, 0.085f, 0.92f, 0.385f), "카드 업그레이드", new Color(1f, 0.82f, 0.5f),
                "같은 등급 77장 → 윗 등급 무작위\nCommon→Uncommon 7 · Uncommon→Rare 7 · Rare→Unique 1\n<color=#f98><size=14>가공 카드를 넣어도 결과는 일반 카드</size></color>",
                "업그레이드 하러 가기", new Color(0.6f, 0.36f, 0.08f, 0.97f), OpenUpgrade);

            hallStatus = UIKit.Txt(b, "Status", "", 17, UIKit.Ink, TextAnchor.MiddleCenter);
            hallStatus.rectTransform.Anchor(0, 0, 1, 0.075f, 10, 4, 10, 0);
        }

        /// <summary>부모님 초상 (허리 위만, 클릭하면 대사 + 깡총)</summary>
        RectTransform ParentPortrait(RectTransform stage, bool rohan)
        {
            var holder = UIKit.Rect(rohan ? "Rohan" : "Bella", stage).Anchor(rohan ? 0.02f : 0.7f, 0, rohan ? 0.3f : 0.98f, 1);
            holder.gameObject.AddComponent<RectMask2D>();
            var sp = GameAssets.TexSprite(rohan ? "shop_rohan" : "shop_bella");
            var img = UIKit.Img(holder, "Img", Color.white, sp);
            var ir = img.rectTransform;
            ir.anchorMin = new Vector2(0.02f, 1); ir.anchorMax = new Vector2(0.98f, 1); ir.pivot = new Vector2(0.5f, 1);
            ir.anchoredPosition = new Vector2(0, 6);
            var f = img.gameObject.AddComponent<AspectRatioFitter>();
            f.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            f.aspectRatio = sp != null ? sp.rect.width / sp.rect.height : 0.7f;
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(Hop(holder));
                ParentsSay((rohan, rohan ? RohanPoke[rng.Next(RohanPoke.Length)] : BellaPoke[rng.Next(BellaPoke.Length)]));
            });
            // 이름표: 로지·루콘 대화창과 같은 색 둥근 꼬리표 (이름만)
            var tag = UIKit.Img(holder, "Name", rohan ? new Color(0.4f, 0.22f, 0.62f, 1f) : new Color(0.62f, 0.25f, 0.45f, 1f), GameAssets.Rounded, true);
            tag.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(120, 38), new Vector2(0.5f, 0));
            tag.raycastTarget = false;
            var t = UIKit.Txt(tag.transform, "T", rohan ? "로한" : "벨라", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Fill();
            return holder;
        }

        IEnumerator Hop(RectTransform r)
        {
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.4f;
                r.localScale = new Vector3(1f - Mathf.Sin(k * Mathf.PI) * 0.03f, 1f + Mathf.Sin(k * Mathf.PI) * 0.05f, 1f);
                yield return null;
            }
            r.localScale = Vector3.one;
        }

        void ServicePanel(Transform parent, Vector4 a, string head, Color headCol, string body, string go, Color goCol, Action onGo)
        {
            var p = UIKit.Img(parent, head, new Color(0.03f, 0.02f, 0.06f, 0.88f), GameAssets.Rounded, true).rectTransform;
            p.Anchor(a.x, a.y, a.z, a.w);
            var h = UIKit.Txt(p, "Head", head, 23, headCol, TextAnchor.UpperLeft, FontStyle.Bold);
            h.rectTransform.Anchor(0, 1, 1, 1, 18, -40, 14, 10);
            h.Section(headCol, 22);
            var t = UIKit.Txt(p, "Body", body, 16, UIKit.Ink, TextAnchor.UpperLeft);
            t.rectTransform.Anchor(0, 0.33f, 1, 1, 18, 0, 14, 46);
            t.lineSpacing = 1.15f;
            var b = UIKit.Button(p, go, onGo, goCol, 19, Color.white);
            b.rt.Anchor(0.05f, 0.05f, 0.95f, 0.3f);
        }

        void ParentsSay(params (bool rohan, string line)[] lines)
        {
            if (parentLine == null) return;
            if (parentCo != null) StopCoroutine(parentCo);
            parentCo = StartCoroutine(ParentsTalk(lines));
        }

        IEnumerator ParentsTalk((bool rohan, string line)[] lines)
        {
            for (int n = 0; n < lines.Length; n++)
            {
                var (rohan, line) = lines[n];
                parentName.text = rohan ? "로한" : "벨라";
                parentTag.color = rohan ? new Color(0.4f, 0.22f, 0.62f, 1f) : new Color(0.62f, 0.25f, 0.45f, 1f);
                Voice.Line(rohan ? "rohan" : "bella", line);
                for (int i = 1; i <= line.Length; i++)
                {
                    parentLine.text = line.Substring(0, i);
                    yield return new WaitForSecondsRealtime(0.022f);
                }
                if (n < lines.Length - 1) yield return new WaitForSecondsRealtime(1.6f);
            }
        }

        void GreetParents()
        {
            var d = Profile.Data;
            StartCoroutine(Hop(rohanImg));
            if (!d.parentsMet)
            {
                d.parentsMet = true;
                Profile.Save();
                ParentsSay((true, "오, 처음 보는 얼굴이구나! 나는 로한, 이쪽은 내 아내 벨라. 둘 다 마력부여사지!"),
                           (false, "로지의 엄마 아빠란다. 카드 가공이랑 업그레이드는 우리한테 맡기렴."));
            }
            else ParentsSay(ParentGreet[rng.Next(ParentGreet.Length)]);
        }

        void RefreshHall()
        {
            if (hallStatus == null) return;
            hallStatus.text = $"보유 CP  {Crafting.CpText()}   ·   보유 오팔 <color=#ffd77a>{Profile.Data.opal:N0}</color>";
        }

        // ══════════════════════════ ? 설명 ══════════════════════════

        RectTransform helpRoot;
        Text helpTitle, helpBody;
        UIKit.Btn helpExtra;
        Action helpExtraAct;

        /// <summary>오른쪽 위 ? 아이콘. 누르면 그 방의 설명(확률 · 장수 · 가격)</summary>
        RectTransform HelpIcon(RectTransform parent, Func<(string title, string body, string extra, Action act)> content)
        {
            var bg = UIKit.Img(parent, "Help", new Color(0.95f, 0.78f, 0.35f, 1f), GameAssets.Rounded, true);
            bg.raycastTarget = true;
            var q = UIKit.Txt(bg.transform, "Q", "?", 28, new Color(0.18f, 0.08f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold);
            q.rectTransform.Fill();
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { var c = content(); ShowHelp(c.title, c.body, c.extra, c.act); });
            return bg.rectTransform;
        }

        void ShowHelp(string title, string body, string extra = null, Action act = null)
        {
            if (helpRoot == null)
            {
                var bg = UIKit.Img(root, "Help", new Color(0, 0, 0, 0.85f));
                bg.raycastTarget = true;
                helpRoot = bg.rectTransform;
                helpRoot.Fill();
                OverlayCanvas(helpRoot, 41);
                var panel = UIKit.Frame(helpRoot, "Panel", new Color(0.05f, 0.03f, 0.1f, 0.98f));
                panel.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 760));
                helpTitle = UIKit.Txt(panel.transform, "Title", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                helpTitle.rectTransform.Anchor(0, 1, 1, 1, 34, -70, 34, 22);
                helpTitle.Section(UIKit.SecCollect, 26);
                helpBody = UIKit.Txt(panel.transform, "Body", "", 19, UIKit.Ink, TextAnchor.UpperLeft);
                helpBody.rectTransform.Anchor(0, 0, 1, 1, 36, 100, 36, 90);
                helpBody.lineSpacing = 1.22f;
                helpBody.resizeTextForBestFit = true;
                helpBody.resizeTextMinSize = 13;
                helpBody.resizeTextMaxSize = 19;
                UIKit.Button(panel.transform, "닫기", () => helpRoot.gameObject.SetActive(false), Dark, 22).rt.Anchor(0.5f, 0, 0.5f, 0, 10, 24, -250, -80);
                helpExtra = UIKit.Button(panel.transform, "", () => { helpRoot.gameObject.SetActive(false); helpExtraAct?.Invoke(); }, new Color(0.38f, 0.18f, 0.6f, 0.97f), 20, Color.white);
                helpExtra.rt.Anchor(0.5f, 0, 0.5f, 0, -250, 24, 10, -80);
            }
            helpTitle.text = title;
            helpBody.text = body;
            helpExtraAct = act;
            helpExtra.bg.gameObject.SetActive(extra != null);
            if (extra != null) helpExtra.label.text = extra;
            helpRoot.SetAsLastSibling();
            helpRoot.gameObject.SetActive(true);
        }

        static string KindsLine(Rarity r)
        {
            int kinds = CardUpgrade.Pool(r).Count;
            return $"{CardData.RarityRich(r)} 수록 {kinds}종 · 1종당 {BoosterPack.Pct(1.0 / Mathf.Max(1, kinds))}";
        }

        (string, string, string, Action) RosieHelp()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in BoosterPack.Packs)
            {
                var rows = p.Breakdown();
                int c = p.cardsPerPack;
                double AtLeast(double q) => 1 - Math.Pow(1 - q, c);
                double rare = rows.Where(r => r.rarity >= Rarity.Rare).Sum(r => (double)r.chance);
                double uni = rows.Where(r => r.rarity == Rarity.Unique).Sum(r => (double)r.chance);
                sb.Append($"<b><color=#ffd77a>부스터팩 {p.code}</color></b>  {p.name} · 1팩 {c}장 · {p.price} 오팔\n");
                sb.Append($"· 등급 확률 (카드마다 따로)  {string.Join("  ·  ", rows.Select(r => $"{CardData.RarityRich(r.rarity)} {BoosterPack.Pct(r.chance)}"))}\n");
                sb.Append($"· 수록 종수 · 카드 1종 확률  {string.Join("  ·  ", rows.Select(r => $"{CardData.RarityLabel(r.rarity)} {r.kinds}종 ({BoosterPack.Pct(r.perCard)})"))}\n");
                sb.Append($"· 가공 확률 (카드마다, 등급과 별개)  {BoosterPack.FinishRateText}\n");
                sb.Append($"· 1팩 {c}장 중 1장 이상  Rare 이상 {BoosterPack.Pct(AtLeast(rare))} · Unique {BoosterPack.Pct(AtLeast(uni))} · Seven 이상 {BoosterPack.Pct(AtLeast(BoosterPack.SevenChance + BoosterPack.StrikerZChance))} · StrikerZ {BoosterPack.Pct(AtLeast(BoosterPack.StrikerZChance))}\n");
                sb.Append("· 같은 등급 안의 카드는 모두 같은 확률 · 팩마다 독립 판정 · 천장 없음\n\n");
            }
            sb.Append("<b><color=#9cf>오늘의 특가</color></b>  매일 갱신 · 등급마다 낱장 1장 (Common · Uncommon · Rare)\n");
            sb.Append($"· 가격 = 그 등급 CP {ShopDaily.CardPrice} (생성 비용 {Crafting.CraftCost} 의 절반)\n");
            sb.Append($"· 가공 특가는 가공비도 절반  {FinishStyle.Rich(CardFinish.Seven, " Seven")} +{ShopDaily.FinishPrice(CardFinish.Seven):N0} · {FinishStyle.Rich(CardFinish.StrikerZ, " StrikerZ")} +{ShopDaily.FinishPrice(CardFinish.StrikerZ):N0}\n");
            sb.Append("· 한 번 사면 그날은 품절");
            return ("로지의 카드 상점 — 확률 · 가격", sb.ToString(), "확률 상세 표", () => ShowOdds(BoosterPack.Latest));
        }

        (string, string, string, Action) ParentsHelp()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<b><color=#d9c2ff>마력부여 (가공)</color></b>  같은 등급 CP 를 써서 내 카드 1장에 가공을 입힌다 (확률 없이 100% 성공)\n");
            sb.Append($"· {FinishStyle.Rich(CardFinish.Seven, " Seven")}  CP {ShopDaily.SevenEnchantCost:N0} — 일반 사본 1장을 Seven 으로\n");
            sb.Append($"· {FinishStyle.Rich(CardFinish.StrikerZ, " StrikerZ")}  CP {ShopDaily.StrikerZEnchantCost:N0} — 일반 사본(없으면 Seven 사본) 1장을 StrikerZ 로\n\n");
            sb.Append($"<b><color=#ffd77a>카드 업그레이드</color></b>  같은 등급 카드 {CardUpgrade.Input}장을 넣으면 윗 등급 카드로 바뀐다 (장수는 고정, 어떤 카드인지만 무작위)\n");
            foreach (var r in CardUpgrade.Sources)
                sb.Append($"· {CardData.RarityRich(r)} {CardUpgrade.Input}장 → {CardData.RarityRich(CardUpgrade.Target(r))} {CardUpgrade.Output(r)}장   <size=16><color=#bbb>({KindsLine(CardUpgrade.Target(r))}, 중복 가능)</color></size>\n");
            sb.Append("· <color=#f98>가공(Seven · StrikerZ) 카드를 넣어도 결과는 일반 카드입니다 — 가공은 사라집니다</color>\n");
            sb.Append("· 덱에서 쓰는 장수와 잠긴 카드는 넣을 수 없습니다\n");
            sb.Append("· 자동 채우기는 덱 최대 장수(3·3·2·1)를 넘는 남는 카드부터, 일반 사본부터 넣습니다");
            return ("마력부여실 — 가공 · 업그레이드", sb.ToString(), null, null);
        }

        (string, string, string, Action) LukonHelp()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<b><color=#ffd77a>판매 목록</color></b>  확률 없음 — 원하는 아이템을 골라 정가로 산다 (오팔)\n");
            string[] where = { "내 카드 뒷면", "홈 '덱 편집' 칸", "빛/어둠 전투 코인을 던질 때", "전투 주사위를 던질 때" };
            int i = 0;
            foreach (var k in Cosmetics.Kinds)
            {
                var items = Cosmetics.OfKind(k).ToList();
                int price = items.Where(c => !c.IsDefault).Select(c => c.price).DefaultIfEmpty(0).Max();
                sb.Append($"· {Cosmetics.KindLabel(k)}  {items.Count}종 (기본 1 + 판매 {items.Count - 1})  <color=#ffd77a>{price:N0} 오팔</color>  <size=16><color=#bbb>— {where[i++]}</color></size>\n");
            }
            sb.Append("\n· 사면 바로 장착 · 종류마다 1개씩 장착 · 기본 아이템은 무료\n");
            sb.Append("· 장착한 꾸미기는 듀얼에서 <b>내 쪽에만</b> 보입니다 (상대는 자기 꾸미기)\n");
            sb.Append("· 속성 테마: 화염 · 물 · 바람 · 대지 · 번개 · 빛 · 어둠 (+ 무지개)");
            return ("루콘의 꾸미기 공방 — 판매 안내", sb.ToString(), null, null);
        }

        // ══════════════════════════ 방 이동 ══════════════════════════

        void BuildShopNav(RectTransform page)
        {
            navL = NavTab(page, true, out navLText);
            navR = NavTab(page, false, out navRText);
        }

        RectTransform NavTab(RectTransform page, bool left, out Text label)
        {
            var bg = UIKit.Img(page, left ? "NavL" : "NavR", new Color(0.05f, 0.03f, 0.08f, 0.82f), GameAssets.Rounded, true);
            var r = bg.rectTransform;
            r.Place(new Vector2(left ? 0 : 1, 0.5f), new Vector2(left ? 4 : -4, 0), new Vector2(46, 216), new Vector2(left ? 0 : 1, 0.5f));   // 세로 글자 6자 + 화살표
            bg.raycastTarget = true;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => GoSection(shopSection + (left ? -1 : 1)));
            var edge = UIKit.Img(r, "Edge", new Color(1f, 0.78f, 0.35f, 0.9f));
            edge.rectTransform.Anchor(left ? 1 : 0, 0.08f, left ? 1 : 0, 0.92f, left ? -3 : 0, 0, left ? 0 : -3, 0);
            label = UIKit.Txt(r, "T", "", 19, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Fill();
            label.lineSpacing = 0.95f;
            return r;
        }

        static string Vertical(string s) => string.Join("\n", s.ToCharArray());

        void RefreshNav()
        {
            string[] names = { "카드상점", "마법부여상점", "악세서리상점" };
            bool l = shopSection > SecRosie, r = shopSection < SecLukon;
            navL.gameObject.SetActive(l);
            navR.gameObject.SetActive(r);
            if (l) navLText.text = "◀\n" + Vertical(names[shopSection - 1]);
            if (r) navRText.text = Vertical(names[shopSection + 1]) + "\n▶";
        }

        /// <summary>그 방으로 가기 (정문 연출 중이면 끝난 뒤에)</summary>
        void GoSection(int sec)
        {
            sec = Mathf.Clamp(sec, SecRosie, SecLukon);
            if (doorPlaying) { pendingSection = sec; return; }
            if (shopMoving) return;
            if (sec == shopSection) { OnArriveSection(sec); return; }
            Voice.Stop();   // 다른 방으로 넘어가면 하던 대사는 끊는다
            StartCoroutine(RoomDoorTo(sec));
        }

        void JumpTo(int sec)
        {
            int from = shopSection;
            shopSection = sec;
            shopStrip.anchoredPosition = new Vector2(-sec * PageW, 0);
            if (from == SecLukon && sec != SecLukon) DiceStage.Stop();
        }

        void OnArriveSection(int sec)
        {
            RefreshNav();
            if (sec == SecLukon)
            {
                if (cosSel == null || cosSel.kind != cosKind) cosSel = Cosmetics.Equipped(cosKind);
                RefreshCosmetics();
                if (!lukonGreetedThisVisit) { lukonGreetedThisVisit = true; StartCoroutine(LukonEnter()); }
            }
            else if (sec == SecRosie)
            {
                if (!rosieGreeted) { rosieGreeted = true; rosieHop = Time.unscaledTime; RosieSay(RosieGreet[rng.Next(RosieGreet.Length)]); }
            }
            else
            {
                RefreshHall();
                if (!parentsGreetedThisVisit) { parentsGreetedThisVisit = true; GreetParents(); }
            }
        }

        // ── 방문(외문): 옆에서 다가와 → 손잡이 딸깍 → 열리며 그 문으로 걸어 들어간다 ──

        RectTransform roomRoot, roomWay, roomLeaf;
        CanvasGroup roomGroup;
        Image roomLight;

        void BuildRoomDoor(RectTransform page)
        {
            roomRoot = UIKit.Rect("RoomDoor", page).Fill();
            roomGroup = roomRoot.gameObject.AddComponent<CanvasGroup>();
            roomRoot.HitArea();      // 이동 중 클릭 막기
            // 문틀 자리 (세로 꽉 차게, 문 비율 3:4). 벽은 이 문틀 바깥으로 뻗은 판들
            roomWay = UIKit.Rect("Doorway", roomRoot);
            roomWay.anchorMin = new Vector2(0.5f, 0.02f); roomWay.anchorMax = new Vector2(0.5f, 0.95f);
            roomWay.pivot = new Vector2(0.5f, 0.45f);
            roomWay.sizeDelta = Vector2.zero;
            var fit = roomWay.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 0.72f;
            var wood = new Color(0.17f, 0.095f, 0.05f, 1f);
            // 벽: 왼쪽 · 오른쪽 · 위 · 아래 (문틀 크기 기준으로 넉넉히)
            foreach (var (n, a) in new[] { ("WallL", new Vector4(-6, -2, 0, 3)), ("WallR", new Vector4(1, -2, 7, 3)), ("WallT", new Vector4(0, 1, 1, 3)), ("WallB", new Vector4(0, -2, 1, 0)) })
            {
                var w = UIKit.Img(roomWay, n, wood);
                w.rectTransform.Anchor(a.x, a.y, a.z, a.w);
                w.raycastTarget = false;
            }
            // 벽 무늬: 세로 판자 줄 + 허리 띠 + 벽등
            for (int i = -9; i <= 10; i++)
            {
                if (i == 0) continue;
                float x = i < 0 ? i * 0.62f : 1 + (i - 1) * 0.62f;
                var line = UIKit.Img(roomWay, "Plank", new Color(0, 0, 0, 0.35f));
                line.rectTransform.Anchor(x, -2, x, 3, -2, 0, -2, 0);
                line.raycastTarget = false;
            }
            foreach (var side in new[] { -1, 1 })
            {
                var band = UIKit.Img(roomWay, "Wainscot", new Color(0.32f, 0.18f, 0.08f, 1f));
                band.rectTransform.Anchor(side < 0 ? -6 : 1, 0.3f, side < 0 ? 0 : 7, 0.33f);
                band.raycastTarget = false;
                var lamp = UIKit.Img(roomWay, "Lamp", new Color(1f, 0.75f, 0.4f, 0.75f), GameAssets.Glow);
                lamp.rectTransform.Anchor(side < 0 ? -0.62f : 1.22f, 0.56f, side < 0 ? -0.22f : 1.62f, 0.86f);
                lamp.raycastTarget = false;
            }
            // 문틀 금테: 양옆 기둥 + 위 들보
            foreach (var a in new[] { new Vector4(0, 0, 0, 1), new Vector4(1, 0, 1, 1) })
            {
                var post = UIKit.Img(roomWay, "Post", new Color(1f, 0.78f, 0.35f, 1f));
                post.rectTransform.Anchor(a.x, a.y, a.z, a.w, -9, 0, -9, -14);
                post.raycastTarget = false;
            }
            var beam = UIKit.Img(roomWay, "Beam", new Color(1f, 0.78f, 0.35f, 1f));
            beam.rectTransform.Anchor(0, 1, 1, 1, -9, -6, -9, -14);
            beam.raycastTarget = false;
            roomLight = UIKit.Img(roomWay, "Light", new Color(1f, 0.85f, 0.55f, 0f), GameAssets.Glow);
            roomLight.rectTransform.Anchor(-0.3f, -0.1f, 1.3f, 1.1f);
            roomLight.raycastTarget = false;
            var leaf = UIKit.Img(roomWay, "Leaf", Color.white, GameAssets.TexSprite("shop_room_door"));
            roomLeaf = leaf.rectTransform;
            roomLeaf.Fill();
            leaf.raycastTarget = false;
            roomRoot.gameObject.SetActive(false);
        }

        IEnumerator RoomDoorTo(int sec)
        {
            shopMoving = true;
            int dir = sec > shopSection ? 1 : -1;
            navL.gameObject.SetActive(false);
            navR.gameObject.SetActive(false);
            roomRoot.gameObject.SetActive(true);
            roomRoot.SetAsLastSibling();
            roomWay.localScale = Vector3.one * 0.82f;
            roomLeaf.pivot = new Vector2(0, 0.5f);                          // 그림의 경첩이 왼쪽
            roomLeaf.anchoredPosition = Vector2.zero;
            roomLeaf.localScale = Vector3.one;
            var leafImg = roomLeaf.GetComponent<Image>();
            leafImg.color = Color.white;
            roomLight.color = new Color(1f, 0.85f, 0.55f, 0f);
            float W = PageW;

            // 1) 옆으로 걸어가 문 앞에 선다: 벽과 문이 가는 쪽에서 미끄러져 들어온다 (지금 방은 반대로 밀려나며 흐려짐)
            Sfx.Play("whoosh", 0.4f);
            for (float t = 0; t < 0.34f; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / 0.34f);
                roomGroup.alpha = Mathf.Clamp01(k * 1.6f);
                roomWay.anchoredPosition = new Vector2(dir * W * 0.45f * (1 - k), Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2)) * 6f);
                roomWay.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, k);
                shopCam.anchoredPosition = new Vector2(-dir * 140f * k, 0);
                yield return null;
            }
            roomGroup.alpha = 1;
            roomWay.anchoredPosition = Vector2.zero;
            roomWay.localScale = Vector3.one;
            // 문 뒤에서 방 바꾸기
            shopCam.anchoredPosition = Vector2.zero;
            JumpTo(sec);
            shopCam.localScale = Vector3.one * 1.12f;

            // 2) 딸깍 → 문이 경첩을 축으로 열리고(가로로 접힘·어두워짐) 빛이 새어 나온다
            Sfx.Play("flip", 0.8f);
            yield return new WaitForSecondsRealtime(0.06f);
            const float dur = 0.72f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur;
                float open = Mathf.Clamp01(k / 0.6f);
                float c = Mathf.Cos(EaseOut(open) * 84f * Mathf.Deg2Rad);
                roomLeaf.localScale = new Vector3(c, 1f + 0.07f * (1 - c), 1f);
                float shade = Mathf.Lerp(1f, 0.4f, 1 - c);
                leafImg.color = new Color(shade, shade, shade, 1f);
                roomLight.color = new Color(1f, 0.85f, 0.55f, Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI) * 0.8f);
                // 3) 열린 문틀로 걸어 들어간다: 문틀이 화면 밖으로 커지고, 새 방은 다가온다
                float walk = Mathf.Clamp01((k - 0.35f) / 0.65f);
                roomWay.localScale = Vector3.one * Mathf.Lerp(1f, 3.4f, EaseIn(walk));
                shopCam.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, EaseOut(walk));
                roomGroup.alpha = walk < 0.75f ? 1f : 1f - (walk - 0.75f) / 0.25f;
                yield return null;
            }
            roomRoot.gameObject.SetActive(false);
            shopCam.localScale = Vector3.one;
            shopMoving = false;
            OnArriveSection(sec);
        }

        // ══════════════════════════ 정문 (처음 입장) ══════════════════════════

        void BuildDoor(RectTransform page)
        {
            doorRoot = UIKit.Rect("Door", page).Fill();
            doorGroup = doorRoot.gameObject.AddComponent<CanvasGroup>();
            doorRoot.HitArea();
            var skip = doorRoot.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(() => doorSkip = true);         // 클릭하면 빨리 감기
            doorLight = UIKit.Img(doorRoot, "Light", new Color(1f, 0.85f, 0.55f, 0f), GameAssets.Glow);
            doorLight.rectTransform.Anchor(0.2f, -0.1f, 0.8f, 1.1f);
            doorLight.raycastTarget = false;
            var tex = GameAssets.Tex("shop_door");
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                Sprite half = null;
                if (tex != null) half = Sprite.Create(tex, new Rect(left ? 0 : tex.width / 2f, 0, tex.width / 2f, tex.height), new Vector2(0.5f, 0.5f), 100f);
                var leaf = UIKit.Img(doorRoot, left ? "LeafL" : "LeafR", half != null ? Color.white : new Color(0.25f, 0.13f, 0.06f), half);
                var r = leaf.rectTransform;
                r.Anchor(left ? 0 : 0.5f, 0, left ? 0.5f : 1, 1);
                r.pivot = new Vector2(left ? 0 : 1, 0.5f);       // 바깥쪽 경첩을 축으로 열린다
                leaf.raycastTarget = false;
                if (left) doorL = r; else doorR = r;
            }
            doorRoot.gameObject.SetActive(false);
        }

        IEnumerator DoorEnter()
        {
            doorPlaying = true;
            doorSkip = false;
            doorRoot.gameObject.SetActive(true);
            doorRoot.SetAsLastSibling();
            doorGroup.alpha = 1;
            doorRoot.localScale = Vector3.one;
            foreach (var l in new[] { doorL, doorR }) { l.localScale = Vector3.one; l.GetComponent<Image>().color = Color.white; }
            doorLight.color = new Color(1f, 0.85f, 0.55f, 0f);
            hallBoard.alpha = 0;
            shopCam.localScale = Vector3.one * 0.86f;
            float Speed() => doorSkip ? 4f : 1f;

            // 1) 문 앞: 살짝 다가서고 손잡이 '딸깍'
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime * Speed())
            {
                doorRoot.localScale = Vector3.one * (1f + 0.03f * (t / 0.35f));
                yield return null;
            }
            Sfx.Play("flip", 0.8f);
            // 2) 양문이 경첩을 축으로 열리며 안쪽 빛이 쏟아지고, 그 사이로 걸어 들어간다
            Sfx.Play("whoosh", 0.5f);
            const float open = 0.85f;
            for (float t = 0; t < open; t += Time.unscaledDeltaTime * Speed())
            {
                float k = t / open;
                float ang = EaseOut(k) * 82f;
                float c = Mathf.Cos(ang * Mathf.Deg2Rad);
                foreach (var l in new[] { doorL, doorR })
                {
                    l.localScale = new Vector3(c, 1f + 0.08f * (1 - c), 1f);
                    float shade = Mathf.Lerp(1f, 0.45f, 1 - c);
                    l.GetComponent<Image>().color = new Color(shade, shade, shade, 1f);
                }
                doorLight.color = new Color(1f, 0.85f, 0.55f, Mathf.Sin(k * Mathf.PI) * 0.85f);
                doorRoot.localScale = Vector3.one * Mathf.Lerp(1.03f, 1.35f, EaseIn(k));
                shopCam.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, EaseOut(k));
                doorGroup.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                yield return null;
            }
            doorRoot.gameObject.SetActive(false);
            shopCam.localScale = Vector3.one;
            // 3) 마력부여실 안내판이 떠오른다
            var br = (RectTransform)hallBoard.transform;
            var basePos = br.anchoredPosition;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime * Speed())
            {
                float k = EaseOut(t / 0.35f);
                hallBoard.alpha = k;
                br.anchoredPosition = basePos + new Vector2(0, (1 - k) * -30);
                yield return null;
            }
            hallBoard.alpha = 1;
            br.anchoredPosition = basePos;
            doorPlaying = false;
            if (pendingSection >= 0 && pendingSection != shopSection) { int p = pendingSection; pendingSection = -1; GoSection(p); }
            else { pendingSection = -1; OnArriveSection(shopSection); }
        }

        static float EaseOut(float k) => 1 - (1 - k) * (1 - k) * (1 - k);
        static float EaseIn(float k) => k * k * k;

        /// <summary>상점 탭에 들어올 때마다: 정문 앞에서부터</summary>
        void OnEnterShop()
        {
            StopShopRoutines();
            rosieGreeted = false;
            lukonGreetedThisVisit = parentsGreetedThisVisit = false;
            pendingSection = -1;
            shopMoving = false;
            JumpTo(SecHall);
            // 루콘은 공방에 도착하면 깡총 등장
            lukonGroup.alpha = 0;
            cosPreviewGroup.alpha = cosListGroup.alpha = 0;
            lukonLine.text = "";
            parentLine.text = "";
            navL.gameObject.SetActive(false);
            navR.gameObject.SetActive(false);
            RefreshHall();
            StartCoroutine(DoorEnter());
        }

        void StopShopRoutines()
        {
            doorPlaying = false;
            workshopBusy = false;
            if (doorRoot != null) doorRoot.gameObject.SetActive(false);
            if (roomRoot != null) roomRoot.gameObject.SetActive(false);
            if (shopCam != null) { shopCam.localScale = Vector3.one; shopCam.anchoredPosition = Vector2.zero; }
        }

        /// <summary>매 프레임: 화면 크기가 바뀌어도 방 위치 유지 · 키보드 ←/→ · 홀 상태 갱신</summary>
        void UpdateShopHall()
        {
            if (shopPage == null || !shopPage.gameObject.activeInHierarchy || !shopStrip.gameObject.activeInHierarchy) return;
            if (!shopMoving && !doorPlaying) shopStrip.anchoredPosition = new Vector2(-shopSection * PageW, 0);
            if (shopMoving || doorPlaying) return;
            if (helpRoot != null && helpRoot.gameObject.activeSelf) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) GoSection(shopSection - 1);
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) GoSection(shopSection + 1);
            }
            if (shopSection == SecHall && Time.frameCount % 30 == 0) RefreshHall();
        }

        // ── 테스트 ──
        public void TestGoSection(int sec) => GoSection(sec);
        public int TestShopSection => shopSection;
        public bool TestShopBusy => shopMoving || doorPlaying || workshopBusy;
        public void TestHelp(int sec)
        {
            var c = sec == SecRosie ? RosieHelp() : sec == SecLukon ? LukonHelp() : ParentsHelp();
            ShowHelp(c.Item1, c.Item2, c.Item3, c.Item4);
        }
    }

    /// <summary>상점 스와이프: 시작 · 가로 이동량(캔버스 단위) · 끝</summary>
    public class ShopDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Func<bool> begin;
        public Action<float> move;
        public Action end;
        Canvas canvas;
        public void OnBeginDrag(PointerEventData e)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>().rootCanvas;
            begin?.Invoke();
        }
        public void OnDrag(PointerEventData e) => move?.Invoke(e.delta.x / (canvas != null ? canvas.scaleFactor : 1f));
        public void OnEndDrag(PointerEventData e) => end?.Invoke();
    }
}
