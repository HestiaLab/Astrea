# 软件需求规范（SRS）

| 项目 | 内容 |
|------|------|
| 项目名称 | Astrea |
| 版本 | 0.1.0 |
| 状态 | 草案 |
| 日期 | 2026-08-01 |
| 作者 | wubing7755 |
| 适用范围 | Blazor 组件库（net10.0） |

---

# 1 引言

## 1.1 目的

本文档定义 Astrea——一个面向 .NET 10 / Blazor 的 UI 组件库——的功能需求与非功能需求。本文档是后续概要设计、详细设计与代码开发的权威依据。

## 1.2 范围

### 1.2.1 范围内

Astrea 提供可复用的 UI 组件，覆盖基础表单、反馈、导航与数据展示场景。组件清单如下：

| 分类 | 组件 |
|------|------|
| Basic | Button、Alert、Divider、Label |
| Form | Input、Select |
| Feedback | Modal（对话框） |
| Data Display | Table、Tree、Progress |

### 1.2.2 范围外

以下内容**不属于** Astrea 组件库范围，任何需求不得引入：

| 领域 | 说明 |
|------|------|
| 文件上传/下载 | 分块上传、断点续传、文件预览等不纳入组件库 |
| 翻译服务 | 调用外部翻译 API（如 Google Translate）不纳入 |
| 数学运算 | 矩阵、几何变换等数学工具不纳入 |
| 应用级服务 | 撤销/重做服务、事件总线、跨组件通信框架不纳入 |
| 应用级布局 | 可停靠布局、分栏面板等应用级布局能力不纳入组件库 |

## 1.3 术语

### 1.3.1 术语定义

| English | 中文 | 说明 |
|---------|------|------|
| Component | 组件 | Astrea 提供的可复用 UI 单元 |
| Variant | 变体 | 组件的视觉风格（如 Primary、Danger） |
| Size | 尺寸 | 组件的尺寸级别（Small、Medium、Large） |
| IsDisabled | 禁用 | 组件不可交互但仍可见的状态 |
| EventCallback | 事件回调 | Blazor 组件向使用方通知事件的标准机制 |
| AdditionalAttributes | 附加属性 | 未被组件识别的属性透传给根元素 |
| CSS Variable | CSS 变量 | 用于主题定制的自定义属性（--ast-*） |
| Acceptance Criteria | 验收标准 | 需求的可验证行为准则 |

### 1.3.2 角色定义

| English | 中文 | 说明 |
|---------|------|------|
| Application Developer | 应用开发者 | 使用 Astrea 组件的开发者，负责配置参数、绑定事件与模板 |
| End User | 终端用户 | 使用构建后应用的最终用户，执行点击、输入等交互 |
| System | 系统 | Astrea 组件自身，负责渲染、状态管理与行为保证 |

## 1.4 文档约定

### 1.4.1 需求编号规则

每条功能需求使用唯一编号 `REQ-F-XXX`，非功能需求使用 `REQ-NF-XXX`：

- **REQ**: Requirement（需求）
- **F**: Functional（功能需求）
- **NF**: Non-Functional（非功能需求）
- **XXX**: 三位数字顺序号（F: 001~074；NF: 001~008）

### 1.4.2 优先级定义

| 优先级 | 符号 | 含义 |
|:---:|:---:|------|
| P0 | 🔴 | 必须实现——核心功能，缺失则系统不可用 |
| P1 | 🟡 | 应当实现——重要功能，缺失则显著降低可用性 |
| P2 | 🟢 | 可以实现——增强功能，提升用户体验 |

### 1.4.3 需求格式

每条需求包含以下字段，并使用代码围栏包裹：

- **编号与优先级**: `REQ-F-XXX` + 优先级符号（🔴🟡🟢）+ `[Actor: 角色]`
- **Title**: 需求标题
- **Description**: 需求描述
- **Acceptance Criteria**: 使用 Given/When/Then 格式的验收标准，每行一个步骤

---

# 2 总体描述

## 2.1 产品定位

Astrea 是一个企业级 Blazor UI 组件库，强调**正确性、可访问性与可测试性**。组件命名源自希腊神话中的星辰女神 Astraea（阿斯特蕾亚），寓意组件如同夜空中的星辰，为界面提供清晰、可靠、一致的视觉元素。

## 2.2 设计原则

以下原则源自前身项目 AWComponent 的经验教训，是需求评审的判定依据：

1. **行为可预测**：组件遵循 Blazor 标准渲染与事件语义，不引入"优化渲染"等自定义行为。
2. **职责单一**：组件库只承载 UI 组件职责，不混入应用级服务。
3. **可访问性优先**：每个组件必须提供正确的语义角色与键盘交互。
4. **可验证**：每个组件的关键行为必须有自动化测试覆盖。
5. **文档真实**：文档描述的每项能力必须真实存在，禁止"声称已实现但实际缺失"。

## 2.3 约束与假设

| 类型 | 内容 |
|------|------|
| 技术约束 | 目标框架 net10.0；运行时依赖仅 Microsoft.AspNetCore.Components.Web |
| 浏览器约束 | 支持现代浏览器最近两个主版本（Chrome、Edge、Firefox、Safari） |
| 假设 | 使用方项目已配置 Blazor 所需的基础设施 |

---

# 3 功能需求

## 3.1 通用组件需求

> 本节需求适用于所有 Astrea 组件，各组件需求隐含继承本节约束。

```
REQ-F-001 🔴 [Actor: Application Developer]

Title
组件基类与属性透传

Description
所有组件继承统一的基类（AstreaComponentBase），支持 Class（CSS 类）、Style（内联样式）、IsDisabled（禁用）参数；未识别的属性通过 AdditionalAttributes 透传到根元素。

Source
组件库基础能力定义

Acceptance Criteria

AC1：Class 参数合并

Given 使用方传入 Class="my-class"
When 组件渲染
Then 根元素 class 包含组件基类与 my-class

AC2：附加属性透传

Given 使用方传入 id="btn-save" 与 data-test="42"
When 组件渲染
Then 根元素包含 id 与 data-test 属性

AC3：Style 参数

Given 使用方传入 Style="margin: 8px"
When 组件渲染
Then 根元素内联样式包含 margin: 8px
```

