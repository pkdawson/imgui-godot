using Godot;
using ImGuiNET;

public partial class MyNode : Node
{
    public override void _EnterTree()
    {
        if (ImGuiLayer.Instance is null)
        {
            // if the plugin is disabled, we can do some basic setup to avoid crashes
            ImGuiGD.Init();
            ImGuiGD.RebuildFontAtlas();
            ImGui.NewFrame();
        }
    }

    public override void _Process(double delta)
    {
        ImGui.ShowDemoWindow();
    }

    private void _on_button1_pressed()
    {
        GetTree().ChangeSceneToFile("res://data/demo2.tscn");
    }
}
