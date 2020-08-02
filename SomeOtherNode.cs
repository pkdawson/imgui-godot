using Godot;
using System;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

public class SomeOtherNode : Node
{
    private ImGuiNode ig;
    private IntPtr iconTextureId;
    private Texture iconTexture;
    private bool filter;
    private int iconSize;

    public override void _Ready()
    {
        ig = GetNode<ImGuiNode>("/root/Control/ImGui");
        ig.Connect("LayoutSignal", this, nameof(_Layout));
        iconTexture = GD.Load<Texture>("res://icon.png");
        iconTextureId = ig.BindTexture(iconTexture);
        filter = (iconTexture.Flags & (uint)Texture.FlagsEnum.Filter) != 0;
        iconSize = (int)iconTexture.GetSize().x;
    }

    public void _Layout()
    {
        ImGui.Begin("test");
        ImGui.Text("hello Godot");
        ImGui.Image(iconTextureId, new Vector2(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);
        ImGui.Checkbox("filter", ref filter);
        ImGui.End();

        ImGui.ShowDemoWindow();

        if (!filter)
            iconTexture.Flags &= ~((uint)Texture.FlagsEnum.Filter);
        else
            iconTexture.Flags |= (uint)Texture.FlagsEnum.Filter;
    }
}
