## 功能需求汇总与实现 TODO

以下内容汇总自仓库中的 README.md、docs/ENHANCEMENTS.md、docs/AUTHENTICATION.md 与 docs/GITHUB_WORKFLOWS_README.md，包含已实现功能、短期与长期增强建议，以及每项的优先级、实现难度估算、受影响文件与具体实现 TODO（含验收标准），以便直接用于拆分 issue 或开发任务。

摘要（快速概览）
- 已实现/当前功能：仪表板、容器管理、镜像管理、卷管理、网络管理（新增页面）、多语言、本地登录认证、实时列表刷新等。
- 短期待补充：容器实时 CPU/内存监控、搜索/筛选、批量操作等。
- 长期计划：Docker Compose 支持、容器 exec（终端）、从 Dockerfile 构建镜像、镜像仓库/注册表管理、基于资源的用户访问控制等。
- 认证增强（未来）：注册、忘记/重置密码、邮箱验证、2FA、角色/权限、用户管理界面、会话/登录历史等。
- CI/CD / 部署：自动构建多平台镜像、打标签并推送、Docker Compose 部署说明。

详细功能点（按类别、去重并附来源）

一、界面与整体特性（README.md / ENHANCEMENTS.md）
- 仪表板（Dashboard）
  - 功能：系统总览（容器/镜像/卷/网络计数、容器状态分布、Docker 版本、OS、架构、内存/CPU），一键跳转到管理页面。
  - 状态：已新增页面（ENHANCEMENTS.md 表示完成）。
- 现代化 UI（MudBlazor）
  - 功能：响应式、使用 MudBlazor 组件（MudGrid、MudPaper、MudDialog 等）。
- 多语言支持（i18n）
  - 功能：支持 zh-cn、en-us、ja、ko-kr、fr-fr、es 等（CSV 驱动）。
- 实时更新/自动刷新
  - 功能：进行操作后自动刷新相关列表（事件驱动的更新）。

二、容器管理（README.md / ENHANCEMENTS.md）
- 列表展示与操作：启动、停止、重启、暂停/恢复、删除（force）、查看日志（last 500 行）、inspect JSON、刷新、上下文动作。
- 短期计划：容器实时 CPU/内存 监控、搜索/筛选、批量操作、日志 tail 流式查看（可选）。

三、镜像管理（README.md / ENHANCEMENTS.md）
- 功能：查看镜像、pull/push、导入/导出 tar、inspect、删除、刷新。
- 长期：从 Dockerfile 构建镜像、增强 registry 管理。

四、卷（Volumes）管理
- 功能：查看、创建（驱动选择）、inspect、prune（清理未使用卷）、删除、刷新；支持分页与流式处理。

五、网络（Networks）管理
- 新增页面：列出、创建（驱动选择）、删除（保护系统网络）、inspect、刷新。

六、搜索、筛选、分页与性能
- 在所有列表中添加搜索/筛选，并使用分页与事件驱动更新以优化性能。

七、批量操作
- 支持多选并对选中项批量运行操作（启动/停止/删除等）。

八、可扩展/高级功能（长期）
- Docker Compose 支持、容器 Exec（Web 终端）、镜像仓库管理、基于资源的 RBAC 权限控制。

九、身份认证与用户管理（AUTHENTICATION.md）
- 当前：ASP.NET Identity + EF Core + SQLite；默认管理员；所有页面需登录。
- 未来增强：用户注册、忘记/重置密码、邮箱验证、2FA、角色/权限管理、用户管理界面、登录历史、会话管理。

十、本地化 / 国际化实现
- CSV 驱动本地化；所有用户文本通过本地化 key 获取；已扩展多语言与 50+ keys。

十一、架构/开发规范
- MVVM 模式（所有业务逻辑在 ViewModel）、DI 自动注册、事件驱动状态更新、source generators（CSV->代码、Mapperly 映射）。

十二、运维 / CI & 部署
- GitHub Actions: 监测 csproj <Version> 变更触发 multi-platform 镜像构建、打标签并推送至配置的镜像仓库。
- Docker 部署: docker-compose 支持与运行说明。

十三、安全与操作注意
- Docker socket 访问风险提示、删除操作使用 force 标志、生产建议启用 HTTPS、修改默认管理员密码、考虑 2FA。

十四、测试与质量点
- 自动化/手动测试清单、流式处理大文件以防 OOM、错误处理与验证。

实现 TODO（按模块，含优先级 / 难度 / 估时 / 受影响文件 / 验收标准）

注：难度 S=简单(0.5-2 人天)、M=中等(2-6 人天)、L=复杂(>6 人天)。估时为单人含测试的粗略范围。

一、仪表板（Dashboard） — 优先级: High，难度: S，估时: 0.5–1.5 人天
受影响文件：ViewModels/DashboardViewModel.cs, Pages/Dashboard.razor, Pages/Home.razor, Shared/NavMenu.razor, Localization.table.csv
TODO：
  1. 点击卡片跳转带筛选参数（实现 QueryString 或 StateManager 传参）。
     验收：点击后 Container 页面应用筛选且 URL 可复现。
  2. 自动刷新节流与错误重试策略。
     验收：短期网络波动时 UI 稳定，重试符合策略。

