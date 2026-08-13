# Contributing

Astrea 是一个企业级 Blazor UI 组件库（.NET 6.0），强调正确性、可访问性、可测试性。每个功能变更须可追溯到 `docs/SRS.md` 中的需求（REQ-F-xxx）。

## 本地检查（提交前）

```sh
# 构建（RCL + 演示 + 测试）
dotnet build Astrea.sln

# 测试（核心纯 C# 逻辑：TreeNode / TreeController / TreeSelection）
dotnet test Astrea.sln --no-build

# 代码风格
dotnet format Astrea.sln --verify-no-changes --no-restore

# SRS 一致性（需求编号/统计/附录）
python3 scripts/verify_srs_consistency.py

# 实机浏览器冒烟（演示 app）
dotnet run --project src/Astrea.Demo
```

## 分支

- `feature/<topic>`
- `fix/<topic>`
- `infra/<topic>`
- `docs/<topic>`

功能开发在 `feature/<topic>` 分支进行，完成后经 PR 合并回 `dev`（默认分支）。

## 提交信息格式（Conventional Commits）

```
<type>(<scope>): <subject>
```

- **type**（必填）：`feat` 新功能 / `fix` 缺陷修复 / `docs` 文档 / `test` 测试 / `refactor` 重构 / `style` 样式视觉 / `perf` 性能 / `ci` CI 与部署 / `chore` 杂项
- **scope**（可选）：`tree`（Tree 组件）、`core`（核心模型 TreeNode/TreeController/TreeSelection）、`srs`（需求文档）、`ci`（CI/CD）、`demo`（演示 app）、`test`（测试）、`docs`（文档）
- **subject**：中文简短描述，动词开头，≤ 72 字符

示例：

```
feat(tree): 实现 Tree 组件渲染层与 ARIA 键盘导航
fix(core): 修复拖拽校验未覆盖全部源节点
docs(srs): Tree 新增多选/拖拽/编辑/右键需求
ci: 增加 CI 质量门禁（build/test/format/srs）
```

## PR 标题

与提交信息格式同源：`<type>(<scope>): <subject>`。一个 PR 对应一个批次/关注点。

## PR 前检查

- [ ] 运行上方本地检查
- [ ] 填写 PR 模板相应节
- [ ] 引用相关 SRS 需求（REQ-F-xxx）
- [ ] 行为变更补充测试
- [ ] 不夹带无关清理
