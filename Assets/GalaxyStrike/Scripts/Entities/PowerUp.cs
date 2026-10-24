using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【战术掉落物资 · PowerUp.cs】
    // 对应 C++ 原工程 PowerUp.h / PowerUp.cpp：
    // 支持 5 种物资类型、失重正弦波飘落、以及 16 码近距磁力吸附机制。
    // ============================================================================

    public class PowerUp : Entity
    {
        private readonly PowerUpType m_type;
        private float                m_age;
        private float                m_baseX;
        private string               m_label;

        public PowerUp(Vector2 pos, PowerUpType type)
            : base(pos, new Vector2(0.0f, 5.0f), 3, 1, 1, GameColors.White)
        {
            m_type = type;
            m_age = 0.0f;
            m_baseX = pos.x;
            m_label = "[?]";

            switch (m_type)
            {
                case PowerUpType.Firepower:
                    m_label = "[P]";
                    m_color = GameColors.BrightYellow;
                    break;
                case PowerUpType.ShipExp:
                    m_label = "[U]";
                    m_color = GameColors.BrightCyan;
                    break;
                case PowerUpType.Shield:
                    m_label = "[S]";
                    m_color = GameColors.BrightGreen;
                    break;
                case PowerUpType.Bomb:
                    m_label = "[B]";
                    m_color = GameColors.BrightRed;
                    break;
                case PowerUpType.Score:
                    m_label = "[$]";
                    m_color = GameColors.BrightWhite;
                    break;
            }
        }

        public PowerUpType Type => m_type;
        public string Label => m_label;
        public float Age => m_age;

        public override void Update(float dt)
        {
            m_age += dt;
            m_pos.y += m_vel.y * dt;
            m_pos.x = m_baseX + Mathf.Sin(m_age * 2.5f) * 2.0f;

            if (m_pos.y > GameConfig.PLAY_MAX_Y + 1)
            {
                m_active = false;
            }
        }

        /// <summary>
        /// 结合玩家战机距离的增强版更新（带 16 码近距磁力吸附机制）
        /// </summary>
        public void UpdateWithPlayer(float dt, Vector2 playerPos)
        {
            if (!m_active) return;

            float dist = m_pos.DistanceTo(playerPos);

            // 磁吸判定半径 16.0 字符：靠近玩家时自动施加向心加速度
            if (dist < 16.0f)
            {
                m_age += dt;
                Vector2 pullDir = (playerPos - m_pos).NormalizedSafe();
                float magnetSpeed = 22.0f;
                m_pos += pullDir * magnetSpeed * dt;
                m_baseX = m_pos.x; // 同步基准点，防止脱离磁吸后瞬移

                if (m_pos.y > GameConfig.PLAY_MAX_Y + 1)
                {
                    m_active = false;
                }
            }
            else
            {
                Update(dt);
            }
        }
    }
}
