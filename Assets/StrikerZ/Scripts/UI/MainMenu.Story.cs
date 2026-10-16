using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>스토리 탭: 장 목록 · 장 표지 · 스테이지 경로(7칸) · 스테이지 상세 · 시작/다시 보기</summary>
    public partial class MainMenu
    {
        /// <summary>스테이지 시작 (컷신 → 전투 → 컷신)</summary>
        public Action<StoryStage> onStory;
        /// <summary>클리어한 스테이지의 이야기만 다시 보기</summary>
        public Action<StoryStage> onStoryReplay;

        StoryVolume storyVol;
        StoryStage storySel;
        readonly List<(StoryVolume vol, UIKit.Btn btn)> volBtns = new List<(StoryVolume, UIKit.Btn)>();
        readonly List<(Image ring, Image fill, Text num, Text label, Button btn)> stageNodes = new List<(Image, Image, Text, Text, Button)>();
        readonly List<Image> stageLinks = new List<Image>();
        RawImage coverImg, stageThumb;
        Text volNo, volTitle, volTag, volProgress, stTitle, stSub, stSynopsis, stReward;
        UIKit.Btn stStart, stReplay;
        RectTransform stageDetail;
        const int StoryCardSlots = 4;
        readonly List<(RectTransform holder, CardFace face, Text cap, CardData card)> storyCards = new List<(RectTransform, CardFace, Text, CardData)>();

        RectTransform BuildStory()
        {
            var page = UIKit.Rect("StoryPage", contentArea).Fill();

            // 왼쪽: 장 목록
            var list = UIKit.Frame(page, "Volumes", Dark);
            list.rectTransform.Anchor(0, 0, 0, 1, 0, 0, -330, 0);
            var lt = UIKit.Txt(list.transform, "T", "메인 스토리", 32, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            lt.rectTransform.Anchor(0, 1, 1, 1, 24, -62, 14, 18);
            lt.Section(UIKit.SecPlay);
            for (int i = 0; i < StoryData.Volumes.Count; i++)
            {
                var v = StoryData.Volumes[i];
                var b = UIKit.Button(list.transform, "", () => SelectVolume(v), new Color(0.08f, 0.06f, 0.12f, 0.95f), 20, Color.white);
                b.rt.Anchor(0, 1, 1, 1, 14, -84 - (i + 1) * 74 + 8, 14, 84 + i * 74);
                b.label.alignment = TextAnchor.MiddleLeft;
                b.label.rectTransform.Anchor(0, 0, 1, 1, 18, 2, 8, 2);
                volBtns.Add((v, b));
            }

            // 오른쪽: 표지
            var det = UIKit.Frame(page, "Detail", Dark);
            det.rectTransform.Anchor(0, 0, 1, 1, 346, 0, 0, 0);
            var coverMask = UIKit.Img(det.transform, "CoverMask", Color.white, GameAssets.Rounded, true);
            coverMask.rectTransform.Anchor(0, 1, 1, 1, 14, -344, 14, 14);
            coverMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            coverImg = new GameObject("Cover", typeof(RectTransform)).AddComponent<RawImage>();
            coverImg.transform.SetParent(coverMask.transform, false);
            coverImg.rectTransform.Fill();
            coverImg.raycastTarget = false;
            var shade = UIKit.Img(coverMask.transform, "Shade", new Color(0.02f, 0.01f, 0.05f, 0.6f));
            shade.rectTransform.Anchor(0, 0, 0.62f, 1);
            volNo = UIKit.Txt(coverMask.transform, "No", "", 26, UIKit.Gold, TextAnchor.LowerLeft, FontStyle.Bold);
            volNo.rectTransform.Anchor(0, 0.62f, 0.7f, 0.8f, 34, 0, 0, 0);
            volTitle = UIKit.Txt(coverMask.transform, "Title", "", 60, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            volTitle.rectTransform.Anchor(0, 0.36f, 0.8f, 0.64f, 32, 0, 0, 0);
            volTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            volTitle.Outline(new Color(0, 0, 0, 0.85f), 2.5f);
            volTag = UIKit.Txt(coverMask.transform, "Tag", "", 24, new Color(0.9f, 0.88f, 0.95f), TextAnchor.UpperLeft, FontStyle.Italic);
            volTag.rectTransform.Anchor(0, 0.2f, 0.62f, 0.36f, 36, 0, 0, 0);
            volTag.Shadow();
            volProgress = UIKit.Txt(coverMask.transform, "Progress", "", 22, UIKit.Gold, TextAnchor.LowerLeft, FontStyle.Bold);
            volProgress.rectTransform.Anchor(0, 0, 0.62f, 0.2f, 36, 14, 0, 0);
            volProgress.Shadow();

            // 스테이지 경로
            var path = UIKit.Rect("Path", det.transform).Anchor(0, 1, 1, 1, 60, -476, 60, 356);
            for (int i = 0; i < 7; i++)
            {
                int idx = i;
                float x = i / 6f;
                if (i < 6)
                {
                    var link = UIKit.Img(path, "Link" + i, new Color(1, 1, 1, 0.2f));
                    link.rectTransform.Anchor(x, 0.62f, (i + 1) / 6f, 0.62f, 34, -3, 34, -3);
                    stageLinks.Add(link);
                }
                var ring = UIKit.Img(path, "Node" + i, UIKit.Gold, GameAssets.Circle);
                ring.rectTransform.Anchor(x, 0.62f, x, 0.62f, -34, -34, -34, -34);
                ring.raycastTarget = true;
                var fill = UIKit.Img(ring.transform, "Fill", Dark, GameAssets.Circle);
                fill.rectTransform.Anchor(0, 0, 1, 1, 4, 4, 4, 4);
                var num = UIKit.Txt(ring.transform, "Num", (i + 1).ToString(), 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                num.rectTransform.Fill();
                var label = UIKit.Txt(path, "Label" + i, "", 16, UIKit.Ink, TextAnchor.UpperCenter);
                label.rectTransform.Anchor(x, 0, x, 0, -90, 0, -90, -34);   // 원 아래
                var btn = ring.gameObject.AddComponent<Button>();
                btn.targetGraphic = ring;
                btn.onClick.AddListener(() => SelectStage(idx));
                stageNodes.Add((ring, fill, num, label, btn));
            }

            // 스테이지 상세
            stageDetail = UIKit.Rect("Stage", det.transform).Anchor(0, 0, 1, 1, 24, 24, 24, 496);
            var thumbFrame = UIKit.Img(stageDetail, "ThumbFrame", UIKit.Gold, GameAssets.Rounded, true);
            thumbFrame.rectTransform.Anchor(0, 0, 0, 1, 0, 0, -540, 0);
            var thumbMask = UIKit.Img(thumbFrame.transform, "Mask", Color.white, GameAssets.Rounded, true);
            thumbMask.rectTransform.Anchor(0, 0, 1, 1, 4, 4, 4, 4);
            thumbMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            stageThumb = new GameObject("Thumb", typeof(RectTransform)).AddComponent<RawImage>();
            stageThumb.transform.SetParent(thumbMask.transform, false);
            stageThumb.rectTransform.Fill();
            stageThumb.raycastTarget = false;

            var info = UIKit.Rect("Info", stageDetail).Anchor(0, 0, 1, 1, 572, 0, 0, 0);
            stTitle = UIKit.Txt(info, "Title", "", 40, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            stTitle.rectTransform.Anchor(0, 1, 1, 1, 0, -56, 0, 0);
            stTitle.Shadow();
            stSub = UIKit.Txt(info, "Sub", "", 20, new Color(1f, 0.75f, 0.5f), TextAnchor.UpperLeft, FontStyle.Bold);
            stSub.rectTransform.Anchor(0, 1, 1, 1, 0, -92, 0, 60);
            stSynopsis = UIKit.Txt(info, "Synopsis", "", 22, UIKit.Ink, TextAnchor.UpperLeft);
            stSynopsis.rectTransform.Anchor(0, 1, 1, 1, 0, -198, 0, 100);
            stSynopsis.lineSpacing = 1.1f;
            stSynopsis.resizeTextForBestFit = true;
            stSynopsis.resizeTextMinSize = 16;
            stSynopsis.resizeTextMaxSize = 22;

            // 이 스테이지의 카드 (상대 영웅 · 내 영웅 · 필드 · 보상 영웅) — 클릭하면 확대
            for (int i = 0; i < StoryCardSlots; i++)
            {
                int idx = i;
                var holder = UIKit.Rect("StageCard" + i, info);
                holder.Place(new Vector2(0, 1), new Vector2(i * 84, -224), new Vector2(72, 101), new Vector2(0, 1));
                var f = CardFace.Build(holder, 72, 101, 0.095f);
                f.root.Fill();
                f.glow.gameObject.SetActive(false);
                var cap = UIKit.Txt(info, "StageCardCap" + i, "", 14, UIKit.Muted, TextAnchor.LowerCenter, FontStyle.Bold);
                cap.rectTransform.Place(new Vector2(0, 1), new Vector2(i * 84 - 6, -200), new Vector2(84, 20), new Vector2(0, 1));
                cap.horizontalOverflow = HorizontalWrapMode.Overflow;
                CardZoom.Bind(holder, () => storyCards[idx].card);
                storyCards.Add((holder, f, cap, null));
            }
            stReward = UIKit.Txt(info, "Reward", "", 20, Color.white, TextAnchor.UpperLeft);
            stReward.rectTransform.Anchor(0, 1, 1, 1, 0, -325, 0, 222);
            stReward.lineSpacing = 1.15f;
            stStart = UIKit.Button(info, "시작 ▶", StartStoryClicked, Red, 32, Color.white);
            stStart.rt.Anchor(1, 0, 1, 0, -290, 0, 0, -92);
            stReplay = UIKit.Button(info, "이야기 다시 보기", () => { if (storySel != null) onStoryReplay?.Invoke(storySel); }, Dark, 22);
            stReplay.rt.Anchor(1, 0, 1, 0, -540, 6, 306, -86);

            storyVol = StoryData.Volumes[0];
            return page;
        }

        void SelectVolume(StoryVolume v)
        {
            if (!v.ready) { Toast($"제{v.number}장 「{v.title}」은(는) 준비 중입니다"); return; }
            if (!VolumeUnlocked(v)) { Toast($"제{v.number - 1}장을 먼저 클리어하세요"); return; }
            storyVol = v;
            storySel = null;
            RefreshStory();
        }

        void SelectStage(int i)
        {
            if (storyVol == null || i >= storyVol.stages.Count) return;
            var st = storyVol.stages[i];
            if (!StageUnlocked(st)) { Toast("이전 스테이지를 먼저 클리어하세요"); return; }
            storySel = st;
            RefreshStory();
        }

        static bool StageUnlocked(StoryStage st) => st.number == 1 ? VolumeUnlocked(st.volume) : Profile.StoryCleared(st.volume.stages[st.number - 2].id);

        /// <summary>장 해금: 이전 장의 마지막 스테이지를 클리어해야 한다</summary>
        static bool VolumeUnlocked(StoryVolume v)
        {
            int i = StoryData.Volumes.IndexOf(v);
            return i <= 0 || Profile.StoryCleared(StoryData.Volumes[i - 1].stages.Last().id);
        }

        /// <summary>장 안에서 다음에 할 스테이지 (모두 클리어면 마지막)</summary>
        static StoryStage NextStage(StoryVolume v) => v.stages.FirstOrDefault(s => !Profile.StoryCleared(s.id)) ?? v.stages.Last();

        void RefreshStory()
        {
            if (storyVol == null) storyVol = StoryData.Volumes[0];
            if (storySel == null || storySel.volume != storyVol) storySel = NextStage(storyVol);
            var v = storyVol;

            foreach (var (vol, btn) in volBtns)
            {
                bool on = vol == v;
                int done = vol.stages.Count(s => Profile.StoryCleared(s.id));
                btn.baseColor = on ? new Color(0.45f, 0.32f, 0.08f, 0.97f) : vol.ready ? new Color(0.1f, 0.08f, 0.15f, 0.95f) : new Color(0.05f, 0.05f, 0.07f, 0.9f);
                bool unlocked = VolumeUnlocked(vol);
                btn.label.text = vol.ready && !unlocked
                    ? $"<size=16><color=#777>제{vol.number}장</color></size>  <color=#888>{vol.title}</color>\n<size=15><color=#666>잠김 · 제{vol.number - 1}장 클리어 필요</color></size>"
                    : vol.ready
                    ? $"<size=16><color=#ffd05c>제{vol.number}장</color></size>  <b>{vol.title}</b>\n<size=15><color=#ccc>{done} / {vol.stages.Count} 클리어</color></size>"
                    : $"<size=16><color=#777>제{vol.number}장</color></size>  <color=#888>{vol.title}</color>\n<size=15><color=#666>준비 중</color></size>";
                btn.textColor = Color.white;
                btn.Interactable = true;
            }

            coverImg.texture = Resources.Load<Texture2D>(StoryData.Root + v.cover);
            volNo.text = $"제{v.number}장";
            volTitle.text = v.title;
            volTag.text = v.tagline;
            int cleared = v.stages.Count(s => Profile.StoryCleared(s.id));
            volProgress.text = $"진행  {cleared} / {v.stages.Count}" + (cleared == v.stages.Count ? "   ·   완결!" : "");

            for (int i = 0; i < stageNodes.Count; i++)
            {
                var (ring, fill, num, label, btn) = stageNodes[i];
                bool exists = i < v.stages.Count;
                ring.gameObject.SetActive(exists);
                label.gameObject.SetActive(exists);
                if (!exists) continue;
                var st = v.stages[i];
                bool done = Profile.StoryCleared(st.id), open = StageUnlocked(st), sel = st == storySel;
                ring.color = sel ? Color.white : done ? UIKit.Gold : open ? new Color(1f, 0.6f, 0.4f) : new Color(0.35f, 0.35f, 0.4f);
                fill.color = done ? new Color(0.42f, 0.3f, 0.06f, 1f) : open ? new Color(0.35f, 0.08f, 0.06f, 1f) : new Color(0.08f, 0.08f, 0.1f, 1f);
                num.text = open ? (done ? "★" : (i + 1).ToString()) : "?";
                num.color = open ? Color.white : new Color(0.6f, 0.6f, 0.65f);
                label.text = open ? $"<b>{st.title}</b>" : "???";
                label.color = sel ? UIKit.Gold : open ? UIKit.Ink : UIKit.Muted;
                ring.rectTransform.localScale = Vector3.one * (sel ? 1.15f : 1f);
                if (i < stageLinks.Count) stageLinks[i].color = done ? new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.8f) : new Color(1, 1, 1, 0.18f);
            }

            var s = storySel;
            stageThumb.texture = Resources.Load<Texture2D>(StoryData.Root + s.thumb);
            stTitle.text = $"STAGE {s.number}  {s.title}";
            stSub.text = s.subtitle;
            stSynopsis.text = s.synopsis + $"\n<size=19><color=#bbb>상대 · {s.battle.oppName}</color></size>";
            bool clearedSel = Profile.StoryCleared(s.id);
            var rewardHero = string.IsNullOrEmpty(s.rewardHero) ? null : CardDB.Get(s.rewardHero);

            // 스테이지 카드 줄: 있는 것만 왼쪽부터
            var shown = new List<(string cap, CardData card)> { ("상대 영웅", s.battle.oppHero) };
            if (s.battle.playerHero != null) shown.Add(("내 영웅", s.battle.playerHero));
            if (s.battle.field != null) shown.Add(("필드", s.battle.field));
            if (rewardHero != null) shown.Add(("보상 영웅", rewardHero));
            shown.RemoveAll(x => x.card == null);
            for (int i = 0; i < storyCards.Count; i++)
            {
                var (holder, face, cap, _) = storyCards[i];
                bool on = i < shown.Count;
                holder.gameObject.SetActive(on);
                cap.gameObject.SetActive(on);
                storyCards[i] = (holder, face, cap, on ? shown[i].card : null);
                if (!on) continue;
                face.Set(shown[i].card);
                face.SetFinish(CardFinish.Normal);
                cap.text = shown[i].cap;
                cap.color = shown[i].cap == "보상 영웅" ? new Color(0.6f, 0.8f, 1f) : UIKit.Muted;
            }
            stReward.rectTransform.offsetMin = new Vector2(shown.Count * 84 + 10, stReward.rectTransform.offsetMin.y);

            string hero = rewardHero != null ? $"\n<color=#9cf>영웅 「{rewardHero.name}」 해금</color>" : "";
            stReward.text = clearedSel
                ? "<color=#8f8>클리어 완료</color>\n<size=17><color=#aaa>첫 클리어 보상 수령함</color></size>"
                : $"<size=17><color=#ccc>첫 클리어 보상</color></size>\n<color=#ffd05c><b>오팔 ×{s.rewardOpal}</b></color>{hero}";
            stStart.label.text = clearedSel ? "다시 도전 ▶" : "시작 ▶";
            stStart.Interactable = StageUnlocked(s);
            stReplay.rt.gameObject.SetActive(clearedSel);
        }

        void StartStoryClicked()
        {
            if (storySel == null || !StageUnlocked(storySel)) return;
            onStory?.Invoke(storySel);
        }

        // ── 테스트 ──
        public void TestStorySelect(int stage, int vol = 0) { storyVol = StoryData.Volumes[vol]; storySel = storyVol.stages[Mathf.Clamp(stage, 0, storyVol.stages.Count - 1)]; RefreshStory(); }
    }
}