```
REQ-F-002 🔴 [Actor: Application Developer]

Title
禁用状态语义

Description
组件禁用时不可交互但仍可见。禁用必须通过原生机制表达（如 disabled 属性），不得通过隐藏元素实现。

Source
经验教训：AWComponent 禁用实现为隐藏元素

Acceptance Criteria

AC1：禁用可见

Given 组件 IsDisabled="true"
When 组件渲染
Then 组件仍渲染在页面中且可见

AC2：禁用不可交互

Given 组件 IsDisabled="true"
When 终端用户触发交互（点击、输入等）
Then 交互不生效，事件回调不被触发

AC3：禁用状态表达

Given 组件 IsDisabled="true"
When 组件渲染
Then 根元素带原生禁用属性，且带可感知的禁用视觉反馈
```

```
REQ-F-003 🔴 [Actor: System]

Title
事件处理与渲染语义

Description
组件的事件处理遵循 Blazor 标准语义：事件回调执行后，组件状态变更必须反映到界面。禁止自定义跳过渲染的"优化"。

Source
经验教训：AWComponent 渲染优化致 UI 不更新

Acceptance Criteria

AC1：事件后重渲染

Given 事件回调修改了组件状态
When 事件处理完成
Then 界面反映最新状态
```

```
REQ-F-004 🔴 [Actor: Application Developer]

Title
主题定制

Description
组件的颜色、尺寸、圆角等视觉属性通过 CSS 变量（前缀 --ast-）定制，并提供合理的默认值。

Source
经验教训：AWComponent 内联硬编码样式无法主题化

Acceptance Criteria

AC1：CSS 变量覆盖

Given 使用方在根容器定义 --ast-primary: #ff0000
When 组件渲染
Then 组件主色使用覆盖值

AC2：默认值

Given 使用方未定义任何 CSS 变量
When 组件渲染
Then 组件使用内置默认值且视觉完整
```

```
REQ-F-005 🔴 [Actor: System]

Title
样式作用域隔离

Description
组件样式使用 scoped CSS（.razor.css），不得使用会泄漏到全局的选择器。

Source
经验教训：AWComponent 全局 CSS 选择器泄漏

Acceptance Criteria

AC1：无全局泄漏

Given 使用方页面存在同名 class
When 组件渲染
Then 组件样式不覆盖使用方页面的无关元素
```

```
REQ-F-006 🔴 [Actor: System]

Title
语义角色

Description
每个组件根元素必须设置正确的语义 role 或使用原生语义元素（如 button、table）。

Source
经验教训：AWComponent A11Y 缺失；WCAG 2.1

Acceptance Criteria

AC1：语义正确

Given 任意 Astrea 组件
When 渲染完成
Then 根元素具有与组件语义匹配的角色或原生元素
```

```
REQ-F-007 🟡 [Actor: End User]

Title
键盘交互基础

Description
可交互组件必须支持标准键盘操作（Tab 聚焦、Enter/Space 激活等），并使用 focus-visible 提供可见的焦点指示。

Source
WCAG 2.1

Acceptance Criteria

AC1：焦点可见

Given 组件可交互
When 键盘 Tab 聚焦到组件
Then 显示清晰的焦点环
```

```
REQ-F-008 🔴 [Actor: Application Developer]

Title
组件测试要求

Description
每个组件必须提供 bUnit 自动化测试，覆盖渲染、核心交互与禁用状态。

Source
经验教训：AWComponent 测试覆盖不足

Acceptance Criteria

AC1：测试存在

Given 任意已实现的组件
Then 测试项目中存在对应测试文件且全部通过
```

```
REQ-F-009 🟡 [Actor: Application Developer]

Title
组件文档要求

Description
每个组件必须配套使用文档，包含：组件用途、参数列表、使用示例。文档描述的每项能力必须真实存在。

Source
经验教训：AWComponent 调试代码混入库、文档描述夸大

Acceptance Criteria

AC1：文档真实

Given 文档声称某组件支持某参数
When 审查文档与实现
Then 参数真实存在且行为与文档一致
```

## 3.2 Button（按钮）

```
REQ-F-010 🔴 [Actor: System]

Title
渲染为原生按钮

Description
Button 渲染为原生 button 元素，Class 合并规则遵循 REQ-F-001。

Source
组件需求定义

Acceptance Criteria

AC1：元素类型

Given 使用方渲染 Button 组件
When 组件渲染
Then 生成 button 元素
```

```
REQ-F-011 🔴 [Actor: End User]

Title
点击事件

Description
Button 提供 OnClick 事件回调（EventCallback），点击时触发。

Source
组件需求定义

Acceptance Criteria

AC1：点击触发

Given 使用方绑定 OnClick
When 终端用户点击按钮
Then OnClick 被触发一次

AC2：未绑定不报错

Given 使用方未绑定 OnClick
When 终端用户点击按钮
Then 无异常发生
```

```
REQ-F-012 🔴 [Actor: Application Developer]

Title
按钮类型

Description
Button 支持原生类型：Button（默认）、Submit、Reset。默认值必须为 Button，避免意外提交表单。

Source
组件需求定义

Acceptance Criteria

AC1：默认类型

Given 使用方未指定类型
When 组件渲染
Then type="button"

AC2：提交类型

Given 使用方指定 Type="Submit" 且按钮位于表单内
When 终端用户点击按钮
Then 表单提交
```

```
REQ-F-013 🔴 [Actor: Application Developer]

Title
视觉变体

Description
Button 支持 Variant 参数：Primary（默认）、Secondary、Text、Danger。各变体必须有可辨识的视觉差异。

Source
组件需求定义

Acceptance Criteria

AC1：变体类名

Given 使用方指定 Variant="Danger"
When 组件渲染
Then 根元素 class 包含对应的变体标识

AC2：变体差异

Given 各变体按钮同时渲染
When 视觉检查
Then 变体之间可辨识
```

```
REQ-F-014 🟡 [Actor: Application Developer]

Title
尺寸

Description
Button 支持 Size 参数：Small、Medium（默认）、Large。具体尺寸数值由详细设计确定。

Source
组件需求定义

Acceptance Criteria

AC1：尺寸类名

Given 使用方指定 Size="Large"
When 组件渲染
Then 根元素 class 包含对应的尺寸标识
```

