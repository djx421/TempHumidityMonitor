# 温湿度传感器监控程序

基于 MODBUS RTU 协议的温湿度/气压传感器数据采集与监控系统，WinForms 桌面应用，.NET Framework 4.7.2。

## ⚠️ 下载后必读

从 GitHub 下载 ZIP 包解压后，Windows 会自动给所有文件打上"Internet 区域"标记，导致 Visual Studio 打开项目时报错：

> 无法处理文件 MainForm.resx，因为它位于 Internet 或受限区域中

**直接双击项目根目录下的 `打开项目.bat`**，它会自动解除标记并打开解决方案。或者先运行 `解除Web标记.bat` 再手动打开 `.sln`。

如果使用 `git clone` 下载则不会遇到此问题。

## 功能

- MODBUS RTU 串口通信，10 种读取模式（浮点/整型，单项/组合）
- 温度、湿度、气压实时采集与折线图展示
- 统计面板：最小值、最大值、平均值
- 报警功能：可独立设置温度/湿度/气压上下限，超限声光报警
- SQLite 本地数据库存储，历史数据查询与 CSV 导出
- 串口断开自动重连（最多 3 次）
- 模拟模式：无需硬件即可演示
- 嵌入式 HTTP API 服务（REST + SSE 实时推送），配套网页监控面板
- 系统托盘最小化运行

## 快速开始

```bash
git clone https://github.com/你的用户名/TempHumidityMonitor.git
```

在 Visual Studio 2017+ 中打开 `TempHumidityMonitor.sln`，NuGet 包会自动恢复（SQLite），直接编译运行即可。

如需修改端口：默认 HTTP API 端口为 8090，可在程序左侧"网页监控"面板中调整。

## 项目结构

```
TempHumidityMonitor/
├── MainForm.cs / MainForm.Designer.cs  主窗体
├── Program.cs                          入口
├── Models/
│   └── SensorData.cs                   数据模型
├── Services/
│   ├── ModbusService.cs                MODBUS 协议处理
│   ├── DatabaseService.cs             SQLite 数据存取
│   ├── LogService.cs                  文件日志
│   └── ApiService.cs                  嵌入式 HTTP API + SSE
├── Properties/
│   ├── Settings.settings              用户设置（串口参数/报警阈值）
│   └── Resources.resx                 资源文件
├── web/
│   └── index.html                     网页监控面板
├── modbus_config.json                  MODBUS 命令配置
└── TempHumidityData.db                SQLite 模板数据库
```

## 依赖

- .NET Framework 4.7.2
- System.Data.SQLite（NuGet 自动恢复）
- System.Windows.Forms.DataVisualization（.NET Framework 内置）

## 硬件连接

默认 MODBUS 地址 0x01，支持 RS-485/RS-232 串口。传感器寄存器地址和读取命令可通过 `modbus_config.json` 配置。
