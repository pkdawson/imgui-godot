using Godot;
using ImGuiGodot;
using ImGuiNET;

[Tool]
public partial class MyTool : TextureRect
{
    private bool _window1Open = true;
    private bool _window2Open = true;

    public override void _Ready()
    {
        SetProcess(false);

        // run only in editor
        if (!Engine.IsEditorHint())
            return;

        // run only if imgui-godot-native is enabled
        if (!ImGuiGD.ToolInit())
            return;

        SetProcess(true);
        ImGuiGD.Connect(ImGuiLayout);
    }

    public override void _Process(double delta)
    {
        if (_window1Open)
        {
            ImGui.Begin("tool window", ref _window1Open);

            ImGui.Text("visible when the node's scene is active");
            float rot = RotationDegrees;
            if (ImGui.DragFloat("rotation", ref rot))
                RotationDegrees = rot;
            ImGui.End();
        }
    }

    private void ImGuiLayout()
    {
        if (_window2Open)
        {
            ImGui.Begin("tool signal window", ref _window2Open);
            ImGui.Text("always visible");
            ImGui.End();
        }
    }
}
