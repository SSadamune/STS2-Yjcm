using STS2RitsuLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using Yjcm.Extensions;

#nullable enable

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// �����-��񷣺3��ϡ�й����ơ����{DMG}���˺�����δ����ȫ����ɻ���Ŀ��һ����λ
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class ShenHuangZhongLieCang : WuZhugeliangCard
{
    private const string DmgKey = "Dmg";

    public ShenHuangZhongLieCang()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.ShenJiang];

    // 为描述中的 [gold]击伤[/gold] 提供悬浮提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(YjcmKeywords.JiShang)];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ShenHuangZhongLieCang).Assembly.Location)!,
        "Voice", "Bank", "Assets", "lie_cang.wav");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(DmgKey, 20m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var target = cardPlay.Target!;
        var dmg = DynamicVars[DmgKey].BaseValue;

        // 造成伤害
#if GAME_BETA
        var attack = await DamageCmd.Attack(dmg).FromCard(this, cardPlay).Targeting(target).Execute(choiceContext);
#else
        var attack = await DamageCmd.Attack(dmg).FromCard(this).Targeting(target).Execute(choiceContext);
#endif
        var results = attack.Results.SelectMany(hit => hit).ToList();
        if (!results.Any(r => r.UnblockedDamage > 0))
            return; // 被完全格挡则不触发击伤

        // ����ѡ��
        var options = new List<CardModel>
        {
            Owner.Creature.CombatState.CreateCard<LieCangOption1>(Owner),
            Owner.Creature.CombatState.CreateCard<LieCangOption2>(Owner),
            Owner.Creature.CombatState.CreateCard<LieCangOption3>(Owner),
            Owner.Creature.CombatState.CreateCard<LieCangOption4>(Owner),
        };

        // ���Ŀ���ѱ������˹���׷�����
        if (target.Powers?.OfType<LieCangMarkerPower>().Any() == true)
            options.Add(Owner.Creature.CombatState.CreateCard<LieCangOption5>(Owner));

        var chosenCard = await CardSelectCmd.FromSimpleGrid(
            choiceContext, options, Owner,
            new CardSelectorPrefs(new("cards", "liecang.select_debuff"), 1, 1));

        var chosen = chosenCard.FirstOrDefault();

        if (chosen == null) return;

        // ������Ч��ʩ�Ӷ�ӦЧ��
        if (chosen is LieCangOption1)
        {
            VoicePlayer.PlayWav(LieCangAudio.LiFeng);
            await PowerCmd.Apply<LieCangLiFengPower>(choiceContext, target, 1, Owner.Creature, this, false);
        }
        else if (chosen is LieCangOption2)
        {
            VoicePlayer.PlayWav(LieCangAudio.DiJi);
            await PowerCmd.Apply<LieCangDiJiPower>(choiceContext, target, 1, Owner.Creature, this, false);
        }
        else if (chosen is LieCangOption3)
        {
            VoicePlayer.PlayWav(LieCangAudio.ZhongShu);
            // 移除目标所有力量（普通力量 + 临时力量）
            var strengthPowers = target.Powers?.Where(p => p is StrengthPower or TemporaryStrengthPower).ToList() ?? [];
            foreach (var sp in strengthPowers)
                await PowerCmd.Remove(sp);
        }
        else if (chosen is LieCangOption4)
        {
            VoicePlayer.PlayWav(LieCangAudio.QiHai);
            await PowerCmd.Apply<LieCangQiHaiPower>(choiceContext, target, 1, Owner.Creature, this, false);
        }
        else if (chosen is LieCangOption5)
        {
            VoicePlayer.PlayWav(LieCangAudio.TianChong);
            await CreatureCmd.Kill(target, force: true);
            await CreatureCmd.GainMaxHp(Owner.Creature, 3);
            await CreatureCmd.Heal(Owner.Creature, 3);
        }

        // ���ϱ��
        await PowerCmd.Apply<LieCangMarkerPower>(choiceContext, target, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DmgKey].UpgradeValueBy(10); // 20��30
    }
}

// ������ ��Ƶ·�� ������

internal static class LieCangAudio
{
    private static readonly string Base = Path.Combine(
        Path.GetDirectoryName(typeof(ShenHuangZhongLieCang).Assembly.Location)!,
        "Voice", "Bank", "Assets");

    public static readonly string LiFeng = Path.Combine(Base, "li_feng.wav");
    public static readonly string DiJi = Path.Combine(Base, "di_ji.wav");
    public static readonly string ZhongShu = Path.Combine(Base, "zhong_shu.wav");
    public static readonly string QiHai = Path.Combine(Base, "qi_hai.wav");
    public static readonly string TianChong = Path.Combine(Base, "tian_chong.wav");
}

// ������ ����� Power ������

public sealed class LieCangMarkerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.None;

    public override string CustomIconPath => "res://Yjcm/Images/Powers/tian_chong.png";
    public override string CustomBigIconPath => "res://Yjcm/Images/Powers/Big/tian_chong.png";

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Enemy)
            await PowerCmd.Decrement(this);
    }
}

// ������ ѡ� ������

[RegisterCard(typeof(TokenCardPool))]
public sealed class LieCangOption1 : CardModel
{
    public override string PortraitPath => "shen_huang_zhong_lie_cang.png".CardImagePath();
    public LieCangOption1() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class LieCangOption2 : CardModel
{
    public override string PortraitPath => "shen_huang_zhong_lie_cang.png".CardImagePath();
    public LieCangOption2() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class LieCangOption3 : CardModel
{
    public override string PortraitPath => "shen_huang_zhong_lie_cang.png".CardImagePath();
    public LieCangOption3() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class LieCangOption4 : CardModel
{
    public override string PortraitPath => "shen_huang_zhong_lie_cang.png".CardImagePath();
    public LieCangOption4() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(TokenCardPool))]
public sealed class LieCangOption5 : CardModel
{
    public override string PortraitPath => "shen_huang_zhong_lie_cang.png".CardImagePath();
    public LieCangOption5() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
    protected override Task OnPlay(PlayerChoiceContext _, CardPlay __) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}
