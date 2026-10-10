using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// �Ӻ�-�ռ�1�Ѽ����ƣ��佫����=(Ŀ������+���˲���)��2��3
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class PangHongGangJian : WuZhugeliangCard
{
    private const string MultiplierKey = "Multiplier";

    public PangHongGangJian()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(MultiplierKey, 2m),
        new BlockVar(0, ValueProp.Move)
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(PangHongGangJian).Assembly.Location)!,
        "Voice", "Bank", "Assets", "pang_hong_gang_jian.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var target = cardPlay.Target!;
        await PowerCmd.Apply<WeakPower>(choiceContext, target, 2, Owner.Creature, this, false);
        var weakAmt = target.Powers?.OfType<WeakPower>().FirstOrDefault()?.Amount ?? 0;
        var vulnAmt = target.Powers?.OfType<VulnerablePower>().FirstOrDefault()?.Amount ?? 0;
        var multiplier = (int)DynamicVars[MultiplierKey].BaseValue;
        var block = (weakAmt + vulnAmt) * multiplier;

        if (block > 0)
        {
            DynamicVars.Block.BaseValue = block;
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MultiplierKey].UpgradeValueBy(1);
    }
}
