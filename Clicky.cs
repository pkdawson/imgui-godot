using Godot;
using ImGuiNET;
using System;

public partial class Clicky : Button
{
    private bool show = false;
    
    private void _on_button2_pressed()
    {
        show = !show;
    }

    private void _on_imgui_layout()
    {
        if (show)
        {
            ImGui.Begin("a new window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text("you clicked the button");
            ImGui.End();
        }
    }
}
