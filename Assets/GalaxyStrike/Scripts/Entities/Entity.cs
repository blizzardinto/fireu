using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【面向对象核心 · 实体抽象基类 Entity】
    // 对应 C++ 原工程 Entity.h / Entity.cpp：
    // 定义了所有游戏内活动对象（玩家、敌机、子弹、道具）的通用属性与抽象生命周期。
    // ============================================================================

    public abstract class Entity
    {
        protected Vector2 m_pos;       // 浮点坐标（亚像素平滑移动）
        protected Vector2 m_vel;       // 移动速度向量（网格/秒）
        protected int     m_width;     // 碰撞箱宽度（字符/格数）
        protected int     m_height;    // 碰撞箱高度（字符/行数）
        protected int     m_hp;        // 当前生命值
        protected int     m_maxHp;     // 生命值上限
        protected bool    m_active;    // 存活激活标记：false 表示待清理回收
        protected Color   m_color;     // 实体基础渲染配色

        protected Entity(Vector2 pos, Vector2 vel, int width, int height, int hp, Color color)
        {
            m_pos = pos;
            m_vel = vel;
            m_width = width;
            m_height = height;
            m_hp = hp;
            m_maxHp = hp;
            m_active = true;
            m_color = color;
        }

        // ------------------------------------------------------------------------
        // 【核心生命周期抽象接口】
        // ------------------------------------------------------------------------
        public abstract void Update(float dt);

        // ------------------------------------------------------------------------
        // 【伤害与碰撞检测】
        // ------------------------------------------------------------------------
        public virtual void TakeDamage(int dmg)
        {
            m_hp -= dmg;
            if (m_hp <= 0)
            {
                m_hp = 0;
                m_active = false; // 实体失去生命，等待游戏引擎回收清理
            }
        }

        public virtual Rect2D GetBounds()
        {
            return new Rect2D(m_pos.x, m_pos.y, m_width, m_height);
        }

        public virtual bool CollidesWith(Entity other)
        {
            if (other == null || !m_active || !other.m_active) return false;
            return GetBounds().Intersects(other.GetBounds());
        }

        // ------------------------------------------------------------------------
        // 【封装与访问器】
        // ------------------------------------------------------------------------
        public bool IsAlive => m_active && m_hp > 0;
        public bool IsActive => m_active;
        public void SetActive(bool active) => m_active = active;

        public Vector2 Pos { get => m_pos; set => m_pos = value; }
        public Vector2 Vel { get => m_vel; set => m_vel = value; }
        public int Width => m_width;
        public int Height => m_height;
        public int Hp { get => m_hp; set => m_hp = value; }
        public int MaxHp => m_maxHp;
        public Color Color { get => m_color; set => m_color = value; }
    }
}
