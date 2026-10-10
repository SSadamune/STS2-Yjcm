using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace Yjcm;

// Only replace presentation: CardSelectCmd still owns choice signalling, replay and network sync.
[HarmonyPatch(typeof(NSimpleCardSelectScreen), "_Ready")]
internal static class LieCangBodySelectionPatch
{
    internal const string PanelName = "YjcmLieCangBodySelection";
    internal const string AtlasPath = "res://Yjcm/Images/ui/lie_cang_atlas.webp";
    private static readonly System.Reflection.FieldInfo CardsField =
        AccessTools.Field(typeof(NCardGridSelectionScreen), "_cards");
    private static readonly System.Reflection.FieldInfo SelectedField =
        AccessTools.Field(typeof(NSimpleCardSelectScreen), "_selectedCards");
    private static readonly System.Reflection.MethodInfo CompleteMethod =
        AccessTools.Method(typeof(NSimpleCardSelectScreen), "CompleteSelection");

    // Coordinates are relative to the original 303px-wide atlas panel, not the viewport.
    internal static readonly (int Option, string Name, Vector2 Center)[] Points =
    [
        (3, "中枢", new Vector2(149, 171)),
        (4, "气海", new Vector2(148, 245)),
        (1, "力烽", new Vector2(221, 229)),
        (2, "地机", new Vector2(199, 430)),
        (5, "天冲", new Vector2(149, 90))
    ];

    private static void Postfix(NSimpleCardSelectScreen __instance)
    {
        if (CardsField.GetValue(__instance) is not IReadOnlyList<CardModel> cards ||
            cards.Count is not (4 or 5) || cards.Any(c => OptionNumber(c) == 0))
            return;

        Control overlay = null;
        try
        {
            var atlas = ResourceLoader.Load<Texture2D>(AtlasPath)
                ?? throw new InvalidOperationException("裂穹部位选择贴图未加载。");
            // Build everything before hiding the native controls; keep the original selector if assets fail.
            overlay = BuildOverlay(__instance, cards, atlas);
            var originalControls = __instance.GetChildren().OfType<Control>().ToArray();
            __instance.AddChild(overlay);
            foreach (var child in originalControls) child.Hide();
        }
        catch (Exception ex)
        {
            overlay?.QueueFree();
            MainFile.Logger.Warn($"裂穹部位界面初始化失败，保留原生选牌界面：{ex.Message}");
        }
    }

    private static int OptionNumber(CardModel card)
    {
        string name = card.GetType().Name;
        // Both the original character and generated Dong Zhuo copies use this presentation.
        for (int option = 1; option <= 5; option++)
            if (name == $"LieCangOption{option}" || name == $"WeiDongZhuoLieCangOption{option}")
                return option;
        return 0;
    }

    private static Control BuildOverlay(NSimpleCardSelectScreen screen, IReadOnlyList<CardModel> cards, Texture2D atlas)
    {
        var overlay = new Control { Name = PanelName, MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, 0.35f), MouseFilter = Control.MouseFilterEnum.Ignore };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(shade);

        var panel = new Control { Name = "BodyPanel", Size = new Vector2(303, 616) };
        overlay.AddChild(panel);
        AddTexture(panel, atlas, new Rect2(598, 0, 28, 547), new Vector2(0, 26), new Vector2(303, 547));
        AddTexture(panel, atlas, new Rect2(0, 0, 298, 547), new Vector2(2, 26), new Vector2(298, 547));
        AddTexture(panel, atlas, new Rect2(0, 548, 303, 26), Vector2.Zero, new Vector2(303, 26));
        AddTexture(panel, atlas, new Rect2(0, 575, 302, 9), new Vector2(0, 573), new Vector2(303, 9));
        AddLabel(panel, "裂穹", Vector2.Zero, new Vector2(303, 26), 23);
        AddLabel(panel, "选择一个部位击伤", new Vector2(-35, 584), new Vector2(373, 32), 23);

