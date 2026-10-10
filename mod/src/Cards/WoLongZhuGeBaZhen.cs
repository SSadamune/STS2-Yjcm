using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

using Yjcm.Extensions;

namespace Yjcm;

/// <summary>卧龙诸葛-八阵：根据意图为攻击的怪物数量获得格挡。</summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class WoLongZhuGeBaZhen : WuZhugeliangCard
{
    private const string MultiplierKey = "Multiplier";

    public WoLongZhuGeBaZhen()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags =>
        [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(MultiplierKey, 10m)];

    public override string CustomPortraitPath => "ba_zhen.png".CardImagePath();

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(WoLongZhuGeBaZhen).Assembly.Location)!,
        "Voice", "Bank", "Assets", "ba_zhen.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);
        var attackingEnemies = CombatState.HittableEnemies.Count(
            enemy => enemy.Monster?.NextMove?.Intents.Any(intent => intent is AttackIntent) == true);
        var block = attackingEnemies * DynamicVars[MultiplierKey].BaseValue;
        if (block > 0)
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MultiplierKey].UpgradeValueBy(5);
    }
}
