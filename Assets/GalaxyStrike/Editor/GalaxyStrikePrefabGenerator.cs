using System.IO;
using UnityEngine;
using UnityEditor;
using GalaxyStrike.Core;
using GalaxyStrike.Presentation;

namespace GalaxyStrike.Editor
{
    // ============================================================================
    // 【实体预制体模型全自动生成器 · GalaxyStrikePrefabGenerator.cs】
    // 自动在 Assets/GalaxyStrike/Prefabs/ 下生成所有实体 Prefab：
    // - 玩家战机（含护盾、尾焰层级）
    // - 5 类敌机（Scout, Gunship, Bomber, Elite, Boss）
    // - 5 类弹药（Plasma, Laser, Missile, Orb, EnemyLaser）
    // - 补给物资掉落物
    // 并自动将预制体引用绑定到 GalaxyStrike_GameMaster。
    // ============================================================================

    [InitializeOnLoad]
    public static class GalaxyStrikePrefabGenerator
    {
        public const string PrefabsDir = "Assets/GalaxyStrike/Prefabs";

        static GalaxyStrikePrefabGenerator()
        {
            EditorApplication.delayCall += EnsureAllPrefabs;
        }

        [MenuItem("Galaxy Strike/Generate All Game Prefabs (生成所有实体预制体模型)", false, 4)]
        public static void GeneratePrefabsMenu()
        {
            EnsureAllPrefabs();
            EditorUtility.DisplayDialog(
                "Galaxy Strike 预制体生成完成",
                "已成功在 Assets/GalaxyStrike/Prefabs/ 目录生成全部战机、敌机、子弹与补给物资的 Prefab 预制体！\n\n已整体调校至最佳大尺寸，场景中清晰可辨！",
                "确定"
            );
        }

        public static void EnsureAllPrefabs()
        {
            if (!Directory.Exists(PrefabsDir))
            {
                Directory.CreateDirectory(PrefabsDir);
                AssetDatabase.Refresh();
            }

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (spriteShader == null) spriteShader = Shader.Find("Unlit/Color");
            var mat = new Material(spriteShader);

            // 1. 玩家战机 Prefab
            CreatePlayerShipPrefab(mat);

            // 2. 五类敌机 Prefabs
            CreateEntityPrefab("Enemy_Scout", 5, new Color(1f, 0.35f, 0.35f), new Vector3(1.4f, 1.0f, 1f), mat);
            CreateEntityPrefab("Enemy_Gunship", 5, new Color(1f, 0.85f, 0.2f), new Vector3(2.0f, 1.4f, 1f), mat);
            CreateEntityPrefab("Enemy_Bomber", 5, new Color(1f, 0.4f, 0.85f), new Vector3(2.6f, 1.8f, 1f), mat);
            CreateEntityPrefab("Enemy_EliteCruiser", 6, new Color(0.75f, 0.35f, 1f), new Vector3(3.2f, 2.2f, 1f), mat);
            CreateEntityPrefab("Enemy_BossMothership", 8, new Color(0.2f, 1f, 1f), new Vector3(6.5f, 2.8f, 1f), mat);

            // 3. 弹药 Prefabs
            CreateEntityPrefab("Bullet_PlayerPlasma", 7, new Color(1f, 0.95f, 0.2f), new Vector3(0.45f, 1.1f, 1f), mat);
            CreateEntityPrefab("Bullet_PlayerLaser", 7, new Color(0.2f, 1f, 1f), new Vector3(0.65f, 2.0f, 1f), mat);
            CreateEntityPrefab("Bullet_PlayerMissile", 7, new Color(1f, 0.5f, 0.1f), new Vector3(0.9f, 0.9f, 1f), mat);
            CreateEntityPrefab("Bullet_EnemyOrb", 6, new Color(1f, 0.25f, 0.25f), new Vector3(0.85f, 0.85f, 1f), mat);
            CreateEntityPrefab("Bullet_EnemyLaser", 6, new Color(1f, 0.35f, 0.35f), new Vector3(0.45f, 1.1f, 1f), mat);

            // 4. 补给物资 Prefab
            CreateEntityPrefab("PowerUp_Item", 4, Color.white, new Vector3(1.4f, 1.4f, 1f), mat);

            AssetDatabase.Refresh();

            // 5. 绑定预制体引用到 GalaxyStrike_GameMaster
            BindPrefabsToGameMaster();
        }

