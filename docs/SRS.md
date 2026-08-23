# 软件需求规范（SRS）

| 项目 | 内容 |
|------|------|
| 项目名称 | Astrea |
| 版本 | 0.6.0 |
| 状态 | 草案 |
| 日期 | 2026-08-14 |
| 作者 | wubing7755 |
| 适用范围 | Blazor 组件库（.NET 6.0） |

---

# 1 引言

## 1.1 目的

本文档定义 Astrea——一个面向 .NET 6.0 / Blazor 的 UI 组件库——的功能需求与非功能需求。本文档是 Astrea 用户可观察行为和非功能需求的权威来源，是后续概要设计、详细设计、开发与测试的需求依据。

## 1.2 范围

### 1.2.1 范围内

Astrea 提供可复用的 UI 组件，覆盖基础控件、表单、反馈与数据展示场景。组件清单如下：

| 分类 | 组件 |
|------|------|
| Basic | Button、Divider、Label |
| Form | Input、Select |
| Feedback | Alert、Modal（对话框） |
| Data Display | Table、Tree、Progress、ResourceTable |

### 1.2.2 范围外

以下内容**不属于** Astrea 组件库范围，任何需求不得引入：

| 领域 | 说明 |
|------|------|
| 文件上传/下载 | 分块上传、断点续传、文件预览等不纳入组件库 |
| 翻译服务 | 调用外部翻译 API（如 Google Translate）不纳入 |
| 数学运算 | 矩阵、几何变换等数学工具不纳入 |
| 应用级服务 | 撤销/重做服务、事件总线、跨组件通信框架不纳入 |
| 应用级布局 | 可停靠布局、分栏面板等应用级布局能力不纳入组件库 |
| 未纳入首版的 UI 组件 | NumberInput、Checkbox、Radio、Switch、TextArea、DatePicker、Combobox、Tooltip、Toast 等未列于 §1.2.1 的组件不纳入本版 |

## 1.3 术语

### 1.3.1 术语定义

| English | 中文 | 说明 |
|---------|------|------|
| Component | 组件 | Astrea 提供的可复用 UI 单元 |
| Variant | 变体 | 组件的视觉风格（如 Primary、Danger） |
| Size | 尺寸 | 组件的尺寸级别（Small、Medium、Large） |
| IsDisabled | 禁用 | 组件不可交互但仍可见的状态 |
| Event Notification | 事件通知 | 组件向应用开发者报告终端用户操作或状态变化的可观察行为 |
| AdditionalAttributes | 附加属性 | 未被组件单独声明的属性应用到组件文档规定的目标元素 |
| CSS Variable | CSS 变量 | 用于主题定制的自定义属性（--ast-*） |
| Acceptance Criteria | 验收标准 | 需求的可验证行为准则 |
| Accessible Name | 可访问名称 | 辅助技术用于识别控件用途的名称 |
| Focus Trap | 焦点陷阱 | 对话框打开时将键盘焦点限制在对话框内的行为 |
| Live Region | 实时区域 | 内容变化时可由辅助技术自动播报的页面区域 |

### 1.3.2 角色定义

| English | 中文 | 说明 |
|---------|------|------|
| Application Developer | 应用开发者 | 在应用中使用 Astrea 组件，并观察公开配置、数据、通知和内容效果的开发者 |
| End User | 终端用户 | 使用构建后应用的最终用户，执行点击、输入和浏览等交互 |
| Keyboard User | 键盘用户 | 主要通过键盘导航和操作界面的终端用户 |
| Assistive Technology User | 辅助技术用户 | 通过屏幕阅读器或其他辅助技术感知和操作界面的终端用户 |
| System | 系统 | Astrea 组件在运行时产生对外可观察结果的行为主体 |

## 1.4 文档约定

### 1.4.1 需求编号规则

每条功能需求使用唯一编号 `REQ-F-XXX`，非功能需求使用 `REQ-NF-XXX`：

- **REQ**: Requirement（需求）
- **F**: Functional（功能需求）
- **NF**: Non-Functional（非功能需求）
- **XXX**: 三位数字顺序号（F: 001~097；NF: 001~012）。新增需求沿用末尾编号并归入对应组件章节，因此正文中的物理顺序不要求与编号顺序一致

### 1.4.2 优先级定义

| 优先级 | 符号 | 含义 |
|:---:|:---:|------|
| P0 | 🔴 | 必须实现——核心功能，缺失则系统不可用 |
| P1 | 🟡 | 应当实现——重要功能，缺失则显著降低可用性 |
| P2 | 🟢 | 增强能力——是否纳入当前版本由需求状态决定 |

### 1.4.3 需求状态

- 除附录 B 明确列出的需求外，本文档中的需求均为 **Active**，属于当前版本需求基线。
- 附录 B 中的需求状态为 **Deferred**，不属于当前版本必须实现的范围，但保留其预期行为供后续版本评估。
- 优先级表示需求的重要程度，需求状态表示是否纳入当前版本，两者不得混用。

### 1.4.4 需求格式

每条需求包含以下字段，并使用代码围栏包裹：

- **编号与优先级**: `REQ-F-XXX` + 优先级符号（🔴🟡🟢）+ `[Actor: 角色]`
- **Title**: 需求标题
- **Description**: 需求描述
- **Source**: 需求来源、产品决策、标准或经验教训
- **Acceptance Criteria**: 使用 Given/When/Then 格式的验收标准，每行一个步骤

---

## 1.5 需求与软件设计边界

本文档只规定应用开发者、终端用户和辅助技术能够观察到的行为，以及可验证的非功能约束。需求应描述系统必须表现出的结果，不规定实现这些结果的软件结构或技术方案。

需求条目可以规定公开能力、输入效果、事件通知、渲染语义、键盘行为、状态变化和质量目标，但不得规定内部基类、继承关系、具体类型、接口签名、状态字段、项目结构、样式隔离机制、测试工具或其他软件设计细节。上述内容由后续概要设计和详细设计确定。

---

# 2 总体描述

## 2.1 产品定位

Astrea 是一个企业级 Blazor UI 组件库，强调**正确性、可访问性与可测试性**。组件命名源自希腊神话中的星辰女神 Astraea（阿斯特蕾亚），寓意组件如同夜空中的星辰，为界面提供清晰、可靠、一致的视觉元素。

## 2.2 产品与需求原则

以下原则源自前身项目 AWComponent 的经验教训，是确定和评审需求的依据：

1. **行为可预测**：相同输入和用户操作产生一致、可说明的可观察结果。
2. **职责单一**：组件库只承载 UI 组件职责，不纳入应用级服务。
3. **可访问性优先**：每个组件提供与用途一致的语义；可交互组件提供与交互模式一致的键盘行为。
4. **可验证**：每项关键行为和质量约束具有可执行、结果可判定的验收标准。
5. **文档真实**：文档描述的能力与发布包的实际可观察行为一致。

## 2.3 约束与假设

| 类型 | 内容 |
|------|------|
| 技术约束 | 目标框架为 .NET 6.0；支持在 .NET 6.0 Blazor Server 与 Blazor WebAssembly 应用中使用 |
| 浏览器约束 | 支持发布时 Chrome、Edge、Firefox、Safari 的最近两个桌面主版本 |
| 假设 | 使用方项目已配置 Blazor 所需的基础设施 |

---

# 3 功能需求

## 3.1 通用组件需求

> 本节需求与各组件的专用需求共同适用；专用需求明确限定适用组件时，以该限定为准。

```
REQ-F-001 🔴 [Actor: Application Developer]

Title
公共外观属性与附加属性

Description
所有 Astrea 组件必须允许应用开发者提供自定义 CSS 类、内联样式和附加 HTML 属性。组件文档必须说明这些属性作用于哪个可观察元素，并采用一致的名称冲突规则：自定义类与组件类同时保留；同名样式声明以应用提供的值为准；组件保证的状态和可访问性属性以组件值为准；其他附加属性使用应用提供的值。

Source
组件公共使用契约

Acceptance Criteria

AC1：CSS 类

Given 应用开发者提供一个或多个自定义 CSS 类
When 组件显示
Then 自定义类和组件自身用于表达外观或状态的类均保留

AC2：内联样式

Given 应用开发者提供的内联样式与组件默认样式包含同一 CSS 属性
When 组件显示
Then 该 CSS 属性采用应用提供的值，其他默认样式继续生效

AC3：普通附加属性

Given 应用开发者提供组件未单独声明且不与保证属性冲突的 HTML 属性
When 组件显示
Then 该属性和值出现在组件文档声明的目标元素上

AC4：保证属性冲突

Given 应用提供的附加属性与组件保证的状态或可访问性属性同名
When 组件显示
Then 使用组件为当前状态计算的值，且文档说明该冲突规则
```

```
REQ-F-002 🔴 [Actor: End User]

Title
适用组件的禁用状态

Description
具有禁用能力的交互组件在禁用时必须保持可见、不可触发交互，并向视觉用户和辅助技术表达禁用状态。非交互组件不要求提供禁用能力。

Source
AWComponent 经验教训：禁用状态不应通过隐藏元素实现

Acceptance Criteria

AC1：交互抑制

Given 一个支持禁用状态的组件处于禁用状态
When 终端用户尝试通过鼠标、触摸或键盘触发其主要操作
Then 组件不执行操作且不发送对应的事件通知

AC2：状态表达

Given 一个支持禁用状态的组件处于禁用状态
When 组件呈现给视觉用户和辅助技术
Then 组件保持可见并提供可辨识的禁用状态

AC3：适用范围

Given 一个没有交互或禁用语义的展示组件
When 应用开发者使用该组件
Then 组件不要求提供无意义的禁用状态
```

```
REQ-F-003 🔴 [Actor: Application Developer]

Title
稳定的状态更新

Description
组件在应用重新渲染或更新公开输入后，必须反映应用提供的最新状态，不得无故重置未被替换的用户可观察状态，也不得产生重复事件。

Source
组件状态一致性要求

Acceptance Criteria

AC1：应用更新

Given 组件已经呈现
When 应用开发者更新该组件的公开输入并触发重新渲染
Then 组件呈现与最新输入一致的结果

AC2：无关重新渲染

Given 组件状态未被应用更新
When 父级内容因无关原因重新渲染
Then 组件的用户可观察状态保持不变且不产生额外事件通知

AC3：重复更新

Given 应用连续提供相同的状态值
When 组件重新渲染
Then 组件不产生重复的用户操作通知
```

```
REQ-F-004 🟡 [Actor: Application Developer]

Title
主题定制

Description
组件的颜色、间距、圆角、字体和边框等视觉特征必须能够通过公开的 --ast-* 主题变量进行定制；未提供自定义值时使用完整且可用的默认主题。

Source
主题定制需求

Acceptance Criteria

AC1：默认主题

Given 应用未覆盖任何 Astrea 主题变量
When 组件渲染
Then 组件使用完整的默认主题且内容保持可读

AC2：主题覆盖

Given 应用覆盖一个已记录的 --ast-* 主题变量
When 相关组件渲染
Then 对应视觉特征使用覆盖值，其他视觉特征保持默认值

AC3：无效覆盖

Given 应用提供无效的主题变量值
When 浏览器无法采用该值
Then 组件仍可使用公开的回退值保持内容可见和可操作
```

```
REQ-F-005 🟡 [Actor: Application Developer]

Title
样式影响范围

Description
组件自带样式不得意外改变宿主应用中组件外部元素的视觉结果；应用按文档覆盖组件公开样式入口时，影响范围必须可预测。

Source
组件集成隔离要求

Acceptance Criteria

AC1：宿主隔离

Given 宿主页面包含 Astrea 组件和普通 HTML 元素
When 组件样式加载
Then 组件外部的普通 HTML 元素不会仅因 Astrea 样式而发生非预期变化

AC2：公开覆盖

Given 应用仅覆盖某一组件记录的公开样式入口
When 页面渲染
Then 覆盖结果限定在文档声明的组件或作用域内
```

```
REQ-F-006 🔴 [Actor: Assistive Technology User]

Title
语义与可访问名称

Description
每个组件必须向辅助技术暴露与其用途一致的语义。可交互组件和表达状态的组件必须具有可访问名称，并在状态变化后同步更新可访问性信息。

Source
WCAG 2.2 与 WAI-ARIA 语义要求

Acceptance Criteria

AC1：语义

Given 任意 Astrea 组件
When 辅助技术读取该组件
Then 获得与组件用途一致且不重复冲突的语义

AC2：可访问名称

Given 一个需要名称的交互组件或状态组件
When 辅助技术聚焦或读取该组件
Then 获得能够说明其用途的可访问名称

AC3：状态同步

Given 组件具有选中、展开、禁用、无效或进度等可观察状态
When 该状态发生变化
Then 辅助技术获得的状态信息与视觉状态一致
```

```
REQ-F-007 🔴 [Actor: Keyboard User]

Title
键盘可操作性

Description
所有可交互组件必须支持与其组件类型和交互模式一致的键盘操作。键盘用户必须能够到达、操作并离开组件，且不得陷入无退出路径的焦点状态。

Source
WCAG 2.2 键盘可访问性要求

Acceptance Criteria

AC1：焦点到达

Given 页面包含可交互的 Astrea 组件
When 键盘用户按顺序导航
Then 每个可用交互目标均能获得焦点，顺序与页面视觉阅读顺序一致且没有目标被跳过

AC2：组件操作

Given 键盘焦点位于可交互组件
When 用户使用该组件专用需求规定的按键
Then 组件产生该专用需求规定的用户可观察结果

AC3：焦点可见与退出

Given 键盘用户正在操作组件
When 焦点移动或用户离开组件
Then 当前焦点具有可见指示，且用户能够继续导航到组件外部
```

```
REQ-F-008 🔴 [Actor: Application Developer]

Title
外部状态同步

Description
组件公开显示的值、可见性、选择、展开和集合内容必须与应用提供的最新状态保持一致。终端用户操作产生状态变更请求时，应用必须能够收到一次明确通知。

Source
组件状态所有权与同步需求

Acceptance Criteria

AC1：外部状态更新

Given 组件已经显示一个公开状态
When 应用开发者提供新的状态值
Then 组件显示结果更新为新状态

AC2：用户状态操作

Given 终端用户执行会改变公开状态的操作
When 操作被组件接受
Then 应用收到一次对应的状态变更通知

AC3：应用决定最终状态

Given 组件发送状态变更通知
When 应用随后提供最终状态
Then 组件显示结果与应用提供的最终状态一致
```

```
REQ-F-009 🟡 [Actor: Application Developer]

Title
内置文本可配置

Description
组件提供的关闭、确认、取消、空数据、加载和错误等内置可见文本必须可由应用开发者替换，组件不得要求终端用户理解固定语言。

Source
国际化与应用集成需求

Acceptance Criteria

AC1：默认文本

Given 应用未提供替代文本
When 组件需要显示内置文本
Then 组件显示已记录的默认文本

AC2：替代文本

Given 应用为内置文本提供替代内容
When 对应内容显示
Then 视觉用户和辅助技术获得替代内容

AC3：无固定语言依赖

Given 应用替换全部用户可见的内置文本
When 终端用户操作组件
Then 组件功能不依赖默认文本所使用的语言
```

## 3.2 Button（按钮）

> 当前版本不包含专用加载或忙碌状态；该增强能力见 Deferred 的 REQ-F-080。

```
REQ-F-010 🔴 [Actor: End User]

Title
Button 基础呈现

Description
Button 必须显示应用提供的内容，并向视觉用户和辅助技术表达为可操作按钮。

Source
Button 基础能力

Acceptance Criteria

AC1：内容呈现

Given 应用为 Button 提供文本或其他可见内容
When Button 渲染
Then 所有提供的内容按原顺序显示且可用于识别按钮用途

AC2：按钮语义

Given Button 已渲染
When 视觉用户或辅助技术访问 Button
Then Button 被识别为一个可操作按钮
```