```
REQ-F-015 🔴 [Actor: Application Developer]

Title
禁用

Description
Button 禁用时不可点击，遵循 REQ-F-002 语义。

Source
REQ-F-002

Acceptance Criteria

AC1：禁用属性

Given IsDisabled="true"
When 组件渲染
Then button 带 disabled 属性
And 点击不触发 OnClick
```

```
REQ-F-016 🟡 [Actor: Application Developer]

Title
图标支持

Description
Button 支持前置图标（Icon 参数，接受图标字体类名）。

Source
组件需求定义

Acceptance Criteria

AC1：图标渲染

Given 使用方指定 Icon="fa-solid fa-save"
When 组件渲染
Then 图标元素渲染在文本之前
```

```
REQ-F-017 🔴 [Actor: End User]

Title
可访问性

Description
Button 必须支持键盘激活（Enter/Space）、focus-visible 焦点环、禁用时 aria-disabled。

Source
REQ-F-006、REQ-F-007

Acceptance Criteria

AC1：键盘激活

Given 焦点在按钮上
When 按下 Enter 或 Space
Then OnClick 触发

AC2：禁用状态

Given IsDisabled="true"
When 组件渲染
Then 同时带 disabled 与 aria-disabled 属性
```

## 3.3 Alert（警告提示）

```
REQ-F-018 🔴 [Actor: Application Developer]

Title
渲染与类型

Description
Alert 渲染为区块元素，支持类型：Info、Success、Warning、Error。各类型有可辨识的视觉差异。

Source
组件需求定义

Acceptance Criteria

AC1：类型渲染

Given 使用方指定 Type="Error"
When 组件渲染
Then 根元素 class 包含对应类型标识
And 视觉上可辨识
```

```
REQ-F-019 🔴 [Actor: Application Developer]

Title
内容

Description
Alert 支持通过 ChildContent 渲染任意内容，支持标题与正文（标题可选）。

Source
组件需求定义

Acceptance Criteria

AC1：内容渲染

Given 使用方传入 ChildContent
When 组件渲染
Then 内容完整渲染在 Alert 内部
```

```
REQ-F-020 🔴 [Actor: End User]

Title
关闭行为

Description
Alert 提供显式的关闭按钮。仅点击关闭按钮时触发 OnClose，点击 Alert 其他区域不得触发。

Source
经验教训：AWComponent Alert 整区点击关闭

Acceptance Criteria

AC1：关闭按钮

Given Alert 可见且绑定 OnClose
When 终端用户点击关闭按钮
Then OnClose 触发

AC2：区域点击不关闭

Given Alert 可见且绑定 OnClose
When 终端用户点击 Alert 非按钮区域
Then OnClose 不触发
```

```
REQ-F-021 🔴 [Actor: Application Developer]

Title
可见性控制

Description
Alert 通过 IsVisible 参数控制显示，隐藏时不渲染。

Source
组件需求定义

Acceptance Criteria

AC1：隐藏

Given IsVisible="false"
When 组件渲染
Then Alert 不渲染
```

```
REQ-F-022 🟢 [Actor: System]

Title
自动关闭

Description
Alert 支持 AutoClose 与超时配置，达到超时后自动触发 OnClose。具体超时数值由详细设计确定。

Source
组件需求定义（P2 待定）

Acceptance Criteria

AC1：自动关闭

Given 使用方启用 AutoClose
When 显示时长超过配置超时
Then 自动触发 OnClose
```

```
REQ-F-023 🔴 [Actor: End User]

Title
可访问性

Description
Alert 必须设置 role="alert"，关闭按钮可键盘操作。

Source
REQ-F-006

Acceptance Criteria

AC1：角色

Given Alert 渲染
Then 根元素 role="alert"

AC2：关闭按钮可键盘操作

Given 焦点在 Alert 关闭按钮上
When 按下 Enter 或 Space
Then 关闭按钮被激活
```

## 3.4 Divider（分割线）

```
REQ-F-024 🔴 [Actor: Application Developer]

Title
渲染与方向

Description
Divider 渲染为分割线元素，支持水平与垂直方向。

Source
组件需求定义

Acceptance Criteria

AC1：水平默认

Given 使用方未指定方向
When 组件渲染
Then 渲染为水平分割线

AC2：垂直方向

Given 使用方指定垂直方向
When 组件渲染
Then 渲染为垂直分割线
```

```
REQ-F-025 🟡 [Actor: Application Developer]

Title
视觉定制

Description
Divider 支持自定义颜色、粗细与线型（实线、虚线等）。

Source
组件需求定义

Acceptance Criteria

AC1：视觉属性

Given 使用方指定颜色与线型
When 组件渲染
Then 分割线呈现指定视觉
```

```
REQ-F-026 🟡 [Actor: Application Developer]

Title
嵌入内容

Description
Divider 支持在分割线中间嵌入文本或自定义内容。

Source
组件需求定义

Acceptance Criteria

AC1：嵌入内容

Given 使用方传入 ChildContent
When 组件渲染
Then 内容渲染在分割线中间位置
```

```
REQ-F-027 🟢 [Actor: System]

Title
响应式适配

Description
Divider 在容器尺寸变化时保持完整显示，不出现溢出或截断。

Source
组件需求定义

Acceptance Criteria

AC1：无溢出

Given 容器宽度变化
When 组件渲染
Then 分割线不溢出容器
```

## 3.5 Input（输入框）

```
REQ-F-028 🔴 [Actor: Application Developer]

Title
渲染与类型

Description
Input 渲染为原生 input 元素，支持类型：Text（默认）、Email、Password、Number、Checkbox 等。

Source
组件需求定义

Acceptance Criteria

AC1：类型映射

Given 使用方指定 Type="Password"
When 组件渲染
Then input 的 type 为 password
```

```
REQ-F-029 🔴 [Actor: Application Developer]

Title
值绑定

Description
Input 支持双向绑定：Value 参数 + ValueChanged 回调（EventCallback）。

Source
组件需求定义

Acceptance Criteria

AC1：外部值渲染

Given 使用方传入 Value="hello"
When 组件渲染
Then 输入框显示 hello

AC2：用户输入通知

Given 使用方绑定 ValueChanged
When 终端用户输入内容
Then ValueChanged 携带最新值触发

AC3：外部值不覆盖输入

Given 终端用户正在输入
When 父组件因无关原因重渲染
Then 用户输入中的内容不被覆盖
```

