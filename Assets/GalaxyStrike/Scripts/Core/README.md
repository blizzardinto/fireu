# Core 纯逻辑核心层 · 架构说明与 AI 规范

> **所属命名空间**：`GalaxyStrike.Core`  
> **设计定位**：纯 C# 基础类型、物理碰撞数学、全局平衡参数与确定性随机数发生器。本层严禁依赖任何 Unity 场景对象与 `MonoBehaviour`。

---

## 模块组成与文件清单

| 文件 | 核心结构 / 类型 | 职责说明 |
| :--- | :--- | :--- |
| [`GameConfig.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Core/GameConfig.cs) | `GameConfig` 静态常量类 | 定义虚拟网格分辨率（$80 \times 24$）、世界单位换算比例、刷新步长、以及核心投影转换函数 `GridToWorld(Vector2)`。 |
| [`Types.cs`](file:///d:/Code/fireu/Assets/GalaxyStrike/Scripts/Core/Types.cs) | `Rect2D`<br>`Vector2Extensions`<br>`GameRandom`<br>`GameColors`<br>`GameDifficulty` | 封装无 GC 的轴对齐包围盒 (AABB) 相交检测算法、高效向量距离计算、跨平台确定性伪随机数发生器，以及标准街机调色板常数。 |

---

## 核心数学与坐标转换 (Coordinate Transformation)

* **逻辑网格坐标**：
  * 左上角原点 $(0, 0)$，横向范围 $[0, 80]$，纵向范围 $[0, 24]$。
  * 游戏主交火区限制在左侧 $74\%$，即 $X \in [0, 59.2]$。
* **Unity 世界空间坐标**：
  $$\text{world.x} = (\text{grid.x} - 40.0) \times 0.20f$$
  $$\text{world.y} = (12.0 - \text{grid.y}) \times 0.40f$$
* **碰撞检测 (AABB Intersection)**：
  通过 `Rect2D.Intersects(in Rect2D other)` 纯数值比较实现，不使用 Unity 2D 物理引擎碰撞体（Collider2D / Rigidbody2D），性能极高，毫秒级完成数百个弹幕判定。

---

## AI Agent 开发规范与注意事项

1. **绝对隔离**：禁止向本层代码注入任何 `UnityEngine.Object`、`GameObject`、`Transform` 等依赖。
2. **零堆分配**：`Rect2D` 为 `struct`，方法参数尽量使用 `in` 只读引用传递，避免拷贝开销。
3. **色彩管理**：全局统一通过 `GameColors` 引用预设色号（如 `GameColors.BrightRed`、`GameColors.BrightCyan`），保证街机霓虹色温一致性。
