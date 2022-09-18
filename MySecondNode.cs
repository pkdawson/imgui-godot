using Godot;
using ImGuiNET;
using System;
using Vector2 = System.Numerics.Vector2;

public partial class MySecondNode : Node
{
    private IntPtr iconTextureId;
    private Texture2D iconTexture;
    private int iconSize = 64;

    public override void _Ready()
    {
        // connect the signal in code or in the editor
        GetNode<ImGuiNode>("%ImGuiNode").imgui_layout += _on_imgui_layout;

        iconTexture = GD.Load<Texture2D>("res://icon.svg");
        iconTextureId = ImGuiGD.BindTexture(iconTexture);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
    }

    public override void _ExitTree()
    {
        ImGuiGD.UnbindTexture(iconTextureId);
    }

    public void _on_imgui_layout()
    {
        ImGui.Begin("test");
        ImGui.Text("hello Godot 4");
        ImGui.Image(iconTextureId, new Vector2(iconSize, iconSize));
        ImGui.DragInt("size", ref iconSize, 1.0f, 32, 512);

        ImGui.Dummy(new Vector2(0, 20.0f));

        if (ImGui.Button("change scene"))
        {
            GetTree().ChangeSceneToFile("res://demo.tscn");
        }

        ImGui.End();

        ImGui.ShowDemoWindow();
    }
}
