# 需求说明：容器 Exec 交互式终端访问（弹窗/新页面）

- 对应 Issue：#18 容器 exec 终端访问（弹窗/新页面）
- 项目：Docker.Dotnet.UI（Blazor Server + MudBlazor + Docker.DotNet，严格 MVVM）
- 日期：2025-10-24

## 背景与目标

当前容器页面（`/containers`）已具备容器列表、启动/停止、日志查看、Inspect、实时性能等能力，但缺少通过 Docker Exec 在浏览器中打开交互式终端的功能（进入容器内执行命令，例如 /bin/sh、/bin/bash 或 Windows 容器下的 powershell/cmd）。

本需求旨在：
- 在“容器列表”中为单个容器提供“终端”入口。
- 支持以弹窗（MudDialog）或全屏新页面两种方式访问交互式终端。
- 与现有 MVVM、i18n、本地化、鉴权、MudBlazor UI 风格保持一致。

非目标：
- 非覆盖完整的 SSH/WebTTY 管理平台，仅面向 Docker Exec 的简化交互。
- 不实现多用户会话共享同一终端的复杂协作场景。

## 用户故事

- 作为管理员，我希望在容器运行时，点击“终端”图标，即可在浏览器中打开一个交互式 Shell，直接执行命令诊断问题。
- 作为管理员，我希望在新页面全屏模式中获得更大的可视空间，并可随时返回容器列表。
- 作为管理员，我希望终端默认为容器内常见 Shell（Linux: /bin/sh 或 /bin/bash；Windows: powershell.exe/cmd.exe），并可选择自定义命令、工作目录、用户。
- 作为管理员，当容器停止或 Exec 会话异常时，能看到明确的错误信息，并可以快速重连。

## 交互与 UX

入口位置：
- 容器卡片操作区（已存在“启动/停止/重启/暂停/日志/统计/Inspect/删除”），新增“终端”图标按钮（例如 Icons.Material.Filled.Terminal 或近似图标）。
- 按钮可见性/状态：
  - 运行中（running）：启用。
  - 非运行状态：禁用（或点击时提示“容器未运行，无法打开终端”）。

打开方式：
- 方式 A：弹窗（MudDialog）默认高度 ~70vh，宽度 MaxWidth.Large/FullWidth，自适应暗色主题（终端常见黑底风格）。
- 方式 B：新页面（建议路由：`/containers/{id}/exec`），顶部显示容器名与返回按钮；全屏占位，适合长时间操作。
- 允许通过 UI 切换（“在新页面中打开”链接）或通过参数选择默认方式。

终端区域：
- 字体：等宽字体、暗色背景、12px-14px。
- 输入：单行键入即时发送；支持常见按键（Enter、Backspace、方向键等）。
- 滚动：输出区域可滚动查看历史；提供清屏按钮。
- 状态栏：显示连接状态（连接中/已连接/已断开）、Shell 类型、窗口尺寸（cols × rows）。
- 设置面板（简易）：
  - Shell/命令（默认值按容器平台自动选择，可自定义）。
  - 工作目录（WorkingDir）。
  - 用户（User，可选）。
  - TTY 开关（默认开启）。

错误与引导：
- 当 Docker Desktop 未就绪或 API 超时，显示一致的错误提示（沿用现有错误文案与样式）。
- 会话断开/容器退出时，在终端上方显示可重连按钮。

国际化（i18n）：
- 所有文案通过 `@Localizer["KEY"]` 调用，新增 keys 见“本地化 Key 建议”。

无障碍：
- 按键操作与焦点管理；为按钮/图标提供 `title` 和 `aria-label`。

## 技术实现（与现有架构对齐）

架构与原则：
- 页面：Razor 仅负责 UI，继承 `MyComponentBase<TViewModel>`；不写“转发”包装方法。
- 逻辑：全部在 ViewModel（`ContainersPageViewModel` 或新建专用 ViewModel）中实现。
- Docker API：使用 Docker.DotNet 的 Exec 流程：
  1. `ExecCreateContainerAsync(containerId, ContainerExecCreateParameters)`
  2. `StartAndAttachContainerExecAsync(execId, false)` 获取 `MultiplexedStream`（TTY=true 时为原始流）。
  3. 后台读循环（输出 -> UI 缓冲/事件），前端键入事件 -> 写入流。
  4. 支持 `ResizeContainerExecAsync(execId, h, w)` 响应终端尺寸变化。
