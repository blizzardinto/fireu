using System;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【弹道与弹药体系 · Bullet.cs】
    // 对应 C++ 原工程 Bullet.h / Bullet.cpp：
    // 支持穿透判定、多段贯通、智能追踪引导（向目标平滑转向角插值）与螺旋弹幕演算。
    // ============================================================================

    public class Bullet : Entity
    {
        private readonly BulletOwner m_owner;
        private readonly BulletType  m_type;
        private readonly int         m_damage;
        private readonly bool        m_piercing;
        private int                  m_penetrationsLeft;
        private float                m_lifetime;
        private char                 m_symbol;

        public Bullet(Vector2 pos, Vector2 vel, BulletOwner owner, BulletType type, int damage,
                      char symbol = '|', Color? color = null, bool piercing = false)
            : base(pos, vel, 1, 1, 1, color ?? GameColors.BrightYellow)
        {
            m_owner = owner;
            m_type = type;
            m_damage = damage;
            m_piercing = piercing;
            m_penetrationsLeft = piercing ? 3 : 1;
            m_lifetime = 0.0f;
            m_symbol = symbol;
        }

        public BulletOwner Owner => m_owner;
        public BulletType Type => m_type;
        public int Damage => m_damage;
        public bool IsPiercing => m_piercing;
        public char Symbol => m_symbol;
        public float Lifetime => m_lifetime;

        public override void Update(float dt)
        {
            m_lifetime += dt;
            m_pos += m_vel * dt;

            // 出界判定：飞出战斗空域外围即刻失活
            if (m_pos.x < GameConfig.PLAY_MIN_X - 1 || m_pos.x > GameConfig.PLAY_MAX_X + 1 ||
                m_pos.y < GameConfig.PLAY_MIN_Y - 1 || m_pos.y > GameConfig.PLAY_MAX_Y + 1)
            {
                m_active = false;
            }

            // Boss 螺旋弹幕微积分角速度演算：速度向量绕原点持续旋转
            if (m_type == BulletType.BossHelix)
            {
                float angleSpeed = 3.5f; // 旋转角速度 (rad/s)
                float currentAngle = Mathf.Atan2(m_vel.y, m_vel.x) + angleSpeed * dt;
                float speed = m_vel.magnitude;
                m_vel.x = Mathf.Cos(currentAngle) * speed;
                m_vel.y = Mathf.Sin(currentAngle) * speed;
            }
        }

        /// <summary>
        /// 自动追踪索敌微型飞弹算法（矢量角平滑插值）
        /// </summary>
        public void UpdateHoming(float dt, Vector2 targetPos)
        {
            if (m_type != BulletType.Homing || !m_active)
            {
                Update(dt);
                return;
            }

            m_lifetime += dt;

            // 1. 计算从子弹指向目标单位的期望向量
            Vector2 desired = (targetPos - m_pos).NormalizedSafe();
            float currentSpeed = m_vel.magnitude;
            if (currentSpeed < 10.0f) currentSpeed = 24.0f;

            // 2. 转向插值 (Turn Rate Interpolation)：模拟具有质量和惯性的物理导弹平滑拐弯
            float turnRate = 8.0f * dt;
            Vector2 currentDir = m_vel.NormalizedSafe();
            Vector2 newDir = (currentDir + desired * turnRate).NormalizedSafe();
            m_vel = newDir * currentSpeed;

            // 3. 亚像素位移叠加
            m_pos += m_vel * dt;

            // 4. 根据飞弹飞行朝向，动态改变 ASCII 符号形状
            if (Mathf.Abs(m_vel.x) > Mathf.Abs(m_vel.y) * 1.5f)
            {
                m_symbol = (m_vel.x > 0) ? '>' : '<';
            }
            else if (m_vel.y < -0.5f)
            {
                m_symbol = '^';
            }
            else
            {
                m_symbol = '*';
            }

            // 出界检测
            if (m_pos.x < GameConfig.PLAY_MIN_X - 1 || m_pos.x > GameConfig.PLAY_MAX_X + 1 ||
                m_pos.y < GameConfig.PLAY_MIN_Y - 1 || m_pos.y > GameConfig.PLAY_MAX_Y + 1)
            {
                m_active = false;
            }
        }

        /// <summary>
        /// 击中目标响应：穿透弹递减穿透次数，普通弹立即失活
        /// </summary>
        public void OnHitTarget()
        {
            if (!m_piercing)
            {
                m_active = false; // 普通子弹一触即爆
            }
            else
            {
                m_penetrationsLeft--; // 贯穿激光扣除一次穿透次数
                if (m_penetrationsLeft <= 0)
                {
                    m_active = false; // 贯穿次数耗尽后消失
                }
            }
        }
    }
}
