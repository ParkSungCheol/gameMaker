using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameMaker.Dev
{
    /// <summary>
    /// 안드로이드 APK 빌드 메뉴.
    /// 게임은 GameBootstrap(RuntimeInitializeOnLoadMethod)으로 뜨므로 빈 부트 씬 하나만 있으면 된다.
    /// 결과물: c:/GameMaker_app/build/BomulWonjeongdae.apk (디버그 서명 — 폰에 바로 설치 가능)
    /// </summary>
    public static class AndroidBuildMenu
    {
        const string ScenePath = "Assets/Scenes/Boot.unity";
        const string OutputPath = "c:/GameMaker_app/build/BomulWonjeongdae.apk";

        [MenuItem("GameMaker/Android APK 빌드")]
        public static void BuildApk()
        {
            // 0) 한글 사용자명 경로 우회 — Gradle 캐시/임시 폴더가 C:\Users\박성철\... 에 있으면
            //    prefab 배치 스크립트가 인코딩 문제로 깨진다. 빌드 프로세스에만 영문 경로를 쓰게 한다.
            System.IO.Directory.CreateDirectory("C:/GradleHome");
            System.IO.Directory.CreateDirectory("C:/UnityTemp");
            System.Environment.SetEnvironmentVariable("GRADLE_USER_HOME", "C:/GradleHome");
            System.Environment.SetEnvironmentVariable("TMP", "C:/UnityTemp");
            System.Environment.SetEnvironmentVariable("TEMP", "C:/UnityTemp");

            // 1) 빈 부트 씬 보장
            if (!System.IO.File.Exists(ScenePath))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            // 2) 앱 정보 / 안드로이드 설정
            PlayerSettings.companyName = "ParkSungCheol";
            PlayerSettings.productName = "보물 원정대";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.parksungcheol.bomulwonjeongdae");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            // 3) 빌드 (디버그 키 자동 서명)
            System.IO.Directory.CreateDirectory("c:/GameMaker_app/build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("[AndroidBuild] 성공! " + OutputPath +
                    " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                EditorUtility.RevealInFinder(OutputPath);
            }
            else
            {
                Debug.LogError("[AndroidBuild] 실패 — Console 의 에러를 확인하세요. result=" + report.summary.result);
            }
        }
    }
}
