# 로지의 부모님 — 로한(아빠) · 벨라(엄마), 마력부여사

- 로지 그림(VARCO 929e5198…)을 화풍 레퍼런스로 GenerateImage (3:4, 마젠타 배경), 각 2장 중 채택
  - 로한 B dffbdb3a…/8c8b1da8…(B 채택), 벨라 A 2e31a722…(A 채택)/e96cb2e4…
- 방문(외문): 상점 배경(shop_bg)을 레퍼런스로 문짝만 (3:4) → 3ae96015…
- 후처리: `python Tools/parents/process.py` → `Textures/shop_rohan.png`, `shop_bella.png`, `shop_room_door.png`
- 코드: `Scripts/UI/MainMenu.ShopHall.cs` (가운데 마력부여실 · ? 설명 · 방문 이동 연출), `Scripts/UI/MainMenu.Upgrade.cs` + `Scripts/Meta/CardUpgrade.cs` (카드 업그레이드)
