using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>스토리 모드 전투: 고정 상대 · 특수 규칙(회색 안개) · 동료 지원 · 튜토리얼 코치</summary>
    public partial class GameManager
    {
        public StoryStage story;
        StoryStage pendingStory;
        readonly HashSet<string> coachShown = new HashSet<string>();

        public bool IsStory => story != null;
        int storySummonTime = -1, storySummons;

        /// <summary>스토리 상대의 타임당 소환 제한 (CanSummon 에서 확인)</summary>
        bool StorySummonBlocked(CardInstance c)
        {
            if (story == null || c.owner != Cpu) return false;
            if (storySummonTime != time) { storySummonTime = time; storySummons = 0; }
            return storySummons >= story.battle.summonCap;
        }

        void StoryCountSummon(PlayerState p)
        {
            if (story == null || p != Cpu) return;
            if (storySummonTime != time) { storySummonTime = time; storySummons = 0; }
            storySummons++;
        }

        /// <summary>스토리 전투 시작: 플레이어는 카일 + 카일 덱, 상대는 스테이지 설정대로</summary>
        public void StartStory(StoryStage st)
        {
            coachShown.Clear();
            storySummonTime = -1;
            humanDeck = st.battle.playerDeck != null ? st.battle.playerDeck(rng) : StoryData.KaiDeck(rng);
            // 장기전에서 내가 먼저 덱이 바닥나 지지 않도록 몬스터 6장 여유 (선공이라 먼저 뽑기 시작함)
            humanDeck.AddRange(humanDeck.Where(c => c.type == CardType.Monster).OrderBy(_ => rng.Next()).Take(6).ToList());
            pendingStory = st;
            StartMatch(st.battle.playerHero ?? StoryData.Kai, st.battle.field ?? CardList.Fields.First(f => f.element == Element.None));
        }

        void SetupStoryOpponent(PlayerState p)
        {
            var b = story.battle;
            SetupPlayer(p, b.oppHero, b.oppDeck != null ? b.oppDeck(rng) : null);
            if (b.oppHp > 0) p.maxHp = p.hp = b.oppHp;
            if (b.playerHp > 0) Human.maxHp = Human.hp = b.playerHp;
        }

        /// <summary>내 타임 시작: 동료 지원 (아리아 회복 · 레온 화염)</summary>
        IEnumerator StoryTimeStart()
        {
            var b = story.battle;
            if (b.ariaHeal > 0 && Human.hp < Human.maxHp)
            {
                hud.StoryToast("아리아", $"치유의 물방울 — HP {b.ariaHeal} 회복");
                yield return Heal(Human, b.ariaHeal);
            }
            if (b.leonFire > 0)
            {
                hud.StoryToast("레온", $"홍염 연격 — {Cpu.name}에게 {b.leonFire} 데미지");
                var prev = fxSource;
                fxSource = Human.hero;
                yield return DamagePlayer(Cpu, b.leonFire, "레온의 화염");
                fxSource = prev;
            }
            foreach (var s in b.supports)
            {
                if (gameOver) yield break;
                hud.StoryToast(s.who, $"{s.skill}");
                switch (s.kind)
                {
                    case StorySupport.Kind.Heal:
                        if (Human.hp < Human.maxHp) yield return Heal(Human, s.amount);
                        break;
                    case StorySupport.Kind.Burn:
                    {
                        var prev = fxSource; fxSource = Human.hero;
                        yield return DamagePlayer(Cpu, s.amount, s.who);
                        fxSource = prev;
                        break;
                    }
                    case StorySupport.Kind.Atk:
                        AddStatus(new Status { kind = StatusKind.AtkBonus, target = Human, caster = Human, expire = Expire.EndOfTime, value = s.amount, createdTime = time, source = s.who });
                        yield return Wait(0.3f);
                        break;
                    case StorySupport.Kind.Strike:
                    {
                        var t = Cpu.Monsters().OrderByDescending(EffectiveAtk).FirstOrDefault();
                        if (t != null) yield return DamageMonster(t, s.amount, s.who);
                        break;
                    }
                }
            }
        }

        /// <summary>코치 팁 목록: 스토리 스테이지의 팁, 튜토리얼 대전이면 TutorialData.MatchTips, 그 외 없음</summary>
        List<StoryTip> CoachTips => story != null ? story.battle.tips : mode == MatchMode.Tutorial ? TutorialData.MatchTips : null;

        /// <summary>튜토리얼 코치 팁: 그 key 의 팁이 있으면 한 번만 띄운다 (튜토리얼 대전은 클릭할 때까지 남는다)</summary>
        void StoryCoach(string key)
        {
            var tips = CoachTips;
            if (tips == null || autoHuman || coachShown.Contains(key)) return;
            var tip = tips.FirstOrDefault(t => t.key == key);
            if (tip == null) return;
            coachShown.Add(key);
            hud.ShowCoach(tip.title, tip.text, mode == MatchMode.Tutorial);
        }

        /// <summary>메인 페이즈 시작 시: 기본 메인 팁을 먼저, 그다음 T 부터 영웅 능력 · 마법 카드 팁을 쓸 수 있는 순간에</summary>
        void StoryMainCoach()
        {
            var tips = CoachTips;
            if (tips == null) return;
            if (!coachShown.Contains("main")) { StoryCoach("main"); return; }
            if (CanUseHero(Human) && tips.Any(t => t.key == "hero") && !coachShown.Contains("hero")) { StoryCoach("hero"); return; }
            if (Human.hand.Any(c => c.IsMagic) && tips.Any(t => t.key == "magic") && !coachShown.Contains("magic")) { StoryCoach("magic"); return; }
        }

        /// <summary>스토리 결과: 이겼거나 이벤트 전투면 다음 이야기로 진행</summary>
        public bool StoryProceeds => story != null && (winner == Human || story.battle.allowLose);
    }
}