```
REQ-F-030 🟡 [Actor: End User]

Title
输入事件

Description
Input 支持 OnBlur（失焦）、OnEnter（回车）事件回调。

Source
组件需求定义

Acceptance Criteria

AC1：回车事件

Given 使用方绑定 OnEnter
When 终端用户聚焦输入框并按下 Enter
Then OnEnter 携带当前值触发

AC2：失焦事件

Given 使用方绑定 OnBlur
When 输入框失焦
Then OnBlur 携带当前值触发
```

```
REQ-F-031 🟡 [Actor: Application Developer]

Title
占位符与自动补全

Description
Input 支持 Placeholder 占位符与 Autocomplete 自动补全配置。

Source
组件需求定义

Acceptance Criteria

AC1：占位符

Given 使用方指定 Placeholder
When 组件渲染
Then input 带对应占位符属性
```

```
REQ-F-032 🔴 [Actor: Application Developer]

Title
禁用与只读

Description
Input 支持禁用（IsDisabled）与只读状态，禁用遵循 REQ-F-002。

Source
REQ-F-002

Acceptance Criteria

AC1：禁用

Given IsDisabled="true"
When 组件渲染
Then input 带 disabled 属性
And 不可输入
```

```
REQ-F-033 🔴 [Actor: Application Developer]

Title
验证

Description
Input 支持验证器（Validator 委托）与错误消息显示。验证失败时显示错误样式与消息，通过 aria-invalid 表达。

Source
组件需求定义

Acceptance Criteria

AC1：验证失败

Given 使用方配置 Validator 且输入值不合法
When 触发验证
Then 组件显示错误消息
And aria-invalid="true"

AC2：验证通过

Given 使用方配置 Validator 且输入值合法
When 触发验证
Then 无错误显示
And aria-invalid="false"
```

```
REQ-F-034 🟢 [Actor: Application Developer]

Title
图标支持

Description
Input 支持前置与后置图标。

Source
组件需求定义

Acceptance Criteria

AC1：图标渲染

Given 使用方指定前置或后置图标
When 组件渲染
Then 图标渲染在输入框对应位置
```

```
REQ-F-035 🟡 [Actor: System]

Title
自动聚焦

Description
Input 支持 AutoFocus 参数，仅在组件首次渲染时聚焦，不得重复抢占焦点。

Source
经验教训：AWComponent AutoFocus 每次渲染反复聚焦

Acceptance Criteria

AC1：仅首次聚焦

Given AutoFocus="true"
When 组件反复重渲染
Then 仅在首次渲染后聚焦
And 后续渲染不移动焦点
```

```
REQ-F-036 🔴 [Actor: End User]

Title
可访问性

Description
Input 必须可关联 Label（通过 for/id），错误状态通过 aria-invalid 表达。

Source
REQ-F-006、REQ-F-007

Acceptance Criteria

AC1：可关联标签

Given 使用方为 Input 提供关联标签
When 点击标签
Then 焦点移动到输入框
```

## 3.6 Label（标签）

```
REQ-F-037 🔴 [Actor: Application Developer]

Title
渲染与关联

Description
Label 渲染为原生 label 元素，支持通过 For 关联目标控件（生成 for 属性）。

Source
组件需求定义

Acceptance Criteria

AC1：关联

Given 使用方指定 For="username"
When 组件渲染
Then label 带 for="username"
```

```
REQ-F-038 🟡 [Actor: Application Developer]

Title
内容与必填标记

Description
Label 支持文本（Text）或自定义内容（ChildContent，优先级更高），支持必填标记。

Source
组件需求定义

Acceptance Criteria

AC1：内容优先级

Given 同时传入 Text 与 ChildContent
When 组件渲染
Then 渲染 ChildContent

AC2：必填标记

Given 使用方标记必填
When 组件渲染
Then 显示必填指示符（如星号）
```

```
REQ-F-039 🔴 [Actor: System]

Title
可访问性

Description
Label 使用原生 label 元素语义，不设置误导性角色。

Source
REQ-F-006

Acceptance Criteria

AC1：原生语义

Given Label 渲染
Then 生成 label 元素
```

## 3.7 Modal（对话框）

```
REQ-F-040 🔴 [Actor: Application Developer]

Title
打开与关闭

Description
Modal 通过 IsVisible 参数控制打开/关闭；关闭时触发 OnClose 事件回调。隐藏时不渲染。

Source
组件需求定义

Acceptance Criteria

AC1：打开

Given IsVisible="true"
When 组件渲染
Then 对话框显示

AC2：关闭通知

Given 对话框打开且绑定 OnClose
When 触发任一关闭途径（关闭按钮、ESC、遮罩点击、确认按钮）
Then OnClose 触发

AC3：隐藏不渲染

Given IsVisible="false"
When 组件渲染
Then 对话框不渲染
```

```
REQ-F-041 🟡 [Actor: Application Developer]

Title
尺寸预设

Description
Modal 支持尺寸预设（Small、Medium、Large）。具体尺寸数值由详细设计确定。

Source
组件需求定义

Acceptance Criteria

AC1：尺寸应用

Given 使用方指定 Size="Large"
When 组件渲染
Then 对话框呈现对应尺寸
```

```
REQ-F-042 🔴 [Actor: Application Developer]

Title
区域模板

Description
Modal 支持三个区域模板：标题（Header）、内容（Body）、页脚（Footer）。

Source
组件需求定义

Acceptance Criteria

AC1：模板渲染

Given 使用方提供标题、内容、页脚模板
When 组件渲染
Then 三个区域分别渲染对应内容
```

```
REQ-F-043 🟡 [Actor: Application Developer]

Title
操作按钮配置

Description
Modal 提供确认与取消按钮，支持分别控制显示（ShowConfirmButton、ShowCancelButton、ShowCloseButton）。

Source
经验教训：AWComponent ShowCancelButton 参数未实现

Acceptance Criteria

AC1：按钮显隐

Given 使用方设置 ShowConfirmButton="false"
When 组件渲染
Then 确认按钮不渲染

AC2：确认事件

Given 按钮可见且绑定 OnConfirm
When 终端用户点击确认按钮
Then OnConfirm 触发
```

