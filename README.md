# 远程连接管理器 1.3

统一管理 **TightVNC** 与 **Windows 远程桌面 (RDP)** 设备。

## 功能

- VNC / RDP 双协议，卡片图标区分
- 分组（排序、+/-、右键编辑删除；删除时设备回默认组）
- 在线检测与延迟、连接前探测
- 最近连接、批量操作、导入导出
- 系统托盘、设置页
- 密码本机 DPAPI 加密

by 52pojie@aini2008ha

## 发布

```powershell
dotnet publish .\VNCManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
```

VNC 需安装 TightVNC Viewer；RDP 使用系统自带 `mstsc.exe`。
