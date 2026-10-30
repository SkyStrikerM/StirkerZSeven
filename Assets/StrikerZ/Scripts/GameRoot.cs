using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace StrikerZ
{
    /// <summary>씬 진입점: 카메라·이벤트시스템·보드·HUD·타이틀·메인 허브를 런타임에 구성</summary>
    public class GameRoot : MonoBehaviour
    {
        Camera cam;
        GameManager gm;
        BoardView board;
        HUD hud;
        TitleScreen title;
        MainMenu menu;
        StoryPlayer storyPlayer;

        void Awake()
        {
            Profile.TestMode = AutoTest.Enabled;
            GameSettings.Load();
            GameSettings.Apply();
            // 공개 확률(팩 확률표)과 수록 카드가 어긋나면 바로 알린다
            foreach (var p in BoosterPack.Packs)
                foreach (var e in p.Validate()) Debug.LogError("[StrikerZ] 확률표 오류: " + e);

            cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.015f, 0.05f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            if (cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>();

            // 씬에 이미 있는 기본 조명은 끈다 (보드가 자체 조명 사용)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.gameObject.SetActive(false);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            board = new GameObject("Board").AddComponent<BoardView>();
            board.Build(cam);

            var dice = new GameObject("Dice").AddComponent<DiceRoller>();
            dice.Init();

            hud = new GameObject("HUD", typeof(RectTransform)).AddComponent<HUD>();
            hud.Build();

            var ai = new GameObject("AI").AddComponent<AIController>();

            gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.board = board;
            gm.hud = hud;
            gm.dice = dice;
            var fx = new GameObject("CardFx").AddComponent<CardFx>();
            fx.Init(board);
            gm.fx = fx;
            gm.ai = ai;
            ai.Init(gm);
            gm.OnMatchFinished += OnMatchFinished;

            title = new GameObject("Title").AddComponent<TitleScreen>();
            title.Build(cam, OnHeroPicked);
            title.onStart = OpenHome;

            menu = new GameObject("MainMenu").AddComponent<MainMenu>();
            menu.Build();
            menu.onDuel = StartDuel;
            menu.onPractice = StartPractice;
            menu.onTutorialDuel = StartTutorial;
            menu.onPuzzle = StartPuzzle;
            menu.onEvent = StartEvent;
            menu.onArenaMatch = StartArena;
            menu.onResumeSeal = ResumeSeal;
            menu.onRaid = StartRaid;
            menu.onStory = StartStory;
            menu.onStoryReplay = ReplayStory;

            storyPlayer = new GameObject("Story").AddComponent<StoryPlayer>();
            storyPlayer.Build();
            menu.onTitle = () => { menu.Hide(); title.Show(); };

            hud.onRematch = OnRematch;
            hud.onTitle = BackToHome;

            board.gameObject.SetActive(false);
            title.Show();
            Sfx.PlayBgm("bgm_menu");

            if (AutoTest.Enabled) gameObject.AddComponent<AutoTest>().Init(this, gm);
        }

        // ── 자동 테스트용 ──
        public void TestShowHeroSelect() => title.OpenHeroSelect();
        public void TestStart(CardData hero) { title.Hide(); menu.Hide(); gm.humanDeck = null; OnHeroPicked(hero); }
        public void TestRematch() => OnRematch();
        public void TestPickFieldIfOpen() => title.ConfirmFieldIfOpen();
        public void TestMenu(MainMenu.Tab tab) { title.Hide(); menu.Show(tab); }
        public void TestBuy(int count) => menu.Buy(BoosterPack.Packs[0], count);
        public void TestDuel() => StartDuel(Profile.ActiveDeck);
        public void TestRaid(int bossIndex, int party, bool rental) => StartRaid(RaidData.Bosses[bossIndex], party, rental);
        public void TestStory(StoryStage st) => StartStory(st);
        public bool TestStoryPlaying => storyPlayer.Playing;
        public void TestStoryContinue() => OnRematch();

        void OpenHome()
        {
            title.Hide();
            menu.Show(MainMenu.Tab.Home);
            menu.MaybeShowAttendance();     // 오늘 첫 접속이면 출석체크
            menu.MaybeShowTutorial();       // 처음이면 튜토리얼 안내
            Sfx.PlayBgm("bgm_menu");
        }

        /// <summary>메인 허브 → 사용 덱으로 CPU 대전</summary>
        void StartDuel(DeckData deck)
        {
            var hero = CardDB.Get(deck.heroId) ?? CardList.Heroes[0];
            gm.humanDeck = DeckRules.ToCards(deck);
            chronicleDeck = deck.name;
            gm.cpuLevel = (CpuLevel)Mathf.Clamp(Profile.Data.cpuLevel, 0, 2);
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            menu.Hide();
            StartGame(hero, field);
        }

        /// <summary>덱 편집 → 훈련장 (수련 골렘, 기록·보상 없음)</summary>
        void StartPractice(DeckData deck)
        {
            lastPracticeDeck = deck;
            var hero = CardDB.Get(deck.heroId) ?? CardList.Heroes[0];
            gm.humanDeck = DeckRules.ToCards(deck);
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            menu.Hide();
            nextMode = MatchMode.Practice;
            StartGame(hero, field);
        }

        DeckData lastPracticeDeck;

        /// <summary>튜토리얼 대전: 사용 덱(쓸 수 없으면 자동 구성)으로 수련 CPU 와, 기록·보상 없음 · 코치 팁 (TutorialData.MatchTips)</summary>
        public void StartTutorial()
        {
            var deck = Profile.ActiveDeck;
            if (DeckRules.Validate(deck) != null) deck = DeckRules.AutoBuild("튜토리얼 덱", deck.heroId);
            var hero = CardDB.Get(deck.heroId) ?? CardList.Heroes[0];
            gm.humanDeck = DeckRules.ToCards(deck);
            chronicleDeck = deck.name;
            gm.cpuLevel = CpuLevel.Trainee;
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            menu.Hide();
            nextMode = MatchMode.Tutorial;
            StartGame(hero, field);
        }

        /// <summary>도전 › 원소 이변전: 사용 덱 · CPU 정예 · 이번 주 규칙</summary>
        void StartEvent(DeckData deck)
        {
            lastEventDeck = deck;
            var hero = CardDB.Get(deck.heroId) ?? CardList.Heroes[0];
            gm.humanDeck = DeckRules.ToCards(deck);
            chronicleDeck = deck.name;
            gm.cpuLevel = CpuLevel.Elite;
            gm.eventRule = ElementEvent.ThisWeek;
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            menu.Hide();
            nextMode = MatchMode.Event;
            StartGame(hero, field);
        }

        DeckData lastEventDeck;

        /// <summary>도전 › 원소 투기장: 드래프트 덱 · CPU 정예</summary>
        void StartArena()
        {
            if (!Arena.Ready) return;
            var hero = CardDB.Get(Arena.S.hero) ?? CardList.Heroes[0];
            gm.humanDeck = Arena.DeckCards;
            chronicleDeck = "투기장 덱";
            gm.cpuLevel = CpuLevel.Elite;
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            menu.Hide();
            nextMode = MatchMode.Arena;
            StartGame(hero, field);
        }

        /// <summary>도전 › 마법진 퍼즐</summary>
        void StartPuzzle(PuzzleDef p)
        {
            menu.Hide();
            PrepareMatch(p.Field);
            gm.StartPuzzle(p);
        }

        void OnHeroPicked(CardData hero)
        {
            var field = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            StartGame(hero, field);
        }

        /// <summary>홈 › 시간 봉인 해제: 저장된 T 의 처음부터 이어서</summary>
        void ResumeSeal()
        {
            var s = TimeSeal.Load();
            if (s == null) { TimeSeal.Clear(); menu.Toast("봉인을 읽지 못했습니다"); return; }
            chronicleDeck = s.deckName;
            if ((MatchMode)s.mode == MatchMode.Event) lastEventDeck = Profile.ActiveDeck;
            menu.Hide();
            PrepareMatch(CardDB.Get(s.field) ?? CardList.Fields[0]);
            gm.ResumeSeal(s);
        }

        void StartGame(CardData hero, CardData field)
        {
            PrepareMatch(field);
            gm.sealDeckName = chronicleDeck ?? "";
            gm.SetNextMode(nextMode);
            nextMode = MatchMode.Duel;
            gm.StartMatch(hero, field);
        }

        /// <summary>다음 StartGame 의 대전 종류 (훈련장 · 이변전 · 투기장 등)</summary>
        MatchMode nextMode = MatchMode.Duel;
        /// <summary>재대결이 같은 종류로 이어지게 마지막 일반 대전 종류를 기억</summary>
        MatchMode lastMode = MatchMode.Duel;

        void PrepareMatch(CardData field)
        {
            title.Hide();
            menu.Hide();
            Sfx.PlayFieldBgm(field);
            gm.humanName = Profile.Data.nickname;
            gm.humanAvatar = Profile.Data.avatar;
            board.gameObject.SetActive(true);
            board.FocusCamera();
            hud.HideGameOver();
            hud.SetReward("");
        }

        // ── 레이드 ──
        RaidBoss lastRaidBoss;
        int lastRaidParty;
        bool lastRaidRental;

        /// <summary>레이드 로비 → 보스전 (추천 대여 덱 또는 내 사용 덱)</summary>
        public void StartRaid(RaidBoss boss, int party, bool rental)
        {
            lastRaidBoss = boss; lastRaidParty = party; lastRaidRental = rental;
            CardData hero;
            System.Collections.Generic.List<CardData> deck;
            chronicleDeck = rental ? "대여 덱" : Profile.ActiveDeck.name;
            if (rental)
            {
                hero = RaidData.RecommendedHero(boss);
                deck = RaidData.RecommendedDeck(boss, new System.Random());
            }
            else
            {
                var d = Profile.ActiveDeck;
                hero = CardDB.Get(d.heroId) ?? CardList.Heroes[0];
                deck = DeckRules.ToCards(d);
            }
            PrepareMatch(RaidData.BossField(boss));
            gm.StartRaid(hero, deck, boss, party);
        }

        // ── 스토리 ──

        /// <summary>스토리 스테이지: 전 컷신 → 전투</summary>
        void StartStory(StoryStage st)
        {
            menu.Hide();
            title.Hide();
            storyPlayer.Play(st.pre, 0, () => StartStoryBattle(st));
        }

        void StartStoryBattle(StoryStage st)
        {
            chronicleDeck = "";
            PrepareMatch(st.battle.field ?? CardList.Fields[0]);
            gm.StartStory(st);
        }

        /// <summary>클리어한 스테이지의 이야기 다시 보기 (메뉴 위에 컷신만)</summary>
        void ReplayStory(StoryStage st)
        {
            var all = new System.Collections.Generic.List<StoryScene>(st.pre);
            all.AddRange(st.post);
            storyPlayer.Play(all, 1, null);
        }

        /// <summary>전투 후: 후 컷신 → 스토리 탭 (마지막 스테이지면 영웅 해금 알림)</summary>
        void ContinueStory()
        {
            var st = gm.story;
            bool won = gm.winner == gm.Human;
            hud.HideGameOver();
            gm.StopAllCoroutines();
            storyPlayer.Play(st.post, won ? 1 : 2, () =>
            {
                BackToHome();
                if (unlockedHero != null) menu.Toast($"새 영웅 「{CardDB.Get(unlockedHero)?.name}」 해금! 덱 편집에서 고를 수 있습니다");
                unlockedHero = null;
            });
        }

        string unlockedHero;

        /// <summary>듀얼 연대기에 남길 덱 이름 (일반 대전 · 레이드에서 내 덱을 썼을 때)</summary>
        string chronicleDeck;

        void OnMatchFinished()
        {
            bool won = gm.winner == gm.Human;
            Chronicle.Add(gm, chronicleDeck);       // 저장은 아래 기록 함수들의 Save 에서
            if (gm.mode != MatchMode.Practice && gm.mode != MatchMode.Puzzle && gm.mode != MatchMode.Tutorial)
                SevenPass.AddXp(SevenPass.XpMatch + (won ? SevenPass.XpWin : 0));
            if (gm.story != null)
            {
                var (first, opal, hero) = Profile.RecordStory(gm.story, won);
                unlockedHero = hero;
                string reward = first ? $"첫 클리어 보상  오팔 +{opal}" + (hero != null ? $"  ·  영웅 「{CardDB.Get(hero)?.name}」 해금!" : "") : "";
                hud.SetReward(gm.StoryProceeds
                    ? (reward != "" ? reward : $"보유 오팔 {Profile.Data.opal:N0}")
                    : "다시 도전해 보세요 — 영웅 능력 [척력 반사]를 아껴 두면 큰 몬스터를 튕겨낼 수 있습니다");
                return;
            }
            if (gm.raid != null)
            {
                var (opal, loot) = Profile.RecordRaid(gm.raid.boss.id, won);
                hud.SetReward(won
                    ? $"전리품 획득!  보유 오팔 {Profile.Data.opal:N0}"
                    : $"레이드 실패 — 보스 HP {gm.Cpu.hp} / {gm.Cpu.maxHp} 남음");
                if (won) hud.ShowRaidLoot(opal, loot);
                return;
            }
            if (gm.mode == MatchMode.Practice)
            {
                hud.SetReward("훈련장 — 기록·보상 없이 끝났습니다");
                return;
            }
            if (gm.mode == MatchMode.Tutorial)
            {
                Profile.Data.tutorialDuelDone = true;
                Profile.Save();
                hud.SetReward(won ? "튜토리얼 완료!  이제 대전 설정에서 수련 · 정예 · 전설 CPU 와 겨뤄 보세요" : "괜찮아요 — 순서만 익히면 됩니다. 대전 설정에서 수련 CPU 부터 도전해 보세요");
                return;
            }
            if (gm.mode == MatchMode.Arena)
            {
                hud.SetReward(Arena.Record(won));
                return;
            }
            if (gm.mode == MatchMode.Event)
            {
                int opal = ElementEvent.Record(won);
                hud.SetReward(won ? $"이변전 승리!  오팔 +{opal}" : $"이변전 패배 — 이번 주 {ElementEvent.WinsThisWeek}승");
                return;
            }
            if (gm.mode == MatchMode.Puzzle && gm.puzzle != null)
            {
                if (won && !Puzzles.Cleared(gm.puzzle.id)) SevenPass.AddXp(SevenPass.XpPuzzle);
                int got = won ? Puzzles.RecordClear(gm.puzzle) : 0;
                hud.SetReward(won ? (got > 0 ? $"퍼즐 클리어!  첫 클리어 보상 오팔 +{got}" : "퍼즐 클리어!") : "힌트: " + gm.puzzle.hint);
                return;
            }
            int bonus = GameManager.WinBonus(gm.cpuLevel);
            int claimable = Profile.RecordMatch(won, bonus);
            string bonusText = won && bonus > 0 ? $"{GameManager.LevelName(gm.cpuLevel)} 승리 보너스 오팔 +{bonus}  ·  " : "";
            hud.SetReward(bonusText + (claimable > 0 ? $"받을 수 있는 퀘스트 보상 {claimable}개  ·  홈 > 퀘스트" : $"보유 오팔 {Profile.Data.opal:N0}"));
        }

        void OnRematch()
        {
            if (gm.story != null)
            {
                if (gm.StoryProceeds) ContinueStory();
                else { hud.HideGameOver(); StartStoryBattle(gm.story); }
                return;
            }
            if (gm.raid != null && lastRaidBoss != null)
            {
                hud.HideGameOver();
                StartRaid(lastRaidBoss, lastRaidParty, lastRaidRental);
                return;
            }
            if (gm.mode == MatchMode.Puzzle && gm.puzzle != null)
            {
                hud.HideGameOver();
                // 성공했으면 다음 퍼즐, 실패면 다시
                var cur = gm.puzzle;
                int idx = Puzzles.All.IndexOf(cur);
                var next = gm.winner == gm.Human && idx >= 0 && idx + 1 < Puzzles.All.Count ? Puzzles.All[idx + 1] : cur;
                StartPuzzle(next);
                return;
            }
            if (gm.mode == MatchMode.Arena)
            {
                hud.HideGameOver();
                if (Arena.Ready) StartArena(); else BackToHome();
                return;
            }
            if (gm.mode == MatchMode.Event && lastEventDeck != null)
            {
                hud.HideGameOver();
                StartEvent(lastEventDeck);
                return;
            }
            if (gm.mode == MatchMode.Practice && lastPracticeDeck != null)
            {
                hud.HideGameOver();
                StartPractice(lastPracticeDeck);
                return;
            }
            if (gm.mode == MatchMode.Tutorial)
            {
                hud.HideGameOver();
                StartTutorial();
                return;
            }
            if (gm.LoserIsHuman)
            {
                hud.HideGameOver();
                title.ShowFieldSelect(f => { title.HideOverlay(); StartGame(gm.humanHero, f); });
            }
            else
            {
                StartCoroutine(CpuPicksField());
            }
        }

        IEnumerator CpuPicksField()
        {
            hud.HideGameOver();
            var f = CardList.Fields[Random.Range(0, CardList.Fields.Count)];
            yield return hud.Banner(f.name, "패배한 CPU가 다음 필드를 선택했습니다", 1.4f);
            StartGame(gm.humanHero, f);
        }

        void BackToHome()
        {
            GameSettings.BoostActive = false;          // 메뉴는 보통 속도
            GameSettings.Apply(false);
            gm.StopAllCoroutines();
            board.ClearCards();
            BossModel.Clear();
            dice.Clear();
            hud.HideGameOver();
            hud.Show(false);
            board.gameObject.SetActive(false);
            title.Hide();
            menu.Show(gm.story != null ? MainMenu.Tab.Story : gm.raid != null ? MainMenu.Tab.Raid : gm.mode == MatchMode.Practice ? MainMenu.Tab.Deck
                : gm.mode == MatchMode.Puzzle || gm.mode == MatchMode.Event || gm.mode == MatchMode.Arena ? MainMenu.Tab.Challenge : MainMenu.Tab.Home);
            Sfx.PlayBgm("bgm_menu");
        }

        DiceRoller dice => gm.dice;
    }
}