```
REQ-F-011 🔴 [Actor: End User]

Title
Button 点击事件

Description
终端用户通过鼠标、触摸或键盘激活可用 Button 时，应用必须收到一次点击通知。

Source
Button 交互需求

Acceptance Criteria

AC1：鼠标或触摸激活

Given Button 可用且已显示
When 终端用户单次点击或轻触 Button
Then 应用收到一次点击通知

AC2：键盘激活

Given 键盘焦点位于可用 Button
When 用户使用按钮的标准激活键
Then 应用收到一次点击通知且行为与点击一致

AC3：单次通知

Given 一次用户激活操作已被接受
When Button 完成该操作
Then 不会因同一次操作产生重复点击通知
```

```
REQ-F-012 🔴 [Actor: Application Developer]

Title
Button 表单行为

Description
应用开发者必须能够选择 Button 的普通、提交或重置行为。未明确选择时，Button 不得意外提交或重置所属表单。

Source
HTML 表单交互需求

Acceptance Criteria

AC1：默认行为

Given Button 位于表单内且应用未指定表单行为
When 终端用户激活 Button
Then Button 不会自动提交或重置表单

AC2：提交行为

Given 应用将 Button 配置为提交操作
When 终端用户激活 Button
Then 所属表单按宿主应用规则发起提交

AC3：重置行为

Given 应用将 Button 配置为重置操作
When 终端用户激活 Button
Then 所属表单恢复其可观察的初始值
```

```
REQ-F-013 🟡 [Actor: Application Developer]

Title
Button 视觉变体

Description
Button 必须提供 Primary、Secondary、Danger、Ghost 四种公开视觉变体。各变体必须能够被视觉用户辨识，且不得只依赖颜色传达危险或主要操作含义。

Source
Button 视觉层次需求

Acceptance Criteria

AC1：变体选择

Given 应用选择任一受支持的 Button 视觉变体
When Button 渲染
Then Button 呈现该变体记录的视觉结果

AC2：默认变体

Given 应用未选择 Button 视觉变体
When Button 渲染
Then Button 使用 Primary 变体

AC3：主题适配

Given 应用覆盖公开主题变量
When 不同 Button 变体渲染
Then 各变体保持可辨识且文本与状态保持可读
```

```
REQ-F-014 🟡 [Actor: Application Developer]

Title
Button 尺寸

Description
Button 必须提供 Small、Medium、Large 三种公开尺寸，并在改变尺寸时保持内容、焦点指示和可操作区域完整。

Source
Button 尺寸需求

Acceptance Criteria

AC1：尺寸选择

Given 应用选择任一受支持的 Button 尺寸
When Button 渲染
Then Button 使用该尺寸记录的高度、间距和字体比例

AC2：默认尺寸

Given 应用未选择 Button 尺寸
When Button 渲染
Then Button 使用 Medium 尺寸

AC3：内容完整

Given Button 包含文本和图标
When Button 以任一支持尺寸渲染
Then 内容不被裁剪且焦点指示完整可见
```

```
REQ-F-015 🔴 [Actor: End User]

Title
Button 禁用

Description
Button 禁用时必须保持可见但不可激活，不得提交或重置表单，也不得向应用发送点击通知。

Source
REQ-F-002

Acceptance Criteria

AC1：用户操作

Given Button 处于禁用状态
When 终端用户通过鼠标、触摸或键盘尝试激活
Then Button 不执行普通、提交或重置操作，应用不收到点击通知

AC2：状态表达

Given Button 处于禁用状态
When 视觉用户或辅助技术访问 Button
Then 能够识别 Button 当前不可用
```

```
REQ-F-016 🟡 [Actor: Application Developer]

Title
Button 图标

Description
应用开发者必须能够在 Button 文本之前提供图标内容。Astrea 不得要求应用使用特定图标字体或图标库。

Source
Button 图标需求

Acceptance Criteria

AC1：图标与文本

Given 应用同时提供图标和文本
When Button 渲染
Then 图标显示在文本之前并保持合理间距

AC2：装饰图标

Given 图标仅用于装饰且 Button 已有可访问文本
When 辅助技术读取 Button
Then 图标不会重复加入 Button 的可访问名称

AC3：无图标

Given 应用未提供图标
When Button 渲染
Then 文本布局不保留无意义的图标空位
```

```
REQ-F-017 🔴 [Actor: Assistive Technology User]

Title
Button 可访问性

Description
Button 必须具有可访问名称，暴露可用或禁用状态，并支持按钮的标准键盘操作。纯图标 Button 必须由应用提供能够说明操作目的的名称。

Source
WCAG 2.2 与 HTML 按钮语义

Acceptance Criteria

AC1：名称

Given Button 包含可见文本或应用提供的可访问名称
When 辅助技术读取 Button
Then 获得能够说明操作目的的名称

AC2：禁用状态

Given Button 处于禁用状态
When 键盘用户或辅助技术访问 Button
Then Button 不可激活且禁用状态可被识别

AC3：键盘激活

Given 键盘焦点位于可用 Button
When 用户按 Enter 或 Space
Then Button 执行与单次点击相同的操作
```

```
REQ-F-080 🟢 [Actor: Application Developer]

Title
Button 加载与忙碌状态

Description
应用可以使 Button 表达异步操作正在进行。忙碌期间 Button 保留可访问名称和可见反馈，不接受重复激活；应用结束忙碌状态后，Button 恢复其公开可用状态。

Source
Button 增强需求（Deferred）

Acceptance Criteria

AC1：进入忙碌状态

Given 可用 Button 已触发异步操作
When 应用将 Button 设置为忙碌状态
Then 用户能够识别操作正在进行且 Button 不接受新的激活

AC2：可访问名称

Given Button 处于忙碌状态
When 辅助技术读取 Button
Then 用户获得原操作目的和忙碌状态信息

AC3：重复操作

Given Button 处于忙碌状态
When 用户重复点击或使用键盘激活
Then 应用不收到新的点击通知

AC4：恢复

Given Button 处于忙碌状态
When 应用结束忙碌状态
Then Button 根据应用提供的禁用状态恢复为可用或禁用
```

## 3.3 Alert（警告提示）

```
REQ-F-018 🔴 [Actor: End User]

Title
Alert 类型

Description
Alert 必须提供 Info、Success、Warning、Error 四种类型。类型必须通过可辨识的文本、图标或其他非颜色线索表达，不得仅依赖颜色。

Source
Alert 状态反馈需求

Acceptance Criteria

AC1：类型呈现

Given 应用选择任一受支持的 Alert 类型
When Alert 显示
Then 视觉用户能够区分该类型且内容保持可读

AC2：辅助技术信息

Given Alert 类型对理解消息重要
When 辅助技术读取 Alert
Then 用户能够获得与该类型等效的信息
```

```
REQ-F-019 🔴 [Actor: Application Developer]

Title
Alert 内容

Description
Alert 必须显示应用提供的正文，并可以显示可选标题或组合内容。标题和正文顺序必须稳定，内容不得被关闭操作遮挡。

Source
Alert 内容需求

Acceptance Criteria

AC1：正文

Given 应用提供 Alert 正文
When Alert 显示
Then 正文完整可见

AC2：标题

Given 应用同时提供标题和正文
When Alert 显示
Then 标题显示在正文之前且两者均可由辅助技术访问

AC3：组合内容

Given 应用提供可交互的组合内容
When Alert 显示
Then 内容保持可操作且关闭操作不会遮挡内容
```

```
REQ-F-020 🔴 [Actor: End User]

Title
Alert 关闭操作

Description
Alert 必须提供具有可访问名称的关闭操作。终端用户激活关闭操作后，应用收到一次关闭请求；Alert 后续是否显示必须与应用提供的最新可见状态一致。

Source
Alert 关闭交互需求

Acceptance Criteria

AC1：关闭请求

Given Alert 可见且关闭操作可用
When 终端用户激活关闭操作
Then 应用收到一次关闭请求

AC2：状态一致

Given 应用收到关闭请求
When 应用提供新的可见状态
Then Alert 显示结果与该状态一致

AC3：可访问名称

Given 辅助技术读取 Alert 的关闭操作
When 关闭操作获得焦点
Then 用户获得能够说明“关闭此消息”用途的名称
```

```
REQ-F-021 🔴 [Actor: Application Developer]

Title
Alert 可见性

Description
应用开发者必须能够控制 Alert 是否显示。可见状态变化不得产生未经用户或应用请求的关闭通知。

Source
Alert 状态控制需求

Acceptance Criteria

AC1：显示

Given 应用提供可见状态
When Alert 渲染
Then Alert 内容和关闭操作显示

AC2：隐藏

Given 应用提供隐藏状态
When Alert 渲染
Then Alert 不显示且不占据页面布局空间

AC3：外部切换

Given Alert 已显示
When 应用将状态切换为隐藏
Then Alert 隐藏且不额外发送用户关闭请求
```

```
REQ-F-022 🟢 [Actor: Application Developer]

Title
Alert 自动关闭

Description
应用开发者可以选择让 Alert 在可配置时间后发送一次自动关闭请求。自动关闭默认不启用，且不得在用户正在阅读或操作 Alert 时使内容意外消失。

Source
Alert 增强需求（Deferred）

Acceptance Criteria

AC1：默认行为

Given 应用未启用自动关闭
When Alert 持续显示
Then Alert 不会因时间经过而请求关闭

AC2：超时请求

Given 应用启用自动关闭并设置有效持续时间
When 持续时间结束且用户未与 Alert 交互
Then 应用收到一次自动关闭请求

AC3：用户交互

Given 自动关闭计时正在进行
When 键盘焦点进入 Alert 或指针停留在 Alert 上
Then Alert 不会在该交互期间自动消失
```

```
REQ-F-023 🔴 [Actor: Assistive Technology User]

Title
Alert 可访问性

Description
Alert 必须向辅助技术表达消息内容和重要程度。Warning 与 Error 动态消息默认作为需要及时注意的消息播报；Info 与 Success 动态消息默认以不打断当前任务的方式播报。消息出现或已显示消息内容更新时不得无故移动当前键盘焦点，同一次可观察更新不得产生重复通知。

Source
WAI-ARIA Alert Pattern 与 WCAG 2.2

Acceptance Criteria

AC1：高优先消息

Given Warning 或 Error Alert 作为动态消息出现
When 辅助技术监测页面变化
Then 消息被及时识别或播报

AC2：普通状态消息

Given Info 或 Success Alert 作为动态消息出现
When 辅助技术监测页面变化
Then 消息以不打断当前任务的方式被识别或播报

AC3：焦点保持

Given 键盘焦点位于页面中的其他控件
When Alert 出现或已显示消息内容更新
Then 当前焦点不会仅因该变化而移动

AC4：关闭操作

Given Alert 允许关闭
When 键盘用户导航到关闭操作并激活
Then 应用收到一次关闭请求

AC5：已显示消息更新

Given Alert 已显示且应用更新正文、标题或重要程度
When 辅助技术监测该次更新
Then 用户获得最新消息和重要程度，组件不因同一次更新产生重复通知
```

## 3.4 Divider（分割线）

```
REQ-F-024 🔴 [Actor: End User]

Title
Divider 方向与语义

Description
Divider 必须支持水平和垂直方向。应用可以将 Divider 用作表达内容区域边界的语义分隔，也可以将其标记为纯视觉装饰；语义 Divider 必须向辅助技术表达分隔及方向，装饰 Divider 不得产生无信息价值的分隔语义。默认使用语义分隔模式。

Source
Divider 基础能力与 WAI-ARIA Separator Pattern

Acceptance Criteria

AC1：水平方向

Given 应用选择水平方向和语义分隔模式
When Divider 显示
Then 分隔线沿水平方向显示并向辅助技术表达水平方向

AC2：垂直方向

Given 应用选择垂直方向和语义分隔模式
When Divider 显示
Then 分隔线沿垂直方向显示并向辅助技术表达垂直方向

AC3：默认模式

Given 应用未指定方向或语义模式
When Divider 显示
Then Divider 使用水平方向并作为语义分隔供辅助技术识别

AC4：装饰模式

Given 应用将 Divider 标记为纯视觉装饰
When 辅助技术浏览页面
Then Divider 不被表达为内容分隔元素且不增加无信息价值的导航项
```

```
REQ-F-025 🟡 [Actor: Application Developer]

Title
Divider 视觉定制

Description
应用开发者必须能够定制 Divider 的颜色、粗细以及实线或虚线样式。定制结果不得使 Divider 嵌入内容不可读。

Source
Divider 视觉定制需求

Acceptance Criteria

AC1：视觉属性

Given 应用提供受支持的颜色、粗细和线型
When Divider 显示
Then 分隔线呈现对应视觉结果

AC2：默认外观

Given 应用未提供视觉定制
When Divider 显示
Then 使用默认主题外观

AC3：内容可读

Given Divider 包含嵌入内容
When 应用定制分隔线外观
Then 嵌入内容仍保持可读
```

```
REQ-F-026 🟡 [Actor: Application Developer]

Title
Divider 嵌入内容

Description
应用开发者可以在 Divider 中提供文本或其他可见内容。内容必须位于分隔区域中并保持正确阅读顺序。

Source
Divider 内容需求

Acceptance Criteria

AC1：文本内容

Given 应用提供文本内容
When Divider 显示
Then 文本显示在分隔区域中且分隔线不会穿过文字

AC2：组合内容

Given 应用提供其他可见内容
When Divider 显示
Then 内容完整显示并保持在页面阅读顺序中

AC3：无内容

Given 应用未提供嵌入内容
When Divider 显示
Then 分隔线连续显示且不保留空白内容区域
```

```
REQ-F-027 🟢 [Actor: End User]

Title
Divider 响应式表现

Description
Divider 在容器尺寸变化和受支持的页面缩放条件下必须保持方向、内容位置和边界完整，不得造成页面非预期溢出。

Source
响应式布局需求

Acceptance Criteria

AC1：容器尺寸变化

Given Divider 位于可调整尺寸的容器中
When 容器宽度或高度变化
Then Divider 重新适配可用空间且方向保持不变

AC2：页面缩放

Given 浏览器页面缩放至受支持范围
When Divider 显示
Then 分隔线和嵌入内容不被裁剪且不产生非预期滚动
```

## 3.5 Input（输入框）

```
REQ-F-028 🔴 [Actor: Application Developer]

Title
Input 文本输入类型

Description
Input 必须支持 Text、Email、Password、Search、Tel、Url 六种文本输入类型。未列出的 Number、Checkbox、Radio 等输入能力不属于本版 Input 范围。

Source
首版表单组件范围决策

Acceptance Criteria

AC1：受支持类型

Given 应用选择任一受支持的 Input 类型
When Input 渲染
Then 终端用户获得与该类型一致的输入行为

AC2：默认类型

Given 应用未指定 Input 类型
When Input 渲染
Then Input 使用 Text 类型

AC3：范围明确

Given 应用需要未列出的输入类型
When 查阅本版 Input 文档
Then 文档明确说明该类型不在本版支持范围内
```