- 通信：Blazor Server（SignalR）下，组件事件将输入同步到服务器；后端读循环通过 `NotifyStateChanged()` 推动 UI 刷新。

ViewModel 设计建议：
- 在 `ContainersPageViewModel` 中新增最小化状态，或为终端新建 `ContainerExecViewModel`（推荐：解耦复杂性，便于在新页面重用）。

最小状态（参考）：
- `bool ShowExecDialog`
- `string? ExecContainerId`
- `string? ExecContainerName`
- `string ExecId`
- `bool IsExecConnected`
- `bool IsExecConnecting`
- `string? ExecError`
- `List<string> TerminalBuffer`（或 `StringBuilder`/环形缓冲区）
- `int Rows`, `int Cols`
- `string DefaultShell`（根据容器 OS 决定：Linux: "/bin/sh" 优先，若存在 bash 可切换；Windows: "powershell.exe"/"cmd.exe"）
- `string WorkingDir`
- `string? User`
- `bool Tty = true`

核心方法（参考）：
- `Task OpenExecAsync(containerId, containerName, options?)`：创建 Exec + 连接 + 启动读循环。
- `Task SendInputAsync(string data)`：把用户输入写入 `MultiplexedStream`。
- `Task ResizeAsync(int rows, int cols)`：调用 `Containers.ResizeContainerExecAsync`。
- `Task CloseExecAsync()`：关闭流、取消读循环，重置状态。

读写流处理：
- 读：后台 Task 循环 `ReadOutputAsync`（或 `ReadAsync`，取决于 TTY），将 UTF-8 字节解码为文本追加到 `TerminalBuffer`；注意截断策略（如最多缓存近 5,000 行）。
- 写：将键盘输入转换为字节写入；处理特殊键（Enter -> `\n` 或 `\r\n`，Backspace 等）。
- 线程安全：用 `ConcurrentQueue<string>` 或锁管理缓冲；每次批量追加后 `NotifyStateChanged()`。

页面与路由：
- 弹窗：在 `ContainersPage.razor` 中参考“日志/Inspect/Stats”模式，新增 Exec 对话框区域。
- 新页面：新增 `Components/Pages/DockerPages/ContainerExecPage.razor`，路由 `/containers/{id}/exec`。继承 `MyComponentBase<ContainerExecViewModel>`，根据 route 参数调用 `Vm.InitializeAsync()` -> `OpenExecAsync`。

UI 控件建议（MudBlazor）：
- 输出：`MudPaper` 容器 + 自定义样式（黑底、等宽字体、自动滚动到底部）。
- 输入：隐藏文本框捕获键盘，或在输出容器上捕获 `onkeydown`；提供一个可见的 `MudTextField` 作为命令行输入（回车发送，保留历史）。
- 操作按钮：连接/断开、清屏、复制全部、在新页面打开、重置大小。

安全与鉴权：
- 仅登录用户可用（页面顶层已有 `[Authorize]`）。
- 可选：增加简单的访问审计记录（未来扩展）。

错误处理与空状态：
- 容器未运行：禁止进入或显示提示。
- Docker 超时：显示“请确认 Docker Desktop 已启动”。
- Exec 创建失败：显示错误消息。
- 读循环异常：显示“会话已断开”并提供重连按钮。

性能与稳定性：
- 缓冲区上限：限制输出缓存行数；超出丢弃最早内容。
- 自动滚动：只在用户未上滚时自动跟随底部；用户向上滚动时暂停跟随。
- 资源释放：离开页面/关闭对话框时停止后台任务、关闭流、取消 Token。

可测试性：
- ViewModel 层提供可注入的时间/流抽象，便于单元测试读写逻辑与状态迁移。

## 接口/数据契约（建议）

- Exec 选项（前端到 VM）：
  - `Command`（string[]，默认根据平台生成）
  - `WorkingDir`（string?）
  - `User`（string?）
  - `Tty`（bool，默认 true）
  - `AttachStdin/Stdout/Stderr`（默认 true/true/true）

- 终端事件：
  - 输入：`SendInputAsync(string data)`
  - 尺寸变更：`ResizeAsync(int rows, int cols)`
  - 生命周期：`OpenExecAsync` / `CloseExecAsync`

