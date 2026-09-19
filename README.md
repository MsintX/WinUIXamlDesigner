<p align="center">
<img src="[[https://cdn.phototourl.com/free/2026-08-07-a4024ebb-a0ef-4509-823d-c323464b7f07.png]([https://img.remit.ee/i/45JuAoErsgmO](https://implicit-amaranth-hjznfrr1.edgeone.dev/))](https://implicit-amaranth-hjznfrr1.edgeone.dev/)" width="150" alt="Logo"/>
</p>

<div align="center">

# WinUI XAML Designer

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/MsintX/WinUIXamlDesigner)](https://github.com/MsintX/WinUIXamlDesigner/releases) ![GitHub Release Date](https://img.shields.io/github/release-date/MsintX/WinUIXamlDesigner) 
![GitHub All Releases](https://img.shields.io/github/downloads/MsintX/WinUIXamlDesigner/total) 
![GitHub stars](https://img.shields.io/github/stars/MsintX/WinUIXamlDesigner?style=flat) 
![GitHub forks](https://img.shields.io/github/forks/MsintX/WinUIXamlDesigner)
![GitHub issues](https://img.shields.io/github/issues/MsintX/WinUIXamlDesigner)
![GitHub license](https://img.shields.io/github/license/MsintX/WinUIXamlDesigner)
![GitHub last commit](https://img.shields.io/github/last-commit/MsintX/WinUIXamlDesigner)

`WinUI XAML Designer`是一个独立于 Visual Studio 的轻量级 WinUI 3 XAML 可视化编辑器。

</div>

## 当前定位

本项目采用 **WinUI 3 + Single-project MSIX / Packaged**，不需要额外的 Windows Application Packaging Project。

核心路径：

```text
打开 XAML
  ↓
解析 → 内存模型
  ↓
自绘设计视图
  ↓
工具箱直接拖拽
  ↓
属性编辑 / x:Name
  ↓
Click / Tapped / Loaded
  ↓
Roslyn 修改 C#
  ↓
事务写回 XAML + C#
  ↓
Visual Studio 重载
```

## 主要功能

- WinUI 3 自绘 Designer
- Grid（网格布局）设计
- 工具箱原生拖拽
- 30+ 常用 WinUI 控件
- 控件/Window 选择
- 动态属性启用/禁用
- WindowTitleBar 预览
- 关于 ContentDialog
- Tip 随机提示，每分钟切换一次
- Undo / Redo
- 空白 XAML 自动创建默认 1×1 Grid
- XAML Property Element 识别
- 复杂/暂不支持的 XAML 尽量保留
- x:Name 驱动 Click / Tapped
- Roslyn C# handler 生成/重命名
- Ctrl+S、Enter、LostFocus 提交属性
- XAML + C# 双文件写回与失败回滚

## 项目结构

```text
WinUIXamlDesigner/
├─ Assets/
├─ Models/
├─ Services/
├─ Properties/
│  ├─ launchSettings.json
│  └─ PublishProfiles/
├─ App.xaml
├─ App.xaml.cs
├─ MainWindow.xaml
├─ MainWindow.xaml.cs
├─ Package.appxmanifest
├─ WinUIXamlDesigner.csproj
└─ WinUIXamlDesigner.sln
```

## Packaged 调试

这是 **Single-project MSIX / Packaged** 项目：

- `EnableMsixTooling=true`
- `WindowsPackageType=MSIX`
- `AppxPackage=true`
- `Package.appxmanifest` 位于项目根目录
- `Properties/launchSettings.json` 使用 `MsixPackage`
- 当前默认平台为 x64

Visual Studio 2026+ 的 Single-project MSIX 调试使用 `MsixPackage` 启动 profile。

## 开发签名证书

工程带有本地开发证书：

`WinUIXamlDesigner_TemporaryKey.pfx`

它只用于本地开发/测试，不用于正式发布。正式发布前应替换为自己的签名证书。

## 作者

**MisntX & ChatGPT**
