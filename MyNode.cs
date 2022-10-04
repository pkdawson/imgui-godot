using Godot;
using ImGuiNET;
using System;

public partial class MyNode : Node
{
    public override void _Process(double delta)
    {
        ImGui.ShowDemoWindow();
    }

    private void _on_button1_pressed()
    {
        GetTree().ChangeSceneToFile("res://demo2.tscn");
    }
}
