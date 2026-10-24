using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;
using GalaxyStrike.Entities;
using GalaxyStrike.Systems;

namespace GalaxyStrike.Presentation
{
    // ============================================================================
    // 【视觉呈现与场景渲染引擎 · GalaxyStrikeRenderer.cs】
    // 负责将纯逻辑层的数据驱动实体（战机、敌军、弹幕、物资、星空、粒子、冲击波）
    // 映射到 Unity 场景中进行高帧率、霓虹发光视觉呈现。
    // ============================================================================

    public class GalaxyStrikeRenderer : MonoBehaviour
    {
        private Material m_spriteMaterial;
        private Material m_additiveMaterial;

        // 程序化生成的图形缓存
        private Texture2D m_whiteTex;
        private Sprite    m_starSprite;
        private Sprite    m_laserSprite;
        private Sprite    m_orbSprite;
        private Sprite    m_missileSprite;
        private Sprite    m_shieldBubbleSprite;
        private Sprite    m_ringSprite;
        private Sprite m_plasmaArt, m_enemyLaserArt;
        private bool m_hasCartoonArt;
        private Sprite m_flameArt, m_explosionArt, m_debrisArt;


        // Resources textures ship with builds; no editor-only asset lookup is needed.
        private void LoadCartoonArt()
        {
            const string path = "GalaxyStrikeArt/Sprites/";
            if (Resources.Load<Sprite>(path + "PlayerShip") == null) return;
            for (int i = 1; i <= 5; i++) m_playerShipSprites[i] = Resources.Load<Sprite>(path + (i == 1 ? "PlayerShip" : "PlayerShip_Lv" + i));
            EnemyType[] enemies = { EnemyType.Scout, EnemyType.Gunship, EnemyType.Bomber, EnemyType.Elite, EnemyType.Boss };
            string[] names = { "Enemy_Scout", "Enemy_Gunship", "Enemy_Bomber", "Enemy_EliteCruiser", "Enemy_BossMothership" };
            PowerUpType[] items = { PowerUpType.Firepower, PowerUpType.ShipExp, PowerUpType.Shield, PowerUpType.Bomb, PowerUpType.Score };
            string[] itemNames = { "PowerUp_Firepower", "PowerUp_ShipExp", "PowerUp_Shield", "PowerUp_Bomb", "PowerUp_Score" };
            for (int i = 0; i < 5; i++)
            {
                m_enemySprites[enemies[i]] = Resources.Load<Sprite>(path + names[i]);
                m_powerUpSprites[items[i]] = Resources.Load<Sprite>(path + itemNames[i]);
            }
            m_plasmaArt = Resources.Load<Sprite>(path + "Bullet_PlayerPlasma");
            m_laserSprite = Resources.Load<Sprite>(path + "Bullet_PlayerLaser");
            m_missileSprite = Resources.Load<Sprite>(path + "Bullet_PlayerMissile");
            m_orbSprite = Resources.Load<Sprite>(path + "Bullet_EnemyOrb");
            m_enemyLaserArt = Resources.Load<Sprite>(path + "Bullet_EnemyLaser");
            m_shieldBubbleSprite = Resources.Load<Sprite>(path + "FX_Shield");
            m_starSprite = Resources.Load<Sprite>(path + "FX_Star");
            m_ringSprite = Resources.Load<Sprite>(path + "FX_EMP");
            m_flameArt = Resources.Load<Sprite>(path + "FX_Thruster");
            m_explosionArt = Resources.Load<Sprite>(path + "FX_Explosion");
            m_debrisArt = Resources.Load<Sprite>(path + "FX_Debris");
            m_hasCartoonArt = true;
        }
        // 机体贴图缓存 (按机型与等级)
        private readonly Dictionary<int, Sprite>       m_playerShipSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<EnemyType, Sprite> m_enemySprites      = new Dictionary<EnemyType, Sprite>();
        private readonly Dictionary<PowerUpType, Sprite> m_powerUpSprites  = new Dictionary<PowerUpType, Sprite>();

        [Header("Prefab References (实体预制体模型)")]
        [SerializeField] private GameObject m_playerShipPrefab;
        [SerializeField] private GameObject m_enemyScoutPrefab;
        [SerializeField] private GameObject m_enemyGunshipPrefab;
        [SerializeField] private GameObject m_enemyBomberPrefab;
        [SerializeField] private GameObject m_enemyElitePrefab;
        [SerializeField] private GameObject m_enemyBossPrefab;
        [SerializeField] private GameObject m_bulletPlayerPlasmaPrefab;
        [SerializeField] private GameObject m_bulletPlayerLaserPrefab;
        [SerializeField] private GameObject m_bulletPlayerMissilePrefab;
        [SerializeField] private GameObject m_bulletEnemyOrbPrefab;
        [SerializeField] private GameObject m_bulletEnemyLaserPrefab;
        [SerializeField] private GameObject m_powerUpItemPrefab;

        [Header("Visual Scale Tuning (全局尺寸倍率调节)")]
        [Tooltip("战机与敌机整体尺寸倍率，默认 1 倍")]
        [Range(1.0f, 4.0f)]
        public float shipScaleMultiplier = 1f;

        [Tooltip("各类子弹弹道整体尺寸倍率，默认 1 倍")]
        [Range(1.0f, 4.0f)]
        public float bulletScaleMultiplier = 1f;

        [Tooltip("掉落物资道具整体尺寸倍率，默认 1 倍")]
        [Range(1.0f, 4.0f)]
        public float powerUpScaleMultiplier = 1f;

        // 对象池管理
        private readonly List<SpriteRenderer> m_starRenderers     = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> m_bulletRenderers   = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> m_enemyRenderers    = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> m_powerUpRenderers  = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> m_particleRenderers = new List<SpriteRenderer>();
        private readonly List<LineRenderer>   m_shockwaveLines    = new List<LineRenderer>();
        private readonly List<SpriteRenderer> m_shockwaveSprites = new List<SpriteRenderer>();

