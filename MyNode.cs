using Godot;
using ImGuiNET;

public partial class MyNode : Node
{
    private void _on_imgui_layout()
    {
        ImGui.ShowDemoWindow();
    }

    private void _on_button1_pressed()
    {
        GetTree().ChangeSceneToFile("res://demo2.tscn");
    }
}