```
REQ-F-029 🔴 [Actor: Application Developer]

Title
Input 值同步

Description
Input 显示的文本值必须与应用提供的最新值保持一致。终端用户每次完成并提交一次实际文本变化时，应用必须通过 REQ-F-030 规定的同一条文本变化通知收到最新值；不得为同一次文本变化另外发送第二条值同步通知。无关的父级重新渲染不得回退已由应用接受的输入。未提供文本或文本被清空时，Input 必须显示为空。

Source
Input 数据绑定行为

Acceptance Criteria

AC1：初始值

Given 应用为 Input 提供初始文本
When Input 首次显示
Then Input 显示该文本

AC2：用户输入

Given Input 可编辑
When 终端用户修改文本
Then 应用通过一条文本变化通知收到当前输入内容，不再收到第二条值同步通知

AC3：外部更新

Given Input 已显示一个值
When 应用提供不同的新值
Then Input 显示新值且不把该外部更新报告为用户输入

AC4：无关重新渲染

Given 应用已接受终端用户输入且未提供替代值
When 父级内容因无关原因重新渲染
Then Input 不回退到更早的值

AC5：无初始文本

Given 应用未提供初始文本
When Input 首次显示
Then Input 显示为空且不产生文本变化通知

AC6：用户清空

Given Input 当前包含文本且可编辑
When 终端用户清空全部文本
Then Input 显示为空且应用只收到一条包含空文本的变化通知
```

```
REQ-F-030 🟡 [Actor: Application Developer]

Title
Input 交互通知

Description
应用开发者必须能够获知 Input 的文本变化、Enter 激活和失去焦点操作。每次用户操作最多产生一次对应通知；文本变化通知同时承担 REQ-F-029 的最新值同步，不是额外于值同步通知的第二条通知。键盘输入、粘贴、浏览器自动填充和输入法完成输入后均遵守相同的最终文本通知契约。

Source
Input 交互需求

Acceptance Criteria

AC1：文本变化

Given Input 可编辑
When 终端用户改变当前文本
Then 应用只收到一条文本变化通知，且该通知包含最新文本

AC2：Enter

Given 键盘焦点位于 Input
When 用户按 Enter
Then 应用收到一次 Enter 操作通知

AC3：失去焦点

Given Input 已获得焦点
When 焦点移动到 Input 外部
Then 应用收到一次失去焦点通知

AC4：其他文本输入路径

Given Input 可编辑
When 用户通过粘贴、浏览器自动填充或输入法完成一次实际文本变化
Then 应用按与键盘输入相同的契约收到一次包含最终文本的变化通知，输入法尚未完成的组合文本不产生业务文本变化通知
```

```
REQ-F-031 🟡 [Actor: End User]

Title
Input 占位提示

Description
Input 在值为空时可以显示应用提供的占位提示，并允许应用声明受支持的浏览器自动填充目的。占位提示不得代替持久可见标签或可访问名称。

Source
Input 提示需求

Acceptance Criteria

AC1：空值

Given Input 的值为空且应用提供占位提示
When Input 显示
Then 占位提示可见

AC2：有值

Given Input 已包含文本
When Input 显示
Then 占位提示不遮挡或替代当前文本

AC3：可访问名称

Given Input 只有占位提示但没有标签或其他可访问名称
When 辅助技术读取 Input
Then 该用法不满足组件记录的可访问性使用条件

AC4：浏览器自动填充

Given 应用为 Input 声明受支持的自动填充目的
When 浏览器提供匹配的已保存值
Then 终端用户可以识别并接受浏览器提供的值
```

```
REQ-F-032 🔴 [Actor: End User]

Title
Input 禁用与只读

Description
Input 必须分别支持禁用和只读状态。禁用 Input 不可编辑且不参与用户提交操作；只读 Input 不可编辑但其值仍可阅读、选择并按宿主表单规则提交。

Source
HTML 输入状态语义

Acceptance Criteria

AC1：禁用状态

Given Input 处于禁用状态
When 终端用户尝试编辑或激活 Input
Then 值不变化且应用不收到输入通知

AC2：只读状态

Given Input 处于只读状态
When 终端用户尝试修改文本
Then 值不变化，但用户仍可聚焦并读取当前文本

AC3：状态辨识

Given Input 处于禁用或只读状态
When 视觉用户或辅助技术访问 Input
Then 用户能够识别相应状态
```

```
REQ-F-033 🔴 [Actor: Application Developer]

Title
Input 验证反馈

Description
Input 在 Blazor 表单中使用时，其值变化、有效性和错误消息必须能够参与宿主表单的验证流程；脱离表单使用时不得因缺少表单上下文而失败。

Source
表单验证集成需求

Acceptance Criteria

AC1：无效状态

Given 宿主应用将 Input 对应字段标记为无效并提供错误消息
When Input 显示验证结果
Then 视觉用户看到错误反馈，辅助技术能够识别无效状态及关联的错误消息

AC2：恢复有效

Given Input 当前显示错误状态
When 宿主应用将字段更新为有效
Then 错误反馈被移除且辅助技术不再读取旧错误

AC3：独立使用

Given Input 不位于宿主表单验证上下文中
When 终端用户输入有效文本
Then Input 正常更新并通知应用，不发生未处理错误
```

```
REQ-F-034 🟢 [Actor: Application Developer]

Title
Input 前后内容

Description
应用开发者可以在 Input 的文本输入区域之前或之后提供图标或其他辅助内容。Astrea 不得要求使用特定图标技术，辅助内容不得遮挡输入文本或破坏可访问名称。

Source
Input 增强需求

Acceptance Criteria

AC1：前置内容

Given 应用提供前置内容
When Input 显示
Then 前置内容位于输入区域之前且输入文本完整可见

AC2：后置内容

Given 应用提供后置内容
When Input 显示
Then 后置内容位于输入区域之后且输入文本完整可见

AC3：装饰内容

Given 前后内容仅用于装饰
When 辅助技术读取 Input
Then 装饰内容不会重复加入 Input 的可访问名称
```

```
REQ-F-035 🟡 [Actor: End User]

Title
Input 自动聚焦

Description
应用可以请求 Input 在首次成为可交互内容后获得焦点。自动聚焦只执行一次，不得在后续重新渲染时反复抢占用户焦点。

Source
Input 焦点需求

Acceptance Criteria

AC1：请求自动聚焦

Given 应用请求 Input 自动聚焦且 Input 可用
When Input 首次成为可交互内容
Then Input 获得键盘焦点

AC2：后续渲染

Given Input 已完成首次自动聚焦
When 页面因状态更新重新渲染
Then Input 不会仅因重新渲染再次抢占焦点

AC3：不可用状态

Given Input 在首次显示时禁用或不可见
When 页面完成显示
Then 自动聚焦不会把焦点移至不可操作目标
```

```
REQ-F-036 🔴 [Actor: Assistive Technology User]

Title
Input 标签与错误关联

Description
Input 必须能够与可见标签建立可访问性关联。存在帮助文本或验证错误时，辅助技术必须能够在读取 Input 时获得相关说明。

Source
WCAG 2.2 表单标签与错误识别要求

Acceptance Criteria

AC1：可见标签

Given 页面为 Input 提供可见标签
When 辅助技术聚焦 Input
Then 读取到与该标签一致的可访问名称

AC2：帮助文本

Given Input 具有相关帮助文本
When 辅助技术读取 Input
Then 能够获得帮助文本而不替代 Input 名称

AC3：错误消息

Given Input 处于无效状态并显示错误消息
When 辅助技术读取 Input
Then 能够识别无效状态并读取关联错误消息
```

## 3.6 Label（标签）

```
REQ-F-037 🔴 [Actor: Application Developer]

Title
Label 内容与控件关联

Description
Label 必须显示应用提供的标签内容，并能够与实际表单控件建立明确关联。终端用户点击 Label 时，关联控件获得该控件类型规定的焦点或激活行为。

Source
Label 基础能力与 HTML 标签语义

Acceptance Criteria

AC1：标签内容

Given 应用提供文本或组合标签内容
When Label 显示
Then 内容完整可见且用途清晰

AC2：控件关联

Given Label 与一个表单控件关联
When 终端用户点击 Label
Then 关联控件获得焦点或执行其标准标签激活行为

AC3：空内容

Given 应用未提供标签内容
When Label 显示
Then 不生成误导性的空白标签
```

```
REQ-F-038 🟡 [Actor: Application Developer]

Title
Label 必填指示

Description
Label 可以显示必填指示。必填状态不得仅通过颜色表达，且必填指示必须与关联控件的实际验证要求保持一致。

Source
表单必填状态表达需求

Acceptance Criteria

AC1：必填字段

Given 应用将关联字段声明为必填
When Label 显示
Then 用户能够通过非颜色线索识别必填状态

AC2：非必填字段

Given 关联字段不是必填
When Label 显示
Then 不显示误导性的必填指示
```

```
REQ-F-039 🔴 [Actor: Assistive Technology User]

Title
Label 可访问性

Description
Label 与表单控件关联后，必须成为该控件可访问名称的来源。必填指示不得产生重复或无意义播报。

Source
WCAG 2.2 表单标签要求

Acceptance Criteria

AC1：可访问名称

Given Label 与一个表单控件关联
When 辅助技术读取该控件
Then 获得与 Label 内容一致的可访问名称

AC2：唯一关联

Given 页面存在多个标签和表单控件
When 辅助技术读取任一控件
Then 获得其自身标签而不是其他控件标签

AC3：必填状态

Given Label 显示必填指示且关联控件为必填
When 辅助技术读取该控件
Then 用户能够识别必填状态且不会收到重复无意义内容
```

## 3.7 Modal（对话框）

> 当前 Active 基线只保证页面同时显示一个 Modal；多个或嵌套 Modal 见 Deferred 的 REQ-F-081。

```
REQ-F-040 🔴 [Actor: Application Developer]

Title
Modal 可见状态与关闭请求

Description
应用开发者必须能够控制 Modal 是否显示。终端用户通过关闭按钮、取消按钮、Escape 或允许的遮罩操作请求关闭时，应用必须只收到一条关闭通知，并能够识别请求途径。取消按钮使用途径为“取消”的关闭通知，不另外发送第二条取消通知。

Source
Modal 状态与事件契约

Acceptance Criteria

AC1：显示状态

Given 应用提供显示状态
When Modal 渲染
Then Modal 内容和遮罩显示

AC2：隐藏状态

Given 应用提供隐藏状态
When Modal 渲染
Then Modal 不显示且不阻挡页面交互

AC3：关闭请求

Given Modal 已显示
When 终端用户通过关闭按钮、Escape 或允许的遮罩操作请求关闭
Then 应用只收到一条关闭通知并能够识别对应途径

AC4：取消关闭请求

Given Modal 已显示取消操作
When 终端用户激活取消操作
Then 应用只收到一条途径为“取消”的关闭通知，不收到额外取消通知

AC5：最终状态

Given 应用收到关闭通知
When 应用提供最终可见状态
Then Modal 显示结果与该状态一致
```

```
REQ-F-041 🟡 [Actor: Application Developer]

Title
Modal 尺寸预设

Description
Modal 必须提供 Small、Medium、Large 三种公开尺寸。尺寸变化不得遮挡标题、关闭操作或正文，内容超过可用高度时用户能够访问全部内容。

Source
Modal 尺寸需求

Acceptance Criteria

AC1：尺寸选择

Given 应用选择任一支持的 Modal 尺寸
When Modal 显示
Then 对话框使用对应尺寸并保持在可用视口内

AC2：默认尺寸

Given 应用未指定 Modal 尺寸
When Modal 显示
Then 使用 Medium 尺寸

AC3：长内容

Given Modal 内容超过当前可用高度
When Modal 显示
Then 用户可以访问全部内容且标题与关闭路径仍可到达
```

```
REQ-F-042 🔴 [Actor: Application Developer]

Title
Modal 内容区域

Description
应用开发者必须能够分别提供 Modal 的标题、正文和页脚内容。未提供某一可选区域时，不得显示无意义空白区域。

Source
Modal 内容组织需求

Acceptance Criteria

AC1：完整内容

Given 应用提供标题、正文和页脚内容
When Modal 显示
Then 三个区域按标题、正文、页脚顺序完整显示

AC2：可选区域

Given 应用未提供页脚内容
When Modal 显示
Then 不显示无意义的空白页脚区域

AC3：交互内容

Given 正文或页脚包含可交互内容
When 键盘用户导航 Modal
Then 所有可用目标按合理顺序获得焦点
```

```
REQ-F-043 🔴 [Actor: End User]

Title
Modal 确认与取消操作

Description
Modal 可以显示确认和取消操作。确认操作只产生一条确认通知；取消操作只产生 REQ-F-040 规定的、途径为“取消”的关闭通知，不产生独立取消通知。是否关闭 Modal 由应用随后提供的可见状态决定。

Source
Modal 操作需求

Acceptance Criteria

AC1：确认

Given Modal 显示确认操作
When 终端用户激活确认操作
Then 应用只收到一条确认通知，不同时收到关闭通知

AC2：取消

Given Modal 显示取消操作
When 终端用户激活取消操作
Then 应用只收到一条途径为“取消”的关闭通知，不收到独立取消通知

AC3：操作文本

Given 应用提供自定义确认或取消文本
When Modal 显示操作区
Then 视觉用户和辅助技术获得自定义文本

AC4：状态一致

Given 应用收到确认通知或关闭通知
When 应用提供最终可见状态
Then Modal 显示结果与该状态一致
```

```
REQ-F-044 🟡 [Actor: End User]

Title
Modal 遮罩关闭

Description
应用开发者可以决定点击 Modal 外部遮罩是否产生关闭请求。点击 Modal 内容区域不得被误判为遮罩点击。

Source
Modal 遮罩交互需求

Acceptance Criteria

AC1：允许遮罩关闭

Given 应用允许遮罩关闭且 Modal 已显示
When 终端用户点击 Modal 内容区域之外的遮罩
Then 应用收到一次来源为遮罩的关闭通知

AC2：禁止遮罩关闭

Given 应用禁止遮罩关闭
When 终端用户点击遮罩
Then Modal 保持显示且应用不收到遮罩关闭通知

AC3：内容点击

Given Modal 已显示
When 终端用户点击 Modal 内容区域
Then 不产生遮罩关闭通知
```

```
REQ-F-045 🔴 [Actor: Keyboard User]

Title
Modal Escape 关闭

Description
Modal 显示时，键盘用户按 Escape 必须产生一次来源为 Escape 的关闭请求。Modal 隐藏后不再处理该关闭操作。

Source
WAI-ARIA Modal Dialog Pattern

Acceptance Criteria

AC1：显示时按 Escape

Given Modal 已显示
When 键盘用户按 Escape
Then 应用收到一次来源为 Escape 的关闭通知

AC2：隐藏后按 Escape

Given Modal 已隐藏
When 用户按 Escape
Then 该 Modal 不产生关闭通知

AC3：单次通知

Given 用户单次按下 Escape
When Modal 接受该操作
Then 只产生一次关闭通知
```

```
REQ-F-046 🔴 [Actor: Keyboard User]

Title
Modal 焦点与背景交互

Description
Modal 显示后，焦点必须进入 Modal，并在 Modal 显示期间限制在其可操作内容内；背景内容不可交互。Modal 隐藏后，焦点必须返回打开它的元素；若该元素可能在关闭前消失，应用必须提供仍然存在且可聚焦的恢复目标。背景交互和滚动随后恢复。

Source
WAI-ARIA Modal Dialog Pattern 与 WCAG 2.2

Acceptance Criteria

AC1：应用指定初始焦点

Given 应用提供一个位于 Modal 内且可聚焦的初始目标
When Modal 完成显示
Then 焦点位于该目标

AC2：默认初始焦点

Given 应用未提供初始目标
When Modal 完成显示
Then 焦点位于第一个可操作目标；没有可操作目标时位于可供辅助技术读取标题和正文的对话框区域

AC3：焦点限制

Given Modal 已显示
When 用户使用 Tab 或 Shift+Tab 导航
Then 焦点在 Modal 的可操作内容之间循环且不会进入背景内容

AC4：背景交互

Given Modal 已显示
When 用户尝试操作或滚动被遮罩的背景内容
Then 背景内容不响应交互且页面不会在背景中滚动

AC5：关闭恢复

Given 打开 Modal 的元素仍然存在且可聚焦
When Modal 隐藏
Then 焦点返回该元素，背景交互和滚动恢复

AC6：替代恢复目标

Given 打开 Modal 的元素将在关闭前消失
When 应用提供的替代恢复目标仍然存在且 Modal 隐藏
Then 焦点返回替代目标，背景交互和滚动恢复
```

