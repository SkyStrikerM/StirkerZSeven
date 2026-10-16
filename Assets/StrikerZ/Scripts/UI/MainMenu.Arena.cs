using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>도전 › 원소 투기장 칸 + 드래프트 화면 (영웅 3 중 1 → 카드 3 중 1 × 30)</summary>
    public partial class MainMenu
    {
        public Action onArenaMatch;
        Text arenaState, arenaDeckInfo;
        UIKit.Btn arenaMain, arenaSub;
        RectTransform draftRoot, draftRow;
        Text draftTitle, draftSide;

        partial void BuildArenaPanel(RectTransform col)
        {
            var d = UIKit.Txt(col, "Desc", $"오팔 {Arena.EntryOpal}으로 입장 · 영웅 3명 중 1명, 카드 3장 중 1장씩 30번 골라 즉석 덱을 만든다.\n{Arena.MaxWins}승 또는 {Arena.MaxLosses}패까지 · 승수만큼 보상 · 고른 카드는 남지 않는다", 17, UIKit.Ink, TextAnchor.UpperLeft);
            d.rectTransform.Anchor(0, 1, 1, 1, 22, -170, 18, 70);
            d.lineSpacing = 1.15f;
            arenaState = UIKit.Txt(col, "State", "", 26, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            arenaState.rectTransform.Anchor(0, 1, 1, 1, 22, -250, 18, 180);
            arenaDeckInfo = UIKit.Txt(col, "Info", "", 18, UIKit.Muted, TextAnchor.UpperLeft);
            arenaDeckInfo.rectTransform.Anchor(0, 0, 1, 1, 22, 240, 18, 256);
            arenaDeckInfo.lineSpacing = 1.2f;
            arenaMain = UIKit.Button(col, "", ArenaMainAction, Red, 28, Color.white);
            arenaMain.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(340, 88), new Vector2(0.5f, 0));
            arenaSub = UIKit.Button(col, "포기하고 보상 받기", () =>
            {
                if (!Arena.Ready) return;
                Toast(Arena.Finish());
                RefreshChallenge();
                RefreshTop();
            }, Dark, 20, UIKit.Muted);
            arenaSub.rt.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(300, 60), new Vector2(0.5f, 0));
        }

        partial void RefreshArenaPanel()
        {
            var s = Arena.S;
            if (s.pendingPacks > 0)
            {
                int n = s.pendingPacks;
                s.pendingPacks = 0;
                Profile.Save();
                OpenPacks(BoosterPack.Latest, n);
            }
            string table = string.Join(" · ", Enumerable.Range(0, Arena.RewardOpal.Length).Select(w => $"{w}승 {Arena.RewardOpal[w]}" + (Arena.Packs(w) > 0 && (w == 3 || w == 6) ? $"+팩{Arena.Packs(w)}" : "")));
            if (!Arena.Active)
            {
                arenaState.text = "입장 대기";
                arenaDeckInfo.text = "보상 (오팔)\n" + table;
                arenaMain.label.text = $"입장 · 오팔 {Arena.EntryOpal}";
                arenaSub.bg.gameObject.SetActive(false);
            }
            else if (Arena.Drafting)
            {
                arenaState.text = Arena.PickingHero ? "드래프트 — 영웅 선택" : $"드래프트 {s.deck.Count} / {Arena.DeckSize}";
                arenaDeckInfo.text = "보상 (오팔)\n" + table;
                arenaMain.label.text = "드래프트 계속";
                arenaSub.bg.gameObject.SetActive(false);
            }
            else
            {
                var cards = Arena.DeckCards;
                arenaState.text = $"<color=#9f9>{s.wins}승</color>  <color=#f99>{s.losses}패</color>   — 영웅 {CardDB.Get(s.hero)?.name}";
                arenaDeckInfo.text = $"덱: 소환수 {cards.Count(c => c.type == CardType.Monster)} · 마법 {cards.Count(c => c.type == CardType.Magic)} · 필드 {cards.Count(c => c.type == CardType.Field)}" +
                                     $"\n지금 끝내면 오팔 {Arena.RewardOpal[Math.Min(s.wins, Arena.MaxWins)]}{(Arena.Packs(s.wins) > 0 ? $" + 팩 {Arena.Packs(s.wins)}" : "")}\n\n보상 (오팔)\n" + table;
                arenaMain.label.text = "다음 대전 ▶";
                arenaSub.bg.gameObject.SetActive(true);
            }
        }

        void ArenaMainAction()
        {
            if (!Arena.Active)
            {
                if (!Arena.Enter(out string msg)) { Toast(msg); return; }
                RefreshTop();
                OpenDraft();
            }
            else if (Arena.Drafting) OpenDraft();
            else onArenaMatch?.Invoke();
            RefreshChallenge();
        }

        // ── 드래프트 화면 ──

        void OpenDraft()
        {
            if (draftRoot == null) BuildDraft();
            RefreshDraft();
            draftRoot.SetAsLastSibling();
            draftRoot.gameObject.SetActive(true);
        }

        public void TestOpenDraft() => OpenDraft();

        void BuildDraft()
        {
            var bg = UIKit.Img(root, "Draft", new Color(0.01f, 0.01f, 0.03f, 1f));   // 리니어 색 공간: 반투명이면 뒤 메뉴가 비친다
            bg.raycastTarget = true;
            draftRoot = bg.rectTransform;
            draftRoot.Fill();
            OverlayCanvas(draftRoot, 41);
            draftTitle = UIKit.Txt(draftRoot, "Title", "", 46, new Color(0.55f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            draftTitle.rectTransform.Anchor(0, 0.86f, 1, 0.96f);
            var sub = UIKit.Txt(draftRoot, "Sub", "한 장을 고르세요 · 우클릭: 확대", 22, UIKit.Ink, TextAnchor.MiddleCenter);
            sub.rectTransform.Anchor(0, 0.81f, 1, 0.86f);
            draftRow = UIKit.Rect("Row", draftRoot).Anchor(0.03f, 0.2f, 0.78f, 0.8f);
            var hl = draftRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 40; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            var side = UIKit.Img(draftRoot, "Side", new Color(0.04f, 0.03f, 0.08f, 0.95f), GameAssets.Rounded, true).rectTransform;
            side.Anchor(0.8f, 0.2f, 0.97f, 0.8f);
            draftSide = UIKit.Txt(side, "T", "", 20, UIKit.Ink, TextAnchor.UpperLeft);
            draftSide.rectTransform.Anchor(0, 0, 1, 1, 18, 16, 16, 16);
            draftSide.lineSpacing = 1.2f;
            var close = UIKit.Button(draftRoot, "나중에 계속", () => { draftRoot.gameObject.SetActive(false); RefreshChallenge(); }, Dark, 22);
            close.rt.Place(new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(280, 64));
            draftRoot.gameObject.SetActive(false);
        }

        void RefreshDraft()
        {
            var s = Arena.S;
            UIKit.Clear(draftRow);
            if (!Arena.Drafting)
            {
                draftRoot.gameObject.SetActive(false);
                Toast("드래프트 완료! 도전 › 원소 투기장에서 대전을 시작하세요");
                RefreshChallenge();
                return;
            }
            bool hero = Arena.PickingHero;
            draftTitle.text = hero ? "원소 투기장 — 영웅 선택" : $"원소 투기장 — 드래프트 {s.deck.Count + 1} / {Arena.DeckSize}";
            var ids = hero ? s.heroChoices : s.choices;
            foreach (var id in ids)
            {
                var d = CardDB.Get(id);
                if (d == null) continue;
                string cid = id;
                var cell = UIKit.Rect("C_" + id, draftRow);
                cell.sizeDelta = new Vector2(330, 462);
                cell.HitArea();
                var face = CardFace.Build(cell, 330, 462, 0.46f);
                face.root.Fill();
                face.glow.gameObject.SetActive(false);
                face.Set(d);
                var ev = cell.gameObject.AddComponent<CellEvents>();
                ev.onLeftClick = () =>
                {
                    if (hero) Arena.PickHero(cid); else Arena.Pick(cid);
                    Sfx.Play("reveal1", 0.7f);
                    RefreshDraft();
                };
                ev.onRightClick = () => CardZoom.Show(d, CardFinish.Normal);
            }
            var cards = Arena.DeckCards;
            var h = CardDB.Get(s.hero);
            string curve = string.Join("  ", Enumerable.Range(1, 7).Select(t => $"T{t}:{cards.Count(c => c.type == CardType.Monster && c.minTime == t)}"));
            draftSide.text = $"<color=#ffd77a><b>지금 덱</b></color>\n영웅  {(h != null ? h.name : "-")}\n소환수 {cards.Count(c => c.type == CardType.Monster)} / 20\n마법 {cards.Count(c => c.type == CardType.Magic)} / 7\n필드 {cards.Count(c => c.type == CardType.Field)} / 3\n\n<color=#ffd77a><b>소환 T 곡선</b></color>\n{curve}\n\n<color=#ffd77a><b>최근 고른 카드</b></color>\n" +
                             string.Join("\n", cards.AsEnumerable().Reverse().Take(8).Select(c => c.name));
        }
    }
}
