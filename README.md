# FocasDemo

**Fanuc FOCAS 通信示例项目（新手友好）**

本仓库用于演示如何通过 C# 调用 Fanuc FOCAS 接口与机床通信，包含：
- WinForms 图形化示例（操作演示更直观）
- .NET 6 控制台示例（最小连接流程）
- 常见读写接口示例（诊断、宏变量、参数、时间、操作履历等）

---

## 项目结构

```text
FocasDemo/
├── Cherish.IOCNC.Focas/          # .NET Framework 4.7.2 WinForms 示例
│   ├── Form1.cs                  # 主要示例逻辑（连接、读写、日志）
│   ├── Form1.Designer.cs         # 界面定义（按钮、分组、输入项）
│   ├── fwlib32.cs                # FOCAS C# 封装（结构体 + DllImport）
│   └── Program.cs                # WinForms 入口
├── Focas.linux/                  # .NET 6 控制台示例
│   ├── Program.cs                # 最小流程：初始化、连接、读取位置、退出
│   └── fwlib32.cs                # Linux 项目的 FOCAS 封装
├── packages/                     # NuGet 依赖（当前含 Obfuscar）
└── Cherish.IOCNC.Focas.sln       # 解决方案
```

---

## 环境准备

1. Fanuc 机床支持 FOCAS 协议。
2. 机床与开发机在同一网络，确保 IP/端口可达（默认端口常见为 `8193`）。
3. 准备 FOCAS 动态库（例如 `FWLIB32.dll` 等对应库文件）。
4. Windows 项目建议使用 Visual Studio 2022 打开 `Cherish.IOCNC.Focas.sln`。

> 提示：仓库中已包含部分 dll 与示例配置，但实际部署需以你的机床型号与 FANUC 官方文档为准。

---

## 快速开始（推荐路径）

### 1）先跑通 WinForms 示例（推荐新手）

1. 打开 `Cherish.IOCNC.Focas.sln`。
2. 启动项目 `Cherish.IOCNC.Focas`。
3. 在界面中填入机床 IP 与端口（默认有 `127.0.0.1` 和 `8193` 示例值）。
4. 点击 **Connect**。
5. 在日志窗口查看 `Connect Success / Connect Fail`。
6. 完成后点击 **DisConnect** 释放句柄。

### 2）再看 .NET 6 控制台最小流程

`Focas.linux/Program.cs` 演示了典型生命周期：
- `cnc_startupprocess` 初始化
- `cnc_allclibhndl3` 连接
- `cnc_rdposition` 读取位置
- `cnc_exitthread` / `cnc_exitprocess` 清理

---

## 核心调用模式（务必掌握）

几乎所有 FOCAS 调用都遵循同一个模式：

1. 连接获取句柄（`cnc_allclibhndl3`）
2. 调用目标 API（`cnc_xxx`）
3. 判断返回码（`EW_OK` 为成功）
4. 断开连接并释放句柄（`cnc_freelibhndl`）

参考示例：

```csharp
var prot = ushort.Parse(this.txt_Port.Text.Trim());
_ret = Focas1.cnc_allclibhndl3(this.txt_IP.Text.Trim(), prot, 10, out _flibhndl);
if (_ret == 0)
{
    Print("Connect Success");
    _isConnect = true;
}
else
{
    Print("Connect Fail");
    _isConnect = false;
}
```

---

## 已覆盖的主要示例功能

WinForms 示例中包含以下典型功能分组：

- 报警信息读取与乱码处理（按机床语言选择编码）
- 诊断号读取
- 宏变量读取
- 参数读写
- 时间参数读取/设置
- 操作履历读取

建议学习顺序：
1. 连接/断开
2. 读取类接口（诊断、宏变量、时间）
3. 写入类接口（参数、系统时间）
4. 履历/报警等复杂结构

---

## 常见问题（FAQ）

### 1. 连接失败怎么办？
- 检查机床 IP 与端口。
- 确认网络互通（同网段、路由、防火墙）。
- 核对机床侧 FOCAS 相关参数和授权。
- 确认使用了正确版本的 FOCAS 库。

### 2. 报警信息乱码怎么办？
- 不要直接用系统默认编码。
- 先读取机床语言参数，再选择对应编码（如 ASCII / Shift-JIS / GB2312）。

### 3. 为什么同一个接口在不同机床表现不一致？
- 不同系统版本、机床选项、轴数、功能包会导致结构体字段和可用功能差异。
- 实际项目请严格对照 FANUC 官方接口文档和机床手册。

---

## 学习建议（给新人）

- 先理解 `fwlib32.cs` 的错误码、结构体和 `DllImport` 映射关系。
- 再阅读 `Form1.cs` 里每个按钮事件，建立“一个按钮 = 一个接口场景”的思维。
- 给每个接口记录 4 件事：输入参数、length 计算、返回码、输出结构。
- 最后再做工程化重构：把事件中的通信逻辑抽到 service 层。

---

## 参考与支持

- B 站： [cherish陆工](https://space.bilibili.com/38361468)
- 如本项目对你有帮助，欢迎 Star 支持。

