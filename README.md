# codex_atm

`codex_atm` 是一个用于管理 Codex App 已归档线程的 Windows 桌面工具。

其中 `atm` 是 Archived Thread Manager（已归档线程管理器）的缩写。

## 程序用途

本程序用于读取本机 Codex App 的归档线程数据，并提供一个本地 GUI 界面，方便用户：

- 查看已归档线程列表
- 预览线程的基础信息与最近几条消息
- 按关键字搜索归档线程
- 将归档线程移到回收站
- 永久删除归档线程

## 数据来源

程序直接读取当前用户目录下的 Codex 归档目录：

- `%USERPROFILE%\.codex\archived_sessions`

程序不依赖网络，不修改 Codex 自身的归档格式，也不会操作该目录之外的其他 Codex 数据文件。

## 当前实现

当前版本基于 `.NET 9 + WPF` 实现，目前仅支持 Windows。
