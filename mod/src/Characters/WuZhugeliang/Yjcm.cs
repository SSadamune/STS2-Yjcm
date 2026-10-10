using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

#nullable enable

namespace Yjcm;

[RegisterCharacter]
public sealed class WuZhugeliang : ModCharacterTemplate<WuZhugeliangCardPool, YjcmRelicPool, YjcmPotionPool>
{
    public const string CharacterId = "yjcm";
    public static readonly Color Color = new("4A90D9");

    public override CharacterGender Gender => CharacterGender.Masculine;
    public override Color NameColor => Color;
    public override int StartingHp => 77;
    public override int StartingGold => 77;

    // 特殊充能球栏位：5 个装备栏（武器/防具/防御马/进攻马/宝物），战斗开始自动生成 5 个充能球槽位。
    // 宝物栏同时收容非装备充能球。
    public override int BaseOrbSlotCount => 5;

    public override bool HideFromVanillaCharacterSelect => false;
    public override bool HideInCardLibraryCompendium => false;
    public override bool RequiresEpochAndTimeline => false;

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new(
            Scenes: new(
                VisualsPath: "res://Yjcm/scenes/creature_visuals/WuZhugeliang.tscn",
                // 商店模型：立绘 Sprite2D，缩放对齐战斗立绘，脚部对齐场景原点
                MerchantAnimPath: "res://Yjcm/scenes/merchant/wuzhugeliang_merchant.tscn",
                // 火堆模型：坐姿立绘 Sprite2D 放 ControlRoot 内（联机 X 翻转），缩放对齐战斗立绘
                RestSiteAnimPath: "res://Yjcm/scenes/rest_site/wuzhugeliang_rest_site.tscn"
            ),
            Ui: new(
                IconTexturePath: "res://images/ui/top_panel/character_icon_wuzhugeliang_new.png",
                IconPath: "res://scenes/ui/character_icons/wuzhugeliang_icon.tscn",
                CharacterSelectIconPath: "res://Yjcm/Images/packed/character_select/char_select_wuzhugeliang_bottom.png",
                MapMarkerPath: "res://images/ui/top_panel/character_icon_wuzhugeliang_new.png"
            ),
            // 多人模式手指 + 石头剪刀布手势（宝藏室遗物小游戏用）
            Multiplayer: new(
                ArmPointingTexturePath: "res://Yjcm/Images/ui/hands/wuzhugeliang_hand_point.png",
                ArmRockTexturePath: "res://Yjcm/Images/ui/hands/wuzhugeliang_hand_rock.png",
                ArmPaperTexturePath: "res://Yjcm/Images/ui/hands/wuzhugeliang_hand_paper.png",
                ArmScissorsTexturePath: "res://Yjcm/Images/ui/hands/wuzhugeliang_hand_scissors.png"
            )
        ));

    public override string CustomCharacterSelectBgPath => "res://scenes/screens/char_select/char_select_bg_wuzhugeliang.tscn";

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

    protected override IEnumerable<Type> StartingDeckTypes => new[]
    {
        typeof(ZhiZhe), typeof(LiuBeiJiJiang),
        typeof(Sha), typeof(Sha), typeof(Sha), typeof(Sha),
        typeof(Shan), typeof(Shan), typeof(Shan), typeof(Shan),
    };

    protected override IEnumerable<Type> StartingRelicTypes => new[] { typeof(YjcmQingShi) };

    public override float CastAnimDelay => 0.5f;
    public override float AttackAnimDelay => 0.3f;
    public override List<string> GetArchitectAttackVfx() => new();

    private static string? _voicePath;
    private static string? _mouSkinVoicePath;
    public static string VoicePath => _voicePath ??= Path.Combine(
        Path.GetDirectoryName(typeof(WuZhugeliang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "char_select.wav");

    public static string MouSkinVoicePath => _mouSkinVoicePath ??= Path.Combine(
        Path.GetDirectoryName(typeof(WuZhugeliang).Assembly.Location)!,
        "Voice", "WuZhugeliang", "mou_zhugeliang_select.wav");

    internal static void PlayCurrentSkinSelectVoice() =>
        VoicePlayer.PlayWav(WuZhugeliangSettings.UseMouZhugeliangSkin ? MouSkinVoicePath : VoicePath);

    public override string CharacterSelectSfx
    {
        get
        {
            PlayCurrentSkinSelectVoice();
            return ModelDb.Character<Ironclad>().CharacterSelectSfx;
        }
    }
}