```
REQ-F-044 🟡 [Actor: End User]

Title
遮罩行为

Description
Modal 显示遮罩层，支持配置点击遮罩是否关闭（CloseOnBackdrop，默认关闭不触发）。

Source
组件需求定义

Acceptance Criteria

AC1：默认不关闭

Given 未启用 CloseOnBackdrop
When 终端用户点击遮罩
Then 对话框不关闭

AC2：启用后关闭

Given 启用 CloseOnBackdrop
When 终端用户点击遮罩
Then 对话框关闭
And 触发 OnClose
```

```
REQ-F-045 🔴 [Actor: End User]

Title
键盘交互

Description
对话框打开时，按下 ESC 键关闭并触发 OnClose。ESC 键判定必须符合浏览器 KeyboardEvent.key 规范（"Escape"）。

Source
经验教训：AWComponent ESC 键大小写不匹配失效

Acceptance Criteria

AC1：ESC 关闭

Given 对话框打开且焦点在对话框内
When 终端用户按下 ESC
Then 对话框关闭
And 触发 OnClose
```

```
REQ-F-046 🔴 [Actor: System]

Title
焦点管理

Description
对话框打开时将焦点移入对话框，关闭后将焦点还原到打开前的元素。焦点被限制在对话框内（焦点陷阱）。

Source
经验教训：AWComponent Modal 无焦点管理

Acceptance Criteria

AC1：打开聚焦

Given 对话框打开
When 打开动画完成
Then 焦点位于对话框内

AC2：焦点陷阱

Given 对话框打开
When 终端用户连续按 Tab
Then 焦点在对话框内循环，不逃逸到背景页面

AC3：关闭还原

Given 对话框关闭
When 关闭完成
Then 焦点还原到打开前的元素
```

```
REQ-F-047 🔴 [Actor: System]

Title
可访问性

Description
Modal 必须设置 role="dialog" 与 aria-modal="true"，标题区域与 aria-labelledby 关联。

Source
经验教训：AWComponent Modal 无 role/aria；WCAG 2.1

Acceptance Criteria

AC1：角色与属性

Given Modal 打开
Then 根元素 role="dialog"
And aria-modal="true"
```

## 3.8 Progress（进度条）

```
REQ-F-048 🔴 [Actor: Application Developer]

Title
渲染与值

Description
Progress 渲染为进度条元素，通过 Value 参数表示进度。

Source
组件需求定义

Acceptance Criteria

AC1：渲染

Given 使用方传入 Value
When 组件渲染
Then 进度条按比例填充
```

```
REQ-F-049 🟡 [Actor: System]

Title
数值范围

Description
Progress 支持 Min/Max 范围（默认 0–100），值超出范围时按边界收敛。

Source
组件需求定义

Acceptance Criteria

AC1：收敛

Given Value 大于 Max
When 组件渲染
Then 进度按 Max 收敛
And 不出现异常值
```

```
REQ-F-050 🟡 [Actor: Application Developer]

Title
文本显示

Description
Progress 支持显示百分比文本（ShowValue），文本内容为计算后的百分比。

Source
组件需求定义

Acceptance Criteria

AC1：文本

Given ShowValue="true"
When 组件渲染
Then 显示百分比文本
```

```
REQ-F-051 🟡 [Actor: Application Developer]

Title
视觉定制

Description
Progress 的填充颜色支持通过 CSS 变量定制。

Source
组件需求定义

Acceptance Criteria

AC1：颜色定制

Given 使用方覆盖进度条颜色变量
When 组件渲染
Then 填充呈现定制颜色
```

```
REQ-F-052 🔴 [Actor: System]

Title
可访问性

Description
Progress 必须设置 role="progressbar" 与 aria-valuenow/aria-valuemin/aria-valuemax。

Source
REQ-F-006

Acceptance Criteria

AC1：属性

Given Progress 渲染
Then 携带 role="progressbar"
And 携带 aria-valuenow、aria-valuemin、aria-valuemax 且值正确
```

## 3.9 Select（下拉选择）

```
REQ-F-053 🔴 [Actor: Application Developer]

Title
渲染与选项

Description
Select 渲染为原生 select 元素，Options 提供选项集合。

Source
组件需求定义

Acceptance Criteria

AC1：选项渲染

Given 使用方提供选项集合
When 组件渲染
Then 每个选项渲染为 option 元素
```

```
REQ-F-054 🔴 [Actor: Application Developer]

Title
值绑定

Description
Select 支持双向绑定：Value + ValueChanged。

Source
组件需求定义

Acceptance Criteria

AC1：选中同步

Given 使用方绑定 Value
When 终端用户选择选项
Then ValueChanged 携带选中值触发
And 界面选中状态更新
```

```
REQ-F-055 🟡 [Actor: Application Developer]

Title
占位符

Description
Select 支持 Placeholder，未选中时显示占位选项。

Source
组件需求定义

Acceptance Criteria

AC1：占位选项

Given 未选中任何值且提供 Placeholder
When 组件渲染
Then 显示占位选项
```

```
REQ-F-056 🔴 [Actor: Application Developer]

Title
禁用

Description
Select 支持禁用，遵循 REQ-F-002。

Source
REQ-F-002

Acceptance Criteria

AC1：禁用

Given IsDisabled="true"
When 组件渲染
Then select 带 disabled 属性
And 不可操作
```

```
REQ-F-057 🟡 [Actor: Application Developer]

Title
选项模板

Description
Select 支持 OptionTemplate（RenderFragment）自定义选项渲染。

Source
组件需求定义

Acceptance Criteria

AC1：模板

Given 使用方提供 OptionTemplate
When 组件渲染
Then 每个选项按模板渲染
```

```
REQ-F-058 🟢 [Actor: End User]

Title
多选模式

Description
Select 支持多选模式。

Source
组件需求定义（P2 待定）

Acceptance Criteria

AC1：多选

Given 启用多选
When 终端用户选择多个选项
Then 选中值集合正确
```

```
REQ-F-059 🟢 [Actor: End User]

Title
搜索过滤

Description
Select 支持在选项较多时按关键字过滤。

Source
组件需求定义（P2 待定）

Acceptance Criteria

AC1：过滤

Given 终端用户输入过滤关键字
When 选项集合变化
Then 仅显示匹配的选项
```

