using Godot;
using ImGuiGodot;
using ImGuiNET;

namespace DemoProject;

[Tool]
public partial class MyTool : Control
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

        // signals can be buggy, avoid them if possible
        // ImGuiGD.Connect(ImGuiLayout);
    }

    public override void _Process(double delta)
    {
        if (Visible && _window1Open)
        {
            ImGui.Begin($"tool window ({Name})", ref _window1Open);

            ImGui.Text("visible when the node's scene is active");
            float rot = RotationDegrees;
            if (ImGui.DragFloat("rotation", ref rot))
                RotationDegrees = rot;
            ImGui.End();
        }
    }

    //private void ImGuiLayout()
    //{
    //    if (_window2Open)
    //    {
    //        ImGui.Begin("tool signal window", ref _window2Open);
    //        ImGui.Text("always visible");
    //        ImGui.End();
    //    }
    //}
}
