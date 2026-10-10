using Godot;

using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Yjcm;

/// <summary>把谋司马懿加入所有第三幕地图的首领候选池。</summary>
[RegisterGlobalEncounter]
public sealed class MouSimaYiBossEncounter : ModEncounterTemplate
{
    private const string BossNodeBasePath =
        "res://Yjcm/Images/Monsters/MouSimaYi/mou_sima_yi_boss_node_lineart_110";
    private const string PortraitPath =
        "res://Yjcm/Images/Monsters/MouSimaYi/mou_sima_yi_portrait.png";

    public override RoomType RoomType => RoomType.Boss;

    public override string BossNodePath => BossNodeBasePath;

    public override EncounterAssetProfile AssetProfile => new(
        RunHistoryIconPath: PortraitPath,
        RunHistoryIconOutlinePath: PortraitPath);

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<MouSimaYi>()];

    public override float GetCameraScaling() => 0.9f;

    public override Vector2 GetCameraOffset() => Vector2.Down * 50f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<MouSimaYi>().ToMutable(), null)];

    /// <summary>仅在第三幕且队伍符合独立设置时进入首领候选池。</summary>
    public override bool IsValidForAct(ActModel act)
    {
        if (act.Index != 2)
            return false;

        string scope = WuZhugeliangSettings.MouSimaYiEncounterScope;
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
}
