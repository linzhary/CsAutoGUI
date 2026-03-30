---
inclusion: always
---

# CsAutoGUI 项目规范

## 项目概述

CsAutoGUI 是一个 Windows 桌面 GUI 自动化库，提供键盘/鼠标控制、窗口管理、屏幕截图和图像识别功能。
仅面向 Windows 平台，依赖 Win32 API、OpenCV 和 DirectX/WinRT 图形捕获。

## 解决方案结构

- CsAutoGUI：核心类库，命名空间 `CsAutoGUI`
- CsAutoGUI.TestConsole：测试用控制台应用，引用核心库

## 目标框架与依赖

- 目标框架：`net8.0-windows10.0.19041.0`
- 核心依赖：
  - Microsoft.Windows.CsWin32：Win32 API 互操作（通过 NativeMethods.txt 声明所需 API）
  - OpenCvSharp4 + Extensions + runtime.win：图像处理与模板匹配
  - Vortice.Direct3D11 / Vortice.DXGI / Vortice.WinUI：DirectX 11 图形捕获
- 全局 using 声明位于 `Usings.cs`，包含 Windows.Win32 相关命名空间

## 代码架构

- `AutoGUI` 是核心分部类（partial class），按功能拆分为三个文件：
  - `AutoGUI.Keyboard.cs`：键盘操作（KeyDown / KeyUp / Press），包含 VirtualKey 枚举
  - `AutoGUI.Mouse.cs`：鼠标操作（MoveTo / LeftClick / RightClick）
  - `AutoGUI.Screen.cs`：屏幕操作（FindWindowLikeTitle / LocateOnScreen / LocateOnRegion / Locate）
- `AutoWindow`：封装窗口句柄的操作类，提供窗口级别的键盘、鼠标、截图、图像识别
- `SmartRect` / `SmartPoint`：record 类型的几何结构，提供与 OpenCvSharp 和 System.Drawing 的隐式转换
- `FastScreenCapture`：基于 GDI 的非前台窗口截图（备用方案）
- `GameWindowCapture`：基于 WinRT Graphics Capture 的高性能窗口捕获

## 命名规范

- 类名、方法名、属性名：PascalCase
- 枚举值（VirtualKey）：UPPER_SNAKE_CASE
- 私有字段：_camelCase 前缀下划线
- 命名空间：与项目名一致 `CsAutoGUI`
- 新增功能文件如果属于 AutoGUI 分部类，放在 `AutoGUI/` 子目录下，文件名格式 `AutoGUI.{功能名}.cs`

## 编码规范

- 启用 nullable 引用类型，所有新代码必须正确处理可空性
- 启用隐式 using
- Win32 API 调用优先通过 CsWin32（NativeMethods.txt 声明），避免手动 DllImport
  - 如需新增 Win32 API，在 `NativeMethods.txt` 中添加函数名或常量名
  - 仅在 CsWin32 不支持的场景下才使用手动 DllImport
- 异步方法使用 async/await 模式，方法名以 Async 结尾
- 资源释放：实现 IDisposable 的类必须正确释放非托管资源
- GDI 资源（DC、Bitmap、HBitmap）必须在 finally 块中释放
- 注释使用中文 XML 文档注释（`<summary>` 标签）

## 新增 Win32 API 的流程

1. 在 `CsAutoGUI/NativeMethods.txt` 中添加所需的函数名、常量名或结构体名
2. 重新构建项目，CsWin32 会自动生成对应的 P/Invoke 代码
3. 通过 `PInvoke.{函数名}` 调用

## 图像识别相关

- 模板匹配使用 OpenCvSharp 的 `MatchTemplate`，匹配模式为 `CCoeffNormed`
- 默认置信度阈值 0.999
- 支持灰度和彩色两种匹配模式
- 坐标系统基于屏幕绝对坐标

## 测试控制台

- TestConsole 项目用于手动测试和功能验证
- 启用了 `AllowUnsafeBlocks` 和 `UseWinRT`
- 不是自动化测试项目，是交互式调试工具
