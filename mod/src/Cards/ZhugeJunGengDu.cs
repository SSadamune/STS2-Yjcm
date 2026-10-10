using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using STS2RitsuLib.Interop.AutoRegistration;

namespace Yjcm;

/// <summary>
/// ����-������2�Ѽ����ơ���4�ţ�ѡһ�����ͱ������������������������Ч��
/// </summary>
[RegisterCard(typeof(WuZhugeliangCardPool))]
public sealed class ZhugeJunGengDu : WuZhugeliangCard
{
    public ZhugeJunGengDu()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [];

    protected override HashSet<CardTag> CanonicalTags => [YjcmCardTags.WuJiang, YjcmCardTags.Shu];

    private const string DmgKey = "Dmg";
    private const string StatKey = "Stat";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DmgKey, 5m),
        new DynamicVar(StatKey, 1m),
        new BlockVar(0, ValueProp.Move)
    ];

    private static readonly string VoicePath = Path.Combine(
        Path.GetDirectoryName(typeof(ZhugeJunGengDu).Assembly.Location)!,
        "Voice", "Bank", "Assets", "zhuge_jun_geng_du.wav");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        VoicePlayer.PlayWav(VoicePath);

        var handPile = PileTypeExtensions.GetPile(PileType.Hand, Owner);
        var beforeCards = handPile?.Cards?.ToHashSet() ?? [];

        // ��4����
        await CardPileCmd.Draw(choiceContext, 4, Owner);

        var afterCards = handPile?.Cards?.ToHashSet() ?? [];
        var drawnCards = afterCards.Where(c => !beforeCards.Contains(c) && c != this).ToList();
        if (drawnCards.Count == 0) return;

        // ѡ��һ���ƾ�����������
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(new("cards", "gengdu.select_type"), 1, 1),
            card => drawnCards.Contains(card), this);
        var chosen = selected.FirstOrDefault();
        if (chosen == null) return;

        var keepType = chosen.Type;

        // ���÷Ǳ������͵ĳ��������ƣ�����
        var discardCount = 0;
        foreach (var card in drawnCards)
        {
            if (card.Type == keepType) continue;
            await CardCmd.Discard(choiceContext, card);
            discardCount++;
        }

        if (discardCount <= 0) return;

        var dmg = DynamicVars[DmgKey].BaseValue;
        var block = DynamicVars[DmgKey].BaseValue; // �˺��͸���ֵ��ͬ
        var statAmt = DynamicVars[StatKey].BaseValue;

        // ���ݱ������ʹ���Ч��
        var enemies = CombatState.HittableEnemies.ToList();
        for (int i = 0; i < discardCount; i++)
        {
            switch (keepType)
            {
                case CardType.Attack:
                    if (enemies.Count > 0)
                    {
                        var target = Owner.RunState.Rng.CombatCardSelection.NextItem(enemies);
                        if (target != null)
                            await CreatureCmd.Damage(choiceContext, target, dmg,
#if GAME_BETA
                                ValueProp.Move, this, cardPlay);
#else
                                ValueProp.Move, this);
#endif
                    }
                    break;
                case CardType.Skill:
                    DynamicVars.Block.BaseValue = block;
                    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
                    break;
                case CardType.Power:
                    await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, statAmt, Owner.Creature, this, false);
                    await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, statAmt, Owner.Creature, this, false);
                    break;
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DmgKey].UpgradeValueBy(2);
        DynamicVars[StatKey].UpgradeValueBy(1);
    }
}
