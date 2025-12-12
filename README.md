# Sprite社游戏steam版通用解包工具

> 参考自 https://www.52pojie.cn/thread-1718909-1-1.html



**AokanaUnpacker** 是一个专为 **Sprite** 社（代表作《苍之彼方的四重奏》）steam移植版 开发的资源解包工具。

本工具针对 Unity 引擎中自定义的 `.dat` 资源文件进行解密和提取。目前支持自动识别多种加密密钥，并支持通过简单的配置扩展以支持该社团未来的新游戏。

- 支持直接拖入 `.dat` 文件进行读取。
- 支持直接拖入文件夹作为输出目录。


### 环境要求

  * Windows 10 / 11
  * .NET Framework 4.7.2 或更高版本

### 使用方法

1.  复刻本仓库或从[发行版](https://github.com/Aionfatedio/AokanaUnpacker/releases/tag/v0.0.1)下载并运行 `SpriteUnpacker.exe`。
2.  将游戏目录下的资源文件（如 `sprites.dat`, `bgm.dat`, `voice.dat`）拖入控制台窗口，程序将自动在 `.dat` 指定的目录下创建解包文件夹。

## 已支持的游戏

| 游戏名称 | 状态 | 备注 |
| :--- | :--- | :--- |
| **苍之彼方的四重奏 (Aokana)** | ✅ 支持 | 使用 Legacy 密钥配置 |
| **苍之彼方的四重奏 (Aokana) EXTRA1** | ✅ 支持 | 使用 Legacy 密钥配置 |
| **苍之彼方的四重奏 (Aokana) EXTRA2** | ✅ 支持 | 使用 Extra2 密钥配置 |
| **Sprite社其他新作** | 🛠 可扩展 | 需手动提取参数 (见下文) |

## 可扩展性 

> [!NOTE]
> 本工具仅适用于 Sprite 在 Steam 上发布的游戏，DL等版本请使用`GARbro`

### 如何适配新游戏？

核心解密逻辑位于 `PRead.cs` 类中。我们需要修改 `KeySet` 结构体配置。

#### 1\. 获取密钥参数

使用 **dnSpy** 反编译游戏目录下的 `Assembly-CSharp.dll`，搜索 `PRead` 类。关注 `gk` 和 `dd` 方法，并提取以下 11 个参数：

| 参数名 | 说明 | 提取位置 | 代码特征 |
| :--- | :--- | :--- | :--- |
| **gk\_mul** | 乘法因子 | `gk()` | `k0 * [数值] + ...` |
| **gk\_add** | 加法因子 | `gk()` | `... + [数值]` |
| **gk\_shift\_amount** | 左移位数 | `gk()` | `num << [数值]` (通常为 7 或 17) |
| **gk\_mag1** | 混淆参数1 | `gk()` | `num + [数值]` |
| **gk\_mag2** | 混淆参数2 | `gk()` | `num2 & [数值]` |
| **gk\_right\_shift** | 右移位数 | `gk()` | `num >>= [数值]` (通常为 1, 2 或 3) |
| **dd\_mod1** | 解密模数1 | `dd()` | `array[i % [数值]]` (第一个) |
| **dd\_add** | 解密加数 | `dd()` | `b2 += [数值]` |
| **dd\_mod2** | 解密模数2 | `dd()` | `array[i % [数值]]` (第二个) |
| **dd\_xor** | 异或因子 | `dd()` | `b2 ^= [数值]` |
| **loop\_start** | 循环起始 | `Init()` | `for (int i = [3或4]; i < 255; ...)` |

#### 2\. 添加配置代码

在 `PRead.cs` 中，找到 `KeySet` 定义区域，添加一个新的静态配置：

```csharp
// 示例：添加一个新游戏配置
private static readonly KeySet Keys_NewGame = new KeySet
{
    gk_mul = 5892,       // 填入你提取的值
    gk_add = 41280,
    gk_shift_amount = 7, 
    gk_mag1 = 341, 
    gk_mag2 = 220,
    gk_right_shift = 2,  // 注意检查新游戏的位移逻辑
    dd_mod1 = 235, 
    dd_add = 31, 
    dd_mod2 = 87, 
    dd_xor = 165,
    loop_start_index = 3
};
```

#### 3\. 注册识别逻辑

在构造函数 `public PRead(string fn)` 中，添加对新配置的尝试：

```csharp
// 现有逻辑
if (!TryInit(Keys_Legacy))
{
    if (!TryInit(Keys_Extra2))
    {
        // 尝试新游戏的密钥
        if (!TryInit(Keys_NewGame)) 
        {
            throw new Exception("FATAL: 无法识别文件加密格式");
        }
        Console.WriteLine("识别为 新加密格式");
    }
}
```
