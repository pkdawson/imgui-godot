using Godot;
using ImGuiNET;

public partial class Clicky : Button
{
    private bool show = false;
    private static readonly ImGuiWindowFlags winFlags = ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;

    private void _on_button2_pressed()
    {
        show = !show;
    }

    public override void _Process(double delta)
    {
        if (show)
        {
            ImGui.Begin("a new window", winFlags);
            ImGui.Text("you clicked the button");
            ImGui.End();
        }
    }
}
