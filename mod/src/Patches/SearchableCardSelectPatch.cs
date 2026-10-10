using System;
using System.Collections.Generic;
using System.Linq;

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

#nullable enable

namespace Yjcm;

/// <summary>
/// 给 NSimpleCardSelectScreen（FromSimpleGrid 使用的选牌屏）注入顶部搜索框与分类筛选。
/// 背置牌 / 荀勖右键在调用选牌前置 SearchEnabled = true，finally 复位，
/// 因此只在这两处出现搜索框，不影响游戏其它选牌流程。
/// 过滤规则与游戏图鉴一致：NSearchBar.Normalize(卡牌标题).Contains(规范化查询)。
/// 分类筛选（2026-08 新增）：
///   - 类型下拉：全部 / 攻击 / 技能 / 能力（按 CardType 过滤）
///   - 角色下拉：全部 / 各候选卡所属卡池（显示名取 characters 本地化表，如 ironclad→铁甲战士、wuzhugeliang→武诸葛亮）
///   两者与标题搜索可叠加；状态为 static，每次注入新屏幕时重置。
/// 注意：必须用类级 [HarmonyPatch] + 方法命名 Postfix（与 YjcmPatches.cs 一致），
/// 方法级 [HarmonyPatch] 在此 Harmony 版本下 PatchAll 不会挂载。
/// </summary>
[HarmonyPatch(typeof(NSimpleCardSelectScreen), "_Ready")]
public static class SearchableCardSelectPatch
{
    /// <summary>是否启用注入（由调用方置位，仅影响下一次创建的选牌屏）</summary>
    public static bool SearchEnabled;

    /// <summary>已注入过搜索框的屏幕实例（防止同一实例重复注入）</summary>
    private static readonly HashSet<NSimpleCardSelectScreen> _injected = new();

    /// <summary>网格原顶部偏移（顶栏高 80px），搜索框放在其正下方</summary>
    private const float TopBarHeight = 80f;
    /// <summary>搜索框高度</summary>
    private const float SearchBarHeight = 48f;
    /// <summary>分类筛选行高度</summary>
    private const float FilterRowHeight = 40f;
    /// <summary>搜索框与筛选行之间的间距</summary>
    private const float RowGap = 8f;

    /// <summary>当前搜索词（static：注入新屏幕时重置）</summary>
    private static string _searchQuery = "";
    /// <summary>当前类型筛选（null = 全部）</summary>
    private static CardType? _typeFilter;
    /// <summary>当前角色（卡池）筛选（null = 全部）</summary>
    private static CardPoolModel? _poolFilter;