```
REQ-F-047 🔴 [Actor: Assistive Technology User]

Title
Modal 可访问性语义

Description
Modal 必须向辅助技术表达为模态对话框，并具有可访问名称。标题存在时应作为名称来源；没有可见标题时，应用必须能够提供等效名称。

Source
WAI-ARIA Modal Dialog Pattern

Acceptance Criteria

AC1：对话框语义

Given Modal 已显示
When 辅助技术读取 Modal
Then 识别为模态对话框

AC2：名称

Given Modal 具有可见标题或应用提供的替代名称
When 辅助技术进入 Modal
Then 读取能够说明对话框用途的名称

AC3：正文说明

Given Modal 正文包含理解任务所必需的简短说明
When 辅助技术进入 Modal
Then 用户能够访问该说明且不会被迫一次性读取不适合线性播报的长内容

AC4：关闭操作

Given Modal 提供关闭操作
When 辅助技术聚焦该操作
Then 获得能够说明“关闭对话框”用途的名称
```

```
REQ-F-081 🟢 [Actor: Application Developer]

Title
Modal 多实例与嵌套

Description
应用可以同时管理多个或嵌套 Modal。只有最上层 Modal 接受键盘和关闭操作；关闭最上层 Modal 后，焦点与背景交互必须恢复到其直接下层 Modal，而不是越过仍然显示的 Modal。

Source
Modal 增强需求（Deferred）

Acceptance Criteria

AC1：最上层交互

Given 页面同时显示多个 Modal
When 用户按 Escape、使用 Tab 或点击可关闭遮罩
Then 只有最上层 Modal 响应该操作

AC2：下层隔离

Given 一个 Modal 上方仍显示另一个 Modal
When 用户尝试操作下层 Modal 或页面背景
Then 下层 Modal 和页面背景不响应交互

AC3：关闭恢复

Given 最上层 Modal 由另一个 Modal 内的操作打开
When 最上层 Modal 隐藏
Then 焦点返回直接下层 Modal 中的打开位置且焦点仍受该 Modal 约束

AC4：最后一个关闭

Given 页面只剩最后一个可见 Modal
When 该 Modal 隐藏
Then 按 REQ-F-046 恢复页面焦点、背景交互和滚动

AC5：非嵌套多实例恢复

Given 多个独立打开的 Modal 同时显示
When 最上层 Modal 隐藏
Then 焦点返回下一层 Modal 中此前聚焦的目标；该目标不存在时使用该 Modal 按 REQ-F-046 确定的默认初始焦点
```

## 3.8 Progress（进度条）

```
REQ-F-048 🔴 [Actor: End User]

Title
Progress 确定进度

Description
Progress 必须根据应用提供的当前值、最小值和最大值显示确定进度。当前值变化后，视觉结果和辅助技术获得的进度信息必须同步更新。

Source
Progress 基础能力

Acceptance Criteria

AC1：范围内值

Given 最小值小于最大值且当前值位于范围内
When Progress 显示
Then 填充比例与当前值在该范围中的位置一致

AC2：值更新

Given Progress 已显示
When 应用提供新的当前值
Then 视觉填充和辅助技术进度同步更新

AC3：默认范围

Given 应用未提供最小值和最大值
When Progress 显示
Then 使用 0 到 100 的默认范围
```

```
REQ-F-049 🔴 [Actor: Application Developer]

Title
Progress 边界处理

Description
Progress 在确定进度模式下必须以确定且已记录的方式处理超出范围或无效范围输入，不得显示超出 0% 到 100% 的填充，也不得产生未处理错误。

Source
Progress 边界行为需求

Acceptance Criteria

AC1：低于最小值

Given Progress 处于确定模式且当前值低于最小值
When Progress 显示
Then 视觉进度按 0% 显示，辅助技术获得的当前值不低于最小值

AC2：高于最大值

Given Progress 处于确定模式且当前值高于最大值
When Progress 显示
Then 视觉进度按 100% 显示，辅助技术获得的当前值不高于最大值

AC3：无效范围

Given Progress 处于确定模式且最大值小于或等于最小值
When Progress 显示
Then 组件显示 0% 进度、保持有效的可访问状态且不产生未处理错误
```

```
REQ-F-050 🟡 [Actor: End User]

Title
Progress 百分比文本

Description
应用可以在确定进度模式下选择显示百分比文本。百分比按当前有效范围计算并四舍五入为最接近的整数；不确定进度模式遵守 REQ-F-076。

Source
Progress 文本反馈需求

Acceptance Criteria

AC1：显示百分比

Given Progress 处于确定模式，应用启用百分比文本且进度为 45.6%
When Progress 显示
Then 可见文本为 46%

AC2：隐藏百分比

Given Progress 处于确定模式且应用未启用百分比文本
When Progress 显示
Then 不显示额外百分比文本

AC3：边界百分比

Given Progress 处于确定模式且当前值低于最小值或高于最大值
When 百分比文本显示
Then 文本分别限制为 0% 或 100%
```

```
REQ-F-051 🟡 [Actor: Application Developer]

Title
Progress 视觉状态

Description
Progress 必须提供 Default、Success、Warning、Error 四种视觉状态。状态需要通过颜色以外的文本、图标或可访问信息传达。

Source
Progress 状态反馈需求

Acceptance Criteria

AC1：状态选择

Given 应用选择任一支持的 Progress 状态
When Progress 显示
Then 呈现对应视觉状态

AC2：默认状态

Given 应用未选择状态
When Progress 显示
Then 使用 Default 状态

AC3：非颜色表达

Given Progress 状态对理解任务结果重要
When 视觉用户或辅助技术访问 Progress
Then 用户能够通过颜色以外的信息识别该状态
```

```
REQ-F-052 🔴 [Actor: Assistive Technology User]

Title
Progress 可访问性

Description
Progress 必须具有可访问名称；在确定进度模式下向辅助技术提供有效的最小值、最大值和当前值，在不确定模式下遵守 REQ-F-076。显示百分比文本时，视觉文本与辅助技术获得的进度不得矛盾。

Source
WAI-ARIA Progressbar 语义与 WCAG 2.2

Acceptance Criteria

AC1：名称与角色

Given 应用为 Progress 提供可见标签或替代名称
When 辅助技术读取 Progress
Then 识别为进度指示并获得该名称

AC2：范围信息

Given Progress 显示确定进度
When 辅助技术读取 Progress
Then 获得有效的最小值、最大值和当前值

AC3：同步

Given 应用更新当前值
When Progress 重新显示
Then 辅助技术获得的值与视觉进度和百分比文本一致
```

```
REQ-F-076 🟡 [Actor: End User]

Title
Progress 不确定进度

Description
应用无法提供可计算进度值时，可以显式使 Progress 显示不确定进度。该模式必须表达操作仍在进行，但不得显示或向辅助技术提供虚假的当前百分比。只有应用关闭不确定模式并提供有效范围和值后，Progress 才切换为确定进度。

Source
未知总量任务的进度反馈需求

Acceptance Criteria

AC1：不确定状态

Given 应用启用不确定进度
When Progress 显示
Then 用户能够识别操作仍在进行且界面不显示确定完成比例

AC2：辅助技术信息

Given Progress 处于不确定模式
When 辅助技术读取 Progress
Then 获得进度指示和可访问名称，但不获得虚假的当前完成值或百分比

AC3：百分比文本

Given Progress 处于不确定模式且应用启用了百分比文本
When Progress 显示
Then 不显示百分比文本

AC4：切换为确定进度

Given Progress 正处于不确定模式
When 应用关闭不确定模式并提供有效的最小值、最大值和当前值
Then Progress 按 REQ-F-048 显示确定进度且视觉与辅助技术信息同步更新

AC5：值更新不改变模式

Given Progress 处于不确定模式
When 应用只更新最小值、最大值或当前值但未关闭不确定模式
Then Progress 保持不确定状态且不显示确定百分比
```

## 3.9 Select（下拉选择）

```
REQ-F-053 🔴 [Actor: End User]

Title
Select 选项与单选

Description
Select 必须显示应用提供的选项，并允许终端用户从可用选项中选择一个值。选项显示顺序必须与应用提供的顺序一致。

Source
Select 基础选择需求

Acceptance Criteria

AC1：选项显示

Given 应用提供多个选项
When Select 显示
Then 所有选项按提供顺序可供浏览

AC2：单项选择

Given Select 包含多个可用选项
When 终端用户选择其中一项
Then 该项成为当前选项且应用收到一次新值通知

AC3：空集合

Given 应用未提供选项或提供空集合
When Select 显示
Then 不出现虚假选项且组件保持稳定可访问
```

```
REQ-F-054 🔴 [Actor: Application Developer]

Title
Select 值与选项同步

Description
Select 的当前值必须与应用提供的最新值保持一致。选项集合更新后，如果当前值不再对应任何选项，Select 不得自行修改应用提供的值或发送值变化通知；界面不得继续显示已移除选项，并显示占位提示或无选择状态。在 Blazor 表单中使用时，Select 能够参与宿主表单验证。

Source
Select 状态与表单集成需求

Acceptance Criteria

AC1：用户选择

Given Select 包含可用选项
When 终端用户选择不同选项
Then 应用收到一次新值通知

AC2：外部值更新

Given Select 已显示
When 应用提供另一个存在于选项集合中的值
Then 对应选项显示为当前选项且不发送用户值变化通知

AC3：当前选项被移除

Given 当前值对应的选项被应用移除
When Select 接收新选项集合
Then 已移除选项不再显示为当前选择；存在占位提示时显示该提示，否则显示无选择状态
And 应用提供的当前值保持不变，应用不收到值变化通知

AC4：重新出现

Given 当前值暂时不对应任何选项
When 应用再次提供包含该值的选项集合
Then 对应选项恢复为当前选项且不发送用户值变化通知

AC5：表单验证

Given 宿主表单将 Select 对应字段标记为无效
When Select 显示验证结果
Then 视觉用户和辅助技术能够识别无效状态及关联错误消息
```

```
REQ-F-055 🟡 [Actor: End User]

Title
Select 占位选项

Description
Select 可以在尚未选择有效值时显示占位提示。占位提示不得被误认为有效业务选项，且不得代替 Select 的可访问名称。

Source
Select 提示需求

Acceptance Criteria

AC1：未选择

Given 应用提供占位提示且当前没有有效选择
When Select 显示
Then 占位提示可见但不作为有效业务值提交

AC2：已有选择

Given Select 已具有有效选择
When Select 显示
Then 当前选项显示且占位提示不替代当前值

AC3：名称

Given Select 只有占位提示但没有标签或替代名称
When 辅助技术读取 Select
Then 该用法不满足组件记录的可访问性使用条件
```

```
REQ-F-056 🔴 [Actor: End User]

Title
Select 禁用状态

Description
Select 处于禁用状态时必须保持可见，不允许改变选择，也不发送值变化通知，并向视觉用户和辅助技术表达禁用状态。

Source
Select 禁用交互需求

Acceptance Criteria

AC1：禁止选择

Given Select 处于禁用状态
When 终端用户尝试打开或改变选择
Then 当前值不变且应用不收到值变化通知

AC2：状态表达

Given Select 处于禁用状态
When 视觉用户或辅助技术访问 Select
Then 能够识别禁用状态
```

```
REQ-F-057 🟡 [Actor: Application Developer]

Title
Select 自定义选项内容

Description
应用开发者可以定制选项的可见内容。自定义内容不得破坏选项顺序、可访问名称、选中状态和键盘操作，也不得要求所有受支持浏览器采用不一致的交互方式。

Source
Select 展示扩展需求

Acceptance Criteria

AC1：自定义显示

Given 应用为选项提供自定义可见内容
When Select 显示选项
Then 每个选项显示对应内容

AC2：可访问名称

Given 自定义内容包含装饰元素
When 辅助技术读取选项
Then 获得清晰且不重复的选项名称

AC3：兼容行为

Given Select 在任一受支持浏览器中显示自定义选项
When 终端用户使用鼠标或键盘选择
Then 选择结果与未定制选项一致
```

```
REQ-F-058 🟢 [Actor: End User]

Title
Select 多选

Description
应用可以启用多选模式。终端用户能够选择和取消选择多个选项，应用收到与当前选择集合一致的更新结果。

Source
Select 增强需求（Deferred）

Acceptance Criteria

AC1：选择多个选项

Given Select 处于多选模式并包含多个可用选项
When 终端用户选择多个选项
Then 所有选中项具有可辨识状态且应用收到当前选择集合

AC2：取消选择

Given 多选 Select 已有多个选中项
When 用户取消其中一项
Then 仅该项取消选中且应用收到更新后的集合

AC3：外部集合更新

Given 多选 Select 已显示
When 应用提供新的选择集合
Then 显示的选中状态与该集合一致
```

```
REQ-F-059 🟢 [Actor: End User]

Title
Select 搜索过滤

Description
应用可以启用选项搜索。终端用户输入查询后，只显示匹配选项；清除查询后恢复完整选项列表，已有有效选择不得无故丢失。

Source
Select 增强需求（Deferred）

Acceptance Criteria

AC1：过滤

Given 搜索已启用且存在多个选项
When 终端用户输入查询文本
Then 只显示与查询匹配的选项并提供结果数量或无结果反馈

AC2：清除搜索

Given 选项列表已被过滤
When 用户清除查询文本
Then 完整选项列表恢复

AC3：保留选择

Given 当前选择在过滤结果中暂时不可见
When 用户修改或清除查询
Then 该有效选择不会仅因过滤而丢失
```

```
REQ-F-060 🔴 [Actor: Keyboard User]

Title
Select 可访问性与键盘操作

Description
Select 必须具有可访问名称，并向辅助技术表达当前值、选项状态、展开状态和禁用状态。键盘操作必须与组件呈现的选择交互模式一致。

Source
WCAG 2.2、HTML Select 与 WAI-ARIA 选择模式

Acceptance Criteria

AC1：名称和当前值

Given Select 具有可见标签或替代名称
When 辅助技术读取 Select
Then 获得名称和当前选择值

AC2：键盘选择

Given 键盘焦点位于可用 Select
When 用户使用该选择模式规定的方向键、确认键和退出键
Then 能够浏览、选择或取消操作且不会陷入组件

AC3：状态同步

Given Select 的当前值、展开状态或禁用状态发生变化
When 辅助技术再次读取 Select
Then 获得与视觉状态一致的信息
```

```
REQ-F-075 🔴 [Actor: Application Developer]

Title
Select 选项值唯一性

Description
同一 Select 选项集合中的值必须唯一，以便当前值能够唯一对应一个选项。重复值属于无效应用输入；组件必须拒绝整个歧义集合，保留上一份有效选项集合和选择，并向应用提供一次已记录的错误结果，不得发送用户值变化通知。首次收到歧义集合时显示无选择状态。

Source
Select 选项身份边界需求

Acceptance Criteria

AC1：唯一值

Given 应用提供值互不重复的选项集合
When Select 根据当前值显示选择
Then 当前值唯一对应一个选项

AC2：首次收到重复值

Given Select 尚未接受过有效选项集合且应用提供重复值
When Select 接收该集合
Then 组件拒绝该集合、显示无选择状态并向应用报告一次错误，不发送值变化通知

AC3：有效集合更新为重复值

Given Select 已显示有效选项集合和当前选择
When 应用提供包含重复值的新集合
Then 组件保留上一有效集合和选择并向应用报告一次错误，不发送值变化通知

AC4：恢复有效集合

Given Select 已拒绝一个重复值集合
When 应用提供值互不重复的新集合
Then 组件接受并显示新集合，当前选择按 REQ-F-054 与应用提供的值同步
```

