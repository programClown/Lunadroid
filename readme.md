# Lunadroid

## 项目概述

**Lunadroid** 是一款基于 .NET 10 MAUI 框架开发的 Android 视频播放与下载应用，同时支持 Android 手机和电视设备。应用提供影视资源搜索、在线播放（HLS 流媒体）、下载缓存以及播放历史等功能。

- **项目名称：** Lunadroid
- **框架：** .NET 10 MAUI（仅 Android 平台）
- **用途：** Android 手机 + 电视 视频播放/下载应用
- **最低 SDK：** Android 10（API 29）

---

## 技术栈

| 类别 | 技术 | 版本 |
|------|------|------|
| 运行时 | .NET SDK | 10.0.103 |
| MAUI 工作负载 | maui-android | 10.0.20 |
| UI 工具包 | CommunityToolkit.Maui | 14.2.0 |
| 视频播放 | CommunityToolkit.Maui.MediaElement | 10.0.0 |
| MVVM 框架 | CommunityToolkit.Mvvm（源生成器） | 8.4.2 |
| UI 框架 | UraniumUI + Material 主题 | 2.16.0 |
| 数据库 ORM | sqlite-net-pcl | 1.9.172 |
| SQLite 原生绑定 | SQLitePCLRaw.bundle_green | 2.1.11 |
| 日志框架 | Serilog | 4.3.1 |
| 日志文件输出 | Serilog.Sinks.File | 7.0.0 |
| 日志 DI 扩展 | Serilog.Extensions.Logging | 10.0.0 |
| 单元测试 | xUnit | 2.9.3 |

---

## 项目结构

```
Lunadroid/
├── Lunadroid.sln                          # 解决方案文件
├── Lunadroid.App/                         # MAUI Android 应用程序
│   ├── Pages/                             # 9 个页面，扁平目录结构（无子文件夹）
│   │   ├── WelcomePage                    # 欢迎界面
│   │   ├── TermsPage                      # 用户协议确认
│   │   ├── HomePage                       # 搜索 + 影视列表
│   │   ├── MovieDetailPage                # 影视详情 + 剧集列表
│   │   ├── PlayerPage                     # MediaElement 视频播放器
│   │   ├── HistoryPage                    # 播放 + 下载历史
│   │   ├── MySourcesPage                  # 视频源管理
│   │   ├── SettingsPage                   # 应用设置
│   │   └── PinLockPage                    # PIN 安全锁
│   ├── ViewModels/                        # 7 个 ViewModel（使用 MVVM 源生成器）
│   ├── Converters/                        # 5 个值转换器
│   ├── Services/                          # AppServices 静态服务定位器
│   ├── MauiProgram.cs                     # 应用构建器、依赖注入、日志配置
│   ├── App.xaml/.cs                       # 主题、导航、引导流程
│   └── AppShell.xaml/.cs                  # 4 标签页 Shell 导航 + 路由注册
├── Lunadroid.Core/                        # 核心类库（net10.0）
│   ├── Models/                            # 7 个模型类（含 SQLite 特性标注）
│   ├── Services/                          # 5 个核心服务
│   │   ├── DatabaseService                # 6 张表的完整 CRUD 操作
│   │   ├── MovieApiService                # 影视 CMS API 客户端
│   │   ├── HlsDownloadService             # HLS m3u8 下载器
│   │   ├── AppConfigService               # JSON 配置管理器
│   │   └── LoggingService                 # Serilog 封装
│   └── Helpers/                           # RelativeTimeHelper（相对时间工具）
├── Lunadroid.Tests/                       # 57 个单元测试（xUnit）
│   ├── DatabaseServiceTests.cs            # 34 个测试
│   ├── RelativeTimeHelperTests.cs         # 17 个测试
│   └── AppConfigServiceTests.cs           # 6 个测试
└── readme.md                              # 本文档
```

---

## 构建环境搭建

### 1. 安装 .NET 10 SDK

安装 .NET SDK 10.0.103，并通过以下命令安装 MAUI Android 工作负载：

```bash
dotnet workload install maui-android
```

已安装工作负载版本：10.0.20

### 2. 安装 Android SDK

下载 Android SDK commandlinetools-win-11076708，安装至 `C:\Android\sdk`。

安装以下组件：

- **平台：** android-34、android-36
- **构建工具：** build-tools 34.0.0
- **平台工具：** platform-tools

---

## 构建过程及问题解决

在项目的构建过程中，遇到了以下编译错误并逐一解决：

### 1. XA5300：找不到 Android SDK

**错误：** 构建系统无法定位 Android SDK。

**解决：** 将 Android SDK 安装至 `C:\Android\sdk`，并在构建时通过 `AndroidSdkDirectory` 属性指定路径。

### 2. XA5207：找不到 API level 36 的 android.jar

**错误：** 项目目标 API 为 36，但本地 SDK 缺少对应平台的 `android.jar`。

**解决：** 通过 SDK Manager 安装 android-36 平台。

### 3. CS0234：MediaElement 命名空间错误

