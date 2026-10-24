using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Entities
{
    // ============================================================================
    // 【玩家战机核心体系 · Player.cs】
    // 对应 C++ 原工程 Player.h / Player.cpp：
    // 封装 5 阶机体进化、5 阶武器火力、护盾脱战自充能、全屏 EMP 核弹与复活逻辑。
    // ============================================================================

    public class Player : Entity
    {
        private int   m_shipLevel;        // 战机机体形态等级 (Lv.1 ~ Lv.5)
        private int   m_shipExp;          // 当前战术经验值
        private int   m_shipExpNext;      // 进阶到下一阶机体所需经验值
        private int   m_bulletLevel;      // 武器弹幕火力等级 (Lv.1 ~ Lv.5)

        private int   m_shield;           // 能量护盾当前值
        private int   m_maxShield;        // 能量护盾最大值
        private float m_shieldRegenTimer; // 脱战自动充能计时器
        private int   m_lives;            // 剩余备用机体数（命数）
        private int   m_bombs;            // 剩余全屏 EMP 核弹数

        private float m_shootCooldown;    // 射击冷却间隔（秒）
        private float m_shootTimer;       // 射击冷却倒计时
        private float m_invulnTimer;      // 受创/复活无敌闪烁倒计时
        private float m_speed;            // 战机移动速度（网格/秒）

        // 丝滑飞行动力学属性
        private Vector2 m_smoothedVel;             // 平滑过渡速度（消除刚性跳步顿挫）
        private float   m_rollAngle;               // 空气动力学滚转角（向左/向右动态倾斜）
        private float   m_throttle;                // 推进器油门拉伸深度 (0.75 ~ 1.45)
        private bool    m_movedThisFrame;          // 本帧是否接收位移输入
        private float   m_shieldRegenAccumulator;  // 高刷新率帧率无关护盾充能累加器

        private List<string> m_modelLines = new List<string>(); // 当前机型 ASCII 字符阵列

        public Player(Vector2 pos)
            : base(pos, Vector2.zero, 3, 2, 100, GameColors.BrightCyan)
        {
            m_shipLevel = 1;
            m_shipExp = 0;
            m_shipExpNext = 50;
            m_bulletLevel = 1;
            m_shield = 50;
            m_maxShield = 50;
            m_shieldRegenTimer = 0.0f;
            m_shieldRegenAccumulator = 0.0f;
            m_lives = 3;
            m_bombs = 2;
            m_shootCooldown = 0.12f;
            m_shootTimer = 0.0f;
            m_invulnTimer = 2.0f;
            m_speed = 32.0f;
            m_smoothedVel = Vector2.zero;
            m_rollAngle = 0.0f;
            m_throttle = 1.0f;
            m_movedThisFrame = false;

            ApplyShipLevelStats();
        }

        public void ApplyShipLevelStats()
        {
            m_modelLines.Clear();

            switch (m_shipLevel)
            {
                case 1:
                    m_width = 3;
                    m_height = 2;
                    m_maxHp = 100;
                    m_maxShield = 50;
                    m_speed = 32.0f;
                    m_color = GameColors.BrightCyan;
                    m_modelLines.AddRange(new[]
                    {
                        " ^ ",
                        "/|\\"
                    });
                    break;

                case 2:
                    m_width = 5;
                    m_height = 2;
                    m_maxHp = 140;
                    m_maxShield = 80;
                    m_speed = 34.0f;
                    m_color = GameColors.BrightGreen;
                    m_modelLines.AddRange(new[]
                    {
                        " /^\\ ",
                        "<|#|>"
                    });
                    break;

                case 3:
                    m_width = 7;
                    m_height = 3;
                    m_maxHp = 190;
                    m_maxShield = 120;
                    m_speed = 35.0f;
                    m_color = GameColors.BrightYellow;
                    m_modelLines.AddRange(new[]
                    {
                        "  /^\\  ",
                        " /|#|\\ ",
                        "<=|O|=>"
                    });
                    break;

                case 4:
                    m_width = 9;
                    m_height = 3;
                    m_maxHp = 250;
                    m_maxShield = 170;
                    m_speed = 36.0f;
                    m_color = GameColors.BrightMagenta;
                    m_modelLines.AddRange(new[]
                    {
                        "  //^\\\\  ",
                        " /=||#||=\\ ",
                        "<<==[W]==>>"
                    });
                    break;

                case 5:
                default:
                    m_width = 11;
                    m_height = 4;
                    m_maxHp = 320;
                    m_maxShield = 240;
                    m_speed = 38.0f;
                    m_color = GameColors.BrightWhite;
                    m_modelLines.AddRange(new[]
                    {
                        "    /o\\    ",
                        "   //^\\\\   ",
                        "  /<=#=>\\  ",
                        "<<==[***]==>>"
                    });
                    break;
            }
        }

        public void Move(float dx, float dy, float dt)
        {
            m_movedThisFrame = true;
            Vector2 targetVel = new Vector2(dx * m_speed, dy * m_speed);

            // 超高响应性柔性阻尼过渡 (0.04s 迅速响应，杜绝操作迟滞；同时抹平单帧跳步顿挫)
            float accelRate = (targetVel.sqrMagnitude > 0.01f) ? 28.0f : 24.0f;
            m_smoothedVel = Vector2.Lerp(m_smoothedVel, targetVel, Mathf.Clamp01(accelRate * dt));

            m_pos.x += m_smoothedVel.x * dt;
            m_pos.y += m_smoothedVel.y * dt;

            // 限制在战斗区域范围内
            if (m_pos.x < GameConfig.PLAY_MIN_X)
            {
                m_pos.x = GameConfig.PLAY_MIN_X;
                m_smoothedVel.x = 0f;
            }
            if (m_pos.x + m_width > GameConfig.PLAY_MAX_X + 1)
            {
                m_pos.x = GameConfig.PLAY_MAX_X + 1 - m_width;
                m_smoothedVel.x = 0f;
            }
            if (m_pos.y < GameConfig.PLAY_MIN_Y + 5)
            {
                m_pos.y = GameConfig.PLAY_MIN_Y + 5;
                m_smoothedVel.y = 0f;
            }
            if (m_pos.y + m_height > GameConfig.PLAY_MAX_Y + 1)
            {
                m_pos.y = GameConfig.PLAY_MAX_Y + 1 - m_height;
                m_smoothedVel.y = 0f;
            }

            // 空气动力学滚转角：向左推杆机身向左侧倾，向右向右倾斜 (最大约 ±14 度)
            float targetRoll = -dx * 14.0f;
            m_rollAngle = Mathf.Lerp(m_rollAngle, targetRoll, Mathf.Clamp01(16.0f * dt));

            // 油门动力学：向上推杆前冲加速时尾焰暴涨，怠速或后退时柔和收拢
            float targetThrottle = (dy < -0.1f) ? 1.45f : (dy > 0.1f ? 0.75f : 1.0f);
            m_throttle = Mathf.Lerp(m_throttle, targetThrottle, Mathf.Clamp01(14.0f * dt));
        }

        public override void Update(float dt)
        {
            if (!m_movedThisFrame)
            {
                // 无输入移动时，机身平滑回正、速度与油门自然归中
                m_rollAngle = Mathf.Lerp(m_rollAngle, 0.0f, Mathf.Clamp01(12.0f * dt));
                m_throttle = Mathf.Lerp(m_throttle, 1.0f, Mathf.Clamp01(8.0f * dt));
                m_smoothedVel = Vector2.Lerp(m_smoothedVel, Vector2.zero, Mathf.Clamp01(18.0f * dt));
            }
            m_movedThisFrame = false;

            if (m_shootTimer > 0.0f)
            {
                m_shootTimer -= dt;
            }

            if (m_invulnTimer > 0.0f)
            {
                m_invulnTimer -= dt;
            }

            // 护盾脱战自动充能系统（脱战 3 秒后每秒恢复 15 点，基于浮点累加保证任意帧率/高刷平稳）
            m_shieldRegenTimer += dt;
            if (m_shieldRegenTimer > 3.0f && m_shield < m_maxShield)
            {
                m_shieldRegenAccumulator += 15.0f * dt;
                int add = Mathf.FloorToInt(m_shieldRegenAccumulator);
                if (add > 0)
                {
                    m_shield += add;
                    m_shieldRegenAccumulator -= add;
                    if (m_shield > m_maxShield) m_shield = m_maxShield;
                }
            }
        }

        public void Fire(List<Bullet> bullets, Vector2? nearestEnemyPos, Action<SfxType> playSfx = null)
        {
            if (m_shootTimer > 0.0f) return;
            m_shootTimer = m_shootCooldown;

            playSfx?.Invoke(SfxType.Shoot);

            float midX = m_pos.x + m_width * 0.5f;
            float noseY = m_pos.y;

            switch (m_bulletLevel)
            {
                case 1:
                {
                    // 单发高能等离子炮
                    bullets.Add(new Bullet(
                        new Vector2(midX, noseY - 1.0f),
                        new Vector2(0.0f, -48.0f),
                        BulletOwner.Player,
                        BulletType.Normal,
                        15,
                        '|',
                        GameColors.BrightYellow
                    ));
                    break;
                }

                case 2:
                {
                    // 双联装平行极速光束
                    bullets.Add(new Bullet(
                        new Vector2(midX - 1.5f, noseY),
                        new Vector2(0.0f, -50.0f),
                        BulletOwner.Player,
                        BulletType.Twin,
                        16,
                        '|',
                        GameColors.BrightCyan
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 1.5f, noseY),
                        new Vector2(0.0f, -50.0f),
                        BulletOwner.Player,
                        BulletType.Twin,
                        16,
                        '|',
                        GameColors.BrightCyan
                    ));
                    break;
                }

                case 3:
                {
                    // 三向散射破甲炮
                    bullets.Add(new Bullet(
                        new Vector2(midX, noseY - 1.0f),
                        new Vector2(0.0f, -50.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        18,
                        '|',
                        GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX - 2.0f, noseY),
                        new Vector2(-14.0f, -46.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        16,
                        '/',
                        GameColors.BrightCyan
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 2.0f, noseY),
                        new Vector2(14.0f, -46.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        16,
                        '\\',
                        GameColors.BrightCyan
                    ));
                    break;
                }

                case 4:
                {
                    // 五向散射 + 中心高能穿透激光！
                    bullets.Add(new Bullet(
                        new Vector2(midX, noseY - 1.0f),
                        new Vector2(0.0f, -54.0f),
                        BulletOwner.Player,
                        BulletType.Laser,
                        30,
                        '!',
                        GameColors.BrightMagenta,
                        true // 穿透多机
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX - 1.5f, noseY),
                        new Vector2(-9.0f, -48.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        18,
                        '|',
                        GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 1.5f, noseY),
                        new Vector2(9.0f, -48.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        18,
                        '|',
                        GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX - 3.0f, noseY + 1.0f),
                        new Vector2(-20.0f, -44.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        16,
                        '/',
                        GameColors.BrightCyan
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 3.0f, noseY + 1.0f),
                        new Vector2(20.0f, -44.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        16,
                        '\\',
                        GameColors.BrightCyan
                    ));
                    break;
                }

                case 5:
                default:
                {
                    // 全弹超载齐射：中心重型穿透光束 + 4向广角光束 + 2枚自动索敌微型飞弹！
                    bullets.Add(new Bullet(
                        new Vector2(midX, noseY - 1.0f),
                        new Vector2(0.0f, -56.0f),
                        BulletOwner.Player,
                        BulletType.Laser,
                        38,
                        '|',
                        GameColors.BrightWhite,
                        true // 穿透
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX - 2.0f, noseY),
                        new Vector2(-10.0f, -50.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        20,
                        '|',
                        GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 2.0f, noseY),
                        new Vector2(10.0f, -50.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        20,
                        '|',
                        GameColors.BrightYellow
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX - 4.0f, noseY + 1.0f),
                        new Vector2(-22.0f, -45.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        18,
                        '/',
                        GameColors.BrightCyan
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 4.0f, noseY + 1.0f),
                        new Vector2(22.0f, -45.0f),
                        BulletOwner.Player,
                        BulletType.Spread,
                        18,
                        '\\',
                        GameColors.BrightCyan
                    ));

                    // 双发自动追踪微型导弹
                    bullets.Add(new Bullet(
                        new Vector2(midX - 3.5f, noseY + 1.5f),
                        new Vector2(-12.0f, -25.0f),
                        BulletOwner.Player,
                        BulletType.Homing,
                        24,
                        '*',
                        GameColors.BrightRed
                    ));
                    bullets.Add(new Bullet(
                        new Vector2(midX + 3.5f, noseY + 1.5f),
                        new Vector2(12.0f, -25.0f),
                        BulletOwner.Player,
                        BulletType.Homing,
                        24,
                        '*',
                        GameColors.BrightRed
                    ));
                    break;
                }
            }
        }

        public bool TriggerBomb(List<Bullet> enemyBullets, Action<Vector2, float> addShockwave, Action<Vector2, int, Color> addExplosion, Action<SfxType> playSfx)
        {
            if (m_bombs <= 0) return false;
            m_bombs--;

            playSfx?.Invoke(SfxType.Bomb);

            // 清除全屏敌方子弹
            enemyBullets.Clear();

            // 产生全屏高爆烈焰震荡波环与核爆火花
            Vector2 center = new Vector2(m_pos.x + m_width * 0.5f, m_pos.y + m_height * 0.5f);
            addShockwave?.Invoke(center, 55.0f);
            addExplosion?.Invoke(center, 50, GameColors.BrightRed);

            // 短暂进入保护状态
            m_invulnTimer = 2.0f;
            return true;
        }

        public override void TakeDamage(int dmg)
        {
            if (m_invulnTimer > 0.0f) return;

            m_shieldRegenTimer = 0.0f; // 重置脱战回盾

            // 1. 能量护盾优先吸收伤害
            if (m_shield > 0)
            {
                if (m_shield >= dmg)
                {
                    m_shield -= dmg;
                    dmg = 0;
                }
                else
                {
                    dmg -= m_shield;
                    m_shield = 0;
                }
            }

            // 2. 护盾被打穿，剩余伤害扣减装甲 HP
            if (dmg > 0)
            {
                m_hp -= dmg;

                if (m_hp <= 0)
                {
                    m_hp = 0;
                    m_lives--;
                    if (m_lives > 0)
                    {
                        Respawn(new Vector2(50.0f, 36.0f));
                    }
                    else
                    {
                        m_active = false; // 命数耗尽，游戏结束
                    }
                }
                else
                {
                    m_invulnTimer = 1.8f; // 受创无敌帧
                }
            }
        }

        public void TakeDamageWithShield(int dmg, Action<Vector2, Color> addHitSpark, Action<Vector2, int, Color> addExplosion, Action<SfxType> playSfx)
        {
            if (m_invulnTimer > 0.0f) return;

            Vector2 center = new Vector2(m_pos.x + m_width * 0.5f, m_pos.y + m_height * 0.5f);
            int prevShield = m_shield;

            TakeDamage(dmg);

            playSfx?.Invoke(SfxType.PlayerHurt);

            if (prevShield > 0)
            {
                addHitSpark?.Invoke(center, GameColors.BrightCyan);
            }
            if (m_hp > 0 && prevShield < dmg)
            {
                addHitSpark?.Invoke(center, GameColors.BrightYellow);
            }
            if (m_hp <= 0)
            {
                addExplosion?.Invoke(center, 12, GameColors.BrightRed);
            }
        }

        public void Respawn(Vector2 pos)
        {
            m_pos = pos;
            m_hp = m_maxHp;
            m_shield = m_maxShield;
            m_invulnTimer = 3.0f; // 重生 3 秒无敌保护
            m_active = true;
        }

        public void ClearInvulnerability()
        {
            m_invulnTimer = 0.0f;
        }

        public bool UpgradeBullet(Action<SfxType> playSfx = null)
        {
            if (m_bulletLevel < GameConfig.MAX_BULLET_LEVEL)
            {
                m_bulletLevel++;
                playSfx?.Invoke(SfxType.PowerUp);
                return true;
            }
            return false;
        }

        public bool AddShipExp(int amount, Action<SfxType> playSfx = null)
        {
            m_shipExp += amount;
            if (m_shipLevel < GameConfig.MAX_SHIP_LEVEL && m_shipExp >= m_shipExpNext)
            {
                m_shipExp -= m_shipExpNext;
                m_shipLevel++;
                m_shipExpNext = m_shipExpNext * 2 + 30;

                ApplyShipLevelStats();
                m_hp = m_maxHp;
                m_shield = m_maxShield;
                playSfx?.Invoke(SfxType.ShipUpgrade);
                return true; // 触发升级
            }
            return false;
        }

        public void RestoreShield(int amount, Action<SfxType> playSfx = null)
        {
            m_shield = Mathf.Min(m_maxShield, m_shield + amount);
            playSfx?.Invoke(SfxType.PowerUp);
        }

        public void AddBomb(int count = 1, Action<SfxType> playSfx = null)
        {
            m_bombs = Mathf.Min(GameConfig.MAX_BOMBS, m_bombs + count);
            playSfx?.Invoke(SfxType.PowerUp);
        }

        public void AddLife(int count = 1, Action<SfxType> playSfx = null)
        {
            m_lives = Mathf.Min(GameConfig.MAX_PLAYER_LIVES, m_lives + count);
            playSfx?.Invoke(SfxType.PowerUp);
        }

        public string GetShipClassName()
        {
            switch (m_shipLevel)
            {
                case 1: return "FALCON-I [SCOUT]";
                case 2: return "VIPER-II [ASSAULT]";
                case 3: return "PHOENIX-III [CRUISER]";
                case 4: return "THUNDER-IV [BATTLE]";
                case 5: return "HYPERION-V [FLAGSHIP]";
                default: return "UNKNOWN";
            }
        }

        public string GetWeaponModeName()
        {
            switch (m_bulletLevel)
            {
                case 1: return "LV.1 SINGLE PLASMA";
                case 2: return "LV.2 TWIN BLASTER";
                case 3: return "LV.3 TRIPLE SPREAD";
                case 4: return "LV.4 PENTA-SPREAD & LASER";
                case 5: return "LV.5 OMEGA OVERKILL SALVO";
                default: return "STANDARD";
            }
        }

        // ------------------------------------------------------------------------
        // 【只读访问器】
        // ------------------------------------------------------------------------
        public int ShipLevel => m_shipLevel;
        public int BulletLevel => m_bulletLevel;
        public int ShipExp => m_shipExp;
        public int ShipExpNext => m_shipExpNext;
        public int Shield => m_shield;
        public int MaxShield => m_maxShield;
        public int Lives => m_lives;
        public int Bombs => m_bombs;
        public float InvulnTimer => m_invulnTimer;
        public bool IsInvulnerable => m_invulnTimer > 0.0f;
        public float Speed => m_speed;
        public float RollAngle => m_rollAngle;
        public float Throttle => m_throttle;
        public Vector2 SmoothedVel => m_smoothedVel;
        public IReadOnlyList<string> ModelLines => m_modelLines;
    }
}
