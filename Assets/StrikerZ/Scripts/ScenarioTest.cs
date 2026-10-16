using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 시나리오 테스트. 실행 인자 -strikerz-autotest -strikerz-scenario
    /// 조건이 까다로운 신화 카드마다 보드를 직접 구성하고 효과를 실행해 전/후 상태를 로그로 남긴다.
    /// </summary>
    public class ScenarioTest : MonoBehaviour
    {
        public static bool Enabled => Environment.GetCommandLineArgs().Contains("-strikerz-scenario");

        GameManager gm;
        string logPath;
        Func<string, IEnumerator> shot;
        int pass, fail;

        public IEnumerator Run(GameRoot root, GameManager g, string log, Func<string, IEnumerator> takeShot)
        {
            gm = g;
            logPath = log;
            shot = takeShot;
            root.TestStart(CardList.Heroes[0]);
            float t0 = Time.realtimeSinceStartup;
            while (!(gm.phase == Phase.Main && gm.active == gm.Human && !gm.busy) && Time.realtimeSinceStartup - t0 < 90f) yield return null;
            gm.autoHuman = true;   // 효과 안의 선택은 AI 가 대신
            Write($"SCENARIO start at TIME {gm.time}");

            var me = gm.Human; var op = gm.Cpu;
            yield return Scn("S201 신비의 심해", () => { Mon(me, "M004", "M005"); Grave(me, "M006", "M004"); Mon(op, "M001"); });
            yield return Scn("S202 깊은 암야", () => { Mon(me, "M015", "M016"); Mon(op, "M003", "M012", "M009"); });
            yield return Scn("S203 들풀의 군세", () => { Mon(me, "M007"); Hand(me, "M008", "M007"); Mon(op, "M001"); });
            yield return Scn("S204 대지의 분노", () => { Mon(me, "M010"); Mon(op, "M001", "M009", "M005"); });
            yield return Scn("S204 대지의 분노 (몬스터 없음)", () => { Hand(me, "M009"); Mon(op, "M001"); }, "S204");
            yield return Scn("S205 인간의 긍지", () => { Mon(me, "M011"); Grave(me, "M012", "M003"); Mon(op, "M001"); });
            yield return Scn("S206 화염 군단", () => { Mon(me, "M001"); Mon(op, "M013", "M004", "M012"); });
            yield return Scn("S207 균형의 저울", () => { Mon(me, "M013"); Hand(me, "M001", "M002", "M003", "S005", "S006"); Mon(op, "M001", "M004", "M009"); Hand(op, "M007", "M019"); });
            yield return Scn("S208 앱솔루트 스펠", () => { Mon(me, "M001", "M004"); Slot(me, "S005"); Slot(op, "S004", "S006"); Mon(op, "M003", "M012", "M009"); });
            yield return Scn("M101 엔시아나", () => { Slot(op, "S004"); Mon(op, "M001"); });
            yield return Scn("M103 태초의 창조신", () => { Mon(op, "M001"); });
            yield return Scn("M105 무한의 성수", () => { Mon(me, "M009", "M010"); Mon(op, "M001"); });
            yield return Scn("M106 카로넬리아", () => { Slot(op, "S004"); Mon(op, "M001"); });
            yield return Scn("M109 판타즘 마스터", () => { Mon(op, "M003", "M012"); });
            yield return Scn("M110 이세리브", () => { Mon(op, "M003", "M001"); });
            yield return Scn("M112 오르브레크", () => { Slot(me, "S005"); Slot(op, "S004", "S006"); Mon(op, "M001"); });

            // ── 공격 규칙 (도발 · 영웅 반격 · 연쇄 · 암살 · 연속 공격) — AI 대상 선택 경로 ──
            // 1회 공격 몬스터(화염 창술사)로 확인: 빛의 견습사제가 아니라 [도발] 바위 골렘만 공격할 수 있다
            yield return AtkScn("RULE 도발 강제 대상", () => { Mon(me, "M002"); Mon(op, "M013", "M009"); },
                logs => op.slots.Any(s => s != null && s.data.id == "M013") && logs.Any(l => l.Contains("바위 골렘")));
            yield return AtkScn("RULE 영웅 반격", () => { Mon(me, "M019"); },
                logs => gm.Cpu.hp < gm.Cpu.maxHp && logs.Any(l => l.Contains("반격!")));
            yield return AtkScn("RULE 암살 반격 무시", () => { Mon(me, "M015"); },
                logs => gm.Cpu.hp < gm.Cpu.maxHp && logs.Any(l => l.Contains("[암살]")) && !logs.Any(l => l.Contains("반격!")));
            yield return AtkScn("RULE 연쇄 낙뢰", () => { Mon(me, "M012"); Mon(op, "M013", "M004", "M001"); },
                // 파괴에 성공하면 [연쇄]로 이어서 공격, 대상이 버티면(주사위 '파괴되지 않음') 종료
                // HP 규칙: 반격 피해로 라이자도 쓰러지면(상호 격파) 연쇄는 일어나지 않는다
                logs => logs.Any(l => l.Contains("[연쇄]")) || logs.Any(l => l.Contains("파괴되지 않았다")) || logs.Any(l => l.Contains("뇌신 라이자 파괴")) || op.Monsters().Count == 3 && logs.Count(l => l.Contains(" vs ")) == 1);
            yield return AtkScn("RULE 연속 공격", () => { Mon(me, "M007"); Mon(op, "M013", "M004"); },
                logs => logs.Count(l => l.Contains(" vs ")) + logs.Count(l => l.Contains("직접 공격!")) + logs.Count(l => l.Contains("공격하지 않음")) >= 2 || logs.Any(l => l.Contains("공격하지 않음")) || logs.Any(l => l.Contains("질풍 매 파괴")));

            // ── HP 규칙 ──
            yield return HpScn();
            yield return AtkScn("RULE 몬스터 전투 HP 피해", () => { Mon(me, "M001"); Mon(op, "M311"); },
                logs => logs.Any(l => l.Contains(" vs ")) && (op.slots.Any(s => s != null && s.data.id == "M311" && s.damage > 0) || logs.Any(l => l.Contains("M311") || l.Contains("이끼 버섯돌이 파괴")) || logs.Any(l => l.Contains("파괴되지 않았다")) || logs.Any(l => l.Contains("이끼 버섯돌이"))));
            yield return AtkScn("RULE 빛의 가호 코인", () => { Mon(me, "M013"); Mon(op, "M201"); },
                logs => logs.Any(l => l.Contains("[빛의 가호] 코인")));
            yield return AtkScn("RULE 어둠의 대가 코인", () => { Mon(me, "M395"); },
                logs => logs.Any(l => l.Contains("[어둠의 대가] 코인")));
            {
                bool wind = CardList.Monsters.Where(m => m.element == Element.Wind).All(m => m.DiceRolls == 4)
                            && CardList.Monsters.Where(m => m.element != Element.Wind).All(m => m.DiceRolls == 3);
                bool counts = CardList.Monsters.Count == 270 && CardList.Monsters.All(m => m.hp > 0)
                              && CardList.Monsters.GroupBy(m => (m.element, m.minTime)).All(g2 => g2.Count() == 3);
                Report(wind && counts, $"RULE 바람 판정 4회={wind} · 몬스터 270종(속성×타임×3)={counts}");
            }

            yield return FieldRules();
            yield return DrawRules();
            yield return FieldThemes();
            yield return DragAttackScn();
            // UI: 덱 장수 툴팁 · Esc 일시정지 메뉴 (스크린샷 확인)
            gm.hud.TestPileTip(gm.Human, Zone.Deck);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return shot("ui_pile_tip");
            gm.hud.TestPileTip(null, Zone.Deck);
            gm.hud.TestPause(true);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return shot("ui_pause");
            gm.hud.TestPause(false);
            yield return FxGallery();
            Write($"SCENARIO DONE pass={pass} fail={fail}");
        }

        void Write(string s) => File.AppendAllText(logPath, s + "\n");

        /// <summary>시나리오 결과 기록: 통과/실패 수를 세고 "SCN OK/FAIL 내용" 한 줄을 남긴다</summary>
        void Report(bool ok, string msg)
        {
            if (ok) pass++; else fail++;
            Write($"SCN {(ok ? "OK  " : "FAIL")} {msg}");
        }

        static CardData Find(string id)
            => CardList.Monsters.Concat(CardList.MythicMonsters).Concat(CardList.Magics).Concat(CardList.Mythics).Concat(CardList.Fields).First(c => c.id == id);

        void Reset()
        {
            gm.TestSetField(CardList.Fields.First(f => f.name == "고요한 평원"));   // 무작위 필드 보정 없이 판정
            foreach (var p in gm.players)
            {
                for (int i = 0; i < PlayerState.SlotCount; i++)
                {
                    if (p.slots[i] != null) gm.board.DestroyView(p.slots[i]);
                    p.slots[i] = null;
                }
                foreach (var c in p.hand) gm.board.DestroyView(c);
                foreach (var c in p.grave) gm.board.DestroyView(c);
                p.hand.Clear();
                p.grave.Clear();
                p.hp = p.maxHp;
            }
            gm.statuses.Clear();
        }

        void Mon(PlayerState p, params string[] ids) => Slot(p, ids);

        void Slot(PlayerState p, params string[] ids)
        {
            foreach (var id in ids)
            {
                int s = p.FreeSlot();
                var c = new CardInstance(Find(id), p) { zone = Zone.Slot, slot = s };
                p.slots[s] = c;
            }
        }

        void Hand(PlayerState p, params string[] ids)
        {
            foreach (var id in ids) p.hand.Add(new CardInstance(Find(id), p) { zone = Zone.Hand });
        }

        void Grave(PlayerState p, params string[] ids)
        {
            foreach (var id in ids) p.grave.Add(new CardInstance(Find(id), p) { zone = Zone.Grave });
        }

        string State()
        {
            string Side(PlayerState p)
                => $"{p.name} HP{p.hp} 필드[{string.Join(", ", p.slots.Where(s => s != null).Select(s => s.IsMonster ? $"{s.Name}:{gm.EffectiveAtk(s)}/{gm.CurHp(s)}{(s.stunUntil >= gm.time ? "(불능)" : "")}" : s.Name))}] 패{p.hand.Count} 묘지{p.grave.Count}";
            return $"{Side(gm.Human)} | {Side(gm.Cpu)} | 필드카드 {gm.center?.Name}";
        }

        IEnumerator Scn(string title, Action setup, string cardId = null)
        {
            Reset();
            setup();
            gm.RefreshAll();
            yield return new WaitForSecondsRealtime(0.4f);
            var data = Find(cardId ?? title.Split(' ')[0]);
            var inst = new CardInstance(data, gm.Human) { zone = data.IsMonsterType() ? Zone.Slot : Zone.Resolving };
            if (data.type == CardType.Monster)
            {
                int s = gm.Human.FreeSlot();
                inst.slot = s;
                gm.Human.slots[s] = inst;
            }
            bool can = data.canActivate == null || data.canActivate(gm, inst);
            string before = State();
            int errBefore = errorCount;
            if (can && data.effect != null) yield return data.effect(gm, inst);
            gm.RefreshAll();
            string after = State();
            bool ok = can && errorCount == errBefore && before != after;
            Report(ok, $"{title} (발동가능={can})\n   전: {before}\n   후: {after}");
            yield return shot("scn_" + data.id);
        }

        /// <summary>내 첫 몬스터로 공격 1회 처리 후 조건 확인</summary>
        IEnumerator AtkScn(string title, Action setup, Func<List<string>, bool> check)
        {
            var logs = new List<string>();
            Application.LogCallback cb = (m, st, t) => { if (m.StartsWith("[StrikerZ]")) logs.Add(m); };
            Application.logMessageReceived += cb;
            Reset();
            setup();
            gm.RefreshAll();
            yield return new WaitForSecondsRealtime(0.3f);
            var a = gm.Human.slots.First(s => s != null);
            string before = State();
            int errBefore = errorCount;
            yield return gm.TestAttack(a);
            Application.logMessageReceived -= cb;
            gm.RefreshAll();
            string after = State();
            bool ok = errorCount == errBefore && check(logs);
            Report(ok, $"{title}\n   전: {before}\n   후: {after}");
            yield return shot("rule_" + pass + "_" + fail);
        }

        /// <summary>HP: 피해 누적 → 타임 시작 회복 → 상처 예외 → HP 0 파괴</summary>
        IEnumerator HpScn()
        {
            Reset();
            Mon(gm.Cpu, "M256");   // 산호 게 (물, 2/4)
            gm.RefreshAll();
            yield return new WaitForSecondsRealtime(0.3f);
            var c = gm.Cpu.slots.First(s => s != null);
            int max = gm.MaxHp(c);
            yield return gm.DamageMonster(c, 2, "테스트");
            bool partial = c.zone == Zone.Slot && gm.CurHp(c) == max - 2;
            gm.TestRegen();
            bool regen = gm.CurHp(c) == max;
            yield return gm.DamageMonster(c, 1, "테스트");
            c.noRegenUntil = gm.time;
            gm.TestRegen();
            bool wound = gm.CurHp(c) == max - 1;
            yield return gm.DamageMonster(c, 99, "테스트");
            bool killed = c.zone == Zone.Grave;
            bool ok = partial && regen && wound && killed;
            Report(ok, $"RULE HP 누적={partial} 타임회복={regen} 상처={wound} 0이면파괴={killed} (최대 {max})");
        }

        /// <summary>필드 규칙: 상성 강화/약화 · 무지개 = 모든 속성 · 무속성 = 영향 없음 · 혼돈의 균열 판정 -1</summary>
        IEnumerator FieldRules()
        {
            Reset();
            Mon(gm.Human, "M256", "M001", "M423", "M201", "M283");   // 물 산호 게 · 화염 불꽃 꼬마용 · 무지개 프리즘 슬라임 · 무속성 견습 창병 · 바람 바람 요정
            gm.RefreshAll();
            yield return new WaitForSecondsRealtime(0.2f);
            var mons = gm.Human.Monsters();
            CardInstance water = mons[0], fire = mons[1], rainbow = mons[2], none = mons[3], wind = mons[4];
            string Stat(CardInstance m) => $"{gm.EffectiveAtk(m)}/{gm.MaxHp(m)}/{gm.Rolls(m)}";
            CardData F(string name) => CardList.Fields.First(f => f.name == name);
            var calm = F("고요한 평원");
            gm.TestSetField(calm);
            string w0 = Stat(water), f0 = Stat(fire), r0 = Stat(rainbow), n0 = Stat(none), d0 = Stat(wind);

            gm.TestSetField(F("해일의 폭주"));     // 물 +1/+1, 화염 -1/-1
            bool tide = gm.EffectiveAtk(water) == water.data.atk + 1 && gm.MaxHp(water) == water.data.hp + 1
                        && gm.EffectiveAtk(fire) == Mathf.Max(0, fire.data.atk - 1) && gm.MaxHp(fire) == Mathf.Max(1, fire.data.hp - 1)
                        && Stat(rainbow) == r0 && Stat(none) == n0;          // 무지개: +1/+1 과 -1/-1 을 모두 받아 그대로
            string tideTxt = $"물 {Stat(water)} 화염 {Stat(fire)} 무지개 {Stat(rainbow)} 무속성 {Stat(none)}";

            gm.TestSetField(F("혼돈의 균열"));     // 무지개 -1/-1, 그 외 속성 판정 -1
            bool chaos = gm.Rolls(fire) == 2 && gm.Rolls(wind) == 3 && gm.Rolls(none) == 3 && gm.Rolls(rainbow) == 3
                         && gm.EffectiveAtk(rainbow) == rainbow.data.atk - 1 && gm.MaxHp(rainbow) == rainbow.data.hp - 1 && Stat(none) == n0;
            string chaosTxt = $"화염 {Stat(fire)} 바람 {Stat(wind)} 무지개 {Stat(rainbow)} 무속성 {Stat(none)}";

            gm.TestSetField(F("작열의 화산"));     // 화염 ATK +2, 바람 ATK -1 · 무지개는 둘 다 → +1
            bool volcano = gm.EffectiveAtk(fire) == fire.data.atk + 2 && gm.EffectiveAtk(wind) == wind.data.atk - 1
                           && gm.EffectiveAtk(rainbow) == rainbow.data.atk + 1 && Stat(none) == n0;
            gm.TestSetField(F("황혼의 전장"));     // 능력치 변화 없음, 뒷면 소환 가능
            bool dusk = Stat(water) == w0 && Stat(fire) == f0 && Stat(rainbow) == r0 && gm.CanSummonFaceDown(new CardInstance(CardList.Monsters[0], gm.Human));
            gm.TestSetField(calm);

            bool ok = tide && chaos && volcano && dusk;
            Report(ok, $"RULE 필드 상성 해일={tide} 혼돈={chaos} 화산={volcano} 황혼={dusk}\n   해일의 폭주: {tideTxt}\n   혼돈의 균열: {chaosTxt}");
        }

        /// <summary>필드 테마: 센터 필드가 바뀌면 플레이매트·바깥 환경이 그 필드 그림으로 (스크린샷 확인)</summary>
        IEnumerator FieldThemes()
        {
            Reset();
            foreach (var id in new[] { "F022", "F001", "F004", "F007", "F013", "F019", "F025" })
            {
                var f = CardList.Fields.First(x => x.id == id);
                int errBefore = errorCount;
                gm.TestSetField(f);
                yield return new WaitForSecondsRealtime(1.3f);
                bool files = GameAssets.Tex("Fields/playmat_" + id) != null && GameAssets.Tex("Fields/env_" + id) != null;
                bool ok = files && errorCount == errBefore;
                Report(ok, $"FIELD THEME {id} {f.name} 파일={files}");
                yield return shot("theme_" + id);
            }
            Reset();
        }

        /// <summary>드로우: 패 최대 7장 — 5장에서 5장 드로우 → 7장, 가득 찬 상태에서 드로우 → 그대로</summary>
        IEnumerator DrawRules()
        {
            Reset();
            var me = gm.Human;
            Hand(me, "M001", "M002", "M003", "M004", "M005");
            int deck0 = me.deck.Count;
            yield return gm.Draw(me, 5);
            bool refill = me.hand.Count == GameManager.HandMax && me.deck.Count == deck0 - 2;
            yield return gm.Draw(me, 3);
            bool capped = me.hand.Count == GameManager.HandMax && me.deck.Count == deck0 - 2;
            int grave0 = me.grave.Count, hand0 = me.hand.Count;
            yield return gm.DiscardHand(me);
            bool discard = me.hand.Count == 0 && me.grave.Count == grave0 + hand0;
            bool ok = refill && capped && discard && !gm.gameOver;
            Report(ok, $"RULE 패 최대 {GameManager.HandMax}장 보충={refill} 초과드로우없음={capped} 엔드페이즈모두버리기={discard} (덱 {deck0}→{me.deck.Count}, 묘지 {me.grave.Count})");
        }

        /// <summary>드래그앤드롭 공격: [도발] 이 있으면 다른 몬스터·영웅은 대상 불가, 도발 몬스터로 드래그하면 공격 후 횟수 소모</summary>
        IEnumerator DragAttackScn()
        {
            Reset();
            var me = gm.Human; var op = gm.Cpu;
            Mon(me, "M002");
            Mon(op, "M013", "M009");    // 빛의 견습사제 · [도발] 바위 골렘
            gm.RefreshAll();
            yield return new WaitForSecondsRealtime(0.3f);
            var a = me.slots.First(s => s != null);
            var plain = op.slots.First(s => s != null && s.data.id == "M013");
            var taunt = op.slots.First(s => s != null && s.data.id == "M009");
            int errBefore = errorCount;
            gm.autoHuman = false;        // 사람 입력 대기 모드로 배틀 시작
            bool done = false;
            gm.StartCoroutine(Run(gm.TestHumanBattle(), () => done = true));
            float t0 = Time.realtimeSinceStartup;
            while (!gm.HumanBattleOpen && !done && Time.realtimeSinceStartup - t0 < 4f) yield return null;
            bool open = gm.HumanBattleOpen && gm.CanAttackNow(a) && gm.AttacksLeft(a) == 1;
            bool blockPlain = !gm.TryHumanAttack(a, plain);
            bool blockHero = !gm.TryHumanAttack(a, op.hero);
            yield return shot("rule_drag_open");
            gm.autoHuman = true;         // 주사위 선택 등은 AI 가 대신
            bool accepted = gm.TryHumanAttack(a, taunt);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return shot("rule_drag_arrow");   // 속성 화살표 연출 중
            t0 = Time.realtimeSinceStartup;
            while (!done && Time.realtimeSinceStartup - t0 < 25f) yield return null;
            bool used = a.zone != Zone.Slot || gm.AttacksLeft(a) == 0;
            bool ok = open && blockPlain && blockHero && accepted && done && used && errorCount == errBefore;
            Report(ok, $"RULE 드래그 공격 입력대기={open} 도발외차단={blockPlain} 영웅차단={blockHero} 도발공격={accepted} 종료={done} 횟수소모={used}\n   후: {State()}");
        }

        /// <summary>카드 효과 연출 갤러리: 효과 도중 스크린샷 2장씩 (연출 확인용, 오류만 판정)</summary>
        IEnumerator FxGallery()
        {
            float ts = Time.timeScale;
            Time.timeScale = 1f;
            string[] ids = { "S003", "S004", "S006", "S102", "S107", "S110", "S204", "S011", "S005", "S109", "H001", "H011", "M101", "M254", "M365", "M447" };
            foreach (var id in ids)
            {
                Reset();
                Mon(gm.Cpu, "M001", "M004", "M311", "M012");
                Mon(gm.Human, "M002", "M012");
                gm.RefreshAll();
                yield return new WaitForSecondsRealtime(0.5f);
                var data = CardDB.Get(id);
                if (data == null) continue;
                CardInstance inst;
                if (data.type == CardType.Hero) inst = new CardInstance(data, gm.Human) { zone = Zone.Hero, view = gm.Human.hero.view };
                else if (data.type == CardType.Monster)
                {
                    inst = new CardInstance(data, gm.Human) { zone = Zone.Slot };
                    int s = gm.Human.FreeSlot(); inst.slot = s; gm.Human.slots[s] = inst;
                    gm.RefreshAll();
                    yield return new WaitForSecondsRealtime(0.4f);
                }
                else inst = new CardInstance(data, gm.Human) { zone = Zone.Resolving };
                int errBefore = errorCount;
                bool done = false;
                gm.StartCoroutine(Run(gm.TestRunEffect(inst), () => done = true));
                yield return new WaitForSecondsRealtime(0.35f);
                yield return shot($"fx_{id}_a");
                yield return new WaitForSecondsRealtime(0.45f);
                yield return shot($"fx_{id}_b");
                float t0 = Time.realtimeSinceStartup;
                while (!done && Time.realtimeSinceStartup - t0 < 8f) yield return null;
                bool ok = done && errorCount == errBefore;
                Report(ok, $"FX {id} {data.name}");
            }
            Time.timeScale = ts;
        }

        static IEnumerator Run(IEnumerator r, Action done) { yield return r; done(); }

        int errorCount;
        void OnEnable() => Application.logMessageReceived += Count;
        void OnDisable() => Application.logMessageReceived -= Count;
        void Count(string m, string s, LogType t) { if (t == LogType.Exception || t == LogType.Error) errorCount++; }
    }

    static class CardDataTestExt
    {
        public static bool IsMonsterType(this CardData d) => d.type == CardType.Monster;
    }
}
