using Godot;
using System;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

public class SomeOtherNode : Node
{
    private IntPtr iconTextureId;
    private Texture iconTexture;
    private bool filter;
    private int iconSize;

    public override void _Ready()
    {
        // connect the signal in code or in the editor
        // GetNode<ImGuiNode>("/root/Control/ImGui").Connect("IGLayout", this, nameof(_onLayout));

        iconTexture = GD.Load<Texture>("res://icon.png");
        iconTextureId = ImGuiGD.BindTexture(iconTexture);
        filter = (iconTexture.Flags & (uint)Texture.FlagsEnum.Filter) != 0;
        iconSize = (int)iconTexture.GetSize().x;
    }

    public override void _ExitTree()
    {
        ImGuiGD.UnbindTexture(iconTextureId);
    }

    public void _onLayout()
    {
        ImGui.Begin("test");
        ImGui.Text("hello Godot");
        ImGui.Image(iconTextureId, new Vector2(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);
        ImGui.Checkbox("filter", ref filter);

        ImGui.Dummy(new Vector2(0, 20.0f));

        if (ImGui.Button("change scene"))
        {
            GetTree().ChangeScene("res://demo2.tscn");
        }

        ImGui.End();

        ImGui.ShowDemoWindow();

        if (!filter)
            iconTexture.Flags &= ~((uint)Texture.FlagsEnum.Filter);
        else
            iconTexture.Flags |= (uint)Texture.FlagsEnum.Filter;
    }
}
