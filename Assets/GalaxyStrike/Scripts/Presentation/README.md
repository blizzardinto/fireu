# Presentation 表现层与图形桥接 · 架构说明与 AI 规范

> **所属命名空间**：`GalaxyStrike.Presentation`  
> **设计定位**：连接纯 C# 游戏逻辑与 Unity 渲染管线。负责 2D Sprite 动态渲染、包围盒自适应缩放、SortingOrder 排序分层及全量对象池维护。

---

## 核心类与职责

| 类名 | 核心职责 |
| :--- | :--- |
| [`GalaxyStrikeRenderer.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Presentation/GalaxyStrikeRenderer.cs) | 统摄游戏全场景实体、弹道、道具、粒子与全屏双层冲击波渲染；统一管理各图层对象池，杜绝运行时内存分配。 |
| [`GalaxyStrikeVisualSize.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Presentation/GalaxyStrikeVisualSize.cs) | 纯数学静态类，根据 Sprite 的实际像素边界将其等比适配缩放到逻辑网格要求的物理世界单位包围盒内。 |

---

## 渲染管线分层与 SortingOrder 规划

为保证霓虹发光与弹幕遮挡关系的绝对清晰，各可视元素严格遵循以下层级：

| SortingOrder | 图层名称 | 对应视觉元素 | 材质与着色器规范 |
| :---: | :--- | :--- | :--- |
| **0** | `Stars_Layer` | 视差背景恒星粒子 | `Sprites/Default`，根据恒星光谱类别调制 Alpha |
| **4** | `PowerUps_Layer` | 战术掉落物资芯片 | 带微浮呼吸光晕 |
| **5 ~ 8** | `Enemies_Layer` | 敌机集群 (杂兵 5，Boss 8) | 矢量切角飞船，受创时呈现高亮白闪 |
| **6** | `EnemyBullets` | 敌方弹道 (红激光/能量球) | 鲜艳警示赤红与炽紫 |
| **7** | `PlayerBullets` | 我方弹幕 (穿透蓝光/追踪飞弹) | 高能电浆青蓝与亮金 |
| **9 ~ 11** | `PlayerShip` | 战机机体 (10)、尾焰 (9)、护盾罩 (11) | 护盾随受击波动自适应半透明扩散 |
| **12 ~ 14** | `Particles_Layer` | 火星 (12)、碎屑 (13)、微型爆闪 (14) | 稀疏高亮金橙色调 |
| **15 ~ 16** | `Shockwaves_Layer` | 全屏双层冲击波 (15)、电弧分叉 (16) | 炽红与金白双层能量环，幂次快速消隐 |

---

## AI Agent 开发规范与注意事项

1. **绝对对象池保护**：
   * 严禁在 `RenderFrame` 及其下属方法中调用 `Instantiate()` 或 `Destroy()`。
   * 对象池扩容通过 `EnsurePoolSize` 预留缓冲，闲置对象通过 `SetActive(false)` 回收。
2. **材质安全性容错**：
   * 着色器初始化时按 `Sprites/Default` -> `Universal Render Pipeline/2D/Sprite-Unlit` -> `Unlit/Color` 自动优雅降级，确保在任何管线配置下永不粉红紫报错。
3. **尺寸适配规则**：
   * 所有实体必须通过 `GalaxyStrikeVisualSize.Fit(sprite, targetSize)` 换算 scale，严禁硬编码 `transform.localScale = Vector3.one` 导致非等比变形。
