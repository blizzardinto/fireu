using UnityEngine;
using GalaxyStrike.Presentation;
using GalaxyStrike.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GalaxyStrike
{
    // ============================================================================
    // 【零门槛即时启动器 · GalaxyStrikeBootstrapper.cs】
    // 使得无论当前打开的是哪个场景（如初学者教程场景），
    // 运行游戏后只需按下 [F1] 键或点击屏幕左上角浮动按钮，
    // 即可直接就地呼出《星际突袭：银河战线》，畅快体验重构的完整空战游戏！
    // ============================================================================

    public class GalaxyStrikeBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (FindAnyObjectByType<GalaxyStrikeGame>() != null) return;
            if (FindAnyObjectByType<GalaxyStrikeBootstrapper>() != null) return;

            var bootGo = new GameObject("[GalaxyStrike_QuickLauncher]");
            DontDestroyOnLoad(bootGo);
            bootGo.AddComponent<GalaxyStrikeBootstrapper>();
        }

        private bool m_launched = false;

        private void Start()
        {
            // 自动就绪并启动星际突袭游戏
            LaunchGame();
        }

        private void Update()
        {
            if (m_launched) return;

            bool f1Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                f1Pressed = true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.F1)) f1Pressed = true;
            }
            catch {}

            if (f1Pressed)
            {
                LaunchGame();
            }
        }

        private void OnGUI()
        {
            if (m_launched) return;

            // 在左上角绘制显眼的启动胶囊按钮
            Rect btnRect = new Rect(16, 16, 260, 42);
            GUI.color = new Color(0.1f, 0.9f, 1.0f, 0.95f);
            if (GUI.Button(btnRect, "★ 启动星际突袭 (Galaxy Strike) [F1] ★"))
            {
                LaunchGame();
            }
            GUI.color = Color.white;
        }

        public void LaunchGame()
        {
            if (m_launched) return;
            m_launched = true;

            // 检查场景内是否有 GalaxyStrikeGame
            if (FindAnyObjectByType<GalaxyStrikeGame>() == null)
            {
                var gameGo = new GameObject("GalaxyStrike_GameMaster");
                gameGo.AddComponent<GalaxyStrikeRenderer>();
                gameGo.AddComponent<GalaxyStrikeHUD>();
                gameGo.AddComponent<GalaxyStrikeGame>();
            }

            Destroy(gameObject);
        }
    }
}
