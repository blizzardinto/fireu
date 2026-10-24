using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GalaxyStrike.Core;
using GalaxyStrike.Entities;
using GalaxyStrike.Presentation;
using Object = UnityEngine.Object;

namespace GalaxyStrike.Editor
{
    public static class GalaxyStrikeArtSetup
    {
        const string Root = "Assets/GalaxyStrike/Resources/GalaxyStrikeArt/";
        static readonly string[] Names = {
            "PlayerShip", "PlayerShip_Lv2", "PlayerShip_Lv3", "PlayerShip_Lv4", "PlayerShip_Lv5",
            "Enemy_Scout", "Enemy_Gunship", "Enemy_Bomber", "Enemy_EliteCruiser", "Enemy_BossMothership",
            "Bullet_PlayerPlasma", "Bullet_PlayerLaser", "Bullet_PlayerMissile", "Bullet_EnemyOrb", "Bullet_EnemyLaser",
            "PowerUp_Firepower", "PowerUp_ShipExp", "PowerUp_Shield", "PowerUp_Bomb", "PowerUp_Score",
            "FX_Shield", "FX_Star", "FX_EMP", "FX_Thruster", "FX_Explosion", "FX_Debris"
        };

        [MenuItem("Galaxy Strike/Apply Cartoon Art & Fit Prefabs")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root + "Sprites");
            Slice("CartoonEntities", 5, 4, 0);
            Slice("CartoonEffects", 3, 2, 20);
            GalaxyStrikePrefabGenerator.EnsureAllPrefabs();
            var report = new List<string>();
            for (int i = 0; i < Names.Length; i++)
            {
                Vector2 target = Target(i);
                SavePrefab(Names[i], Names[i], target, report);
            }
            SavePrefab("PowerUp_Item", "PowerUp_Firepower", GalaxyStrikeVisualSize.Grid(3, 1), report);
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Temp/CartoonPrefabValidation.txt", report);
            Debug.Log("Cartoon art applied and size-validated: " + report.Count + " prefabs.");
        }

        static Vector2 Target(int i)
        {
            if (i < 5)
            {
                var p = new Player(Vector2.zero);
                for (int level = 0; level < i; level++) p.AddShipExp(p.ShipExpNext);
                return GalaxyStrikeVisualSize.Grid(p.Width, p.Height);
            }
            Enemy[] enemies = { new ScoutEnemy(Vector2.zero), new GunshipEnemy(Vector2.zero), new BomberEnemy(Vector2.zero), new EliteCruiser(Vector2.zero), new BossEnemy(Vector2.zero) };
            if (i < 10) return GalaxyStrikeVisualSize.Grid(enemies[i - 5].Width, enemies[i - 5].Height);
            BulletType[] bullets = { BulletType.Normal, BulletType.Laser, BulletType.Homing, BulletType.EnemyOrb, BulletType.EnemyFast };
            if (i < 15) return GalaxyStrikeVisualSize.Bullet(bullets[i - 10]);
            if (i < 20) return GalaxyStrikeVisualSize.Grid(3, 1);
            return Vector2.one;
        }

