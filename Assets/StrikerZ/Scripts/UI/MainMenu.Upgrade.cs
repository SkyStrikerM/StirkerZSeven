using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 마력부여실 › 카드 업그레이드 화면 (규칙은 Meta/CardUpgrade).
    /// 왼쪽 = 등급 선택 · 투입 77장 게이지 · 자동 채우기 · 업그레이드, 오른쪽 = 넣을 카드 고르기 (클릭 +1 · 우클릭 -1).
    /// </summary>
    public partial class MainMenu
    {
        RectTransform upgradeView, upgradeGrid, upGaugeFill;
        Text upCount, upInfo, upPickedText;
        UIKit.Btn upRun;
        readonly Dictionary<Rarity, UIKit.Btn> upTierBtns = new Dictionary<Rarity, UIKit.Btn>();
        readonly Dictionary<(string id, CardFinish f), int> upPicks = new Dictionary<(string id, CardFinish f), int>();
        Rarity upFrom = Rarity.Common;
        RectTransform upResultRoot;

        void BuildUpgradeView(RectTransform page)
        {
            upgradeView = UIKit.Rect("UpgradeView", page).Fill();
            var left = UIKit.Frame(upgradeView, "Furnace", new Color(0.06f, 0.03f, 0.09f, 0.95f));
            left.rectTransform.Anchor(0, 0, 0.36f, 1, 0, 0, 8, 0);
            var back = UIKit.Button(left.transform, "◀ 마력부여실", () => { RefreshShop(); ParentsSay((false, "다 골랐니? 언제든 다시 오렴!")); }, Dark, 20);
            back.rt.Place(new Vector2(0, 1), new Vector2(16, -16), new Vector2(190, 52), new Vector2(0, 1));
            var title = UIKit.Txt(left.transform, "T", "카드 업그레이드", 32, new Color(1f, 0.85f, 0.55f), TextAnchor.UpperRight, FontStyle.Bold);
            title.rectTransform.Anchor(0, 1, 1, 1, 20, -64, 66, 16);
            HelpIcon(left.rectTransform, ParentsHelp).Place(new Vector2(1, 1), new Vector2(-14, -20), new Vector2(40, 40), new Vector2(1, 1));

            // 등급 선택
            for (int i = 0; i < CardUpgrade.Sources.Length; i++)
            {
                var r = CardUpgrade.Sources[i];
                var b = UIKit.Button(left.transform, $"{CardData.RarityLabel(r)} → {CardData.RarityLabel(CardUpgrade.Target(r))}", () => { upFrom = r; upPicks.Clear(); RefreshUpgrade(); }, Dark, 18);
                b.rt.Anchor(0.05f, 0.82f - i * 0.075f, 0.95f, 0.885f - i * 0.075f);
                upTierBtns[r] = b;
            }
            // 투입 게이지
            upCount = UIKit.Txt(left.transform, "Count", "", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            upCount.rectTransform.Anchor(0, 0.5f, 1, 0.6f);
            var gauge = UIKit.Img(left.transform, "Gauge", new Color(1, 1, 1, 0.08f), GameAssets.Rounded, true).rectTransform;
            gauge.Anchor(0.08f, 0.465f, 0.92f, 0.495f);
            upGaugeFill = UIKit.Img(gauge, "Fill", new Color(1f, 0.75f, 0.3f, 0.95f), GameAssets.Rounded, true).rectTransform;
            upGaugeFill.Anchor(0, 0, 0, 1);
            upInfo = UIKit.Txt(left.transform, "Info", "", 18, UIKit.Ink, TextAnchor.UpperCenter);
            upInfo.rectTransform.Anchor(0, 0.25f, 1, 0.455f, 18, 0, 18, 6);
            upInfo.lineSpacing = 1.15f;
            var auto = UIKit.Button(left.transform, "자동 채우기\n<size=14>남는 카드부터</size>", AutoFillUpgrade, Dark, 18);
            auto.rt.Anchor(0.05f, 0.155f, 0.49f, 0.24f);
            var clear = UIKit.Button(left.transform, "비우기", () => { upPicks.Clear(); RefreshUpgrade(); }, Dark, 18);
            clear.rt.Anchor(0.51f, 0.155f, 0.95f, 0.24f);
            upRun = UIKit.Button(left.transform, "업그레이드!", RunUpgrade, new Color(0.62f, 0.38f, 0.08f, 0.97f), 24, Color.white);
            upRun.rt.Anchor(0.05f, 0.025f, 0.95f, 0.135f);

            var right = UIKit.Frame(upgradeView, "Cards", Dark);
            right.rectTransform.Anchor(0.36f, 0, 1, 1, 8, 0, 0, 0);
            var rt = UIKit.Txt(right.transform, "T", "넣을 카드를 고르세요   <size=17><color=#bbb>클릭 +1 · 우클릭 -1 · 덱에서 쓰는 장수와 잠긴 카드는 제외</color></size>", 23, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            rt.rectTransform.Anchor(0, 1, 1, 1, 22, -54, 20, 10);
            upPickedText = UIKit.Txt(right.transform, "Picked", "", 17, UIKit.Ink, TextAnchor.MiddleLeft);
            upPickedText.rectTransform.Anchor(0, 1, 1, 1, 22, -88, 20, 54);
            upgradeGrid = UIKit.ScrollGrid(right.transform, "Grid", new Vector2(112, 157), new Vector2(12, 16), out _);
            upgradeGrid.parent.GetComponent<RectTransform>().Anchor(0, 0, 1, 1, 12, 12, 12, 96);
            upgradeView.gameObject.SetActive(false);
        }

        void OpenUpgrade()
        {
            shopCam.gameObject.SetActive(false);
            navL.gameObject.SetActive(false);
            navR.gameObject.SetActive(false);
            enchantView.gameObject.SetActive(false);
            upgradeView.gameObject.SetActive(true);
            upPicks.Clear();
            RefreshUpgrade();
        }

        int UpTotal => upPicks.Values.Sum();

        void RefreshUpgrade()
        {
            foreach (var kv in upTierBtns) kv.Value.SetSelected(kv.Key == upFrom, new Color(0.62f, 0.38f, 0.08f, 0.97f));
            var to = CardUpgrade.Target(upFrom);
            int total = UpTotal, outN = CardUpgrade.Output(upFrom), kinds = CardUpgrade.Pool(to).Count;
            upCount.text = $"<color=#{CardData.RarityHex(upFrom)}>{total}</color> <size=26><color=#999>/ {CardUpgrade.Input}</color></size>";
            upGaugeFill.anchorMax = new Vector2(Mathf.Clamp01(total / (float)CardUpgrade.Input), 1);
            int seven = upPicks.Where(kv => kv.Key.f == CardFinish.Seven).Sum(kv => kv.Value), sz = upPicks.Where(kv => kv.Key.f == CardFinish.StrikerZ).Sum(kv => kv.Value);
            upInfo.text = $"{CardData.RarityRich(upFrom)} {CardUpgrade.Input}장 → {CardData.RarityRich(to)} <b>{outN}장</b> (무작위)\n" +
                          $"<size=15><color=#bbb>{CardData.RarityLabel(to)} 수록 {kinds}종 중 1종당 {BoosterPack.Pct(1.0 / Mathf.Max(1, kinds))} · 중복 가능</color></size>\n" +
                          "<color=#f98>가공 카드를 넣어도 결과는 일반 카드입니다</color>" +
                          (seven + sz > 0 ? $"\n<size=15>넣은 가공 카드  {FinishStyle.Rich(CardFinish.Seven, $" {seven}")}  {FinishStyle.Rich(CardFinish.StrikerZ, $" {sz}")}</size>" : "");
            upRun.Interactable = total == CardUpgrade.Input;
            upPickedText.text = $"넣은 카드 {upPicks.Count(kv => kv.Value > 0)}종 {total}장   ·   보유 {CardData.RarityLabel(upFrom)} 남는 카드 {Crafting.Surplus().Where(s => s.card.rarity == upFrom).Sum(s => s.n)}장";

            UIKit.Clear(upgradeGrid);
            foreach (var c in CardDB.All.Where(c => c.rarity == upFrom && c.type != CardType.Hero && Profile.Owned(c.id) > 0).OrderBy(c => c.id))
            {
                var cd = c;
                int spare = CardUpgrade.Spare(c);
                var cell = CardCell(upgradeGrid, c, 112, 157, 0.15f, () => UpgradeAdd(cd), null, () => UpgradeRemove(cd));
                int picked = upPicks.Where(kv => kv.Key.id == c.id).Sum(kv => kv.Value);
                Badge(cell, picked > 0 ? $"<color=#ffd77a>+{picked}</color> / {spare}" : $"{spare}", new Color(0, 0, 0, 0.78f), new Vector4(0.08f, 0f, 0.92f, 0.14f), 16);
                if (picked > 0)
                {
                    var hl = UIKit.Img(cell, "Sel", new Color(1f, 0.75f, 0.3f, 1f), GameAssets.RoundedOutline, true);
                    hl.rectTransform.Anchor(-0.04f, -0.03f, 1.04f, 1.03f);
                }
                if (spare == 0)
                {
                    var dim = UIKit.Img(cell, "Dim", new Color(0.02f, 0.02f, 0.04f, 0.78f), GameAssets.Rounded, true);
                    dim.rectTransform.Fill();
                    dim.raycastTarget = false;
                }
            }
        }

        void UpgradeAdd(CardData c)
        {
            if (UpTotal >= CardUpgrade.Input) { Toast($"이미 {CardUpgrade.Input}장을 넣었습니다"); return; }
            var f = CardUpgrade.NextFinish(c, upPicks);
            if (f == null) { Toast("더 넣을 수 있는 사본이 없습니다 (덱 사용분·잠금 제외)"); return; }
            upPicks[(c.id, f.Value)] = CardUpgrade.Picked(upPicks, c.id, f.Value) + 1;
            if (f.Value != CardFinish.Normal) Toast($"{FinishStyle.Label(f.Value)} 가공 사본을 넣었습니다 — 결과는 일반 카드입니다");
            Sfx.Play("click", 0.5f);
            RefreshUpgrade();
        }

        void UpgradeRemove(CardData c)
        {
            var f = CardUpgrade.LastFinish(c, upPicks);
            if (f == null) return;
            int n = CardUpgrade.Picked(upPicks, c.id, f.Value) - 1;
            if (n <= 0) upPicks.Remove((c.id, f.Value)); else upPicks[(c.id, f.Value)] = n;
            RefreshUpgrade();
        }

        void AutoFillUpgrade()
        {
            int n = CardUpgrade.AutoFill(upFrom, upPicks);
            if (n < CardUpgrade.Input) Toast($"남는 카드는 {n}장이라 {CardUpgrade.Input - n}장이 모자랍니다 — 직접 골라 넣어 주세요");
            Sfx.Play("drop", 0.5f);
            RefreshUpgrade();
        }

        void RunUpgrade()
        {
            if (!CardUpgrade.Run(upFrom, upPicks, rng, out var result, out var why)) { Toast(why); return; }
            upPicks.Clear();
            Quests.Add(Stat.Crafted);
            RefreshTop();
            RefreshUpgrade();
            // 결과 카드는 출석·세븐 패스 카드 보상과 같은 "뒤집어 공개" 연출 (2026-10-29)
            RevealCards("카드 업그레이드 결과", result.Select(r => (r.card, r.isNew, CardFinish.Normal)).ToList());
        }

        /// <summary>결과: 카드가 한 장씩 빛과 함께 나타난다. 클릭하면 확대</summary>
        IEnumerator ShowUpgradeResult(List<(CardData card, bool isNew)> result)
        {
            if (upResultRoot == null)
            {
                var bg = UIKit.Img(root, "UpgradeResult", new Color(0, 0, 0, 0.96f));
                bg.raycastTarget = true;
                upResultRoot = bg.rectTransform;
                upResultRoot.Fill();
                OverlayCanvas(upResultRoot, 41);
            }
            UIKit.Clear(upResultRoot);
            upResultRoot.gameObject.SetActive(true);
            var title = UIKit.Txt(upResultRoot, "T", "업그레이드 완료!", 48, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(0, 0.82f, 1, 0.95f);
            title.Outline(new Color(0.3f, 0.1f, 0, 0.9f), 3);
            int n = result.Count;
            float w = n > 1 ? 190 : 260, h = w * 1.4f, gap = 24;
            var row = UIKit.Rect("Row", upResultRoot);
            row.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(n * w + (n - 1) * gap, h));
            var cells = new List<RectTransform>();
            for (int i = 0; i < n; i++)
            {
                var (c, isNew) = result[i];
                var holder = UIKit.Rect("Card" + i, row);
                holder.Place(new Vector2(0, 0.5f), new Vector2(i * (w + gap) + w / 2, 0), new Vector2(w, h));
                var glow = UIKit.Img(holder, "Glow", CardData.RarityColor(c.rarity) * new Color(1, 1, 1, 0.6f), GameAssets.Glow);
                glow.rectTransform.Anchor(-0.3f, -0.2f, 1.3f, 1.2f);
                var face = CardFace.Build(holder, w, h, 0.2f * w / 150f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(c);
                holder.HitArea();
                var cd = c;
                holder.gameObject.AddComponent<CellEvents>().onLeftClick = () => CardZoom.Show(cd, CardFinish.Normal);
                if (isNew) Badge(holder, "NEW", new Color(0.85f, 0.2f, 0.25f, 0.95f), new Vector4(0.6f, 0.9f, 1.02f, 1.02f), 18);
                holder.localScale = Vector3.zero;
                cells.Add(holder);
            }
            var ok = UIKit.Button(upResultRoot, "확인", () => upResultRoot.gameObject.SetActive(false), Dark, 24);
            ok.rt.Place(new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(240, 64));
            ok.bg.gameObject.SetActive(false);
            ParentsSay((false, n > 1 ? "반짝반짝 새 카드들이야. 마음에 드는 게 있니?" : "Unique 카드야! 77장의 마력이 이 한 장에 모였단다."));
            for (int i = 0; i < n; i++)
            {
                Sfx.Play(result[i].card.rarity >= Rarity.Rare ? "reveal3" : "reveal1", 0.8f);
                var cell = cells[i];
                for (float t = 0; t < 0.28f; t += Time.unscaledDeltaTime)
                {
                    float k = t / 0.28f;
                    cell.localScale = Vector3.one * (k < 0.7f ? Mathf.Lerp(0.2f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f));
                    yield return null;
                }
                cell.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(n > 1 ? 0.08f : 0.3f);
            }
            ok.bg.gameObject.SetActive(true);
        }

        // ── 테스트 ──
        public void TestOpenUpgrade(Rarity from) { upFrom = from; OpenUpgrade(); }
        public void TestAutoFillUpgrade() => AutoFillUpgrade();
        public void TestRunUpgrade() => RunUpgrade();
    }
}
