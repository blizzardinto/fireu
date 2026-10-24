# Editor 编辑器工具与自动化测试 · 架构说明与 AI 规范

> **所属命名空间**：`GalaxyStrike.Editor`  
> **设计定位**：负责 Unity 编辑器下的自动化测试运行器、一键场景搭建、贴图自动切片导入及预制体批量生成。

---

## 核心脚本清单与功能说明

| 脚本文件 | 核心职责 | 菜单路径 / 触发时机 |
| :--- | :--- | :--- |
| [`GalaxyStrikeLogicTests.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Editor/GalaxyStrikeLogicTests.cs) | **12 项纯逻辑 Headless 自动化测试集**，秒级验证战机形态、数值公式、脱战回盾、弹幕穿透、AABB 碰撞、磁吸半径、Boss狂暴与波次编排。 | 菜单：`GalaxyStrike/Run Logic Tests`<br>并在每次代码编译重新加载后自动静默验证。 |
| [`GalaxyStrikeSceneSetup.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Editor/GalaxyStrikeSceneSetup.cs) | 场景一键就绪搭建器，自动配置主摄像机、挂载 `GalaxyStrikeGame`、`GalaxyStrikeRenderer`、`GalaxyStrikeHUD` 并完成各引用注入。 | 菜单：`GalaxyStrike/Setup New Scene` |
| [`GalaxyStrikePrefabGenerator.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Editor/GalaxyStrikePrefabGenerator.cs) | 自动化从 Sprite 生成对应的战机、敌机、子弹与补给预制体并保存至 `Assets/GalaxyStrike/Prefabs/`。 | 菜单：`GalaxyStrike/Generate All Prefabs` |
| [`GalaxyStrikeArtImporter.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Editor/GalaxyStrikeArtImporter.cs) | 自动化将图集切片导入为具有透明通道、点过滤（Point Filter）或双线性抗锯齿的 Sprite 资源。 | 菜单：`GalaxyStrike/Slice and Setup Art` |

---

## 自动化测试用例清单 (Logic Test Suite)

在 `GalaxyStrikeLogicTests.cs` 中维护了 12 个关键维度的测试断言：

1. **`TestPlayerInit`**：验证 Lv.1 基础属性（HP 100, 护盾 50, 命数 3, 核弹 2, 机型 Falcon-I）。
2. **`TestPlayerEvolution`**：验证 5 级演进经验增长公式（`exp * 2 + 30`）及属性跃迁。
3. **`TestShieldRegen`**：验证脱战 3 秒不回盾，满 3 秒后以 15点/秒平稳回复。
4. **`TestPlayerDamage`**：验证护盾优先吸收伤害，护盾击穿后真实扣除生命。
5. **`TestBulletPenetration`**：验证普通子弹命中即毁，而贯穿激光可连续击穿多体重叠敌机。
6. **`TestHomingBullet`**：验证自动追踪飞弹以角速度插值精准拐弯追踪目标。
7. **`TestBombClearing`**：验证核弹释放后敌弹瞬间清空、自身获得无敌保护且触发冲击波。
8. **`TestPowerUpMagnet`**：验证 16 码外正弦下落，16 码内触发 22格/秒超强磁吸。
9. **`TestEnemyBehaviors`**：验证 Scout、Gunship 巡航，以及 Boss 三阶段血量演化判定。
10. **`TestWaveManager`**：验证波次出兵状态机流转与母舰登场时机。
11. **`TestCollisions`**：验证 `Rect2D.Intersects` 在边缘相切与分离状态下的数学准确性。
12. **`TestGameVictoryAndLoss`**：验证 Boss 击坠胜利与战机命数归零失败逻辑。

---

## AI Agent 开发规范与注意事项

1. **断言必须严格通过**：
   * 任何代码变更后，请必须检查 `D:\Code\fireu\Logs\Editor.log`，必须保证 `12 / 12 测试全部通过`。
2. **修改玩法逻辑需同步更新测试**：
   * 若调整了升级经验公式或初始数值，必须在测试类对应的断言中同步更新基准值，严禁使测试处于失败状态。
