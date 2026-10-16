using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrikerZ.EditorTools
{
    /// <summary>
    /// 첫 컴파일 시 게임 씬(Assets/StrikerZ/Scenes/StrikerZ.unity)을 자동 생성하고 빌드 세팅 첫 번째로 등록.
    /// 메뉴: StrikerZ > Create Game Scene
    /// </summary>
    [InitializeOnLoad]
    static class StrikerZSetup
    {
        const string ScenePath = "Assets/StrikerZ/Scenes/StrikerZ.unity";

        static StrikerZSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(ScenePath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (EditorSceneManager.GetActiveScene().isDirty) return; // 사용자의 작업 중 씬은 건드리지 않음
                CreateScene();
            };
        }

        [MenuItem("StrikerZ/Create Game Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            new GameObject("GameRoot").AddComponent<GameRoot>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[StrikerZ] 게임 씬 생성 완료: " + ScenePath + " — Play 를 눌러 시작하세요.");
        }

        [MenuItem("StrikerZ/Open Game Scene")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { CreateScene(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
    }

    /// <summary>StrikerZ 텍스처 임포트 설정 (UI 선명도)</summary>
    class StrikerZTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/StrikerZ/Resources/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = !(assetPath.Contains("/Textures/logo") || assetPath.Contains("/Textures/title_bg"));
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (assetPath.EndsWith("_normal.png"))
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.alphaIsTransparency = false;
            }
        }
    }
}
