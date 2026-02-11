# P3 Runbook（桌面集成）

## 目标

验证 P3 阶段桌面集成最小闭环：

1. 透明置顶窗口初始化
2. 点击穿透可切换
3. 图标坐标桥接到碰撞检测

## 手工验证步骤

1. 初始化窗口宿主并调用 `OverlayWindowController.Initialize()`。
2. 切换 `ToggleClickThrough()` 两次，确认状态往返。
3. 传入图标列表调用 `InteractionOrchestrator.OnPetMovedNearIcons()`，命中时应返回气泡文案。

## 失败降级策略

- 窗口创建失败：降级为无透明样式窗口（保留交互）。
- 图标采集失败：返回空列表并仅保留鼠标/点击交互。
- 点击穿透异常：调用 `ForceInteractiveMode()` 强制恢复可交互状态。

4. 周期调用 `DesktopIntegrationRuntime.TryTick(...)`，观察 `IntegrationDiagnostics` 日志是否输出 tick 成功/失败信息。

5. 使用 `DesktopIntegrationRuntime.Tick(...)` 查看 `RuntimeTickResult`（Executed/Succeeded/PollingHz）确认调度退避与恢复。

6. 检查 `RuntimeTickResult.Advisory`：无图标时应为 `UseCachedIcons`，连续失败降频后应出现 `EnterRecoveryMode`。

7. 调用 `DesktopIntegrationRuntime.GetHealthSnapshot()`，确认 `ConsecutiveFailures`、`IsInRecoveryMode`、Info/Warn 计数符合预期。

8. 调用 `DesktopRuntimeOperator.Update(...)`，验证 `RuntimeUserNotice` 在 `UseCachedIcons`/恢复模式场景下会给出非阻塞提示。

9. 连续两次同类型异常时，`DesktopRuntimeOperator.Update(...)` 应保持 notice 但 `IsNoticeChanged=false`，避免 warning 日志刷屏。

10. 在每次 `DesktopRuntimeOperator.Update(...)` 后读取 `result.Progress.CompletionPercent` 输出当前实现完成度（例如 75.0%）。

11. 通过 `P3ProgressProbe` 注入 `DesktopRuntimeOperator`，确保完成度仅在图标源真实可用后提升，避免因临时缓存造成误判。

12. 若图标源波动，使用 `RetryingDesktopIconSource` 并检查 `DesktopIconSourceHealth`（失败次数/最后错误）以辅助排障。

13. 默认接线可通过 `DesktopIconProviderFactory.CreateDefault(...)` 创建（Win32ExplorerIconSource + RetryingDesktopIconSource），当前 Win32 source 会先发现 Explorer ListView 并读取 item count，再基于 Desktop 目录项生成网格坐标。
14. 执行 `pwsh ./tests/p3_smoke_test.ps1`，确认四项验证脚本串行通过并输出 `Summary: all checks passed`。

