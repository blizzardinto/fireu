using System;
using UnityEngine;

namespace GalaxyStrike.Core
{
    // ============================================================================
    // 【数学与类型基石 · Types.cs】
    // 对应 C++ 原工程 Types.h：
    // 封装了二维亚像素数学扩展、AABB 矩形包围盒、强类型枚举与配色方案。
    // ============================================================================

    public static class Vector2Extensions
    {
        public static float DistanceTo(this Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        public static float Length(this Vector2 v)
        {
            return v.magnitude;
        }

        public static float LengthSquared(this Vector2 v)
        {
            return v.sqrMagnitude;
        }

        public static Vector2 NormalizedSafe(this Vector2 v)
        {
            float mag = v.magnitude;
            return mag > 0.0001f ? v / mag : Vector2.zero;
        }
    }

    /// <summary>
    /// 跨平台轻量级随机数生成器（规避 Unity 原生 ECall 依赖，保障脱机单元测试与高频性能）
    /// </summary>
    public static class GameRandom
    {
        private static readonly System.Random s_rand = new System.Random();

        public static int Range(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) return minInclusive;
            return s_rand.Next(minInclusive, maxExclusive);
        }

        public static float Range(float minInclusive, float maxInclusive)
        {
            if (minInclusive >= maxInclusive) return minInclusive;
            return (float)(minInclusive + s_rand.NextDouble() * (maxInclusive - minInclusive));
        }
    }

    /// <summary>
    /// 轴对齐矩形包围盒 (AABB - Axis-Aligned Bounding Box)
    /// 对应原工程 Rect，实现高精度相交检测。
    /// </summary>
    [Serializable]
    public struct Rect2D
    {
        public float x;
        public float y;
        public float w;
        public float h;

        public Rect2D(float x, float y, float w, float h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        public bool Intersects(Rect2D o)
        {
            return (x < o.x + o.w && x + w > o.x &&
                    y < o.y + o.h && y + h > o.y);
        }

        public bool Contains(Vector2 p)
        {
            return (p.x >= x && p.x <= x + w &&
                    p.y >= y && p.y <= y + h);
        }
    }

    /// <summary>
    /// 子弹阵营归属
    /// </summary>
    public enum BulletOwner
    {
        Player, // 玩家战机射出的弹药（只攻击敌机）
        Enemy   // 敌机射出的弹幕（只攻击玩家）
    }

    /// <summary>
    /// 弹道类型枚举
    /// </summary>
    public enum BulletType
    {
        Normal,     // 基础等离子弹
        Twin,       // 双联极速弹
        Spread,     // 散射破甲弹
        Laser,      // 贯穿型高能激光
        Homing,     // 自动索敌追踪微型飞弹
        EnemyOrb,   // 敌方普通光球弹
        EnemyFast,  // 敌方高速突击弹
        BossHelix,  // Boss 旋转螺旋弹幕
        BombWave    // 全屏 EMP 震荡波
    }

    /// <summary>
    /// 战术物资掉落类型
    /// </summary>
    public enum PowerUpType
    {
        Firepower, // [P] 强化子弹火力等级 (Lv 1 ~ Lv 5)
        ShipExp,   // [U] 战机核心能量：增加战术经验并进阶形态 (Lv 1 ~ Lv 5)
        Shield,    // [S] 修复并充能能量护盾 (+50 护盾值)
        Bomb,      // [B] 补充战术清屏全屏 EMP 核弹
        Score      // [$] 高额战场荣誉积分 (+1000 分)
    }

    /// <summary>
    /// 敌机类别
    /// </summary>
    public enum EnemyType
    {
        Scout,   // 蜂群冲锋机（集群俯冲、快速微弹）
        Gunship, // 突击强袭舰（中型护甲、定点双联齐射）
        Bomber,  // 重装轰炸机（高甲重巡、三向散弹）
        Elite,   // 精英巡洋舰（护盾强化、旋转弹幕）
        Boss     // 灭世级星际母舰 BOSS（三阶段变身、全屏弹幕狂暴）
    }

    /// <summary>
    /// 波次管理器状态机
    /// </summary>
    public enum WaveState
    {
        Preparation, // 战术准备
        Spawning,    // 集群编队投放中
        Fighting,    // 全力交火中
        WaveClear    // 战区肃清休整
    }

    /// <summary>
    /// 游戏主状态机
    /// </summary>
    public enum GameState
    {
        Title,    // 游戏标题欢迎界面（按空格开战）
        Playing,  // 激烈空战进行中
        Paused,   // 战术暂停状态
        GameOver, // 任务失败（战机损毁）
        Victory   // 任务大捷（摧毁灭世母舰 BOSS）
    }

    /// <summary>
    /// 音效类型枚举
    /// </summary>
    public enum SfxType
    {
        Start,
        Shoot,
        EnemyHit,
        Explosion,
        PowerUp,
        ShipUpgrade,
        Bomb,
        BossAlert,
        PlayerHurt,
        SpeedUp,
        GameOver
    }

    /// <summary>
    /// 经典控制台配色向 Unity Color 转换表
    /// </summary>
    public static class GameColors
    {
        public static readonly Color Black = Color.black;
        public static readonly Color Blue = new Color(0.1f, 0.3f, 0.9f);
        public static readonly Color Green = new Color(0.1f, 0.8f, 0.2f);
        public static readonly Color Cyan = new Color(0.1f, 0.8f, 0.9f);
        public static readonly Color Red = new Color(0.9f, 0.15f, 0.15f);
        public static readonly Color Magenta = new Color(0.85f, 0.2f, 0.85f);
        public static readonly Color Yellow = new Color(0.9f, 0.85f, 0.1f);
        public static readonly Color White = Color.white;
        public static readonly Color BrightCyan = new Color(0.3f, 1.0f, 1.0f);
        public static readonly Color BrightGreen = new Color(0.3f, 1.0f, 0.3f);
        public static readonly Color BrightYellow = new Color(1.0f, 1.0f, 0.3f);
        public static readonly Color BrightMagenta = new Color(1.0f, 0.4f, 1.0f);
        public static readonly Color BrightRed = new Color(1.0f, 0.3f, 0.3f);
        public static readonly Color BrightBlue = new Color(0.2f, 0.6f, 1.0f);
        public static readonly Color BrightWhite = Color.white;
    }
}
