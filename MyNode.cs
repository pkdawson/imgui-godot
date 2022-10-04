using Godot;
using ImGuiNET;
using System;

public partial class MyNode : Node
{
    public override void _Ready()
    {
        ImGuiLayer.Instance.imgui_layout += _imgui_layout;
    }

    public override void _ExitTree()
    {
        // TODO: remove after beta 3
        ImGuiLayer.Instance.imgui_layout -= _imgui_layout;
    }

    private void _imgui_layout()
    {
        ImGui.ShowDemoWindow();
    }

    private void _on_button1_pressed()
    {
        GetTree().ChangeSceneToFile("res://demo2.tscn");
    }
}
