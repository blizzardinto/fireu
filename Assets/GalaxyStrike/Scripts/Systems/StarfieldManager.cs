using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Systems
{
    // ============================================================================
    // 【视差太空星尘滚动背景 · StarfieldManager.cs】
    // 对应 C++ 原工程 Starfield.h / Starfield.cpp：
    // 3 层视差太空星尘滚动背景，Boss 战触发曲率跃迁加速。
    // ============================================================================

    public struct StarData
    {
        public float x;
        public float y;
        public float speed;
        public char  symbol;
        public Color color;
    }

    public class StarfieldManager
    {
        private readonly List<StarData> m_stars = new List<StarData>();
        private bool                    m_warp;

        public StarfieldManager(int count = 140)
        {
            m_warp = false;
            for (int i = 0; i < count; i++)
            {
                float x = GameRandom.Range(GameConfig.PLAY_MIN_X, GameConfig.PLAY_MAX_X + 1);
                float y = GameRandom.Range(GameConfig.PLAY_MIN_Y, GameConfig.PLAY_MAX_Y + 1);

                int layer = GameRandom.Range(0, 4); // 0: 远景微尘, 1: 中景流星, 2: 前景亮星, 3: 高速穿梭流光
                float speed;
                char symbol;
                Color color;

                if (layer == 0)
                {
                    // 远景微尘：细密深邃、慢速漂移、柔和微蓝紫
                    speed = 2.0f + GameRandom.Range(0f, 2f);
                    symbol = '.';
                    color = (GameRandom.Range(0, 2) == 0)
                        ? new Color(0.28f, 0.42f, 0.65f, 0.50f)
                        : new Color(0.40f, 0.32f, 0.60f, 0.45f);
                }
                else if (layer == 1)
                {
                    // 中景恒星：青蓝与琥珀金星芒
                    speed = 6.0f + GameRandom.Range(0f, 3.5f);
                    symbol = '+';
                    int cType = GameRandom.Range(0, 3);
                    color = (cType == 0) ? new Color(0.25f, 0.85f, 1.0f, 0.75f)
                          : (cType == 1) ? new Color(1.0f, 0.85f, 0.45f, 0.70f)
                          : new Color(0.75f, 0.85f, 1.0f, 0.75f);
                }
                else if (layer == 2)
                {
                    // 近景亮星：明亮耀眼
                    speed = 12.0f + GameRandom.Range(0f, 4f);
                    symbol = '*';
                    color = (GameRandom.Range(0, 3) == 0) ? GameColors.BrightYellow : GameColors.BrightWhite;
                }
                else
                {
                    // 高速流光线段：提供高速冲刺的强烈纵深感
                    speed = 22.0f + GameRandom.Range(0f, 8f);
                    symbol = '|';
                    color = new Color(0.35f, 0.78f, 1.0f, 0.45f);
                }

                m_stars.Add(new StarData
                {
                    x = x,
                    y = y,
                    speed = speed,
                    symbol = symbol,
                    color = color
                });
            }
        }

        public IReadOnlyList<StarData> Stars => m_stars;
        public bool Warp { get => m_warp; set => m_warp = value; }

        public void Update(float dt)
        {
            float mult = m_warp ? 3.5f : 1.0f;
            for (int i = 0; i < m_stars.Count; i++)
            {
                var s = m_stars[i];
                s.y += s.speed * mult * dt;

                if (s.y > GameConfig.PLAY_MAX_Y + 1)
                {
                    s.y = GameConfig.PLAY_MIN_Y - 1;
                    s.x = GameRandom.Range(GameConfig.PLAY_MIN_X, GameConfig.PLAY_MAX_X + 1);
                }

                m_stars[i] = s;
            }
        }
    }
}
