using Godot;
using MegaCrit.Sts2.Core.Assets;

namespace Yjcm;

/// <summary>
/// 充能球视觉构建助手。
/// NOrb 在创建球 sprite 时要求存在 SpineSkeleton 子节点，否则
/// <c>new MegaSprite(GetNode("SpineSkeleton"))</c> 会抛「Expected a GodotObject but was Nil!」异常，
/// 在装备替换路径（ReplaceOrb）上会导致游戏卡死。
/// 这里在装备图标上方附加一个隐藏的原版 SpineSprite 节点，仅用于满足 spine 检测。
/// </summary>
public static class EquipmentOrbSprite
{
    // 复用原版闪电球场景的 SpineSprite（自带 skeleton 数据与 idle_loop 动画）
    private const string VanillaSpineScene = "res://scenes/orbs/orb_visuals/lightning_orb.tscn";

    /// <summary>
    /// 构建充能球 sprite：装备图标 + 隐藏的 SpineSkeleton 子节点。
    /// </summary>
    /// <param name="iconPath">装备图标贴图路径。</param>
    /// <param name="scale">图标缩放（空槽贴图需要缩小到 50%）。</param>
    public static Node2D Create(string iconPath, float scale = 1f)
    {
        var node = new Node2D();
        node.AddChild(new Sprite2D
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            Scale = new Vector2(scale, scale)
        });
        node.AddChild(CreateHiddenSpineSkeleton());
        return node;
    }

    /// <summary>
    /// 构建充能球 sprite，并按目标最大尺寸归一化缩放（适配不同来源贴图：256 图标、800 卡面等）。
    /// </summary>
    /// <param name="iconPath">图标贴图路径。</param>
    /// <param name="maxSize">渲染后最长边像素值（默认 77，约为空槽的 60%）。</param>
    public static Node2D CreateNormalized(string iconPath, float maxSize = 77f)
    {
        var tex = ResourceLoader.Load<Texture2D>(iconPath);
        var node = new Node2D();
        float scale = 1f;
        if (tex != null)
        {
            float largest = Math.Max(tex.GetWidth(), tex.GetHeight());
            if (largest > 0) scale = maxSize / largest;
        }
        node.AddChild(new Sprite2D { Texture = tex, Scale = new Vector2(scale, scale) });
        node.AddChild(CreateHiddenSpineSkeleton());
        return node;
    }

    // 从原版充能球场景提取 SpineSprite 节点，重命名并隐藏，仅满足 NOrb 的 spine 检测
    private static CanvasItem CreateHiddenSpineSkeleton()
    {
        var vanilla = PreloadManager.Cache.GetScene(VanillaSpineScene)
            .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        var spine = vanilla.GetNode<CanvasItem>("SpineSkeleton");
        vanilla.RemoveChild(spine);
        spine.Name = "SpineSkeleton";
        spine.Visible = false;
        return spine;
    }
}
