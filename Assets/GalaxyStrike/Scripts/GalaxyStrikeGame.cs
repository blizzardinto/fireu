using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;
using GalaxyStrike.Entities;
using GalaxyStrike.Systems;
using GalaxyStrike.Audio;
using GalaxyStrike.Presentation;
using GalaxyStrike.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GalaxyStrike
{
    // ============================================================================
    // 【游戏主控中枢 · GalaxyStrikeGame.cs】
    // 对应 C++ 原工程 Game.h / Game.cpp：
    // 作为中枢调度器，组合持有战机、关卡、星空、粒子、渲染、音效、HUD 等各大子系统，
    // 运行标准的 60 FPS 生命周期：处理输入 -> 物理演算 -> 碰撞检测 -> 画面渲染。
    // ============================================================================

    [RequireComponent(typeof(GalaxyStrikeHUD))]
    [RequireComponent(typeof(GalaxyStrikeRenderer))]
    public class GalaxyStrikeGame : MonoBehaviour
    {
        private GameState             m_state;
        private Player                m_player;
        private WaveManager           m_waveMgr;
        private StarfieldManager      m_starfield;
        private ParticleSystemManager m_particles;

        private ProceduralSoundManager m_soundMgr;
        private GalaxyStrikeRenderer   m_renderer;
        private GalaxyStrikeHUD        m_hud;

        private readonly List<Bullet>  m_playerBullets = new List<Bullet>(64);
        private readonly List<Bullet>  m_enemyBullets  = new List<Bullet>(128);
        private readonly List<Enemy>   m_enemies       = new List<Enemy>(32);
        private readonly List<PowerUp> m_powerUps      = new List<PowerUp>(16);

        private int  m_score;
        private int  m_highScore;
        private int  m_kills;
        private bool m_soundOn;

        private readonly HashSet<KeyCode> m_guiPressedKeys = new HashSet<KeyCode>();
        private KeyCode m_lastGuiKeyDown = KeyCode.None;
        private bool m_guiClicked = false;

        private Camera m_gameCamera;

        private void Awake()
        {
            SetupCamera();

            m_score = 0;
            m_highScore = PlayerPrefs.GetInt("GalaxyStrike_HighScore", 10000);
            m_kills = 0;
            m_soundOn = true;

            m_player = new Player(new Vector2(50.0f, 36.0f));
            m_waveMgr = new WaveManager();
            m_starfield = new StarfieldManager(140);
            m_particles = new ParticleSystemManager();

            m_renderer = GetComponent<GalaxyStrikeRenderer>();
            m_renderer.Initialize();

            m_hud = GetComponent<GalaxyStrikeHUD>();

            // 动态挂载程序化音频组件
            var soundGo = new GameObject("ProceduralSoundManager");
            soundGo.transform.SetParent(transform, false);
            m_soundMgr = soundGo.AddComponent<ProceduralSoundManager>();

            // 启动时呈现全新超清科幻主开始菜单
            m_state = GameState.Title;
        }

        private void Start()
        {
            // 自适应高刷新率显示器 (120Hz / 144Hz / 240Hz)，开启垂直同步确保极致丝滑
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }

        private void SetupCamera()
        {
            m_gameCamera = Camera.main;
            if (m_gameCamera == null)
            {
                var camGo = new GameObject("GalaxyStrike_Camera");
                m_gameCamera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            m_gameCamera.orthographic = true;
            m_gameCamera.orthographicSize = 5.2f;
            m_gameCamera.backgroundColor = GameColors.Black;
            m_gameCamera.clearFlags = CameraClearFlags.SolidColor;

            if (m_gameCamera.GetComponent<AudioListener>() == null && FindAnyObjectByType<AudioListener>() == null)
            {
                m_gameCamera.gameObject.AddComponent<AudioListener>();
            }

            // 调整相机 X 偏移，使战场完美居中于左侧 75% 战斗空域（留出右侧 25% 放置全息 HUD）
            m_cameraOrigin = new Vector3(3.2f, 0.0f, -10.0f);
            m_gameCamera.transform.position = m_cameraOrigin;
        }

        private Vector3 m_cameraOrigin = new Vector3(3.2f, 0.0f, -10.0f);
        private float   m_cameraShakeTimer = 0.0f;
        private float   m_cameraShakeDuration = 0.0f;
        private float   m_cameraShakeIntensity = 0.0f;

        public void TriggerCameraShake(float duration, float intensity)
        {
            m_cameraShakeDuration = Mathf.Max(0.001f, duration);
            m_cameraShakeTimer = duration;
            m_cameraShakeIntensity = Mathf.Max(m_cameraShakeIntensity, intensity);
        }

        private void UpdateCameraShake(float dt)
        {
            if (m_cameraShakeTimer > 0f && m_gameCamera != null)
            {
                m_cameraShakeTimer -= dt;
                float factor = Mathf.Clamp01(m_cameraShakeTimer / m_cameraShakeDuration);
                float amp = m_cameraShakeIntensity * factor;
                float offsetX = (Mathf.PerlinNoise(Time.time * 45f, 0f) - 0.5f) * 2f * amp;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 45f) - 0.5f) * 2f * amp;
                m_gameCamera.transform.position = m_cameraOrigin + new Vector3(offsetX, offsetY, 0f);

                if (m_cameraShakeTimer <= 0f)
                {
                    m_cameraShakeIntensity = 0f;
                    m_gameCamera.transform.position = m_cameraOrigin;
                }
            }
        }

        public void ResetGame()
        {
            m_player = new Player(new Vector2(50.0f, 36.0f));
            m_waveMgr.Reset();
            m_playerBullets.Clear();
            m_enemyBullets.Clear();
            m_enemies.Clear();
            m_powerUps.Clear();
            m_particles.Clear();
            m_score = 0;
            m_kills = 0;

            m_soundMgr.StartMusic();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            // 限制单帧最大步长以防卡顿漂移
            if (dt > 0.05f) dt = 0.05f;

            UpdateCameraShake(dt);
            ProcessInput(dt);
            UpdatePhysics(dt);

            m_lastGuiKeyDown = KeyCode.None;
        }

        private void LateUpdate()
        {
            // 将纯逻辑数据投射到 Unity 渲染系统
            m_renderer.RenderFrame(
                m_player,
                m_enemies,
                m_playerBullets,
                m_enemyBullets,
                m_powerUps,
                m_particles,
                m_starfield
            );
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e != null)
            {
                if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                {
                    m_guiPressedKeys.Add(e.keyCode);
                    m_lastGuiKeyDown = e.keyCode;
                }
                else if (e.type == EventType.KeyUp && e.keyCode != KeyCode.None)
                {
                    m_guiPressedKeys.Remove(e.keyCode);
                    if (m_lastGuiKeyDown == e.keyCode) m_lastGuiKeyDown = KeyCode.None;
                }
                else if (e.type == EventType.MouseDown)
                {
                    m_guiClicked = true;
                }
            }

            m_hud.Draw(m_player, m_waveMgr, m_score, m_highScore, m_kills, m_state);
        }

        // ------------------------------------------------------------------------
        // 【输入处理系统 (兼容 Unity New Input System, Legacy Input 与 GUI 原始事件)】
        // ------------------------------------------------------------------------
        private void ProcessInput(float dt)
        {
            bool isEnterPressed = IsKeyPressedThisFrame(KeyCode.Return, KeyCode.KeypadEnter);
            bool isSpacePressed = IsKeyPressedThisFrame(KeyCode.Space);
            bool isQPressed     = IsKeyPressedThisFrame(KeyCode.Q);
            bool isPPressed     = IsKeyPressedThisFrame(KeyCode.P);
            bool isEscPressed   = IsKeyPressedThisFrame(KeyCode.Escape);
            bool isMPressed     = IsKeyPressedThisFrame(KeyCode.M);
            bool isYPressed     = IsKeyPressedThisFrame(KeyCode.Y);
            bool isNPressed     = IsKeyPressedThisFrame(KeyCode.N);
            bool isBPressed     = IsKeyPressedThisFrame(KeyCode.B, KeyCode.K);
            bool isMouseClicked = m_guiClicked;
            m_guiClicked = false;

            // 1. 标题画面输入：点击主界面“立即出击”按钮、按任意动作键、移动键或点击屏幕直接出战
            if (m_state == GameState.Title)
            {
                bool anyStart = (m_hud != null && m_hud.StartGameRequested) ||
                                isEnterPressed || isSpacePressed || isMouseClicked ||
                                IsKeyDown(KeyCode.W) || IsKeyDown(KeyCode.S) ||
                                IsKeyDown(KeyCode.A) || IsKeyDown(KeyCode.D) ||
                                IsKeyDown(KeyCode.UpArrow) || IsKeyDown(KeyCode.DownArrow) ||
                                IsKeyDown(KeyCode.LeftArrow) || IsKeyDown(KeyCode.RightArrow) ||
                                IsKeyDown(KeyCode.J) || IsKeyDown(KeyCode.Space);
                if (anyStart)
                {
                    if (m_hud != null) m_hud.StartGameRequested = false;
                    ResetGame();
                    m_state = GameState.Playing;
                }
                else if (isQPressed || isEscPressed)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
                return;
            }

            // 2. 暂停状态输入：点击继续/返回按钮或按键
            if (m_state == GameState.Paused)
            {
                if ((m_hud != null && m_hud.ResumeGameRequested) || isPPressed || isEscPressed || isSpacePressed || isEnterPressed || isMouseClicked)
                {
                    if (m_hud != null) m_hud.ResumeGameRequested = false;
                    m_state = GameState.Playing;
                }
                else if ((m_hud != null && m_hud.TitleGameRequested) || isQPressed)
                {
                    if (m_hud != null) m_hud.TitleGameRequested = false;
                    m_state = GameState.Title;
                    m_soundMgr.StopMusic();
                }
                return;
            }

            // 3. 结算状态输入 (GameOver / Victory)：点击重开/返回按钮或按键
            if (m_state == GameState.GameOver || m_state == GameState.Victory)
            {
                if ((m_hud != null && m_hud.RestartGameRequested) || isYPressed || isEnterPressed || isSpacePressed || isMouseClicked)
                {
                    if (m_hud != null) m_hud.RestartGameRequested = false;
                    ResetGame();
                    m_state = GameState.Playing;
                }
                else if ((m_hud != null && m_hud.TitleGameRequested) || isNPressed || isQPressed || isEscPressed)
                {
                    if (m_hud != null) m_hud.TitleGameRequested = false;
                    m_state = GameState.Title;
                    m_soundMgr.StopMusic();
                }
                return;
            }

            // 4. 空战战斗中按键响应
            if (m_state == GameState.Playing)
            {
                if (isPPressed || isEscPressed)
                {
                    m_state = GameState.Paused;
                    return;
                }
                if (isQPressed)
                {
                    m_state = GameState.Title;
                    m_soundMgr.StopMusic();
                    return;
                }
                if (isMPressed)
                {
                    m_soundOn = !m_soundOn;
                    m_soundMgr.SoundEnabled = m_soundOn;
                }
                if (isBPressed || IsMouseRightClicked())
                {
                    // 释放全屏高爆核弹
                    if (m_player.TriggerBomb(m_enemyBullets,
                        (center, r) => m_particles.AddShockwave(center, r),
                        (pos, cnt, col) => m_particles.AddExplosion(pos, cnt, col),
                        sfx => m_soundMgr.PlaySfx(sfx)))
                    {
                        Vector2 bombCenter = new Vector2(m_player.Pos.x + m_player.Width * 0.5f, m_player.Pos.y + m_player.Height * 0.5f);
                        // 全屏高爆核弹特效：快速双层清屏光环 + 辐射能量分叉 + 清脆屏幕震颤与极速闪光
                        m_particles.AddBombDetonation(bombCenter);
                        TriggerCameraShake(0.40f, 0.22f);
                        if (m_hud != null) m_hud.TriggerScreenFlash(new Color(1f, 0.28f, 0.1f, 0.28f), 0.16f);

                        // 对所有敌机造成 220 点毁灭性高伤并迅速清屏
                        for (int i = 0; i < m_enemies.Count; i++)
                        {
                            var e = m_enemies[i];
                            if (!e.IsAlive) continue;
                            e.TakeDamage(220);
                            if (!e.IsAlive)
                            {
                                // 敌机被核弹摧毁：轻快爆闪与稀疏星芒，迅速清空视野不拖泥带水
                                m_soundMgr.PlaySfx(SfxType.Explosion);
                                m_particles.AddExplosion(e.Pos, e.IsBoss ? 14 : 7, GameColors.BrightRed);
                                m_score += e.ScoreValue;
                                if (m_score > m_highScore)
                                {
                                    m_highScore = m_score;
                                    PlayerPrefs.SetInt("GalaxyStrike_HighScore", m_highScore);
                                }
                                m_kills++;
                                int expGain = e.IsBoss ? 100 : (e.ScoreValue / 25 + 2);
                                m_player.AddShipExp(expGain, sfx => m_soundMgr.PlaySfx(sfx));
                                var loot = e.TryDropLoot();
                                if (loot != null) m_powerUps.Add(loot);

                                if (e.IsBoss)
                                {
                                    m_state = GameState.Victory;
                                    m_soundMgr.StopMusic();
                                    m_soundMgr.PlaySfx(SfxType.Start);
                                }
                            }
                            else
                            {
                                // 存活 Boss 受到核弹重创：剧烈受创火花与爆鸣
                                m_particles.AddExplosion(e.Pos, 35, GameColors.BrightRed);
                                m_soundMgr.PlaySfx(SfxType.EnemyHit);
                            }
                        }
                    }
                }

                // 八向平滑摇杆/键盘控制
                float dx = 0.0f;
                float dy = 0.0f;

                // 在内部网格中：向上是 dy < 0 (Y减小)，向下是 dy > 0 (Y增大)
                if (IsKeyDown(KeyCode.W) || IsKeyDown(KeyCode.UpArrow))    dy -= 1.0f;
                if (IsKeyDown(KeyCode.S) || IsKeyDown(KeyCode.DownArrow))  dy += 1.0f;
                if (IsKeyDown(KeyCode.A) || IsKeyDown(KeyCode.LeftArrow))  dx -= 1.0f;
                if (IsKeyDown(KeyCode.D) || IsKeyDown(KeyCode.RightArrow)) dx += 1.0f;

                // 兼容 Unity 标准轴输入 (手柄、摇杆、虚拟摇杆)
                try
                {
                    float hAxis = Input.GetAxisRaw("Horizontal");
                    float vAxis = Input.GetAxisRaw("Vertical");
                    if (hAxis != 0f && dx == 0f) dx = Mathf.Sign(hAxis);
                    if (vAxis != 0f && dy == 0f) dy = -Mathf.Sign(vAxis);
                }
                catch {}

                // 斜向移动速度归一化
                if (dx != 0.0f && dy != 0.0f)
                {
                    dx *= 0.7071f;
                    dy *= 0.7071f;
                }

                m_player.Move(dx, dy, dt);

                // 持续连发射击 (按住 Space / J，或者按住鼠标左键)
                bool isFire = IsKeyDown(KeyCode.Space) || IsKeyDown(KeyCode.J) || IsMouseLeftHeld();
                if (isFire)
                {
                    // 寻找最近存活敌机以供追踪导弹精准导引
                    Vector2 closestPos = Vector2.zero;
                    bool found = false;
                    float minDist = 99999.0f;
                    Vector2 pCenter = m_player.Pos;

                    for (int i = 0; i < m_enemies.Count; i++)
                    {
                        var e = m_enemies[i];
                        if (e.IsAlive)
                        {
                            float dist = pCenter.DistanceTo(e.Pos);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestPos = e.Pos;
                                found = true;
                            }
                        }
                    }

                    m_player.Fire(m_playerBullets, found ? (Vector2?)closestPos : null, sfx => m_soundMgr.PlaySfx(sfx));
                }
            }
        }

        // ------------------------------------------------------------------------
        // 【物理更新流水线】
        // ------------------------------------------------------------------------
        private void UpdatePhysics(float dt)
        {
            if (m_state != GameState.Playing) return;

            // 1. 星空背景流动
            m_starfield.Warp = m_waveMgr.IsBossActive;
            m_starfield.Update(dt);

            // 2. 玩家战机逻辑
            m_player.Update(dt);

            // 3. 粒子特效
            m_particles.Update(dt);

            // 4. 战术波次生成系统
            m_waveMgr.Update(dt, m_enemies, sfx => m_soundMgr.PlaySfx(sfx));

            // 5. 查找最近敌人用于微型飞弹弹道追踪
            Vector2 playerCenter = m_player.Pos;
            Vector2 closestEnemyPos = new Vector2(50.0f, 0.0f);
            bool hasTarget = false;
            float closestDist = 99999.0f;

            for (int i = 0; i < m_enemies.Count; i++)
            {
                var e = m_enemies[i];
                if (e.IsAlive)
                {
                    float d = playerCenter.DistanceTo(e.Pos);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestEnemyPos = e.Pos;
                        hasTarget = true;
                    }
                }
            }

            // 6. 玩家子弹更新
            for (int i = 0; i < m_playerBullets.Count; i++)
            {
                var b = m_playerBullets[i];
                if (b.Type == BulletType.Homing && hasTarget)
                {
                    b.UpdateHoming(dt, closestEnemyPos);
                }
                else
                {
                    b.Update(dt);
                }
            }

            // 7. 敌方子弹更新
            for (int i = 0; i < m_enemyBullets.Count; i++)
            {
                m_enemyBullets[i].Update(dt);
            }

            // 8. 敌机多态逻辑与开火
            for (int i = 0; i < m_enemies.Count; i++)
            {
                var e = m_enemies[i];
                e.UpdateWithTarget(dt, m_player.Pos);
                e.Shoot(m_enemyBullets, m_player.Pos, dt);
            }

            // 9. 掉落物资更新（磁吸判定）
            for (int i = 0; i < m_powerUps.Count; i++)
            {
                m_powerUps[i].UpdateWithPlayer(dt, m_player.Pos);
            }

            // 10. 碰撞检测矩阵
            CheckCollisions();

            // 11. 清除失活实体 (垃圾回收)
            m_playerBullets.RemoveAll(b => !b.IsActive);
            m_enemyBullets.RemoveAll(b => !b.IsActive);
            m_enemies.RemoveAll(e => !e.IsActive);
            m_powerUps.RemoveAll(p => !p.IsActive);

            // 12. 检查玩家存活状态
            if (m_player.Lives <= 0 && !m_player.IsAlive)
            {
                m_state = GameState.GameOver;
                m_soundMgr.StopMusic();
                m_soundMgr.PlaySfx(SfxType.GameOver, true);
                m_particles.AddMegaExplosion(m_player.Pos, 2.6f, false);
                TriggerCameraShake(0.6f, 0.35f);
                if (m_hud != null) m_hud.TriggerScreenFlash(new Color(1f, 0.15f, 0.1f, 0.5f), 0.4f);
            }
        }

        // ------------------------------------------------------------------------
        // 【统一碰撞检测矩阵】
        // ------------------------------------------------------------------------
        private void CheckCollisions()
        {
            // 1. 玩家子弹 vs 敌机群
            for (int bi = 0; bi < m_playerBullets.Count; bi++)
            {
                var b = m_playerBullets[bi];
                if (!b.IsActive) continue;

                for (int ei = 0; ei < m_enemies.Count; ei++)
                {
                    var e = m_enemies[ei];
                    if (!e.IsAlive) continue;

                    if (b.CollidesWith(e))
                    {
                        e.TakeDamage(b.Damage);
                        b.OnHitTarget();

                        m_particles.AddHitSpark(b.Pos, GameColors.BrightYellow);
                        m_soundMgr.PlaySfx(SfxType.EnemyHit);

                        if (!e.IsAlive)
                        {
                            // 击毁敌机：根据敌机吨位产生层级爆破与震屏
                            m_soundMgr.PlaySfx(SfxType.Explosion);
                            if (e.IsBoss)
                            {
                                m_particles.AddMegaExplosion(e.Pos, 3.6f, false);
                                TriggerCameraShake(0.75f, 0.36f);
                                if (m_hud != null) m_hud.TriggerScreenFlash(new Color(1f, 0.85f, 0.3f, 0.4f), 0.35f);
                            }
                            else if (e.Type == EnemyType.Elite)
                            {
                                m_particles.AddMegaExplosion(e.Pos, 2.3f, false);
                                TriggerCameraShake(0.35f, 0.18f);
                            }
                            else if (e.Type == EnemyType.Bomber)
                            {
                                m_particles.AddExplosion(e.Pos, 26, GameColors.BrightRed);
                                TriggerCameraShake(0.2f, 0.12f);
                            }
                            else
                            {
                                m_particles.AddExplosion(e.Pos, 16, GameColors.BrightRed);
                            }

                            m_score += e.ScoreValue;
                            if (m_score > m_highScore)
                            {
                                m_highScore = m_score;
                                PlayerPrefs.SetInt("GalaxyStrike_HighScore", m_highScore);
                            }

                            m_kills++;

                            // 战机经验增长
                            int expGain = e.IsBoss ? 100 : (e.ScoreValue / 25 + 2);
                            m_player.AddShipExp(expGain, sfx => m_soundMgr.PlaySfx(sfx));

                            // 敌机爆装掉落物
                            var loot = e.TryDropLoot();
                            if (loot != null)
                            {
                                m_powerUps.Add(loot);
                            }

                            // 若击毁母舰 BOSS，触发胜利大捷
                            if (e.IsBoss)
                            {
                                m_state = GameState.Victory;
                                m_soundMgr.PlaySfx(SfxType.ShipUpgrade, true);
                            }
                        }

                        if (!b.IsActive) break; // 非穿透子弹命中后跳出当前内层循环
                    }
                }
            }

            // 2. 敌方子弹 vs 玩家战机
            for (int bi = 0; bi < m_enemyBullets.Count; bi++)
            {
                var b = m_enemyBullets[bi];
                if (!b.IsActive) continue;

                if (b.CollidesWith(m_player))
                {
                    b.SetActive(false);
                    m_player.TakeDamageWithShield(
                        b.Damage,
                        (pos, col) => m_particles.AddHitSpark(pos, col),
                        (pos, count, col) => m_particles.AddExplosion(pos, count, col),
                        sfx => m_soundMgr.PlaySfx(sfx)
                    );
                }
            }

            // 3. 玩家拾取升级补给物资
            for (int pi = 0; pi < m_powerUps.Count; pi++)
            {
                var p = m_powerUps[pi];
                if (!p.IsActive) continue;

                if (p.CollidesWith(m_player))
                {
                    p.SetActive(false);
                    m_particles.AddHitSpark(p.Pos, p.Color);

                    switch (p.Type)
                    {
                        case PowerUpType.Firepower:
                            m_player.UpgradeBullet(sfx => m_soundMgr.PlaySfx(sfx));
                            m_score += 200;
                            break;
                        case PowerUpType.ShipExp:
                            m_player.AddShipExp(40, sfx => m_soundMgr.PlaySfx(sfx));
                            m_score += 300;
                            break;
                        case PowerUpType.Shield:
                            m_player.RestoreShield(50, sfx => m_soundMgr.PlaySfx(sfx));
                            m_score += 200;
                            break;
                        case PowerUpType.Bomb:
                            m_player.AddBomb(1, sfx => m_soundMgr.PlaySfx(sfx));
                            m_score += 500;
                            break;
                        case PowerUpType.Score:
                            m_score += 1000;
                            m_soundMgr.PlaySfx(SfxType.PowerUp);
                            break;
                    }

                    if (m_score > m_highScore)
                    {
                        m_highScore = m_score;
                        PlayerPrefs.SetInt("GalaxyStrike_HighScore", m_highScore);
                    }
                }
            }

            // 4. 玩家机身 vs 敌机机身碰撞（战术近距撞击自损）
            for (int ei = 0; ei < m_enemies.Count; ei++)
            {
                var e = m_enemies[ei];
                if (!e.IsAlive) continue;

                if (m_player.CollidesWith(e))
                {
                    m_player.TakeDamageWithShield(
                        25,
                        (pos, col) => m_particles.AddHitSpark(pos, col),
                        (pos, count, col) => m_particles.AddExplosion(pos, count, col),
                        sfx => m_soundMgr.PlaySfx(sfx)
                    );
                    e.TakeDamage(40);
                    m_particles.AddExplosion(m_player.Pos, 14, GameColors.BrightRed);
                    TriggerCameraShake(0.25f, 0.16f);

                    if (!e.IsAlive)
                    {
                        m_soundMgr.PlaySfx(SfxType.Explosion);
                        m_particles.AddExplosion(e.Pos, 20, GameColors.BrightRed);
                        m_score += e.ScoreValue;
                        m_kills++;
                        var loot = e.TryDropLoot();
                        if (loot != null) m_powerUps.Add(loot);
                    }
                }
            }
        }

        // ------------------------------------------------------------------------
        // 【按键与指针输入跨系统多重冗余保障 (OnGUI + New Input System + Legacy Input)】
        // ------------------------------------------------------------------------
        private bool IsMouseLeftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            }
            catch {}
