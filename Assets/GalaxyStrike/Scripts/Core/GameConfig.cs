namespace GalaxyStrike.Core
{
    // ============================================================================
    // 【全局配置常量 · GameConfig.cs】
    // 对应 C++ 原工程 GameConfig.h：
    // 定义了屏幕网格尺寸、空战交火空域、HUD 区域、数值上限等核心常量。
    // ============================================================================

    public static class GameConfig
    {
        // ---- 控制台整体尺寸（超宽屏全景视界） ----
        public const int SCREEN_W = 140; // 总列宽（字符）
        public const int SCREEN_H = 45;  // 总行高（字符）

        // ---- 核心空战交火区域（战机自由飞行与弹幕走位空域） ----
        public const int PLAY_MIN_X = 1;
        public const int PLAY_MAX_X = 104; // 宽达 104 格的大纵深空战视野
        public const int PLAY_MIN_Y = 1;
        public const int PLAY_MAX_Y = 43;  // 高达 43 行的纵向弹幕回旋余地

        public const int PLAY_WIDTH = PLAY_MAX_X - PLAY_MIN_X + 1; // 104 字符
        public const int PLAY_HEIGHT = PLAY_MAX_Y - PLAY_MIN_Y + 1; // 43 字符

        // ---- 战术 HUD 仪表盘区域（右侧独立全息侧边栏） ----
        public const int HUD_X = 106;
        public const int HUD_Y = 0;
        public const int HUD_W = 34;
        public const int HUD_H = 45;

        // ---- 物理引擎步长与渲染帧率 ----
        public const int TARGET_FPS = 60;
        public const float FIXED_DT = 0.016f; // 固定物理更新时间步长（秒）

        // ---- 游戏系统数值上限平衡约束 ----
        public const int MAX_PLAYER_LIVES = 5; // 玩家最多可拥有的战机命数
        public const int MAX_BOMBS = 5;        // 战术全屏 EMP 核弹上限
        public const int MAX_SHIP_LEVEL = 5;   // 战机形态进化阶数上限 (1~5 阶)
        public const int MAX_BULLET_LEVEL = 5; // 武器弹幕火力阶数上限 (1~5 阶)

        // ---- Unity 视觉呈现世界坐标转换参数 ----
        // 游戏逻辑使用与 C++ 100% 相同的 (1~104, 1~43) 亚像素坐标，
        // 在向 Unity 场景投影时，通过该比例平移缩放为 Unity 世界空间。
        public const float WORLD_UNIT_SCALE = 0.2f;
        public const float WORLD_ORIGIN_X = 52.5f;
        public const float WORLD_ORIGIN_Y = 22.0f;

        /// <summary>
        /// 逻辑网格坐标转 Unity 世界空间坐标 (Z=0, Y向上)
        /// </summary>
        public static UnityEngine.Vector3 GridToWorld(UnityEngine.Vector2 gridPos)
        {
            float wx = (gridPos.x - WORLD_ORIGIN_X) * WORLD_UNIT_SCALE;
            float wy = (WORLD_ORIGIN_Y - gridPos.y) * WORLD_UNIT_SCALE;
            return new UnityEngine.Vector3(wx, wy, 0f);
        }
    }
}
