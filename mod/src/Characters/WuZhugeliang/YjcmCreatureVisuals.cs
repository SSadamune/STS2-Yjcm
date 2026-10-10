using MegaCrit.Sts2.Core.Nodes.Combat;

using Godot;

namespace Yjcm;

public partial class YjcmCreatureVisuals : NCreatureVisuals
{
    public override void _Ready()
    {
        base._Ready();
        Node2D? host = GetNodeOrNull<Node2D>("%Visuals") ?? GetNodeOrNull<Node2D>("Visuals");
        if (host == null)
        {
            MainFile.Logger.Error("武诸葛网格动画加载失败：角色场景缺少 Visuals 节点。");
            return;
        }
        try
        {
            var animation = new WuZhugeliangMeshAnimation
            {
                Name = "WuZhugeliangMeshAnimation",
                Position = new Vector2(130f, -77f),
                Scale = new Vector2(0.65f, 0.65f)
            };
            host.AddChild(animation);
            MainFile.Logger.Info("武诸葛 Laya 网格动画已启用（play/play2，单图集实时渲染）。");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"武诸葛网格动画加载失败：{ex}");
        }
    }
}
