# EckyLanguage

EckyLanguage 是一个以 C# 编写的轻量级通用脚本与数据格式类库。它希望用简单、易读的文本格式处理应用配置、结构化记录，并进一步通过脚本调用应用功能。

项目把语言用途分成三类：配置语言、记录语言和执行脚本。前两类已有基础实现；执行脚本仍处于早期探索阶段，尚未完成，不适合作为生产环境中的脚本引擎使用。

## 三类格式

### 配置语言

配置语言采用接近 INI 的文本格式，用 `[节名]` 分组，并以 `键 = 值` 保存配置：

~~~ini
[Device]
Port = COM1
BaudRate = 115200
~~~

`EckyLanguage` 仓库中的 `Config` 提供底层配置文件读写能力：按节名、键名读取字符串，并可将新值写回对应配置项。需要直接控制读写时，可以调用 `ReadString`、`WriteString`。配套项目中的 `Config3_0` 也实现了相同用途的 INI 风格读写接口，配置绑定模型使用的是这一实现。

#### 通过继承绑定配置项

在配套的 [cn.eckystudio.m_dotNet](https://github.com/mynameyi/cn.eckystudio.m_dotNet) 项目中，`ConfigManagementModel` 提供了更方便的配置模型：定义一个继承它的类，在类中声明公开的 `ConfigItem` 字段；基类会在初始化时扫描这些字段，并把它们绑定到配置文件。`DataItem` 特性可以一次说明默认值、配置节和键名。这样，配置的声明集中在一个类里，增删或调整映射时不必在业务代码各处重复读写配置。

~~~csharp
using EckyStudio.M.BaseModel.DataManagementModel;

public sealed class DeviceConfig : ConfigManagementModel
{
    public DeviceConfig(string fileName) : base(fileName) { }

    // DataItem 参数依次为：默认值、节名、键名
    [DataItem("COM1", "Device", "Port")]
    public ConfigItem Port;

    [DataItem("115200", "Device", "BaudRate")]
    public ConfigItem BaudRate;
}

var config = new DeviceConfig("app.ini");
string port = config.Port.StringValue;
int baudRate = config.BaudRate.IntValue;

// 默认 AutoFlushMode 下，Set 会立即写回配置文件
config.Port.Set("COM2");
config.Dispose();
~~~

未显式指定的节名使用 `General`，键名使用字段名，默认值为空字符串。`ConfigItem` 可通过 `StringValue`、`IntValue`、`BoolValue`、`FloatValue` 和 `DoubleValue` 读取常用类型；`Set` 用于更新配置值。此绑定模型针对公开的 `ConfigItem` 字段设计，并非任意 C# 属性或类型的自动序列化。配置管理辅助类位于配套项目中，当前仓库的 `Config` 本身仍是底层读写 API。

### 记录语言

用于按条目保存配置记录或运行数据。每条记录由记录边界和分隔符组成，Record1_0 提供基本的追加、顺序读取与记录编辑能力。

~~~text
[2026-10-09,DeviceConfigured,COM1,115200]
[2026-10-09,DeviceConfigured,COM2,9600]
~~~

记录字段的边界、分隔符和转义行为以对应实现为准；当前仓库中的标准记录格式使用方括号包裹记录，默认以逗号分隔字段。

### 执行脚本

目标是用简洁的脚本调用应用中自行实现的功能，便于把常用操作写成可读、可复用的文本指令。当前代码包含早期的脚本解析和方法注册尝试，但完整的命令调用与执行流程尚未开发完成，语法也不应视为稳定规范。此部分目前主要用于表达项目方向，不建议用于真实业务执行。

## C# 类库

项目命名空间为 EckyLanguage，程序集名称为 el。主要类型包括：

- Config：INI 风格的配置读写。
- Record 和 Record1_0：结构化记录读写。
- Action：执行脚本的早期实现。
- StringHelper：字符串处理辅助方法。

配置读写示例：

~~~csharp
using EckyLanguage;

var config = new Config("app.ini");
string port = config.ReadString("Device", "Port", "COM1");
config.WriteString("Device", "Port", port);
config.Dispose();
~~~

## 当前实现状态

这是一个较早期的个人类库项目，不是已经稳定发布的通用脚本平台。当前源码仍有未完成部分，例如 Config 的 ReadVariant、GetSections 和 GetKeyValuePair 目前是占位实现；Action 的完整脚本执行能力也尚未完成。使用前请先根据自身需求检查并验证具体方法。

## 构建信息

- 项目文件：EckyStudio.M.EckyLanguage.csproj
- 目标框架：.NET Framework 4.0
- 输出程序集：el
- 主要使用 .NET Framework 自带的 System、System.Data 和 System.Xml 等程序集。

仓库未包含 Visual Studio Solution、自动化测试项目或示例应用。可使用兼容 .NET Framework 4.0 的 Visual Studio/MSBuild 打开项目文件；较新的开发环境可能需要安装相应的 .NET Framework targeting pack 或调整项目配置。

## 目录结构

~~~text
EckyLanguage.cs                         配置、记录及早期执行脚本实现
Record1_0.cs                            独立的基础记录格式实现
StringHelper.cs                         字符串辅助方法
EckyStudio.M.EckyLanguage.csproj        .NET Framework 类库项目
doc/EckyLanguage计划.txt                 开发计划与未完成事项
~~~

## 许可

仓库根目录目前没有通用 LICENSE 文件。源码复用或再分发前，请先确认相应授权范围。