**错误：** 引用了不正确的 `using` 指令，导致 MediaElement 命名空间无法解析。

**解决：** 移除了错误的 `using` 指令。

### 4. CS0200：RequestedTheme 为只读属性

**错误：** 尝试对 `Application.Current.RequestedTheme` 赋值，但该属性为只读。

**解决：** 改用 `Application.Current.UserAppTheme` 来设置应用主题。

### 5. CS0104：ServiceProvider 命名冲突

**错误：** `ServiceProvider` 类名与框架中已有类型产生歧义。

**解决：** 将服务定位器类重命名为 `AppServices`，避免命名冲突。

### 6. CS1739 / CS7036：MediaElement 参数问题

**错误：** MediaElement 的注册方法参数不匹配。

**解决：** 使用 `UseMauiCommunityToolkitMediaElement(true)` 进行注册，传入 `true` 参数以启用自动初始化。

### 7. CS1061：缺少 DatabaseService 方法

**错误：** ViewModel 中调用了 DatabaseService 中尚不存在的方法。

**解决：** 在 DatabaseService 中添加了以下方法：

- `GetMovieSourceByIdAsync` —— 根据 ID 获取影视源
- `InsertOrUpdateMovieAsync` —— 插入或更新影视记录
- `InsertEpisodesAsync` —— 批量插入剧集
- `InsertDownloadRecordAsync` —— 插入下载记录

### 8. CS1061：ViewModel 中方法名不匹配

**错误：** ViewModel 调用的方法名与 DatabaseService 中实际的方法名不一致。

**解决：** 修正 ViewModel 中的方法调用，使其匹配 DatabaseService 的实际 API。

### 9. CS1503：删除方法类型不匹配

**错误：** 删除操作传入的参数类型与方法签名不匹配。

**解决：** 改用带 `ById` 后缀的重载方法（如 `DeleteXxxByIdAsync`），确保传入 ID 类型正确。

### 10. CS0618：DisplayAlert 已弃用警告

**错误：** `DisplayAlert` 方法标记为已过时，产生编译器警告。

**说明：** 此为已知警告，不影响编译和运行，暂未处理。

---

## 测试结果

全部测试通过，测试覆盖情况如下：

| 测试类 | 测试数量 | 说明 |
|--------|---------|------|
| DatabaseServiceTests | 34 | 6 张表的完整 CRUD 操作及全局操作 |
| RelativeTimeHelperTests | 17 | 相对时间、时长、文件大小格式化 |
| AppConfigServiceTests | 6 | 默认值、更新、重置、持久化 |
| **合计** | **57** | **全部通过，0 失败** |

---

## 构建命令

在项目根目录执行以下命令进行构建：

```bash
dotnet build -p:AndroidSdkDirectory="C:\Android\sdk"
```

## 测试命令

运行单元测试：

```bash
dotnet test Lunadroid.Tests
```

---

## 关键设计决策

### 页面目录结构
Pages 目录采用**扁平结构**（无子文件夹），所有 9 个页面直接放置在 `Pages/` 下，符合项目需求规范。

### MVVM 架构
使用 CommunityToolkit.Mvvm 源生成器实现 MVVM 模式：

- `[ObservableProperty]` —— 自动生成可通知属性
- `[RelayCommand]` —— 自动生成命令绑定

### 服务定位
采用静态 `AppServices` 类进行服务定位，而非在页面中使用构造函数依赖注入。这简化了页面实例化流程，同时保持了服务的集中管理。

### 数据持久化
使用 sqlite-net-pcl ORM 进行所有数据持久化操作，共 6 张数据表，支持完整的 CRUD 操作。

### 日志系统
基于 Serilog 的文件日志系统，采用**每日滚动文件**策略，便于问题追踪和调试。

### 导航架构
Shell 导航采用 4 标签页布局：

1. **首页（Home）** —— 搜索与影视列表
2. **历史（History）** —— 播放与下载历史
3. **视频源（My Sources）** —— 视频源管理
4. **设置（Settings）** —— 应用设置

### 云源导入
支持从可配置 URL 导入云端视频源，方便扩展影视资源。

### PIN 锁安全功能
内置 PIN 锁功能，保护应用访问安全。

### HLS 下载
实现 HLS（m3u8）下载功能，支持：

- m3u8 播放列表解析
- 分片下载与合并
- 下载进度实时跟踪

---

## Android 权限

应用在 `AndroidManifest.xml` 中声明了以下权限：

| 权限 | 用途 |
|------|------|
| `INTERNET` | 网络请求（API 调用、视频流、下载） |
| `ACCESS_NETWORK_STATE` | 检测网络连接状态 |
| `FOREGROUND_SERVICE` | HLS 后台下载前台服务 |
| `WRITE_EXTERNAL_STORAGE` | 写入外部存储（下载文件） |
| `READ_EXTERNAL_STORAGE` | 读取外部存储 |

### 设备兼容性

- **Leanback 支持：** 兼容 Android TV 设备
- **触屏支持：** 兼容 Android 手机和平板设备
- **最低版本：** Android 10（API 29）
