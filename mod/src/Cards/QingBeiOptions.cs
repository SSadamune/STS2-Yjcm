using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Yjcm.Extensions;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

// 选项卡：攻击
[RegisterCard(typeof(TokenCardPool))]
public sealed class QingBeiAttack : CardModel
{
    public override string PortraitPath => "qing_bei_attack.png".CardImagePath();
    public QingBeiAttack() : base(0, CardType.Attack, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
}

// 选项卡：技能
[RegisterCard(typeof(TokenCardPool))]
public sealed class QingBeiSkill : CardModel
{
    public override string PortraitPath => "qing_bei_skill.png".CardImagePath();
    public QingBeiSkill() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
}

// 选项卡：能力
[RegisterCard(typeof(TokenCardPool))]
public sealed class QingBeiPowerOption : CardModel
{
    public override string PortraitPath => "qing_bei_power_option.png".CardImagePath();
    public QingBeiPowerOption() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
}

// 选项卡：跳过
[RegisterCard(typeof(TokenCardPool))]
public sealed class QingBeiSkip : CardModel
{
    public override string PortraitPath => "qing_bei_skip.png".CardImagePath();
    public QingBeiSkip() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false) { }
    public override bool CanBeGeneratedInCombat => false;
}