#endif
            try
            {
                if (Input.GetMouseButton(0)) return true;
            }
            catch {}
            return false;
        }

        private bool IsMouseRightClicked()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            }
            catch {}
#endif
            try
            {
                if (Input.GetMouseButtonDown(1)) return true;
            }
            catch {}
            return false;
        }

        private bool IsKeyDown(KeyCode code)
        {
            // 1. OnGUI 原始窗口事件队列 (100% 免疫系统设置或窗口失焦问题)
            if (m_guiPressedKeys.Contains(code)) return true;

            // 2. New Input System 模式
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    switch (code)
                    {
                        case KeyCode.W: if (kb.wKey.isPressed) return true; break;
                        case KeyCode.S: if (kb.sKey.isPressed) return true; break;
                        case KeyCode.A: if (kb.aKey.isPressed) return true; break;
                        case KeyCode.D: if (kb.dKey.isPressed) return true; break;
                        case KeyCode.UpArrow: if (kb.upArrowKey.isPressed) return true; break;
                        case KeyCode.DownArrow: if (kb.downArrowKey.isPressed) return true; break;
                        case KeyCode.LeftArrow: if (kb.leftArrowKey.isPressed) return true; break;
                        case KeyCode.RightArrow: if (kb.rightArrowKey.isPressed) return true; break;
                        case KeyCode.Space: if (kb.spaceKey.isPressed) return true; break;
                        case KeyCode.J: if (kb.jKey.isPressed) return true; break;
                        case KeyCode.K: if (kb.kKey.isPressed) return true; break;
                        case KeyCode.B: if (kb.bKey.isPressed) return true; break;
                        case KeyCode.P: if (kb.pKey.isPressed) return true; break;
                        case KeyCode.M: if (kb.mKey.isPressed) return true; break;
                        case KeyCode.Q: if (kb.qKey.isPressed) return true; break;
                        case KeyCode.Y: if (kb.yKey.isPressed) return true; break;
                        case KeyCode.N: if (kb.nKey.isPressed) return true; break;
                        case KeyCode.Escape: if (kb.escapeKey.isPressed) return true; break;
                        case KeyCode.Return: case KeyCode.KeypadEnter: if (kb.enterKey.isPressed || kb.numpadEnterKey.isPressed) return true; break;
                    }
                }
            }
            catch {}
