using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【蜂群冲锋侦察机 · ScoutEnemy.cs】
    // 对应 C++ 原工程 ScoutEnemy：
    // 战术特点：多架集中快速俯冲，支持直线俯冲、正弦蛇形摆动、倾斜切入三种编队模式。
    // ============================================================================

    public class ScoutEnemy : Enemy
    {
        private readonly int   m_pattern; // 0: 直线冲锋, 1: 正弦波蛇形, 2: 目标切入
        private readonly float m_baseX;   // 蛇形摆动的基础中心 X 坐标

        public ScoutEnemy(Vector2 pos, int divePattern = 0)
            : base(pos, new Vector2(0.0f, 15.0f), 3, 2, 2, GameColors.BrightYellow,
                   EnemyType.Scout, 100)
        {
            m_pattern = divePattern;
            m_baseX = pos.x;

            m_shootInterval = 1.8f + GameRandom.Range(0, 10) * 0.1f;
            m_shootTimer = GameRandom.Range(0, 15) * 0.1f;

            m_modelLines.AddRange(new[]
            {
                " v ",
                "<#>"
            });
        }

        public int Pattern => m_pattern;

        public override void UpdateBehavior(float dt, Vector2 playerPos)
        {
            if (m_pattern == 1)
            {
                // 蛇形正弦扫荡
                m_pos.y += 14.0f * dt;
                m_pos.x = m_baseX + Mathf.Sin(m_age * 4.0f) * 8.0f;
            }
            else if (m_pattern == 2)
            {
                // 向玩家方向微倾斜切入
                float targetX = playerPos.x;
                float dir = (targetX > m_pos.x) ? 1.0f : -1.0f;
                m_pos.x += dir * 6.0f * dt;
                m_pos.y += 16.0f * dt;
            }
            else
            {
                // 直线下冲
                m_pos.y += 18.0f * dt;
            }

            if (m_pos.y > GameConfig.PLAY_MAX_Y + 1)
            {
                m_active = false;
            }
        }

        public override void Shoot(List<Bullet> bullets, Vector2 playerPos, float dt)
        {
            m_shootTimer += dt;
            if (m_shootTimer >= m_shootInterval)
            {
                m_shootTimer = 0.0f;
                bullets.Add(new Bullet(
                    new Vector2(m_pos.x + 1.0f, m_pos.y + 2.0f),
                    new Vector2(0.0f, 20.0f),
                    BulletOwner.Enemy,
                    BulletType.EnemyOrb,
                    12,
                    'v',
                    GameColors.BrightYellow
                ));
            }
        }
    }
}