        private SpriteRenderer m_playerRenderer;
        private SpriteRenderer m_playerShieldRenderer;
        private SpriteRenderer m_leftThrusterRenderer;
        private SpriteRenderer m_rightThrusterRenderer;

        // 全息深空战术背景网格 (Cyber Holographic Grid)
        private GameObject m_gridQuad;
        private Mesh       m_gridMesh;
        private Material   m_gridMaterial;
        private Texture2D  m_gridTexture;
        private Vector2[]  m_gridUvs;
        private float      m_gridScrollV = 0.0f;

        private Transform m_starsRoot;
        private Transform m_bulletsRoot;
        private Transform m_enemiesRoot;
        private Transform m_powerUpsRoot;
        private Transform m_particlesRoot;
        private Transform m_shockwavesRoot;

        public void Initialize()
        {
            LoadPrefabsIfNull();

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (spriteShader == null) spriteShader = Shader.Find("Unlit/Color");

            m_spriteMaterial = new Material(spriteShader);
            m_additiveMaterial = new Material(spriteShader);

            GenerateProceduralSprites();
            LoadCartoonArt();
            CreateHierarchyRoots();
            CreateCyberGrid();
            CreatePlayerVisuals();
        }

        private void LoadPrefabsIfNull()
        {
#if UNITY_EDITOR
            const string dir = "Assets/GalaxyStrike/Prefabs";
            if (m_playerShipPrefab == null)
                m_playerShipPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/PlayerShip.prefab");
            if (m_enemyScoutPrefab == null)
                m_enemyScoutPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Enemy_Scout.prefab");
            if (m_enemyGunshipPrefab == null)
                m_enemyGunshipPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Enemy_Gunship.prefab");
            if (m_enemyBomberPrefab == null)
                m_enemyBomberPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Enemy_Bomber.prefab");
            if (m_enemyElitePrefab == null)
                m_enemyElitePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Enemy_EliteCruiser.prefab");
            if (m_enemyBossPrefab == null)
                m_enemyBossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Enemy_BossMothership.prefab");
            if (m_bulletPlayerPlasmaPrefab == null)
                m_bulletPlayerPlasmaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Bullet_PlayerPlasma.prefab");
            if (m_bulletPlayerLaserPrefab == null)
                m_bulletPlayerLaserPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Bullet_PlayerLaser.prefab");
            if (m_bulletPlayerMissilePrefab == null)
                m_bulletPlayerMissilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Bullet_PlayerMissile.prefab");
            if (m_bulletEnemyOrbPrefab == null)
                m_bulletEnemyOrbPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Bullet_EnemyOrb.prefab");
            if (m_bulletEnemyLaserPrefab == null)
                m_bulletEnemyLaserPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/Bullet_EnemyLaser.prefab");
            if (m_powerUpItemPrefab == null)
                m_powerUpItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/PowerUp_Item.prefab");
#endif
        }

        private void OnDestroy()
        {
            if (m_spriteMaterial != null) Destroy(m_spriteMaterial);
            if (m_additiveMaterial != null) Destroy(m_additiveMaterial);
            if (m_whiteTex != null) Destroy(m_whiteTex);
            if (m_gridMesh != null) Destroy(m_gridMesh);
            if (m_gridMaterial != null) Destroy(m_gridMaterial);
            if (m_gridTexture != null) Destroy(m_gridTexture);
            if (m_gridQuad != null) Destroy(m_gridQuad);
        }

        private void CreateHierarchyRoots()
        {
            m_starsRoot      = CreateFolder("Stars_Layer");
            m_bulletsRoot    = CreateFolder("Bullets_Layer");
            m_enemiesRoot    = CreateFolder("Enemies_Layer");
            m_powerUpsRoot   = CreateFolder("PowerUps_Layer");
            m_particlesRoot  = CreateFolder("Particles_Layer");
            m_shockwavesRoot = CreateFolder("Shockwaves_Layer");
        }

        private Transform CreateFolder(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        // ------------------------------------------------------------------------
        // 【深空战术全息网格系统 (Tactical Holographic Cyber Grid)】
        // ------------------------------------------------------------------------
        private void CreateCyberGrid()
        {
            // 1. 程序化生成 64x64 无缝循环科幻全息经纬网格贴图
            m_gridTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            m_gridTexture.wrapMode = TextureWrapMode.Repeat;
            m_gridTexture.filterMode = FilterMode.Bilinear;

            Color gridLine = new Color(0.04f, 0.40f, 0.75f, 0.14f);   // 优雅半透明青蓝经纬线
            Color gridCorner = new Color(0.18f, 0.88f, 1.0f, 0.35f);  // 十字交叉点高光节点
            Color clear = Color.clear;

            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool isBorder = (x == 0 || x == 63 || y == 0 || y == 63);
                    bool isCross = (x <= 2 || x >= 61) && (y <= 2 || y >= 61);

                    if (isCross)
                        pixels[y * 64 + x] = gridCorner;
                    else if (isBorder)
                        pixels[y * 64 + x] = gridLine;
                    else
                        pixels[y * 64 + x] = clear;
                }
            }
            m_gridTexture.SetPixels(pixels);
            m_gridTexture.Apply();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            m_gridMaterial = new Material(shader);
            m_gridMaterial.mainTexture = m_gridTexture;

            // 2. 构建精准对齐战斗交火空域的 Quad
            m_gridQuad = new GameObject("CyberGrid_Background");
            m_gridQuad.transform.SetParent(transform, false);
            m_gridQuad.transform.position = Vector3.zero;

