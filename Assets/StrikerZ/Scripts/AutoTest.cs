using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 자동 테스트 모드. 실행 인자 -strikerz-autotest [-strikerz-out 폴더]
    /// 양쪽 모두 AI 가 플레이하며 스크린샷과 로그를 남기고 종료한다.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        public static bool Enabled => Environment.GetCommandLineArgs().Contains("-strikerz-autotest");

        GameRoot root;
        GameManager gm;
        string outDir, logPath;
        int shot;
        int errors;

        public void Init(GameRoot r, GameManager g)
        {
            root = r;
            gm = g;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-strikerz-out");
            outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autotest");
            Directory.CreateDirectory(outDir);
            logPath = Path.Combine(outDir, "autotest_log.txt");
            File.WriteAllText(logPath, $"StrikerZ autotest {DateTime.Now}\n");
            Application.logMessageReceived += OnLog;
            // 테스트 창이 포커스를 잃어도(사용자가 다른 창 사용 중) 멈추지 않게
            Application.runInBackground = true;
            StartCoroutine(Run());
        }

        void OnLog(string msg, string stack, LogType type)
        {
            bool bad = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            if (bad) errors++;
            if (bad || msg.StartsWith("[StrikerZ]"))
                File.AppendAllText(logPath, $"[{Time.realtimeSinceStartup:F1}] {type}: {msg}\n{(bad ? stack + "\n" : "")}");
        }

        /// <summary>
        /// 스크린샷: ScreenCapture.CaptureScreenshot 은 이 환경에서 메인 스레드를 수십 초 멈추게 해서
        /// 렌더 텍스처 → 비동기 GPU 읽기 → 백그라운드 스레드에서 PNG 인코딩·저장으로 바꿨다.
        /// </summary>
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(outDir, $"{++shot:00}_{name}.png");
            int w = Screen.width, h = Screen.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
            var req = UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32);
            float t0 = Time.realtimeSinceStartup;
            while (!req.done && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            RenderTexture.ReleaseTemporary(rt);
            if (!req.done || req.hasError) yield break;
            var data = req.GetData<byte>().ToArray();
            bool flip = SystemInfo.graphicsUVStartsAtTop;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (flip)
                    {
                        int row = w * 4;
                        var tmp = new byte[row];
                        for (int y = 0; y < h / 2; y++)
                        {
                            Buffer.BlockCopy(data, y * row, tmp, 0, row);
                            Buffer.BlockCopy(data, (h - 1 - y) * row, data, y * row, row);
                            Buffer.BlockCopy(tmp, 0, data, (h - 1 - y) * row, row);
                        }
                    }
                    var png = ImageConversion.EncodeArrayToPNG(data, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h);
                    File.WriteAllBytes(path, png);
                }
                catch (Exception) { }
            });
            yield return null;
        }

        /// <summary>스토리 자동 테스트: 제1장 7스테이지를 컷신(자동 넘김) → 전투(AI) → 후 컷신까지 진행</summary>
        IEnumerator StoryRun()
        {
            Profile.Reset();
            var menu = FindFirstObjectByType<MainMenu>();
            root.TestMenu(MainMenu.Tab.Story);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("story_menu");
            StoryPlayer.AutoAdvance = true;
            var vol = StoryData.Volumes[0];
            for (int i = 0; i < vol.stages.Count; i++)
            {
                var st = vol.stages[i];
                Time.timeScale = 1f;
                StoryPlayer.AutoAdvance = i > 1;            // 1·2 스테이지는 대사 화면을 찍으려고 천천히
                gm.autoHuman = i != 0;                       // 1스테이지만 사람 입력 대기(코치 팁 확인용)
                root.TestStory(st);
                float t0 = Time.realtimeSinceStartup;
                bool shotScene = false;
                while (root.TestStoryPlaying || gm.story != st || gm.time == 0)
                {
                    if (!shotScene && root.TestStoryPlaying && Time.realtimeSinceStartup - t0 > 2.2f) { shotScene = true; yield return Shot($"story{i + 1}_scene"); StoryPlayer.AutoAdvance = true; }
                    if (Time.realtimeSinceStartup - t0 > 120f) break;
                    yield return null;
                }
                // 1스테이지: 코치 팁이 뜬 메인 페이즈를 찍고 AI 에 넘긴다
                if (i == 0)
                {
                    gm.autoHuman = false;
                    float t1 = Time.realtimeSinceStartup;
                    while (!(gm.phase == Phase.Main && gm.active == gm.Human && !gm.busy) && Time.realtimeSinceStartup - t1 < 30f) yield return null;
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Shot("story1_coach");
                    // 영웅 3D 확대 확인: 게임 카메라를 잠시 카일 쪽으로 좁혀 찍는다
                    {
                        var cam = gm.board.cam;
                        var pos0 = cam.transform.position; var rot0 = cam.transform.rotation; float fov0 = cam.fieldOfView;
                        var hp = gm.board.HeroPos(gm.Human);
                        cam.fieldOfView = 12f;
                        cam.transform.LookAt(hp + Vector3.up * 0.6f);
                        yield return new WaitForSecondsRealtime(0.3f);
                        yield return Shot("story1_kai_zoom");
                        cam.transform.position = pos0; cam.transform.rotation = rot0; cam.fieldOfView = fov0;
                    }
                    gm.autoHuman = true;
                    gm.RequestBattle();
                }
                gm.autoHuman = true;
                int tries = 0;
                while (true)
                {
                    Time.timeScale = 5f;
                    float tb = Time.realtimeSinceStartup;
                    while (!gm.MatchFinished && Time.realtimeSinceStartup - tb < 300f) yield return null;
                    Time.timeScale = 1f;
                    yield return new WaitForSecondsRealtime(0.8f);
                    bool won = gm.winner == gm.Human;
                    File.AppendAllText(logPath, $"STORY {st.id} {st.title} try={tries + 1} result={(gm.gameOver ? (won ? "WIN" : "LOSE") : "TIMEOUT")} time={gm.time} me HP{gm.Human.hp} opp HP{gm.Cpu.hp}/{gm.Cpu.maxHp} proceeds={gm.StoryProceeds}\n");
                    if (i == 0 || i == 3 || i == 6) yield return Shot($"story{i + 1}_result");
                    if (gm.StoryProceeds || tries >= 2) break;
                    tries++;
                    root.TestStoryContinue();      // 패배: 다시 도전
                    float tr = Time.realtimeSinceStartup;
                    while ((gm.gameOver || gm.time == 0) && Time.realtimeSinceStartup - tr < 30f) yield return null;
                    gm.autoHuman = true;
                }
                if (!gm.StoryProceeds) continue;
                root.TestStoryContinue();          // 후 컷신 → 메뉴
                float tp = Time.realtimeSinceStartup;
                yield return new WaitForSecondsRealtime(0.5f);
                while (root.TestStoryPlaying && Time.realtimeSinceStartup - tp < 120f) yield return null;
                File.AppendAllText(logPath, $"STORY {st.id} cleared={Profile.StoryCleared(st.id)} opal={Profile.Data.opal}\n");
            }
            StoryPlayer.AutoAdvance = false;
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("story_menu_after");
            File.AppendAllText(logPath, $"STORY SUMMARY cleared={vol.stages.Count(x => Profile.StoryCleared(x.id))}/{vol.stages.Count} kaiUnlocked={StoryData.HeroAvailable(StoryData.Kai)}\n");
        }

        /// <summary>
        /// 스토리 전 장 자동 테스트 (-strikerz-story-all [-strikerz-chapters 2-10]):
        /// 장마다 스테이지 7개를 컷신(자동) → 전투(AI, 최대 3번 도전) → 후 컷신 순으로 진행하고 장면 · 전투 · 메뉴를 찍는다.
        /// </summary>
        IEnumerator StoryAllRun()
        {
            var args = Environment.GetCommandLineArgs();
            int from = 2, to = StoryData.Volumes.Count;
            int ai = Array.IndexOf(args, "-strikerz-chapters");
            if (ai >= 0 && ai + 1 < args.Length)
            {
                var r = args[ai + 1].Split('-');
                from = int.Parse(r[0]); to = int.Parse(r[r.Length - 1]);
            }
            Profile.Reset();
            var menu = FindFirstObjectByType<MainMenu>();
            StoryPlayer.AutoAdvance = true;
            for (int c = from; c <= to; c++)
            {
                var vol = StoryData.Volumes[c - 1];
                for (int i = 0; i < vol.stages.Count; i++)
                {
                    var st = vol.stages[i];
                    Time.timeScale = 1f;
                    gm.autoHuman = true;
                    root.TestStory(st);
                    float t0 = Time.realtimeSinceStartup;
                    bool shotScene = false;
                    while (root.TestStoryPlaying || gm.story != st || gm.time == 0)
                    {
                        if (!shotScene && root.TestStoryPlaying && Time.realtimeSinceStartup - t0 > 1.4f) { shotScene = true; yield return Shot($"ch{c}_s{i + 1}_scene"); }
                        if (Time.realtimeSinceStartup - t0 > 150f) break;
                        yield return null;
                    }
                    int tries = 0;
                    bool shotBattle = !(i == 0 || i == 6 || (c == 9 && i == 5));
                    while (true)
                    {
                        Time.timeScale = 6f;
                        float tb = Time.realtimeSinceStartup;
                        while (!gm.MatchFinished && Time.realtimeSinceStartup - tb < 300f)
                        {
                            if (!shotBattle && gm.time >= 4 && !gm.busy)
                            {
                                shotBattle = true;
                                Time.timeScale = 1f;
                                yield return Shot($"ch{c}_s{i + 1}_battle");
                                Time.timeScale = 6f;
                            }
                            yield return null;
                        }
                        Time.timeScale = 1f;
                        yield return new WaitForSecondsRealtime(0.6f);
                        bool won = gm.winner == gm.Human;
                        File.AppendAllText(logPath, $"STORY {st.id} {st.title} try={tries + 1} result={(gm.gameOver ? (won ? "WIN" : "LOSE") : "TIMEOUT")} time={gm.time} me HP{gm.Human.hp}/{gm.Human.maxHp} opp HP{gm.Cpu.hp}/{gm.Cpu.maxHp} proceeds={gm.StoryProceeds}\n");
                        if (gm.StoryProceeds || tries >= 2) break;
                        tries++;
                        root.TestStoryContinue();      // 패배: 다시 도전
                        float tr = Time.realtimeSinceStartup;
                        while ((gm.gameOver || gm.time == 0) && Time.realtimeSinceStartup - tr < 30f) yield return null;
                        gm.autoHuman = true;
                    }
                    if (!gm.StoryProceeds) { File.AppendAllText(logPath, $"STORY {st.id} FAILED\n"); root.TestMenu(MainMenu.Tab.Story); yield return new WaitForSecondsRealtime(0.5f); continue; }
                    root.TestStoryContinue();          // 후 컷신 → 메뉴
                    float tp = Time.realtimeSinceStartup;
                    yield return new WaitForSecondsRealtime(0.5f);
                    while (root.TestStoryPlaying && Time.realtimeSinceStartup - tp < 150f) yield return null;
                }
                root.TestMenu(MainMenu.Tab.Story);
                yield return new WaitForSecondsRealtime(0.5f);
                if (menu != null) menu.TestStorySelect(0, c - 1);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot($"ch{c}_menu");
                File.AppendAllText(logPath, $"CHAPTER {c} cleared={vol.stages.Count(x => Profile.StoryCleared(x.id))}/{vol.stages.Count} opal={Profile.Data.opal}\n");
            }
            StoryPlayer.AutoAdvance = false;
            File.AppendAllText(logPath, "STORY HEROES " + string.Join(" ", StoryData.StoryHeroes.Select(h => $"{h.id}:{(StoryData.HeroAvailable(h) ? "O" : "x")}")) + "\n");
        }

        /// <summary>성능 측정 (-strikerz-perf): 덱 탭 진입 · 덱 갱신(카드 추가/제거 경로) 시간을 다음 프레임 레이아웃까지 포함해 잰다</summary>
        IEnumerator PerfRun()
        {
            var menu = FindFirstObjectByType<MainMenu>();
            root.TestMenu(MainMenu.Tab.Home);
            yield return new WaitForSecondsRealtime(3f);           // 홈에 있는 동안 덱 셀 미리 만들기
            for (int i = 0; i < 3; i++)
            {
                yield return Measure("deck_tab#" + (i + 1), () => root.TestMenu(MainMenu.Tab.Deck), menu);
                yield return new WaitForSecondsRealtime(0.6f);
                if (i == 0) yield return Shot("perf_deck");
                root.TestMenu(MainMenu.Tab.Home);
                yield return new WaitForSecondsRealtime(0.6f);
            }
            root.TestMenu(MainMenu.Tab.Deck);
            yield return new WaitForSecondsRealtime(1f);
            for (int i = 0; i < 3; i++)
            {
                yield return Measure("deck_refresh#" + (i + 1), menu.TestRefreshDeck, menu);
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        /// <summary>호출 시간 · 다 그려질 때까지 시간 · 그동안 가장 긴 프레임</summary>
        IEnumerator Measure(string name, Action act, MainMenu menu)
        {
            float t0 = Time.realtimeSinceStartup;
            act();
            float call = Time.realtimeSinceStartup - t0, last = Time.realtimeSinceStartup, worst = call;
            int frames = 0;
            do
            {
                yield return null;
                float now = Time.realtimeSinceStartup;
                worst = Mathf.Max(worst, now - last);
                last = now;
                frames++;
            } while ((menu.TestDeckBusy || frames < 2) && Time.realtimeSinceStartup - t0 < 10f);
            File.AppendAllText(logPath, $"PERF {name} call={call * 1000:F0}ms done={(Time.realtimeSinceStartup - t0) * 1000:F0}ms worstFrame={worst * 1000:F0}ms frames={frames}\n");
        }

        /// <summary>레이드 자동 테스트: 로비 스크린샷 → 보스 × 파티 인원 조합을 AI 가 끝까지 플레이</summary>
        IEnumerator RaidRun()
        {
            Profile.Reset();
            var menu = FindFirstObjectByType<MainMenu>();
            root.TestMenu(MainMenu.Tab.Raid);
            menu.TestRaidSelect(0, 3, true);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("raid_lobby");
            menu.TestRaidSelect(6, 1, false);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("raid_lobby_dark");
            var args = Environment.GetCommandLineArgs();
            var runs = new (int boss, int party)[] { (0, 1), (4, 3), (5, 2), (6, 3), (1, 1), (2, 3), (3, 2), (0, 3), (3, 1), (6, 1) };
            int wins = 0;
            for (int r = 0; r < runs.Length; r++)
            {
                var (bi, party) = runs[r];
                gm.autoHuman = true;
                Time.timeScale = 5f;
                int opal0 = Profile.Data.opal;
                root.TestRaid(bi, party, true);
                float t0 = Time.realtimeSinceStartup;
                bool shotMid = false, shotAlly = false;
                while (!gm.gameOver && Time.realtimeSinceStartup - t0 < 300f)
                {
                    if (r < 3 && !shotMid && gm.time >= 3 && gm.active == gm.Human && gm.phase == Phase.Main)
                    {
                        shotMid = true;
                        yield return Shot($"raid{r}_board");
                    }
                    if (r < 3 && party > 1 && !shotAlly && gm.time >= 5 && gm.active == gm.Human && gm.phase == Phase.Main)
                    {
                        shotAlly = true;
                        gm.hud.SetRaidView(1);
                        yield return new WaitForSecondsRealtime(0.5f);
                        yield return Shot($"raid{r}_ally1");
                        if (party > 2) { gm.hud.SetRaidView(2); yield return new WaitForSecondsRealtime(0.5f); yield return Shot($"raid{r}_ally2"); }
                        gm.hud.SetRaidView(0);
                    }
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(1.2f);
                var rs = gm.raid;
                bool won = gm.winner == gm.Human;
                if (won) wins++;
                string allies = rs == null ? "" : string.Join(", ", rs.allies.Select(a => $"{a.name}({a.hero.name}) HP{a.hp}/{a.maxHp} 피해{a.dealt}"));
                File.AppendAllText(logPath, $"RAID {r} {rs?.boss.name} party={party} result={(gm.gameOver ? (won ? "CLEAR" : "FAIL") : "TIMEOUT")} time={gm.time} bossHP={gm.Cpu.hp}/{gm.Cpu.maxHp} me HP{gm.Human.hp} 피해{rs?.humanDealt} 덱{gm.Human.deck.Count} key={rs?.keyStrikes} opal+{Profile.Data.opal - opal0} 도감{Profile.OwnedKinds} | {allies}\n");
                // 레이드 필드 고정: 필드 카드 사용 불가 · 효과로도 변경 불가
                if (rs != null)
                {
                    var bossField = RaidData.BossField(rs.boss);
                    bool kept = gm.center != null && gm.center.data == bossField;
                    var probe = new CardInstance(CardList.Fields[(CardList.Fields.IndexOf(bossField) + 3) % CardList.Fields.Count], gm.Human) { zone = Zone.Hand };
                    bool blocked = !gm.CanPlayField(probe);
                    yield return gm.ChangeField(probe.data, "TEST");
                    bool stays = gm.center != null && gm.center.data == bossField;
                    bool noFieldInRental = !RaidData.RecommendedDeck(rs.boss, new System.Random(r)).Any(c => c.type == CardType.Field);
                    File.AppendAllText(logPath, $"RAID {r} FIELD 유지={kept} 사용차단={blocked} 효과변경차단={stays} 대여덱필드없음={noFieldInRental}\n");
                }
                if (r < 2) yield return Shot($"raid{r}_end");
            }
            File.AppendAllText(logPath, $"RAID SUMMARY wins={wins}/{runs.Length}\n");
        }

        /// <summary>모드 실행 끝: 오류 수를 남기고 종료</summary>
        void Finish()
        {
            File.AppendAllText(logPath, $"DONE errors={errors}\n");
            Application.Quit();
        }

        /// <summary>로그 한 줄 (같은 문자열을 돌려줘서 식 안에서도 쓸 수 있다)</summary>
        string L(string s) { File.AppendAllText(logPath, s + "\n"); return s; }

        IEnumerator Run()
        {
            float start = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("title");
            if (Environment.GetCommandLineArgs().Contains("-strikerz-perf"))
            {
                yield return PerfRun();
                Finish();
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-strikerz-story-all"))
            {
                yield return StoryAllRun();
                Finish();
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-strikerz-story"))
            {
                yield return StoryRun();
                Finish();
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-strikerz-raid"))
            {
                yield return RaidRun();
                Finish();
                yield break;
            }
            if (ScenarioTest.Enabled)
            {
                Time.timeScale = 3f;
                yield return gameObject.AddComponent<ScenarioTest>().Run(root, gm, logPath, Shot);
                Finish();
                yield break;
            }
            // 메인 허브 투어 (테스트용 저장 파일 사용)
            Profile.Reset();
            foreach (var tab in new[] { MainMenu.Tab.Home, MainMenu.Tab.Deck, MainMenu.Tab.Shop, MainMenu.Tab.Settings })
            {
                root.TestMenu(tab);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("menu_" + tab);
            }
            int gemsBefore = Profile.Data.opal, kindsBefore = Profile.OwnedKinds;
            var menu = FindFirstObjectByType<MainMenu>();
            // 팩 개봉 연출: 1팩(마지막 장 신화 고정) → 단계별 스크린샷
            MainMenu.TestForceRarity = Rarity.Unique;
            // 가공 확인: 첫 장 StrikerZ, 둘째 장 Seven
            MainMenu.TestForceFinish = new[] { CardFinish.StrikerZ, CardFinish.Seven };
            root.TestBuy(1);
            float openStart = Time.realtimeSinceStartup;
            string lastStage = "";
            float nextOpenShot = 0;
            int openShots = 0;
            while (menu.Opening.Stage != "summary" && Time.realtimeSinceStartup - openStart < 60f)
            {
                var st = menu.Opening.Stage;
                if ((st != lastStage || Time.realtimeSinceStartup > nextOpenShot) && openShots < 48)
                {
                    lastStage = st;
                    nextOpenShot = Time.realtimeSinceStartup + 0.55f;
                    openShots++;
                    yield return Shot("pack_" + st);
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("pack_summary");
            File.AppendAllText(logPath, $"OPENING took {Time.realtimeSinceStartup - openStart:F1}s shots={openShots}\n");
            menu.TestContinueOpening();
            yield return new WaitForSecondsRealtime(0.5f);
            if (Environment.GetCommandLineArgs().Contains("-strikerz-packonly"))
            {
                Finish();
                yield break;
            }
            File.AppendAllText(logPath, $"SHOP opal {gemsBefore}->{Profile.Data.opal} kinds {kindsBefore}->{Profile.OwnedKinds} packs={Profile.Data.packsOpened}\n");
            root.TestMenu(MainMenu.Tab.Profile);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("menu_Profile");
            root.TestMenu(MainMenu.Tab.Deck);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("menu_Deck_finish");

            // ── 분해 · 생성 · 잠금 · 덱 코드 ──
            {
                var m001 = CardDB.Get("M001");
                Profile.AddCard("M001", 5);                                  // 초과분 만들기
                var sur = Crafting.Surplus();
                int cpN0 = Crafting.CP(Rarity.Common);
                int gained = Crafting.DismantleSurplus();
                L($"CRAFT surplus={sur.Sum(x => x.n)} gained={gained} C_CP {cpN0}->{Crafting.CP(Rarity.Common)} M001 owned={Profile.Owned("M001")}");
                // 잠금: 가공 사본은 자동 잠금 → 분해 불가, 해제하면 가능
                Profile.AddCard("M002", 3, true, CardFinish.Seven);
                int lockedCan = Crafting.Dismantlable(CardDB.Get("M002"), CardFinish.Seven);
                bool autoLocked = Crafting.IsLocked("M002", CardFinish.Seven);
                Crafting.SetLock("M002", CardFinish.Seven, false);
                int unlockedCan = Crafting.Dismantlable(CardDB.Get("M002"), CardFinish.Seven);
                bool okDis = Crafting.Dismantle(CardDB.Get("M002"), CardFinish.Seven, 1, out int g2, out string why2);
                L($"LOCK auto={autoLocked} dismantlable locked={lockedCan} unlocked={unlockedCan} dismantle={okDis}({g2} {why2}) C_CP={Crafting.CP(Rarity.Common)}");
                // 생성: 미보유 카드 하나를 CP 로
                var target = CardDB.All.FirstOrDefault(c => Profile.Owned(c.id) == 0 && c.rarity == Rarity.Uncommon);
                if (target != null)
                {
                    Profile.Data.cp[(int)Rarity.Uncommon] += 30;
                    bool crafted = Crafting.Craft(target, out string why3);
                    L($"CRAFT {target.name} ok={crafted} {why3} owned={Profile.Owned(target.id)} UC_CP={Crafting.CP(Rarity.Uncommon)}");
                }
                // 덱 코드 왕복
                var deck = Profile.ActiveDeck;
                string code = Crafting.ExportDeck(deck);
                var back = Crafting.ParseDeck(code, "t");
                bool same = back != null && back.heroId == deck.heroId && back.cards.OrderBy(x => x).SequenceEqual(deck.cards.OrderBy(x => x));
                L($"DECKCODE len={code.Length} roundtrip={same} missing={Crafting.Missing(back).Count}");
                {
                    // 덱 매수 30~40 · 필드 최대 7장 규칙
                    var t = new DeckData { heroId = "H001" };
                    foreach (var f in CardList.Fields.Take(8)) Profile.AddCard(f.id, 1, false, CardFinish.Normal);
                    int fieldsAdded = 0;
                    foreach (var f in CardList.Fields.Take(8)) if (DeckRules.CanAdd(t, f, out _)) { t.cards.Add(f.id); fieldsAdded++; }
                    DeckRules.CanAdd(t, CardList.Fields[7], out string whyField);
                    var fill = CardDB.All.Where(c => c.type != CardType.Field && Profile.Owned(c.id) > 0).ToList();
                    for (int k = 0; k < 60 && t.cards.Count < 45; k++) foreach (var c in fill) if (DeckRules.CanAdd(t, c, out _)) t.cards.Add(c.id);
                    L($"DECKRULE fields={fieldsAdded} (8th: {whyField}) maxCards={t.cards.Count} valid={DeckRules.Validate(t) ?? "OK"}");
                    t.cards.RemoveRange(29, t.cards.Count - 29);
                    L($"DECKRULE 29cards valid={DeckRules.Validate(t) ?? "OK"}");
                }
                root.TestMenu(MainMenu.Tab.Deck);
                yield return new WaitForSecondsRealtime(0.5f);
                menu.TestPin("M002");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("menu_Deck_craft");
            }
            // ── 로지의 상점: 오늘의 특가 · 마력부여기 ──
            {
                ShopDaily.Roll(new System.Random(5), new[] { CardFinish.Normal, CardFinish.Seven, CardFinish.StrikerZ });
                var offers = ShopDaily.Offers;
                L("SHOP offers " + string.Join(" | ", offers.Select(o => $"{o.rarity} {CardDB.Get(o.id).name} {o.finish} price={ShopDaily.Price(o)}")));
                foreach (var r in ShopDaily.SlotRarities) Profile.Data.cp[(int)r] = 0;
                bool lackOk = ShopDaily.Buy(offers[0], out string whyLack);
                foreach (var r in ShopDaily.SlotRarities) Profile.Data.cp[(int)r] = 6000;
                int bought = 0;
                var before = offers.Select(o => (o.id, Profile.FinishCount(o.id, o.finish))).ToList();
                foreach (var o in offers.Take(2)) if (ShopDaily.Buy(o, out _)) bought++;
                bool again = ShopDaily.Buy(offers[0], out string whySold);
                L($"SHOP lack={lackOk}({whyLack}) bought={bought} again={again}({whySold}) " +
                  string.Join(" ", offers.Take(2).Select((o, i) => $"{o.id}:{o.finish} {before[i].Item2}->{Profile.FinishCount(o.id, o.finish)}")) +
                  $" CP C={Crafting.CP(Rarity.Common)} UC={Crafting.CP(Rarity.Uncommon)} R={Crafting.CP(Rarity.Rare)}");
                root.TestMenu(MainMenu.Tab.Shop);
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Shot("menu_Shop_specials");
                // 마력부여기: 일반 → Seven → StrikerZ
                var ec = CardDB.All.First(c => c.type == CardType.Monster && Profile.FinishCount(c.id, CardFinish.Normal) >= 2);
                Profile.Data.cp[(int)ec.rarity] = 11500;
                string Counts() => $"normal={Profile.FinishCount(ec.id, CardFinish.Normal)} seven={Profile.FinishCount(ec.id, CardFinish.Seven)} strikerz={Profile.FinishCount(ec.id, CardFinish.StrikerZ)} CP={Crafting.CP(ec.rarity)}";
                L($"ENCHANT {ec.name}({ec.rarity}) before {Counts()}");
                menu.TestOpenEnchanter(ec.id);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shot("menu_Enchant");
                menu.TestEnchant(CardFinish.Seven);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return Shot("menu_Enchant_charge");
                yield return new WaitForSecondsRealtime(1.4f);
                L($"ENCHANT seven {Counts()}");
                menu.TestEnchant(CardFinish.StrikerZ);
                yield return new WaitForSecondsRealtime(2.8f);
                yield return Shot("menu_Enchant_strikerz");
                bool noCp = ShopDaily.CanEnchant(ec, CardFinish.StrikerZ, out string whyCp);
                L($"ENCHANT strikerz {Counts()} canAgain={noCp}({whyCp})");
            }
            root.TestMenu(MainMenu.Tab.Quest);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("menu_Quest");
            File.AppendAllText(logPath, $"QUEST claimable={Quests.ClaimableCount} opal={Profile.Data.opal}\n");
            int got = Quests.ClaimAll();
            File.AppendAllText(logPath, $"QUEST claimed +{got} -> opal={Profile.Data.opal} claimable={Quests.ClaimableCount}\n");
            File.AppendAllText(logPath, $"DECK starter valid={DeckRules.Validate(Profile.ActiveDeck) ?? "OK"} cards={Profile.ActiveDeck.cards.Count}\n");

            gm.autoHuman = true;
            CardList.TestAllMythics = Environment.GetCommandLineArgs().Contains("-strikerz-mythic");
            Time.timeScale = 2.5f;
            root.TestDuel();

            yield return new WaitForSecondsRealtime(3f);
            // 주사위 구간 선택 UI 확인용: 실제로 주사위를 굴린 상태에서 선택창을 띄운다
            int[] rolled = null;
            yield return gm.dice.RollBattle(false, false, true, (x, y) => rolled = x);
            var act = new bool[3]; var faces = new string[3];
            for (int b = 0; b < 3; b++)
            {
                var f = rolled.Where(v => Dice.Band(v) == b).ToList();
                act[b] = f.Count > 0; faces[b] = string.Join(", ", f);
            }
            File.AppendAllText(logPath, $"DICE rolled={string.Join(",", rolled)}\n");
            HUD.I.ShowBandChoice("빛의 견습사제 — 효과를 선택하세요", new[] { "HP 3 회복", "HP 1 회복 · 1장 드로우", "파괴되지 않음" },
                act, faces, false, _ => { });
            yield return new WaitForEndOfFrame();
            Time.timeScale = 0f;
            yield return Shot("band_choice");
            Time.timeScale = 2.5f;
            HUD.I.HideChoice();
            gm.dice.Clear();
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (var pl in gm.players)
            {
                var all = pl.deck.Concat(pl.hand).ToList();
                int mm = all.Count(c => c.data.rarity == Rarity.Unique && c.IsMonster);
                int ms = all.Count(c => c.data.rarity == Rarity.Unique && c.IsMagic);
                File.AppendAllText(logPath, $"DECK {pl.name}: cards={all.Count} mythicMonster={mm} mythicMagic={ms}\n");
            }
            // 영웅 3D 모델: 카드 위 표시 + 공격/피격 모션 확인 (근접 카메라)
            {
                var hmH = HeroModel.Of(gm.Human.hero);
                var hmC = HeroModel.Of(gm.Cpu.hero);
                File.AppendAllText(logPath, $"HERO3D human={gm.Human.hero.data.id}:{hmH != null} cpu={gm.Cpu.hero.data.id}:{hmC != null}\n");
                var cam = Camera.main;
                var cp = cam.transform.position; var cr = cam.transform.rotation; float fov = cam.fieldOfView;
                yield return new WaitForSecondsRealtime(0.6f);
                cam.transform.position = new Vector3(-2.2f, 2.3f, -3.6f);
                cam.transform.LookAt(new Vector3(-4.15f, 0.45f, 0f));
                cam.fieldOfView = 48;
                yield return Shot("hero3d_idle");
                HeroModel.PlayAttack(gm.Human.hero, gm.board.HeroPos(gm.Cpu));
                HeroModel.PlayAttack(gm.Cpu.hero, gm.board.HeroPos(gm.Human));
                yield return new WaitForSecondsRealtime(0.45f);
                yield return Shot("hero3d_attack");
                yield return new WaitForSecondsRealtime(1.6f);
                HeroModel.PlayHit(gm.Human.hero);
                HeroModel.PlayHit(gm.Cpu.hero);
                yield return new WaitForSecondsRealtime(0.18f);
                yield return Shot("hero3d_hit");
                cam.transform.position = cp; cam.transform.rotation = cr; cam.fieldOfView = fov;
            }
            int matches = 0, clashShots = 0;
            bool targetShot = false;
            float lastClash = 0;
            float nextShot = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup - start < 420f)
            {
                if (gm.gameOver && gm.phase == Phase.None && GameObject.Find("GameOver") != null && GameObject.Find("GameOver").activeInHierarchy)
                {
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Shot($"gameover_match{matches + 1}");
                    File.AppendAllText(logPath, $"MATCH {matches + 1} END: winner={gm.winner?.name} time={gm.time} hp={gm.Human.hp}/{gm.Cpu.hp}\n");
                    matches++;
                    if (matches >= 2) break;
                    root.TestRematch();
                    yield return new WaitForSecondsRealtime(1.5f);
                    yield return Shot("rematch_field");
                    root.TestPickFieldIfOpen();
                    nextShot = Time.realtimeSinceStartup + 5f;
                }
                // 공격 대상 모드 UI 확인 (선택창 없이 드래그/단축키)
                if (!targetShot && !gm.gameOver && !gm.ChoicePending && gm.Human != null && gm.Cpu.Monsters().Count > 0 && gm.Human.Monsters().Count > 0)
                {
                    targetShot = true;
                    HUD.I.ShowTargetMode(gm.Human.Monsters()[0], gm.Cpu.Monsters(), $"{gm.Human.Monsters()[0].Name}의 공격 대상 선택  (자유 대상)", _ => { });
                    float ts = Time.timeScale;
                    Time.timeScale = 0f;
                    yield return null;
                    yield return Shot("target_mode");
                    HUD.I.HideTargetMode();
                    Time.timeScale = ts;
                    File.AppendAllText(logPath, $"TARGET mode shown for {gm.Cpu.Monsters().Count} targets\n");
                }
                var clash = GameObject.Find("Clash");
                if (clash != null && clashShots < 3 && Time.realtimeSinceStartup > lastClash + 3f)
                {
                    lastClash = Time.realtimeSinceStartup;
                    clashShots++;
                    yield return new WaitForSecondsRealtime(0.25f);
                    yield return Shot("clash");
                }
                if (Time.realtimeSinceStartup > nextShot)
                {
                    // 확대 패널 확인용: 패 카드 하나를 호버 상태로
                    CardInstance hover = null;
                    if (gm.Human != null && gm.Human.hand.Count > 0) hover = gm.Human.hand[shot % gm.Human.hand.Count];
                    if (hover != null) HUD.I.OnCardHover(hover, true);
                    yield return Shot($"t{gm.time:00}_{gm.phase}");
                    if (hover != null) HUD.I.OnCardHover(hover, false);
                    nextShot = Time.realtimeSinceStartup + 7f;
                }
                yield return null;
            }
            File.AppendAllText(logPath, $"DONE matches={matches} errors={errors} elapsed={Time.realtimeSinceStartup - start:F0}s\n");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }
}