```
REQ-F-060 🔴 [Actor: End User]

Title
可访问性

Description
Select 使用原生 select 语义（原生支持键盘导航），禁用时带 aria-disabled。

Source
REQ-F-006

Acceptance Criteria

AC1：原生语义

Given Select 渲染
Then 生成 select 元素
And 键盘上下键可切换选项
```

## 3.10 Tree（树）

```
REQ-F-061 🔴 [Actor: Application Developer]

Title
渲染与层级

Description
Tree 渲染节点层级结构，节点模型支持文本、图标、子节点、展开/折叠状态。

Source
组件需求定义

Acceptance Criteria

AC1：层级渲染

Given 使用方提供多级节点树
When 组件渲染
Then 各级节点按层级缩进渲染
```

```
REQ-F-062 🔴 [Actor: End User]

Title
展开与折叠

Description
有子节点的节点支持展开/折叠，通过点击展开指示器切换。

Source
组件需求定义

Acceptance Criteria

AC1：展开切换

Given 节点有子节点且当前折叠
When 终端用户点击展开指示器
Then 子节点显示

AC2：折叠切换

Given 节点当前展开
When 终端用户点击展开指示器
Then 子节点隐藏
```

```
REQ-F-063 🟡 [Actor: End User]

Title
节点选中

Description
点击节点文本触发 OnNodeSelected 事件，携带选中节点。

Source
组件需求定义

Acceptance Criteria

AC1：选中事件

Given 使用方绑定 OnNodeSelected
When 终端用户点击节点文本
Then 事件携带对应节点触发
```

```
REQ-F-064 🟡 [Actor: Application Developer]

Title
节点图标

Description
节点支持图标：折叠/展开状态可分别配置图标，叶子节点可配置独立图标。

Source
组件需求定义

Acceptance Criteria

AC1：状态图标

Given 节点配置展开与折叠图标
When 节点状态切换
Then 图标随之切换
```

```
REQ-F-065 🟢 [Actor: System]

Title
懒加载

Description
Tree 支持子节点懒加载，展开时通过回调获取子节点。

Source
组件需求定义（P2 待定）

Acceptance Criteria

AC1：懒加载

Given 节点启用懒加载且未加载
When 首次展开
Then 通过回调加载子节点并显示
```

```
REQ-F-066 🔴 [Actor: End User]

Title
可访问性

Description
Tree 必须设置 role="tree"、节点 role="treeitem"，支持键盘导航（方向键移动、Enter 选中、Space/Enter 展开折叠）。

Source
REQ-F-006、REQ-F-007

Acceptance Criteria

AC1：角色

Given Tree 渲染
Then 根元素 role="tree"
And 节点 role="treeitem"

AC2：键盘导航

Given 焦点在树节点上
When 使用方向键与 Enter
Then 焦点在节点间移动
And 选中与展开折叠行为正确
```

## 3.11 Table（表格）

```
REQ-F-067 🔴 [Actor: System]

Title
渲染真实数据

Description
Table 必须渲染 Data 集合中的真实行数据，禁止硬编码或占位内容。

Source
经验教训：AWComponent Table 硬编码假数据

Acceptance Criteria

AC1：真实数据

Given 使用方提供 3 行数据
When 组件渲染
Then 表格渲染 3 行
And 内容与数据一致

AC2：空数据

Given 使用方提供空集合
When 组件渲染
Then 表格体不渲染行
And 渲染空状态（见 REQ-F-071）
```

```
REQ-F-068 🔴 [Actor: Application Developer]

Title
列定义

Description
Table 通过列定义控制列：列标题、值模板（RenderFragment）、对齐方式。

Source
组件需求定义

Acceptance Criteria

AC1：列标题

Given 使用方定义 2 列并指定标题
When 组件渲染
Then 表头渲染 2 个列标题

AC2：值模板

Given 使用方为列提供值模板
When 组件渲染
Then 每行对应单元格按模板渲染
```

```
REQ-F-069 🟡 [Actor: Application Developer]

Title
表头固定

Description
Table 支持 IsFixedHeader，启用后表头固定、表体滚动。

Source
组件需求定义

Acceptance Criteria

AC1：固定表头

Given 启用 IsFixedHeader 且数据超出一屏
When 表体滚动
Then 表头保持可见
```

```
REQ-F-070 🟡 [Actor: End User]

Title
行交互

Description
Table 支持行点击事件（OnRowClick），携带对应行数据。

Source
组件需求定义

Acceptance Criteria

AC1：行点击

Given 使用方绑定 OnRowClick
When 终端用户点击行
Then 事件携带该行数据触发
```

```
REQ-F-071 🟡 [Actor: System]

Title
空状态

Description
数据为空时 Table 渲染空状态提示（EmptyTemplate 可自定义）。

Source
组件需求定义

Acceptance Criteria

AC1：默认空状态

Given 数据为空且未提供 EmptyTemplate
When 组件渲染
Then 显示默认空状态提示

AC2：自定义空状态

Given 数据为空且提供 EmptyTemplate
When 组件渲染
Then 按模板渲染空状态
```

```
REQ-F-072 🟢 [Actor: Application Developer]

Title
斑马纹

Description
Table 支持斑马纹（隔行变色），可通过参数开关。

Source
组件需求定义

Acceptance Criteria

AC1：斑马纹

Given 启用斑马纹
When 组件渲染
Then 相邻行底色可辨识差异
```

```
REQ-F-073 🟢 [Actor: End User]

Title
分页

Description
Table 支持分页（页大小可配置）。

Source
组件需求定义（P2 待定）

Acceptance Criteria

AC1：分页

Given 数据超过页大小
When 组件渲染
Then 仅显示当前页数据
And 提供翻页控件
```

```
REQ-F-074 🔴 [Actor: System]

Title
可访问性

Description
Table 必须使用原生 table 语义（thead/tbody/th），支持 Caption 标题，表头 scope="col"。

Source
REQ-F-006

Acceptance Criteria

AC1：语义结构

Given Table 渲染
Then 生成 table 元素
And 表头 th 带 scope="col"

AC2：标题

Given 使用方提供 Caption
When 组件渲染
Then 渲染 caption 内容
```

