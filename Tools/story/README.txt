스토리 모드 일러스트 (VARCO, 모델 nano-banana-pro)

1) 캐릭터 시트(3:4)  : char_kai / aria / leon / bane / gregor — 모든 장면의 레퍼런스
2) 대사 초상화(1:1) : face_* — 시트를 레퍼런스로 "얼굴~어깨 흉상, 글자 없음"
3) 장면 CG(16:9)    : v1_p0, v1_1a … v1_7d — 등장인물 시트를 reference 포트로 연결
4) 카드 아트(4:3)    : SM01~03 잿빛 짐승, SH01 훈련 인형 / H017_full 카일(3:4)

story_urls.json  : 결과 이미지 주소 (raw/ 로 다운로드)
nodes_v1.json    : VARCO 워크플로 노드 id (프롬프트·생성·레퍼런스 대응)
install_story.py : raw/ → Resources/StrikerZ/Art/Story · Art/Cards (흰 테두리 자동 제거, 비율 맞춤)

새 권을 만들 때: 같은 캐릭터 시트 노드를 레퍼런스로 연결하면 디자인이 유지된다.

── 제2~10장 (2026-10-06) ──
specs_v2.py        : 이미지 명세 → specs_v2.json (장면 CG v{장}_{스테이지} + v10_e, 초상화 face_*, 영웅 풀아트 H018~H027_full, 하수인 SM04~06, 3D용 A포즈)
job_*.json         : 노드 생성 작업 분할 / nodes_job_*.json : key → GenerateImage 노드 id
nodes_job_3d.json  : 영웅별 apose → Generate3D → Rig(humanoid-fingers) → Animate(idle/attack/hit) 노드 id
install_story2.py  : fetch <graph.txt> 로 raw/ 받기 → install 로 Art/Story · Art/Cards 설치 (SH07~SH26 상대 영웅 아트는 장면 CG에서 잘라냄)
vstat.py           : 저장된 get_workflow_graph 결과에서 노드 상태 요약
