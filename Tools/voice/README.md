# 캐릭터 음성 (VARCO Voice)

게임 쪽: `Scripts/View/Voice.cs` — `Resources/StrikerZ/Audio/Voice/` 의 ogg 를 재생 (효과음과 별도 AudioSource, 새 대사가 나오면 이전 대사는 끊김)

| 파일 | 언제 |
|---|---|
| `hero_{H###}_atk` | 영웅 반격 (GameManager HeroCounter) |
| `hero_{H###}_hit` | 영웅 피격 (DamagePlayer, 1.2초 쿨다운) |
| `hero_{H###}_skill` | 영웅 능력 사용 (UseHero) |
| `npc_{rosie/lukon/rohan/bella}_{md5 앞8}` | 상점 대화창 고정 대사 (RosieSay / LukonSay / ParentsSay) |

**영웅 음성은 현재 꺼 둠**: `Voice.HeroVoices = false`, 녹음본 81개는 `hero_ogg/` 에 보관 (다시 쓰려면 Resources/…/Voice/ 로 옮기고 true).

NPC 대사에 후후·허허·하하·히히 같은 추임새는 넣지 않는다 (TTS 로 읽으면 어색).

이름·숫자가 끼워지는 `$"…"` 대사는 녹음하지 않음 → 재생 없이 글자만 뜬다.

## 다시 만들기
1. `python make_lines.py` → `lines.json` (영웅 27×3 + NPC 고정 대사 자동 추출). 대사를 고치거나 추가하면 다시 돌린다.
2. `voices.json` (화자 → VARCO 보이스 id 뒷 6자리) 로 `jobs.json` = [파일, voice_id, 읽을 문장] 생성.
   읽을 문장은 `make_lines.py` 의 `SAY` 표로 발음을 바꾼다 (CP→씨피, 77장→칠십칠 장 …). 화면 글자/파일 키는 원문 그대로.
3. voice.varco.ai 에 로그인한 브라우저에서 `POST https://gateway.varco.ai/voice/api/playground/creations/tts`
   `{voice_id, text, language:'ko_KR', speed?, pitch?}` (기본 speed 1, pitch 1) (credentials include) → `audio_url` (wav 44.1kHz float). 한 줄 ≈ 3 크레딧.
   결과를 `urls.json` (파일 → url) 로 저장, `raw/` 에 내려받기 (다운로드는 쿠키 불필요).
4. 변환 (앞뒤 무음 제거 + 음량 맞추기 → ogg):
   ```
   ffmpeg -i raw/X.wav -af "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.03,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08,areverse,loudnorm=I=-16:TP=-1.5:LRA=11" -ar 44100 -ac 1 -c:a libvorbis -q:a 5 Assets/StrikerZ/Resources/StrikerZ/Audio/Voice/X.ogg
   ```

## 목소리 배정 (voices.json)
로지 엘렌 (speed 1.2) · 루콘 공현도 · 로한 브로드릭 · 벨라 송연채 (speed 1.15 · 헬레나→김민영→송연채, 후보 샘플은 bella_samples/) / 영웅은 voices.json 참고 (H017 카일과 H027 프리즘 카일은 같은 목소리 조렌).
