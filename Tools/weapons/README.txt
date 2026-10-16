영웅 무기 (VARCO nano-banana-pro 이미지 → Generate3D 8000 tri) + 무기용 공격 모션
1) weapons.py → weapons.json : 영웅별 무기(설명·손 R/L·종류·길이·손잡이 위치·공격 모션 키) + VARCO 노드 id
   종류: blade(검·도끼·망치: 앞과 팔 방향 사이) · pole(지팡이·창·낫: 옆에 세움) · bow(왼손, 세움) · shield(아래팔 바깥)
   length = 캐릭터 키(1) 대비, grip = 무기 아래쪽에서 손잡이까지 비율. _flip = 이미지가 거꾸로 나온 무기(H010R: 손잡이가 위)
2) VARCO: 프롬프트 → GenerateImage(1:1) → ImageInput → Generate3D(tPose 0)
   공격 모션: Rigged3DInput(기존 영웅 리깅 RIG taskId) → Animate(무기 모션, inPlace) — 크레딧 0
3) python fetch_weapons.py urls.json → Heroes/<id>/weapon_<slot>.fbx · weapon_<slot>.png · weapons.json · attack.json
4) Unity: StrikerZ/영웅 3D 프리팹 전부 만들기 (HeroModelBuilder.Build → AttachWeapons)
   무기 메시의 긴 축(PCA)·얇은 축을 찾아 손잡이를 RightHand/LeftHand(방패는 ForeArm) 뼈대에 붙인다 → 모션에서 손을 그대로 따라감
H011 볼트는 거대 건틀릿이 모델에 포함되어 있어 별도 무기 없음.
이전 공격 모션·프리팹 백업: 세션 scratchpad/heroes_before_weapons
