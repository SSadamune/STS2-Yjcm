using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>
/// 先古之民「武张飞」：英灵殿守门人。
/// 张飞英魂驻守门扉：武诸葛亮来访时有三段叙旧对话（首次/再访/三访），
/// 其他角色访问则道出豪言（通用台词每次不同）。
/// 奖励：残魂系列遗物三选一（爻袁术残魂 / 任婉残魂 / 荀勖残魂）。
/// 背景场景：Yjcm/scenes/wu_zhang_fei_ancient.tscn（全屏武张飞立绘背景）。
/// 本地化 ID：YJCM_EVENT_WU_ZHANG_FEI（ancients.json）。
/// 生成章节：所有第三幕地图。
/// </summary>
[RegisterSharedAncient] // 通用注册后按幕序号和玩家设置筛选
public sealed class WuZhangFei : ModAncientEventTemplate
{
    /// <summary>仅在所有第三幕地图且当前队伍符合玩家设置时进入先古候选池。</summary>
    public override bool IsValidForAct(ActModel act)
    {
        if (act.Index != 2)
            return false;

        string scope = WuZhugeliangSettings.WuZhangFeiEncounterScope;
        if (scope == WuZhugeliangSettings.AllCharacters)
            return true;
        if (scope == WuZhugeliangSettings.NoCharacters)
            return false;

        RunState? state = RunManager.Instance.DebugOnlyGetState();
        if (state is null)
            return false;

        return scope switch
        {
            WuZhugeliangSettings.OwnModCharacters => state.Players.Any(player =>
                player.Character.GetType().Assembly == typeof(WuZhugeliang).Assembly),
            WuZhugeliangSettings.ModCharacters => state.Players.Any(player =>
                player.Character.GetType().Assembly != typeof(CharacterModel).Assembly),
            _ => true
        };
    }

    /// <summary>选项按钮颜色（张飞主题：暗红偏紫）</summary>
    public override Color ButtonColor => new(0.45f, 0.08f, 0.08f, 0.55f);

    /// <summary>对话框颜色（与按钮呼应）</summary>
    public override Color DialogueColor => new(0.25f, 0.06f, 0.06f);

    /// <summary>自定义背景场景：武张飞全屏立绘背景</summary>
    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://Yjcm/scenes/wu_zhang_fei_ancient.tscn"
    );

    /// <summary>地图节点与历史图标（武张飞头像）</summary>
    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://Yjcm/Images/ancients/wu_zhang_fei_icon.png",
        MapIconOutlinePath: "res://Yjcm/Images/ancients/wu_zhang_fei_icon.png",
        RunHistoryIconPath: "res://Yjcm/Images/ancients/wu_zhang_fei_icon.png",
        RunHistoryIconOutlinePath: "res://Yjcm/Images/ancients/wu_zhang_fei_icon.png"
    );

    /// <summary>奖励池一：爻袁术残魂</summary>
    private IReadOnlyList<EventOption> Pool1 => [CreateModRelicOption<YaoYuanShuCanHun>()];

    /// <summary>奖励池二：任婉残魂</summary>
    private IReadOnlyList<EventOption> Pool2 => [CreateModRelicOption<RenWanCanHun>()];

    /// <summary>奖励池三：荀勖残魂</summary>
    private IReadOnlyList<EventOption> Pool3 => [CreateModRelicOption<XunXuCanHun>()];

    /// <summary>奖励池四：谋陆逊残魂（1.0.13beta 加入）</summary>
    private IReadOnlyList<EventOption> Pool4 => [CreateModRelicOption<MouLuXunCanHun>()];

    /// <summary>奖励池五：原版白银狮子</summary>
    private IReadOnlyList<EventOption> Pool5 => [CreateModRelicOption<YuanBanBaiYinShiZi>()];

    /// <summary>奖励池六：高达一号残魂</summary>
    private IReadOnlyList<EventOption> Pool6 => [CreateModRelicOption<GaoDaYiHaoCanHun>()];

    /// <summary>所有残魂奖励（6 件）</summary>
    public override IEnumerable<EventOption> AllPossibleOptions =>
        [.. Pool1, .. Pool2, .. Pool3, .. Pool4, .. Pool5, .. Pool6];

    /// <summary>
    /// 生成初始选项：4 件残魂随机抽 3 件，玩家三选一（与其他先古之民一致）。
    /// 用 Ancient 的 Rng 逐个抽取并从池中移除，保证不重复。
    /// </summary>
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var pool = AllPossibleOptions.ToList();
        var result = new List<EventOption>(3);
        while (pool.Count > 0 && result.Count < 3)
        {
            var pick = Rng.NextItem(pool);
            pool.Remove(pick);
            result.Add(pick);
        }
        return result;
    }
}