        private static void CreatePlayerShipPrefab(Material mat)
        {
            string path = $"{PrefabsDir}/PlayerShip.prefab";
            if (File.Exists(path)) return;

            var rootGo = new GameObject("PlayerShip");
            rootGo.transform.localScale = new Vector3(1.5f, 1.2f, 1f);
            var sr = rootGo.AddComponent<SpriteRenderer>();
            sr.material = mat;
            sr.color = GameColors.BrightCyan;
            sr.sortingOrder = 10;

            // 护盾子对象
            var shieldGo = new GameObject("ShieldBubble");
            shieldGo.transform.SetParent(rootGo.transform, false);
            shieldGo.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            var shieldSr = shieldGo.AddComponent<SpriteRenderer>();
            shieldSr.material = mat;
            shieldSr.color = new Color(0.2f, 0.9f, 1f, 0.45f);
            shieldSr.sortingOrder = 11;

            // 尾焰喷射口
            var tLeft = new GameObject("Thruster_L");
            tLeft.transform.SetParent(rootGo.transform, false);
            tLeft.transform.localPosition = new Vector3(-0.35f, -0.6f, 0f);
            tLeft.transform.localScale = new Vector3(0.25f, 0.8f, 1f);
            var tLeftSr = tLeft.AddComponent<SpriteRenderer>();
            tLeftSr.material = mat;
            tLeftSr.color = GameColors.BrightBlue;
            tLeftSr.sortingOrder = 9;

            var tRight = new GameObject("Thruster_R");
            tRight.transform.SetParent(rootGo.transform, false);
            tRight.transform.localPosition = new Vector3(0.35f, -0.6f, 0f);
            tRight.transform.localScale = new Vector3(0.25f, 0.8f, 1f);
            var tRightSr = tRight.AddComponent<SpriteRenderer>();
            tRightSr.material = mat;
            tRightSr.color = GameColors.BrightBlue;
            tRightSr.sortingOrder = 9;

            PrefabUtility.SaveAsPrefabAsset(rootGo, path);
            Object.DestroyImmediate(rootGo);
            Debug.Log($"<color=#00FFCC><b>[Galaxy Strike]</b> 已生成玩家战机预制体：{path}</color>");
        }

        private static void CreateEntityPrefab(string name, int sortingOrder, Color col, Vector3 defaultScale, Material mat)
        {
            string path = $"{PrefabsDir}/{name}.prefab";
            if (File.Exists(path)) return;

            var go = new GameObject(name);
            go.transform.localScale = defaultScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.material = mat;
            sr.color = col;
            sr.sortingOrder = sortingOrder;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#00FFCC><b>[Galaxy Strike]</b> 已生成实体预制体：{path}</color>");
        }

        private static void BindPrefabsToGameMaster()
        {
            string masterPrefabPath = $"{PrefabsDir}/GalaxyStrike_GameMaster.prefab";
            if (!File.Exists(masterPrefabPath)) return;

            var masterGo = PrefabUtility.LoadPrefabContents(masterPrefabPath);
            if (masterGo == null) return;

            var renderer = masterGo.GetComponent<GalaxyStrikeRenderer>();
            if (renderer != null)
            {
                var so = new SerializedObject(renderer);
                AssignProperty(so, "m_playerShipPrefab", $"{PrefabsDir}/PlayerShip.prefab");
                AssignProperty(so, "m_enemyScoutPrefab", $"{PrefabsDir}/Enemy_Scout.prefab");
                AssignProperty(so, "m_enemyGunshipPrefab", $"{PrefabsDir}/Enemy_Gunship.prefab");
                AssignProperty(so, "m_enemyBomberPrefab", $"{PrefabsDir}/Enemy_Bomber.prefab");
                AssignProperty(so, "m_enemyElitePrefab", $"{PrefabsDir}/Enemy_EliteCruiser.prefab");
                AssignProperty(so, "m_enemyBossPrefab", $"{PrefabsDir}/Enemy_BossMothership.prefab");
                AssignProperty(so, "m_bulletPlayerPlasmaPrefab", $"{PrefabsDir}/Bullet_PlayerPlasma.prefab");
                AssignProperty(so, "m_bulletPlayerLaserPrefab", $"{PrefabsDir}/Bullet_PlayerLaser.prefab");
                AssignProperty(so, "m_bulletPlayerMissilePrefab", $"{PrefabsDir}/Bullet_PlayerMissile.prefab");
                AssignProperty(so, "m_bulletEnemyOrbPrefab", $"{PrefabsDir}/Bullet_EnemyOrb.prefab");
                AssignProperty(so, "m_bulletEnemyLaserPrefab", $"{PrefabsDir}/Bullet_EnemyLaser.prefab");
                AssignProperty(so, "m_powerUpItemPrefab", $"{PrefabsDir}/PowerUp_Item.prefab");

                var pShip = so.FindProperty("shipScaleMultiplier");
                if (pShip != null) pShip.floatValue = 1f;

                var pBullet = so.FindProperty("bulletScaleMultiplier");
                if (pBullet != null) pBullet.floatValue = 1f;

                var pPower = so.FindProperty("powerUpScaleMultiplier");
                if (pPower != null) pPower.floatValue = 1f;

                so.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(masterGo, masterPrefabPath);
            PrefabUtility.UnloadPrefabContents(masterGo);
        }

        private static void AssignProperty(SerializedObject so, string propName, string assetPath)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null)
                {
                    prop.objectReferenceValue = asset;
                }
            }
        }
    }
}

