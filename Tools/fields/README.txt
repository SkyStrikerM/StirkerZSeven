필드 테마 (필드 카드가 센터에 놓이면 게임판·바깥 환경이 바뀜) — VARCO nano-banana-pro

게임판은 레이어 2장:
  [위] Textures/playmat_overlay.png  : 기본 매트의 룬 문자 · 금색 연성진(기하학 선·칸 선) · 중앙 용 문장. 항상 표시 (python make_overlay.py)
  [아래] Textures/Fields/playmat_<F001>.png : 필드 배경 그림 (선·칸·문장 없음, 살짝 어둡게). 없으면 기본 Textures/playmat
바깥: Textures/Fields/env_<F001>.png — 테이블 아래 넓은 바닥(FieldEnvironment)에 위에서 본 주변 지형
적용 코드: BoardView.ApplyFieldTheme / PlaymatOverlay. 새 필드 카드도 이 파일 두 장만 넣으면 자동 적용.

만드는 법
1) schem_playmat.png (칸 배치 스케매틱, VARCO 업로드: https://3d.varco.ai/api/objects/6073c8580f894e863ebd8cbb1802579c.png)
2) python field_prompts.py → field_prompts.json
   playmat: EditImage(16:9) sourceImage = 스케매틱, referenceText = playmat 프롬프트 → 테마 그림 (선 포함)
   clean  : EditImage(16:9) sourceImage = 위 결과, referenceText = "선·칸·원·문장 제거, 배경만" (field_nodes.json 의 cleanText)
   env    : GenerateImage(16:9) prompt = env 프롬프트
3) python install_field.py <F001> <playmat URL> <env URL>   (env 설치. playmat 은 다음 단계로 덮어씀)
   python install_clean.py <F001> <clean URL>                (선 없는 배경을 playmat_<id> 로, 0.82 배 어둡게)
raw/ = 원본 캐시 (다시 뽑으면 해당 파일을 지울 것). field_nodes.json = VARCO 노드 id · URL 기록