```
REQ-F-083 🔴 [Actor: End User]

Title
Select 单个选项禁用

Description
应用可以禁用单个 Select 选项。禁用选项保持可见并向视觉用户和辅助技术表达禁用状态，但不能由终端用户选择。当前选项随后被禁用时继续显示，直到应用提供其他值；该外部状态更新不发送用户值变化通知。

Source
Select 单项可用状态需求

Acceptance Criteria

AC1：浏览禁用选项

Given 一个未选中的选项被应用标记为禁用
When 终端用户浏览该选项
Then 选项保持可见且视觉用户和辅助技术能够识别禁用状态

AC2：尝试选择

Given 一个未选中的选项处于禁用状态
When 终端用户尝试选择该选项
Then 当前值不变且应用不收到值变化通知

AC3：当前选项被禁用

Given 当前选项随后被应用标记为禁用
When Select 接收更新后的选项集合
Then 该选项仍显示为当前值并表达禁用状态，且不发送用户值变化通知

AC4：外部替换值

Given 当前选项处于禁用状态
When 应用提供另一个存在且可用的选项值
Then 新选项显示为当前值且不把该外部更新报告为用户选择
```

## 3.10 Tree（树）

```
REQ-F-061 🔴 [Actor: Application Developer]

Title
Tree 节点数据

Description
应用开发者必须能够为 Tree 提供层级节点。每个节点必须具有稳定身份、可见标签和子节点关系，并可提供禁用、展开或选中等公开状态。禁用节点仍须能够通过 Tree 的方向键导航被访问和识别，但不能被选择、展开或激活。

Source
Tree 数据契约需求

Acceptance Criteria

AC1：层级显示

Given 应用提供包含多层子节点的集合
When Tree 显示
Then 节点按照父子层级和应用提供的顺序呈现

AC2：稳定身份

Given 应用重新排序节点但保持节点身份不变
When Tree 接收更新后的集合
Then 选中和展开状态仍与相同逻辑节点关联

AC3：空数据

Given 应用未提供节点或提供空集合
When Tree 显示
Then 不出现虚假节点且组件保持稳定可访问

AC4：禁用节点导航

Given 应用将一个节点标记为禁用
When 键盘用户使用 Tree 的方向键导航到该节点
Then 节点可以获得 Tree 内部焦点，辅助技术能够读取其名称和禁用状态

AC5：禁用节点操作

Given 焦点位于禁用节点
When 终端用户尝试选择、展开或激活该节点
Then 节点状态不改变，应用不收到选择、展开或激活通知
```

```
REQ-F-062 🔴 [Actor: End User]

Title
Tree 展开与折叠

Description
具有子节点且未禁用的 Tree 节点必须能够由终端用户展开和折叠。应用仍可以通过外部状态更新禁用节点的展开状态。展开状态必须清晰可见并向辅助技术表达；隐藏的子节点不得继续出现在键盘导航或辅助技术的可见节点序列中。应用将 `ExpandedIds` 设为 `null` 时，Tree 自行保持展开集合；应用提供非 `null` 的 `ExpandedIds` 时，该集合是显示状态的权威来源，用户操作只产生一次变更请求，显示在应用回传新集合前保持不变。

Source
Tree 基础交互与 WAI-ARIA Tree View Pattern

Acceptance Criteria

AC1：展开

Given 应用将 `ExpandedIds` 设为 `null`，且一个可用并具有子节点的节点处于折叠状态
When 终端用户执行展开操作
Then 其直接子节点显示且应用收到一次展开状态通知

AC2：折叠

Given 应用将 `ExpandedIds` 设为 `null`，且一个可用并具有已显示子节点的节点处于展开状态
When 终端用户执行折叠操作
Then 后代节点从可见导航序列中移除且应用收到一次折叠状态通知

AC3：应用更新

Given Tree 已显示
When 应用改变某节点的展开状态
Then 该节点显示结果和辅助技术状态同步更新

AC4：受控展开

Given 应用已提供非 `null` 的 `ExpandedIds`
When 终端用户请求展开或折叠节点
Then 应用最多收到一次候选集合更新，并且 Tree 在应用回传新集合前仍显示原集合

AC5：异步展开决策

Given 应用对展开或折叠请求的决策尚未完成
When 相关节点在等待期间被替换或移除
Then Tree 不应用过期决策；决策过程报错时状态保持不变且错误不被静默忽略
```

```
REQ-F-063 🔴 [Actor: End User]

Title
Tree 单选与焦点

Description
Tree 必须支持单节点选择，并清晰区分键盘焦点与选中状态。选择新节点时，应用收到一次选择通知；仅移动焦点不得改变选中状态或发送选择通知。应用将 `SelectedIds` 设为 `null` 时，Tree 自行保持选择集合；应用提供非 `null` 的 `SelectedIds` 时，该集合是显示状态的权威来源，用户操作只产生一次变更请求，显示在应用回传新集合前保持不变。

Source
Tree 选择与焦点需求

Acceptance Criteria

AC1：选择节点

Given 应用将 `SelectedIds` 设为 `null`，且 Tree 包含多个可选节点
When 终端用户执行选择操作
Then 仅该节点显示为选中且应用收到一次选择通知

AC2：焦点移动

Given 一个节点已选中
When 键盘用户只移动焦点到另一节点
Then 原选中状态保持不变且应用不收到选择通知

AC3：外部选择

Given Tree 已显示
When 应用提供另一个有效节点作为当前选择
Then 选中状态移动到该节点且不把外部更新报告为用户选择

AC4：受控选择

Given 应用已提供非 `null` 的 `SelectedIds`
When 终端用户请求改变选择
Then 应用最多收到一次候选集合更新，并且 Tree 在应用回传新集合前仍显示原集合
```

```
REQ-F-064 🟡 [Actor: Application Developer]

Title
Tree 节点图标

Description
应用开发者可以为 Tree 节点提供图标内容。Astrea 不得强制应用使用特定图标技术；装饰图标不得替代或重复节点名称。

Source
Tree 节点展示需求

Acceptance Criteria

AC1：节点图标

Given 应用为节点提供图标
When Tree 显示该节点
Then 图标显示在节点标签之前且不遮挡标签

AC2：无图标

Given 节点没有图标
When Tree 显示
Then 标签布局不保留无意义的图标空位

AC3：辅助技术

Given 节点图标仅用于装饰
When 辅助技术读取节点
Then 图标不会重复加入节点名称
```

```
REQ-F-065 🟢 [Actor: Application Developer]

Title
Tree 懒加载

Description
应用可以在父节点首次展开时异步提供子节点。加载期间必须显示可感知状态；失败时必须提供错误反馈和重试路径；同一展开操作不得产生重复加载请求。

Source
Tree 增强需求（Deferred）

Acceptance Criteria

AC1：首次加载

Given 一个可展开节点尚未加载子节点
When 终端用户首次展开该节点
Then 应用收到一次加载请求且节点显示加载状态

AC2：加载成功

Given 节点正在加载
When 应用提供子节点
Then 加载状态结束并显示子节点

AC3：加载失败

Given 节点正在加载
When 应用报告加载失败
Then 节点显示可访问的错误反馈并允许重试

AC4：重复操作

Given 同一节点的加载请求尚未完成
When 用户重复执行展开操作
Then 不产生并发的重复加载请求
```

```
REQ-F-066 🔴 [Actor: Keyboard User]

Title
Tree 可访问性与键盘操作

Description
Tree 必须符合树形控件的键盘和辅助技术交互模式。Tree 只有一个顺序导航入口；进入后，方向键在可见节点间移动焦点，焦点、选中、展开和层级状态保持可区分。

Source
WAI-ARIA Tree View Pattern

Acceptance Criteria

AC1：上下导航

Given 键盘焦点位于 Tree 节点
When 用户按 ArrowDown 或 ArrowUp
Then 焦点移动到下一个或上一个可见节点

AC2：右方向键

Given 焦点位于可用且可展开的节点
When 用户按 ArrowRight
Then 折叠节点展开；已展开节点的焦点移动到第一个子节点

AC3：左方向键

Given 焦点位于可用的展开节点或子节点
When 用户按 ArrowLeft
Then 展开节点折叠；子节点的焦点移动到父节点

AC4：首尾导航

Given Tree 中存在可见节点
When 用户按 Home 或 End
Then 焦点分别移动到第一个或最后一个可见节点

AC5：选择

Given 焦点位于可选节点
When 用户按 Space 或执行公开的选择操作
Then 该节点成为唯一选中节点且应用收到一次选择通知

AC6：辅助技术状态

Given Tree 显示层级节点
When 辅助技术读取任一节点
Then 获得其名称、层级、展开状态、选中状态和禁用状态中适用的信息
```

```
REQ-F-077 🔴 [Actor: Keyboard User]

Title
Tree 动态节点移除与焦点恢复

Description
应用删除节点或折叠包含当前焦点的分支后，Tree 必须把焦点恢复到仍然存在的可见目标，不得让焦点停留在已移除或隐藏节点上。删除当前选中节点时进入无选择状态，数据更新不得被报告为用户选择。

Source
Tree 动态数据、焦点与选择一致性需求

Acceptance Criteria

AC1：移除焦点节点

Given 键盘焦点位于即将被应用移除的节点
When Tree 接收更新后的节点集合
Then 焦点依次选择仍存在的下一个可见同级节点、上一个可见同级节点或最近的可见祖先节点；若这些目标均不存在但 Tree 非空，则移动到第一个可见节点

AC2：Tree 变为空

Given 焦点节点被移除且 Tree 不再包含任何节点
When Tree 显示空状态
Then 焦点位于 Tree 的可聚焦入口，不停留在已移除节点上

AC3：移除选中节点

Given 当前选中节点被应用移除
When Tree 接收更新后的节点集合
Then Tree 进入无选择状态且应用不收到用户选择通知

AC4：折叠焦点所在分支

Given 焦点位于一个已展开节点的后代
When 应用或用户折叠该节点
Then 焦点移动到被折叠的父节点，隐藏后代不再进入可见导航序列
```

```
REQ-F-086 🟡 [Actor: End User]

Title
Tree 多选

Description
应用可以启用多选模式。多选模式下，终端用户可以同时选中多个节点，并可以通过范围选择（一次操作选中两个节点之间的连续节点）与切换选择（逐项增减选中）调整选择集合，应用收到与当前选择集合一致的更新结果。关闭多选模式后 Tree 恢复单选中行为（REQ-F-063）；焦点与选中的区分（REQ-F-063）在多选模式下仍然成立。一个 Tree 可以包含多个根节点，这些根节点属于同一个交互边界；范围选择可以跨越根节点边界，选中两个目标之间除禁用节点以外的全部可见节点；端点禁用时不执行范围选择。

Source
全功能树需求——多选是树控件在文件管理、层级编辑等场景的常见能力

Acceptance Criteria

AC1：启用多选

Given 应用启用多选模式且将 `SelectedIds` 设为 `null`
When 终端用户执行选择操作
Then 应用收到一次与当前选择集合一致的更新结果

AC2：范围选择

Given 多选模式、应用将 `SelectedIds` 设为 `null`，且已存在一个选中节点
When 终端用户对另一可见节点执行范围选择
Then 范围可以跨越同一 Tree 的根节点边界；任一端点禁用时整次操作被拒绝，中间的禁用节点被跳过，其余两端点之间的可见节点进入选中集合且应用最多收到一次更新

AC3：切换选择

Given 多选模式且应用将 `SelectedIds` 设为 `null`
When 终端用户对某节点执行切换选择
Then 该节点选中状态翻转，其余选中节点保持不变且应用收到一次更新

AC4：关闭回退

Given 多选模式已启用
When 应用关闭多选模式
Then Tree 恢复单选中行为且选择集合收敛为单节点或无选择
```

```
REQ-F-087 🟡 [Actor: Application Developer]

Title
Tree 拖拽重排

Description
应用可以启用拖拽。终端用户将选中的一个或多个节点拖拽到目标节点时，可以指定放置位置（目标之前、目标之后或目标之内）。放置前应用可以异步否决该放置；在决策完成前 Tree 不提交放置，相关节点被替换或移除后不应用过期决策，决策过程报错时不改变树结构且错误不被静默忽略。放置被接受后应用收到一次包含源节点、目标节点与放置位置的放置通知，并由应用决定最终的树结构（例如纳入其撤销/重做体系）。一个 Tree 可以包含多个根节点，根节点之间属于同一个交互边界，允许跨根节点拖拽放置；「跨树」指放置到其他 Tree 组件实例，Tree 不进行跨 Tree 实例的放置。Tree 拒绝产生自引用或循环引用的放置；多选拖拽源同时包含互为祖先与后代的节点时，只以最高层祖先作为实际拖拽源，后代不重复参与本次拖拽。

Source
全功能树需求——层级编辑场景需要拖拽重排能力

Acceptance Criteria

AC1：拖拽源

Given 拖拽已启用且存在选中节点
When 终端用户开始拖拽
Then Tree 识别被拖拽的源节点集合

AC2：放置位置

Given 拖拽进行中
When 指针位于目标节点的不同区域
Then Tree 表达放置位置（之前、之后或之内）的可辨识反馈

AC3：应用否决

Given 应用配置了放置否决条件且放置不满足条件
When 终端用户尝试放置
Then 不产生放置通知且树结构不变

AC4：放置通知

Given 放置满足所有条件
When 终端用户完成放置
Then 应用收到一次包含源节点、目标节点与放置位置的放置通知，且应用据此更新树结构

AC5：非法放置拒绝

Given 拖拽进行中
When 放置将产生自引用、循环引用或跨树放置
Then Tree 拒绝该放置且不发送放置通知

AC6：拖拽源归一化

Given 拖拽已启用且选中的源节点集合同时包含互为祖先与后代的节点
When 终端用户开始拖拽
Then Tree 以最高层祖先作为实际拖拽源，后代不重复参与本次拖拽

AC7：单次放置

Given 同一次拖拽操作已经被接受、拒绝、取消或失效
When Tree 随后收到该操作的重复或过期完成结果
Then Tree 不再发送放置通知
```

```
REQ-F-088 🟡 [Actor: Application Developer]

Title
Tree 节点编辑

Description
应用可以为节点进入编辑状态。编辑状态下节点标签可被修改，应用可以异步否决修改；决策完成前不提交编辑结果，相关节点被替换或移除后不应用过期决策，决策过程报错时保持原标签且错误不被静默忽略。修改被接受后应用收到一次包含节点与新值的结果通知，并决定节点标签的最终值。终端用户可以完成或取消编辑。

Source
全功能树需求——层级节点重命名能力

Acceptance Criteria

AC1：进入编辑

Given 应用为某节点启用编辑
When Tree 显示该节点
Then 节点以可编辑状态呈现且获得编辑焦点

AC2：应用否决

Given 节点处于编辑状态且应用配置了否决条件
When 终端用户提交不满足条件的新值
Then 编辑结果不被接受且应用收到否决结果

AC3：提交

Given 节点处于编辑状态
When 终端用户提交新值且未被否决
Then 应用收到一次包含节点与新值的结果通知，并决定最终标签

AC4：取消

Given 节点处于编辑状态
When 终端用户取消编辑
Then 节点退出编辑状态且应用不收到结果通知

AC5：过期编辑操作

Given 应用已经开始编辑另一节点，或原编辑节点已被替换或移除
When 旧编辑界面随后请求提交或取消
Then Tree 不得改变当前编辑会话、节点标签或发送结果通知
```

