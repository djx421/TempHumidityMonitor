# 温湿度传感器监控程序

基于 MODBUS RTU 协议的温湿度/气压传感器数据采集与监控系统，WinForms 桌面应用，.NET Framework 4.7.2。

## ⚠️ 下载 vs 克隆

**推荐使用 `git clone`：**

```bash
git clone https://github.com/你的用户名/TempHumidityMonitor.git
```

`git clone` 从本地 Git 仓库创建文件，**不会**产生 Web 标记，解压后直接打开 `.sln` 即可编译运行。

---

**如果从 GitHub 下载 ZIP 包：**

Windows 会自动给所有文件打上"Internet 区域"标记，导致 Visual Studio 报错：

> 无法处理文件 MainForm.resx，因为它位于 Internet 或受限区域中

**解决方法：** 解压后双击 `打开项目.bat`（一键解除标记并打开解决方案），或先运行 `解除Web标记.bat` 再手动打开 `.sln`。

| 方式 | 会遇到 Web 标记？ | 说明 |
|------|:---:|------|
| `git clone` | 否 | 直接从 Git 创建文件，零配置 |
| 下载 ZIP | 是 | 需运行 `打开项目.bat` 解除标记 |

## 功能

- MODBUS RTU 串口通信，10 种读取模式（浮点/整型，单项/组合）
- 温度、湿度、气压实时采集与折线图展示
- 主图表可按按钮独立切换每条曲线的显示/隐藏
- 三个独立详情子窗体：温度 / 湿度 / 气压，各有大图表和手动 Y 轴分度
- 统计面板：最小值、最大值、平均值
- 报警功能：可独立设置温度/湿度/气压上下限，超限声光报警
- SQLite 本地数据库存储，历史数据查询与 CSV 导出
- 串口断开自动重连（最多 3 次）
- 模拟模式：无需硬件即可演示
- 嵌入式 HTTP API 服务（REST + SSE 实时推送），配套网页监控面板
- 系统托盘最小化运行，托盘右键可快速打开详情窗体
v1.2新增：
- 主窗体新增三个独立的传感器详情子窗体（温度/湿度/气压），各有完整的大图表区域和手动 Y 轴分度控制，同时主图表曲线支持按按钮独立切换显示。
## 快速开始

```bash
git clone https://github.com/你的用户名/TempHumidityMonitor.git
```

在 Visual Studio 2017+ 中打开 `TempHumidityMonitor.sln`，NuGet 包会自动恢复，直接编译运行即可。

如果下载的是 ZIP 包，请双击 `打开项目.bat` 再开始。

## 项目结构

```
TempHumidityMonitor/
├── MainForm.cs / MainForm.Designer.cs  主窗体
├── SensorDetailForm.cs                 传感器详情子窗体（温度/湿度/气压）
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
├── 打开项目.bat                       一键解除 Web 标记并打开项目
├── 解除Web标记.bat                    手动清除 Web 标记
└── TempHumidityData.db                SQLite 模板数据库
```

## 依赖

- .NET Framework 4.7.2
- System.Data.SQLite（NuGet 自动恢复）
- System.Windows.Forms.DataVisualization（.NET Framework 内置）

## 硬件连接

默认 MODBUS 地址 0x01，支持 RS-485/RS-232 串口。传感器寄存器地址和读取命令可通过 `modbus_config.json` 配置。


