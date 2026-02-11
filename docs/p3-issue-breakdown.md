# P3 任务拆分（Issue 级）

> 阶段目标：完成 Windows 桌面集成接入（透明窗、点击穿透、图标坐标桥接）。

## 当前执行进展（持续更新）

- [x] ISSUE-01：已完成状态化窗口宿主 + 透明/置顶样式服务抽象。
- [x] ISSUE-02：已完成点击穿透切换控制与强制恢复交互接口。
- [ ] ISSUE-03：图标坐标采集 Provider（已接入 source/cached 回退 + source health/retry；Win32 source 已支持 Explorer ListView 句柄发现 + item count 获取 + Desktop 项扫描网格映射，下一步补齐 per-icon 原生坐标枚举）。
- [x] ISSUE-04：交互编排器已支持 icon list 路由碰撞。
- [x] ISSUE-06：已加入失败降频 + 成功后恢复首选频率调度器，并可输出恢复状态快照。
- [x] ISSUE-07：已加入集成诊断日志、运行时 advisory 输出、非阻塞用户提示与提示去重日志策略。
- [x] ISSUE-08：已提供可执行的 P3 smoke 脚本（串行执行 4 项校验并汇总失败）。

当前代码内可通过 `P3ProgressEstimator` 输出完成度百分比，完成度通过 probe 绑定真实能力状态。

## 看板建议

- Epic: `P3 Desktop Integration`
- Milestone: `P3`
- Sprint: `7 working days`

---

## ISSUE-01：Windows 透明置顶窗口基础能力

- **类型**：Feature
- **优先级**：P0
- **预计工时**：1 天
- **依赖**：无
- **目标**：实现桌面无边框透明窗口，并支持置顶开关。
- **交付物**：
  - `src/Desktop/Win32/WindowStyleService.cs`
  - `src/Desktop/Win32/OverlayWindowHost.cs`
- **验收标准**：
  1. 启动后显示透明窗口。
  2. 置顶开关可切换且状态正确。
  3. 不影响现有行为模块运行。
- **风险回滚**：
  - 若分层透明在当前环境异常，回退到普通无边框窗口（保留置顶能力）。

---

## ISSUE-02：点击穿透模式与编辑模式切换

- **类型**：Feature
- **优先级**：P0
- **预计工时**：1 天
- **依赖**：ISSUE-01
- **目标**：实现点击穿透与可交互模式的双向切换。
- **交付物**：
  - `src/Desktop/Win32/ClickThroughService.cs`
  - `src/Desktop/OverlayWindowController.cs`（增强）
- **验收标准**：
  1. 穿透模式下鼠标可操作桌面图标。
  2. 可交互模式下可拖拽宠物锚点。
  3. 模式切换延迟 < 100ms。
- **风险回滚**：
  - 增加“强制恢复交互模式”快捷键（例如 Ctrl+Shift+P）。

---

## ISSUE-03：桌面图标坐标采集 Provider

- **类型**：Feature
- **优先级**：P0
- **预计工时**：1.5 天
- **依赖**：ISSUE-01
- **目标**：读取桌面图标名称与屏幕坐标，提供标准数据接口。
- **交付物**：
  - `src/Desktop/Icons/DesktopIconProvider.cs`
  - `src/Desktop/Icons/DesktopIconInfo.cs`
- **验收标准**：
  1. 能读取图标数量、名称、矩形坐标。
  2. Provider 可 2~10Hz 轮询输出。
  3. Explorer 重启后可自动恢复采集。
- **风险回滚**：
  - 坐标采集失败时返回空集并打 warning，不影响主循环。

---

## ISSUE-04：图标坐标与碰撞桥接到交互编排器

- **类型**：Feature
- **优先级**：P0
- **预计工时**：1 天
- **依赖**：ISSUE-03
- **目标**：将图标坐标接入碰撞判断与气泡反馈。
- **交付物**：
  - `src/App/InteractionOrchestrator.cs`（增强）
  - `src/Desktop/IconCollisionService.cs`（增强）
- **验收标准**：
  1. 宠物位置进入图标矩形时触发 `OnIconCollision`。
  2. 气泡文案显示图标名。
  3. 不修改真实图标位置（只视觉反馈）。
- **风险回滚**：
  - 坐标异常时降级为仅鼠标/点击交互。

---

## ISSUE-05：多显示器与 DPI 坐标转换

- **类型**：Feature
- **优先级**：P1
- **预计工时**：0.5 天
- **依赖**：ISSUE-03, ISSUE-04
- **目标**：统一桌面图标坐标与渲染坐标系，支持 DPI 缩放。
- **交付物**：
  - `src/Desktop/Geometry/ScreenTransformService.cs`
- **验收标准**：
  1. 100%/125%/150% 缩放下碰撞区域可对齐。
  2. 多显示器主副屏切换后坐标仍正确。
- **风险回滚**：
  - 遇到异常环境时仅启用主屏碰撞。

---

## ISSUE-06：性能与稳定性（节流、超时、重试）

- **类型**：Tech Debt / Reliability
- **优先级**：P1
- **预计工时**：0.5 天
- **依赖**：ISSUE-04
- **目标**：降低 CPU 抖动，避免桥接模块阻塞主循环。
- **交付物**：
  - `src/Desktop/Runtime/IntegrationScheduler.cs`
- **验收标准**：
  1. 轮询任务超时可中断并重试。
  2. CPU 占用保持在预期区间（空闲场景可控）。
  3. 错误日志可定位来源模块。
- **风险回滚**：
  - 将轮询频率降级到 2Hz，优先保证稳定。

---

## ISSUE-07：日志、诊断与故障提示

- **类型**：Feature
- **优先级**：P1
- **预计工时**：0.5 天
- **依赖**：ISSUE-03, ISSUE-04
- **目标**：提供可观测性，便于定位桌面桥接故障。
- **交付物**：
  - `src/Desktop/Diagnostics/IntegrationDiagnostics.cs`
  - `docs/p3-runbook.md`
- **验收标准**：
  1. 关键事件（窗口创建、穿透切换、图标采集）有结构化日志。
  2. 用户可看到“图标交互不可用”的非阻塞提示。
- **风险回滚**：
  - 日志系统异常时自动降级到 console 简版日志。

---

## ISSUE-08：P3 回归测试与验收脚本

- **类型**：Test
- **优先级**：P0
- **预计工时**：1 天
- **依赖**：ISSUE-01 ~ ISSUE-07
- **目标**：固化 P3 验收流程，确保后续 P4 接入不回归。
- **交付物**：
  - `tests/p3_acceptance_checklist.md`
  - `tests/p3_smoke_test.ps1`
- **验收标准**：
  1. 覆盖 AC-01 / AC-02 / AC-05 对应场景。
  2. 冒烟测试可一键执行并输出报告。
- **风险回滚**：
  - 自动化失败时保留人工 checklist + 录屏验收。

---

## 推荐执行顺序（DAG）

1. ISSUE-01
2. ISSUE-02 + ISSUE-03（并行）
3. ISSUE-04
4. ISSUE-05 + ISSUE-06 + ISSUE-07（并行）
5. ISSUE-08

---

## Definition of Done（P3）

- 透明置顶窗口可稳定运行。
- 点击穿透/交互模式可双向切换。
- 图标坐标可读取并驱动视觉碰撞反馈。
- 出现异常时可降级，不阻塞主流程。
- 验收脚本与 checklist 可复用到后续阶段。