```
REQ-F-089 🟡 [Actor: Application Developer]

Title
Tree 右键通知

Description
终端用户在节点上触发上下文菜单操作时，应用收到一次包含该节点与触发位置的上下文通知。应用据此决定是否展示及展示何种菜单内容；Tree 自身不渲染菜单内容。仅触发通知不得改变选中或焦点状态。

Source
全功能树需求——树控件需向应用暴露右键触发时机

Acceptance Criteria

AC1：右键通知

Given Tree 已显示
When 终端用户在节点上触发上下文菜单操作
Then 应用收到一次包含该节点与触发位置的上下文通知

AC2：状态不变

Given 终端用户触发上下文菜单
When 该操作未伴随其他选择操作
Then Tree 的选中与焦点状态不因此改变

AC3：菜单由应用决定

Given 应用收到上下文通知
When 应用决定是否展示菜单及菜单内容
Then 菜单的呈现与行为完全由应用控制
```

## 3.11 Table（表格）

> 当前 Active 基线不包含排序、筛选、行选择、列调整大小或数据虚拟化；排序、筛选和行选择分别见 Deferred 的 REQ-F-082、REQ-F-084、REQ-F-085，列调整大小和数据虚拟化仅作为延期范围记录。

```
REQ-F-067 🔴 [Actor: Application Developer]

Title
Table 数据集合

Description
应用开发者必须能够为 Table 提供行数据。Table 在正常数据状态下必须区分有数据、空集合和未提供数据，并在应用更新、增加、删除或重排行后显示最新结果；加载与错误状态遵守 REQ-F-078。

Source
Table 数据显示需求

Acceptance Criteria

AC1：数据显示

Given 应用提供多行数据
When Table 显示
Then 每个数据项对应一行且顺序与应用提供的顺序一致

AC2：数据更新

Given Table 已显示
When 应用更新、增加、删除或重排行数据
Then Table 显示最新行集合且不会保留已删除行

AC3：空或未提供数据

Given Table 处于正常数据状态且应用提供空集合或未提供数据
When Table 显示
Then Table 进入空数据状态且不产生未处理错误
```

```
REQ-F-068 🔴 [Actor: Application Developer]

Title
Table 列定义

Description
应用开发者必须能够定义每列的标题、顺序、对齐方式和单元格显示内容。每个单元格必须使用其所在行的数据生成可观察内容。

Source
Table 列展示需求

Acceptance Criteria

AC1：列标题与顺序

Given 应用定义多列
When Table 显示
Then 表头按定义顺序显示对应标题

AC2：单元格内容

Given 应用定义列内容规则并提供多行数据
When Table 显示
Then 每个单元格显示当前行对应内容

AC3：对齐方式

Given 应用为某列选择受支持的对齐方式
When Table 显示
Then 该列表头和单元格采用一致对齐结果
```

```
REQ-F-069 🟡 [Actor: End User]

Title
Table 固定表头

Description
应用可以在 Table 数据区域滚动时保持表头可见。固定表头不得遮挡数据、改变列对齐或产生第二套冲突的表头语义。

Source
Table 长数据浏览需求

Acceptance Criteria

AC1：启用固定表头

Given Table 内容超过可用高度且应用启用固定表头
When 终端用户滚动数据区域
Then 表头保持可见且列标题与数据列对齐

AC2：未启用固定表头

Given 应用未启用固定表头
When 终端用户滚动包含 Table 的页面
Then 表头随表格正常滚动

AC3：缩放与调整尺寸

Given 固定表头正在使用
When 浏览器缩放或容器尺寸变化
Then 表头不遮挡第一行数据且保持列对齐
```

```
REQ-F-070 🟡 [Actor: End User]

Title
Table 行操作

Description
应用可以使数据行成为可操作目标。可操作行必须同时支持指针和键盘操作，具有可见焦点，并在每次用户激活时向应用发送一次当前行通知。

Source
Table 行交互需求

Acceptance Criteria

AC1：指针激活

Given 一行被应用声明为可操作
When 终端用户点击该行
Then 应用收到一次该行通知

AC2：键盘激活

Given 键盘焦点位于可操作行
When 用户按 Enter 或 Space
Then 应用收到一次与点击相同的该行通知

AC3：非交互行

Given 应用未将行声明为可操作
When 用户浏览 Table
Then 该行不会仅因显示数据而进入键盘焦点顺序
```

```
REQ-F-071 🟡 [Actor: End User]

Title
Table 空状态

Description
Table 在正常数据状态下没有可显示行时必须显示应用提供的空状态内容；未提供自定义内容时显示可替换的默认文本。加载或错误状态遵守 REQ-F-078，不得同时显示为空数据。空状态不得伪装成正常数据行。

Source
Table 空数据反馈需求

Acceptance Criteria

AC1：默认空状态

Given Table 处于正常数据状态、没有行且应用未提供空状态内容
When Table 显示
Then 显示默认空数据文本

AC2：自定义空状态

Given Table 处于正常数据状态、没有行且应用提供自定义空状态内容
When Table 显示
Then 显示自定义内容

AC3：恢复数据

Given Table 正在显示空状态
When 应用提供非空数据并保持正常数据状态
Then 空状态消失并显示数据行
```

```
REQ-F-072 🟢 [Actor: End User]

Title
Table 斑马纹

Description
应用可以启用交替行视觉样式。该样式只用于辅助浏览，不得成为表达行状态或选择状态的唯一方式。

Source
Table 可读性增强需求

Acceptance Criteria

AC1：启用斑马纹

Given Table 包含多行且应用启用交替行样式
When Table 显示
Then 相邻行呈现交替视觉效果

AC2：禁用斑马纹

Given 应用未启用交替行样式
When Table 显示
Then 不强制使用交替行背景

AC3：状态区分

Given 某行同时具有选择、错误或禁用等业务状态
When Table 显示
Then 该状态不只依赖斑马纹颜色表达
```

```
REQ-F-073 🟢 [Actor: End User]

Title
Table 分页

Description
应用可以启用分页浏览。分页状态必须显示当前页、总页数和可用操作；用户请求换页时应用收到一次目标页通知，最终显示内容与应用提供的页状态和数据一致。

Source
Table 增强需求（Deferred）

Acceptance Criteria

AC1：分页状态

Given Table 启用分页且总页数大于一
When Table 显示
Then 用户能够识别当前页、总页数和可用的上一页或下一页操作

AC2：用户换页

Given 存在可访问的目标页
When 终端用户请求切换页面
Then 应用收到一次目标页通知

AC3：边界页

Given 当前位于第一页或最后一页
When 分页操作显示
Then 不可用的上一页或下一页操作具有明确禁用状态

AC4：外部更新

Given Table 已显示某一页
When 应用提供新的页状态和行数据
Then Table 的分页信息和数据内容同步更新
```

```
REQ-F-074 🔴 [Actor: Assistive Technology User]

Title
Table 可访问性语义

Description
Table 必须向辅助技术表达为数据表，并保持表题、列标题、行和数据单元格之间的关联。可操作内容必须保留其自身的可访问名称和键盘行为。

Source
HTML Table 语义与 WCAG 2.2

Acceptance Criteria

AC1：表格结构

Given Table 包含列标题和数据行
When 辅助技术浏览 Table
Then 能够识别表格、列标题、行和数据单元格

AC2：表题

Given 应用为 Table 提供表题或替代名称
When 辅助技术进入 Table
Then 获得能够说明表格用途的名称

AC3：标题关联

Given 辅助技术读取任一数据单元格
When 用户请求其标题上下文
Then 能够获得对应列标题

AC4：单元格操作

Given 单元格包含按钮、链接或其他控件
When 键盘用户导航和激活该控件
Then 控件保持自身名称、焦点和标准交互行为
```

```
REQ-F-078 🟡 [Actor: End User]

Title
Table 加载与错误状态

Description
Table 必须区分错误、加载、正常数据和空数据状态。状态优先级固定为：错误高于加载；既非错误也非加载时，有行则为正常数据，无行则为空数据。应用可以为加载与错误状态提供可访问内容；状态变化后不得把加载或错误误报为空数据。

Source
异步数据展示状态需求

Acceptance Criteria

AC1：加载状态

Given 应用将 Table 标记为正在加载且未报告错误
When Table 显示
Then 用户获得可识别的加载反馈，空状态不同时显示

AC2：自定义错误状态

Given 应用报告 Table 数据加载失败并提供错误内容
When Table 显示
Then 用户获得应用提供的可访问错误反馈，错误状态不被显示为空数据

AC3：默认错误反馈

Given 应用报告 Table 数据加载失败但未提供错误内容
When Table 显示
Then 用户获得可配置的默认错误文本且空状态不同时显示

AC4：加载成功

Given Table 正显示加载状态
When 应用提供非空数据并结束加载状态
Then 加载反馈消失并显示最新数据行

AC5：错误恢复

Given Table 正显示错误状态
When 应用提供有效数据并清除错误状态
Then 错误反馈消失并显示最新数据行

AC6：错误优先

Given 应用同时提供加载和错误状态
When Table 显示
Then 显示错误状态而不同时显示加载或空数据状态

AC7：状态可感知

Given Table 从正常或空数据状态切换为加载或错误状态
When 辅助技术监测该次状态变化
Then 用户能够识别最新状态且当前键盘焦点不会仅因状态变化而移动
```

```
REQ-F-079 🟡 [Actor: End User]

Title
Table 宽内容访问

Description
Table 的列总宽度超过可用容器时，不得裁掉或永久隐藏数据。终端用户必须能够访问全部列；横向浏览后，列标题与数据单元格的对应关系以及单元格内交互控件的键盘行为必须保持有效。

Source
宽数据表可用性与 WCAG 2.2 二维内容要求

Acceptance Criteria

AC1：宽表访问

Given Table 的列总宽度超过可用容器
When 终端用户浏览 Table
Then 用户能够横向访问全部列且数据内容不被永久裁掉

AC2：标题对应

Given 用户已经横向移动 Table 的可见区域
When 用户读取任一可见数据单元格
Then 对应列标题仍可识别且与该单元格对齐

AC3：键盘控件

Given 宽 Table 的非初始可见列包含可交互控件
When 键盘用户导航到该控件
Then 控件能够进入可见区域并保持自身标准焦点和操作行为

AC4：静态列键盘访问

Given 宽 Table 的非初始可见列只包含静态数据
When 键盘用户聚焦 Table 的横向浏览区域并使用 ArrowLeft 或 ArrowRight
Then 可见区域沿对应方向移动，用户能够读取全部列并返回初始列
```

```
REQ-F-082 🟢 [Actor: Application Developer]

Title
Table 排序

Description
应用可以为列启用受控排序。用户请求改变排序时，应用只收到一次目标列和目标方向通知；最终排序状态和行数据由应用提供，Table 不自行覆盖应用状态。

Source
Table 排序增强需求（Deferred）

Acceptance Criteria

AC1：首次排序

Given 应用为某列启用排序且当前未排序
When 用户请求按该列排序
Then 应用收到一次目标列和目标排序方向通知

AC2：改变方向

Given 某列已有排序方向
When 用户请求改变该列排序
Then 应用收到一次新的目标方向通知，不同时产生第二条排序通知

AC3：外部状态

Given Table 已显示排序状态
When 应用提供新的排序状态和行数据
Then 排序指示和行顺序与应用提供的最终状态一致

AC4：可访问状态

Given 某列具有当前排序状态
When 视觉用户或辅助技术读取该列表头
Then 能够识别该列及适用的升序或降序状态
```

```
REQ-F-084 🟢 [Actor: Application Developer]

Title
Table 筛选

Description
应用可以启用受控筛选。用户提交筛选条件时，应用只收到一次筛选请求；最终筛选条件和行数据由应用提供，Table 必须区分筛选后无结果与未应用筛选的普通空数据。

Source
Table 筛选增强需求（Deferred）

Acceptance Criteria

AC1：提交筛选

Given 应用启用筛选并提供可操作的筛选入口
When 用户提交筛选条件
Then 应用收到一次包含该条件的筛选请求

AC2：外部结果

Given 应用收到筛选请求
When 应用提供最终筛选条件和行数据
Then Table 显示的条件与行数据和应用提供的结果一致

AC3：无匹配结果

Given 已应用筛选且应用提供空结果
When Table 显示
Then 用户能够识别“无匹配结果”，不将其误认为尚未加载数据

AC4：清除筛选

Given Table 已显示筛选结果
When 用户请求清除筛选
Then 应用收到一次清除请求，最终条件和行数据仍由应用提供
```

```
REQ-F-085 🟢 [Actor: End User]

Title
Table 行选择

Description
应用可以启用受控行选择。每次用户选择或取消选择一行时，应用只收到一次包含目标行和目标选择状态的通知；最终选择状态由应用提供。行选择与 REQ-F-070 的行操作必须使用具有不同含义的交互，不得由同一次操作同时触发两类通知。

Source
Table 行选择增强需求（Deferred）

Acceptance Criteria

AC1：选择行

Given 应用启用行选择且某行当前未选中
When 用户执行已记录的行选择操作
Then 应用收到一次该行的选中请求，不同时收到行操作通知

AC2：取消选择

Given 一行当前已选中
When 用户执行已记录的取消选择操作
Then 应用收到一次该行的取消选择请求

AC3：外部状态

Given Table 已显示选择状态
When 应用提供新的行选择状态
Then 视觉用户和辅助技术获得的选择状态与应用提供的结果一致

AC4：行操作隔离

Given 同一行同时支持行选择和 REQ-F-070 的行操作
When 用户执行行操作
Then 应用只收到行操作通知且选择状态不因该操作改变
```

---

## 3.12 ResourceTable（资源表）

> ResourceTable 是与 Table（§3.11）并行存在的资源表组件：以列定义 + 应用模板表体为核心，提供列宽拖拽、工具栏操作与撤销重做按钮。列宽拖拽是 ResourceTable 的专属能力，不属于 Table 的延期范围。

```
REQ-F-090 🔴 [Actor: Application Developer]

Title
ResourceTable 列定义与表体模板

Description
应用开发者必须能够为 ResourceTable 提供列定义与表体模板。每列可声明标题、宽度、稳定标识、最小拖拽宽度与是否允许拖拽；表体内容由应用提供的模板渲染。ResourceTable 必须按列定义顺序渲染表头。

Source
ResourceTable 展示需求（源自 UA_Modeler ResourceTableBase 组件经验）

Acceptance Criteria

AC1：列定义渲染

Given 应用提供多列定义
When ResourceTable 显示
Then 表头按定义顺序显示对应标题

AC2：表体模板

Given 应用提供表体模板
When ResourceTable 显示
Then 表体区域按应用模板渲染且不修改模板内容

AC3：列级配置

Given 应用为列声明宽度、稳定标识、最小拖拽宽度或禁用拖拽
When ResourceTable 显示
Then 列宽与拖拽行为与声明一致且拖拽结束通知携带该列稳定标识
```

```
REQ-F-091 🟡 [Actor: End User]

Title
ResourceTable 列宽拖拽

Description
应用启用列宽拖拽后，终端用户必须能够拖动列边界调整列宽。调整过程中相邻列宽度同步变化且总宽度守恒；每列宽度不得小于其最小拖拽宽度。最后一列不提供拖拽手柄。

Source
ResourceTable 列宽调整需求（源自 UA_Modeler ResourceTable.ts 经验）

Acceptance Criteria

AC1：拖拽调整

Given ResourceTable 已启用列宽拖拽且列具有拖拽手柄
When 终端用户拖动列边界
Then 该列与相邻列宽度随拖动变化且表头与数据列保持对齐

AC2：最小宽度约束

Given 某列声明了最小拖拽宽度
When 终端用户将列宽拖至低于该值
Then 该列宽度停在最小拖拽宽度且相邻列不再继续变窄

AC3：总宽守恒

Given 终端用户正在拖动列边界
When 拖动结束
Then 拖动列与相邻列宽度之和与拖动前一致

AC4：末列无手柄

Given ResourceTable 显示多列
When 终端用户观察最后一列
Then 最后一列没有拖拽手柄且不能通过拖拽改变其宽度
```