- 成功标准：
  - 连接建立后 1s 内能在容器内执行 `echo` 并看到输出。
  - 调整窗口列/行后容器端得到 `winsize` 更新（验证不严格，可通过不报错和排版变化判断）。

## 兼容性
- Linux/Mac Docker：`/var/run/docker.sock`（已由现有 DI 自动适配）。
- Windows Docker：`npipe://./pipe/docker_engine`（已由现有 DI 自动适配）。
- 容器镜像需包含相应 Shell（若无 `/bin/sh` 则回退 `/bin/bash`；Windows 容器优先 `powershell.exe`，再回退 `cmd.exe`）。

## 验收标准（Acceptance Criteria）
1. 在容器卡片操作区看到“终端”按钮：运行中容器可点击，停止容器显示禁用或提示。
2. 点击后弹出终端对话框；能够输入命令并看到输出；支持回车、方向键、删除等基本按键。
3. 对话框内提供“在新页面中打开”链接，点击跳转到 `/containers/{id}/exec` 全屏页面，功能一致。
4. 支持清屏、断开/重连、窗口大小变化（变更列/行后能继续正常输入输出）。
5. 错误场景（容器停止、超时、无 Shell）有清晰提示，不导致页面崩溃。
6. 所有新增文案接入 `Localizer`，通过 CSV 表中新增 Key 管理。
7. 符合 MVVM：Razor 不包含业务逻辑或转发包装方法，交互直接绑定到 VM。

## 日志与监控（轻量）
- 在 VM 中以 Debug 级别记录会话开始/结束、异常堆栈（避免记录用户输入的敏感内容）。
- 不引入额外外部依赖（后续可考虑可选集成）。

## 本地化 Key 建议
- TERMINAL
- OPEN_TERMINAL
- OPEN_IN_NEW_PAGE
- EXEC_CONNECTING
- EXEC_CONNECTED
- EXEC_DISCONNECTED
- EXEC_RECONNECT
- EXEC_CLEAR
- EXEC_COPY_ALL
- EXEC_SETTINGS
- EXEC_SHELL
- EXEC_WORKDIR
- EXEC_USER
- EXEC_TTY
- CONTAINER_NOT_RUNNING
- EXEC_UNAVAILABLE_SHELL
- RESIZE
- ROWS
- COLS

（按项目规则添加至 `ImmutableTables/Localization.table.csv`，9 列格式，含描述）

## 开发任务清单（建议）
1. ViewModel（推荐新建 `ContainerExecViewModel`，并 `[RegisterScoped]`）：
   - 状态字段与属性、`OpenExecAsync/SendInputAsync/ResizeAsync/CloseExecAsync` 实现。
   - 读写循环与取消令牌、缓冲管理、错误处理、`NotifyStateChanged()`。
2. 页面：
   - 弹窗：在 `ContainersPage.razor` 中新增终端对话框片段与按钮入口（参考现有 Logs/Stats/Inspect 模式）。
   - 新页面：新增 `ContainerExecPage.razor`（继承 `MyComponentBase<ContainerExecViewModel>`），路由参数解析并自动连接。
3. 样式与可用性：
   - 终端区域暗色主题、等宽字体；自动滚动；清屏、复制按钮。
   - 键盘事件绑定与特殊键处理。
4. i18n：
   - 新增文案 Key 至 CSV，重建生成代码后替换硬编码字符串。
5. 测试与验证：
   - Linux 容器（带 /bin/sh）与 Windows 容器（powershell/cmd）至少各验证一例。
   - 暂停/停止容器后的行为与提示。
6. 文档：
   - 在 `docs/USER_MANUAL.md` 增补一小节“容器终端”。

## 风险与缓解
- 不同镜像 Shell 不一致：提供默认与回退逻辑，并允许自定义。
- 输出量较大：设置缓冲上限并提供清屏；避免每字符触发 UI 刷新（批量刷新或节流）。
- 断线/重连：提供显式重连按钮；离开页面/关闭对话框时清理资源。

## 相关代码位置（现有）
- 容器列表页面：`Components/Pages/DockerPages/ContainersPage.razor`
- ViewModel：`ViewModels/ContainersPageViewModel.cs`
- 基类与本地化：`Components/MyComponentBase.cs`、`Services/MyLocalizer.cs`、`ImmutableTables/Localization.table.csv`

---

以上为与当前项目架构与约束保持一致的详细需求说明，可作为实现与验收的指导。