        var hint = new Label
        {
            Name = "EffectHint", Position = new Vector2(319, 195), Size = new Vector2(240, 145),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1, 0.9f, 0.64f), Visible = false
        };
        hint.AddThemeFontSizeOverride("font_size", 21);
        panel.AddChild(hint);

        var buttons = new List<Button>();
        bool completed = false;
        foreach (var point in Points)
        {
            var card = cards.FirstOrDefault(c => OptionNumber(c) == point.Option);
            if (card == null) continue; // Tian Chong is absent until the original marker unlocks it.
            var button = new Button
            {
                Name = $"HitPoint{point.Option}", Position = point.Center - new Vector2(37.5f, 38),
                Size = new Vector2(75, 76), CustomMinimumSize = new Vector2(75, 76),
                MouseDefaultCursorShape = Control.CursorShape.PointingHand,
                FocusMode = Control.FocusModeEnum.All, TooltipText = $"{point.Name}\n{card.Description.GetFormattedText()}"
            };
            foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
                button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
            panel.AddChild(button);
            var glow = AddTexture(button, atlas, new Rect2(304, 548, 75, 76), Vector2.Zero, new Vector2(75, 76));
            void Highlight(bool active)
            {
                glow.Modulate = active ? new Color(1.3f, 1.2f, 0.8f) : Colors.White;
                hint.Text = $"{point.Name}\n{card.Description.GetFormattedText()}";
                hint.Visible = active;
            }
            button.MouseEntered += () => Highlight(true);
            button.MouseExited += () => Highlight(button.HasFocus());
            button.FocusEntered += () => Highlight(true);
            button.FocusExited += () => Highlight(false);
            button.Pressed += () =>
            {
                if (completed) return;
                completed = true;
                foreach (var item in buttons) item.Disabled = true;
                var selected = (HashSet<CardModel>)SelectedField.GetValue(screen)!;
                selected.Clear();
                selected.Add(card);
                // Native completion closes the overlay and returns the same card to CardSelectCmd.
                CompleteMethod.Invoke(screen, null);
            };
            buttons.Add(button);
        }
        // Explicit focus loop supports keyboard/controller without focusing the hidden card grid.
        for (int i = 0; i < buttons.Count; i++)
        {
            // Paths on each button must be relative to that button, not to the panel.
            buttons[i].FocusNeighborTop = buttons[i].GetPathTo(buttons[(i + buttons.Count - 1) % buttons.Count]);
            buttons[i].FocusNeighborBottom = buttons[i].GetPathTo(buttons[(i + 1) % buttons.Count]);
            buttons[i].FocusNeighborLeft = buttons[i].FocusNeighborTop;
            buttons[i].FocusNeighborRight = buttons[i].FocusNeighborBottom;
        }
        void Layout()
        {
            float factor = Mathf.Min(1.5f, Mathf.Min((overlay.Size.Y - 100) / 616f, (overlay.Size.X - 100) / 820f));
            factor = Mathf.Max(0.3f, factor);
            panel.Scale = Vector2.One * factor;
            panel.Position = (overlay.Size - panel.Size * factor) / 2;
        }
        overlay.Resized += Layout;
        overlay.Ready += Layout;
        return overlay;
    }

    private static TextureRect AddTexture(Control parent, Texture2D atlas, Rect2 region, Vector2 position, Vector2 size)
    {
        var texture = new TextureRect
        {
            Texture = new AtlasTexture { Atlas = atlas, Region = region, FilterClip = true }, Position = position, Size = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        parent.AddChild(texture);
        return texture;
    }

    private static void AddLabel(Control parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var label = new Label
        {
            Text = text, Position = position, Size = size, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1, 0.9f, 0.64f)
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        parent.AddChild(label);
    }
}

[HarmonyPatch]
internal static class LieCangBodySelectionFocusPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.DefaultFocusedControl));
        yield return AccessTools.PropertyGetter(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.FocusedControlFromTopBar));
    }

    private static void Postfix(NCardGridSelectionScreen __instance, ref Control __result)
    {
        if (__instance.GetNodeOrNull<Control>(LieCangBodySelectionPatch.PanelName) is { } panel)
            __result = panel.GetNode<Button>("BodyPanel/HitPoint3");
    }
}

[HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.AfterOverlayShown))]
internal static class LieCangBodySelectionPeekPatch
{
    private static void Postfix(NCardGridSelectionScreen __instance)
    {
        if (__instance.GetNodeOrNull<Control>(LieCangBodySelectionPatch.PanelName) != null)
            __instance.GetNode<MegaCrit.Sts2.Core.Nodes.Combat.NPeekButton>("%PeekButton").Disable();
    }
}
