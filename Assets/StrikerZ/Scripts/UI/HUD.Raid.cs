using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 레이드 HUD: 보스 HP 바 · 파티 목록 · 팀원 화면.
    /// 화면 전환 = ←/→ 방향키, 마우스 휠, 터치 좌우 스와이프. 0번 = 내 전장, 1~ = 팀원 전장.
    /// </summary>
    public partial class HUD
    {
        RectTransform raidRoot, raidPartyList, allyView;
        Image raidHpFill;
        Text raidBossName, raidBossHp, raidBossInfo, raidViewHint, raidToast, raidTimeText;
        RectTransform raidTimePill, lootRow;
        CanvasGroup raidToastGroup, allyViewGroup;
        float raidToastUntil;
        readonly List<(RectTransform rt, Image bg, Image fill, Text name, Text hp)> raidRows = new List<(RectTransform, Image, Image, Text, Text)>();
        int raidView;
        float raidSlide, raidWheelAt;
        // 팀원 화면 구성
        Image allyAvatar, allyHpFill;
        Text allyName, allyHp, allyInfo, allyLog, allyMinionLabel, allyLaneLabel;
        readonly List<CardFace> allyMinionFaces = new List<CardFace>();
        readonly List<CardFace> allyLaneFaces = new List<CardFace>();
        readonly List<Text> allyMinionStats = new List<Text>();
        readonly List<Text> allyLaneStats = new List<Text>();
        Text allyMinionEmpty, allyLaneEmpty;
        // 터치 스와이프
        Vector2 swipeStart;
        float swipeStartAt;
        bool swipeActive;

        public int RaidViewIndex => raidView;

        void BuildRaid()
        {
            raidRoot = UIKit.Rect("Raid", root);
            raidRoot.Fill();

            // 보스 HP 바 (상단 중앙, 타임 패널 아래)
            var bar = UIKit.Frame(raidRoot, "BossBar");
            bar.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(900, 42));
            raidBossName = UIKit.Txt(bar.transform, "Name", "", 20, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleLeft, FontStyle.Bold);
            raidBossName.rectTransform.Anchor(0, 0, 0.34f, 1, 14, 0, 4, 0);
            raidBossName.verticalOverflow = VerticalWrapMode.Overflow;
            raidBossName.Shadow();
            var hpBg = UIKit.Img(bar.transform, "HpBg", new Color(0.12f, 0.02f, 0.03f, 1), GameAssets.Rounded, true);
            hpBg.rectTransform.Anchor(0.34f, 0.18f, 1, 0.82f, 0, 0, 12, 0);
            raidHpFill = UIKit.Img(hpBg.transform, "Fill", new Color(0.9f, 0.2f, 0.2f), GameAssets.Rounded, true);
            raidHpFill.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            raidBossHp = UIKit.Txt(hpBg.transform, "Hp", "", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            raidBossHp.rectTransform.Fill();
            raidBossHp.verticalOverflow = VerticalWrapMode.Overflow;
            raidBossHp.Outline(new Color(0, 0, 0, 0.9f));
            raidBossInfo = UIKit.Txt(bar.transform, "Info", "", 16, UIKit.Ink, TextAnchor.MiddleLeft);
            raidBossInfo.rectTransform.Anchor(0, 0, 1, 0.3f, 18, 2, 18, 0);
            raidBossInfo.horizontalOverflow = HorizontalWrapMode.Overflow;
            raidBossInfo.verticalOverflow = VerticalWrapMode.Overflow;
            raidBossInfo.gameObject.SetActive(false);   // 한 줄 바: 약점은 이름 옆에, 나머지 설명은 생략

            raidToast = UIKit.Txt(raidRoot, "Toast", "", 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            raidToast.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(1000, 40));

            // 타임 표시: 상단 TIME 패널 대신 내 패 아래 중앙에 작게
            var pill = UIKit.Img(raidRoot, "TimePill", new Color(0.03f, 0.025f, 0.06f, 0.88f), GameAssets.Rounded, true);
            raidTimePill = pill.rectTransform;
            raidTimePill.Place(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(430, 44), new Vector2(0.5f, 0));
            var pb = UIKit.Img(pill.transform, "Border", new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.6f), GameAssets.RoundedOutline, true);
            pb.rectTransform.Fill();
            raidTimeText = UIKit.Txt(pill.transform, "Time", "", 22, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            raidTimeText.rectTransform.Fill();
            raidTimeText.Shadow();
            raidToast.Outline(new Color(0, 0, 0, 0.9f), 2f);
            raidToastGroup = raidToast.gameObject.AddComponent<CanvasGroup>();
            raidToastGroup.alpha = 0;
            raidToastGroup.blocksRaycasts = false;

            // 파티 목록 (오른쪽, 필드 패널 아래)
            raidPartyList = UIKit.Rect("Party", raidRoot);
            raidPartyList.Place(new Vector2(1, 1), new Vector2(-18, -220), new Vector2(400, 240), new Vector2(1, 1));
            var head = UIKit.Img(raidPartyList, "Head", UIKit.Panel, GameAssets.Rounded, true);
            head.rectTransform.Anchor(0, 1, 1, 1, 0, -40, 0, 0);
            raidViewHint = UIKit.Txt(head.transform, "Hint", "", 18, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            raidViewHint.rectTransform.Fill();
            for (int i = 0; i < RaidData.MaxParty; i++)
            {
                int idx = i;
                var bg = UIKit.Img(raidPartyList, "Member" + i, UIKit.Panel, GameAssets.Rounded, true);
                var rt = bg.rectTransform;
                rt.Anchor(0, 1, 1, 1, 0, -44 - (i + 1) * 62, 0, 44 + i * 62);
                var name = UIKit.Txt(bg.transform, "Name", "", 20, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.Anchor(0, 0.5f, 1, 1, 12, 0, 8, 0);
                var hb = UIKit.Img(bg.transform, "HpBg", new Color(0.1f, 0.02f, 0.03f, 1), GameAssets.Rounded, true);
                hb.rectTransform.Anchor(0, 0.1f, 1, 0.48f, 12, 0, 12, 0);
                var fill = UIKit.Img(hb.transform, "Fill", HpColor(1), GameAssets.Rounded, true);
                fill.rectTransform.Anchor(0, 0, 1, 1, 2, 2, 2, 2);
                var hp = UIKit.Txt(hb.transform, "Hp", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                hp.rectTransform.Fill();
                hp.Outline(new Color(0, 0, 0, 0.9f));
                var btn = bg.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => SetRaidView(idx));
                raidRows.Add((rt, bg, fill, name, hp));
            }

            BuildAllyView();
            raidRoot.gameObject.SetActive(false);
        }

        void BuildAllyView()
        {
            var bg = UIKit.Frame(raidRoot, "AllyView", new Color(0.03f, 0.03f, 0.06f, 0.94f));
            allyView = bg.rectTransform;
            allyView.Anchor(0.2f, 0.14f, 0.78f, 0.73f);
            allyViewGroup = bg.gameObject.AddComponent<CanvasGroup>();
            var solid = UIKit.Img(allyView, "Solid", new Color(0.035f, 0.03f, 0.07f, 0.97f), GameAssets.Rounded, true);
            solid.rectTransform.Anchor(0, 0, 1, 1, 6, 6, 6, 6);

            allyAvatar = UIKit.AvatarCircle(allyView, 92);
            ((RectTransform)allyAvatar.transform.parent.parent).Place(new Vector2(0, 1), new Vector2(22, -16), new Vector2(92, 92), new Vector2(0, 1));

            allyName = UIKit.Txt(allyView, "Name", "", 30, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            allyName.rectTransform.Anchor(0, 1, 0.62f, 1, 130, -58, 0, 14);
            allyName.Shadow();
            var hb = UIKit.Img(allyView, "HpBg", new Color(0.1f, 0.02f, 0.03f, 1), GameAssets.Rounded, true);
            hb.rectTransform.Anchor(0, 1, 0.62f, 1, 130, -96, 0, 62);
            allyHpFill = UIKit.Img(hb.transform, "Fill", HpColor(1), GameAssets.Rounded, true);
            allyHpFill.rectTransform.Anchor(0, 0, 1, 1, 3, 3, 3, 3);
            allyHp = UIKit.Txt(hb.transform, "Hp", "", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            allyHp.rectTransform.Fill();
            allyHp.Outline(new Color(0, 0, 0, 0.9f));
            allyInfo = UIKit.Txt(allyView, "Info", "", 19, UIKit.Ink, TextAnchor.UpperRight);
            allyInfo.rectTransform.Anchor(0.62f, 1, 1, 1, 10, -110, 22, 18);

            // 보스의 졸개 (위) · 팀원 전장 (아래)
            allyMinionLabel = UIKit.Txt(allyView, "MinionLabel", "보스의 졸개", 20, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleLeft, FontStyle.Bold);
            allyMinionLabel.rectTransform.Anchor(0, 1, 0.68f, 1, 24, -146, 0, 118);
            allyLaneLabel = UIKit.Txt(allyView, "LaneLabel", "팀원 전장", 20, new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            allyLaneLabel.rectTransform.Anchor(0, 0, 0.68f, 0, 24, 226, 0, -254);
            BuildFaceRow(allyMinionFaces, allyMinionStats, 3, new Vector2(24, -152), out allyMinionEmpty);
            BuildFaceRow(allyLaneFaces, allyLaneStats, 5, new Vector2(24, -152 - 262), out allyLaneEmpty);

            var logBg = UIKit.Img(allyView, "LogBg", new Color(0, 0, 0, 0.35f), GameAssets.Rounded, true);
            logBg.rectTransform.Anchor(0.69f, 0, 1, 1, 0, 20, 20, 126);
            allyLog = UIKit.Txt(logBg.transform, "Log", "", 17, UIKit.Ink, TextAnchor.LowerLeft);
            allyLog.rectTransform.Anchor(0, 0, 1, 1, 12, 10, 10, 10);
            allyLog.verticalOverflow = VerticalWrapMode.Truncate;
        }

        void BuildFaceRow(List<CardFace> faces, List<Text> stats, int n, Vector2 origin, out Text empty)
        {
            const float w = 132, h = 185;
            for (int i = 0; i < n; i++)
            {
                var holder = UIKit.Rect("Slot" + i, allyView);
                holder.Place(new Vector2(0, 1), origin + new Vector2(i * (w + 10), 0), new Vector2(w, h), new Vector2(0, 1));
                var f = CardFace.Build(holder, w, h, 0.62f);
                f.root.anchoredPosition = Vector2.zero;
                faces.Add(f);
                // 클릭 = 확대 (현재 ATK·HP 반영)
                var h0 = holder;
                CardZoom.Bind(holder, () => raidFaceMon.TryGetValue(h0, out var m) ? m.data : null,
                    stats: () => raidFaceMon.TryGetValue(h0, out var m) ? (m.atk, m.hp) : (-1, -1));
                var st = UIKit.Txt(holder, "Stat", "", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                st.rectTransform.Anchor(0, 0, 1, 0, 0, -26, 0, 0);
                st.Outline(new Color(0, 0, 0, 0.9f));
                stats.Add(st);
                holder.gameObject.SetActive(false);
            }
            empty = UIKit.Txt(allyView, "Empty", "— 없음 —", 20, UIKit.Muted, TextAnchor.MiddleLeft);
            empty.rectTransform.Place(new Vector2(0, 1), origin + new Vector2(10, -h * 0.5f), new Vector2(400, 40), new Vector2(0, 0.5f));
        }

        public void BeginRaid(GameManager gm)
        {
            timePanel.gameObject.SetActive(false);
            raidView = 0;
            raidSlide = 0;
            raidToastGroup.alpha = 0;
            raidRoot.gameObject.SetActive(true);
            RefreshRaid();
        }

        void EndRaidHud()
        {
            if (raidRoot != null) raidRoot.gameObject.SetActive(false);
            if (timePanel != null) timePanel.gameObject.SetActive(true);
            if (lootRow != null) lootRow.gameObject.SetActive(false);
            raidView = 0;
        }

        public void RaidToast(string msg)
        {
            if (raidToast == null) return;
            raidToast.text = msg;
            raidToastGroup.alpha = 1;
            raidToastUntil = Time.unscaledTime + 2.2f;
        }

        public void SetRaidView(int i)
        {
            if (g == null || g.raid == null) return;
            int n = g.raid.allies.Count + 1;
            i = ((i % n) + n) % n;
            if (i == raidView) return;
            raidSlide = i > raidView ? 1f : -1f;
            raidView = i;
            Sfx.Play("click", 0.5f);
            RefreshRaid();
        }

        void RefreshRaid()
        {
            if (g == null || g.raid == null || raidRoot == null) { EndRaidHud(); return; }
            var r = g.raid;
            var boss = g.Cpu;
            if (boss == null) return;

            // 상대 패널 초상화를 보스 일러스트로
            var art = GameAssets.ArtFor(boss.hero.data);
            if (art != null) { cpuPanel.portrait.sprite = art; cpuPanel.portrait.color = Color.white; }

            string who = g.active == null ? "" : g.active == g.Human ? "파티 타임" : "보스 타임";
            raidTimeText.text = g.time > 0 ? $"TIME {g.time}  <size=17><color=#ddd>{who} · {PhaseName(g.phase)}</color></size>" : "<size=18>레이드 준비</size>";
            raidTimeText.color = g.active == g.Human ? UIKit.Gold : new Color(1f, 0.6f, 0.5f);
            raidBossName.text = $"{r.boss.name}  <size=15><color=#9cf>약점 {CardData.ElementName(r.boss.weakness)}</color></size>";
            float hr = boss.maxHp > 0 ? (float)boss.hp / boss.maxHp : 0;
            raidHpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(hr), 1);
            raidBossHp.text = $"보스 HP {boss.hp} / {boss.maxHp}  ({Mathf.CeilToInt(hr * 100)}%)";
            string wk = CardData.ElementName(r.boss.weakness);
            raidBossInfo.text = $"<color=#ffd05c>약점 {wk}</color> (피해 2배) · <color=#ffd05c>핵심</color> {string.Join(", ", r.boss.KeyCards.Select(k => k.name))} · [{r.boss.traitName}] · {r.partySize}인 HP×{r.hpMul:0.##} ATK×{r.atkMul:0.##}";

            int members = r.allies.Count + 1;
            raidPartyList.gameObject.SetActive(true);
            raidViewHint.text = members > 1 ? $"◀  {(raidView == 0 ? "내 전장" : r.allies[raidView - 1].name + "의 전장")}  ▶   <size=14>←/→ · 휠 · 스와이프</size>" : "솔로 레이드";
            for (int i = 0; i < raidRows.Count; i++)
            {
                var row = raidRows[i];
                bool on = i < members;
                row.rt.gameObject.SetActive(on);
                if (!on) continue;
                string nm; int hp, max; bool ko; int dealt;
                if (i == 0) { nm = g.Human.name + " (나)"; hp = g.Human.hp; max = g.Human.maxHp; ko = hp <= 0; dealt = r.humanDealt; }
                else { var a = r.allies[i - 1]; nm = a.name + $" · {a.hero.name}"; hp = a.hp; max = a.maxHp; ko = a.KO; dealt = a.dealt; }
                row.name.text = $"{nm}  <size=15><color=#ffd05c>피해 {dealt}</color>{(ko ? "  <color=#f66>KO</color>" : "")}</size>";
                float f = max > 0 ? (float)hp / max : 0;
                row.fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(f), 1);
                row.fill.color = HpColor(f, ko);
                row.hp.text = $"HP {hp} / {max}";
                row.bg.color = i == raidView ? new Color(0.2f, 0.14f, 0.03f, 0.95f) : UIKit.Panel;
            }

            bool ally = raidView > 0 && raidView <= r.allies.Count;
            allyView.gameObject.SetActive(ally);
            if (ally) FillAllyView(r.allies[raidView - 1]);
        }

        void FillAllyView(RaidAlly a)
        {
            var av = GameAssets.Avatar(a.avatar);
            allyAvatar.sprite = av ?? GameAssets.ArtFor(a.hero);
            allyName.text = $"{a.name}  <size=20><color=#ccc>영웅 {a.hero.name} ({CardData.ElementName(a.hero.element)})</color>{(a.KO ? "  <color=#f66>KO</color>" : "")}</size>";
            float f = a.maxHp > 0 ? (float)a.hp / a.maxHp : 0;
            allyHpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(f), 1);
            allyHpFill.color = HpColor(f, a.KO);
            allyHp.text = $"HP {a.hp} / {a.maxHp}";
            allyInfo.text = $"보스에게 준 피해 <color=#ffd05c>{a.dealt}</color>\n패 {a.hand.Count} · 덱 {a.deck.Count}\n{(a.isRemote ? "온라인" : "AI 팀원")}";
            FillRow(allyMinionFaces, allyMinionStats, a.minions, allyMinionEmpty);
            FillRow(allyLaneFaces, allyLaneStats, a.lane, allyLaneEmpty);
            allyLog.text = string.Join("\n", a.log);
        }

        readonly Dictionary<Transform, SimMon> raidFaceMon = new Dictionary<Transform, SimMon>();

        void FillRow(List<CardFace> faces, List<Text> stats, List<SimMon> mons, Text empty)
        {
            for (int i = 0; i < faces.Count; i++)
            {
                bool on = i < mons.Count;
                faces[i].root.parent.gameObject.SetActive(on);
                if (!on) { raidFaceMon.Remove(faces[i].root.parent); continue; }
                var m = mons[i];
                raidFaceMon[faces[i].root.parent] = m;
                faces[i].Set(m.data, m.atk, m.hp);
                faces[i].glow.color = m.key ? new Color(1f, 0.85f, 0.3f, 0.8f) : new Color(1, 1, 1, 0);
                stats[i].text = $"{(m.key ? "★ " : "")}<color=#ffb070>{m.atk}</color> / <color=#8f8>{m.hp}</color>";
            }
            empty.gameObject.SetActive(mons.Count == 0);
        }

        /// <summary>Update 에서 호출: 화면 전환 입력 + 슬라이드/토스트 애니메이션</summary>
        void UpdateRaid()
        {
            if (g == null || g.raid == null || raidRoot == null || !raidRoot.gameObject.activeSelf) return;
            if (raidToastGroup.alpha > 0 && Time.unscaledTime > raidToastUntil)
                raidToastGroup.alpha = Mathf.MoveTowards(raidToastGroup.alpha, 0, Time.unscaledDeltaTime * 2.5f);
            if (allyView.gameObject.activeSelf)
            {
                raidSlide = Mathf.MoveTowards(raidSlide, 0, Time.unscaledDeltaTime * 5f);
                allyView.anchoredPosition = new Vector2(raidSlide * 360f, 0);
                allyViewGroup.alpha = 1f - Mathf.Abs(raidSlide) * 0.8f;
            }
            if (g.raid.allies.Count == 0 || Paused || CardZoom.IsOpen) return;     // 확대 중에는 화면 전환 입력 무시

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rightArrowKey.wasPressedThisFrame) SetRaidView(raidView + 1);
                else if (kb.leftArrowKey.wasPressedThisFrame) SetRaidView(raidView - 1);
            }
            var mouse = Mouse.current;
            if (mouse != null && Time.unscaledTime > raidWheelAt)
            {
                var s = mouse.scroll.ReadValue();
                float v = Mathf.Abs(s.x) > Mathf.Abs(s.y) ? s.x : -s.y;
                if (Mathf.Abs(v) > 0.1f) { SetRaidView(raidView + (v > 0 ? 1 : -1)); raidWheelAt = Time.unscaledTime + 0.3f; }
            }
            // 모바일: 좌우 스와이프 (공격 드래그 중이 아닐 때만)
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var t = ts.primaryTouch;
                if (t.press.wasPressedThisFrame) { swipeStart = t.position.ReadValue(); swipeStartAt = Time.unscaledTime; swipeActive = dragAttacker == null; }
                else if (t.press.wasReleasedThisFrame && swipeActive)
                {
                    swipeActive = false;
                    var d = t.position.ReadValue() - swipeStart;
                    if (dragAttacker == null && Time.unscaledTime - swipeStartAt < 0.8f && Mathf.Abs(d.x) > Screen.width * 0.12f && Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.5f)
                        SetRaidView(raidView + (d.x < 0 ? 1 : -1));
                }
                if (dragAttacker != null) swipeActive = false;
            }
        }

        /// <summary>레이드 결과 화면: 획득한 오팔 · 전리품 카드를 보상 문구 아래에 나열</summary>
        public void ShowRaidLoot(int opal, List<CardData> cards)
        {
            if (lootRow != null) Destroy(lootRow.gameObject);
            lootRow = UIKit.Rect("Loot", gameOverRoot);
            lootRow.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(900, 190));
            int n = cards.Count + (opal > 0 ? 1 : 0);
            float w = 118, gap = 18, x0 = -(n * w + (n - 1) * gap) * 0.5f + w * 0.5f;
            int i = 0;
            if (opal > 0)
            {
                var slot = UIKit.Rect("Opal", lootRow);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2(x0, 10), new Vector2(w, 165));
                var gem = UIKit.Img(slot, "Gem", Color.white, GameAssets.TexSprite("opal"));
                gem.preserveAspect = true;
                gem.rectTransform.Anchor(0.1f, 0.3f, 0.9f, 0.95f);
                var t = UIKit.Txt(slot, "N", $"오팔 ×{opal}", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Anchor(0, 0, 1, 0.28f);
                t.Outline(new Color(0, 0, 0, 0.9f));
                i++;
            }
            foreach (var c in cards)
            {
                var slot = UIKit.Rect("Card" + i, lootRow);
                slot.Place(new Vector2(0.5f, 0.5f), new Vector2(x0 + i * (w + gap), 10), new Vector2(w, 165));
                var f = CardFace.Build(slot, w, 165, 0.55f);
                f.root.anchoredPosition = Vector2.zero;
                f.Set(c);
                var lc = c;
                CardZoom.Bind(slot, () => lc);
                var tag = UIKit.Txt(slot, "R", c.rarity.ToString(), 17, CardData.RarityColor(c.rarity), TextAnchor.MiddleCenter, FontStyle.Bold);
                tag.rectTransform.Anchor(0, 0, 1, 0, 0, -24, 0, 0);
                tag.Outline(new Color(0, 0, 0, 0.9f));
                i++;
            }
        }

        // ── 테스트 ──
        public bool TestRaidHudVisible => raidRoot != null && raidRoot.gameObject.activeSelf;
        public bool TestAllyViewVisible => allyView != null && allyView.gameObject.activeSelf;
    }
}
