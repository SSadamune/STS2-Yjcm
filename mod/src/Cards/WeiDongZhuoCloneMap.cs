using MegaCrit.Sts2.Core.Models;

namespace Yjcm;

/// <summary>Explicit type pairs; never infer resource paths or IDs by stripping a prefix.</summary>
public static class WeiDongZhuoCloneMap
{
    private static readonly IReadOnlyDictionary<Type, Type> OriginalsToClones = new Dictionary<Type, Type>
    {
        [typeof(Sha)] = typeof(WeiDongZhuoSha),
        [typeof(Shan)] = typeof(WeiDongZhuoShan),
        [typeof(Jiu)] = typeof(WeiDongZhuoJiu),
        [typeof(Tao)] = typeof(WeiDongZhuoTao),
        [typeof(LeBuSiShu)] = typeof(WeiDongZhuoLeBuSiShu),
        [typeof(WuZhongShengYou)] = typeof(WeiDongZhuoWuZhongShengYou),
        [typeof(HuoGong)] = typeof(WeiDongZhuoHuoGong),
        [typeof(JueDou)] = typeof(WeiDongZhuoJueDou),
        [typeof(TieSuoLianHuan)] = typeof(WeiDongZhuoTieSuoLianHuan),
        [typeof(NanManRuQin)] = typeof(WeiDongZhuoNanManRuQin),
        [typeof(WanJianQiFa)] = typeof(WeiDongZhuoWanJianQiFa),
        [typeof(QingLongYanYueDao)] = typeof(WeiDongZhuoQingLongYanYueDao),
        [typeof(GuanShiFu)] = typeof(WeiDongZhuoGuanShiFu),
        [typeof(ZhuQueYuShan)] = typeof(WeiDongZhuoZhuQueYuShan),
        [typeof(ZhuGeLianNu)] = typeof(WeiDongZhuoZhuGeLianNu),
        [typeof(HanBingJian)] = typeof(WeiDongZhuoHanBingJian),
        [typeof(ZhangBaSheMao)] = typeof(WeiDongZhuoZhangBaSheMao),
        [typeof(QingGangJian)] = typeof(WeiDongZhuoQingGangJian),
        [typeof(CiXiongShuangGuJian)] = typeof(WeiDongZhuoCiXiongShuangGuJian),
        [typeof(RenWangDun)] = typeof(WeiDongZhuoRenWangDun),
        [typeof(BaGuaZhen)] = typeof(WeiDongZhuoBaGuaZhen),
        [typeof(BaiYinShiZi)] = typeof(WeiDongZhuoBaiYinShiZi),
        [typeof(DiLu)] = typeof(WeiDongZhuoDiLu),
        [typeof(ChiTu)] = typeof(WeiDongZhuoChiTu),
        [typeof(JueYing)] = typeof(WeiDongZhuoJueYing),
        [typeof(ZiXing)] = typeof(WeiDongZhuoZiXing),
        [typeof(ShenHuangZhongLieCang)] = typeof(WeiDongZhuoShenHuangZhongLieCang),
        [typeof(ShenHuangZhongZhanJue)] = typeof(WeiDongZhuoShenHuangZhongZhanJue),
        [typeof(ShenJiangWeiPingXiang)] = typeof(WeiDongZhuoShenJiangWeiPingXiang),
        [typeof(ShenJiangWeiTianRen)] = typeof(WeiDongZhuoShenJiangWeiTianRen),
        [typeof(ShenMaChaoShouLi)] = typeof(WeiDongZhuoShenMaChaoShouLi),
        [typeof(ShenPangTongFengLiaoYang)] = typeof(WeiDongZhuoShenPangTongFengLiaoYang),
        [typeof(ShenZhangFeiShenCai)] = typeof(WeiDongZhuoShenZhangFeiShenCai),
        [typeof(ShenZhangFeiXunShi)] = typeof(WeiDongZhuoShenZhangFeiXunShi),
        [typeof(ShenZhaoYunLongHun)] = typeof(WeiDongZhuoShenZhaoYunLongHun),
        [typeof(ShenZhugeliangDaWu)] = typeof(WeiDongZhuoShenZhugeliangDaWu),
        [typeof(ShenZhugeliangKuangFeng)] = typeof(WeiDongZhuoShenZhugeliangKuangFeng),
        [typeof(ShenZhugeliangQiXing)] = typeof(WeiDongZhuoShenZhugeliangQiXing),
        [typeof(WuSeSha)] = typeof(WeiDongZhuoWuSeSha),
        [typeof(ShenPangTongFengLiaoYin)] = typeof(WeiDongZhuoShenPangTongFengLiaoYin),
        [typeof(LieCangOption1)] = typeof(WeiDongZhuoLieCangOption1),
        [typeof(LieCangOption2)] = typeof(WeiDongZhuoLieCangOption2),
        [typeof(LieCangOption3)] = typeof(WeiDongZhuoLieCangOption3),
        [typeof(LieCangOption4)] = typeof(WeiDongZhuoLieCangOption4),
        [typeof(LieCangOption5)] = typeof(WeiDongZhuoLieCangOption5),
        [typeof(ZhanJueOption1)] = typeof(WeiDongZhuoZhanJueOption1),
        [typeof(ZhanJueOption2)] = typeof(WeiDongZhuoZhanJueOption2),
    };
    private static readonly IReadOnlyDictionary<Type, Type> ClonesToOriginals =
        OriginalsToClones.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static Type CloneType(Type type) => OriginalsToClones.TryGetValue(type, out var clone) ? clone : type;
    public static Type OriginalType(Type type) => ClonesToOriginals.TryGetValue(type, out var original) ? original : type;
    public static bool IsClone(Type type) => ClonesToOriginals.ContainsKey(type);

    // Called only with canonical entries from ModelDb.AllCards, before combat copies are created.
    public static CardModel ForGeneration(CardModel canonical)
    {
        var type = CloneType(canonical.GetType());
        return type == canonical.GetType() ? canonical : ModelDb.GetById<CardModel>(ModelDb.GetId(type));
    }

    public static string OriginalPortraitPath(Type cloneType)
    {
        if (!ClonesToOriginals.TryGetValue(cloneType, out var originalType))
            throw new ArgumentException("Unregistered clone type", nameof(cloneType));
        var original = ModelDb.GetById<CardModel>(ModelDb.GetId(originalType));
        return original is WuZhugeliangCard card ? card.CustomPortraitPath : original.PortraitPath;
    }
}