---

# 4 非功能需求

```
REQ-NF-001 🔴 [Actor: System]

Title
目标框架

Description
组件库目标框架为 net10.0。

Source
技术决策：目标框架 net10.0

Acceptance Criteria

AC1：构建

Given 解决方案
When 执行构建
Then 无错误生成
```

```
REQ-NF-002 🔴 [Actor: System]

Title
浏览器兼容

Description
组件在 Chrome、Edge、Firefox、Safari 最近两个主版本中行为一致。

Source
非功能需求定义

Acceptance Criteria

AC1：兼容

Given 任一组件
When 在受支持浏览器中渲染与交互
Then 行为一致
```

```
REQ-NF-003 🟡 [Actor: System]

Title
性能

Description
组件渲染遵循 Blazor 标准渲染语义，不引入不必要的重渲染；常规列表（≤100 行）渲染无感知延迟。

Source
设计原则 1（行为可预测）

Acceptance Criteria

AC1：常规渲染

Given 100 行数据的表格
When 渲染
Then 完成时间在可感知范围（具体指标由详细设计确定）
```

```
REQ-NF-004 🔴 [Actor: System]

Title
可访问性标准

Description
组件整体达到 WCAG 2.1 AA 级别。

Source
WCAG 2.1

Acceptance Criteria

AC1：标准

Given 任一组件
When 使用辅助技术（屏幕阅读器）与键盘访问
Then 所有功能可访问
```

```
REQ-NF-005 🔴 [Actor: System]

Title
依赖最小化

Description
组件库运行时依赖仅允许 Microsoft.AspNetCore.Components.Web，不引入 UI 无关的第三方包。

Source
设计原则 2（职责单一）

Acceptance Criteria

AC1：依赖审计

Given 组件库项目文件
When 审查 PackageReference
Then 仅包含允许的运行时依赖
```

```
REQ-NF-006 🟡 [Actor: System]

Title
包发布

Description
组件库可打包为 NuGet 包（PackageId Astrea），静态资源（CSS/JS）随包分发。

Source
非功能需求定义

Acceptance Criteria

AC1：打包

Given 执行 dotnet pack
When 构建完成
Then 生成有效 nupkg
And 包含静态资源
```

```
REQ-NF-007 🟡 [Actor: Application Developer]

Title
代码文档

Description
公共 API 必须提供 XML 文档注释，注释描述行为而非重复参数名。

Source
设计原则 5（文档真实）

Acceptance Criteria

AC1：公共 API 注释

Given 任一公共类型或成员
Then 存在 XML 文档注释
```

```
REQ-NF-008 🔴 [Actor: Application Developer]

Title
测试覆盖

Description
每个组件的关键行为（渲染、交互、禁用、A11Y 属性）有测试覆盖，测试套件全部通过。

Source
设计原则 4（可验证）

Acceptance Criteria

AC1：套件通过

Given 测试套件
When 执行 dotnet test
Then 全部通过
```

---

# 5 需求统计

## 5.1 按优先级

| 优先级 | 数量 |
|:---:|:---:|
| 🔴 P0 | 48 |
| 🟡 P1 | 26 |
| 🟢 P2 | 8 |
| 合计 | 82 |

## 5.2 按章节

| 章节 | 范围 | 数量 |
|------|------|:---:|
| §3.1 通用 | REQ-F-001 ~ 009 | 9 |
| §3.2 Button | REQ-F-010 ~ 017 | 8 |
| §3.3 Alert | REQ-F-018 ~ 023 | 6 |
| §3.4 Divider | REQ-F-024 ~ 027 | 4 |
| §3.5 Input | REQ-F-028 ~ 036 | 9 |
| §3.6 Label | REQ-F-037 ~ 039 | 3 |
| §3.7 Modal | REQ-F-040 ~ 047 | 8 |
| §3.8 Progress | REQ-F-048 ~ 052 | 5 |
| §3.9 Select | REQ-F-053 ~ 060 | 8 |
| §3.10 Tree | REQ-F-061 ~ 066 | 6 |
| §3.11 Table | REQ-F-067 ~ 074 | 8 |
| §4 NFR | REQ-NF-001 ~ 008 | 8 |
| 合计 | | 82 |

---

## 附录A：需求统计

### §3.1 通用组件需求

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-001 | 🔴 | Application Developer | 组件基类与属性透传 |
| REQ-F-002 | 🔴 | Application Developer | 禁用状态语义 |
| REQ-F-003 | 🔴 | System | 事件处理与渲染语义 |
| REQ-F-004 | 🔴 | Application Developer | 主题定制 |
| REQ-F-005 | 🔴 | System | 样式作用域隔离 |
| REQ-F-006 | 🔴 | System | 语义角色 |
| REQ-F-007 | 🟡 | End User | 键盘交互基础 |
| REQ-F-008 | 🔴 | Application Developer | 组件测试要求 |
| REQ-F-009 | 🟡 | Application Developer | 组件文档要求 |

### §3.2 Button（按钮）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-010 | 🔴 | System | 渲染为原生按钮 |
| REQ-F-011 | 🔴 | End User | 点击事件 |
| REQ-F-012 | 🔴 | Application Developer | 按钮类型 |
| REQ-F-013 | 🔴 | Application Developer | 视觉变体 |
| REQ-F-014 | 🟡 | Application Developer | 尺寸 |
| REQ-F-015 | 🔴 | Application Developer | 禁用 |
| REQ-F-016 | 🟡 | Application Developer | 图标支持 |
| REQ-F-017 | 🔴 | End User | 可访问性 |

### §3.3 Alert（警告提示）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-018 | 🔴 | Application Developer | 渲染与类型 |
| REQ-F-019 | 🔴 | Application Developer | 内容 |
| REQ-F-020 | 🔴 | End User | 关闭行为 |
| REQ-F-021 | 🔴 | Application Developer | 可见性控制 |
| REQ-F-022 | 🟢 | System | 自动关闭 |
| REQ-F-023 | 🔴 | End User | 可访问性 |

