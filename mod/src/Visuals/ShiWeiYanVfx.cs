using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Yjcm;

/// <summary>势魏延事件牌的临时 Spine 特效映射，供游戏内逐项确认素材归属。</summary>
internal enum ShiWeiYanVfxStyle
{
    ZhuangShi,
    ZhuangShiAo,
    ZhongAoCompleted
}

internal static class ShiWeiYanVfx
{
    private const float BannerSourceWidth = 1334f;
    private const float BannerFill = 1.05f;
    private const float SmallEffectScale = 3.5f;
    private const float Duration = 2.5f;
    private static readonly string Root = Path.Combine(
        Path.GetDirectoryName(typeof(ShiWeiYanVfx).Assembly.Location)!, "Spine", "ShiWeiYan");
    private static CanvasLayer? _activeLayer;

    public static async Task Play(ShiWeiYanVfxStyle style)
    {
        if (!WuZhugeliangSettings.EnableEffects) return;
        CanvasLayer? layer = null;
        try
        {
            if (!ClassDB.ClassExists("SpineSprite") || NCombatRoom.Instance is not { } room) return;
            Stop();
            var asset = GetAsset(style);
            Vector2 viewport = room.GetViewport().GetVisibleRect().Size;
            float scale = asset.IsBanner ? viewport.X / BannerSourceWidth * BannerFill : SmallEffectScale;
            layer = new CanvasLayer { Name = $"ShiWeiYanVfx_{style}", Layer = 31 };
            room.AddChild(layer);
            var sprite = CreateSprite(viewport * 0.5f, scale, asset);
            sprite.ZIndex = 100;
            layer.AddChild(sprite);
            _activeLayer = layer;
            await layer.ToSignal(layer.GetTree().CreateTimer(Duration), SceneTreeTimer.SignalName.Timeout);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"势魏延特效播放失败（{style}）：{ex}");
        }
        finally
        {
            if (layer != null && GodotObject.IsInstanceValid(layer)) layer.QueueFree();
            if (ReferenceEquals(_activeLayer, layer)) _activeLayer = null;
        }
    }

    internal static void HideActive()
    {
        // Keep the signal receiver alive until its existing timer completes.
        if (_activeLayer != null && GodotObject.IsInstanceValid(_activeLayer)) _activeLayer.Visible = false;
    }

    private static void Stop()
    {
        if (_activeLayer != null && GodotObject.IsInstanceValid(_activeLayer)) _activeLayer.QueueFree();
        _activeLayer = null;
    }

    private static CanvasItem CreateSprite(Vector2 position, float scale,
        (string Folder, string Skeleton, string Atlas, string Animation, bool IsBanner) asset)
    {
        string assetRoot = Path.Combine(Root, asset.Folder);
        string skeletonPath = Path.Combine(assetRoot, asset.Skeleton);
        string atlasPath = Path.Combine(assetRoot, asset.Atlas);
        if (!File.Exists(skeletonPath) || !File.Exists(atlasPath))
            throw new FileNotFoundException($"缺少势魏延 Spine 素材：{assetRoot}");
        var skeleton = Instantiate("SpineSkeletonFileResource");
        skeleton.Call("load_from_file", skeletonPath);
        var atlas = Instantiate("SpineAtlasResource");
        atlas.Call("load_from_atlas_file", atlasPath);
        var data = Instantiate("SpineSkeletonDataResource");
        data.Set("skeleton_file_res", skeleton);
        data.Set("atlas_res", atlas);
        var raw = Instantiate("SpineSprite");
        raw.Set("skeleton_data_res", data);
        if (raw is not Node2D sprite) throw new InvalidCastException("SpineSprite 不是 Node2D。");
        sprite.Position = position;
        sprite.Scale = new Vector2(scale, scale);
        var animationState = sprite.Call("get_animation_state").AsGodotObject()
            ?? throw new InvalidOperationException("势魏延 Spine 没有动画状态。");
        animationState.Call("set_animation", asset.Animation, false, 0);
        return sprite;
    }

    private static GodotObject Instantiate(string className) =>
        ClassDB.Instantiate(className).AsGodotObject()
        ?? throw new InvalidOperationException($"无法实例化 {className}。");

    // 当前为用户指定的临时顺序，待游戏内对照后可只改此映射。
    private static (string Folder, string Skeleton, string Atlas, string Animation, bool IsBanner) GetAsset(ShiWeiYanVfxStyle style) => style switch
    {
        ShiWeiYanVfxStyle.ZhuangShi => ("ZhuangShi", "SS_SWY_zhuangshi1.skel", "SS_SWY_zhuangshi1.atlas", "play", true),
        ShiWeiYanVfxStyle.ZhuangShiAo => ("ZhuangShiAo", "SS_SWY_zhuangshi2.skel", "SS_SWY_zhuangshi2.atlas", "play", true),
        ShiWeiYanVfxStyle.ZhongAoCompleted => ("ZhongAo", "SS_ShiWeiyanFaDongSkill.skel", "SS_ShiWeiyanFaDongSkill.atlas", "play1", false),
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "未知的势魏延特效。")
    };
}
