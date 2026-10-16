# 루콘 (로지의 할아버지) — 꾸미기 공방

- 캐릭터: 로지 그림(VARCO 929e5198…)을 화풍 레퍼런스로 GenerateImage (3:4, 마젠타 배경), 2장 중 A안 채택
  - 설정: 골뱅이(소용돌이) 안경 때문에 눈이 안 보인다 · 로지와 같은 별 띠 마법사 모자(낡고 기움) · 가죽 공구 앞치마 · 황금 주사위
  - 결과: lukon_a 64dbf465…, lukon_b b42968dd…
- 배경: 로지 상점 배경(shop_bg, VARCO f0875651…)을 레퍼런스로 공방 실내 (16:9) → 72f1de18…
- 후처리: `python Tools/lukon/process.py` → `Textures/shop_lukon.png`, `Textures/shop_lukon_bg.png` (안경 위치도 출력)
- 코드: `Scripts/UI/MainMenu.Cosmetics.cs` — 커튼 전환 입장/퇴장, 루콘 등장(깡총·착지·안경 반짝), 첫 방문 인사(ProfileData.lukonMet)
