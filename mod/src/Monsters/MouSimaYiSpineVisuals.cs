using Godot;

using MegaCrit.Sts2.Core.Nodes.Rooms;

#nullable enable

namespace Yjcm;

/// <summary>
/// Loads the pruned Spine 4.2 assets from the loose mod bundle at runtime.
/// The game already ships spine-godot, so no editor extension is required.
/// </summary>
internal static class MouSimaYiSpineVisuals
{
    public const string YangNodeName = "YangSpine";
    public const string YinNodeName = "YinSpine";
    public const string TransitionNodeName = "TransitionSpine";

    private const float BaseScale = 0.65f;
    private const float YangActionScale = 1.00f;
    private const float YinActionScale = 1.00f;
    private const float ActionHorizontalOffset = -400f;
    private static readonly Vector2 YangActionCenter = new(-410.6386f, 34.7932f);
    private static readonly Vector2 YinActionCenter = new(-450.6650f, 36.8907f);
    private static readonly string AssetRoot = Path.Combine(
        Path.GetDirectoryName(typeof(MouSimaYiSpineVisuals).Assembly.Location)!,
        "Spine", "MouSimaYi");

    private static CanvasLayer? _activeActionLayer;

    public static bool Preflight()
    {
        if (!ClassDB.ClassExists("SpineSprite"))
        {
            MainFile.Logger.Warn("谋司马懿 Spine 预检失败：游戏未注册 SpineSprite。");
            return false;
        }

        CanvasItem? yang = null;
        CanvasItem? yin = null;
        CanvasItem? transition = null;
        CanvasItem? yangAttack = null;
        CanvasItem? yangSkill = null;
        CanvasItem? yinAttack = null;
        CanvasItem? yinSkill = null;
        try
        {
            yang = CreateSprite("Yang", "daiji", YangNodeName, Vector2.Zero, BaseScale, true);
            yin = CreateSprite("Yin", "daiji", YinNodeName, new Vector2(0f, 70f), BaseScale * 1.55f, true);
            transition = CreateSprite(
                "Transition", "yinyang_mousimayi", TransitionNodeName, Vector2.Zero, BaseScale, false);
            yangAttack = CreateSprite(
                "ActionYang", "chuchang2", "YangAttackPreflight", Vector2.Zero, 1f, false, "gongji");
            yangSkill = CreateSprite(
                "ActionYang", "chuchang2", "YangSkillPreflight", Vector2.Zero, 1f, false, "jineng");
            yinAttack = CreateSprite(
                "ActionYin", "chuchang2", "YinAttackPreflight", Vector2.Zero, 1f, false, "gongji");
            yinSkill = CreateSprite(
                "ActionYin", "chuchang2", "YinSkillPreflight", Vector2.Zero, 1f, false, "jineng");
            MainFile.Logger.Info("谋司马懿 Spine 4.2 素材预检成功（阳/阴/转换/攻击/技能）。");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"谋司马懿 Spine 预检失败：{ex}");
            return false;
        }
        finally
        {
            yang?.Free();
            yin?.Free();
            transition?.Free();
            yangAttack?.Free();
            yangSkill?.Free();
            yinAttack?.Free();
            yinSkill?.Free();
        }
    }

    /// <summary>
    /// 在独立 CanvasLayer 中播放行动动画，不改变首领本体 Spine。
    /// 含攻击的混合行动由调用方传入 attack=true，从而优先使用 gongji。
    /// </summary>
    public static async Task PlayActionOverlay(bool isYang, bool attack)
    {
        CanvasLayer? layer = null;
        try
        {
            if (!WuZhugeliangSettings.EnableMouSimaYiEffects)
                return;

            if (!ClassDB.ClassExists("SpineSprite") || NCombatRoom.Instance is not { } room)
                return;

            StopActionOverlay();

            string folder = isYang ? "ActionYang" : "ActionYin";
            string animation = attack ? "gongji" : "jineng";
            float scale = isYang ? YangActionScale : YinActionScale;
            Vector2 sourceCenter = isYang ? YangActionCenter : YinActionCenter;
            float duration = attack ? 1.5f : isYang ? 2.3334f : 2.5001f;

            layer = new CanvasLayer
            {
                Name = "MouSimaYiActionOverlay",
                Layer = 30
            };
            room.AddChild(layer);

            Vector2 viewportCenter = room.GetViewport().GetVisibleRect().Size * 0.5f;
            CanvasItem sprite = CreateSprite(
                folder,
                "chuchang2",
                isYang ? "YangActionSpine" : "YinActionSpine",
                viewportCenter - sourceCenter * scale + new Vector2(ActionHorizontalOffset, 0f),
                scale,
                false,
                animation);
            sprite.ZIndex = 100;
            layer.AddChild(sprite);
            _activeActionLayer = layer;

            MainFile.Logger.Info($"谋司马懿屏幕行动动画：{(isYang ? "阳" : "阴")}/{animation}");
            await layer.ToSignal(
                layer.GetTree().CreateTimer(duration),
                SceneTreeTimer.SignalName.Timeout);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"谋司马懿屏幕行动动画播放失败：{ex}");
        }
        finally
        {
            if (layer != null && GodotObject.IsInstanceValid(layer))
                layer.QueueFree();
            if (ReferenceEquals(_activeActionLayer, layer))
                _activeActionLayer = null;
        }
    }

    internal static void HideActionOverlay()
    {
        if (_activeActionLayer != null && GodotObject.IsInstanceValid(_activeActionLayer)) _activeActionLayer.Visible = false;
    }

    public static void StopActionOverlay()
    {
        if (_activeActionLayer != null && GodotObject.IsInstanceValid(_activeActionLayer))
            _activeActionLayer.QueueFree();
        _activeActionLayer = null;
    }

    public static bool EnsureAttached(Node2D visuals)
    {
        if (visuals.GetNodeOrNull<CanvasItem>(YangNodeName) != null)
            return true;

        try
        {
            var yang = CreateSprite("Yang", "daiji", YangNodeName, Vector2.Zero, BaseScale, true);
            var yin = CreateSprite("Yin", "daiji", YinNodeName, new Vector2(0f, 70f), BaseScale * 1.55f, true);
            var transition = CreateSprite(
                "Transition", "yinyang_mousimayi", TransitionNodeName, Vector2.Zero, BaseScale, false);

            yin.Visible = false;
            transition.Visible = false;
            transition.ZIndex = 10;
            visuals.AddChild(yang);
            visuals.AddChild(yin);
            visuals.AddChild(transition);

            // Hide the old sequence-frame nodes while they are still present in older PCKs.
            SetFallbackVisible(visuals, false);
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"谋司马懿 Spine 节点创建失败，将回退到序列帧：{ex}");
            SetFallbackVisible(visuals, true);
            return false;
        }
    }

    public static void SetFormVisibility(Node2D visuals, bool showYang)
    {
        var yang = visuals.GetNodeOrNull<CanvasItem>(YangNodeName);
        var yin = visuals.GetNodeOrNull<CanvasItem>(YinNodeName);
        if (yang != null && yin != null)
        {
            yang.Visible = showYang;
            yin.Visible = !showYang;
            SetFallbackVisible(visuals, false);
            return;
        }

        var oldYang = visuals.GetNodeOrNull<AnimatedSprite2D>("YangVisual");
        var oldYin = visuals.GetNodeOrNull<AnimatedSprite2D>("YinVisual");
        if (oldYang != null) oldYang.Visible = showYang;
        if (oldYin != null) oldYin.Visible = !showYang;
    }

    public static CanvasItem? GetTransition(Node2D visuals) =>
        visuals.GetNodeOrNull<CanvasItem>(TransitionNodeName);

    public static void Play(CanvasItem sprite, string animation, bool loop)
    {
        GodotObject? state = sprite.Call("get_animation_state").AsGodotObject();
        if (state == null)
            throw new InvalidOperationException($"{sprite.Name} 没有 SpineAnimationState。");
        state.Call("set_animation", animation, loop, 0);
    }

    private static CanvasItem CreateSprite(
        string folder,
        string fileName,
        string nodeName,
        Vector2 position,
        float scale,
        bool loop,
        string animation = "play")
    {
        string folderPath = Path.Combine(AssetRoot, folder);
        string skeletonPath = Path.Combine(folderPath, fileName + ".skel");
        string atlasPath = Path.Combine(folderPath, fileName + ".atlas");
        if (!File.Exists(skeletonPath) || !File.Exists(atlasPath))
            throw new FileNotFoundException($"缺少 Spine 素材：{folderPath}");

        GodotObject skeletonFile = Instantiate("SpineSkeletonFileResource");
        skeletonFile.Call("load_from_file", skeletonPath);

        GodotObject atlas = Instantiate("SpineAtlasResource");
        atlas.Call("load_from_atlas_file", atlasPath);

        GodotObject data = Instantiate("SpineSkeletonDataResource");
        data.Set("skeleton_file_res", skeletonFile);
        data.Set("atlas_res", atlas);

        GodotObject rawSprite = Instantiate("SpineSprite");
        rawSprite.Set("skeleton_data_res", data);
        if (rawSprite is not Node2D sprite)
            throw new InvalidCastException("SpineSprite 不是 Node2D。");

        sprite.Name = nodeName;
        sprite.Position = position;
        sprite.Scale = new Vector2(scale, scale);
        // The source art is intentionally dark. HDR self-modulation lifts it without
        // introducing an opaque background and matches the approved Laya preview.
        sprite.SelfModulate = new Color(1.35f, 1.35f, 1.35f, 1f);
        Play(sprite, animation, loop);
        return sprite;
    }

    private static GodotObject Instantiate(string className)
    {
        GodotObject? instance = ClassDB.Instantiate(className).AsGodotObject();
        return instance ?? throw new InvalidOperationException($"无法实例化 {className}。");
    }

    private static void SetFallbackVisible(Node2D visuals, bool visible)
    {
        var yang = visuals.GetNodeOrNull<AnimatedSprite2D>("YangVisual");
        var yin = visuals.GetNodeOrNull<AnimatedSprite2D>("YinVisual");
        var transition = visuals.GetNodeOrNull<AnimatedSprite2D>("TransitionVisual");
        if (yang != null) yang.Visible = visible;
        if (yin != null) yin.Visible = false;
        if (transition != null) transition.Visible = false;
    }
}
