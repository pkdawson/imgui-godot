using Godot;
using ImGuiNET;
using System;

public partial class Clicky : Button
{
    private bool show = false;
    private static readonly ImGuiWindowFlags winFlags = ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;

    public override void _Ready()
    {
        ImGuiLayer.Instance.imgui_layout += _imgui_layout;
    }

    public override void _ExitTree()
    {
        // TODO: remove after beta 3
        ImGuiLayer.Instance.imgui_layout -= _imgui_layout;
    }

    private void _on_button2_pressed()
    {
        show = !show;
    }

    private void _imgui_layout()
    {
        if (show)
        {
            ImGui.Begin("a new window", winFlags);
            ImGui.Text("you clicked the button");
            ImGui.End();
        }
    }
}