二、容器管理（Containers） — 优先级: High，难度: M，估时: 2–5 人天
受影响文件：ViewModels/ContainersPageViewModel.cs, Pages/ContainersPage.razor, Components/Dialog (logs/inspect), Localization.table.csv
TODO：
  1. 容器实时 CPU/内存监控（Short-term）。
     难度 M，估时 3–5 人天，验收：容器详情或列表显示实时 CPU/内存，刷新频率可配置。
  2. 搜索/筛选（Short-term）。
     难度 S–M，估时 1–2 人天，验收：即时过滤并支持 URL 查询参数。
  3. 批量操作（Short-term）。
     难度 M，估时 2–4 人天，验收：能对多选项进行批量操作并展示逐项结果。
  4. 日志 tail 流式查看（可选）。
     难度 M–L，估时 3–7 人天，验收：tail 模式稳定、支持停止与下载。

三、镜像管理（Images） — 优先级: High，难度: M，估时: 1–4 人天
受影响文件：ViewModels/ImagesPageViewModel.cs, Pages/ImagesPage.razor, wwwroot/app.js, Localization.table.csv
TODO：
  1. 从 Dockerfile 构建镜像（Long-term）。
     难度 L，估时 1–3 周，验收：上传 Dockerfile 启动构建并可查看构建日志。
  2. 导出/导入流式处理与进度指示。
     难度 M，估时 1–3 人天，验收：大文件导出稳定且带进度。
  3. 支持 registry 身份验证用于 pull/push（若未完善）。
     难度 M，估时 2–4 人天，验收：凭据安全存储并用于操作。

四、卷管理（Volumes） — 优先级: Medium，难度: S–M，估时: 1–2 人天
受影响文件：ViewModels/VolumesPageViewModel.cs, Pages/VolumesPage.razor
TODO：
  1. 创建卷时支持高级选项（labels、driver_opts）。
     验收：UI 支持并创建成功。
  2. Prune 增加 dry-run 预览与确认。
     验收：用户可预览将被删除的卷并确认。

五、网络管理（Networks） — 优先级: Medium，难度: S–M，估时: 1–3 人天
受影响文件：ViewModels/NetworksPageViewModel.cs, Pages/NetworksPage.razor, NavMenu.razor
TODO：
  1. 创建网络时支持 subnet/gateway/labels/attachable 等高级字段。
     验收：创建后 inspect 显示完整配置。
  2. 系统网络保护策略验证。
     验收：系统网络不可删除且 UI 明示。

六、全局搜索/分页/过滤 — 优先级: High，难度: M，估时: 2–4 人天
受影响文件：各 PageViewModel、公共 SearchBar, Pagination 组件
TODO：实现可复用 SearchBar，ViewModel 支持 query 与分页并保证 URL 可复现。

七、批量操作 — 优先级: Medium，难度: M，估时: 2–4 人天
受影响文件：各 Page 与 ViewModel，Shared OperationService
TODO：实现列表多选、批量执行与逐项结果回显。

八、Docker Compose 支持（长期） — 优先级: Low→Medium，难度: L，估时: 2–4 周
受影响文件/新增模块：ComposePage.razor, ComposePageViewModel.cs, ComposeService
TODO：支持上传/编辑 docker-compose.yml 并一键启动/停止整个 stack，验收：能正确启动并查看日志。

九、容器 Exec / Web Terminal（长期） — 优先级: Medium，难度: L，估时: 2–3 周
受影响/新增：TerminalPage.razor, TerminalViewModel.cs, TerminalHub (SignalR), 前端 xterm.js 集成
TODO：实现 Exec 会话代理、鉴权与会话管理，验收：交互式 shell 功能正常且安全。

十、镜像仓库 / 注册表管理（长期） — 优先级: Low→Medium，难度: L，估时: 2–4 周
受影响/新增：RegistryPage.razor, RegistryViewModel.cs, Credentials 管理
TODO：实现凭据管理、列出仓库、镜像迁移与 push/pull 验证，验收：凭据安全存储并可用于操作。

十一、认证与权限增强 — 优先级: High，难度: M–L
受影响文件：Account 页面、ViewModels/LoginViewModel.cs、UserManagementViewModel.cs、数据库模型
TODO：实现注册/邮箱验证/忘记密码/2FA/角色管理，分别列出验收标准（详见文档）。

十二、本地化改进 — 优先级: Medium，难度: S，估时: 0.5–2 人天
TODO：补全缺失 key、自动化检测缺失翻译，验收：切换语言页面无缺失文本。

十三、性能与流式处理 — 优先级: High，难度: M，估时: 1–4 人天
TODO：确保导出镜像/日志等为流式实现并加进度与内存保护，验收：大文件导出无 OOM。

十四、CI/CD / GitHub Actions — 优先级: Medium，难度: S–M，估时: 0.5–2 人天
受影响文件：.github/workflows/*
TODO：添加手动运行表单参数、引入镜像扫描（Trivy）、验收：版本更新触发构建且扫描步骤生效。

十五、测试用例与自动化 — 优先级: High，难度: M，估时: 1–3 人天
TODO：将手动测试清单转为自动化集成测试或 QA 文档，验收：CI 中能跑通关键路径测试。

附：如何使用本文件
- 可直接将本文拆分成多个 GitHub issues。我可以为你把选定的 N 项生成 issues 草稿并创建（或先生成 JSON draft）。
- 若需要，我也可把每个 TODO 拆成更细的开发/测试/文档子任务并提供更精确估时。

---

(文件由仓库文档与 ENHANCEMENTS.md、AUTHENTICATION.md 的内容合并整理，生成日期：2025-10-23)