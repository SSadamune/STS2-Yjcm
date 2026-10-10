# 一将成名双向联动接口 v1

通用 BCL 桥接保持 v1；新增面向孙乾宇宙的火攻/装备栏强类型玩法接口及反向可选代理，见 [孙乾宇宙接入](孙乾宇宙接入.md)。不自动改变牌、事件、遗物效果，不处理无关 Mod 的兼容补丁。
教程依据：`xiazai/SlayTheSpire2ModdingTutorials-master/SlayTheSpire2ModdingTutorials-master/RitsuLib/03 - 模组工具/10 - 模组联动/README.md`。

## 定位与双向流程

- Mod ID：`Yjcm`；实际内容程序集：`Yjcm`。加载器 `Yjcm.Loader` 不是接口所在程序集。
- 公开类型：`Yjcm.Api.PublicApi`，API 版本 `1`（与 Mod 发布版本 `1.2.0` 无关）。
- 按教程推荐，对方使用 `[AssemblyInterop("Yjcm.Api.PublicApi, Yjcm")]`，无需引用我们的 DLL。
- 对方 → 我们：调用 `InvokeAsync(operation, payloadJson)` 或只读查询方法。
- 我们 → 对方：对方通过 `RegisterPartner` 注册能力列表和接收回调，我们调用 `CallPartnerAsync`。
- 未安装对方 Mod 时没有回调注册，返回 `partner_missing`，本 Mod 不受影响。未安装一将成名时，示例代理保留安全默认值。
- 导出包和游戏 `mods/yjcm` 包附带本说明与代理示例，但游戏只加载 DLL，不运行示例源码。

## 当前公开方法

| 方法/属性 | 用途 |
| --- | --- |
| `ApiVersion`、`IsReady`、`GetModId()` | 版本与就绪检查 |
| `GetCharacterIds()` | 一将成名角色 ID 快照，完整包返回武诸葛亮及威董卓 |
| `GetCapabilities()` | 当前可调用操作快照 |
| `InvokeAsync(string operation, string payloadJson)` | 对方调用我们，返回 `Task<string>` |
| `RegisterPartner(string modId, int apiVersion, string[] capabilities, Func<string,string,Task<string>> receiver)` | 注册反向入口，返回是否成功 |
| `CallPartnerAsync(string modId, string operation, string payloadJson)` | 我们调用对方，返回 `Task<string>` |
| `GetPartnerIds()`、`HasPartner(string modId)`、`GetPartnerCapabilities(string modId)` | 查询已接入的 Mod 与能力 |
| `UnregisterPartner(string modId)` | 释放伙伴回调；已开始的调用不会被撤回 |

初始内置操作只有两个，均为只读：`api.info` 返回 Mod ID、协议版本、能力列表与角色 ID；`content.characters` 返回角色 ID 数组。请求负载可用 `{}`。
未来具体联动由我们添加显式能力处理函数（内部 `RegisterLocalEndpoint`），不会把所有游戏方法自动开放给其他 Mod。

## 返回格式及错误

两方向的调用均返回 JSON：

```json
{"ok":true,"error":null,"data":{"example":123}}
```

失败时为 `{"ok":false,"error":"partner_missing","data":null}`。
错误码包括：`not_ready`、`invalid_operation`、`unsupported_operation`、`partner_missing`、`invalid_payload`、`invalid_response`、`handler_failed`、`cancelled`、`recursive_call`。

接收回调应返回**原始 JSON 数据**，比如 `{"level":5}`，不要重复包裹 `ok/error/data`。桥接层负责包裹。处理函数内部若继续调用接口，需要先检查内层返回的 `ok`，不能把内层错误当作成功数据。
异常会转成错误响应，不向游戏结算直接抛出。协议版本必须精确匹配 `1`；重复注册不覆盖现有回调，更新时先注销再注册。
ID 与能力名区分大小写，只接受 ASCII 字母、数字及 `_.-/:`，长度 1–128；伙伴不能冒用自身 `Yjcm` ID。能力最多 128 项，请求和响应最多 65,536 个 UTF-16 字符，并必须是合法 JSON。
能力列表在注册时复制，各查询结果都是快照；调用伙伴时不会持有注册锁。相同路径递归和超过 16 层嵌套会被拒绝。

## 对方接入示例

复制 `examples/YjcmInterop.cs` 到对方工程。对方仍需按照教程调用：

```csharp
ModTypeDiscoveryHub.RegisterModAssembly(YourModId, Assembly.GetExecutingAssembly());
```

双方完成初始化、RitsuLib 已绑定代理后，在合适的生命周期调用以下连接方法；建议首次主菜单就绪时调用，未就绪时跳过或在下次安全生命周期重试，勿后台无限轮询。

```csharp
if (YjcmInterop.IsReady && YjcmInterop.ApiVersion == 1)
{
    bool connected = YjcmInterop.RegisterPartner(
        "YourModId", 1, ["your-mod.info"],
        (operation, payloadJson) => Task.FromResult("{\"displayName\":\"联动对象\"}"));

    // 正向：对方调用一将成名。
    string response = await YjcmInterop.InvokeAsync("api.info", "{}");
    // 后续一将成名的联动适配器即可反向调用：
    // await Yjcm.Api.PublicApi.CallPartnerAsync("YourModId", "your-mod.info", "{}");
}
```

`RegisterPartner` 本身可在 API 未就绪时注册，但不能假设 RitsuLib 在对方初始化期间已完成代理绑定。未绑定时示例方法返回默认失败，不会假装成功。伙伴的注销与再次注册应放在其自己的安全重载生命周期中。

## 游戏与联机边界

这是同一游戏进程内的接口，不是网络同步。不会自动传输联机消息、生成随机数、排队游戏动作或保存伙伴状态。
目前内置接口不修改玩法。后续涉及生命、力量、卡牌、奖励等变动时，必须另行定义操作，并在原生同步游戏动作/`PlayerChoiceContext` 内执行；不能在一个客户端调用后就认为所有客户端均已生效。
异步回调延续调用者的上下文，不使用 `Task.Run`。游戏操作由游戏主线程发起；禁止后台线程直接改游戏状态。不要使用 `.Wait()` 或 `.Result` 同步等待游戏线程。
接收者须及时完成自己的异步操作。本接口不强行超时取消或回滚已开始的操作，避免出现“调用方认为失败但异步效果仍继续修改游戏”的问题。

## 验证范围

`validation/check_interop.ps1` 在两个 API 变体分别加载实际 DLL，通过一个没有引用 Yjcm DLL 的测试程序集模拟伙伴，检查正向/反向调用、版本拒绝、未安装、异常、非法 JSON、注册快照、重复注册、注销与递归保护。代理示例也在两套 RitsuLib 下编译。
这些测试不等于已与某个真实伙伴 Mod 完成游戏内或联机测试；具体伙伴确定后还需要共同验证。
