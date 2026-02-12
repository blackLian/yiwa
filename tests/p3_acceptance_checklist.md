# P3 Acceptance Checklist

## AC 覆盖

- [ ] AC-01: 透明置顶窗口可见且可切换置顶
- [ ] AC-02: 点击穿透开关生效
- [ ] AC-05: 图标碰撞显示反馈文案，且不改变真实图标位置
- [ ] 异常降级路径可触发（图标采集失败/强制恢复交互）

## 冒烟执行

- [ ] 在 PowerShell 执行：`pwsh ./tests/p3_smoke_test.ps1`
- [ ] 输出包含 `Summary: all checks passed`
- [ ] 四项校验均执行：behavior / p3_progress / desktop_runtime / runtime_operator
