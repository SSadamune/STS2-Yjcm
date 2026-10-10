using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Yjcm.Extensions;

#nullable enable

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// �����-ն����2�Ѽ����ƣ�ϡ�С�ѡ�����Ʒ�ʽ����ö�Ӧ����
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class ShenHuangZhongZhanJue : WuZhugeliangCard
{
    public ShenHuangZhongZhanJue()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenHuangZhongZhanJue).Assembly.Location)!,
        "Voice", "Bank", "Assets", "zhan_jue.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var currentHp = (int)Owner.Creature.CurrentHp;
        var maxHp = (int)Owner.Creature.MaxHp;
        var lostHp = maxHp - currentHp;

        var drawOption1 = Math.Max(1, currentHp / 7);
        var drawOption2 = Math.Max(1, lostHp / 7);

        // ��������ѡ�������չʾ��ֱ�� new�����ڽ������У�
        var option1 = Owner.Creature.CombatState.CreateCard<ZhanJueOption1>(Owner);
        var option2 = Owner.Creature.CombatState.CreateCard<ZhanJueOption2>(Owner);

        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, new CardModel[] { option1, option2 }, Owner, canSkip: false);

        if (chosen is ZhanJueOption1)
        {
            await CardPileCmd.Draw(choiceContext, drawOption1, Owner);
            await PowerCmd.Apply<ZhanJueFreeAttackPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else if (chosen is ZhanJueOption2)
        {
            await CardPileCmd.Draw(choiceContext, drawOption2, Owner);
            await PowerCmd.Apply<ZhanJueLifestealPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
        // ���������� 2��1
        EnergyCost.SetThisCombat(1);
    }
}

/// <summary>
/// ն��-ǿ����������ֵ/7���ƣ���һ�Ź�������Ѵ����������ѡ�����չʾ��
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class ZhanJueOption1 : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();

    public override string PortraitPath =>
        "zhan_jue_option1.png".CardImagePath();

    public ZhanJueOption1()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }

    public override bool CanBeGeneratedInCombat => false;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask; // ��ʵ�ʴ����������ѡ��

    protected override void OnUpgrade() { }
}

/// <summary>
/// ն��-��Ѫ��������ʧ����ֵ/7���ƣ���һ�Ź���������˺���ָ�����������������ѡ�����չʾ��
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class ZhanJueOption2 : CardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();

    public override string PortraitPath =>
        "zhan_jue_option2.png".CardImagePath();

    public ZhanJueOption2()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }

    public override bool CanBeGeneratedInCombat => false;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask; // ��ʵ�ʴ����������ѡ��

    protected override void OnUpgrade() { }
}