        static void Slice(string atlasName, int cols, int rows, int offset)
        {
            // Read the original PNG directly so import settings cannot rescale the atlas.
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            atlas.LoadImage(File.ReadAllBytes(Root + atlasName + ".png"));
            var pixels = atlas.GetPixels32();
            float[][] edges = {
                new float[] { 0, 205, 450, 706, 971, 1280 },
                new float[] { 0, 201, 450, 693, 943, 1280 },
                new float[] { 0, 250, 510, 746, 1010, 1280 },
                new float[] { 0, 256, 512, 768, 1024, 1280 }
            };
            float[] tops = { 0, 346, 638, 981, 1280 };
            for (int row = 0; row < rows; row++) for (int col = 0; col < cols; col++)
            {
                int x0 = cols == 5 ? Mathf.RoundToInt(edges[row][col] * atlas.width / 1280f) : col * atlas.width / cols;
                int x1 = cols == 5 ? Mathf.RoundToInt(edges[row][col + 1] * atlas.width / 1280f) : (col + 1) * atlas.width / cols;
                int y0 = cols == 5 ? atlas.height - Mathf.RoundToInt(tops[row + 1] * atlas.height / 1280f) : (rows - row - 1) * atlas.height / rows;
                int y1 = cols == 5 ? atlas.height - Mathf.RoundToInt(tops[row] * atlas.height / 1280f) : (rows - row) * atlas.height / rows;
                if (cols == 3 && row == 0 && col == 1) { x0 += 24; x1 -= 24; }
                int left = x1, right = x0, bottom = y1, top = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * atlas.width + x].a >= 24)
                    { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
                if (cols == 5)
                {
                    // Ignore detached fragments of neighbouring cells when trimming.
                    int cw = x1 - x0, ch = y1 - y0, best = 0;
                    var seen = new bool[cw * ch]; var queue = new int[cw * ch];
                    for (int sy = 0; sy < ch; sy++) for (int sx = 0; sx < cw; sx++)
                    {
                        int seed = sy * cw + sx;
                        if (seen[seed] || pixels[(y0 + sy) * atlas.width + x0 + sx].a < 24) continue;
                        int head = 0, tail = 1, l = sx, r = sx, b = sy, t = sy;
                        queue[0] = seed; seen[seed] = true;
                        while (head < tail)
                        {
                            int p = queue[head++], px = p % cw, py = p / cw;
                            l = Math.Min(l, px); r = Math.Max(r, px); b = Math.Min(b, py); t = Math.Max(t, py);
                            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = px + dx, ny = py + dy;
                                if (nx < 0 || ny < 0 || nx >= cw || ny >= ch) continue;
                                int n = ny * cw + nx;
                                if (seen[n] || pixels[(y0 + ny) * atlas.width + x0 + nx].a < 24) continue;
                                seen[n] = true; queue[tail++] = n;
                            }
                        }
                        if (tail > best) { best = tail; left = x0 + l; right = x0 + r; bottom = y0 + b; top = y0 + t; }
                    }
                }
                if (left > right) throw new Exception("Empty atlas cell " + row + "," + col);
                left = Math.Max(x0, left - 2); right = Math.Min(x1 - 1, right + 2);
                bottom = Math.Max(y0, bottom - 2); top = Math.Min(y1 - 1, top + 2);
                var output = new Texture2D(right - left + 1, top - bottom + 1, TextureFormat.RGBA32, false);
                output.SetPixels(atlas.GetPixels(left, bottom, output.width, output.height)); output.Apply();
                string path = Root + "Sprites/" + Names[offset + row * cols + col] + ".png";
                File.WriteAllBytes(path, output.EncodeToPNG()); Object.DestroyImmediate(output);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100; importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Object.DestroyImmediate(atlas);
        }

        static void SavePrefab(string name, string spriteName, Vector2 target, List<string> report)
        {
            string path = GalaxyStrikePrefabGenerator.PrefabsDir + "/" + name + ".prefab";
            bool exists = File.Exists(path);
            var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
            try
            {
                var sr = root.GetComponent<SpriteRenderer>() ?? root.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Sprites/" + spriteName + ".png");
                if (sr.sprite == null) throw new Exception("Missing sprite: " + spriteName);
                sr.color = Color.white;
                sr.sortingOrder = name.StartsWith("Player") ? 10 : name.StartsWith("Enemy") ? 5 : name.StartsWith("Power") ? 4 : 7;
                root.transform.localScale = GalaxyStrikeVisualSize.Fit(sr.sprite, target);
                if (name.StartsWith("PlayerShip")) SetupPlayer(root, sr.sprite);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Vector2 actual = Vector2.Scale(sr.sprite.bounds.size, root.transform.localScale);
                if (actual.x > target.x + 0.001f || actual.y > target.y + 0.001f) throw new Exception("Oversized prefab: " + name);
                report.Add(name + ": box=" + target.ToString("F3") + " actual=" + actual.ToString("F3") + " PASS");
            }
            finally { if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
        }

        static void SetupPlayer(GameObject root, Sprite body)
        {
            Vector2 size = body.bounds.size;
            foreach (var name in new[] { "ShieldBubble", "Thruster_L", "Thruster_R" })
            {
                var child = root.transform.Find(name);
                if (child == null) { child = new GameObject(name).transform; child.SetParent(root.transform, false); }
                var sr = child.GetComponent<SpriteRenderer>() ?? child.gameObject.AddComponent<SpriteRenderer>();
                bool shield = name == "ShieldBubble";
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Sprites/" + (shield ? "FX_Shield" : "FX_Thruster") + ".png");
                sr.color = new Color(1, 1, 1, shield ? 0.25f : 1f); sr.sortingOrder = shield ? 11 : 9;
                child.localScale = GalaxyStrikeVisualSize.Fit(sr.sprite, shield ? size * 1.35f : new Vector2(size.x * 0.18f, size.y * 0.35f));
                child.localPosition = shield ? Vector3.zero : new Vector3(size.x * (name.EndsWith("L") ? -0.26f : 0.26f), -size.y * 0.42f, 0);
            }
        }
    }
}

