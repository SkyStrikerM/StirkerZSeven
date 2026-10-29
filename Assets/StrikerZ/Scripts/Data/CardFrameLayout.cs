using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// ★ 카드 프레임 배치는 여기서 조절 ★
    /// 프레임 이미지: Assets/StrikerZ/Resources/StrikerZ/Textures/Frames/frame_*.png (글자·숫자 없는 순수 프레임, 아트 창은 투명)
    /// 프레임 이미지를 교체하면 아래 비율만 새 이미지에 맞추면 된다.
    /// 좌표는 카드 전체 기준 0~1 정규화 (좌, 하, 우, 상) — y 는 아래가 0.
    /// 카드 이름·수치·설명은 이미지가 아니라 CardList.cs 데이터로 실시간 표시된다.
    /// </summary>
    public class FrameLayout
    {
        public string frame;                 // Textures/Frames/ 아래 파일명 (확장자 제외)
        public bool tintByElement;           // 속성 색으로 프레임 틴트
        public float tint = 0.5f;            // 틴트 강도 0~1
        /// <summary>종류 고정 틴트 (속성 틴트를 안 쓰는 종류용). 프레임 이미지에 곱해져 더 진하고 선명해진다 — 마법 카드 구분용 (2026-10-29)</summary>
        public Color frameTint = Color.white;
        public Vector4 art;                  // 아트 창 (프레임 뒤에 깔림)
        public Vector4 name;                 // 이름 배너
        public Vector4 type;                 // 종류/속성/타임 줄
        public Vector4 body;                 // 효과 텍스트 박스
        public Vector4 atk = new Vector4(0.005f, 0.0f, 0.165f, 0.255f);   // ATK 검 아이콘 (왼쪽 아래, 숫자는 손잡이 메달리온)
        public Vector4 hp = new Vector4(0.74f, 0.0f, 0.995f, 0.175f);     // HP 하트 보석 (오른쪽 아래)
        public Vector4 badge = new Vector4(0.02f, 0.01f, 0.3f, 0.1f);   // t0 / t/½ / HERO / FIELD 배지

        public Color nameColor = new Color(0.08f, 0.06f, 0.1f);
        public Color typeColor = new Color(0.1f, 0.08f, 0.12f);
        public Color bodyColor = new Color(0.93f, 0.92f, 0.96f);
        public Color nameOutline = new Color(0, 0, 0, 0);   // 이름 외곽선 (알파 0 = 없음)

        // ── 오버프레임 (Art/Cards/{id}_full.png 가 있으면 자동 적용) ──
        // 일러스트가 카드 전체를 채우고, 프레임은 반투명, 이름·종류·텍스트 영역은 흐림 유리(블러+어둡게)로 처리.
        public float overFrameAlpha = 0.6f;                           // 프레임(_over 이미지) 불투명도
        public Color frost = new Color(0.02f, 0.02f, 0.05f, 0.42f);    // 흐림 패널 위에 덮는 색
        public float frostPad = 0.012f;                               // 흐림 패널을 텍스트 영역보다 넓히는 여백
        public Color overNameColor = new Color(1f, 0.96f, 0.85f);
        public Color overTextOutline = new Color(0f, 0f, 0f, 0.9f);

        /// <summary>오버프레임용 프레임: 원본에서 이름/종류/텍스트 영역을 뚫은 버전 (Frames/{frame}_over.png)</summary>
        public string OverFrame => frame + "_over";
    }

    public static class CardFrameLayout
    {
        // ── 가공 (Seven / StrikerZ) 카드 테두리 ──
        // true  = 3D 속성 프레임을 그대로 보여주고 바깥에 얇은 은/금 테두리 + 포일·광택·발광
        // false = VARCO 가공 전용 테두리(Frames/finish_seven · finish_strikerz)가 프레임을 덮음
        public static bool FinishKeepsFrame = true;
        public static float FinishRim = 0.022f;   // FinishKeepsFrame 일 때 테두리 두께 (카드 폭 비율)

        // 속성 전용 프레임: Frames/{frame}_{속성}.png (+ _over) 가 있으면 카드 종류와 상관없이 자동 사용 (UIKit.CardFace.ApplyLayout)
        // 값은 VARCO 생성 프레임에서 측정한 것. 프레임 이미지를 바꾸면 여기만 수정.
        public static FrameLayout Monster = new FrameLayout
        {
            frame = "frame_monster", tintByElement = true, tint = 0.55f,
            art = new Vector4(0.101f, 0.431f, 0.899f, 0.875f),
            name = new Vector4(0.115f, 0.886f, 0.884f, 0.964f),
            type = new Vector4(0.125f, 0.367f, 0.874f, 0.429f),
            body = new Vector4(0.14f, 0.095f, 0.858f, 0.341f),
        };

        public static FrameLayout Magic = new FrameLayout
        {
            frame = "frame_magic",
            frameTint = new Color(0.42f, 0.78f, 0.92f),   // 연한 청록 프레임을 진한 청록으로 — 마법 카드임이 한눈에 보이게
            art = new Vector4(0.077f, 0.442f, 0.935f, 0.899f),
            name = new Vector4(0.16f, 0.908f, 0.84f, 0.971f),
            type = new Vector4(0.101f, 0.368f, 0.909f, 0.44f),
            body = new Vector4(0.117f, 0.082f, 0.882f, 0.342f),
            badge = new Vector4(0.36f, 0.015f, 0.64f, 0.075f),
            nameColor = new Color(0.04f, 0.13f, 0.14f),
            typeColor = new Color(0.05f, 0.16f, 0.17f),
        };

        public static FrameLayout Hero = new FrameLayout
        {
            frame = "frame_hero",
            art = new Vector4(0.086f, 0.462f, 0.912f, 0.878f),
            name = new Vector4(0.12f, 0.889f, 0.88f, 0.953f),
            type = new Vector4(0.12f, 0.372f, 0.877f, 0.418f),
            body = new Vector4(0.127f, 0.087f, 0.871f, 0.344f),
            nameColor = new Color(1f, 0.95f, 0.78f),
            nameOutline = new Color(0.18f, 0.08f, 0f, 1f),
            badge = new Vector4(0.36f, 0.015f, 0.64f, 0.075f),   // HERO 배지는 가운데 아래 (왼쪽 검·오른쪽 하트)
            typeColor = new Color(1f, 0.86f, 0.5f),
        };

        public static FrameLayout Field = new FrameLayout
        {
            frame = "frame_field", tintByElement = true, tint = 0.68f,
            art = new Vector4(0.111f, 0.44f, 0.889f, 0.867f),
            name = new Vector4(0.125f, 0.879f, 0.874f, 0.962f),
            type = new Vector4(0.135f, 0.383f, 0.864f, 0.438f),
            body = new Vector4(0.151f, 0.121f, 0.848f, 0.357f),
        };

        public static FrameLayout For(CardType t)
        {
            switch (t)
            {
                case CardType.Magic: return Magic;
                case CardType.Hero: return Hero;
                case CardType.Field: return Field;
                default: return Monster;
            }
        }
    }
}
