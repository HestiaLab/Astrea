# Astrea Roadmap 与延期组件

> 本文件记录当前版本之外的能力规划。需求的权威状态以 [docs/SRS.md](SRS.md) 附录 B 为准；本文件只解释范围与推进方式。

## 当前交付范围（SRS v0.9.0）

| 组件 | SRS 章节 | 状态 |
|---|---|---|
| Tree | §3.10 | Active（REQ-F-065 懒加载除外） |
| ResourceTable | §3.12 | Active |

§3.1 通用组件需求与 §4 中未列入附录 B 的非功能需求同样属于当前版本 Active 基线。

## Deferred 组件

以下组件的需求正文仍保留在 SRS 中，但状态为 Deferred，不属于当前版本验收范围。

| 组件 | SRS 章节 | 需求编号 | 数量 | 备注 |
|---|---|---|---:|---|
| Button | §3.2 | REQ-F-010 ~ 017、080 | 9 | REQ-F-080 为组件内 Deferred |
| Alert | §3.3 | REQ-F-018 ~ 023 | 6 | REQ-F-022 为组件内 Deferred |
| Divider | §3.4 | REQ-F-024 ~ 027 | 4 | |
| Input | §3.5 | REQ-F-028 ~ 036 | 9 | |
| Label | §3.6 | REQ-F-037 ~ 039 | 3 | |
| Modal | §3.7 | REQ-F-040 ~ 047、081 | 9 | REQ-F-081 为组件内 Deferred |
| Progress | §3.8 | REQ-F-048 ~ 052、076 | 6 | |
| Select | §3.9 | REQ-F-053 ~ 060、075、083 | 10 | REQ-F-058、059 为组件内 Deferred |
| Table | §3.11 | REQ-F-067 ~ 074、078、079、082、084、085 | 13 | REQ-F-073、082、084、085 为组件内 Deferred |

依赖 Table 的非功能需求 REQ-NF-003（代表性数据量性能）随 Table 一并 Deferred。

当前组件内的其他 Deferred 需求：REQ-F-065（Tree 懒加载）。

合计：70 条功能需求 + 1 条非功能需求 = 71 条 Deferred；27 条功能需求 + 11 条非功能需求 = 38 条 Active。

## 提升 Deferred 组件的条件

在实现任何一个 Deferred 组件之前，必须先完成：

1. 由仓库所有者批准范围变更；
2. 更新 SRS 的 §1.2.1、§1.4.3、§5.3、附录 B 与附录 C（版本号递增）；
3. 为该组件产出分组件实施计划与需求-测试矩阵条目；
4. 更新 README、示例与发布说明；
5. 通过完整门禁与浏览器/可访问性证据要求。

不得在未更新 SRS 的情况下零散实现 Deferred 组件，也不得把 Deferred 需求计入当前版本发布声明。
