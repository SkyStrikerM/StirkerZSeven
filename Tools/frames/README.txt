카드 프레임·스탯 아이콘 (VARCO, nano-banana-pro)
1) python schem.py      → 레이아웃 스케매틱(CardFrameLayout 좌표) schem_*.png 생성
2) VARCO EditImage(3:4) : 스케매틱을 sourceImage 로, 속성별 3D 프레임 프롬프트로 생성
3) python process_frame.py <monster|magic|hero|field> <이미지 URL> <frame_monster_fire 등>
   → Resources/StrikerZ/Textures/Frames/<이름>.png + _over.png (아트 창·여백 투명 처리)
4) 아이콘: 흰 배경 검/하트 보석 생성 → raw_sword.png / raw_heart.png → python icons.py
   → Textures/icon_sword.png, icon_heart.png (작은 아이콘용 icon_sword_pip.png 는 검을 90° 회전)
속성 프레임: Frames/{frame}_{fire|water|wind|earth|thunder|light|dark|rainbow|none}.png 가 있으면 카드 종류와 상관없이 자동 사용
   (현재 frame_monster_* · frame_field_* 9종씩. frame_magic_* / frame_hero_* 도 파일만 넣으면 적용, 없으면 기본 프레임 + 속성 틴트)
   필드 프롬프트: 몬스터와 같은 속성 색 + 지형 부조(화산·파도·협곡·성 등)로 구분. 하나씩 다시 뽑을 때는 raw/<이름>.png 캐시를 지울 것.
가공 카드: CardFrameLayout.FinishKeepsFrame = true → 속성 프레임 유지 + 얇은 은/금 림, false → finish_seven/strikerz 테두리가 덮음.
   얇은 림(VARCO): schem_rim.png 를 sourceImage 로 EditImage → python process_rim.py <URL> finish_rim_seven|finish_rim_strikerz
   (모델이 띠를 두껍게 그려도 모서리·변을 비율 유지 축소해 BAND=카드 폭 3.7% 로 맞춤. 림 파일이 없으면 절차적 테두리(FinishRim 두께)로 대체)
