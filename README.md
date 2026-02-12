# Desktop 3D Cat Companion

面向 Windows 桌面的 3D 猫宠 Companion 项目（进行中）。

## 本轮交付（按“先需求、后规划、再实现”）

1. 完整需求文档（PRD）
2. 实施规划表（Roadmap）
3. 资产配置加载与校验
4. 行为状态机增强（输出状态 + 反馈动作）
5. 交互编排器（统一路由鼠标/点击/图标碰撞/idle）

## 文档

- `docs/prd-desktop-3d-cat-companion.md`：需求文档（FR/NFR/AC/风险）。
- `docs/implementation-roadmap.md`：阶段规划与本轮逐项实现表。
- `docs/desktop-3d-cat-companion-mvp.md`：总体架构草案。
- `pipeline/README.md`：3DGS 录入与重建流程。
- `docs/p3-issue-breakdown.md`：P3 issue 级执行拆分。
- `docs/p3-runbook.md`：P3 集成验证与故障降级手册。

## 代码结构

- `src/Asset`：资产配置模型、加载器、校验器。
- `src/Behavior`：人格配置、事件定义、状态机决策。
- `src/Desktop`：桌面窗口状态与图标视觉碰撞。
- `src/Desktop/Win32`：桌面透明窗/点击穿透服务抽象。
- `src/Desktop/Icons`：桌面图标坐标提供器抽象（含 `Win32ExplorerIconSource`、`RetryingDesktopIconSource` 与 source health）。
- `src/Desktop/Diagnostics`：桌面集成日志与诊断。
- `src/Desktop/Runtime`：轮询调度、降频策略与集成运行时（`DesktopIntegrationRuntime`、`DesktopRuntimeOperator`、`P3ProgressEstimator`、`P3ProgressProbe`）。
- `src/App`：交互编排器。
- `assets/pets`：可爱猫 / 高冷猫示例配置。
- `tests`：最小逻辑校验脚本。

## Deployment

- 完整部署方案：`docs/deployment-plan.md`