```
REQ-F-092 🟡 [Actor: Application Developer]

Title
ResourceTable 列宽拖拽结束通知

Description
应用启用列宽拖拽并订阅通知后，终端用户完成一次列宽拖拽时应用必须收到一次通知，通知携带列索引、列稳定标识与最终宽度。未订阅时组件行为不受影响。

Source
ResourceTable 列宽持久化需求（源自 UA_Modeler OnColumnResized 设计）

Acceptance Criteria

AC1：拖拽结束通知

Given 应用订阅列宽拖拽结束通知且终端用户完成一次拖拽
When 拖拽结束
Then 应用收到一次通知且携带该列索引、稳定标识与最终宽度

AC2：无订阅者

Given 应用未订阅列宽拖拽结束通知
When 终端用户完成一次拖拽
Then 组件不产生错误且列宽仍正确更新
```

```
REQ-F-093 🟡 [Actor: Application Developer]

Title
ResourceTable 工具栏操作

Description
应用可以启用 ResourceTable 工具栏的添加、删除、上移与下移按钮，并为每个按钮绑定操作通知。按钮被触发时应用必须收到对应通知；应用可以隐藏整个工具栏按钮组。

Source
ResourceTable 行操作需求（源自 UA_Modeler ResourceTableBase 工具栏）

Acceptance Criteria

AC1：按钮通知

Given 应用启用工具栏并绑定操作通知
When 终端用户点击添加、删除、上移或下移按钮
Then 应用收到与按钮对应的一次通知

AC2：隐藏按钮组

Given 应用隐藏工具栏按钮组
When ResourceTable 显示
Then 不渲染添加、删除、上移与下移按钮
```

```
REQ-F-094 🟡 [Actor: Application Developer]

Title
ResourceTable 撤销重做按钮

Description
应用可以启用 ResourceTable 工具栏的撤销与重做按钮，并通过状态标志控制按钮禁用态。按钮被触发时应用必须收到对应通知；未启用时按钮不渲染。

Source
ResourceTable 撤销重做需求（源自 UA_Modeler ShowRedoButton 设计）

Acceptance Criteria

AC1：按钮通知与禁用态

Given 应用启用撤销重做按钮并提供禁用标志
When ResourceTable 显示且终端用户点击按钮
Then 按钮禁用态与应用提供标志一致，可点击时应用收到对应通知

AC2：未启用

Given 应用未启用撤销重做按钮
When ResourceTable 显示
Then 不渲染撤销与重做按钮
```

```
REQ-F-095 🟡 [Actor: Keyboard User]

Title
ResourceTable 键盘操作

Description
ResourceTable 必须允许应用接收容器上的键盘事件，使应用可以实现方向键导航等键盘操作。组件不得吞掉已绑定到应用的键盘事件。

Source
ResourceTable 键盘导航需求（源自 UA_Modeler ResourceTableBase 键盘支持）

Acceptance Criteria

AC1：键盘事件转发

Given 应用为 ResourceTable 绑定键盘事件回调
When 终端用户在组件容器内按下按键
Then 应用收到对应的键盘事件

AC2：未绑定

Given 应用未绑定键盘事件回调
When 终端用户在组件容器内按下按键
Then 组件不产生错误
```

```
REQ-F-096 🔴 [Actor: Application Developer]

Title
ResourceTable 空列集合

Description
应用向 ResourceTable 提供空列集合或未提供列定义时，组件必须正常渲染，不产生未处理错误，且不渲染误导性的表头列。

Source
ResourceTable 边界行为需求（源自 UA_Modeler Columns 空集合修复）

Acceptance Criteria

AC1：空列集合

Given 应用提供空列集合
When ResourceTable 显示
Then 组件正常渲染且不产生未处理错误

AC2：表体仍渲染

Given 应用提供空列集合与表体模板
When ResourceTable 显示
Then 表体模板仍按应用提供内容渲染
```

```
REQ-F-097 🟢 [Actor: Application Developer]

Title
ResourceTable 图标定制

Description
应用可以覆盖 ResourceTable 工具栏按钮的默认图标。未提供自定义图标时组件必须显示内置图标且不依赖任何外部图标库。

Source
ResourceTable 图标定制需求（源自 UA_Modeler 自包含 SVG 图标设计）

Acceptance Criteria

AC1：默认图标

Given 应用未提供自定义图标
When ResourceTable 显示工具栏
Then 按钮显示内置图标且不依赖外部图标库资源

AC2：自定义图标

Given 应用为某按钮提供自定义图标内容
When ResourceTable 显示工具栏
Then 该按钮显示应用提供的图标内容
```

---

# 4 非功能需求

```
REQ-NF-001 🔴 [Actor: Application Developer]

Title
.NET 6.0 目标框架

Description
Astrea 发布包的目标框架标识必须为 net6.0，并且能够由以 .NET 6.0 为目标的 Blazor 应用引用、还原、编译和运行，不得要求应用升级到更高 .NET 版本。

Source
项目技术基线：.NET 6.0

Acceptance Criteria

AC1：包框架标识

Given Astrea 发布包已经生成
When 应用开发者检查包中声明的目标框架资产
Then 存在 net6.0 资产且不以更高目标框架资产代替

AC2：应用消费

Given 一个以 net6.0 为目标的最小 Blazor 应用
When 安装 Astrea 包、使用一个基础组件并执行还原与编译
Then 还原和编译成功且不要求更改目标框架

AC3：运行

Given 前述最小应用已编译
When 应用运行并显示 Astrea 组件
Then 组件能够显示并按对应功能需求交互
```

```
REQ-NF-002 🟡 [Actor: End User]

Title
桌面浏览器兼容性

Description
Astrea 必须支持发布时 Chrome、Edge、Firefox、Safari 的最近两个桌面主版本。受支持浏览器中的功能、键盘行为、可访问状态和核心布局必须一致；非核心视觉差异不得影响使用。

Source
浏览器兼容性约束

Acceptance Criteria

AC1：核心功能

Given 同一示例应用运行于任一受支持浏览器版本
When 终端用户执行组件文档中的核心操作
Then 操作结果和事件通知一致

AC2：键盘与可访问性

Given 键盘用户或辅助技术在任一受支持浏览器中访问组件
When 执行组件规定的操作
Then 焦点、名称、角色和状态行为符合需求

AC3：发布记录

Given Astrea 版本准备发布
When 兼容性验证完成
Then 发布记录明确列出实际验证的浏览器及版本
```

```
REQ-NF-003 🟡 [Actor: End User]

Title
代表性数据量性能

Description
在发布测试记录所声明的参考环境中，应用已经提供数据且页面处于可交互状态时，包含 100 行、10 列简单文本数据的 Table 必须在 1 秒内完成显示并可供用户操作。

Source
代表性数据量性能目标

Acceptance Criteria

AC1：Table 显示性能

Given 参考测试应用已处于可交互状态并准备好 100 行、10 列简单文本数据
When 应用将数据提供给 Table
Then 从数据提供到 Table 完成显示并可操作的时间不超过 1 秒

AC2：重复测量

Given 相同参考环境和数据集
When 性能场景连续执行 10 次
Then 至少 9 次满足 1 秒目标，且测试记录包含环境、测量方法和原始结果
```

```
REQ-NF-004 🔴 [Actor: Assistive Technology User]

Title
支持 WCAG 2.2 AA

Description
Astrea 生成的语义、内置样式和内置交互在按照组件文档使用时，不得阻碍宿主应用达到 WCAG 2.2 AA。应用开发者提供的内容、标签、模板、主题覆盖和页面组合由应用开发者负责。

Source
WCAG 2.2

Acceptance Criteria

AC1：适用标准

Given Astrea 组件按文档提供必要标签和内容
When 按组件适用的 WCAG 2.2 A 与 AA 成功标准验证
Then 组件生成的语义、键盘行为、焦点和内置视觉状态不存在阻断性不符合项

AC2：非颜色表达

Given 组件表达错误、警告、选择、禁用或必填状态
When 视觉用户访问组件
Then 状态不只依赖颜色传达

AC3：默认主题对比度

Given 组件使用 Astrea 默认主题
When 测量文本、控件边界、焦点指示和表达状态所需图形的对比度
Then 各项满足适用的 WCAG 2.2 AA 对比度要求

AC4：页面缩放

Given 组件使用 Astrea 默认样式且浏览器页面缩放至 200%
When 用户浏览和操作组件
Then 内容和功能均可访问，且组件自身不产生同时要求横向和纵向滚动的二维内容区域；Table 等本质二维内容除外

AC5：主题责任边界

Given 应用覆盖颜色或其他公开主题变量
When 覆盖结果不满足可访问性要求
Then 文档明确该覆盖属于应用开发者责任，Astrea 默认主题仍满足其适用要求

AC6：验证证据

Given Astrea 版本准备发布
When 可访问性验证完成
Then 保留自动检查、人工键盘检查以及适用辅助技术场景的人工验证结果
```

```
REQ-NF-005 🟡 [Actor: Application Developer]

Title
运行时依赖约束

Description
使用 Astrea 的 .NET 6.0 Blazor 应用不得被强制安装与组件功能无关的第三方运行时包。任何必须随 Astrea 安装的运行时依赖都必须在包元数据和使用文档中声明。

Source
组件库轻量依赖要求

Acceptance Criteria

AC1：依赖可见

Given 应用安装 Astrea NuGet 包
When 应用开发者查看解析后的运行时依赖
Then 所有必需依赖均已声明且不存在未记录依赖

AC2：最小应用

Given 一个仅包含 .NET 6.0 Blazor 基础能力的最小应用
When 安装 Astrea 并使用基础组件
Then 不需要手工安装文档之外的第三方运行时包
```

```
REQ-NF-006 🔴 [Actor: Application Developer]

Title
NuGet 包可消费性

Description
Astrea 必须以 NuGet 包交付。包必须包含运行组件所需的程序集和静态资源，并能够被干净的 .NET 6.0 Blazor Server 与 Blazor WebAssembly 应用安装和使用。

Source
发布与分发要求

Acceptance Criteria

AC1：包内容

Given Astrea 发布包已生成
When 检查包内容
Then 包含运行公开组件所需的程序集、静态资源和包元数据

AC2：Server 消费

Given 一个干净的 .NET 6.0 Blazor Server 应用
When 安装 Astrea 包并显示代表性组件
Then 应用构建和运行成功，组件样式与交互可用

AC3：WebAssembly 消费

Given 一个干净的 .NET 6.0 Blazor WebAssembly 应用
When 安装 Astrea 包并显示代表性组件
Then 应用构建和运行成功，组件样式与交互可用
```

```
REQ-NF-007 🟡 [Actor: Application Developer]

Title
公共使用文档

Description
所有面向应用开发者公开的组件能力、输入、事件通知、默认值、状态行为、可访问性责任和限制必须具有准确文档。文档不得声称当前包中不存在的能力。

Source
公共文档质量要求

Acceptance Criteria

AC1：能力文档

Given 任一公开组件能力
When 应用开发者查阅文档
Then 能够找到用途、默认行为、输入效果、事件结果和最小使用示例

AC2：限制文档

Given 某能力存在适用范围、浏览器限制或应用开发者责任
When 应用开发者查阅对应组件文档
Then 限制和责任被明确说明

AC3：真实性

Given 发布包和公开文档属于同一版本
When 对照文档示例与实际包验证
Then 示例可构建且文档中声称的能力实际存在
```

```
REQ-NF-008 🔴 [Actor: Application Developer]

Title
需求可验证性与发布门禁

Description
每条 Active 的 P0 和 P1 需求必须具有可追溯的验证证据。验证方式必须与行为风险匹配；浏览器布局、焦点、键盘、静态资源和包消费行为不得只依赖非浏览器级验证。

Source
发布质量保障要求

Acceptance Criteria

AC1：需求追踪

Given 任一 Active 的 P0 或 P1 需求
When 检查发布验证记录
Then 能够定位覆盖该需求验收标准的自动或人工验证证据

AC2：验证层级

Given 验收标准涉及真实浏览器布局、焦点、键盘、静态资源或包消费
When 执行发布验证
Then 在能够观察对应行为的环境中完成验证

AC3：发布门禁

Given 任一 Active 的 P0 或 P1 验收标准失败
When 评估当前版本是否可发布
Then 发布被阻止，除非需求状态经正式变更并记录
```

```
REQ-NF-009 🔴 [Actor: Application Developer]

Title
.NET 6 Blazor 托管兼容性

Description
Astrea 的公开组件能力必须同时支持 .NET 6.0 Blazor Server 和 Blazor WebAssembly。Blazor Server 预呈现阶段应提供稳定的初始内容，建立交互连接后功能正常；WebAssembly 应用加载完成后提供相同行为。

Source
.NET 6 Blazor 应用兼容性要求

Acceptance Criteria

AC1：Blazor Server

Given .NET 6.0 Blazor Server 应用使用 Astrea 组件
When 页面完成初始显示并建立交互连接
Then 初始内容稳定，交互功能按需求工作且不出现重复事件

AC2：Blazor WebAssembly

Given .NET 6.0 Blazor WebAssembly 应用使用相同组件场景
When 应用加载完成
Then 组件功能和状态结果与 Blazor Server 场景一致

AC3：初始显示

Given 组件在交互尚未就绪时产生初始页面内容
When 用户查看该内容
Then 不显示误导性的交互结果，交互就绪后状态保持连续
```

```
REQ-NF-010 🔴 [Actor: Application Developer]

Title
安全内容呈现

Description
Astrea 必须把普通文本输入作为文本安全呈现，不得将其解释为可执行标记。组件不得在未记录且未经应用明确触发的情况下向外部网络发送数据。应用提供的自定义可执行内容由应用开发者负责。

Source
组件库安全边界要求

Acceptance Criteria

AC1：普通文本

Given 应用向普通文本能力提供包含 HTML 或脚本字符的字符串
When 组件显示该字符串
Then 字符作为文本呈现且不会执行其中代码

AC2：外部网络

Given 应用未显式配置外部数据操作
When 用户使用 Astrea 组件
Then 组件不会自行向外部网络发送应用数据

AC3：责任边界

Given 应用开发者提供自定义可执行内容或主动绕过普通文本呈现
When 该内容运行
Then 文档明确其安全性由应用开发者负责
```

```
REQ-NF-011 🔴 [Actor: End User]

Title
重复使用与移除可靠性

Description
组件被反复显示、隐藏、添加或移除后，必须保持与首次使用一致的行为。已移除组件不得继续响应用户输入、抢占焦点、阻止页面滚动或产生重复事件通知。

Source
组件生命周期可靠性要求

Acceptance Criteria

AC1：重复显示

Given 同一组件场景被连续显示和隐藏多次
When 用户再次操作当前显示的组件
Then 每次操作只产生一次通知且行为与首次显示一致

AC2：组件移除

Given 一个交互组件已从页面移除
When 用户继续操作页面
Then 已移除组件不响应键盘或指针输入

AC3：全局状态恢复

Given Modal 等组件曾限制焦点或背景滚动
When 组件隐藏或从页面移除
Then 焦点导航和页面滚动恢复为可用状态
```

```
REQ-NF-012 🟡 [Actor: Application Developer]

Title
公共行为兼容性

Description
Astrea 必须采用语义化版本表达公共行为兼容性。对已发布组件能力、默认行为或事件结果的不兼容变更必须在新的主版本中发布，并在发布说明中提供迁移信息。

Source
公共组件库版本兼容要求

Acceptance Criteria

AC1：兼容更新

Given 一个补丁版本或次版本准备发布
When 对照上一稳定版本检查公开行为
Then 不包含未经说明的不兼容变更

AC2：不兼容更新

Given 发布包含不可避免的公开行为变更
When 确定版本号和发布说明
Then 使用新的主版本并记录影响范围、旧行为、新行为和迁移方法
```

