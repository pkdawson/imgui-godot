using Godot;
using ImGuiNET;
using System;

public class Clicky : Button
{
    private bool show = false;
    
    private void _on_Button_pressed()
    {
        show = !show;
    }

    private void _on_ImGui_IGLayout()
    {
        if (show)
        {
            ImGui.Begin("a new window", ref show, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
            ImGui.Text("you clicked the button");
            ImGui.End();
        }
    }
}
