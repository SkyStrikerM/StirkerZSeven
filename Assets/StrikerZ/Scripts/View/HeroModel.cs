using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerZ
{
    /// <summary>
    /// 영웅 카드 위에 서 있는 VARCO 3D 캐릭터 (Resources/StrikerZ/Heroes/{id}.prefab, HeroModelBuilder 로 생성).
    /// Animator: Idle 루프 · "Attack"(반격 / 능력 발동) · "Hit"(데미지를 받을 때) 트리거.
    /// </summary>
    public class HeroModel : MonoBehaviour
    {
        public const float Height = 1.2f;

        CardView card;
        Animator anim;
        Transform model;
        float baseYaw, yaw;
        float faceTimer, faceYaw;
        float appear = -0.7f;       // 카드가 내려앉은 뒤 등장
        float hitShake;

        public static HeroModel Attach(CardView v, string id, bool nearSide)
        {
            if (v == null || string.IsNullOrEmpty(id)) return null;
            var prefab = Resources.Load<GameObject>("StrikerZ/Heroes/" + id);
            if (prefab == null) return null;
            var go = new GameObject("Hero3D_" + id);
            go.transform.SetParent(v.transform, false);
            var h = go.AddComponent<HeroModel>();
            h.card = v;
            h.model = Instantiate(prefab, go.transform, false).transform;
            h.model.localScale = Vector3.zero;
            h.anim = h.model.GetComponentInChildren<Animator>();
            // 기본 정면(+Z). 내 영웅은 보드 중앙 쪽으로 몸을 틀고, 상대 영웅은 카메라를 본다
            h.baseYaw = h.yaw = nearSide ? 125f : 160f;
            return h;
        }

        public static HeroModel Of(CardInstance hero) => hero != null && hero.view != null ? hero.view.GetComponentInChildren<HeroModel>() : null;
        public static void PlayAttack(CardInstance hero, Vector3? target = null) { var h = Of(hero); if (h != null) h.Attack(target); }
        public static void PlayHit(CardInstance hero) { var h = Of(hero); if (h != null) h.Hit(); BossModel.Hit(hero); }

        /// <summary>공격 모션. target 이 있으면 잠시 그쪽을 바라본다</summary>
        public void Attack(Vector3? target = null)
        {
            if (target.HasValue)
            {
                Vector3 d = target.Value - transform.position;
                d.y = 0;
                if (d.sqrMagnitude > 1e-4f) { faceYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; faceTimer = 1.6f; }
            }
            if (anim != null) { anim.ResetTrigger("Hit"); anim.SetTrigger("Attack"); }
        }

        /// <summary>공격 모션이 발사 순간(정규화 시간 at)에 이를 때까지 대기 (모델이 없거나 너무 오래 걸리면 그냥 진행)</summary>
        public IEnumerator WaitRelease(float at)
        {
            float limit = 3f;
            bool entered = false;
            while (anim != null && limit > 0)
            {
                limit -= Time.deltaTime;
                var st = anim.GetCurrentAnimatorStateInfo(0);
                if (st.IsName("Attack"))
                {
                    entered = true;
                    if (st.normalizedTime >= at) yield break;
                }
                else if (entered && !anim.IsInTransition(0)) yield break;
                yield return null;
            }
        }

        /// <summary>발사 위치: 무기 끝(활은 손잡이) · 맨손이면 과녁에 가까운 손</summary>
        public Vector3 LaunchPoint(Vector3 target)
        {
            var weapons = new List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Weapon_")) weapons.Add(t);
            Transform hand = null;
            float best = float.MaxValue;
            foreach (var n in new[] { "LeftHand", "RightHand" })
            {
                var h = Find(transform, n);
                if (h == null) continue;
                float d = (h.position - target).sqrMagnitude;
                if (d < best) { best = d; hand = h; }
            }
            if (weapons.Count > 0)
            {
                var w = weapons[0];
                // 지팡이 머리: 홀더 +Y 방향으로 가장 먼 정점 쪽 (렌더러 경계에서)
                var rs = w.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    Vector3 up = w.up;
                    float ext = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z))));
                    bool bow = Projectile.KindOf(card != null && card.card != null ? card.card.data : null) == Projectile.Kind.Arrow;
                    if (bow) return w.position + (target - w.position).normalized * 0.05f;
                    return b.center + up * ext * 0.9f;
                }
            }
            return hand != null ? hand.position : transform.position + Vector3.up * 0.7f;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform ch in t) { var r = Find(ch, name); if (r != null) return r; }
            return null;
        }

        public void Hit()
        {
            hitShake = 1f;
            if (anim != null) { anim.ResetTrigger("Attack"); anim.SetTrigger("Hit"); }
        }

        void LateUpdate()
        {
            if (card == null) { Destroy(gameObject); return; }
            var p = card.transform;
            // 카드 회전(뒤집힘 연출 등)과 무관하게 항상 똑바로 선다
            transform.position = p.position + Vector3.up * (CardView.T * 0.5f + 0.004f);
            float target = faceTimer > 0 ? faceYaw : baseYaw;
            if (faceTimer > 0) faceTimer -= Time.deltaTime;
            yaw = Mathf.LerpAngle(yaw, target, 1f - Mathf.Exp(-Time.deltaTime * 8f));
            Vector3 shake = Vector3.zero;
            if (hitShake > 0)
            {
                hitShake = Mathf.Max(0, hitShake - Time.deltaTime * 3f);
                shake = new Vector3(Mathf.Sin(Time.time * 70f), 0, Mathf.Cos(Time.time * 55f)) * 0.025f * hitShake;
            }
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            transform.position += shake;

            appear = Mathf.Min(1f, appear + Time.deltaTime * 2.5f);
            float a = Mathf.Clamp01(appear);
            float pop = a <= 0 ? 0 : 1f + Mathf.Sin(a * Mathf.PI) * 0.12f;
            float s = Height * pop / Mathf.Max(0.0001f, p.lossyScale.y);
            // 카드(부모) 스케일을 상쇄해 월드 키 Height 유지. Model 자식의 정규화 스케일은 프리팹에 들어 있음
            transform.localScale = Vector3.one * s;
            if (model.localScale == Vector3.zero && a > 0) model.localScale = Vector3.one;
            if (a <= 0) model.localScale = Vector3.zero;
        }
    }
}