---

# 5 需求统计

## 5.1 按优先级

| 优先级 | 数量 |
|:---:|:---:|
| 🔴 P0 | 56 |
| 🟡 P1 | 39 |
| 🟢 P2 | 14 |
| 合计 | 109 |

## 5.2 按章节

| 章节 | 范围 | 数量 |
|------|------|:---:|
| §3.1 通用 | REQ-F-001 ~ 009 | 9 |
| §3.2 Button | REQ-F-010 ~ 017、080 | 9 |
| §3.3 Alert | REQ-F-018 ~ 023 | 6 |
| §3.4 Divider | REQ-F-024 ~ 027 | 4 |
| §3.5 Input | REQ-F-028 ~ 036 | 9 |
| §3.6 Label | REQ-F-037 ~ 039 | 3 |
| §3.7 Modal | REQ-F-040 ~ 047、081 | 9 |
| §3.8 Progress | REQ-F-048 ~ 052、076 | 6 |
| §3.9 Select | REQ-F-053 ~ 060、075、083 | 10 |
| §3.10 Tree | REQ-F-061 ~ 066、077、086 ~ 089 | 11 |
| §3.11 Table | REQ-F-067 ~ 074、078、079、082、084、085 | 13 |
| §3.12 ResourceTable | REQ-F-090 ~ 097 | 8 |
| §4 NFR | REQ-NF-001 ~ 012 | 12 |
| 合计 | | 109 |

---

## 附录A：需求统计

### §3.1 通用组件需求

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-001 | 🔴 | Application Developer | 公共外观属性与附加属性 |
| REQ-F-002 | 🔴 | End User | 适用组件的禁用状态 |
| REQ-F-003 | 🔴 | Application Developer | 稳定的状态更新 |
| REQ-F-004 | 🟡 | Application Developer | 主题定制 |
| REQ-F-005 | 🟡 | Application Developer | 样式影响范围 |
| REQ-F-006 | 🔴 | Assistive Technology User | 语义与可访问名称 |
| REQ-F-007 | 🔴 | Keyboard User | 键盘可操作性 |
| REQ-F-008 | 🔴 | Application Developer | 外部状态同步 |
| REQ-F-009 | 🟡 | Application Developer | 内置文本可配置 |

### §3.2 Button（按钮）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-010 | 🔴 | End User | Button 基础呈现 |
| REQ-F-011 | 🔴 | End User | Button 点击事件 |
| REQ-F-012 | 🔴 | Application Developer | Button 表单行为 |
| REQ-F-013 | 🟡 | Application Developer | Button 视觉变体 |
| REQ-F-014 | 🟡 | Application Developer | Button 尺寸 |
| REQ-F-015 | 🔴 | End User | Button 禁用 |
| REQ-F-016 | 🟡 | Application Developer | Button 图标 |
| REQ-F-017 | 🔴 | Assistive Technology User | Button 可访问性 |
| REQ-F-080 | 🟢 | Application Developer | Button 加载与忙碌状态 |

### §3.3 Alert（警告提示）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-018 | 🔴 | End User | Alert 类型 |
| REQ-F-019 | 🔴 | Application Developer | Alert 内容 |
| REQ-F-020 | 🔴 | End User | Alert 关闭操作 |
| REQ-F-021 | 🔴 | Application Developer | Alert 可见性 |
| REQ-F-022 | 🟢 | Application Developer | Alert 自动关闭 |
| REQ-F-023 | 🔴 | Assistive Technology User | Alert 可访问性 |

### §3.4 Divider（分割线）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-024 | 🔴 | End User | Divider 方向与语义 |
| REQ-F-025 | 🟡 | Application Developer | Divider 视觉定制 |
| REQ-F-026 | 🟡 | Application Developer | Divider 嵌入内容 |
| REQ-F-027 | 🟢 | End User | Divider 响应式表现 |

### §3.5 Input（输入框）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-028 | 🔴 | Application Developer | Input 文本输入类型 |
| REQ-F-029 | 🔴 | Application Developer | Input 值同步 |
| REQ-F-030 | 🟡 | Application Developer | Input 交互通知 |
| REQ-F-031 | 🟡 | End User | Input 占位提示 |
| REQ-F-032 | 🔴 | End User | Input 禁用与只读 |
| REQ-F-033 | 🔴 | Application Developer | Input 验证反馈 |
| REQ-F-034 | 🟢 | Application Developer | Input 前后内容 |
| REQ-F-035 | 🟡 | End User | Input 自动聚焦 |
| REQ-F-036 | 🔴 | Assistive Technology User | Input 标签与错误关联 |

### §3.6 Label（标签）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-037 | 🔴 | Application Developer | Label 内容与控件关联 |
| REQ-F-038 | 🟡 | Application Developer | Label 必填指示 |
| REQ-F-039 | 🔴 | Assistive Technology User | Label 可访问性 |

### §3.7 Modal（对话框）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-040 | 🔴 | Application Developer | Modal 可见状态与关闭请求 |
| REQ-F-041 | 🟡 | Application Developer | Modal 尺寸预设 |
| REQ-F-042 | 🔴 | Application Developer | Modal 内容区域 |
| REQ-F-043 | 🔴 | End User | Modal 确认与取消操作 |
| REQ-F-044 | 🟡 | End User | Modal 遮罩关闭 |
| REQ-F-045 | 🔴 | Keyboard User | Modal Escape 关闭 |
| REQ-F-046 | 🔴 | Keyboard User | Modal 焦点与背景交互 |
| REQ-F-047 | 🔴 | Assistive Technology User | Modal 可访问性语义 |
| REQ-F-081 | 🟢 | Application Developer | Modal 多实例与嵌套 |

### §3.8 Progress（进度条）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-048 | 🔴 | End User | Progress 确定进度 |
| REQ-F-049 | 🔴 | Application Developer | Progress 边界处理 |
| REQ-F-050 | 🟡 | End User | Progress 百分比文本 |
| REQ-F-051 | 🟡 | Application Developer | Progress 视觉状态 |
| REQ-F-052 | 🔴 | Assistive Technology User | Progress 可访问性 |
| REQ-F-076 | 🟡 | End User | Progress 不确定进度 |

### §3.9 Select（下拉选择）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-053 | 🔴 | End User | Select 选项与单选 |
| REQ-F-054 | 🔴 | Application Developer | Select 值与选项同步 |
| REQ-F-055 | 🟡 | End User | Select 占位选项 |
| REQ-F-056 | 🔴 | End User | Select 禁用状态 |
| REQ-F-057 | 🟡 | Application Developer | Select 自定义选项内容 |
| REQ-F-058 | 🟢 | End User | Select 多选 |
| REQ-F-059 | 🟢 | End User | Select 搜索过滤 |
| REQ-F-060 | 🔴 | Keyboard User | Select 可访问性与键盘操作 |
| REQ-F-075 | 🔴 | Application Developer | Select 选项值唯一性 |
| REQ-F-083 | 🔴 | End User | Select 单个选项禁用 |

### §3.10 Tree（树）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-061 | 🔴 | Application Developer | Tree 节点数据 |
| REQ-F-062 | 🔴 | End User | Tree 展开与折叠 |
| REQ-F-063 | 🔴 | End User | Tree 单选与焦点 |
| REQ-F-064 | 🟡 | Application Developer | Tree 节点图标 |
| REQ-F-065 | 🟢 | Application Developer | Tree 懒加载 |
| REQ-F-066 | 🔴 | Keyboard User | Tree 可访问性与键盘操作 |
| REQ-F-077 | 🔴 | Keyboard User | Tree 动态节点移除与焦点恢复 |
| REQ-F-086 | 🟡 | End User | Tree 多选 |
| REQ-F-087 | 🟡 | Application Developer | Tree 拖拽重排 |
| REQ-F-088 | 🟡 | Application Developer | Tree 节点编辑 |
| REQ-F-089 | 🟡 | Application Developer | Tree 右键通知 |

### §3.11 Table（表格）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-067 | 🔴 | Application Developer | Table 数据集合 |
| REQ-F-068 | 🔴 | Application Developer | Table 列定义 |
| REQ-F-069 | 🟡 | End User | Table 固定表头 |
| REQ-F-070 | 🟡 | End User | Table 行操作 |
| REQ-F-071 | 🟡 | End User | Table 空状态 |
| REQ-F-072 | 🟢 | End User | Table 斑马纹 |
| REQ-F-073 | 🟢 | End User | Table 分页 |
| REQ-F-074 | 🔴 | Assistive Technology User | Table 可访问性语义 |
| REQ-F-078 | 🟡 | End User | Table 加载与错误状态 |
| REQ-F-079 | 🟡 | End User | Table 宽内容访问 |
| REQ-F-082 | 🟢 | Application Developer | Table 排序 |
| REQ-F-084 | 🟢 | Application Developer | Table 筛选 |
| REQ-F-085 | 🟢 | End User | Table 行选择 |

### §3.12 ResourceTable（资源表）

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-F-090 | 🔴 | Application Developer | ResourceTable 列定义与表体模板 |
| REQ-F-091 | 🟡 | End User | ResourceTable 列宽拖拽 |
| REQ-F-092 | 🟡 | Application Developer | ResourceTable 列宽拖拽结束通知 |
| REQ-F-093 | 🟡 | Application Developer | ResourceTable 工具栏操作 |
| REQ-F-094 | 🟡 | Application Developer | ResourceTable 撤销重做按钮 |
| REQ-F-095 | 🟡 | Keyboard User | ResourceTable 键盘操作 |
| REQ-F-096 | 🔴 | Application Developer | ResourceTable 空列集合 |
| REQ-F-097 | 🟢 | Application Developer | ResourceTable 图标定制 |

### §4 非功能需求

| 编号 | 优先级 | Actor | 标题 |
| --- |:---:| --- | --- |
| REQ-NF-001 | 🔴 | Application Developer | .NET 6.0 目标框架 |
| REQ-NF-002 | 🟡 | End User | 桌面浏览器兼容性 |
| REQ-NF-003 | 🟡 | End User | 代表性数据量性能 |
| REQ-NF-004 | 🔴 | Assistive Technology User | 支持 WCAG 2.2 AA |
| REQ-NF-005 | 🟡 | Application Developer | 运行时依赖约束 |
| REQ-NF-006 | 🔴 | Application Developer | NuGet 包可消费性 |
| REQ-NF-007 | 🟡 | Application Developer | 公共使用文档 |
| REQ-NF-008 | 🔴 | Application Developer | 需求可验证性与发布门禁 |
| REQ-NF-009 | 🔴 | Application Developer | .NET 6 Blazor 托管兼容性 |
| REQ-NF-010 | 🔴 | Application Developer | 安全内容呈现 |
| REQ-NF-011 | 🔴 | End User | 重复使用与移除可靠性 |
| REQ-NF-012 | 🟡 | Application Developer | 公共行为兼容性 |

---

## 附录B：延期需求

| 编号 | 标题 | 说明 |
|------|------|------|
| REQ-F-022 | Alert 自动关闭 | Deferred；不属于当前版本需求基线 |
| REQ-F-058 | Select 多选 | Deferred；不属于当前版本需求基线 |
| REQ-F-059 | Select 搜索过滤 | Deferred；不属于当前版本需求基线 |
| REQ-F-065 | Tree 懒加载 | Deferred；不属于当前版本需求基线 |
| REQ-F-073 | Table 分页 | Deferred；不属于当前版本需求基线 |
| REQ-F-080 | Button 加载与忙碌状态 | Deferred；不属于当前版本需求基线 |
| REQ-F-081 | Modal 多实例与嵌套 | Deferred；不属于当前版本需求基线 |
| REQ-F-082 | Table 排序 | Deferred；不属于当前版本需求基线 |
| REQ-F-084 | Table 筛选 | Deferred；不属于当前版本需求基线 |
| REQ-F-085 | Table 行选择 | Deferred；不属于当前版本需求基线 |

---

## 附录C：需求变更记录

| 日期 | 变更 | 作者 |
|------|------|:---:|
| 2026-08-01 | **创建 SRS**: 定义 Astrea 组件库范围、组件清单、非目标与全部功能/非功能需求 | wubing7755 |
| 2026-08-01 | **格式对齐**: 需求条目改为代码围栏 + Title/Description/Acceptance Criteria 格式，非功能需求编号改为 REQ-NF，补充 Actor 与附录A 统计表 | wubing7755 |
| 2026-08-01 | **需求去设计化**: 移除需求正文中涉及软件设计阶段的内容（继承基类、scoped CSS、bUnit、平台类型名、focus-visible、动画时机等），统一为行为与使用方契约表述；F-046 验收标准改用『渲染完成』 | wubing7755 |
| 2026-08-03 | **v0.2.0 基线修订**: 目标框架改为 .NET 6.0；明确 SRS 仅规范用户可观察行为与非功能需求；修正状态、表单、键盘、可访问性、边界和延期范围；新增托管兼容、安全、可靠性与公共行为兼容要求 | Ada |
| 2026-08-03 | **v0.3.0 组件边界补充**: 补充 Alert 动态更新、Divider 装饰语义、Input 清空与输入路径、Select 选项值唯一性与单项禁用、Progress 不确定状态、Tree 动态焦点恢复、Table 数据状态与宽内容访问；将 Button、Modal、Table 排序、筛选和行选择分别定义为延期需求 | Ada |
| 2026-08-13 | **v0.4.0 Tree 全功能扩充**: Tree 新增多选（REQ-F-086）、拖拽重排（REQ-F-087）、节点编辑（REQ-F-088）、右键通知（REQ-F-089），Tree 定位为全功能树 | Ada |
| 2026-08-13 | **v0.5.0 Tree 交互边界澄清**: 明确同一 Tree 多根同界、范围选择与拖拽跨根、拖拽源祖先归一化；修正文件头版本元数据与变更记录一致性 | Ada |
| 2026-08-14 | **v0.6.0 Tree 交付合同冻结**: 明确选择与展开的受控/非受控语义、跨根范围内禁用节点规则、异步应用决策、过期编辑操作和拖拽结果单次消费 | Codex |

---

## 附录D：经验教训映射（AWComponent → Astrea）

| AWComponent 的可观察问题 | 对应需求 |
|---------------------------|----------|
| Table 显示硬编码数据而不是应用提供的数据 | REQ-F-067 |
| 应用状态更新后界面未同步 | REQ-F-003、REQ-F-008 |
| 禁用控件被隐藏而不是保持可见 | REQ-F-002 |
| Modal 的 Escape 关闭在部分按键输入下失效 | REQ-F-045 |
| Modal 缺少名称、模态语义和焦点管理 | REQ-F-046、REQ-F-047 |
| 固定视觉值导致应用无法统一主题 | REQ-F-004 |
| 组件样式意外改变宿主页面外部元素 | REQ-F-005 |
| 文档能力与实际包行为不一致 | REQ-NF-007 |
| 关键行为缺少可追溯验证 | REQ-NF-008 |
| 应用级功能持续进入组件库范围 | §1.2.2 范围外 |
| 纯视觉分隔被辅助技术重复读取 | REQ-F-024 |
| Select 重复值或禁用选项导致选择结果不明确 | REQ-F-075、REQ-F-083 |
| 动态删除 Tree 节点后焦点丢失 | REQ-F-077 |
| Table 将加载或错误状态误显示为空数据 | REQ-F-078 |
| 宽 Table 内容被裁切或无法通过键盘访问 | REQ-F-079 |
| 增强能力未与首版范围隔离 | REQ-F-080、REQ-F-081、REQ-F-082、REQ-F-084、REQ-F-085 |
