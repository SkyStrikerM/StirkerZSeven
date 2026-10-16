#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StrikerZ.EditorTools
{
    /// <summary>
    /// VARCO 로 만든 영웅 3D 캐릭터를 게임용 프리팹으로 조립한다.
    /// 입력: Assets/StrikerZ/Heroes/{id}/ model.fbx · albedo.png · idle.json · attack.json · hit.json
    ///   (json 은 glb2anim.py 가 VARCO Animate GLB 에서 뽑은 본 회전 키)
    /// 출력: Assets/StrikerZ/Resources/StrikerZ/Heroes/{id}.prefab (Animator: Idle 루프 · Attack/Hit 트리거)
    /// </summary>
    public static class HeroModelBuilder
    {
        const string SrcRoot = "Assets/StrikerZ/Heroes";
        const string OutRoot = "Assets/StrikerZ/Resources/StrikerZ/Heroes";

        [Serializable] class AnimCurveJson { public string path; public string prop; public int dim; public float[] times; public float[] values; }
        [Serializable] class AnimJson { public float length; public List<AnimCurveJson> curves; }

        [MenuItem("StrikerZ/영웅 3D 프리팹 전부 만들기")]
        public static void BuildAll()
        {
            foreach (var dir in Directory.GetDirectories(SrcRoot))
                Build(Path.GetFileName(dir));
        }

        public static string Build(string id)
        {
            string dir = $"{SrcRoot}/{id}";
            string fbx = $"{dir}/model.fbx";
            if (!File.Exists(fbx)) return $"{id}: model.fbx 없음";
            AssetDatabase.ImportAsset(fbx);
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (imp.animationType != ModelImporterAnimationType.Generic || imp.importAnimation || imp.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.importAnimation = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();
            }

            // 재질 (URP Lit + VARCO 텍스처)
            string texPath = $"{dir}/albedo.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            string matPath = $"{dir}/{id}_mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.25f);
            mat.SetFloat("_Metallic", 0.1f);
            EditorUtility.SetDirty(mat);

            // 주먹: 손가락 본이 있는 리그(VARCO Rig humanoid-fingers)면 손가락을 말아 쥔 자세를 모든 클립에 고정
            var gripSides = WeaponHands(dir);
            var fist = FistPose(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), gripSides);

            // 애니메이션 클립
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var name in new[] { "idle", "attack", "hit" })
            {
                string jp = $"{dir}/{name}.json";
                if (!File.Exists(jp)) continue;
                var aj = JsonUtility.FromJson<AnimJson>(File.ReadAllText(jp));
                var clip = new AnimationClip { name = $"{id}_{name}", frameRate = 30 };
                foreach (var c in aj.curves)
                {
                    string[] comps = c.prop == "rotation" ? new[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" }
                                                          : new[] { "localPosition.x", "localPosition.y", "localPosition.z" };
                    for (int k = 0; k < c.dim && k < comps.Length; k++)
                    {
                        var keys = new Keyframe[c.times.Length];
                        for (int i = 0; i < c.times.Length; i++) keys[i] = new Keyframe(c.times[i], c.values[i * c.dim + k]);
                        var curve = new AnimationCurve(keys);
                        for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
                        clip.SetCurve(c.path, typeof(Transform), comps[k], curve);
                    }
                }
                foreach (var kv in fist)
                {
                    string[] rc = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
                    for (int k = 0; k < 4; k++)
                        clip.SetCurve(kv.Key, typeof(Transform), rc[k], AnimationCurve.Constant(0, Mathf.Max(aj.length, 0.1f), kv.Value[k]));
                }
                // 차렷 자세: 주먹 쥔 영웅은 대기 모션에서 손바닥이 허벅지를 향하도록 팔목을 비튼다
                if (name == "idle" && fist.Count > 0) FaceFistsInward(clip, AssetDatabase.LoadAssetAtPath<GameObject>(fbx), gripSides);
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = name == "idle";
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                string cp = $"{dir}/{id}_{name}.anim";
                AssetDatabase.DeleteAsset(cp);
                AssetDatabase.CreateAsset(clip, cp);
                clips[name] = clip;
            }

            // 애니메이터: Idle ↔ Attack / Hit
            string ctrlPath = $"{dir}/{id}.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = clips.TryGetValue("idle", out var ic) ? ic : null;
            sm.defaultState = idle;
            foreach (var (st, trig) in new[] { ("Attack", "attack"), ("Hit", "hit") })
            {
                if (!clips.TryGetValue(trig, out var clip)) continue;
                var s = sm.AddState(st);
                s.motion = clip;
                // 긴 모션(콤보 등)은 연출 템포에 맞게 약 2.8초 안에 끝나도록 가속
                s.speed = Mathf.Clamp(clip.length / 2.8f, 1.3f, 2.3f);
                var t = sm.AddAnyStateTransition(s);
                t.AddCondition(AnimatorConditionMode.If, 0, st);
                t.duration = 0.08f;
                t.canTransitionToSelf = true;
                var back = s.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 0.92f;
                back.duration = 0.2f;
            }

            // 프리팹: 루트(발 = 원점, 높이 1) ─ 모델
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = new GameObject("Hero_" + id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Model";
            inst.transform.SetParent(root.transform, false);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null) { smr.updateWhenOffscreen = true; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            var anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 크기 정규화: 대기 자세로 굽힌 메시의 실제 정점 경계 기준 (발 = 원점, 키 1)
            if (clips.TryGetValue("idle", out var idleClip)) idleClip.SampleAnimation(inst, 0f);
            var b = new Bounds();
            bool any = false;
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new UnityEngine.Mesh();
                r.BakeMesh(baked, true);
                foreach (var v in baked.vertices)
                {
                    var w = r.transform.TransformPoint(v);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            if (!any)
            {
                var rends = inst.GetComponentsInChildren<Renderer>(true);
                b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
            }
            float h = Mathf.Max(0.01f, b.size.y);
            float k2 = 1f / h;
            inst.transform.localScale *= k2;
            inst.transform.localPosition = new Vector3(-b.center.x * k2, -b.min.y * k2, -b.center.z * k2);

            int weapons = AttachWeapons(dir, id, inst);
            if (clips.TryGetValue("attack", out var atkClip))
            {
                AimBows(dir, inst, atkClip);
                if (idleClip != null) idleClip.SampleAnimation(inst, 0f);     // 프리팹은 대기 자세로 저장
            }

            Directory.CreateDirectory(OutRoot);
            string outPath = $"{OutRoot}/{id}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return $"{id}: OK clips={clips.Count} weapons={weapons} height={h:F2} size={b.size}";
        }

        // ══════════════════════════ 주먹 쥐기 ══════════════════════════
        // 손가락 본(…_Finger{n}{seg}: 0 = 엄지, 1~4 = 검지~새끼)을 손바닥 쪽으로 말아 쥔 로컬 회전을 구한다.
        /// <summary>weapons.json 에서 무기(검·지팡이·활·방패 손잡이)를 쥔 손: "Left" / "Right"</summary>
        static HashSet<string> WeaponHands(string dir)
        {
            var set = new HashSet<string>();
            string cfg = $"{dir}/weapons.json";
            if (!File.Exists(cfg)) return set;
            var list = JsonUtility.FromJson<WeaponList>(File.ReadAllText(cfg));
            if (list?.weapons != null)
                foreach (var w in list.weapons) set.Add(w.hand == "L" ? "Left" : "Right");
            return set;
        }

        static Dictionary<string, Quaternion> FistPose(GameObject modelAsset, HashSet<string> gripSides = null)
        {
            var result = new Dictionary<string, Quaternion>();
            if (modelAsset == null) return result;
            var inst = UnityEngine.Object.Instantiate(modelAsset);
            try
            {
                var hips = FindDeep(inst.transform, "Hips");
                foreach (var side in new[] { "Left", "Right" })
                {
                    var hand = FindDeep(inst.transform, side + "Hand");
                    if (hand == null || hips == null) continue;
                    var fingers = hand.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Finger")).ToList();
                    if (fingers.Count == 0) continue;
                    // 손바닥 법선 = 손등 반대쪽. 손가락 방향 f(손목 → 중지) × 가로 방향 t(새끼 → 검지), 왼손/오른손 손잡이에 따라 부호가 반대
                    var f = (FingerTip(hand, fingers, "Finger2") - hand.position).normalized;
                    var t = (FingerTip(hand, fingers, "Finger1") - FingerTip(hand, fingers, "Finger4")).normalized;
                    var palm = Vector3.Cross(f, t).normalized * (side == "Left" ? -1f : 1f);
                    // 손가락 사슬마다 뿌리 → 끝 순서로 접는다 (부모를 먼저 접어야 자식 방향이 갱신됨)
                    bool grip = gripSides != null && gripSides.Contains(side);
                    var roots = fingers.Where(x => x.parent == hand || !x.parent.name.Contains("Finger")).ToList();
                    Transform thumbRoot = null;
                    foreach (var root in roots)
                    {
                        if (root.name.EndsWith("Finger0")) { thumbRoot = root; continue; }   // 엄지는 네 손가락을 다 접은 뒤에
                        // 빈손 = 꽉 쥔 주먹 / 무기 든 손 = 손잡이를 감싸 쥔 자세 (손잡이 굵기만큼 덜 접음)
                        float[] angles = grip ? new[] { 72f, 82f, 55f } : new[] { 92f, 100f, 78f };
                        // 굽힘 축은 뿌리 마디에서 한 번만 구한다 (부모를 접은 뒤엔 손가락 방향이 손바닥 법선과 겹쳐 축이 무너짐).
                        // 부모를 이 축으로 돌려도 축 자체는 그대로이므로 사슬 전체에 같은 월드 축을 쓴다.
                        var rootChild = root.childCount > 0 ? root.GetChild(0) : null;
                        var rootDir = rootChild != null ? (rootChild.position - root.position).normalized : (root.position - hand.position).normalized;
                        var axis = Vector3.Cross(rootDir, palm);
                        if (axis.sqrMagnitude < 1e-6f) continue;
                        axis.Normalize();
                        var seg = root;
                        for (int i = 0; seg != null && i < angles.Length; i++)
                        {
                            var child = seg.childCount > 0 ? seg.GetChild(0) : null;
                            seg.rotation = Quaternion.AngleAxis(angles[i], axis) * seg.rotation;
                            result[AnimationUtility.CalculateTransformPath(seg, inst.transform)] = seg.localRotation;
                            seg = child != null && child.name.Contains("Finger") ? child : null;
                        }
                    }
                    if (thumbRoot != null) FoldThumb(thumbRoot, fingers, palm, inst.transform, result, grip);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(inst); }
            return result;
        }

        /// <summary>
        /// 엄지: 말아 쥔 검지·중지의 가운데 마디 바깥을 가로질러 덮도록 CCD IK 로 세 마디를 굽힌다.
        /// (뿌리만 겨누면 엄지가 목표보다 길어서 주먹 끝으로 삐죽 나온다)
        /// 목표 = 검지·중지 둘째 마디 중간에서 손목 쪽으로 반 마디, 새끼 쪽으로 한 마디 폭, 손바닥 바깥으로 한 마디 폭.
        /// </summary>
        static void FoldThumb(Transform root, List<Transform> fingers, Vector3 palm, Transform modelRoot, Dictionary<string, Quaternion> result, bool grip = false)
        {
            Transform F(string key) => fingers.FirstOrDefault(t => t.name.EndsWith(key));
            var idx1 = F("Finger1"); var mid1 = F("Finger2"); var idx2 = F("Finger11"); var mid2 = F("Finger21");
            var t1 = root.childCount > 0 ? root.GetChild(0) : null;
            var t2 = t1 != null && t1.childCount > 0 ? t1.GetChild(0) : null;
            if (idx1 == null || mid1 == null || idx2 == null || mid2 == null || t1 == null || t2 == null) return;
            var hand = root.parent;
            var along = (mid1.position - hand.position).normalized;
            var across = Vector3.Cross(palm, along).normalized;
            float thumbSide = Mathf.Sign(Vector3.Dot(root.position - hand.position, across));
            float width = Vector3.Distance(idx1.position, mid1.position);
            // 쥔 자세: 엄지가 손잡이를 사이에 두고 검지 둘째 마디 쪽으로 감싼다 (덜 가로지르고, 손바닥에서 조금 더 떨어져서)
            var target = grip
                ? idx2.position - along * width * 0.3f - across * thumbSide * width * 0.3f + palm * width * 1.3f
                : (idx2.position + mid2.position) * 0.5f - along * width * 0.5f - across * thumbSide * width + palm * width * 0.9f;

            // 끝마디 다음엔 본이 없으므로, 끝마디 방향(로컬)과 길이로 가상의 엄지 끝을 만든다
            float lastLen = Vector3.Distance(t2.position, t1.position);
            var lastDirLocal = Quaternion.Inverse(t2.rotation) * (t2.position - t1.position).normalized;
            Vector3 Tip() => t2.position + t2.rotation * lastDirLocal * lastLen;

            var joints = new[] { t2, t1, root };
            for (int iter = 0; iter < 16; iter++)
                foreach (var j in joints)
                {
                    var a = Tip() - j.position; var b = target - j.position;
                    if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f) continue;
                    j.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(a, b), 0.7f) * j.rotation;
                }
            foreach (var j in joints) result[AnimationUtility.CalculateTransformPath(j, modelRoot)] = j.localRotation;
        }

        /// <summary>
        /// 대기 클립의 손목 회전을 고쳐 주먹의 손바닥 쪽이 몸 안쪽(허벅지)을 보게 한다 (차렷 자세).
        /// 프레임마다 손바닥 법선과 '손 → 엉덩이' 수평 방향의 각도를 재서, 절반은 아래팔(ForeArm)을 제 축으로 돌리고
        /// 나머지는 손(Hand)으로 맞춘다 (손목 한 곳만 크게 꺾으면 피부가 사탕 껍질처럼 꼬임).
        /// </summary>
        static void FaceFistsInward(AnimationClip clip, GameObject modelAsset, HashSet<string> skipSides = null)
        {
            if (modelAsset == null) return;
            var inst = UnityEngine.Object.Instantiate(modelAsset);
            try
            {
                var hips = FindDeep(inst.transform, "Hips");
                if (hips == null) return;
                float len = Mathf.Max(clip.length, 0.1f);
                int frames = Mathf.Max(2, Mathf.CeilToInt(len * 15f) + 1);
                var sides = new List<(Transform fore, Transform hand, Transform idx, Transform mid, Transform pinky, float sign)>();
                foreach (var side in new[] { "Left", "Right" })
                {
                    if (skipSides != null && skipSides.Contains(side)) continue;   // 무기 든 손은 무기 방향 그대로
                    var hand = FindDeep(inst.transform, side + "Hand");
                    var fore = FindDeep(inst.transform, side + "ForeArm");
                    if (hand == null || fore == null) continue;
                    var fingers = hand.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Finger")).ToList();
                    var idx = fingers.FirstOrDefault(t => t.name.EndsWith("Finger1"));
                    var mid = fingers.FirstOrDefault(t => t.name.EndsWith("Finger2"));
                    var pinky = fingers.FirstOrDefault(t => t.name.EndsWith("Finger4"));
                    if (idx == null || mid == null || pinky == null) continue;
                    sides.Add((fore, hand, idx, mid, pinky, side == "Left" ? -1f : 1f));
                }
                if (sides.Count == 0) return;
                var keys = sides.Select(_ => (fore: new Keyframe[4][], hand: new Keyframe[4][])).ToList();
                for (int k = 0; k < sides.Count; k++)
                    for (int c = 0; c < 4; c++) { keys[k].fore[c] = new Keyframe[frames]; keys[k].hand[c] = new Keyframe[frames]; }

                for (int f = 0; f < frames; f++)
                {
                    float t = len * f / (frames - 1);
                    clip.SampleAnimation(inst, t);
                    for (int k = 0; k < sides.Count; k++)
                    {
                        var (fore, hand, idx, mid, pinky, sign) = sides[k];
                        float Angle()
                        {
                            var along = (mid.position - hand.position).normalized;
                            var across = (idx.position - pinky.position).normalized;
                            var palm = Vector3.Cross(along, across).normalized * sign;
                            var inward = Vector3.ProjectOnPlane(hips.position - hand.position, along);
                            return inward.sqrMagnitude < 1e-6f ? 0f : Vector3.SignedAngle(Vector3.ProjectOnPlane(palm, along), inward, along);
                        }
                        // 아래팔: 팔꿈치 → 손목 축으로 절반
                        float a = Angle();
                        var foreAxis = (hand.position - fore.position).normalized;
                        fore.rotation = Quaternion.AngleAxis(a * 0.5f, foreAxis) * fore.rotation;
                        // 손: 남은 만큼
                        float b = Angle();
                        var handAxis = (mid.position - hand.position).normalized;
                        hand.rotation = Quaternion.AngleAxis(b, handAxis) * hand.rotation;
                        var fq = fore.localRotation; var hq = hand.localRotation;
                        for (int c = 0; c < 4; c++)
                        {
                            keys[k].fore[c][f] = new Keyframe(t, fq[c]);
                            keys[k].hand[c][f] = new Keyframe(t, hq[c]);
                        }
                    }
                }
                string[] rc = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
                for (int k = 0; k < sides.Count; k++)
                {
                    string fp = AnimationUtility.CalculateTransformPath(sides[k].fore, inst.transform);
                    string hp = AnimationUtility.CalculateTransformPath(sides[k].hand, inst.transform);
                    for (int c = 0; c < 4; c++)
                    {
                        clip.SetCurve(fp, typeof(Transform), rc[c], new AnimationCurve(keys[k].fore[c]));
                        clip.SetCurve(hp, typeof(Transform), rc[c], new AnimationCurve(keys[k].hand[c]));
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(inst); }
        }

        static Vector3 FingerTip(Transform hand, List<Transform> fingers, string key)
        {
            var f = fingers.FirstOrDefault(t => t.name.EndsWith(key)) ?? fingers[0];
            return f.position;
        }

        // ══════════════════════════ 무기 장착 ══════════════════════════
        // Heroes/{id}/weapons.json + weapon_{src}.fbx/png (Tools/weapons/fetch_weapons.py, VARCO Generate3D).
        // 무기 메시의 긴 축(PCA)을 찾아 손잡이(grip 비율) 위치를 손 뼈대에 붙인다 → 공격 모션에서 손을 그대로 따라감.
        //   blade  : 팔과 수직, 몸 앞쪽으로 (검·도끼·망치)
        //   pole   : 팔과 수직, 위쪽으로 세움 (지팡이·창·낫) — 팔이 수직이면 앞쪽
        //   bow    : pole 과 같음 (왼손)
        //   shield : 아래팔 바깥쪽, 긴 축은 팔을 따라

        [Serializable] class WeaponCfg { public string slot, src, hand, kind; public float length, grip; public bool flip; }
        [Serializable] class WeaponList { public List<WeaponCfg> weapons; }

        static int AttachWeapons(string dir, string id, GameObject model)
        {
            string cfgPath = $"{dir}/weapons.json";
            if (!File.Exists(cfgPath)) return 0;
            var list = JsonUtility.FromJson<WeaponList>(File.ReadAllText(cfgPath));
            if (list?.weapons == null) return 0;
            int n = 0;
            foreach (var w in list.weapons)
            {
                string fbx = $"{dir}/weapon_{w.src}.fbx";
                if (!File.Exists(fbx)) continue;
                AssetDatabase.ImportAsset(fbx);
                var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
                if (imp.animationType != ModelImporterAnimationType.None || imp.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    imp.animationType = ModelImporterAnimationType.None;
                    imp.importAnimation = false;
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.SaveAndReimport();
                }
                string bone = w.hand == "L" ? "Left" : "Right";
                var hand = FindDeep(model.transform, bone + "Hand");
                var fore = FindDeep(model.transform, bone + "ForeArm");
                if (hand == null || fore == null) continue;

                // 재질
                string matPath = $"{dir}/{id}_weapon_{w.src}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/weapon_{w.src}.png");
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.4f);
                mat.SetFloat("_Metallic", 0.2f);
                EditorUtility.SetDirty(mat);

                var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                var wi = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(wi, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                wi.transform.position = Vector3.zero;          // 회전·스케일은 FBX 가져오기 값 유지 (VARCO FBX 는 X 270° = 이미지 위쪽이 +Y)
                foreach (var r in wi.GetComponentsInChildren<Renderer>(true)) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }

                // 무기 정점 → 주축(PCA)
                var pts = new List<Vector3>();
                foreach (var mf in wi.GetComponentsInChildren<MeshFilter>(true))
                    foreach (var v in mf.sharedMesh.vertices) pts.Add(mf.transform.TransformPoint(v));
                if (pts.Count < 10) { UnityEngine.Object.DestroyImmediate(wi); continue; }
                Vector3 c = Vector3.zero; foreach (var p in pts) c += p; c /= pts.Count;
                var e1 = PrincipalAxis(pts, c, null);
                if (e1.y < 0) e1 = -e1;                        // 이미지에서 위쪽 = 머리(날·보석) 쪽
                if (w.flip) e1 = -e1;
                var e3 = SmallestAxis(pts, c, e1);
                float tmin = float.MaxValue, tmax = float.MinValue;
                foreach (var p in pts) { float t = Vector3.Dot(p - c, e1); tmin = Mathf.Min(tmin, t); tmax = Mathf.Max(tmax, t); }
                float L = Mathf.Max(1e-4f, tmax - tmin);
                Vector3 grip = c + e1 * (tmin + w.grip * L);
                float s = w.length / L;

                // 컨테이너: 원점 = 손잡이, +Y = 무기 축, +X = 무기의 얇은 축
                var holder = new GameObject("Weapon_" + w.slot).transform;
                var q = Quaternion.LookRotation(Vector3.Cross(e3, e1), e1);   // canonical → 무기 축
                var inv = Quaternion.Inverse(q);
                // 무기 루트(가져오기 회전 포함)를 감싸는 피벗: 월드 기준으로 계산한 축·손잡이를 정렬
                var pivot = new GameObject("Mesh").transform;
                pivot.SetParent(holder, false);
                wi.transform.SetParent(pivot, true);
                pivot.localRotation = inv;
                pivot.localScale = Vector3.one * s;
                pivot.localPosition = -(inv * grip) * s;

                // 손(또는 아래팔) 기준 방향
                Vector3 f = (hand.position - fore.position).normalized;            // 아래팔 → 손
                Vector3 fwd = Vector3.forward, up = Vector3.up;
                Vector3 side = w.hand == "L" ? Vector3.left : Vector3.right;
                Vector3 d, pos;
                if (w.kind == "shield")
                {
                    d = -f;
                    pos = Vector3.Lerp(fore.position, hand.position, 0.55f) + side * 0.035f;
                }
                else
                {
                    Vector3 Perp(Vector3 v) => v - f * Vector3.Dot(v, f);
                    if (w.kind == "pole" || w.kind == "bow")
                        d = (up + fwd * 0.15f).normalized;                          // 옆에 세워 든 지팡이·창·활
                    else
                    {
                        var pf = Perp(fwd);
                        if (pf.sqrMagnitude < 1e-4f) pf = fwd;
                        d = (pf.normalized + f * 0.9f).normalized;                  // 검: 앞과 팔 방향 사이 (대기 때 비스듬히 아래로)
                    }
                    pos = hand.position + f * 0.035f;                             // 손목 → 손바닥
                    // 손가락 본이 있으면(쥐는 자세): 손잡이를 말아 쥔 손가락 고리의 가운데 (손가락 뿌리 관절 ↔ 둘째 관절 중간)
                    var fs = hand.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Finger")).ToList();
                    Transform Fg(string k) => fs.FirstOrDefault(t => t.name.EndsWith(k));
                    var k1 = Fg("Finger1"); var k4 = Fg("Finger4"); var p1 = Fg("Finger11"); var p4 = Fg("Finger41");
                    if (k1 != null && k4 != null && p1 != null && p4 != null)
                        pos = ((k1.position + k4.position) * 0.5f + (p1.position + p4.position) * 0.5f) * 0.5f;
                }
                Vector3 xw = side - d * Vector3.Dot(side, d);
                if (xw.sqrMagnitude < 1e-4f) xw = Vector3.Cross(d, fwd);
                xw.Normalize();
                holder.position = pos;
                holder.rotation = Quaternion.LookRotation(Vector3.Cross(xw, d), d);
                holder.SetParent(w.kind == "shield" ? fore : hand, true);
                n++;
            }
            return n;
        }

        /// <summary>
        /// 활: 손에 고정된 각도 그대로면 팔을 들 때 활이 눕는다. 공격 모션의 매 프레임마다 팔이 들린 만큼
        /// 활을 세로로 세우고(축 = 위) 활 면이 겨누는 방향을 향하도록 홀더 회전 키를 굽는다 (대기 자세는 그대로).
        /// </summary>
        static void AimBows(string dir, GameObject inst, AnimationClip clip)
        {
            string cfgPath = $"{dir}/weapons.json";
            if (!File.Exists(cfgPath)) return;
            var list = JsonUtility.FromJson<WeaponList>(File.ReadAllText(cfgPath));
            if (list?.weapons == null) return;
            foreach (var w in list.weapons.Where(x => x.kind == "bow"))
            {
                var holder = FindDeep(inst.transform, "Weapon_" + w.slot);
                string bone = w.hand == "L" ? "Left" : "Right";
                var hand = FindDeep(inst.transform, bone + "Hand");
                var fore = FindDeep(inst.transform, bone + "ForeArm");
                if (holder == null || hand == null || fore == null) continue;
                Quaternion rest = holder.localRotation;
                string path = AnimationUtility.CalculateTransformPath(holder, inst.transform);
                int n = Mathf.Max(2, Mathf.CeilToInt(clip.length * 30f) + 1);
                var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                Quaternion prev = rest;
                for (int i = 0; i < n; i++)
                {
                    float t = clip.length * i / (n - 1);
                    clip.SampleAnimation(inst, t);
                    Vector3 f = (hand.position - fore.position).normalized;              // 아래팔 → 손 (겨누는 방향)
                    float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((f.y + 0.85f) / 0.6f)); // 팔이 내려가 있으면 0, 수평 가까이 들면 1
                    Vector3 up = Vector3.up;
                    Vector3 aim = f - up * Vector3.Dot(f, up);
                    Quaternion local = rest;
                    if (k > 0 && aim.sqrMagnitude > 1e-4f)
                    {
                        aim.Normalize();
                        // 홀더: +Y = 활 축(위), +X = 얇은 축 → 겨누는 방향 (활 면이 과녁을 본다)
                        var world = Quaternion.LookRotation(Vector3.Cross(aim, up), up);
                        var aimLocal = Quaternion.Inverse(hand.rotation) * world;
                        local = Quaternion.Slerp(rest, aimLocal, k);
                    }
                    if (Quaternion.Dot(prev, local) < 0) local = new Quaternion(-local.x, -local.y, -local.z, -local.w);
                    prev = local;
                    curves[0].AddKey(t, local.x); curves[1].AddKey(t, local.y); curves[2].AddKey(t, local.z); curves[3].AddKey(t, local.w);
                }
                string[] rc = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
                for (int c = 0; c < 4; c++) clip.SetCurve(path, typeof(Transform), rc[c], curves[c]);
                EditorUtility.SetDirty(clip);
                holder.localRotation = rest;
            }
            AssetDatabase.SaveAssets();
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform ch in t) { var r = FindDeep(ch, name); if (r != null) return r; }
            return null;
        }

        static Vector3 PrincipalAxis(List<Vector3> pts, Vector3 c, Vector3? exclude)
        {
            // 공분산 행렬의 최대 고유벡터 (거듭제곱법)
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (var p in pts)
            {
                var v = p - c;
                if (exclude.HasValue) v -= exclude.Value * Vector3.Dot(v, exclude.Value);
                xx += v.x * v.x; xy += v.x * v.y; xz += v.x * v.z; yy += v.y * v.y; yz += v.y * v.z; zz += v.z * v.z;
            }
            Vector3 a = new Vector3(0.3f, 1f, 0.2f).normalized;
            for (int i = 0; i < 64; i++)
            {
                var b = new Vector3(xx * a.x + xy * a.y + xz * a.z, xy * a.x + yy * a.y + yz * a.z, xz * a.x + yz * a.y + zz * a.z);
                if (b.sqrMagnitude < 1e-12f) break;
                a = b.normalized;
            }
            return a;
        }

        static Vector3 SmallestAxis(List<Vector3> pts, Vector3 c, Vector3 e1)
        {
            var e2 = PrincipalAxis(pts, c, e1);                 // 두 번째 축 (폭)
            e2 = (e2 - e1 * Vector3.Dot(e2, e1)).normalized;
            return Vector3.Cross(e1, e2).normalized;            // 세 번째 축 (두께)
        }

        /// <summary>미리보기: 대기 · 공격 · 피격 자세를 가로로 렌더해 PNG 로 저장</summary>
        public static void RenderPreview(string id, string outPng)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutRoot}/{id}.prefab");
            if (prefab == null) return;
            var poses = new[] { ("idle", 0.5f), ("attack", 0.45f), ("hit", 0.35f) };
            const int S = 384;
            var rt = new RenderTexture(S, S, 24);
            var outTex = new Texture2D(S * poses.Length, S, TextureFormat.RGB24, false);
            var lightGo = new GameObject("TmpLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(35, 160, 0);
            for (int i = 0; i < poses.Length; i++)
            {
                var go = (GameObject)UnityEngine.Object.Instantiate(prefab, new Vector3(200 + i * 10, 0, 0), Quaternion.identity);
                var model = go.transform.Find("Model").gameObject;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SrcRoot}/{id}/{id}_{poses[i].Item1}.anim");
                if (clip != null) clip.SampleAnimation(model, clip.length * poses[i].Item2);
                var camGo = new GameObject("TmpCam");
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.18f, 0.24f);
                cam.transform.position = go.transform.position + new Vector3(0.5f, 0.75f, 2.4f);
                cam.transform.LookAt(go.transform.position + new Vector3(0, 0.5f, 0));
                cam.fieldOfView = 34;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                outTex.ReadPixels(new Rect(0, 0, S, S), i * S, 0);
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
            UnityEngine.Object.DestroyImmediate(lightGo);
            outTex.Apply();
            File.WriteAllBytes(outPng, outTex.EncodeToPNG());
        }

        /// <summary>무기 든 손 근접: 윗줄 무기 숨김 · 아랫줄 무기 표시, 정면 · 바깥 · 뒤 · 안쪽 앞 (대기 자세)</summary>
        public static void RenderGrip(string id, string outPng)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutRoot}/{id}.prefab");
            var sides = WeaponHands($"{SrcRoot}/{id}").ToList();
            if (prefab == null || sides.Count == 0) return;
            var lightGo = new GameObject("TmpLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(35, 160, 0);
            const int S = 320;
            var rt = new RenderTexture(S, S, 24);
            var tex = new Texture2D(S * 4, S * 2 * sides.Count, TextureFormat.RGB24, false);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SrcRoot}/{id}/{id}_idle.anim");
            int rowY = tex.height;
            foreach (var side in sides)
                for (int row = 0; row < 2; row++)
                {
                    rowY -= S;
                    var go = (GameObject)UnityEngine.Object.Instantiate(prefab, new Vector3(400, 0, 0), Quaternion.identity);
                    if (clip != null) clip.SampleAnimation(go.transform.Find("Model").gameObject, clip.length * 0.5f);
                    if (row == 0) foreach (var w in go.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Weapon_")).ToList()) w.gameObject.SetActive(false);
                    var hand = FindDeep(go.transform, side + "Hand");
                    var fore = FindDeep(go.transform, side + "ForeArm");
                    var c = hand.position + (hand.position - fore.position).normalized * 0.04f;
                    var outer = side == "Left" ? Vector3.left : Vector3.right;
                    var dirs = new[] { Vector3.forward, outer, Vector3.back, (-outer + Vector3.forward).normalized };
                    for (int i = 0; i < 4; i++)
                    {
                        var cam = new GameObject("TmpCam").AddComponent<Camera>();
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.18f, 0.18f, 0.24f);
                        cam.nearClipPlane = 0.01f;
                        cam.transform.position = c + dirs[i] * 0.3f;
                        cam.transform.LookAt(c);
                        cam.fieldOfView = 34;
                        cam.targetTexture = rt;
                        cam.Render();
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, S, S), i * S, rowY);
                        RenderTexture.active = null;
                        UnityEngine.Object.DestroyImmediate(cam.gameObject);
                    }
                    UnityEngine.Object.DestroyImmediate(go);
                }
            UnityEngine.Object.DestroyImmediate(lightGo);
            tex.Apply();
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
        }

        /// <summary>손 근접 4컷 (왼손 정면 · 왼손 바깥 · 오른손 정면 · 오른손 바깥), 대기 자세</summary>
        public static void RenderHandsClose(string id, string outPng, float t = 0.5f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutRoot}/{id}.prefab");
            if (prefab == null) return;
            var go = (GameObject)UnityEngine.Object.Instantiate(prefab, new Vector3(400, 0, 0), Quaternion.identity);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SrcRoot}/{id}/{id}_idle.anim");
            if (clip != null) clip.SampleAnimation(go.transform.Find("Model").gameObject, clip.length * t);
            var lightGo = new GameObject("TmpLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(35, 160, 0);
            const int S = 384;
            var rt = new RenderTexture(S, S, 24);
            var tex = new Texture2D(S * 4, S, TextureFormat.RGB24, false);
            int n = 0;
            foreach (var side in new[] { "Left", "Right" })
            {
                var hand = FindDeep(go.transform, side + "Hand");
                if (hand == null) { n += 2; continue; }
                var c = hand.position + (hand.position - FindDeep(go.transform, side + "ForeArm").position).normalized * 0.04f;
                foreach (var dir in new[] { Vector3.forward, side == "Left" ? Vector3.left : Vector3.right })
                {
                    var cam = new GameObject("TmpCam").AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.18f, 0.18f, 0.24f);
                    cam.nearClipPlane = 0.01f;
                    cam.transform.position = c + dir * 0.3f;
                    cam.transform.LookAt(c);
                    cam.fieldOfView = 34;
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, S, S), n * S, 0);
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
                    n++;
                }
            }
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(lightGo);
            tex.Apply();
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
        }

        /// <summary>손 확인용 근접 렌더 (대기 자세, 허리 높이)</summary>
        public static void RenderHands(string id, string outPng)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutRoot}/{id}.prefab");
            if (prefab == null) return;
            const int S = 768;
            var rt = new RenderTexture(S, S, 24);
            var outTex = new Texture2D(S, S, TextureFormat.RGB24, false);
            var lightGo = new GameObject("TmpLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(35, 160, 0);
            var go = (GameObject)UnityEngine.Object.Instantiate(prefab, new Vector3(300, 0, 0), Quaternion.identity);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{SrcRoot}/{id}/{id}_idle.anim");
            if (clip != null) clip.SampleAnimation(go.transform.Find("Model").gameObject, clip.length * 0.5f);
            var cam = new GameObject("TmpCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.18f, 0.24f);
            cam.transform.position = go.transform.position + new Vector3(0.15f, 0.5f, 1.1f);
            cam.transform.LookAt(go.transform.position + new Vector3(0, 0.46f, 0));
            cam.fieldOfView = 34;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            outTex.ReadPixels(new Rect(0, 0, S, S), 0, 0);
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(lightGo);
            outTex.Apply();
            File.WriteAllBytes(outPng, outTex.EncodeToPNG());
        }
    }
}
#endif
