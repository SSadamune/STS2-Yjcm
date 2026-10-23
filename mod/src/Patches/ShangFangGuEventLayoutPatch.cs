using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace Yjcm;

/// <summary>Keep the landscape illustration out of the native portrait's oversized minimum size.</summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
internal static class ShangFangGuEventLayoutPatch
{
    internal const string BackgroundPath = "res://Yjcm/scenes/shang_fang_gu_event_background.tscn";

    private static void Postfix(NEventLayout __instance, EventModel eventModel)
    {
        if (eventModel is not ShangFangGuZhiYu || __instance.HasNode("ShangFangGuBackdrop")) return;
        var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait == null) return;
        var scene = GD.Load<PackedScene>(BackgroundPath);
        if (scene == null)
        {
            MainFile.Logger.Warn("上方谷之雨背景布局加载失败，保留原生图片。");
            return;
        }

        var backdrop = scene.Instantiate<Control>();
        Node anchor = portrait;
        while (anchor.GetParent() != null && anchor.GetParent() != __instance)
            anchor = anchor.GetParent();
        __instance.AddChild(backdrop);
        __instance.MoveChild(backdrop, anchor.GetIndex());
        // Only this event replaces its portrait. Keep the native labels, buttons,
        // voting, hover tips and selection callbacks unchanged and above the art.
        portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        portrait.CustomMinimumSize = Vector2.Zero;
        portrait.Texture = null;
        portrait.Hide();
    }
}
