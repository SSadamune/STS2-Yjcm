using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// ı��ƽ-������2�ѹ����� �������� 6��n�˺� ��2��3
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class MouGuanPingWuWei : WuZhugeliangCard
{
    public MouGuanPingWuWei()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    private const string DrawKey = "Draw";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move), new DynamicVar(DrawKey, 2m)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(MouGuanPingWuWei).Assembly.Location)!,
        "Voice", "Bank", "Assets", "mou_guan_ping_wu_wei.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var handPile = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        var handCards = handPile?.Cards?.ToList() ?? [];

        // �����������ƣ�ÿ��һ�����6���˺�
        if (handCards.Any(c => c != this))
        {
            // 每张弃牌的一击属于同一次攻击，所有命中均受活力加成，结束只扣一次。
#if GAME_BETA
            await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, cardPlay);
#else
            await using var attack = await AttackContext.CreateAsync(CombatState, choiceContext, this);
#endif
            foreach (var c in handCards)
            {
                if (c == this) continue;
                await CardCmd.Discard(choiceContext, c);
                attack.AddHit(await CreatureCmd.Damage(choiceContext, cardPlay.Target!,
#if GAME_BETA
                    DynamicVars.Damage.BaseValue, ValueProp.Move, this, cardPlay));
#else
                    DynamicVars.Damage.BaseValue, ValueProp.Move, this));
#endif
            }
        }

        // ����
        var drawCount = (int)DynamicVars[DrawKey].BaseValue;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DrawKey].UpgradeValueBy(1);
    }
}
