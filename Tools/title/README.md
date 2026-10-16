# 타이틀 화면 그림 (VARCO 3D · nano-banana-pro)

- `raw/bg_a.png` → `title_bg_clean` : 기존 키아트(title_bg)를 참고로 운석·무지개 꼬리만 뺀 배경
- `raw/met_b.png` → `title_meteor_0..6` : 7원소 운석 시트(마젠타 배경) → 키잉 · 7개로 자르기 · 머리가 아래로 오게 회전
  순서 화염 · 물 · 바람 · 대지 · 번개 · 빛 · 어둠 (TitleScreen.MeteorGlow 와 같음)
- `raw/txt_b.png` → `title_start` : SEVEN 글씨체의 "Click Anywhere To Start" (검은 배경 → 밝기 알파)

`python process.py` 로 다시 만든다. 게임 쪽은 `Scripts/UI/TitleScreen.cs` (BuildMeteors / UpdateMeteors: 하나씩 순서대로 아레나 테두리로 떨어지고 착지 때 속성 색 빛).