#endif
            // 3. Unity 原生 Input Manager
            try
            {
                return Input.GetKey(code);
            }
            catch
            {
                return false;
            }
        }

        private bool IsKeyPressedThisFrame(params KeyCode[] codes)
        {
            // 1. OnGUI 本帧触发捕获
            if (m_lastGuiKeyDown != KeyCode.None)
            {
                foreach (var c in codes)
                {
                    if (m_lastGuiKeyDown == c) return true;
                }
            }

            // 2. New Input System 本帧触发捕获
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    foreach (var c in codes)
                    {
                        switch (c)
                        {
                            case KeyCode.Return: case KeyCode.KeypadEnter:
                                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) return true;
                                break;
                            case KeyCode.Space: if (kb.spaceKey.wasPressedThisFrame) return true; break;
                            case KeyCode.Q: if (kb.qKey.wasPressedThisFrame) return true; break;
                            case KeyCode.P: if (kb.pKey.wasPressedThisFrame) return true; break;
                            case KeyCode.Escape: if (kb.escapeKey.wasPressedThisFrame) return true; break;
                            case KeyCode.M: if (kb.mKey.wasPressedThisFrame) return true; break;
                            case KeyCode.Y: if (kb.yKey.wasPressedThisFrame) return true; break;
                            case KeyCode.N: if (kb.nKey.wasPressedThisFrame) return true; break;
                            case KeyCode.B: if (kb.bKey.wasPressedThisFrame) return true; break;
                            case KeyCode.K: if (kb.kKey.wasPressedThisFrame) return true; break;
                            case KeyCode.W: if (kb.wKey.wasPressedThisFrame) return true; break;
                            case KeyCode.S: if (kb.sKey.wasPressedThisFrame) return true; break;
                            case KeyCode.A: if (kb.aKey.wasPressedThisFrame) return true; break;
                            case KeyCode.D: if (kb.dKey.wasPressedThisFrame) return true; break;
                            case KeyCode.J: if (kb.jKey.wasPressedThisFrame) return true; break;
                        }
                    }
                }
            }
            catch {}
#endif
            // 3. Unity 原生 Input Manager
            try
            {
                foreach (var c in codes)
                {
                    if (Input.GetKeyDown(c)) return true;
                }
            }
            catch
            {
                // New Input System 独占模式下静默捕获
            }
            return false;
        }
    }
}
