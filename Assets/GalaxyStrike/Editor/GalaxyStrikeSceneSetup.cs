using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GalaxyStrike.Presentation;
using GalaxyStrike.UI;

namespace GalaxyStrike.Editor
{
    // ============================================================================
    // 【一键场景与工程配置工具 · GalaxyStrikeSceneSetup.cs】
    // 在 Unity 顶部菜单栏添加 "Galaxy Strike" 菜单项：
    // - 一键创建并配置专属的 GalaxyStrike.unity 场景
    // - 自动添加并配置正交相机、游戏主控与全息 HUD
    // - 自动将场景加入 Build Settings
    // ============================================================================

    [InitializeOnLoad]
    public static class GalaxyStrikeAutoInitializer
    {
        static GalaxyStrikeAutoInitializer()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!File.Exists(GalaxyStrikeSceneSetup.ScenePath))
            {
                Debug.Log("<color=#00FFCC><b>[Galaxy Strike AutoSetup]</b> 检测到未创建打飞机场景，正在自动生成...</color>");
                GalaxyStrikeSceneSetup.CreateSceneInternal(false);
            }
            else
            {
                GalaxyStrikeSceneSetup.EnsureBuildSettings(GalaxyStrikeSceneSetup.ScenePath);
            }

            // 自动静默运行一次逻辑单元测试，确保代码与算法100%正确
            bool allPassed = GalaxyStrikeLogicTests.RunAllTestsSilently();
            if (allPassed)
            {
                Debug.Log("<color=#00FF88><b>[Galaxy Strike]</b> 全部 12 项面向对象核心战斗逻辑单元测试已自动化通过验证！</color>");
            }
            else
            {
                Debug.LogWarning("<b>[Galaxy Strike]</b> 部分单元测试未通过，请检查日志！");
            }
        }
    }

    public static class GalaxyStrikeSceneSetup
    {
        public const string SceneDirectory = "Assets/GalaxyStrike/Scenes";
        public const string ScenePath = "Assets/GalaxyStrike/Scenes/GalaxyStrike.unity";
        public const string PrefabDirectory = "Assets/GalaxyStrike/Prefabs";
        public const string PrefabPath = "Assets/GalaxyStrike/Prefabs/GalaxyStrike_GameMaster.prefab";

        [MenuItem("Galaxy Strike/Create & Open Galaxy Strike Scene (创建并打开打飞机场景)", false, 1)]
        public static void CreateGalaxyStrikeScene()
        {
            CreateSceneInternal(true);
        }

        [MenuItem("Galaxy Strike/Create GameMaster Prefab (创建主控预制体模型)", false, 3)]
        public static void CreateGameMasterPrefab()
        {
            EnsurePrefabCreated();
            EditorUtility.DisplayDialog("Galaxy Strike 预制体创建成功",
                $"已成功在 {PrefabPath} 创建 GalaxyStrike_GameMaster 预制体模型！",
                "确定");
        }

        public static void CreateSceneInternal(bool interactive)
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            EnsurePrefabCreated();

            // 新建场景
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. 创建并设置 Main Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(3.2f, 0.0f, -10.0f);
            camGo.AddComponent<AudioListener>();

            // 2. 实例化或创建 GalaxyStrikeGame 主控制器
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject gameGo;
            if (prefab != null)
            {
                gameGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                gameGo = new GameObject("GalaxyStrike_GameMaster");
                gameGo.AddComponent<GalaxyStrikeRenderer>();
                gameGo.AddComponent<GalaxyStrikeHUD>();
                gameGo.AddComponent<GalaxyStrikeGame>();
            }

            // 3. 保存场景
            EditorSceneManager.SaveScene(newScene, ScenePath);
            AssetDatabase.Refresh();

            // 4. 将场景添加到 EditorBuildSettings 第一位
            EnsureBuildSettings(ScenePath);

            Debug.Log($"<color=#00FFCC><b>[Galaxy Strike]</b> 场景创建成功！路径：{ScenePath}</color>");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Galaxy Strike 场景创建成功",
                    "星际突袭 (Galaxy Strike) 游戏场景已成功创建并打开！\n\n点击 Unity 编辑器顶部的【Play (运行)】按钮即可直接开战！",
                    "立即开战！");
            }
        }

        public static void EnsurePrefabCreated()
        {
            if (!Directory.Exists(PrefabDirectory))
            {
                Directory.CreateDirectory(PrefabDirectory);
                AssetDatabase.Refresh();
            }

            if (!File.Exists(PrefabPath))
            {
                var tempGo = new GameObject("GalaxyStrike_GameMaster");
                tempGo.AddComponent<GalaxyStrikeRenderer>();
                tempGo.AddComponent<GalaxyStrikeHUD>();
                tempGo.AddComponent<GalaxyStrikeGame>();
                PrefabUtility.SaveAsPrefabAsset(tempGo, PrefabPath);
                Object.DestroyImmediate(tempGo);
                AssetDatabase.Refresh();
                Debug.Log($"<color=#00FFCC><b>[Galaxy Strike]</b> 已生成主控预制体：{PrefabPath}</color>");
            }
        }

        public static void EnsureBuildSettings(string scenePath = ScenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            bool alreadyFirst = scenes.Length > 0 && scenes[0].path == scenePath;
            if (alreadyFirst) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene(scenePath, true));

            foreach (var s in scenes)
            {
                if (s.path != scenePath)
                {
                    list.Add(s);
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"<color=#00FFCC><b>[Galaxy Strike]</b> 已将 {scenePath} 设为主运行场景 (Scene 0)。</color>");
        }
    }
}
