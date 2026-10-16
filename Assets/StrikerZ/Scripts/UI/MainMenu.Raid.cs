using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>레이드 로비: 보스 7종 선택 · 파티 인원(1~3) · 덱(내 덱 / 추천 대여 덱) · 시작</summary>
    public partial class MainMenu
    {
        /// <summary>(보스, 파티 인원, 추천 대여 덱 사용)</summary>
        public Action<RaidBoss, int, bool> onRaid;

        RaidBoss raidSel;
        int raidParty = 1;
        bool raidRental = true;
        readonly List<(RaidBoss boss, UIKit.Btn btn)> raidBossBtns = new List<(RaidBoss, UIKit.Btn)>();
        readonly List<UIKit.Btn> raidPartyBtns = new List<UIKit.Btn>();
        UIKit.Btn raidDeckMine, raidDeckRental, raidStartBtn;
        Image raidArt;
        Text raidName, raidTags, raidLore, raidTrait, raidOpalCount;
        readonly List<CardFace> raidSkillFaces = new List<CardFace>();

        RectTransform BuildRaid()
        {
            var page = UIKit.Rect("RaidPage", contentArea).Fill();

            // 왼쪽: 보스 목록
            var list = UIKit.Frame(page, "BossList", Dark);
            list.rectTransform.Anchor(0, 0, 0, 1, 0, 0, -380, 0);
            var lt = UIKit.Txt(list.transform, "T", "속성 레이드", 32, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            lt.rectTransform.Anchor(0, 1, 1, 1, 24, -62, 14, 18);
            lt.Section(UIKit.SecPlay);
            for (int i = 0; i < RaidData.Bosses.Count; i++)
            {
                var b = RaidData.Bosses[i];
                var ec = CardData.ElementColor(b.element);
                var btn = UIKit.Button(list.transform, "", () => SelectRaid(b), new Color(ec.r * 0.25f, ec.g * 0.25f, ec.b * 0.25f, 0.95f), 22, Color.white);
                btn.rt.Anchor(0, 1, 1, 1, 16, -84 - (i + 1) * 112 + 8, 16, 84 + i * 112);
                btn.label.alignment = TextAnchor.MiddleLeft;
                btn.label.rectTransform.Anchor(0, 0, 1, 1, 96, 4, 8, 4);
                var icon = UIKit.Img(btn.bg.transform, "Art", Color.white, GameAssets.ArtFor(b.Hero));
                icon.preserveAspect = true;
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(76, 92), new Vector2(0, 0.5f));
                raidBossBtns.Add((b, btn));
            }

            // 오른쪽: 상세
            var det = UIKit.Frame(page, "Detail", Dark);
            det.rectTransform.Anchor(0, 0, 1, 1, 396, 0, 0, 0);
            var artFrame = UIKit.Img(det.transform, "ArtFrame", new Color(0, 0, 0, 0.5f), GameAssets.Rounded, true);
            artFrame.rectTransform.Anchor(0, 0, 0, 1, 24, 24, -470, 24);
            raidArt = UIKit.Img(artFrame.transform, "Art", Color.white);
            raidArt.preserveAspect = true;
            raidArt.rectTransform.Anchor(0, 0, 1, 1, 8, 8, 8, 8);
            CardZoom.Bind(artFrame.rectTransform, () => raidSel?.Hero);     // 보스 일러스트 클릭 = 보스 영웅 카드 확대

            var info = UIKit.Rect("Info", det.transform).Anchor(0, 0, 1, 1, 500, 24, 24, 24);
            raidName = UIKit.Txt(info, "Name", "", 40, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            raidName.rectTransform.Anchor(0, 1, 1, 1, 0, -100, 0, 0);
            raidName.Shadow();
            raidTags = UIKit.Txt(info, "Tags", "", 19, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleLeft);
            raidTags.rectTransform.Anchor(0, 1, 1, 1, 0, -134, 0, 104);
            raidLore = UIKit.Txt(info, "Lore", "", 19, new Color(0.86f, 0.84f, 0.9f), TextAnchor.UpperLeft, FontStyle.Italic);
            raidLore.rectTransform.Anchor(0, 1, 1, 1, 0, -236, 0, 142);
            raidLore.lineSpacing = 1.1f;
            raidTrait = UIKit.Txt(info, "Trait", "", 18, UIKit.Ink, TextAnchor.UpperLeft);
            raidTrait.rectTransform.Anchor(0, 1, 1, 1, 0, -268, 0, 240);

            // 보스 스킬 카드
            var sl = UIKit.Txt(info, "SkillLabel", "보스 스킬", 21, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleLeft, FontStyle.Bold);
            sl.rectTransform.Anchor(0, 1, 1, 1, 0, -300, 0, 274);
            for (int i = 0; i < 4; i++)
            {
                var holder = UIKit.Rect("Skill" + i, info);
                holder.Place(new Vector2(0, 1), new Vector2(i * 112, -304), new Vector2(102, 143), new Vector2(0, 1));
                var f = CardFace.Build(holder, 102, 143, 0.5f);
                f.root.anchoredPosition = Vector2.zero;
                raidSkillFaces.Add(f);
                int idx = i;
                CardZoom.Bind(holder, () => raidSel != null && idx < raidSel.skills.Count ? raidSel.skills[idx].card : null);
            }

            // 전리품
            var ll = UIKit.Txt(info, "LootLabel", "전리품", 21, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            ll.rectTransform.Anchor(0, 1, 1, 1, 0, -480, 0, 454);
            var lootBg = UIKit.Img(info, "LootBg", new Color(0, 0, 0, 0.35f), GameAssets.Rounded, true);
            lootBg.rectTransform.Anchor(0, 1, 1, 1, 0, -606, 0, 486);
            BuildLootSlot(lootBg.transform, 0, null);
            for (int i = 0; i < RaidData.LootRarities.Length; i++) BuildLootSlot(lootBg.transform, i + 1, RaidData.LootRarities[i]);

            // 파티 인원
            var pl = UIKit.Txt(info, "PartyLabel", "파티 인원", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            pl.rectTransform.Anchor(0, 0, 0.5f, 0, 0, 246, 0, -280);
            for (int i = 1; i <= RaidData.MaxParty; i++)
            {
                int n = i;
                var b = UIKit.Button(info, i == 1 ? "1인 (솔로)" : $"{i}인 (AI 팀원 {i - 1})", () => { raidParty = n; RefreshRaid(); }, Dark, 20);
                b.rt.Anchor(0, 0, 0, 0, (i - 1) * 216, 188, -(i - 1) * 216 - 206, -240);
                raidPartyBtns.Add(b);
            }
            var dl = UIKit.Txt(info, "DeckLabel", "덱", 22, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            dl.rectTransform.Anchor(0, 0, 0.5f, 0, 0, 128, 0, -162);
            raidDeckRental = UIKit.Button(info, "추천 대여 덱", () => { raidRental = true; RefreshRaid(); }, Dark, 20);
            raidDeckRental.rt.Anchor(0, 0, 0, 0, 0, 64, -206, -120);
            raidDeckMine = UIKit.Button(info, "내 사용 덱", () => { raidRental = false; RefreshRaid(); }, Dark, 20);
            raidDeckMine.rt.Anchor(0, 0, 0, 0, 216, 64, -422, -120);
            raidStartBtn = UIKit.Button(info, "레이드 시작", StartRaidClicked, Red, 32, Color.white);
            raidStartBtn.rt.Anchor(1, 0, 1, 0, -300, 64, 0, -170);

            raidSel = RaidData.Bosses[0];
            return page;
        }

        /// <summary>전리품 칸: 0번 = 오팔, 나머지 = 등급별 ? 카드 (Textures/Raid/loot_{등급}.png 가 있으면 그 이미지)</summary>
        void BuildLootSlot(Transform parent, int index, Rarity? rarity)
        {
            const float w = 96, h = 108, gap = 20;
            var root = UIKit.Rect("Loot" + index, parent);
            root.Place(new Vector2(0, 0.5f), new Vector2(18 + index * (w + gap), 0), new Vector2(w, h), new Vector2(0, 0.5f));
            var label = UIKit.Txt(root, "Label", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Anchor(0, 0, 1, 0.2f, -10, 0, -10, 0);
            var slot = UIKit.Rect("Icon", root).Anchor(0, 0.22f, 1, 1);
            label.Outline(new Color(0, 0, 0, 0.9f));
            if (rarity == null)
            {
                var gem = UIKit.Img(slot, "Opal", Color.white, GameAssets.TexSprite("opal"));
                gem.preserveAspect = true;
                gem.rectTransform.Anchor(0.05f, 0.12f, 0.95f, 0.98f);
                raidOpalCount = label;
                return;
            }
            var r = rarity.Value;
            var rc = CardData.RarityColor(r);
            label.text = r.ToString();
            label.color = Color.Lerp(rc, Color.white, 0.25f);
            var icon = GameAssets.TexSprite("Raid/loot_" + r.ToString().ToLower());
            if (icon != null)
            {
                var img = UIKit.Img(slot, "Icon", Color.white, icon);
                img.preserveAspect = true;
                img.rectTransform.Fill();
                return;
            }
            // 이미지가 없을 때: 등급 색 테두리 + 물음표 카드
            var glow = UIKit.Img(slot, "Glow", new Color(rc.r, rc.g, rc.b, 0.35f), GameAssets.Glow);
            glow.rectTransform.Anchor(-0.25f, -0.2f, 1.25f, 1.2f);
            var frame = UIKit.Img(slot, "Frame", rc, GameAssets.Rounded, true);
            frame.rectTransform.Anchor(0.19f, 0, 0.81f, 1);
            var inner = UIKit.Img(frame.transform, "Inner", new Color(0.05f, 0.04f, 0.09f, 1), GameAssets.Rounded, true);
            inner.rectTransform.Anchor(0, 0, 1, 1, 4, 4, 4, 4);
            var q = UIKit.Txt(inner.transform, "Q", "?", 50, Color.Lerp(rc, Color.white, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            q.rectTransform.Fill();
            q.Outline(new Color(0, 0, 0, 0.8f), 2f);
        }

        void SelectRaid(RaidBoss b) { raidSel = b; RefreshRaid(); }

        void RefreshRaid()
        {
            if (raidSel == null) raidSel = RaidData.Bosses[0];
            var b = raidSel;
            foreach (var (boss, btn) in raidBossBtns)
            {
                bool on = boss == b;
                var ec = CardData.ElementColor(boss.element);
                btn.baseColor = on ? new Color(ec.r * 0.55f, ec.g * 0.55f, ec.b * 0.55f, 0.98f) : new Color(ec.r * 0.22f, ec.g * 0.22f, ec.b * 0.22f, 0.95f);
                btn.label.text = $"<size=24><b>{boss.name}</b></size>{(Profile.RaidCleared(boss.id) ? "  <color=#8f8>CLEAR</color>" : "")}\n<size=17><color=#ddd>{CardData.ElementName(boss.element)} · {boss.title}</color></size>";
                btn.Interactable = true;
            }
            raidArt.sprite = GameAssets.FullArtFor(b.Hero) ?? GameAssets.ArtFor(b.Hero);
            raidName.text = $"{b.name}\n<size=22><color=#ddd>{CardData.ElementName(b.element)} 속성 레이드 · {b.title}</color></size>";
            raidTags.text = $"서식지 · {b.habitat}     약점 · <color=#9cf>{CardData.ElementName(b.weakness)}</color>{(Profile.RaidCleared(b.id) ? "     <color=#8f8>CLEAR</color>" : "")}";
            raidLore.text = b.lore;
            raidTrait.text = $"<color=#ffd05c>[{b.traitName}]</color> {b.traitText}";
            for (int i = 0; i < raidSkillFaces.Count; i++)
            {
                bool on = i < b.skills.Count;
                raidSkillFaces[i].root.parent.gameObject.SetActive(on);
                if (on) raidSkillFaces[i].Set(b.skills[i].card);
            }
            raidOpalCount.text = "오팔 ×" + RaidData.ClearOpal;
            for (int i = 0; i < raidPartyBtns.Count; i++) raidPartyBtns[i].SetSelected(i + 1 == raidParty);
            raidDeckRental.SetSelected(raidRental);
            raidDeckMine.SetSelected(!raidRental);
            var deck = Profile.ActiveDeck;
            string err = DeckRules.Validate(deck);
            raidStartBtn.Interactable = raidRental || err == null;
        }

        void StartRaidClicked()
        {
            if (raidSel == null) return;
            if (!raidRental && DeckRules.Validate(Profile.ActiveDeck) is string err) { Toast(err); return; }
            onRaid?.Invoke(raidSel, raidParty, raidRental);
        }

        // ── 테스트 ──
        public void TestRaidSelect(int bossIndex, int party, bool rental)
        {
            raidSel = RaidData.Bosses[Mathf.Clamp(bossIndex, 0, RaidData.Bosses.Count - 1)];
            raidParty = party;
            raidRental = rental;
            RefreshRaid();
        }
    }
}
