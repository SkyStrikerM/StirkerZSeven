#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StrikerZ.EditorTools
{
    /// <summary>
    /// VARCO 로 만든 레이드 보스 3D 메시(리그 없음)를 프리팹으로 조립한다.
    /// 입력: Assets/StrikerZ/Bosses/{id}/model.fbx · albedo.png  (Tools/raid/fetch_bosses.py)
    /// 출력: Assets/StrikerZ/Resources/StrikerZ/Bosses/{id}.prefab — 움직임은 BossModel 이 절차적으로 준다
    /// </summary>
    public static class BossModelBuilder
    {
        const string SrcRoot = "Assets/StrikerZ/Bosses";
        const string OutRoot = "Assets/StrikerZ/Resources/StrikerZ/Bosses";

        [MenuItem("StrikerZ/레이드 보스 3D 프리팹 만들기")]
        public static void BuildAll()
        {
            if (!Directory.Exists(SrcRoot)) return;
            foreach (var dir in Directory.GetDirectories(SrcRoot))
                Debug.Log(Build(Path.GetFileName(dir)));
        }

        public static string Build(string id)
        {
            string dir = $"{SrcRoot}/{id}";
            string fbx = $"{dir}/model.fbx";
            if (!File.Exists(fbx)) return $"{id}: model.fbx 없음";
            AssetDatabase.ImportAsset(fbx);
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (imp.animationType != ModelImporterAnimationType.None || imp.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/albedo.png");
            string matPath = $"{dir}/{id}_mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0.05f);
            // 보드 조명이 닿지 않는 뒤편에서도 존재감이 보이게 텍스처를 살짝 자체 발광
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", tex);
            mat.SetColor("_EmissionColor", new Color(0.28f, 0.28f, 0.28f));
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = new GameObject(id);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(src);
            model.transform.SetParent(root.transform, false);
            int n = 0;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                n++;
            }
            Directory.CreateDirectory(OutRoot);
            string outPath = $"{OutRoot}/{id}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return $"{id}: 프리팹 저장 (렌더러 {n}, 텍스처 {(tex != null ? "O" : "X")}) → {outPath}";
        }
    }
}
#endif