            var mf = m_gridQuad.AddComponent<MeshFilter>();
            var mr = m_gridQuad.AddComponent<MeshRenderer>();
            mr.material = m_gridMaterial;
            mr.sortingOrder = -18; // 位于视差星空(-10)之后，纯黑背景之前

            float halfW = (GameConfig.PLAY_WIDTH * GameConfig.WORLD_UNIT_SCALE) * 0.5f;
            float halfH = (GameConfig.PLAY_HEIGHT * GameConfig.WORLD_UNIT_SCALE) * 0.5f;

            m_gridMesh = new Mesh();
            m_gridMesh.name = "CyberGridMesh";
            m_gridMesh.vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, 0f),
                new Vector3( halfW, -halfH, 0f),
                new Vector3(-halfW,  halfH, 0f),
                new Vector3( halfW,  halfH, 0f)
            };
            m_gridMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            float tilingX = 13.0f;
            float tilingY = 5.375f;
            m_gridUvs = new Vector2[4]
            {
                new Vector2(0f, 0f),
                new Vector2(tilingX, 0f),
                new Vector2(0f, tilingY),
                new Vector2(tilingX, tilingY)
            };
            m_gridMesh.uv = m_gridUvs;
            m_gridMesh.RecalculateNormals();
            mf.sharedMesh = m_gridMesh;
        }

        private void UpdateCyberGrid(bool isWarp, float dt)
        {
            if (m_gridMesh == null || m_gridUvs == null) return;
            float scrollSpeed = isWarp ? 0.38f : 0.075f;
            m_gridScrollV += scrollSpeed * dt;
            if (m_gridScrollV > 1.0f) m_gridScrollV -= 1.0f;

            float tilingX = 13.0f;
            float tilingY = 5.375f;
            m_gridUvs[0] = new Vector2(0f, -m_gridScrollV);
            m_gridUvs[1] = new Vector2(tilingX, -m_gridScrollV);
            m_gridUvs[2] = new Vector2(0f, tilingY - m_gridScrollV);
            m_gridUvs[3] = new Vector2(tilingX, tilingY - m_gridScrollV);
            m_gridMesh.uv = m_gridUvs;
        }

        private void CreatePlayerVisuals()
        {
            GameObject pGo;
            if (m_playerShipPrefab != null)
            {
                pGo = Instantiate(m_playerShipPrefab, transform, false);
                pGo.name = "PlayerShip";
            }
            else
            {
                pGo = new GameObject("PlayerShip");
                pGo.transform.SetParent(transform, false);
            }

            m_playerRenderer = pGo.GetComponent<SpriteRenderer>();
            if (m_playerRenderer == null) m_playerRenderer = pGo.AddComponent<SpriteRenderer>();
            m_playerRenderer.material = m_spriteMaterial;
            m_playerRenderer.sortingOrder = 10;

            // 护盾光罩
            var sTrans = pGo.transform.Find("ShieldBubble");
            GameObject sGo = sTrans != null ? sTrans.gameObject : new GameObject("ShieldBubble");
            if (sTrans == null) sGo.transform.SetParent(pGo.transform, false);
            m_playerShieldRenderer = sGo.GetComponent<SpriteRenderer>();
            if (m_playerShieldRenderer == null) m_playerShieldRenderer = sGo.AddComponent<SpriteRenderer>();
            m_playerShieldRenderer.material = m_spriteMaterial;
            m_playerShieldRenderer.sprite = m_shieldBubbleSprite;
            m_playerShieldRenderer.color = new Color(0.2f, 0.9f, 1f, 0.45f);
            m_playerShieldRenderer.sortingOrder = 11;

            // 双发喷射尾焰
            var tLTrans = pGo.transform.Find("Thruster_L");
            GameObject tLeft = tLTrans != null ? tLTrans.gameObject : new GameObject("Thruster_L");
            if (tLTrans == null) tLeft.transform.SetParent(pGo.transform, false);
            m_leftThrusterRenderer = tLeft.GetComponent<SpriteRenderer>();
            if (m_leftThrusterRenderer == null) m_leftThrusterRenderer = tLeft.AddComponent<SpriteRenderer>();
            m_leftThrusterRenderer.material = m_spriteMaterial;
            m_leftThrusterRenderer.sprite = m_flameArt != null ? m_flameArt : m_laserSprite;
            m_leftThrusterRenderer.color = m_hasCartoonArt ? Color.white : GameColors.BrightBlue;
            m_leftThrusterRenderer.sortingOrder = 9;

            var tRTrans = pGo.transform.Find("Thruster_R");
            GameObject tRight = tRTrans != null ? tRTrans.gameObject : new GameObject("Thruster_R");
            if (tRTrans == null) tRight.transform.SetParent(pGo.transform, false);
            m_rightThrusterRenderer = tRight.GetComponent<SpriteRenderer>();
            if (m_rightThrusterRenderer == null) m_rightThrusterRenderer = tRight.AddComponent<SpriteRenderer>();
            m_rightThrusterRenderer.material = m_spriteMaterial;
            m_rightThrusterRenderer.sprite = m_flameArt != null ? m_flameArt : m_laserSprite;
            m_rightThrusterRenderer.color = m_hasCartoonArt ? Color.white : GameColors.BrightBlue;
            m_rightThrusterRenderer.sortingOrder = 9;
        }

        // ------------------------------------------------------------------------
        // 【核心逐帧渲染分发】
        // ------------------------------------------------------------------------
        public void RenderFrame(Player player, List<Enemy> enemies, List<Bullet> playerBullets,
                                List<Bullet> enemyBullets, List<PowerUp> powerUps,
                                ParticleSystemManager particles, StarfieldManager starfield)
        {
            // 0. 更新全息战术网格平滑流光滚动
            UpdateCyberGrid(starfield.Warp, Time.deltaTime);

            // 1. 渲染星空背景
            RenderStars(starfield);

            // 2. 渲染玩家战机
            RenderPlayer(player);

            // 3. 渲染敌机机群
            RenderEnemies(enemies);

            // 4. 渲染子弹与弹幕
            RenderBullets(playerBullets, enemyBullets);

            // 5. 渲染掉落补给
            RenderPowerUps(powerUps);

            // 6. 渲染爆炸碎片与火花
            RenderParticles(particles);

            // 7. 渲染全屏 EMP 震荡波
            RenderShockwaves(particles);
        }

        private void RenderStars(StarfieldManager starfield)
        {
            var stars = starfield.Stars;
            EnsurePoolSize(m_starRenderers, stars.Count, m_starsRoot, "Star");

            for (int i = 0; i < m_starRenderers.Count; i++)
            {
                if (i < stars.Count)
                {
                    var s = stars[i];
                    m_starRenderers[i].gameObject.SetActive(true);
                    m_starRenderers[i].transform.position = GameConfig.GridToWorld(new Vector2(s.x, s.y));
                    m_starRenderers[i].sprite = m_starSprite;
                    m_starRenderers[i].color = s.color;
                    m_starRenderers[i].sortingOrder = -10;

                    if (s.symbol == '|')
                    {
                        // 高速空间流光细线：拉伸纵向比例
                        Vector2 streakSize = new Vector2(0.08f, 0.45f);
                        m_starRenderers[i].transform.localScale = GalaxyStrikeVisualSize.Fit(m_starSprite, streakSize);
                    }
                    else
                    {
                        float sz = ((s.symbol == '*') ? 0.45f : ((s.symbol == '+') ? 0.32f : 0.20f)) * 1.35f;
                        m_starRenderers[i].transform.localScale = GalaxyStrikeVisualSize.Fit(m_starSprite, Vector2.one * sz * 0.16f);
                    }
                }
                else
                {
                    m_starRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void RenderPlayer(Player player)
        {
            if (!player.IsActive)
            {
                m_playerRenderer.gameObject.SetActive(false);
                return;
            }

            m_playerRenderer.gameObject.SetActive(true);

            // 无敌保护闪烁反馈
            if (player.IsInvulnerable)
            {
                int blink = Mathf.FloorToInt(player.InvulnTimer * 12.0f);
                m_playerRenderer.enabled = (blink % 2 != 0);
            }
            else
            {
                m_playerRenderer.enabled = true;
            }

            Vector2 center = new Vector2(player.Pos.x + player.Width * 0.5f, player.Pos.y + player.Height * 0.5f);
            m_playerRenderer.transform.position = GameConfig.GridToWorld(center);
            m_playerRenderer.transform.rotation = Quaternion.Euler(0f, 0f, player.RollAngle);

            if (m_playerShipSprites.TryGetValue(player.ShipLevel, out var sprite))
            {
                m_playerRenderer.sprite = sprite;
            }
            m_playerRenderer.color = m_hasCartoonArt ? Color.white : player.Color;

            // 应用尺寸放大倍率，使战机在画面中清晰鲜明
            float scaleX = player.Width * GameConfig.WORLD_UNIT_SCALE * shipScaleMultiplier;
            float scaleY = player.Height * GameConfig.WORLD_UNIT_SCALE * shipScaleMultiplier;
            m_playerRenderer.transform.localScale = GalaxyStrikeVisualSize.Fit(m_playerRenderer.sprite, new Vector2(scaleX, scaleY));

            // 护盾光罩渲染
            bool hasShield = player.Shield > 0;
            m_playerShieldRenderer.gameObject.SetActive(hasShield);
            if (hasShield)
            {
                float shieldScale = 1.35f;
                m_playerShieldRenderer.transform.localScale = GalaxyStrikeVisualSize.Fit(m_playerShieldRenderer.sprite, (Vector2)m_playerRenderer.sprite.bounds.size * shieldScale);
                float shieldAlpha = Mathf.Clamp01(0.12f + 0.22f * ((float)player.Shield / player.MaxShield));
                m_playerShieldRenderer.color = new Color(0.2f, 0.9f, 1f, shieldAlpha);
            }

            // 引擎喷焰动态抖动与喷口定位（随油门推力动态伸缩）
            float flameW = m_playerRenderer.sprite.bounds.size.x * 0.18f;
            float flameH = m_playerRenderer.sprite.bounds.size.y * UnityEngine.Random.Range(0.24f, 0.38f) * player.Throttle;
            float nozzleX = m_playerRenderer.sprite.bounds.size.x * 0.26f;
            float nozzleY = -m_playerRenderer.sprite.bounds.size.y * 0.42f;

            m_leftThrusterRenderer.transform.localPosition = new Vector3(-nozzleX, nozzleY, 0f);
            m_leftThrusterRenderer.transform.localScale = GalaxyStrikeVisualSize.Fit(m_leftThrusterRenderer.sprite, new Vector2(flameW, flameH));

            m_rightThrusterRenderer.transform.localPosition = new Vector3(nozzleX, nozzleY, 0f);
            m_rightThrusterRenderer.transform.localScale = GalaxyStrikeVisualSize.Fit(m_rightThrusterRenderer.sprite, new Vector2(flameW, flameH));
        }

        private void RenderEnemies(List<Enemy> enemies)
        {
            EnsureEnemyPoolSize(enemies);

            for (int i = 0; i < m_enemyRenderers.Count; i++)
            {
                if (i < enemies.Count)
                {
                    var e = enemies[i];
                    m_enemyRenderers[i].gameObject.SetActive(true);

                    Vector2 center = new Vector2(e.Pos.x + e.Width * 0.5f, e.Pos.y + e.Height * 0.5f);
                    m_enemyRenderers[i].transform.position = GameConfig.GridToWorld(center);

                    if (m_enemySprites.TryGetValue(e.Type, out var spr))
                    {
                        m_enemyRenderers[i].sprite = spr;
                    }
                    m_enemyRenderers[i].color = m_hasCartoonArt ? Color.Lerp(Color.white, e.Color, e.IsBoss ? 0.15f : 0f) : e.Color;
                    m_enemyRenderers[i].sortingOrder = e.IsBoss ? 8 : 5;

                    // 战术尺寸缩放（BOSS微调避免超出交火区边界）
                    float mul = shipScaleMultiplier;
                    float sx = e.Width * GameConfig.WORLD_UNIT_SCALE * mul;
                    float sy = e.Height * GameConfig.WORLD_UNIT_SCALE * mul;
                    m_enemyRenderers[i].transform.localScale = GalaxyStrikeVisualSize.Fit(m_enemyRenderers[i].sprite, new Vector2(sx, sy));
                }
                else
                {
                    m_enemyRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void RenderBullets(List<Bullet> playerBullets, List<Bullet> enemyBullets)
        {
            int totalBullets = playerBullets.Count + enemyBullets.Count;
            EnsureBulletPoolSize(totalBullets);

            int idx = 0;

            // 玩家子弹渲染（加粗加大 + 运动自适应拉伸光刃，彻底消灭频闪跳跃）
            for (int i = 0; i < playerBullets.Count; i++, idx++)
            {
                var b = playerBullets[i];
                var r = m_bulletRenderers[idx];
                r.gameObject.SetActive(true);
                r.transform.position = GameConfig.GridToWorld(b.Pos + new Vector2(b.Width * 0.5f, b.Height * 0.5f));
                r.color = m_hasCartoonArt ? Color.white : b.Color;
                r.sortingOrder = 7;

                float speed = b.Vel.magnitude;
                // 动态运动拉伸：速度越快纵向越拉伸，消除帧间断裂，形成连续光刃
                float stretch = Mathf.Max(1.0f, 1.0f + (speed - 22.0f) * 0.020f);

                if (b.Type == BulletType.Homing)
                {
                    r.sprite = m_missileSprite;
                    float angle = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg + 90f;
                    r.transform.rotation = Quaternion.Euler(0, 0, -angle);
                    Vector2 baseSz = GalaxyStrikeVisualSize.Bullet(b.Type) * bulletScaleMultiplier;
                    r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(baseSz.x, baseSz.y * (1.0f + (speed - 15f) * 0.012f)));
                }
                else if (b.Type == BulletType.Laser)
                {
                    r.sprite = m_laserSprite;
                    float angle = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg + 90f;
                    r.transform.rotation = Quaternion.Euler(0, 0, -angle);
                    Vector2 baseSz = GalaxyStrikeVisualSize.Bullet(b.Type) * bulletScaleMultiplier;
                    r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(baseSz.x, baseSz.y * stretch * 1.35f));
                }
                else
                {
                    r.sprite = m_plasmaArt != null ? m_plasmaArt : m_laserSprite;
                    float angle = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg + 90f;
                    r.transform.rotation = Quaternion.Euler(0, 0, -angle);
                    Vector2 baseSz = GalaxyStrikeVisualSize.Bullet(b.Type) * bulletScaleMultiplier;
                    r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(baseSz.x, baseSz.y * stretch));
                }
            }

            // 敌方子弹渲染
            for (int i = 0; i < enemyBullets.Count; i++, idx++)
            {
                var b = enemyBullets[i];
                var r = m_bulletRenderers[idx];
                r.gameObject.SetActive(true);
                r.transform.position = GameConfig.GridToWorld(b.Pos + new Vector2(b.Width * 0.5f, b.Height * 0.5f));
                r.color = m_hasCartoonArt ? Color.white : b.Color;
                r.sortingOrder = 6;

                if (b.Type == BulletType.BossHelix || b.Type == BulletType.EnemyOrb)
                {
                    r.sprite = m_orbSprite;
                    r.transform.rotation = Quaternion.identity;
                    // 能量光球微脉冲呼吸动效
                    float pulse = 1.0f + 0.09f * Mathf.Sin(Time.time * 16.0f + i * 1.4f);
                    r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, GalaxyStrikeVisualSize.Bullet(b.Type) * bulletScaleMultiplier * pulse);
                }
                else
                {
                    r.sprite = m_enemyLaserArt != null ? m_enemyLaserArt : m_laserSprite;
                    float angle = Mathf.Atan2(b.Vel.y, b.Vel.x) * Mathf.Rad2Deg + 90f;
                    r.transform.rotation = Quaternion.Euler(0, 0, -angle);
                    float speed = b.Vel.magnitude;
                    float stretch = Mathf.Max(1.0f, 1.0f + (speed - 15.0f) * 0.016f);
                    Vector2 baseSz = GalaxyStrikeVisualSize.Bullet(b.Type) * bulletScaleMultiplier;
                    r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(baseSz.x, baseSz.y * stretch));
                }
            }

            // 关闭多余池对象
            for (; idx < m_bulletRenderers.Count; idx++)
            {
                m_bulletRenderers[idx].gameObject.SetActive(false);
            }
        }

        private void RenderPowerUps(List<PowerUp> powerUps)
        {
            EnsurePowerUpPoolSize(powerUps.Count);

            for (int i = 0; i < m_powerUpRenderers.Count; i++)
            {
                if (i < powerUps.Count)
                {
                    var p = powerUps[i];
                    m_powerUpRenderers[i].gameObject.SetActive(true);

                    Vector2 center = new Vector2(p.Pos.x + p.Width * 0.5f, p.Pos.y + p.Height * 0.5f);
                    m_powerUpRenderers[i].transform.position = GameConfig.GridToWorld(center);

                    if (m_powerUpSprites.TryGetValue(p.Type, out var spr))
                    {
                        m_powerUpRenderers[i].sprite = spr;
                    }
                    m_powerUpRenderers[i].color = m_hasCartoonArt ? Color.white : p.Color;
                    m_powerUpRenderers[i].sortingOrder = 4;
                    m_powerUpRenderers[i].transform.localScale = GalaxyStrikeVisualSize.Fit(spr, GalaxyStrikeVisualSize.Grid(p.Width, p.Height) * powerUpScaleMultiplier);
                }
                else
                {
                    m_powerUpRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void RenderParticles(ParticleSystemManager particles)
        {
            var pList = particles.Particles;
            EnsurePoolSize(m_particleRenderers, pList.Count, m_particlesRoot, "Particle");

            for (int i = 0; i < m_particleRenderers.Count; i++)
            {
                if (i < pList.Count)
                {
                    var p = pList[i];
                    var r = m_particleRenderers[i];
                    r.gameObject.SetActive(true);
                    r.transform.position = GameConfig.GridToWorld(p.pos);

                    float ratio = Mathf.Clamp01(p.life / Mathf.Max(0.001f, p.maxLife));
                    float progress = 1f - ratio;

                    if (m_hasCartoonArt)
                    {
                        if (p.kind == ParticleKind.Fireball)
                        {
                            // 核心微型爆闪：利落迅速，无浓烟残留
                            r.sprite = m_explosionArt != null ? m_explosionArt : m_starSprite;
                            r.sortingOrder = 14;
                            r.transform.rotation = Quaternion.Euler(0f, 0f, p.rotation);

                            // 绽放曲线：极速爆发后迅速清透消散，不留深暗浓烟
                            float scaleMult = progress < 0.2f
                                ? Mathf.Lerp(0.3f, 1.0f, progress / 0.2f)
                                : Mathf.Lerp(1.0f, 1.15f, (progress - 0.2f) / 0.8f);
                            float size = (p.baseScale > 0f ? p.baseScale : 0.58f) * scaleMult;
                            r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(size, size));

                            float alpha = Mathf.Pow(ratio, 1.5f);
                            Color fireCol = Color.Lerp(new Color(1f, 0.95f, 0.75f, alpha), new Color(1f, 0.35f, 0.1f, alpha * 0.85f), progress);
                            r.color = fireCol;
                        }
                        else if (p.kind == ParticleKind.Debris)
                        {
                            // 极少量飞旋破片残骸
                            r.sprite = m_debrisArt != null ? m_debrisArt : m_starSprite;
                            r.sortingOrder = 13;
                            r.transform.rotation = Quaternion.Euler(0f, 0f, p.rotation);
                            float size = (p.baseScale > 0f ? p.baseScale : 0.28f) * (0.4f + 0.6f * ratio);
                            r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(size, size));
                            float alpha = Mathf.Pow(ratio, 1.3f);
                            r.color = Color.Lerp(new Color(0.6f, 0.15f, 0.08f, alpha * 0.7f), new Color(1f, 0.65f, 0.25f, alpha), ratio);
                        }
                        else if (p.kind == ParticleKind.BranchFork)
                        {
                            // 炸弹清屏分叉微型电弧/能量光刺：高能炽热，带方向延伸，迅速消隐
                            r.sprite = m_laserSprite != null ? m_laserSprite : m_starSprite;
                            r.sortingOrder = 16;
                            float angle = Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg + 90f;
                            r.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
                            float length = (p.baseScale > 0f ? p.baseScale : 0.5f) * (0.35f + 0.65f * ratio);
                            float width = 0.10f * ratio;
                            r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(width, length));
                            float alpha = Mathf.Pow(ratio, 1.2f);
                            r.color = Color.Lerp(new Color(1f, 0.25f, 0.1f, alpha * 0.85f), new Color(1f, 0.95f, 0.45f, alpha), ratio);
                        }
                        else
                        {
                            // 稀疏飞溅火花星芒 (Spark / Thruster)
                            r.sprite = m_starSprite;
                            r.sortingOrder = 12;
                            r.transform.rotation = Quaternion.identity;
                            float size = (p.baseScale > 0f ? p.baseScale : 0.22f) * (0.4f + 0.6f * ratio);
                            r.transform.localScale = GalaxyStrikeVisualSize.Fit(r.sprite, new Vector2(size, size));
                            float alpha = Mathf.Pow(ratio, 1.3f);
                            Color sparkCol = Color.Lerp(new Color(1f, 0.25f, 0.1f, alpha), new Color(1f, 0.95f, 0.45f, alpha), ratio);
                            r.color = sparkCol;
                        }
                    }
                    else
                    {
                        // 经典矢量/字符模式兼容
                        r.sprite = (p.kind == ParticleKind.Fireball && m_explosionArt != null) ? m_explosionArt : m_starSprite;
                        r.color = p.color;
                        r.sortingOrder = 12;
                        r.transform.localScale = Vector3.one * (ratio * 0.25f);
                    }
                }
                else
                {
                    m_particleRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureEnemyPoolSize(List<Enemy> enemies)
        {
            while (m_enemyRenderers.Count < enemies.Count)
            {
                int idx = m_enemyRenderers.Count;
                EnemyType type = (idx < enemies.Count) ? enemies[idx].Type : EnemyType.Scout;
                GameObject prefab = GetEnemyPrefab(type);

                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab, m_enemiesRoot, false);
                    go.name = $"Enemy_{idx}";
                }
                else
                {
                    go = new GameObject($"Enemy_{idx}");
                    go.transform.SetParent(m_enemiesRoot, false);
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.material = m_spriteMaterial;
                m_enemyRenderers.Add(sr);
            }
        }

        private GameObject GetEnemyPrefab(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Scout: return m_enemyScoutPrefab;
                case EnemyType.Gunship: return m_enemyGunshipPrefab;
                case EnemyType.Bomber: return m_enemyBomberPrefab;
                case EnemyType.Elite: return m_enemyElitePrefab;
                case EnemyType.Boss: return m_enemyBossPrefab;
                default: return m_enemyScoutPrefab;
            }
        }

        private void EnsureBulletPoolSize(int needed)
        {
            while (m_bulletRenderers.Count < needed)
            {
                int idx = m_bulletRenderers.Count;
                GameObject prefab = m_bulletPlayerPlasmaPrefab;

                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab, m_bulletsRoot, false);
                    go.name = $"Bullet_{idx}";
                }
                else
                {
                    go = new GameObject($"Bullet_{idx}");
                    go.transform.SetParent(m_bulletsRoot, false);
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.material = m_spriteMaterial;
                m_bulletRenderers.Add(sr);
            }
        }

        private void EnsurePowerUpPoolSize(int needed)
        {
            while (m_powerUpRenderers.Count < needed)
            {
                int idx = m_powerUpRenderers.Count;
                GameObject prefab = m_powerUpItemPrefab;

                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab, m_powerUpsRoot, false);
                    go.name = $"PowerUp_{idx}";
                }
                else
                {
                    go = new GameObject($"PowerUp_{idx}");
                    go.transform.SetParent(m_powerUpsRoot, false);
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.material = m_spriteMaterial;
                m_powerUpRenderers.Add(sr);
            }
        }

        private void RenderShockwaves(ParticleSystemManager particles)
        {
            var swList = particles.Shockwaves;
            if (m_flameArt != null)
            {
                EnsurePoolSize(m_shockwaveSprites, swList.Count, m_shockwavesRoot, "CartoonEMP");
                for (int i = 0; i < m_shockwaveSprites.Count; i++)
                {
                    var renderer = m_shockwaveSprites[i];
                    renderer.gameObject.SetActive(i < swList.Count);
                    if (i >= swList.Count) continue;
                    var wave = swList[i];
                    renderer.sprite = m_ringSprite;
                    renderer.sortingOrder = 15;
                    renderer.transform.position = GameConfig.GridToWorld(wave.center);

                    float progress = Mathf.Clamp01(wave.currentRadius / Mathf.Max(0.01f, wave.maxRadius));
                    // 快速清透消隐曲线：干净利落，无拖泥带水的残影
                    float alpha = Mathf.Pow(1f - progress, 1.6f);

                    // 鲜亮辐射双层环色彩调和
                    Color flameTone;
                    if (progress < 0.3f)
                        flameTone = Color.Lerp(new Color(1f, 0.96f, 0.8f), wave.color, progress / 0.3f);
                    else
                        flameTone = Color.Lerp(wave.color, new Color(1f, 0.2f, 0.08f), (progress - 0.3f) / 0.7f);

                    renderer.color = new Color(flameTone.r, flameTone.g, flameTone.b, alpha);

                    float diameter = wave.currentRadius * GameConfig.WORLD_UNIT_SCALE * 2f;
                    renderer.transform.localScale = GalaxyStrikeVisualSize.Fit(renderer.sprite, new Vector2(diameter, diameter * 0.85f));
                }
                return;
            }
            EnsureShockwavePool(swList.Count);

            for (int i = 0; i < m_shockwaveLines.Count; i++)
            {
                if (i < swList.Count)
                {
                    var sw = swList[i];
                    var lr = m_shockwaveLines[i];
                    lr.gameObject.SetActive(true);

                    int segments = 48;
                    lr.positionCount = segments + 1;

                    float progress = Mathf.Clamp01(sw.currentRadius / Mathf.Max(0.01f, sw.maxRadius));
                    float alpha = 1f - progress;
                    Color lineCol = sw.color * new Color(1f, 0.5f, 0.2f, alpha);
                    lr.startColor = lineCol;
                    lr.endColor = lineCol;
                    lr.startWidth = Mathf.Lerp(0.26f, 0.08f, progress);
                    lr.endWidth = Mathf.Lerp(0.26f, 0.08f, progress);

                    Vector3 centerWorld = GameConfig.GridToWorld(sw.center);
                    float r = sw.currentRadius * GameConfig.WORLD_UNIT_SCALE;

                    for (int s = 0; s <= segments; s++)
                    {
                        float rad = s * (Mathf.PI * 2f / segments);
                        float px = centerWorld.x + Mathf.Cos(rad) * r;
                        // 终端纵横比补偿
                        float py = centerWorld.y + Mathf.Sin(rad) * r * 0.85f;
                        lr.SetPosition(s, new Vector3(px, py, 0f));
                    }
                }
                else
                {
                    m_shockwaveLines[i].gameObject.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------------
        // 【对象池辅助】
        // ------------------------------------------------------------------------
        private void EnsurePoolSize(List<SpriteRenderer> pool, int needed, Transform parent, string prefix)
        {
            while (pool.Count < needed)
            {
                var go = new GameObject($"{prefix}_{pool.Count}");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.material = m_spriteMaterial;
                pool.Add(sr);
            }
        }

        private void EnsureShockwavePool(int needed)
        {
            while (m_shockwaveLines.Count < needed)
            {
                var go = new GameObject($"Shockwave_{m_shockwaveLines.Count}");
                go.transform.SetParent(m_shockwavesRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = m_additiveMaterial;
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.sortingOrder = 15;
                m_shockwaveLines.Add(lr);
            }
        }

        // ------------------------------------------------------------------------
        // 【程序化贴图与精灵生成 (Procedural Vector & Pixel Sprites)】
        // ------------------------------------------------------------------------
        private void GenerateProceduralSprites()
        {
            m_whiteTex = new Texture2D(4, 4);
            Color[] wPix = new Color[16];
            for (int i = 0; i < 16; i++) wPix[i] = Color.white;
            m_whiteTex.SetPixels(wPix);
            m_whiteTex.Apply();

            m_starSprite = CreateCircleSprite(16, 16, Color.white);
            m_laserSprite = CreateCapsuleSprite(12, 32, Color.white);
            m_orbSprite = CreateGlowCircleSprite(24, 24, Color.white);
            m_missileSprite = CreateMissileSprite(16, 28, GameColors.BrightRed);
            m_shieldBubbleSprite = CreateRingSprite(64, 64, 4, Color.white);
            m_ringSprite = CreateRingSprite(64, 64, 3, Color.white);

            // 战机 5 阶形态
            m_playerShipSprites[1] = CreateShipSprite(24, 20, 1);
            m_playerShipSprites[2] = CreateShipSprite(28, 22, 2);
            m_playerShipSprites[3] = CreateShipSprite(34, 26, 3);
            m_playerShipSprites[4] = CreateShipSprite(40, 30, 4);
            m_playerShipSprites[5] = CreateShipSprite(48, 36, 5);

            // 5 种敌机
            m_enemySprites[EnemyType.Scout]   = CreateEnemySprite(20, 18, EnemyType.Scout);
            m_enemySprites[EnemyType.Gunship] = CreateEnemySprite(28, 20, EnemyType.Gunship);
            m_enemySprites[EnemyType.Bomber]  = CreateEnemySprite(34, 24, EnemyType.Bomber);
            m_enemySprites[EnemyType.Elite]   = CreateEnemySprite(38, 26, EnemyType.Elite);
            m_enemySprites[EnemyType.Boss]    = CreateBossSprite(80, 36);

            // 5 种掉落物
            m_powerUpSprites[PowerUpType.Firepower] = CreateBadgeSprite(24, 24, "P", GameColors.BrightYellow);
            m_powerUpSprites[PowerUpType.ShipExp]   = CreateBadgeSprite(24, 24, "U", GameColors.BrightCyan);
            m_powerUpSprites[PowerUpType.Shield]    = CreateBadgeSprite(24, 24, "S", GameColors.BrightGreen);
            m_powerUpSprites[PowerUpType.Bomb]      = CreateBadgeSprite(24, 24, "B", GameColors.BrightRed);
            m_powerUpSprites[PowerUpType.Score]     = CreateBadgeSprite(24, 24, "$", GameColors.BrightWhite);
        }

        private Sprite CreateCircleSprite(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float radius = Mathf.Min(cx, cy) - 1f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateGlowCircleSprite(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxR = Mathf.Min(cx, cy);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float ratio = Mathf.Clamp01(dist / maxR);
                    float alpha = Mathf.Pow(1.0f - ratio, 2.0f);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCapsuleSprite(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x - (w * 0.5f)) / (w * 0.5f));
                    float alpha = Mathf.Clamp01(1.0f - u);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite(int w, int h, int thickness, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rOuter = Mathf.Min(cx, cy) - 1f;
            float rInner = rOuter - thickness;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist >= rInner && dist <= rOuter)
                    {
                        float mid = (rOuter + rInner) * 0.5f;
                        float diff = 1.0f - Mathf.Abs(dist - mid) / (thickness * 0.5f);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, diff));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateMissileSprite(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (float)y / h;
                    bool inBody = Mathf.Abs(nx) < (1.0f - ny * 0.6f) * 0.6f;
                    bool inFin = y < h * 0.35f && Mathf.Abs(nx) < 0.9f;

                    if (inBody || inFin)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateShipSprite(int w, int h, int tier)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.5f));
                    float ny = (float)y / h; // 0 是机尾，1 是机头 (在世界空间 Y 轴朝上)

                    // 三角与主翼拓扑
                    bool hull = nx <= (1.0f - ny * 0.85f);
                    bool wingWings = (y < h * 0.45f) && nx <= 0.95f;
                    bool cockpit = (ny > 0.4f && ny < 0.8f && nx < 0.25f);

                    if (hull || wingWings)
                    {
                        Color c = cockpit ? Color.white : (nx < 0.3f ? new Color(0.9f, 0.9f, 0.9f) : Color.white);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateEnemySprite(int w, int h, EnemyType type)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.5f));
                    float ny = (float)y / h; // 0 是头部朝下俯冲，1 是尾部

                    bool body = false;
                    switch (type)
                    {
                        case EnemyType.Scout:
                            body = (nx <= (ny * 0.9f)) || (ny > 0.7f && nx < 0.9f);
                            break;
                        case EnemyType.Gunship:
                            body = (nx < 0.75f && ny > 0.2f && ny < 0.85f) || (ny < 0.35f && (nx > 0.45f && nx < 0.75f));
                            break;
                        case EnemyType.Bomber:
                            body = (nx < 0.9f && ny > 0.15f && ny < 0.85f);
                            break;
                        case EnemyType.Elite:
                            body = (nx + Mathf.Abs(ny - 0.5f) * 1.3f) < 0.95f;
                            break;
                    }

                    tex.SetPixel(x, y, body ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateBossSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.5f));
                    float ny = (float)y / h;

                    bool mainBody = (nx < 0.85f && ny > 0.2f && ny < 0.8f);
                    bool wings = (nx < 0.98f && ny > 0.35f && ny < 0.65f);
                    bool centralCore = (nx < 0.2f && ny > 0.4f && ny < 0.6f);

                    if (mainBody || wings)
                    {
                        tex.SetPixel(x, y, centralCore ? Color.cyan : Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateBadgeSprite(int w, int h, string text, Color col)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = (x == 1 || x == w - 2 || y == 1 || y == h - 2);
                    bool bg = (x > 1 && x < w - 2 && y > 1 && y < h - 2);

                    if (border)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else if (bg)
                    {
                        tex.SetPixel(x, y, new Color(col.r * 0.3f, col.g * 0.3f, col.b * 0.3f, 0.9f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}



