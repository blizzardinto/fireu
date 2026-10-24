# Prefabs 游戏预制体资产 · 架构说明与 AI 规范

> **所属资源路径**：`Assets/GalaxyStrike/Prefabs/`  
> **设计定位**：存放通过 `GalaxyStrikePrefabGenerator.cs` 自动生成的战机、敌机、子弹与补给预制体，供 `GalaxyStrikeRenderer.cs` 的对象池加载。

---

## 预制体清单

* **玩家主力战机**：`PlayerShip.prefab`
* **敌机集群模型**：
  * `Enemy_Scout.prefab` (蜂群侦察机)
  * `Enemy_Gunship.prefab` (定高炮艇)
  * `Enemy_Bomber.prefab` (重装轰炸机)
  * `Enemy_EliteCruiser.prefab` (精英巡洋舰)
  * `Enemy_BossMothership.prefab` (灭世母舰)
* **弹药预制体**：
  * `Bullet_PlayerPlasma.prefab` (等离子弹)
  * `Bullet_PlayerLaser.prefab` (激光)
  * `Bullet_PlayerMissile.prefab` (追踪飞弹)
  * `Bullet_EnemyOrb.prefab` (能量球)
  * `Bullet_EnemyLaser.prefab` (敌方激光)
* **战术掉落物资**：
  * `PowerUp_Item.prefab` (通用物资载体)

---

## 自动化重新生成指令
在 Unity 编辑器中点击菜单栏：  
`GalaxyStrike` -> `Generate All Prefabs`，即可自动检测最新 Sprite 切片并批量重塑所有预制体。