### §3.4 Divider（分割线）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-024 | 🔴 | Application Developer | 渲染与方向 |
| REQ-F-025 | 🟡 | Application Developer | 视觉定制 |
| REQ-F-026 | 🟡 | Application Developer | 嵌入内容 |
| REQ-F-027 | 🟢 | System | 响应式适配 |

### §3.5 Input（输入框）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-028 | 🔴 | Application Developer | 渲染与类型 |
| REQ-F-029 | 🔴 | Application Developer | 值绑定 |
| REQ-F-030 | 🟡 | End User | 输入事件 |
| REQ-F-031 | 🟡 | Application Developer | 占位符与自动补全 |
| REQ-F-032 | 🔴 | Application Developer | 禁用与只读 |
| REQ-F-033 | 🔴 | Application Developer | 验证 |
| REQ-F-034 | 🟢 | Application Developer | 图标支持 |
| REQ-F-035 | 🟡 | System | 自动聚焦 |
| REQ-F-036 | 🔴 | End User | 可访问性 |

### §3.6 Label（标签）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-037 | 🔴 | Application Developer | 渲染与关联 |
| REQ-F-038 | 🟡 | Application Developer | 内容与必填标记 |
| REQ-F-039 | 🔴 | System | 可访问性 |

### §3.7 Modal（对话框）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-040 | 🔴 | Application Developer | 打开与关闭 |
| REQ-F-041 | 🟡 | Application Developer | 尺寸预设 |
| REQ-F-042 | 🔴 | Application Developer | 区域模板 |
| REQ-F-043 | 🟡 | Application Developer | 操作按钮配置 |
| REQ-F-044 | 🟡 | End User | 遮罩行为 |
| REQ-F-045 | 🔴 | End User | 键盘交互 |
| REQ-F-046 | 🔴 | System | 焦点管理 |
| REQ-F-047 | 🔴 | System | 可访问性 |

### §3.8 Progress（进度条）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-048 | 🔴 | Application Developer | 渲染与值 |
| REQ-F-049 | 🟡 | System | 数值范围 |
| REQ-F-050 | 🟡 | Application Developer | 文本显示 |
| REQ-F-051 | 🟡 | Application Developer | 视觉定制 |
| REQ-F-052 | 🔴 | System | 可访问性 |

### §3.9 Select（下拉选择）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-053 | 🔴 | Application Developer | 渲染与选项 |
| REQ-F-054 | 🔴 | Application Developer | 值绑定 |
| REQ-F-055 | 🟡 | Application Developer | 占位符 |
| REQ-F-056 | 🔴 | Application Developer | 禁用 |
| REQ-F-057 | 🟡 | Application Developer | 选项模板 |
| REQ-F-058 | 🟢 | End User | 多选模式 |
| REQ-F-059 | 🟢 | End User | 搜索过滤 |
| REQ-F-060 | 🔴 | End User | 可访问性 |

### §3.10 Tree（树）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-061 | 🔴 | Application Developer | 渲染与层级 |
| REQ-F-062 | 🔴 | End User | 展开与折叠 |
| REQ-F-063 | 🟡 | End User | 节点选中 |
| REQ-F-064 | 🟡 | Application Developer | 节点图标 |
| REQ-F-065 | 🟢 | System | 懒加载 |
| REQ-F-066 | 🔴 | End User | 可访问性 |

### §3.11 Table（表格）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-067 | 🔴 | System | 渲染真实数据 |
| REQ-F-068 | 🔴 | Application Developer | 列定义 |
| REQ-F-069 | 🟡 | Application Developer | 表头固定 |
| REQ-F-070 | 🟡 | End User | 行交互 |
| REQ-F-071 | 🟡 | System | 空状态 |
| REQ-F-072 | 🟢 | Application Developer | 斑马纹 |
| REQ-F-073 | 🟢 | End User | 分页 |
| REQ-F-074 | 🔴 | System | 可访问性 |

### §4 非功能需求

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-NF-001 | 🔴 | System | 目标框架 |
| REQ-NF-002 | 🔴 | System | 浏览器兼容 |
| REQ-NF-003 | 🟡 | System | 性能 |
| REQ-NF-004 | 🔴 | System | 可访问性标准 |
| REQ-NF-005 | 🔴 | System | 依赖最小化 |
| REQ-NF-006 | 🟡 | System | 包发布 |
| REQ-NF-007 | 🟡 | Application Developer | 代码文档 |
| REQ-NF-008 | 🔴 | Application Developer | 测试覆盖 |

---

## 附录B：延期需求

| 编号 | 标题 | 说明 |
|------|------|------|
| REQ-F-022 | Alert 自动关闭 | P2，待确认是否纳入首个版本 |
| REQ-F-058 | Select 多选 | P2，待确认交互设计 |
| REQ-F-059 | Select 搜索过滤 | P2，待确认交互设计 |
| REQ-F-065 | Tree 懒加载 | P2，依赖数据加载策略 |
| REQ-F-073 | Table 分页 | P2，依赖分页组件设计 |

---

## 附录C：需求变更记录

| 日期 | 变更 | 作者 |
|------|------|:---:|
| 2026-08-01 | **创建 SRS**: 定义 Astrea 组件库范围、组件清单、非目标与全部功能/非功能需求 | wubing7755 |
| 2026-08-01 | **格式对齐**: 需求条目改为代码围栏 + Title/Description/Acceptance Criteria 格式，非功能需求编号改为 REQ-NF，补充 Actor 与附录A 统计表 | wubing7755 |

---

## 附录D：经验教训映射（AWComponent → Astrea）

| AWComponent 问题 | 对应需求 |
|------------------|----------|
| Table 硬编码假数据 | REQ-F-067 |
| 渲染优化导致 UI 不更新 | REQ-F-003 |
| 禁用实现为隐藏元素 | REQ-F-002 |
| Modal ESC 键大小写不匹配失效 | REQ-F-045 |
| Modal A11Y 缺失（无 role/aria/焦点管理） | REQ-F-046、REQ-F-047 |
| 内联硬编码样式无法主题化 | REQ-F-004 |
| 调试演示代码混入组件库 | REQ-F-009、REQ-F-008 |
| 上传/翻译/矩阵等范围蔓延 | §1.2.2 范围外 |
| 全局 CSS 选择器泄漏 | REQ-F-005 |
| 测试覆盖不足 | REQ-F-008、REQ-NF-008 |
