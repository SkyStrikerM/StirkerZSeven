using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StrikerZ.EditorTools
{
    /// <summary>
    /// 명령줄 빌드 (Tools/build/build_windows.bat). 에디터 메뉴: StrikerZ > Build > Windows.
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod StrikerZ.EditorTools.BuildCli.BuildWindows
    ///             [-buildOut Builds/Windows/StrikerZ.exe] [-buildDev]
    /// 게임 씬(StrikerZ.unity) 하나만 넣는다. 실패하면 종료 코드 1.
    /// </summary>
    public static class BuildCli
    {
        const string Scene = "Assets/StrikerZ/Scenes/StrikerZ.unity";
        const string DefaultOut = "Builds/Windows/StrikerZ.exe";

        [MenuItem("StrikerZ/Build/Windows")]
        static void BuildWindowsMenu() => Run(DefaultOut, false, false);

        [MenuItem("StrikerZ/Build/Windows (Development)")]
        static void BuildWindowsDevMenu() => Run(DefaultOut, true, false);

        /// <summary>-executeMethod 진입점</summary>
        public static void BuildWindows()
        {
            string output = Arg("-buildOut") ?? DefaultOut;
            bool dev = HasArg("-buildDev");
            Run(output, dev, Application.isBatchMode);
        }

        static void Run(string output, bool dev, bool exitWhenDone)
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            Debug.Log($"[BuildCli] Windows 빌드 시작 → {output}{(dev ? " (Development)" : "")}");
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            bool ok = s.result == BuildResult.Succeeded;
            Debug.Log($"[BuildCli] {(ok ? "성공" : "실패")} · {s.result} · {s.totalSize / (1024f * 1024f):0.0} MB · {s.totalTime.TotalSeconds:0}초 · 오류 {s.totalErrors}");
            if (exitWhenDone) EditorApplication.Exit(ok ? 0 : 1);
            else if (ok) EditorUtility.RevealInFinder(output);
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }
}