    /// <summary>补丁：NSimpleCardSelectScreen._Ready 之后注入搜索框与分类筛选</summary>
    private static void Postfix(NSimpleCardSelectScreen __instance)
    {
        // 注入失败绝不能影响原选牌屏，整体兜底
        try
        {
            MainFile.Logger.Info($"[搜索框] postfix 触发, SearchEnabled={SearchEnabled}, 实例={__instance.GetType().Name}");
            if (!SearchEnabled) return;
            if (!_injected.Add(__instance)) return;

            // 读取基类私有字段：网格与候选卡列表（RitsuLib 保留字段名，走反射）
            var grid = (NCardGrid?)AccessTools.Field(typeof(NCardGridSelectionScreen), "_grid").GetValue(__instance);
            var cards = (IReadOnlyList<CardModel>?)AccessTools.Field(typeof(NCardGridSelectionScreen), "_cards").GetValue(__instance);
            MainFile.Logger.Info($"[搜索框] grid={grid != null}, cards={cards?.Count ?? -1}");
            if (grid == null || cards == null || cards.Count == 0) return;

            // 新屏幕：重置分类状态（static 字段跨屏幕共享，必须复位）
            _searchQuery = "";
            _typeFilter = null;
            _poolFilter = null;

            // 把网格整体下移，给搜索框与筛选行腾出空间（场景里网格顶部 = 顶栏 80px）
            var gridTop = TopBarHeight + SearchBarHeight + RowGap + FilterRowHeight + RowGap;
            grid.OffsetTop = gridTop;

            // 创建搜索框：顶部居中，宽 440、高 48
            var search = new LineEdit
            {
                Name = "YjcmSearchBar",
                PlaceholderText = new LocString("card_library", "SEARCH_PLACEHOLDER").GetRawText(),
                ClearButtonEnabled = true,
            };
            search.AnchorLeft = 0.5f;
            search.AnchorRight = 0.5f;
            search.AnchorTop = 0f;
            search.AnchorBottom = 0f;
            search.OffsetLeft = -220;
            search.OffsetRight = 220;
            search.OffsetTop = TopBarHeight + 4f;
            search.OffsetBottom = TopBarHeight + 4f + SearchBarHeight;
            // 半透明深色背景 + 描边，盖住卡面也能看清
            search.AddThemeStyleboxOverride("normal", MakeStyleBox());
            search.AddThemeStyleboxOverride("focus", MakeStyleBox());

            // 实时过滤（组合：标题搜索 + 类型 + 角色）
            search.TextChanged += q =>
            {
                _searchQuery = q;
                ApplyFilter(grid, cards);
            };
            __instance.AddChild(search);

            // 创建分类筛选行：两个下拉框并排（类型在左、角色在右），位于搜索框正下方
            var typeSelect = CreateDropdown(
                "YjcmTypeFilter",
                new List<string>
                {
                    new LocString("cards", "beizhi.filter_type_all").GetRawText(),
                    new LocString("cards", "beizhi.filter_type_attack").GetRawText(),
                    new LocString("cards", "beizhi.filter_type_skill").GetRawText(),
                    new LocString("cards", "beizhi.filter_type_power").GetRawText(),
                },
                left: -212, right: -2, top: TopBarHeight + 4f + SearchBarHeight + RowGap);
            // 索引 0 = 全部；1/2/3 = 攻击/技能/能力
            typeSelect.ItemSelected += idx =>
            {
                _typeFilter = (long)idx switch
                {
                    1 => CardType.Attack,
                    2 => CardType.Skill,
                    3 => CardType.Power,
                    _ => null,
                };
                MainFile.Logger.Info($"[搜索框] 类型筛选: {_typeFilter?.ToString() ?? "全部"}");
                ApplyFilter(grid, cards);
            };

            // 角色下拉：从候选卡去重取卡池，显示名取 characters 本地化表
            var pools = cards.Select(c => c.Pool).Distinct().OrderBy(p => p.Title).ToList();
            var poolItems = new List<CardPoolModel> { null! }; // 索引 0 占位：全部
            var poolLabels = new List<string> { new LocString("cards", "beizhi.filter_pool_all").GetRawText() };
            foreach (var p in pools)
            {
                poolItems.Add(p);
                poolLabels.Add(PoolDisplayName(p));
            }
            var poolSelect = CreateDropdown("YjcmPoolFilter", poolLabels,
                left: 2, right: 212, top: TopBarHeight + 4f + SearchBarHeight + RowGap);
            poolSelect.ItemSelected += idx =>
            {
                var i = (long)idx;
                _poolFilter = i <= 0 ? null : poolItems[(int)i];
                MainFile.Logger.Info($"[搜索框] 角色筛选: {(_poolFilter?.Title ?? "全部")}");
                ApplyFilter(grid, cards);
            };

            __instance.AddChild(typeSelect);
            __instance.AddChild(poolSelect);
            MainFile.Logger.Info($"[搜索框] 搜索框与分类筛选已注入（网格顶部: {gridTop}），角色数: {pools.Count}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"搜索框注入失败（不影响选牌）: {ex}");
        }
    }

    /// <summary>统一背景样式：半透明深色 + 描边</summary>
    private static StyleBoxFlat MakeStyleBox()
    {
        var bg = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.85f) };
        bg.SetBorderWidthAll(2);
        bg.BorderColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        return bg;
    }

    /// <summary>创建一个顶栏下方居中的下拉框（宽 210、高 40）</summary>
    private static OptionButton CreateDropdown(string name, List<string> labels, float left, float right, float top)
    {
        var dropdown = new OptionButton { Name = name };
        dropdown.AnchorLeft = 0.5f;
        dropdown.AnchorRight = 0.5f;
        dropdown.AnchorTop = 0f;
        dropdown.AnchorBottom = 0f;
        dropdown.OffsetLeft = left;
        dropdown.OffsetRight = right;
        dropdown.OffsetTop = top;
        dropdown.OffsetBottom = top + FilterRowHeight;
        foreach (var label in labels)
            dropdown.AddItem(label);
        dropdown.Select(0); // 默认选中"全部"
        // 样式：与搜索框一致的背景 + 白字
        dropdown.AddThemeStyleboxOverride("normal", MakeStyleBox());
        dropdown.AddThemeStyleboxOverride("focus", MakeStyleBox());
        dropdown.AddThemeStyleboxOverride("hover", MakeStyleBox());
        dropdown.AddThemeStyleboxOverride("pressed", MakeStyleBox());
        dropdown.AddThemeColorOverride("font_color", Colors.White);
        dropdown.AddThemeColorOverride("font_hover_color", Colors.White);
        dropdown.AddThemeColorOverride("font_pressed_color", Colors.White);
        dropdown.AddThemeColorOverride("font_focus_color", Colors.White);
        return dropdown;
    }

    /// <summary>卡池显示名：characters 本地化表（pool.Title 大写 + .title），不存在时回退英文池名</summary>
    private static string PoolDisplayName(CardPoolModel pool)
    {
        var loc = LocString.GetIfExists("characters", pool.Title.ToUpperInvariant() + ".title");
        return loc != null ? loc.GetFormattedText() : pool.Title;
    }

    /// <summary>按 类型 + 角色 + 标题搜索 组合过滤并重建网格</summary>
    private static void ApplyFilter(NCardGrid grid, IReadOnlyList<CardModel> cards)
    {
        try
        {
            IEnumerable<CardModel> filtered = cards;

            // 类型筛选
            if (_typeFilter != null)
                filtered = filtered.Where(c => c.Type == _typeFilter.Value);

            // 角色（卡池）筛选
            if (_poolFilter != null)
                filtered = filtered.Where(c => c.Pool == _poolFilter);

            // 标题搜索（与游戏图鉴一致的规范化包含匹配）
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var normalized = NSearchBar.Normalize(_searchQuery);
                filtered = filtered.Where(c => NSearchBar.Normalize(c.Title).Contains(normalized));
            }

            var result = filtered.ToList();

            // 重建网格并回到顶部（无匹配时为空网格，清空筛选即可恢复）
            grid.SetCards(result, PileType.None, new List<SortingOrders> { SortingOrders.Ascending });
            grid.SetScrollPosition(0f);
            grid.OffsetTop = TopBarHeight + SearchBarHeight + RowGap + FilterRowHeight + RowGap; // SetCards 后兜底保证位置
            MainFile.Logger.Info($"[搜索框] 过滤: 候选={cards.Count}, 匹配={result.Count}, query='{_searchQuery}', type={_typeFilter?.ToString() ?? "全部"}, pool={_poolFilter?.Title ?? "全部"}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"搜索过滤失败: {ex.Message}");
        }
    }
}
