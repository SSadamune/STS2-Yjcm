using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;

namespace Yjcm;

/// <summary>Opt-in asset/model smoke test only. Never creates a run or edits a deck/save.</summary>
[HarmonyPatch(typeof(NGame), "LaunchMainMenu")]
internal static class WeiDongZhuoPreflight
{
    private static void Postfix(NGame __instance, ref Task __result)
    {
        if (OS.GetCmdlineArgs().Contains("--weidongzhuo-preflight"))
            __result = Check(__result, __instance);
    }

    private static async Task Check(Task startup, NGame game)
    {
        await startup;
        await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        Control? background = null;
        Node2D? creature = null;
        try
        {
            var character = ModelDb.Character<WeiDongZhuo>();
            if (character.Title.GetFormattedText() != "威董卓") throw new InvalidOperationException("Character localization failed");
            if (character.StartingHp != 80 || character.StartingGold != 99) throw new InvalidOperationException("Character stats changed");
            if (WeiDongZhuoMaxHpCapPatch.Cap != 350234) throw new InvalidOperationException("WeiDongZhuo max HP cap changed");
            var starter = character.StartingDeck.ToList();
            if (starter.Count != 10 || starter.Count(c => c is WeiDongZhuoSha) != 4 ||
                starter.Count(c => c is WeiDongZhuoShan) != 4 ||
                starter.Count(c => c is WeiDongZhuoJuChui) != 1 ||
                starter.Count(c => c is WeiDongZhuoHaoShou) != 1 ||
                character.StartingRelics.Single() is not WeiDongZhuoGuangYong)
                throw new InvalidOperationException("Starter deck / GuangYong missing");
            var relic = ModelDb.Relic<WeiDongZhuoGuangYong>();
            var upgraded = ModelDb.Relic<WeiDongZhuoJieGuangYong>();
            if (relic.Title.GetFormattedText() != "犷勇" || upgraded.Title.GetFormattedText() != "界犷勇")
                throw new InvalidOperationException("GuangYong localization missing");
            if (ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(relic) != upgraded)
                throw new InvalidOperationException("Ancient upgrade does not resolve to JieGuangYong");
            if (GD.Load<Texture2D>(relic.PackedIconPath).GetWidth() != 256)
                throw new InvalidOperationException("GuangYong transparent icon missing");
            if (relic.HoverTipsExcludingRelic.Any()) throw new InvalidOperationException("GuangYong should not expose buff hover tips");
            MainFile.Logger.Info("GUANGYONG_PREFLIGHT_OK starter+ancient_upgrade / names / 256px icon / compact hover tips");
            if (character.IconTexture.GetWidth() <= 0 || character.CharacterSelectIcon.GetWidth() <= 0)
                throw new InvalidOperationException("Character icons missing");
            var cards = character.CardPool.AllCards.ToList();
            if (cards.Count == 0 || cards.Any(c => !c.GetType().Name.StartsWith("WeiDongZhuo", StringComparison.Ordinal)))
                throw new InvalidOperationException("Pool contains original rather than independent cards");
            if (character.CardPool.FrameMaterial is not ShaderMaterial frame || frame.Shader == null)
                throw new InvalidOperationException("Golden card frame did not load");
            foreach (var card in cards)
            {
                if (!card.Id.Entry.Contains("WEI_DONG_ZHUO", StringComparison.Ordinal))
                    throw new InvalidOperationException("Shared model ID: " + card.Id.Entry);
                if (card.Title.Contains("WEI_DONG_ZHUO", StringComparison.Ordinal))
                    throw new InvalidOperationException("Missing clone title: " + card.Id.Entry);
                if (card.Portrait == null) throw new InvalidOperationException("Missing portrait: " + card.Id.Entry);
            }
            foreach (var card in new CardModel[]
                     { ModelDb.Card<WeiDongZhuoJuChui>(), ModelDb.Card<WeiDongZhuoJieJuChui>(), ModelDb.Card<WeiDongZhuoHaoShou>() })
            {
                if (card.Title.Contains("WEI_DONG_ZHUO", StringComparison.Ordinal) || card.Portrait == null)
                    throw new InvalidOperationException("New card resource/localization failed: " + card.Id.Entry);
            }
            var juChui = ModelDb.Card<WeiDongZhuoJuChui>();
            var jieJuChui = ModelDb.Card<WeiDongZhuoJieJuChui>();
            if (juChui.TargetType != WeiDongZhuoTargetTypes.SelfOrEnemy || !juChui.GainsBlock ||
                juChui.DynamicVars.Block.BaseValue != 6 || juChui.DynamicVars["ComboHeal"].BaseValue != 4 ||
                juChui.DynamicVars["ComboLoss"].BaseValue != 8 ||
                jieJuChui.TargetType != WeiDongZhuoTargetTypes.SelfOrEnemy || !jieJuChui.GainsBlock ||
                jieJuChui.DynamicVars.Block.BaseValue != 12 || jieJuChui.DynamicVars["ComboHeal"].BaseValue != 9 ||
                jieJuChui.DynamicVars["ComboLoss"].BaseValue != 15)
                throw new InvalidOperationException("JuChui self/enemy branch values are incorrect");
            if (ModelDb.Card<WeiDongZhuoMouXuYouMoYou>().Rarity != CardRarity.Uncommon)
                throw new InvalidOperationException("MouXuYou rarity must be Uncommon");
            var lianJie = ModelDb.Card<WeiDongZhuoWuHuangFuSongLianJie>();
            var jiangXian = ModelDb.Card<WeiDongZhuoWuHuangFuSongJiangXian>();
            if (lianJie.Rarity != CardRarity.Rare || jiangXian.Rarity != CardRarity.Rare ||
                !lianJie.Tags.Contains(YjcmCardTags.WuJiang) || !lianJie.Tags.Contains(YjcmCardTags.Qun) ||
                !jiangXian.Tags.Contains(YjcmCardTags.WuJiang) || !jiangXian.Tags.Contains(YjcmCardTags.Qun) ||
                lianJie.Portrait == null || jiangXian.Portrait == null)
                throw new InvalidOperationException("HuangFuSong cards failed registration/tags/assets");
            if (ModelDb.Card<WeiDongZhuoMouJiaXuSuShen>().Rarity != CardRarity.Uncommon ||
                ModelDb.Card<WeiDongZhuoShenZhaoYunLongHun>().Rarity != CardRarity.Uncommon)
                throw new InvalidOperationException("MouJiaXu/ShenZhaoYun rarity must be Uncommon");
            var transformTip = HoverTipFactory.FromKeyword(YjcmKeywords.ZhuanHuanJi);
            var fuMouYang = ModelDb.Card<WeiDongZhuoMouJiaXuFuMouYang>();
            var fuMouYin = ModelDb.Card<WeiDongZhuoMouJiaXuFuMouYin>();
            if (!fuMouYang.HoverTips.Any(t => t.Equals(transformTip)) ||
                !fuMouYin.HoverTips.Any(t => t.Equals(transformTip)) ||
                !fuMouYang.HoverTips.Any(t => t.Equals(HoverTipFactory.FromCard<WeiDongZhuoMouJiaXuFuMouYin>(false))) ||
                !fuMouYin.HoverTips.Any(t => t.Equals(HoverTipFactory.FromCard<WeiDongZhuoMouJiaXuFuMouYang>(false))))
                throw new InvalidOperationException("FuMou transformation keyword/card preview missing");
            var retainTip = HoverTipFactory.FromKeyword(CardKeyword.Retain);
            if (!ModelDb.Card<WeiDongZhuoMouJiaXuSuShen>().HoverTips.Any(t => t.Equals(retainTip)) ||
                !ModelDb.Card<WeiDongZhuoMouJiaXuRuShi>().HoverTips.Any(t => t.Equals(retainTip)))
                throw new InvalidOperationException("SuShen/RuShi Retain keyword missing");
            var wuSeShaTip = HoverTipFactory.FromCard<WeiDongZhuoWuSeSha>(false);
            var duelTip = HoverTipFactory.FromCard<WeiDongZhuoJueDou>(false);
            if (!ModelDb.Card<WeiDongZhuoWangYueJianDao>().HoverTips.Any(t => t.Equals(wuSeShaTip)) ||
                !ModelDb.Card<WeiDongZhuoMaTieQuXian>().HoverTips.Any(t => t.Equals(wuSeShaTip)) ||
                !ModelDb.Card<WeiDongZhuoWangYueShiFeng>().HoverTips.Any(t => t.Equals(duelTip)))
                throw new InvalidOperationException("MaTie/WangYue derived-card hover tip missing");
            var liSao = ModelDb.Card<WeiDongZhuoQuYuanLiSao>();
            if (liSao.Title != "屈原-离骚" || liSao.Rarity != CardRarity.Ancient || liSao.Portrait == null)
                throw new InvalidOperationException("LiSao event card resource/localization failed");
            if (liSao.Pool is not TokenCardPool || liSao.Rarity != CardRarity.Ancient ||
                liSao.ShouldShowInCardLibrary || liSao.CanBeGeneratedInCombat)
                throw new InvalidOperationException("LiSao must remain a universal event-only Ancient card");
            if (LiSaoQuestionBank.Count < 180)
                throw new InvalidOperationException("LiSao full-text question bank incomplete");
            var huoShaoLuoYang = ModelDb.Card<WeiDongZhuoDongZhuoHuoShaoLuoYang>();
            if (huoShaoLuoYang.Pool is not WeiDongZhuoCardPool ||
                huoShaoLuoYang.Rarity != CardRarity.Ancient ||
                huoShaoLuoYang.TargetType != TargetType.AllEnemies ||
                !huoShaoLuoYang.Tags.Contains(YjcmCardTags.WuJiang) ||
                !huoShaoLuoYang.Tags.Contains(YjcmCardTags.Qun) ||
                huoShaoLuoYang.Portrait == null ||
                HuoShaoLuoYangRules.MaxHpLoss(83, 20) != 16 ||
                HuoShaoLuoYangRules.MaxHpLoss(83, 10) != 8 ||
                HuoShaoLuoYangRules.DamagePerExhaustedCard(8, true) != 16m)
                throw new InvalidOperationException("HuoShaoLuoYang pool/tags/assets/rules failed");
            if (ModelDb.Power<LiSaoPower>().Title.GetFormattedText() != "离骚" ||
                ModelDb.Power<JieLiSaoPower>().Title.GetFormattedText() != "离骚")
                throw new InvalidOperationException("LiSao power registration/localization failed");
            if (!WeiDongZhuoComboVfx.Preflight())
                throw new InvalidOperationException("Combo Spine VFX preflight failed");
            string voiceRoot = Path.Combine(Path.GetDirectoryName(typeof(WeiDongZhuo).Assembly.Location)!, "Voice", "WeiDongZhuo");
            string[] voices =
            [
                "ba_guan.wav", "ba_guan_combo.wav", "bao_nue_xing_tai.wav", "character_select.wav", "dou_wei.wav", "duan_ti.wav",
                "fu_mou_yang.wav", "fu_mou_yin.wav", "fu_xi.wav", "guang_yong_1.wav", "guang_yong_2.wav", "huo_shao_luo_yang.wav",
                "hao_shou.wav", "ji_zhan.wav", "ji_zhan_combo.wav", "jian_dao.wav", "jiao_wang.wav",
                "jie_guang_yong_1.wav", "jie_guang_yong_2.wav", "jie_ju_chui_normal.wav", "jie_ju_chui_combo.wav",
                "jiang_xian.wav", "ju_chui_normal.wav", "ju_chui_combo.wav", "li_sao_punish.wav", "lian_jie.wav", "mo_shou.wav", "mo_you.wav", "nei_fa.wav",
                "po_rong.wav", "po_rong_combo.wav", "qu_xian.wav", "qu_yuan_li_sao.wav", "ru_shi.wav", "sha_jue.wav",
                "shi_feng.wav", "su_shen.wav", "wei_lin.wav", "xian_shuai.wav", "xiao_xi.wav", "xiao_yan.wav",
                "shu_guo.wav", "xiong_huo.wav", "xiong_suan.wav", "yu_xi.wav", "zhui_tao.wav", "zhui_tao_combo.wav",
                "zong_shi.wav", "zuo_jun.wav"
            ];
            if (voices.Any(file => !File.Exists(Path.Combine(voiceRoot, file))))
                throw new InvalidOperationException("WeiDongZhuo voice bundle incomplete");

            string[] powerIcons =
            [
                "bao_nue_xing_tai_buff.png", "duan_ti_buff.png", "jiang_xian_buff.png", "lian_jie_buff.png", "qu_xian_buff.png", "mo_you_buff.png", "mo_shou_buff.png",
                "wang_yue_buff.png", "xiong_huo_buff.png", "yu_xi_buff.png", "zhui_tao_buff.png"
            ];
            foreach (var icon in powerIcons)
            {
                var texture = GD.Load<Texture2D>($"res://WeiDongZhuo/images/{icon}");
                if (texture.GetWidth() != 256 || texture.GetHeight() != 256)
                    throw new InvalidOperationException("WeiDongZhuo power icon invalid: " + icon);
            }

            var exhaustTip = HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            var etherealTip = HoverTipFactory.FromKeyword(CardKeyword.Ethereal);
            if (!ModelDb.Card<WeiDongZhuoHaoShou>().HoverTips.Any(t => t.Equals(exhaustTip)))
                throw new InvalidOperationException("HaoShou Exhaust keyword missing");
            foreach (var leBu in new[] { ModelDb.Card<LeBuSiShu>(), cards.Single(c => c.Id.Entry == "YJCM_CARD_WEI_DONG_ZHUO_LE_BU_SI_SHU") })
            {
                if (leBu.HoverTips.Any(t => t.Equals(exhaustTip)) || !leBu.HoverTips.Any(t => t.Equals(etherealTip)))
                    throw new InvalidOperationException("LeBu hover filtering failed: " + leBu.Id.Entry);
            }
            if (!ModelDb.Card<Dazed>().HoverTips.Any(t => t.Equals(exhaustTip)))
                throw new InvalidOperationException("Other Ethereal cards were affected");
            MainFile.Logger.Info("LEBU_HOVER_PREFLIGHT_OK original+clone / Ethereal retained / other cards unchanged");

            background = GD.Load<PackedScene>(character.CustomCharacterSelectBgPath).Instantiate<Control>();
            background.Size = new Vector2(1920, 1080);
            background.Visible = false;
            game.GetTree().Root.AddChild(background);
            creature = character.CreateVisuals();
            creature.Visible = false;
            game.GetTree().Root.AddChild(creature);
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (background.FindChild("CharacterSpine", true, false) == null || background.FindChild("BackgroundSpine", true, false) == null)
                throw new InvalidOperationException("Character-select Spine layers missing");
            if (creature.FindChild("WeiDongZhuoSpine", true, false) == null)
                throw new InvalidOperationException("Character-only combat Spine missing");
            MainFile.Logger.Info($"WEIDONGZHUO_PREFLIGHT_OK cards={cards.Count}, starter=4Sha+4Shan+JuChui+HaoShou, voices={voices.Length}, power_icons={powerIcons.Length}, lisao_questions={LiSaoQuestionBank.Count}, combo_spine=OK, combat_spine=OK");
            if (OS.GetCmdlineArgs().Contains("--guangyong-mechanics-preflight"))
                await WeiDongZhuoGuangYongPreflight.Check();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"WEIDONGZHUO_PREFLIGHT_FAILED: {ex}");
        }
        finally
        {
            background?.QueueFree();
            creature?.QueueFree();
        }
    }
}
