using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 시간 봉인: 일반 대전 · 원소 이변전 · 원소 투기장은 T 가 시작될 때마다 상태를 파일로 남긴다.
    /// 게임이 꺼져도 홈에서 [시간 봉인 해제] 로 그 T 의 처음부터 이어서 한다. 대전이 끝나면 봉인을 지운다.
    /// </summary>
    public partial class GameManager
    {
        bool SealAllowed => (mode == MatchMode.Duel || mode == MatchMode.Event || mode == MatchMode.Arena) && raid == null && story == null && !gameOver;
        /// <summary>봉인에 남길 덱 이름 (듀얼 연대기)</summary>
        public string sealDeckName = "";

        static SealCard Pack(CardInstance c)
        {
            var s = new SealCard
            {
                id = c.data.id, finish = (int)c.finish, slot = c.slot, faceDown = c.faceDown, fx = c.summonFxDone,
                damage = c.damage, tempAtk = c.tempAtk, weaken = c.weaken, stunUntil = c.stunUntil, tauntUntil = c.tauntUntil, noRegenUntil = c.noRegenUntil,
            };
            foreach (var b in c.buffs) s.buffs.Add(new SealBuff { value = b.value, until = b.untilTime });
            foreach (var b in c.hpBuffs) s.hpBuffs.Add(new SealBuff { value = b.value, until = b.untilTime });
            return s;
        }

        static SealPlayer Pack(PlayerState p)
        {
            var s = new SealPlayer { hero = p.hero.data.id, hp = p.hp, maxHp = p.maxHp, heroUsed = p.heroUsed, deckOutWarned = p.deckOutWarned };
            foreach (var c in p.deck) s.deck.Add(Pack(c));
            foreach (var c in p.hand) s.hand.Add(Pack(c));
            foreach (var c in p.grave) s.grave.Add(Pack(c));
            foreach (var c in p.extra) s.extra.Add(Pack(c));
            foreach (var c in p.slots) if (c != null) s.slots.Add(Pack(c));
            return s;
        }

        /// <summary>SimulTimeRoutine 시작 때 (time = 지금 시작하는 T)</summary>
        void SealNow()
        {
            if (!SealAllowed || players[0] == null) return;
            var d = new SealData
            {
                savedAt = DateTime.Now.ToString("MM-dd HH:mm"),
                mode = (int)mode, cpuLevel = (int)cpuLevel, eventRule = (int)eventRule,
                time = time, firstHuman = first == Human, fieldFirstHuman = fieldFirst == Human, duelRounds = duelRounds,
                cpuName = Cpu.name, deckName = sealDeckName,
                field = center != null ? center.data.id : "", fieldOwner = center?.owner != null ? center.owner.index : -1,
                deckIds = humanDeck != null ? humanDeck.Select(c => c.id).ToList() : new List<string>(),
                human = Pack(Human), cpu = Pack(Cpu),
            };
            foreach (var st in statuses)
                d.statuses.Add(new SealStatus
                {
                    kind = (int)st.kind, expire = (int)st.expire, value = st.value, createdTime = st.createdTime, source = st.source,
                    target = st.target != null ? st.target.index : -1, caster = st.caster != null ? st.caster.index : -1,
                });
            TimeSeal.Save(d);
        }

        CardInstance Unpack(SealCard s, PlayerState owner, Zone zone)
        {
            var data = CardDB.Get(s.id);
            if (data == null) return null;
            var c = new CardInstance(data, owner)
            {
                zone = zone, slot = zone == Zone.Slot ? s.slot : -1, faceDown = s.faceDown, summonFxDone = s.fx, finish = (CardFinish)s.finish,
                damage = s.damage, tempAtk = s.tempAtk, weaken = s.weaken, stunUntil = s.stunUntil, tauntUntil = s.tauntUntil, noRegenUntil = s.noRegenUntil,
            };
            foreach (var b in s.buffs) c.buffs.Add(new AtkBuff { value = b.value, untilTime = b.until });
            foreach (var b in s.hpBuffs) c.hpBuffs.Add(new AtkBuff { value = b.value, untilTime = b.until });
            return c;
        }

        void Unpack(PlayerState p, SealPlayer s)
        {
            var heroData = CardDB.Get(s.hero) ?? CardList.Heroes[0];
            p.hero = new CardInstance(heroData, p) { zone = Zone.Hero };
            p.hp = s.hp; p.maxHp = s.maxHp; p.heroUsed = s.heroUsed; p.deckOutWarned = s.deckOutWarned;
            board.CreateView(p.hero, board.HeroPos(p) + Vector3.up * 2f);
            HeroModel.Attach(p.hero.view, heroData.id, p.index == 0);
            foreach (var x in s.deck) { var c = Unpack(x, p, Zone.Deck); if (c == null) continue; p.deck.Add(c); board.CreateView(c, board.DeckPos(p, p.deck.Count)); }
            foreach (var x in s.hand) { var c = Unpack(x, p, Zone.Hand); if (c == null) continue; p.hand.Add(c); board.CreateView(c, board.DeckPos(p, 1)); }
            foreach (var x in s.grave) { var c = Unpack(x, p, Zone.Grave); if (c == null) continue; p.grave.Add(c); board.CreateView(c, board.GravePos(p, p.grave.Count)); }
            if (s.extra != null) foreach (var x in s.extra) { var c = Unpack(x, p, Zone.Extra); if (c != null) p.extra.Add(c); }
            foreach (var x in s.slots)
            {
                if (x.slot < 0 || x.slot >= PlayerState.SlotCount) continue;
                var c = Unpack(x, p, Zone.Slot);
                if (c == null) continue;
                p.slots[x.slot] = c;
                board.CreateView(c, board.SlotPos(p, x.slot) + Vector3.up * 2f);
            }
        }

        /// <summary>봉인된 대전 이어하기</summary>
        public void ResumeSeal(SealData s)
        {
            StopAllCoroutines();
            raid = null; pendingRaid = null;
            story = null; pendingStory = null;
            puzzle = null; pendingPuzzle = null;
            mode = (MatchMode)s.mode;
            pendingMode = MatchMode.Duel;
            cpuLevel = (CpuLevel)s.cpuLevel;
            eventRule = (EventRule)s.eventRule;
            sealDeckName = s.deckName;
            humanDeck = s.deckIds.Select(CardDB.Get).Where(c => c != null).ToList();
            humanHero = CardDB.Get(s.human.hero) ?? CardList.Heroes[0];
            StartCoroutine(ResumeRoutine(s));
        }

        IEnumerator ResumeRoutine(SealData s)
        {
            matchNumber++;
            duelRounds = s.duelRounds;
            eclipseTime = battleOpenTime = -1;
            ResetExtrasData();
            board.ClearCards();
            BossModel.Clear();
            statuses.Clear();
            extraAttacks.Clear();
            gameOver = false; MatchFinished = false; winner = null; surrendered = false; time = 0; center = null; phase = Phase.None; busy = true;
            choiceOptions = null;
            players[0] = new PlayerState { index = 0, name = humanName, isHuman = true, avatar = humanAvatar };
            players[1] = new PlayerState { index = 1, name = s.cpuName, isHuman = false, avatar = Profile.CpuAvatar };
            players[0].opponent = players[1];
            players[1].opponent = players[0];
            Unpack(players[0], s.human);
            Unpack(players[1], s.cpu);
            hud.BeginMatch(this);
            var fieldData = CardDB.Get(s.field);
            if (fieldData != null)
            {
                center = new CardInstance(fieldData, s.fieldOwner >= 0 ? players[s.fieldOwner] : null) { zone = Zone.Center };
                Sfx.PlayFieldBgm(fieldData);
                board.CreateView(center, new Vector3(0, 3f, 0));
            }
            foreach (var st in s.statuses)
                statuses.Add(new Status
                {
                    kind = (StatusKind)st.kind, expire = (Expire)st.expire, value = st.value, createdTime = st.createdTime, source = st.source,
                    target = st.target >= 0 ? players[st.target] : null, caster = st.caster >= 0 ? players[st.caster] : null,
                });
            first = s.firstHuman ? Human : Cpu;
            fieldFirst = s.fieldFirstHuman ? Human : Cpu;
            RefreshAll();
            yield return Wait(0.5f);
            yield return hud.Banner("시간 봉인 해제", $"TIME {s.time} 의 처음부터 이어서 합니다", 1.3f);
            busy = false;
            time = s.time - 1;
            while (!gameOver)
            {
                time++;
                yield return SimulTimeRoutine();
            }
            yield return FinishMatch();
        }
    }
}
