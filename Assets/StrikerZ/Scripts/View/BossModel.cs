using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 레이드 보스 3D 모델: 보스 카드 위가 아니라 보드 뒤편(보스 하수인 줄 너머)에 거대하게 세운다.
    /// 모델은 Resources/StrikerZ/Bosses/{보스 id} 프리팹 (Tools/raid 에서 VARCO 로 생성).
    /// 리그가 없는 정적 메시이므로 등장 · 호흡 · 포효 · 피격 · 쓰러짐을 절차적으로 움직인다.
    /// </summary>
    public class BossModel : MonoBehaviour
    {
        public static BossModel Active { get; private set; }

        /// <summary>보드 기준 위치 (보스 하수인 줄 뒤) · 목표 높이 · 최대 폭</summary>
        public static readonly Vector3 Anchor = new Vector3(0f, 0f, 1.95f);
        public const float TargetHeight = 2.4f, MaxWidth = 6.2f;

        public CardInstance hero;
        Transform model;
        float t, appear, roar, hit, defeat = -1f;
        float baseScale = 1f;

        public static BossModel Spawn(string bossId, CardInstance hero)
        {
            Clear();
            var prefab = Resources.Load<GameObject>(GameAssets.Root + "Bosses/" + bossId);
            if (prefab == null) return null;
            var go = new GameObject("Boss3D_" + bossId);
            var b = go.AddComponent<BossModel>();
            b.hero = hero;
            b.model = Instantiate(prefab, go.transform, false).transform;
            b.Fit();
            go.transform.position = Anchor;
            go.transform.rotation = Quaternion.Euler(0, 180f, 0);   // 정면(+Z)이 카메라를 보게
            go.transform.localScale = Vector3.zero;
            Active = b;
            return b;
        }

        public static void Clear()
        {
            if (Active != null) Destroy(Active.gameObject);
            Active = null;
        }

        /// <summary>렌더러 경계로 크기 맞춤: 높이 TargetHeight, 폭은 MaxWidth 이내, 발은 바닥에</summary>
        void Fit()
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var bb = rs[0].bounds;
            foreach (var r in rs) bb.Encapsulate(r.bounds);
            float s = TargetHeight / Mathf.Max(0.01f, bb.size.y);
            s = Mathf.Min(s, MaxWidth / Mathf.Max(0.01f, Mathf.Max(bb.size.x, bb.size.z)));
            model.localScale *= s;
            model.localPosition -= new Vector3(bb.center.x, bb.min.y, bb.center.z) * s;
            baseScale = 1f;
        }

        public static void Roar() { if (Active != null) Active.roar = 1f; }
        public static void Hit(CardInstance h) { if (Active != null && Active.hero == h) Active.hit = 1f; }
        public static void Defeat() { if (Active != null && Active.defeat < 0) Active.defeat = 0f; }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            appear = Mathf.MoveTowards(appear, 1f, dt * 0.7f);
            roar = Mathf.MoveTowards(roar, 0f, dt * 1.4f);
            hit = Mathf.MoveTowards(hit, 0f, dt * 3f);

            float a = Mathf.SmoothStep(0, 1, appear);
            // 호흡 · 둥실거림 · 느린 좌우 흔들림
            float breathe = 1f + Mathf.Sin(t * 1.6f) * 0.015f;
            float bob = Mathf.Sin(t * 0.8f) * 0.06f;
            float sway = Mathf.Sin(t * 0.35f) * 4f;
            // 포효: 앞으로 쏠리며 커졌다가 돌아온다
            float r = Mathf.Sin(roar * Mathf.PI);
            // 피격: 짧게 떨림
            float shake = hit > 0 ? Mathf.Sin(t * 70f) * 0.08f * hit : 0f;

            var pos = Anchor + new Vector3(shake, bob + (a - 1f) * 2.2f, -r * 0.45f);
            float scale = baseScale * a * breathe * (1f + r * 0.07f);
            if (defeat >= 0)
            {
                defeat += dt;
                float d = Mathf.Clamp01(defeat / 2.2f);
                pos.y -= d * d * 3f;
                scale *= 1f - d * 0.3f;
                if (d >= 1f) gameObject.SetActive(false);
            }
            transform.position = pos;
            transform.rotation = Quaternion.Euler(-r * 8f, 180f + sway, 0);
            transform.localScale = Vector3.one * scale;
        }

        void OnDestroy() { if (Active == this) Active = null; }
    }
}
