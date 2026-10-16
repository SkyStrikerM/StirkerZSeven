# 꾸미기 아이템 (슬리브 · 덱케이스 · 코인 · 주사위)

게임 안 에셋을 바탕으로 VARCO(nano-banana-pro)에서 만든 상점용 꾸미기 아이템입니다. 종류마다 기본 1개 + 7속성(화염·물·바람·대지·번개·빛·어둠) 테마이고, 슬리브·코인은 무지개 1개가 더 있습니다 (슬리브 9 · 덱케이스 8 · 코인 9 · 주사위 8).

| 종류 | 원본 | 방법 | 가격 |
|---|---|---|---|
| 카드 슬리브 | card_back.png | EditImage (3:4). 원본 알파(모서리)를 그대로 씌움 | 500 오팔 |
| 덱케이스 | card_back 레퍼런스 + 레이드 보스 7종 테마 | GenerateImage, 마젠타 배경 → 키잉 | 500 오팔 |
| 코인 | coin_heads / coin_tails | EditImage (앞·뒷면에 같은 프롬프트), 원형 마스크 | 300 오팔 |
| 주사위 | dice_faces.png 배치 | 재질 텍스처만 생성 → 원본 눈 위치·장식선을 다시 그려 3×2 아틀라스로 합성 | 300 오팔 |

- `nodes.json`: VARCO 워크플로(fb0ce105, y ≥ 140000)의 노드 id
- `urls.json`: 생성 결과 해시 (`https://3d.varco.ai/api/objects/{hash}.png`)
- `raw/`: 받은 원본 이미지
- `process.py`: 후처리 → `Assets/StrikerZ/Resources/StrikerZ/Textures/Cosmetics/`

```
python Tools/cosmetics/process.py
```

코드: `Scripts/Meta/Cosmetics.cs` (카탈로그·구매·장착), `Scripts/UI/MainMenu.Cosmetics.cs` (상점 › 꾸미기 상점).
적용 위치: 슬리브 = 내 카드 뒷면 (CardView), 주사위·코인 = 내가 던질 때 (DiceRoller.humanSide), 덱케이스 = 홈 '덱 편집' 칸.

3D: 주사위는 둥근 모서리 메시(DiceRoller.BuildDieMesh), 상점의 주사위·코인 미리보기는 `View/DiceStage.cs`(화면 밖 카메라 → RenderTexture)가 전투와 같은 BuildDie/BuildCoin 으로 그립니다. 코인 옆테 색은 `Cosmetics.CoinRims`(그림 테두리 평균색).
